using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Atomex.Currencies;
using Atomex.Entities;
using Atomex.Guard.Blockchain.Abstract;
using Atomex.Guard.Common;

namespace Atomex.Guard.Blockchain.Bitcoin
{
    public class BlockCypherSettings
    {
        public string BaseUri { get; set; }
        public bool UseCache { get; set; }
        public string PathToCache { get; set; }
    }

    public class BlockCypherApi : IBlockchainApi
    {
        private const int DelayMs = 1000;

        private static readonly RequestLimitControl RequestLimitControl
            = new RequestLimitControl(DelayMs);

        private readonly BitcoinBased _currency;
        protected readonly BlockCypherSettings _settings;

        public BlockCypherApi(BitcoinBased currency, BlockCypherSettings settings)
        {
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<BlockchainTransaction> GetTransactionAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            BitcoinTransaction result = null;

            var limit = 100;
            var instart = 0;
            var outstart = 0;

            while (true)
            {
                var requestUri = $"txs/{txId}?limit={limit}&instart={instart}&outstart={outstart}";

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

                        var tx = JsonConvert.DeserializeObject<JObject>(content);

                        var inputIndex = 0u;
                        var inputs = new List<BitcoinInput>();

                        foreach (var i in tx["inputs"])
                        {
                            inputs.Add(new BitcoinInput
                            {
                                Index       = inputIndex++,
                                TxId        = i["prev_hash"]?.Value<string>(),
                                OutputIndex = i["output_index"]?.Value<uint>() ?? 0,
                                ScriptSig   = i["script"]?.Value<string>()
                            });
                        }

                        var outputIndex = 0u;
                        var outputs = new List<BitcoinOutput>();

                        foreach (var o in tx["outputs"])
                        {
                            outputs.Add(new BitcoinOutput
                            {
                                Index        = outputIndex++,
                                Amount       = o["value"]?.Value<long>() ?? 0,
                                SpentTxId    = o["spent_by"]?.Value<string>(),
                                ScriptPubKey = o["script"]?.Value<string>()
                            });
                        }

                        return new BitcoinTransaction
                        {
                            Currency      = _currency.Name,
                            TxId          = txId,
                            BlockHeight   = tx["block_height"]?.Value<long>() ?? 0,
                            Confirmations = tx["confirmations"]?.Value<long>() ?? 0,
                            Status        = tx["confirmations"].Value<int>() >= 1
                                ? TransactionStatus.Confirmed
                                : TransactionStatus.Pending,
                            Network       = _currency.Network,
                            Inputs        = inputs,
                            Outputs       = outputs
                        };
                    },
                    useCache: _settings.UseCache,
                    pathToCache: _settings.PathToCache,
                    requestLimitControl: RequestLimitControl,
                    cancellationToken: cancellationToken);

                if (tx == null)
                    return tx;

                if (result == null)
                {
                    result = tx;
                }
                else
                {
                    if (tx.Inputs.Any())
                        result.Inputs.AddRange(tx.Inputs);

                    if (tx.Outputs.Any())
                        result.Outputs.AddRange(tx.Outputs);
                }

                if (tx.Inputs.Count < limit && tx.Outputs.Count < limit)
                    return result;

                instart += limit;
                outstart += limit;
            }
        }

        public Task<BlockchainTransaction> FindLockAsync(
            string secretHash,
            string contractAddress = null,
            string address = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("BitCoreApi does not support lock transaction search.");
        }

        public Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string secretHash,
            string contractAddress = null,
            bool useCacheOnly = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("BitCoreApi does not support additional lock transactions search.");
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