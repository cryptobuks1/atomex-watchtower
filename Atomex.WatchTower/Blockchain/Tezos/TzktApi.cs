using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Atomex.Guard.Common;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Entities;

namespace Atomex.WatchTower.Blockchain.Tezos
{
    public class TzktContractSettings
    {
        public string Address { get; set; }
        public string BaseUri { get; set; } = "https://api.tzkt.io/v1/";
        public string TokenContract { get; set; }
        public long TokenId { get; set; }
        public bool Active { get; set; }
    }

    public class TzktSettings
    {
        public List<TzktContractSettings> Contracts { get; set; }

        public TzktContractSettings CurrentContract => Contracts?.FirstOrDefault(c => c.Active);
        public bool UseCache { get; set; }
        public string PathToCache { get; set; }
    }

    public class TzktApi : IBlockchainApi
    {
        protected readonly Atomex.Tezos _currency;
        protected readonly TzktSettings _settings;

        protected bool _useCache;
        protected ConcurrentDictionary<string, IEnumerable<BlockchainTransaction>> _cache;

        public TzktApi(Atomex.Tezos currency, TzktSettings settings)
        {
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _useCache = true;
            _cache = new ConcurrentDictionary<string, IEnumerable<BlockchainTransaction>>();
        }

        public virtual Task<BlockchainTransaction> GetTransactionAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            return GetTransactionAsync<BlockchainTransaction>(
                txId: txId,
                parser: txs => ParseTransactions(txs).FirstOrDefault(),
                contractSettings: _settings.CurrentContract,
                cancellationToken: cancellationToken);
        }

