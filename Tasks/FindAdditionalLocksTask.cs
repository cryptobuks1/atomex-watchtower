using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

using Atomex.Common;
using Atomex.Entities;
using Atomex.Guard.Common;
using Atomex.Guard.Services.Abstract;
using Atomex.Services.Abstract;
using Atomex.Guard.Services;

namespace Atomex.Guard.Tasks
{
    public class FindAdditionalLocksTask : SwapTask
    {
        public FindAdditionalLocksTask(
            ILogger<FindAdditionalLocksTask> logger,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrenciesProvider currenciesProvider,
            SwapParty party)
                : base(logger, dataRepository, blockchainService, currenciesProvider, party)
        {
        }

        protected override async Task<TaskResult<Swap>> DoInLoopAsync(
            Swap value,
            CancellationToken cancellationToken = default)
        {
            var swap = value;

            var party = _party == SwapParty.Initiator
                ? swap.Initiator
                : swap.Acceptor;

            var counterParty = _party == SwapParty.Initiator
                ? swap.Acceptor
                : swap.Initiator;

            _logger.LogDebug("Try to find additional locks for swap {@swapId} party {@party}",
                swap.Id,
                swap.Initiator == party ? "initiator" : "acceptor");

            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var isBitcoinBased = _currenciesProvider.IsBitcoinBased(soldCurrency);
            var isBaseCurrency = swap.Symbol.BaseCurrency() == soldCurrency;
            var digitsMultiplier = _currenciesProvider
                .GetByName(soldCurrency)
                .DigitsMultiplier;

            // skip bitcoin based currencies
            if (isBitcoinBased)
                return Pass(swap);

            var lockTx = party.Transactions
                .First(t => t.Type == PartyTransactionType.Lock &&
                            t.Transaction.Status == TransactionStatus.Confirmed);

            var confirmedAdditionalLockTxs = party.Transactions
                .Where(t => t.Type == PartyTransactionType.AdditionalLock &&
                            t.Transaction.Status == TransactionStatus.Confirmed);

            var confirmedAdditionalLockedAmount = confirmedAdditionalLockTxs
                .Aggregate(0m, (s, t) => s += decimal.Parse(t.Amount));

            var lockedAmount = decimal.Parse(lockTx.Amount);
            var requiredAmount = AmountHelper.RoundAmount(isBaseCurrency ? swap.Qty : swap.Qty * swap.Price, digitsMultiplier);
            var requiredReward = counterParty.Requisites.RewardForRedeem * digitsMultiplier;

            // if total locked amount enougth then skip and return
            if (lockedAmount + confirmedAdditionalLockedAmount >= requiredAmount - requiredReward)
            {
                if (party.Status == PartyStatus.PartiallyInitiated)
                {
                    party.Transactions = null;
                    party.Status = PartyStatus.Initiated;

                    await _dataRepository.UpdatePartyAsync(
                        party: party,
                        cancellationToken: cancellationToken);
                }

                return Pass(await UpdateValueAsync(swap, cancellationToken));
            }

            var additionalLockTxs = party.Transactions
                .Where(t => t.Type == PartyTransactionType.AdditionalLock &&
                            t.Transaction.Status != TransactionStatus.Canceled);

            if (!additionalLockTxs.Any()) // if there are not additional lock txs in database
            {
                return await FindAdditionalLockAsync(
                    swap,
                    party,
                    counterParty,
                    cancellationToken);
            }
            else // if there are additional lock txs in database
            {
                return await WaitForConfirmationsAsync(
                    additionalLockTxs,
                    swap,
                    party,
                    counterParty,
                    lockedAmount,
                    cancellationToken);
            }
        }

