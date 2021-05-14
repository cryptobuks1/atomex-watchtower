using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Atomex.Abstract;
using Atomex.Common;
using Atomex.WatchTower.Entities;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Blockchain.Bitcoin;
using Atomex.WatchTower.Services.Abstract;

namespace Atomex.WatchTower.Services.Searchers
{
    public enum SwapParty
    {
        Initiator,
        Acceptor
    }

    public static class SwapExtensions
    {
        public static Party GetParty(this Swap swap, SwapParty swapParty) =>
            swapParty == SwapParty.Initiator
                ? swap.Initiator
                : swap.Acceptor;

        public static Party GetCounterParty(this Swap swap, SwapParty swapParty) =>
            swapParty == SwapParty.Initiator
                ? swap.Acceptor
                : swap.Initiator;

        public static string ContractByCurrency(this Swap swap, string currency) =>
            swap.Symbol.BaseCurrency() == currency
                ? swap.BaseCurrencyContract
                : swap.QuoteCurrencyContract;

        public static SwapParty CounterParty(this SwapParty swapParty) =>
            swapParty == SwapParty.Initiator
                ? SwapParty.Acceptor
                : SwapParty.Initiator;
    }

    public partial class TransactionsSearcher
    {
        private const int NotificationTimeoutSec = 24 * 60 * 60;

        private readonly ILogger<TransactionsSearcher> _logger;
        private readonly IDataRepository _dataRepository;
        private readonly IBlockchainService _blockchainService;
        private readonly ICurrencies _currenciesProvider;

        public TransactionsSearcher(
            ILogger<TransactionsSearcher> logger,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrencies currenciesProvider)
        {
            _logger             = logger;
            _dataRepository     = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _blockchainService  = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _currenciesProvider = currenciesProvider ?? throw new ArgumentNullException(nameof(currenciesProvider));
        }

        private async Task UpsertTransactionsAsync(
            IEnumerable<(BlockchainTransaction tx, PartyTransactionType type)> transactions,
            Swap swap,
            SwapParty swapParty,
            string currency,
            CancellationToken cancellationToken = default)
        {
            var party = swap.GetParty(swapParty);

            foreach (var (tx, type) in transactions)
            {
                var existsPartyTx = party.Transactions
                    .FirstOrDefault(t => t.Transaction.TxId == tx.TxId);

                if (existsPartyTx != null)
                {
                    // update exists party tx
                    existsPartyTx.Transaction = new Transaction
                    {
                        Id            = existsPartyTx.Transaction.Id,
                        Currency      = tx.Currency,
                        TxId          = tx.TxId,
                        BlockHeight   = tx.BlockHeight,
                        Confirmations = tx.Confirmations,
                        Status        = tx.Status
                    };

                    await _dataRepository
                        .UpdatePartyTransactionAsync(existsPartyTx, cancellationToken);
                }
                else
                {
                    // add new party tx
                    var hasAmount = type == PartyTransactionType.Lock ||
                                    type == PartyTransactionType.AdditionalLock;

                    var amount = hasAmount
                        ? tx.GetAmount(
                            secretHash: swap.SecretHash,
                            participantAddress: swap
                                .GetCounterParty(swapParty)
                                .Requisites
                                .ReceivingAddress,
                            refundAddress: party.Requisites.RefundAddress,
                            timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                            lockTime: party.Requisites.LockTime,
                            secretSize: SecretSize)
                        : 0;

                    var existsTx = await _dataRepository.GetTransactionAsync(
                        txId: tx.TxId,
                        currency: currency,
                        cancellationToken: cancellationToken);

                    var partyTx = new PartyTransaction
                    {
                        PartyId     = party.Id,
                        Type        = type,
                        Transaction = new Transaction
                        {
                            Id            = existsTx?.Id ?? 0,
                            Currency      = tx.Currency,
                            TxId          = tx.TxId,
                            BlockHeight   = tx.BlockHeight,
                            Confirmations = tx.Confirmations,
                            Status        = tx.Status
                        },
                        TransactionId = existsTx?.Id ?? 0,
                        Amount = hasAmount
                            ? amount.ToString()
                            : null
                    };

                    await _dataRepository
                        .AddPartyTransactionAsync(partyTx, cancellationToken);
                }
            }
        }
    
        private async Task UpdatePartyStatusAsync(
            Party party,
            PartyStatus partyStatus,
            CancellationToken cancellationToken = default)
        {
            party.Status = partyStatus;

            await _dataRepository.UpdatePartyStatusAsync(party.Id, partyStatus, cancellationToken);
        }

