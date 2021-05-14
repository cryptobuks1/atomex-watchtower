using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nethereum.Hex.HexTypes;

using Atomex.Guard.Common;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Blockchain.Ethereum.Messages;
using Atomex.WatchTower.Blockchain.Ethereum.Dto;

namespace Atomex.WatchTower.Blockchain.Ethereum
{
    public class EtherScanContractSettings
    {
        public string Address { get; set; }
        public ulong StartBlock { get; set; }
    }

    public class EtherScanSettings
    {
        public string BaseUri { get; set; } = "https://api.etherscan.io/";
        public string ApiToken { get; set; }
        public List<EtherScanContractSettings> Contracts { get; set; }
        public string TokenContract { get; set; }
        public bool UseCache { get; set; }
        public string PathToCache { get; set; }
    }

    public class EtherScanApi : IBlockchainApi
    {
        private const int DelayMs = 500;

        protected static readonly RequestLimitControl RequestLimitControl
            = new RequestLimitControl(DelayMs);

        protected readonly Atomex.Ethereum _currency;
        protected readonly EtherScanSettings _settings;

        public EtherScanApi(Atomex.Ethereum currency, EtherScanSettings settings)
        {
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public virtual async Task<BlockchainTransaction> GetTransactionAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            return await GetTransactionAsync(
                txId: txId,
                transactionCreator: tx =>
                {
                    return new EthereumTransaction
                    {
                        Currency = _currency.Name,
                        TxId = txId,
                        BlockHeight = (long)new HexBigInteger(tx["blockNumber"].Value<string>()).Value,
                        Input = tx["input"]?.Value<string>(),
                        Amount = new HexBigInteger(tx["value"].Value<string>()).Value
                    };
                },
                cancellationToken: cancellationToken);
        }

        protected async Task<T> GetTransactionAsync<T>(
            string txId,
            Func<JToken, T> transactionCreator,
            CancellationToken cancellationToken = default) where T : BlockchainTransaction
        {
            var requestUri = $"api?module=proxy&" +
                $"action=eth_getTransactionByHash&" +
                $"txhash={txId}&" +
                $"apikey={_settings.ApiToken}";

            var tx = await HttpHelper.GetAsync(
                baseUri: _settings.BaseUri,
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

                    var tx = JsonConvert.DeserializeObject<JObject>(content)?["result"];

                    if (tx == null)
                        return null;

                    return transactionCreator(tx);
                },
                useCache: _settings.UseCache,
                pathToCache: _settings.PathToCache,
                requestLimitControl: RequestLimitControl,
                cancellationToken: cancellationToken);

            requestUri = $"api?module=transaction&" +
                $"action=gettxreceiptstatus&" +
                $"txhash={txId}&" +
                $"apikey={_settings.ApiToken}";

            var txReceipt = await HttpHelper.GetAsync<int?>(
                baseUri: _settings.BaseUri,
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

                    var txReceipt = JsonConvert.DeserializeObject<JObject>(content);

                    if (txReceipt?["status"]?.Value<string>() == "0")
                        throw new Exception($"Request error code: {txReceipt?["message"]?.Value<string>()}. Description: {txReceipt?["result"]?.Value<string>()}");

                    var status = txReceipt?["result"]?["status"]?.Value<string>();

                    if (status == "")
                        return null;

                    return status != null
                        ? int.Parse(status)
                        : 0;
                },
                useCache: _settings.UseCache,
                pathToCache: _settings.PathToCache,
                requestLimitControl: RequestLimitControl,
                cancellationToken: cancellationToken);

            if (txReceipt == null)
                return null;

            tx.Status = txReceipt.Value == 0
                ? Entities.TransactionStatus.Canceled
                : Entities.TransactionStatus.Confirmed;

            var blockNumber = await GetRecentBlockAsync(cancellationToken);

            if (blockNumber == null)
                return null;

            tx.Confirmations = blockNumber.Value - tx.BlockHeight;

