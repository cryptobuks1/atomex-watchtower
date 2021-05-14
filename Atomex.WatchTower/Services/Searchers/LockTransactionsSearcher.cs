using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Atomex.Common;
using Atomex.WatchTower.Entities;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Services.Searchers
{
    public partial class TransactionsSearcher
    {
        private const int SecretSize = 32;

        public async Task<bool> FindLockTxsAsync(
            Swap swap,
            SwapParty swapParty,
            CancellationToken cancellationToken = default)
        {
            if (IsLocked(swap, swapParty))
                return true;

            var party = swap.GetParty(swapParty);
            var counterParty = swap.GetCounterParty(swapParty);

            _logger.LogDebug(
                "[Tracker] Try to find {@party}'s locks for swap {@swapId} (secret hash: {@secretHash})",
                swap.Initiator == party ? "initiator" : "acceptor",
                swap.Id,
                swap.SecretHash
            );

            // check requisites: if the participant has not yet given the requisites => wait
            if (party.Status == PartyStatus.Created || counterParty.Status == PartyStatus.Created)
            {
                _logger.LogDebug("[Tracker] Swap {@id} is missing requisites, waiting...", swap.Id);
                return false;
            }

            // try to find all lock transactions in blockchain
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var contractAddress = swap.ContractByCurrency(soldCurrency);

            _logger.LogDebug("[Tracker] {@currency}: searching for {@contract} invocations...", soldCurrency, contractAddress);

            var lockTxs = await FindLocksAsync(swap, swapParty, cancellationToken);

            // if there is no lock txs in blockchain
            if (lockTxs == null || !lockTxs.Any())
            {
                _logger.LogDebug("[Tracker] No lock transactions found for swap {@id}, continue waiting...", swap.Id);
                return false;
            }

            // save all discovered lock transactions to db
            // todo: transaction count control?
            await UpsertTransactionsAsync(
                transactions: lockTxs,
                swap: swap,
                swapParty: swapParty,
                currency: soldCurrency,
                cancellationToken: cancellationToken);

            var confirmedLockTxs = lockTxs
                .Where(t => t.tx.Status == TransactionStatus.Confirmed);

            if (!confirmedLockTxs.Any()) // if there is no confirmed lock txs in blockchain -> wait
                return false;

            var isRequiredAmountLocked = IsRequiredAmountLocked(
                swap: swap,
                swapParty: swapParty,
                txs: confirmedLockTxs.Select(t => t.tx));

            // update swap party status
            var partyStatus = !isRequiredAmountLocked
                ? PartyStatus.PartiallyInitiated
                : PartyStatus.Initiated;

            if (party.Status != partyStatus)
                await UpdatePartyStatusAsync(party, partyStatus, cancellationToken);

            // notify counterParty if required amount locked and confirmed
            if (isRequiredAmountLocked && !IsNotificationTimeoutReached(swap.TimeStamp))
            {
                _logger.LogDebug(
                    "[Tracker] Ready to send push about lock tx for {@party} swap {@swap}",
                    swap.Initiator == counterParty ? "initiator" : "acceptor",
                    swap.Id);

                //await _pushService.NotifyAsync(
                //    userId: counterParty.UserId,
                //    swapId: swap.Id,
                //    symbol: swap.Symbol,
                //    txType: PartyTransactionType.Lock,
                //    type: PushType.Confirmed,
                //    cancellationToken: cancellationToken);
            }

            return true;
        }

        private async Task<IEnumerable<(BlockchainTransaction tx, PartyTransactionType type)>> FindLocksAsync(
            Swap swap,
            SwapParty swapParty,
            CancellationToken cancellationToken = default)
        {
            var party           = swap.GetParty(swapParty);
            var counterParty    = swap.GetCounterParty(swapParty);
            var soldCurrency    = swap.Symbol.SoldCurrency(party.Side);
            var contractAddress = swap.ContractByCurrency(soldCurrency);

            var lockTxs = await _blockchainService.FindLocksAsync(
                currency: soldCurrency,
                secretHash: swap.SecretHash,
                contractAddress: contractAddress,
                address: counterParty.Requisites.ReceivingAddress,
                refundAddress: party.Requisites.RefundAddress,
                timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                lockTime: party.Requisites.LockTime,
                secretSize: SecretSize,
                cancellationToken: cancellationToken);

            // if lock txs are not found there is no point in looking for additional lock txs
            if (lockTxs == null || !lockTxs.Any())
                return Enumerable.Empty<(BlockchainTransaction tx, PartyTransactionType type)>();

            var additionalLockTxs = await _blockchainService.FindAdditionalLocksAsync(
                currency: soldCurrency,
                secretHash: swap.SecretHash,
                contractAddress: contractAddress,
                cancellationToken: cancellationToken);

            var result = lockTxs.Select(t => (tx: t, type: PartyTransactionType.Lock));

            return additionalLockTxs == null || !additionalLockTxs.Any()
                ? result
                : result.Concat(additionalLockTxs.Select(t => (tx: t, type: PartyTransactionType.AdditionalLock)));
        }

        private bool IsLocked(Swap swap, SwapParty swapParty)
        {
            var party = swap.GetParty(swapParty);

            // check exists lock and additional lock transactions
            var confirmedLockTxs = party.Transactions
                .Where(t => (t.Type == PartyTransactionType.Lock ||
                             t.Type == PartyTransactionType.AdditionalLock) &&
                             t.Transaction.Status == TransactionStatus.Confirmed);

            return confirmedLockTxs.Any() &&
                IsRequiredAmountLocked(swap, swapParty, confirmedLockTxs) &&
                party.Status >= PartyStatus.Initiated; // Initiated, Refunded, Redeemed, Jackpot or Lost
        }

        private bool IsRequiredAmountLocked(
            Swap swap,
            SwapParty swapParty,
            IEnumerable<BlockchainTransaction> txs)
        {
            var party = swap.GetParty(swapParty);
            var counterParty = swap.GetCounterParty(swapParty);

            var lockedAmount = txs.Aggregate(0m, 
                (s, t) => s += t.GetAmount(
                    secretHash: swap.SecretHash,
                    participantAddress: counterParty.Requisites.ReceivingAddress,
                    refundAddress: party.Requisites.RefundAddress,
                    timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                    lockTime: party.Requisites.LockTime,
                    secretSize: SecretSize));

            return IsRequiredAmountLocked(swap, swapParty, lockedAmount);
        }

        private bool IsRequiredAmountLocked(
            Swap swap,
            SwapParty swapParty,
            IEnumerable<PartyTransaction> txs)
        {
            var lockedAmount = txs.Aggregate(0m, 
                (s, t) => s += decimal.Parse(t.Amount));

            return IsRequiredAmountLocked(swap, swapParty, lockedAmount);
        }

        private bool IsRequiredAmountLocked(
            Swap swap,
            SwapParty swapParty,
            decimal lockedAmount)
        {
            var party = swap.GetParty(swapParty);
            var counterParty = swap.GetCounterParty(swapParty);
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var isBaseCurrency = swap.Symbol.BaseCurrency() == soldCurrency;
            var digitsMultiplier = _currenciesProvider
                .GetByName(soldCurrency)
                .DigitsMultiplier;

            var requiredAmount = AmountHelper.RoundAmount(isBaseCurrency ? swap.Qty : swap.Qty * swap.Price, digitsMultiplier);
            var requiredReward = counterParty.Requisites.RewardForRedeem * digitsMultiplier;

            return lockedAmount >= requiredAmount - requiredReward;
        }
    }
}