        private bool IsNotificationTimeoutReached(DateTime timeStamp) =>
            DateTime.UtcNow - timeStamp.ToUniversalTime() > TimeSpan.FromSeconds(NotificationTimeoutSec);

        /// <summary>
        /// Check if there are confirmed spent transactions for swap.
        /// Also makes a more detailed check for Bitcoin based currencies.
        /// </summary>
        /// <param name="swap">Swap</param>
        /// <param name="swapParty">Side of the swap from which the lock transaction was sent</param>
        /// <param name="txs">Transactions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if lock txs are spent, otherwise false</returns>
        private async Task<bool> IsSpentByTxsAsync(
            Swap swap,
            SwapParty swapParty,
            IEnumerable<BlockchainTransaction> txs,
            CancellationToken cancellationToken = default)
        {
            var party = swap.GetParty(swapParty);
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);

            // txs currency check
            if (txs.Any(t => t.Currency != soldCurrency))
                throw new ArgumentException($"Transactions with the wrong currency were found. Expected currency is {soldCurrency}");

            var confirmedTxs = txs.Where(t => t.IsConfirmed);
            var hasConfirmedTxs = confirmedTxs.Any();

            var isBtcBased = Currencies.IsBitcoinBased(soldCurrency);

            return hasConfirmedTxs &&
                   (!isBtcBased ||
                   (isBtcBased && await IsSpentByBtcBasedTxsAsync(swap, swapParty, confirmedTxs, cancellationToken)));
        }

        /// <summary>
        /// Check if a sufficient number of outputs of lock transactions have been spent for Bitcoin based currency.
        /// </summary>
        /// <param name="swap">Swap</param>
        /// <param name="swapParty">Side of the swap from which the lock transaction was sent</param>
        /// <param name="txs">Transactions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if a sufficient amount of locked funds is spent, otherwise false</returns>
        private async Task<bool> IsSpentByBtcBasedTxsAsync(
            Swap swap,
            SwapParty swapParty,
            IEnumerable<BlockchainTransaction> txs,
            CancellationToken cancellationToken = default)
        {
            var party = swap.GetParty(swapParty);
            var counterParty = swap.GetCounterParty(swapParty);
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);

            // txs currency check
            if (txs.Any(t => t.Currency != soldCurrency))
                throw new ArgumentException($"Transactions with the wrong currency were found. Expected currency is {soldCurrency}");

            var currency = _currenciesProvider.Get<BitcoinBasedCurrency>(soldCurrency);

            var confirmedLockTxsTasks = party.Transactions
                .Where(t => (t.Type == PartyTransactionType.Lock ||
                             t.Type == PartyTransactionType.AdditionalLock) &&
                             t.Transaction.Status == TransactionStatus.Confirmed)
                .Select(t => _blockchainService.GetTransactionAsync(
                    currency: soldCurrency,
                    txId: t.Transaction.TxId,
                    cancellationToken: cancellationToken));

            var confirmedLockTxs = (await Task.WhenAll(confirmedLockTxsTasks))
                .Cast<BitcoinTransaction>()
                .ToDictionary(t => t.TxId, t => t);

            var spentAmount = 0L;

            foreach (var tx in txs.Cast<BitcoinTransaction>())
            {
                foreach (var input in tx.Inputs)
                {
                    if (!confirmedLockTxs.TryGetValue(input.TxId, out var lockTx))
                        continue; // some other tx input

                    if (input.OutputIndex >= lockTx.Outputs.Count)
                    {
                        _logger.LogWarning(
                            $"Out of range. Tx {tx.TxId} input {input.Index} has incorrect output index. " +
                            $"Actual is {input.OutputIndex}, but lock tx contains only {lockTx.Outputs.Count} outputs.");

                        continue; // out of range
                    }

                    var output = lockTx.Outputs[(int)input.OutputIndex];

                    var isLockOutput = BitcoinScript.IsSwapPayment(
                        script: output.ScriptPubKey,
                        secretHash: swap.SecretHash,
                        address: counterParty.Requisites.ReceivingAddress,
                        refundAddress: party.Requisites.RefundAddress,
                        lockTimeStamp: swap.TimeStamp.ToUnixTimeSeconds() + (long)party.Requisites.LockTime,
                        secretSize: SecretSize,
                        network: currency.Network);

                    if (isLockOutput)
                        spentAmount += output.Amount;
                }
            }

            return IsRequiredAmountLocked(swap, swapParty, spentAmount);
        }
    }
}