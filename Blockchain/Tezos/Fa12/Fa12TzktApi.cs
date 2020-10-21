﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Atomex.Guard.Blockchain.Abstract;
using FA12 = Atomex.Currencies.Fa12;

namespace Atomex.Guard.Blockchain.Tezos.Fa12
{
    public class Fa12TzktApi : TzktApi
    {
        public Fa12TzktApi(FA12 currency, TzktSettings settings)
                : base(currency, settings)
        {
        }

        public override Task<BlockchainTransaction> GetTransactionAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            return GetTransactionAsync<BlockchainTransaction>(
                txId: txId,
                parser: txs => ParseTransactions(txs).FirstOrDefault(),
                contractSettings: _settings.CurrentContract,
                cancellationToken: cancellationToken);
        }

        public override async Task<BlockchainTransaction> FindLockAsync(
            string secretHash,
            string contractAddress = null,
            string address = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            CancellationToken cancellationToken = default)
        {
            var contractSettings = _settings.Contracts
                .FirstOrDefault(c => c.Address == contractAddress);

            var txs = await GetTransactionsAsync(
                address: contractAddress,
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            return txs?.FirstOrDefault(t => t.IsSwapInit(secretHash, contractSettings.TokenContract, address, timeStamp + lockTime));
        }

        public override Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string secretHash,
            string contractAddress = null,
            bool useCacheOnly = false,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<BlockchainTransaction>());
        }

        public override async Task<BlockchainTransaction> FindRedeemAsync(
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

            var txs = await GetTransactionsAsync(
                address: contractAddress,
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            return txs?.FirstOrDefault(t => t.IsSwapRedeem(secretHash));
        }

        public override async Task<BlockchainTransaction> FindRefundAsync(
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

            var txs = await GetTransactionsAsync(
                address: contractAddress,
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);

            return txs?.FirstOrDefault(t => t.IsSwapRefund(secretHash));
        }

        private Task<IEnumerable<Fa12Transaction>> GetTransactionsAsync(
            string address,
            TzktContractSettings contractSettings,
            CancellationToken cancellationToken = default)
        {
            return GetTransactionsAsync(
                address: address,
                parser: txs => ParseTransactions(txs),
                contractSettings: contractSettings,
                cancellationToken: cancellationToken);
        }

        private IEnumerable<Fa12Transaction> ParseTransactions(JArray data)
        {
            var txs = new List<Fa12Transaction>();

            foreach (var op in data)
            {
                if (!(op is JObject tx))
                    continue;

                txs.Add(new Fa12Transaction
                {
                    Currency    = _currency.Name,
                    TxId        = tx["hash"].Value<string>(),
                    BlockHeight = tx["level"]?.Value<long>() ?? 0,
                    Status      = StateFromStatus(tx["status"]?.Value<string>()),
                    To          = tx["target"]?["address"]?.ToString(),
                    Params = tx["parameters"] != null
                        ? JObject.Parse(tx["parameters"].Value<string>())
                        : null
                });
            }

            return txs;
        }
    }
}