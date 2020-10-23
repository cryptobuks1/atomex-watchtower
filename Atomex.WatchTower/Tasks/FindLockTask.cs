using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

using Atomex.Common;
using Atomex.WatchTower.Services.Abstract;
using Atomex.WatchTower.Entities;
using Atomex.WatchTower.Common;
using Atomex.Abstract;

namespace Atomex.WatchTower.Tasks
{
    public class FindLockTask : SwapTask
    {
        public FindLockTask(
            ILogger<FindLockTask> logger,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrencies currencies,
            SwapParty party)
                : base(logger, dataRepository, blockchainService, currencies, party)
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

            _logger.LogDebug("Try to find lock for swap {@swapId} party {@party}", swap.Id, swap.Initiator == party ? "initiator" : "acceptor");

            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var isBitcoinBased = _currencies.IsBitcoinBased(soldCurrency);
            var digitsMultiplier = _currencies
                .GetByName(soldCurrency)
                .DigitsMultiplier;

            // if the participant has not yet given the requisites => wait
            if (party.Status == PartyStatus.Created &&
                (swap.SecretHash == null || party.Requisites == null || counterParty.Requisites == null))
            {
                if (DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(SwapTimeOutSec))
                {
                    _logger.LogDebug("Skip requsitites waiting for swap {@id}", swap.Id);

                    return Fail(swap);
                }

                return Wait(swap, RequisitesWaitingIntervalSec);
            }

            var partyLockTxs = party.Transactions
                .Where(t => t.Type == PartyTransactionType.Lock && t.Transaction.Status != TransactionStatus.Canceled);

            if (!partyLockTxs.Any() && isBitcoinBased) // if there is no lock txs in database and currency is bitcoin based => wait
            {
                if (DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(SwapTimeOutSec))
                {
                    _logger.LogDebug("Skip lock transaction waiting for swap {@id}", swap.Id);

                    return Fail(swap);
                }

                return Wait(swap, LockWaitingIntervalSec);
            }
            else if (!partyLockTxs.Any()) // if there is no lock txs in database
            {
                return await FindLockAsync(
                    swap,
                    party,
                    counterParty,
                    cancellationToken);
            }
            else // if there is lock tx in database
            {
                return await WaitForConfirmationsAsync(
                    partyLockTxs,
                    swap,
                    party,
                    counterParty,
                    cancellationToken);
            }
        }

        private async Task<TaskResult<Swap>> FindLockAsync(
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

            // try to find lock transaction in blockchain
            var lockTx = await _blockchainService.FindLockAsync(
                currency: soldCurrency,
                secretHash: swap.SecretHash,
                contractAddress: contractAddress,
                address: counterParty.Requisites.ReceivingAddress,
                timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                lockTime: party.Requisites.LockTime,
                cancellationToken: cancellationToken);

            if (lockTx == null)
            {
                if (DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(SwapTimeOutSec))
                {
                    _logger.LogDebug("Skip lock transaction waiting for swap {@id}", swap.Id);

                    return Fail(swap);
                }

                return Wait(swap, LockWaitingIntervalSec);
            }

            // check if transaction already exists in database
            var existsTx = await _dataRepository.GetTransactionAsync(
                txId: lockTx.TxId,
                currency: soldCurrency,
                cancellationToken: cancellationToken);

            // get locked amount
            var lockedAmount = lockTx.GetAmount(
                secretHash: swap.SecretHash,
                participantAddress: counterParty.Requisites.ReceivingAddress,
                refundAddress: party.Requisites.RefundAddress,
                lockTime: party.Requisites.LockTime);

            // create party lock tx
            var partyLockTx = new PartyTransaction
            {
                PartyId = party.Id,
                Type = PartyTransactionType.Lock,
                Transaction = new Transaction
                {
                    Id = existsTx?.Id ?? 0, // if exists use id
                    Currency = lockTx.Currency,
                    TxId = lockTx.TxId,
                    BlockHeight = lockTx.BlockHeight,
                    Confirmations = lockTx.Confirmations,
                    Status = lockTx.Status
                },
                TransactionId = existsTx?.Id ?? 0, // if exists use id
                Amount = lockedAmount.ToString()
            };

            // save party lock tx in db
            await _dataRepository.AddPartyTransactionAsync(
                transaction: partyLockTx,
                cancellationToken: cancellationToken);

            // notify parties about transaction
            //foreach (var userId in new[] { party.UserId, counterParty.UserId })
            //    await _pushService.NotifyAsync(
            //        userId: userId,
            //        swapId: swap.Id,
            //        currency: soldCurrency,
            //        txId: lockTx.TxId,
            //        txType: PartyTransactionType.Lock,
            //        pushType: lockTx.IsConfirmed
            //            ? PushType.Confirmed
            //            : PushType.Discovered,
            //        cancellationToken: cancellationToken);

            if (!lockTx.IsConfirmed)
                return Wait(swap, 0); // pass to waiting confirmations

            // update party status
            return await UpdatePartyStatus(swap, party, counterParty, lockedAmount, cancellationToken);
        }

        private async Task<TaskResult<Swap>> WaitForConfirmationsAsync(
            IEnumerable<PartyTransaction> partyLockTxs,
            Swap swap,
            Party party,
            Party counterParty,
            CancellationToken cancellationToken = default)
        {
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);

            var confirmedLockTx = partyLockTxs
                .FirstOrDefault(t => t.Transaction.Status == TransactionStatus.Confirmed);

            if (confirmedLockTx != null)
                return Pass(swap);

            foreach (var partyLockTx in partyLockTxs)
            {
                var lockTx = await _blockchainService.GetTransactionAsync(
                    currency: soldCurrency,
                    txId: partyLockTx.Transaction.TxId,
                    cancellationToken: cancellationToken);

                // skip unconfirmed tx
                if (lockTx == null || !lockTx.IsConfirmed)
                    continue;

                // get locked amount
                var lockedAmount = lockTx.GetAmount(
                    secretHash: swap.SecretHash,
                    participantAddress: counterParty.Requisites.ReceivingAddress,
                    refundAddress: party.Requisites.RefundAddress,
                    lockTime: party.Requisites.LockTime);

                // update party lock tx
                partyLockTx.Amount = lockedAmount.ToString();
                partyLockTx.Transaction = new Transaction
                {
                    Id = partyLockTx.Transaction.Id,
                    Currency = lockTx.Currency,
                    TxId = lockTx.TxId,
                    BlockHeight = lockTx.BlockHeight,
                    Confirmations = lockTx.Confirmations,
                    Status = lockTx.Status
                };

                // save updated party lock tx in db
                await _dataRepository.UpdatePartyTransactionAsync(
                    transaction: partyLockTx,
                    cancellationToken: cancellationToken);

                // notify parties about confirmed tx
                //foreach (var userId in new[] { party.UserId, counterParty.UserId })
                //    await _pushService.NotifyAsync(
                //        userId: userId,
                //        swapId: swap.Id,
                //        currency: soldCurrency,
                //        txId: lockTx.TxId,
                //        txType: PartyTransactionType.Lock,
                //        pushType: PushType.Confirmed,
                //        cancellationToken: cancellationToken);

                // update party status
                return await UpdatePartyStatus(swap, party, counterParty, lockedAmount, cancellationToken);
            }

            if (DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(ConfirmationsTimeOutSec))
            {
                _logger.LogDebug("Skip lock transaction confirmation waiting for swap {@id}", swap.Id);

                return Fail(swap);
            }

            return Wait(swap, ConfirmationWaitingIntervalSec);
        }

    }
}