        public virtual async Task<BlockchainTransaction> FindLockAsync(
            string secretHash,
            string contractAddress = null,
            string address = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(c => c.Address == contractAddress);

            if (_useCache)
            {
                var lockTx = FindTxInCache<TezosTransaction>(
                    address: contractAddress,
                    predicate: t => t.IsSwapInit(secretHash, address, timeStamp + lockTime));

                if (lockTx != null && lockTx.IsConfirmed)
                    return lockTx;
            }

            var txs = await GetTransactionsAsync(
                address: contractAddress,
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            return txs?.FirstOrDefault(t => t.IsSwapInit(secretHash, address, timeStamp + lockTime));
        }

        public virtual async Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string secretHash,
            string contractAddress = null,
            bool useCacheOnly = false,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(c => c.Address == contractAddress);

            if (_useCache && FindTxsInCache<TezosTransaction>(contractAddress, t => t.IsSwapAdd(secretHash), out var cachedTxs))
            {
                if (!cachedTxs.Any() && useCacheOnly)
                    return cachedTxs;
            }

            var txs = await GetTransactionsAsync(
                address: contractAddress,
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            return txs?.Where(t => t.IsSwapAdd(secretHash));
        }

        public virtual async Task<BlockchainTransaction> FindRedeemAsync(
            string secretHash,
            string contractAddress = null,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(c => c.Address == contractAddress);

            if (_useCache)
            {
                var redeemTx = FindTxInCache<TezosTransaction>(
                    address: contractAddress,
                    predicate: t => t.IsSwapRedeem(secretHash));

                if (redeemTx != null && redeemTx.IsConfirmed)
                    return redeemTx;
            }

            var txs = await GetTransactionsAsync(
                address: contractAddress,
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            return txs?.FirstOrDefault(t => t.IsSwapRedeem(secretHash));
        }

        public virtual async Task<BlockchainTransaction> FindRefundAsync(
            string secretHash,
            string contractAddress = null,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(c => c.Address == contractAddress);

            if (_useCache)
            {
                var refundTx = FindTxInCache<TezosTransaction>(
                    address: contractAddress,
                    predicate: t => t.IsSwapRedeem(secretHash));

                if (refundTx != null && refundTx.IsConfirmed)
                    return refundTx;
            }

            var txs = await GetTransactionsAsync(
                address: contractAddress,
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            return txs?.FirstOrDefault(t => t.IsSwapRefund(secretHash));
        }

        protected async Task<T> GetTransactionAsync<T>(
            string txId,
            Func<JArray, T> parser,
            TzktContractSettings contractSettings,
            CancellationToken cancellationToken = default)
        {
            var requestUri = $"operations/transactions/{txId}";

            return await HttpHelper.GetAsync(
                baseUri: contractSettings.BaseUri,
                requestUri: requestUri,
                responseHandler: async response =>
                {
                    var content = await response.Content
                        .ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                            return default;

                        throw new Exception($"Request error code: {response.StatusCode}");
                    }

                    var txs = JsonConvert.DeserializeObject<JArray>(content);

                    return parser(txs);
                },
                useCache: _settings.UseCache,
                pathToCache: _settings.PathToCache,
                cancellationToken: cancellationToken);
        }

        private async Task<IEnumerable<TezosTransaction>> GetTransactionsAsync(
            string address,
            TzktContractSettings contractSettings,
            CancellationToken cancellationToken = default)
        {
            var txs = await GetTransactionsAsync(
                address: address,
                parser: txs => ParseTransactions(txs),
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            if (_useCache)
                SaveInCache(address, txs);

            return txs;
        }

        protected async Task<IEnumerable<T>> GetTransactionsAsync<T>(
            string address,
            Func<JArray, IEnumerable<T>> parser,
            TzktContractSettings contractSettings,
            CancellationToken cancellationToken = default)
        {
            var offset = 0;
            var limit = 10000;
            var result = new List<T>();

            while (true)
            {
                //var requestUri = $"accounts/{address}/operations?type=transaction&limit=1000";
                var requestUri = $"operations/transactions?target={address}&offset={offset}&limit={limit}";

                var txs = await HttpHelper.GetAsync(
                    baseUri: contractSettings.BaseUri,
                    requestUri: requestUri,
                    responseHandler: async response =>
                    {
                        var content = await response.Content
                            .ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            if (response.StatusCode == HttpStatusCode.NotFound)
                                return null;

                            throw new Exception($"Request error code: {response.StatusCode}");
                        }

                        var txs = JsonConvert.DeserializeObject<JArray>(content);

                        return parser(txs);
                    },
                    useCache: _settings.UseCache,
                    pathToCache: _settings.PathToCache,
                    cancellationToken: cancellationToken);

                result.AddRange(txs);

                if (txs.Count() < limit)
                    break;

                if (txs.Count() == limit)
                    offset += limit;
            }

            return result;
        }

        private IEnumerable<TezosTransaction> ParseTransactions(JArray data)
        {
            var txs = new List<TezosTransaction>();

            foreach (var op in data)
            {
                if (!(op is JObject tx))
                    continue;

                txs.Add(new TezosTransaction
                {
                    Currency = _currency.Name,
                    TxId = tx["hash"].Value<string>(),
                    BlockHeight = tx["level"]?.Value<long>() ?? 0,
                    Status = StateFromStatus(tx["status"]?.Value<string>()),
                    Amount = new BigInteger(tx["amount"]?.Value<decimal>() ?? 0),
                    To = tx["target"]?["address"]?.ToString(),
                    Params = tx["parameters"] != null
                        ? JObject.Parse(tx["parameters"].Value<string>())
                        : null
                });
            }

            return txs;
        }

        protected TransactionStatus StateFromStatus(string status) =>
            status switch
            {
                "applied" => TransactionStatus.Confirmed,
                "backtracked" => TransactionStatus.Canceled,
                "skipped" => TransactionStatus.Canceled,
                "failed" => TransactionStatus.Canceled,
                _ => throw new NotSupportedException($"Unsupported status {status}")
            };

        protected void SaveInCache(
            string contractAddress,
            IEnumerable<BlockchainTransaction> txs)
        {
            _cache.AddOrUpdate(contractAddress, txs, (c, t) => txs);
        }

        protected T FindTxInCache<T>(
            string address,
            Func<T, bool> predicate) where T : BlockchainTransaction
        {
            if (_cache.TryGetValue(address, out var txs))
                return txs.FirstOrDefault(t => predicate(t as T)) as T;

            return null;
        }

        protected bool FindTxsInCache<T>(
            string address,
            Func<T, bool> predicate,
            out IEnumerable<T> txs) where T : BlockchainTransaction
        {
            if (_cache.TryGetValue(address, out var cacheTxs))
            {
                txs = cacheTxs
                    .Where(t => predicate(t as T))
                    .Cast<T>();

                return true;
            }

            txs = null;

            return false;
        }
    }
}