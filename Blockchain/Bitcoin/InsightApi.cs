using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NBitcoin;

using Atomex.Currencies;
using Atomex.Entities;
using Atomex.Guard.Blockchain.Abstract;
using Atomex.Guard.Common;

namespace Atomex.Guard.Blockchain.Bitcoin
{
    public class InsightSettings
    {
        public string BaseUri { get; set; } = "https://insight.bitpay.com/";
        public bool UseCache { get; set; }
        public string PathToCache { get; set; }
    }

    public class InsightApi : IBlockchainApi
    {
        private const int DelayMs = 1000;

        private static readonly RequestLimitControl RequestLimitControl
            = new RequestLimitControl(DelayMs);

        private readonly BitcoinBased _currency;
        protected readonly InsightSettings _settings;

        public InsightApi(BitcoinBased currency, InsightSettings settings)
        {
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<BlockchainTransaction> GetTransactionAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            var requestUri = $"api/tx/{txId}";

            return await HttpHelper.GetAsync<BlockchainTransaction>(
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

                    var tx = JsonConvert.DeserializeObject<JObject>(content);

                    return new BitcoinTransaction
                    {
                        Currency      = _currency.Name,
                        TxId          = txId,
                        BlockHeight   = tx["blockheight"]?.Value<long>() ?? 0,
                        Confirmations = tx["confirmations"]?.Value<long>() ?? 0,
                        Status = tx["confirmations"].Value<int>() >= 1
                            ? TransactionStatus.Confirmed
                            : TransactionStatus.Pending,
                        Network = _currency.Network,
                        Inputs = tx["vin"]
                            ?.Select(i => new BitcoinInput
                            {
                                Index       = i["n"]?.Value<uint>() ?? 0,
                                TxId        = i["txid"]?.Value<string>(),
                                OutputIndex = i["vout"]?.Value<uint>() ?? 0,
                                ScriptSig   = i["scriptSig"]?["hex"]?.Value<string>()
                            })
                            .ToList(),
                        Outputs = tx["vout"]
                            ?.Select(o => new BitcoinOutput
                            {
                                Index        = o["n"]?.Value<uint>() ?? 0,
                                Amount       = new Money(o["value"]?.Value<decimal>() ?? 0, MoneyUnit.BTC).Satoshi,
                                SpentTxId    = o["spentTxId"]?.Value<string>(),
                                ScriptPubKey = o["scriptPubKey"]?["hex"]?.Value<string>()
                            })
                            .ToList()
                    };
                },
                useCache: _settings.UseCache,
                pathToCache: _settings.PathToCache,
                requestLimitControl: RequestLimitControl,
                cancellationToken: cancellationToken);
        }

        public Task<BlockchainTransaction> FindLockAsync(
            string secretHash,
            string contractAddress = null,
            string address = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("InsightApi does not support lock transaction search.");
        }

        public Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string secretHash,
            string contractAddress = null,
            bool useCacheOnly = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("InsightApi does not support additional lock transactions search.");
        }

        public async Task<BlockchainTransaction> FindRedeemAsync(
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
            if (!(await GetTransactionAsync(lockTxId, cancellationToken) is BitcoinTransaction lockTx))
                return null;

            for (var i = 0; i < lockTx.Outputs.Count; ++i)
            {
                var output = lockTx.Outputs[i];

                if (!BitcoinScript.IsSwapPayment(
                    script: output.ScriptPubKey,
                    secretHash: secretHash,
                    address: address,
                    refundAddress: refundAddress,
                    lockTimeStamp: (long)(timeStamp + lockTime),
                    secretSize: secretSize,
                    network: _currency.Network))
                    continue;

                if (output.SpentTxId == null)
                    return null;

                if (!(await GetTransactionAsync(output.SpentTxId, cancellationToken) is BitcoinTransaction spentTx))
                    return null;

                var input = spentTx.Inputs.First(input => input.OutputIndex == i);

                if (BitcoinScript.IsSwapRedeem(input.ScriptSig))
                    return spentTx;
            }

            return null;
        }

        public async Task<BlockchainTransaction> FindRefundAsync(
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
            if (!(await GetTransactionAsync(lockTxId, cancellationToken) is BitcoinTransaction lockTx))
                return null;

            for (var i = 0; i < lockTx.Outputs.Count; ++i)
            {
                var output = lockTx.Outputs[i];

                if (!BitcoinScript.IsSwapPayment(
                    script: output.ScriptPubKey,
                    secretHash: secretHash,
                    address: address,
                    refundAddress: refundAddress,
                    lockTimeStamp: (long)(timeStamp + lockTime),
                    secretSize: secretSize,
                    network: _currency.Network))
                    continue;

                if (output.SpentTxId == null)
                    return null;

                if (!(await GetTransactionAsync(output.SpentTxId, cancellationToken) is BitcoinTransaction spentTx))
                    return null;

                var input = spentTx.Inputs.First(input => input.OutputIndex == i);

                if (BitcoinScript.IsSwapRefund(input.ScriptSig))
                    return spentTx;
            }

            return null;
        }
    }
}