            return tx;
        }

        public virtual async Task<BlockchainTransaction> FindLockAsync(
            string secretHash,
            string contractAddress = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(s => s.Address.Equals(contractAddress, StringComparison.OrdinalIgnoreCase));

            if (contractSettings == null)
                throw new Exception($"Unknown contract address {contractAddress}");

            var events = await GetContractEventsAsync(
                address: contractAddress,
                fromBlock: contractSettings.StartBlock,
                toBlock: ulong.MaxValue,
                topic0: EventSignatureExtractor.GetSignatureHash<InitiatedEventDto>(),
                topic1: "0x" + secretHash,
                topic2: "0x000000000000000000000000" + address[2..],
                cancellationToken: cancellationToken);

            if (events == null || !events.Any())
                return null;
            
            if (!(await GetTransactionAsync(events.First().HexTransactionHash, cancellationToken) is EthereumTransaction lockTx))
                return null;

            InitiateMessage.TryParse(lockTx.Input, out var initiateMessage);

            if (initiateMessage.RefundTimeStamp < (long)(timeStamp + lockTime))
                return null;

            return lockTx;
        }

        public virtual Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string secretHash,
            string contractAddress = null,
            bool useCacheOnly = false,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<BlockchainTransaction>());
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
                .FirstOrDefault(s => s.Address.Equals(contractAddress, StringComparison.OrdinalIgnoreCase));

            if (contractSettings == null)
                throw new Exception($"Unknown contract address {contractAddress}");

            var events = await GetContractEventsAsync(
                address: contractAddress,
                fromBlock: contractSettings.StartBlock,
                toBlock: ulong.MaxValue,
                topic0: EventSignatureExtractor.GetSignatureHash<RedeemedEventDto>(),
                topic1: "0x" + secretHash,
                cancellationToken: cancellationToken);

            if (events == null || !events.Any())
                return null;

            return await GetTransactionAsync(events.First().HexTransactionHash, cancellationToken);
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
                .FirstOrDefault(s => s.Address.Equals(contractAddress, StringComparison.OrdinalIgnoreCase));

            if (contractSettings == null)
                throw new Exception($"Unknown contract address {contractAddress}");

            var events = await GetContractEventsAsync(
                address: contractAddress,
                fromBlock: contractSettings.StartBlock,
                toBlock: ulong.MaxValue,
                topic0: EventSignatureExtractor.GetSignatureHash<RefundedEventDto>(),
                topic1: "0x" + secretHash,
                cancellationToken: cancellationToken);

            if (events == null || !events.Any())
                return null;

            return await GetTransactionAsync(events.First().HexTransactionHash, cancellationToken);
        }

        public async Task<long?> GetRecentBlockAsync(
            CancellationToken cancellationToken = default)
        {
            return await HttpHelper.GetAsync<long?>(
                baseUri: _settings.BaseUri,
                requestUri: $"api?module=proxy&action=eth_blockNumber&apikey={_settings.ApiToken}",
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

                    var blockNumber = JsonConvert.DeserializeObject<JObject>(content)?["result"];

                    if (blockNumber == null)
                        return null;

                    return (long)new HexBigInteger(blockNumber.Value<string>()).Value;
                },
                useCache: _settings.UseCache,
                pathToCache: _settings.PathToCache,
                requestLimitControl: RequestLimitControl,
                cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<ContractEvent>> GetContractEventsAsync(
            string address,
            ulong fromBlock = ulong.MinValue,
            ulong toBlock = ulong.MaxValue,
            CancellationToken cancellationToken = default,
            params string[] topics)
        {
            var fromBlockStr = BlockNumberToStr(fromBlock);
            var toBlockStr = BlockNumberToStr(toBlock);
            var topicsStr = TopicsToStr(topics);

            var uri = $"api?module=logs&" +
                $"action=getLogs&" +
                $"address={address}&" +
                $"fromBlock={fromBlockStr}&" +
                $"toBlock={toBlockStr}{topicsStr}&" +
                $"apikey={_settings.ApiToken}";

            return await HttpHelper.GetAsync(
                baseUri: _settings.BaseUri,
                requestUri: uri,
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

                    return JsonConvert.DeserializeObject<Response<List<ContractEvent>>>(content).Result;
                },
                useCache: _settings.UseCache,
                pathToCache: _settings.PathToCache,
                requestLimitControl: RequestLimitControl,
                cancellationToken: cancellationToken);
        }

        public Task<IEnumerable<ContractEvent>> GetContractEventsAsync(
            string address,
            ulong fromBlock,
            ulong toBlock,
            string topic0,
            string topic1,
            CancellationToken cancellationToken = default) =>
            GetContractEventsAsync(address, fromBlock, toBlock, cancellationToken, topic0, topic1);

        public Task<IEnumerable<ContractEvent>> GetContractEventsAsync(
            string address,
            ulong fromBlock,
            ulong toBlock,
            string topic0,
            string topic1,
            string topic2,
            CancellationToken cancellationToken = default) =>
            GetContractEventsAsync(address, fromBlock, toBlock, cancellationToken, topic0, topic1, topic2);

        public Task<IEnumerable<ContractEvent>> GetContractEventsAsync(
            string address,
            ulong fromBlock,
            ulong toBlock,
            string topic0,
            string topic1,
            string topic2,
            string topic3,
            CancellationToken cancellationToken = default) =>
            GetContractEventsAsync(address, fromBlock, toBlock, cancellationToken, topic0, topic1, topic2, topic3);

        private static string BlockNumberToStr(ulong blockNumber)
        {
            if (blockNumber == ulong.MaxValue)
                return "latest";

            // "earlest" and "pending" not supported by EtherScan yet

            return blockNumber.ToString();
        }

        private static string TopicsToStr(params string[] topics)
        {
            var result = string.Empty;

            if (topics == null)
                return result;

            var lastTopic = -1;

            for (var i = 0; i < topics.Length; ++i)
            {
                if (topics[i] == null)
                    continue;

                if (lastTopic != -1)
                    result += $"&topic{lastTopic}_{i}_opr=and";

                result += $"&topic{i}={topics[i]}";

                lastTopic = i;
            }

            return result;
        }
    }
}