        private async Task<TaskResult<Swap>> FindAdditionalLockAsync(
            Swap swap,
            Party party,
            Party counterParty,
            CancellationToken cancellationToken = default)
        {
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var isBaseCurrency = swap.Symbol.BaseCurrency() == soldCurrency;

            var contractAddress = isBaseCurrency
                ? swap.BaseCurrencyContract
                : swap.QuoteCurrencyContract;

            var additionalLocks = await _blockchainService.FindAdditionalLocksAsync(
                currency: soldCurrency,
                secretHash: swap.SecretHash,
                contractAddress: contractAddress,
                cancellationToken: cancellationToken);

            foreach (var additionalLock in additionalLocks)
            {
                // check if transaction already exists in database
                var existsTx = await _dataRepository.GetTransactionAsync(
                    txId: additionalLock.TxId,
                    currency: soldCurrency,
                    cancellationToken: cancellationToken);

                // create additional lock tx
                var partyLockTx = new PartyTransaction
                {
                    PartyId     = party.Id,
                    Type        = PartyTransactionType.AdditionalLock,
                    Transaction = new Transaction
                    {
                        Id            = existsTx?.Id ?? 0, // if exists use id
                        Currency      = additionalLock.Currency,
                        TxId          = additionalLock.TxId,
                        BlockHeight   = additionalLock.BlockHeight,
                        Confirmations = additionalLock.Confirmations,
                        Status        = additionalLock.Status
                    },
                    TransactionId = existsTx?.Id ?? 0, // if exists use id
                    Amount = additionalLock
                        .GetAmount(
                            swap.SecretHash,
                            counterParty.Requisites.ReceivingAddress,
                            party.Requisites.RefundAddress,
                            party.Requisites.LockTime)
                        .ToString()
                };

                // save party tx in database
                await _dataRepository.AddPartyTransactionAsync(
                    transaction: partyLockTx,
                    cancellationToken: cancellationToken);

                // notify parties about transaction
                //foreach (var userId in new[] { party.UserId, counterParty.UserId })
                //    await _pushService.NotifyAsync(
                //        userId: userId,
                //        swapId: swap.Id,
                //        currency: soldCurrency,
                //        txId: additionalLock.TxId,
                //        txType: PartyTransactionType.AdditionalLock,
                //        pushType: additionalLock.IsConfirmed
                //            ? PushType.Confirmed
                //            : PushType.Discovered,
                //        cancellationToken: cancellationToken);
            }

            // if there are unconfirmed additional locks
            if (additionalLocks.Any(t => !t.IsConfirmed) || !additionalLocks.Any())
            {
                if (DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(ConfirmationsTimeOutSec))
                {
                    _logger.LogDebug("Skip additional locks confirmation waiting for swap {@id}", swap.Id);

                    return Fail(swap);
                }

                return Wait(swap, LockWaitingIntervalSec);
            }

            return Wait(swap, 0); // all additional locks confirmed
        }

        private async Task<TaskResult<Swap>> WaitForConfirmationsAsync(
            IEnumerable<PartyTransaction> partyLockTxs,
            Swap swap,
            Party party,
            Party counterParty,
            decimal lockedAmount,
            CancellationToken cancellationToken = default)
        {
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var isBaseCurrency = swap.Symbol.BaseCurrency() == soldCurrency;

            var digitsMultiplier = _currenciesProvider
                .GetByName(soldCurrency)
                .DigitsMultiplier;

            var requiredAmount = AmountHelper.RoundAmount(isBaseCurrency ? swap.Qty : swap.Qty * swap.Price, digitsMultiplier);
            var requiredReward = counterParty.Requisites.RewardForRedeem * digitsMultiplier;

            // calculate additional locked amount
            var additionalLockedAmount = partyLockTxs
                .Aggregate(0m, (s, t) => s += decimal.Parse(t.Amount));

            // if total locked amount less than required => wait
            if (lockedAmount + additionalLockedAmount < requiredAmount - requiredReward)
            {
                if (DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(SwapTimeOutSec))
                {
                    _logger.LogDebug("Skip additional locks transaction waiting for swap {@id}", swap.Id);

                    return Fail(swap);
                }

                return Wait(swap, LockWaitingIntervalSec);
            }

            // wait for confirmations
            var unconfirmedLocks = partyLockTxs
                .Where(t => t.Transaction.Status == TransactionStatus.Pending);

            var hasUnconfirmed = false;

            foreach (var unconfirmedLock in unconfirmedLocks)
            {
                var additionalLockTx = await _blockchainService.GetTransactionAsync(
                    currency: soldCurrency,
                    txId: unconfirmedLock.Transaction.TxId,
                    cancellationToken: cancellationToken);

                if (additionalLockTx == null || !additionalLockTx.IsConfirmed)
                {
                    hasUnconfirmed = true;
                    continue;
                }

                // get lock amount
                unconfirmedLock.Amount = additionalLockTx
                    .GetAmount(
                        swap.SecretHash,
                        counterParty.Requisites.ReceivingAddress,
                        party.Requisites.RefundAddress,
                        party.Requisites.LockTime)
                    .ToString();

                unconfirmedLock.Transaction = new Transaction
                {
                    Id            = unconfirmedLock.Transaction.Id,
                    Currency      = additionalLockTx.Currency,
                    TxId          = additionalLockTx.TxId,
                    BlockHeight   = additionalLockTx.BlockHeight,
                    Confirmations = additionalLockTx.Confirmations,
                    Status        = additionalLockTx.Status
                };

                // save party tx in database
                await _dataRepository.UpdatePartyTransactionAsync(
                    transaction: unconfirmedLock,
                    cancellationToken: cancellationToken);

                // notify parties about confirmed tx
                //foreach (var userId in new[] { party.UserId, counterParty.UserId })
                //    await _pushService.NotifyAsync(
                //        userId: userId,
                //        swapId: swap.Id,
                //        currency: soldCurrency,
                //        txId: additionalLockTx.TxId,
                //        txType: PartyTransactionType.AdditionalLock,
                //        pushType: PushType.Confirmed,
                //        cancellationToken: cancellationToken);
            }

            if (hasUnconfirmed)
                return Wait(swap, ConfirmationWaitingIntervalSec);

            return await UpdatePartyStatus(
                swap,
                party,
                counterParty,
                lockedAmount + additionalLockedAmount,
                cancellationToken);
        }
    }
}