using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using Atomex.Common;
using Atomex.WatchTower.Entities;

namespace Atomex.WatchTower.Services.Searchers
{
    public partial class TransactionsSearcher
    {
        public async Task<bool> FindRefundTxsAsync(
            Swap swap,
            SwapParty swapParty,
            CancellationToken cancellationToken = default)
        {
            var party = swap.GetParty(swapParty);
            var counterParty = swap.GetCounterParty(swapParty);

            // skip if already refunded
            if (party.Status == PartyStatus.Refunded ||
               (party.Status == PartyStatus.Jackpot && counterParty.Status == PartyStatus.Lost))
                return true;

            // skip if already redeemed or lost
            if (party.Status == PartyStatus.Lost)
                return false;

            if (swap.TimeStamp.ToUniversalTime() + TimeSpan.FromSeconds(party.Requisites.LockTime) > DateTime.UtcNow)
            {
                _logger.LogDebug(
                    "[Tracker] Refund time for {@swap} {@party} not reached.",
                    swap.Id,
                    swap.Initiator == party ? "initiator" : "acceptor");

                return false;
            }

            _logger.LogDebug("[Tracker] Try to find refund for swap {@swapId} party {@party}",
                swap.Id,
                swap.Initiator == party ? "initiator" : "acceptor");

            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var contractAddress = swap.ContractByCurrency(soldCurrency);

            var refundTxs = await _blockchainService.FindRefundsAsync(
                currency: soldCurrency,
                secretHash: swap.SecretHash,
                contractAddress: contractAddress,
                address: counterParty.Requisites.ReceivingAddress,
                refundAddress: party.Requisites.RefundAddress,
                timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                lockTime: party.Requisites.LockTime,
                secretSize: SecretSize,
                cancellationToken: cancellationToken);

            if (refundTxs == null || !refundTxs.Any())
            {
                _logger.LogDebug("[Tracker] No refund transactions found for swap {@id}, continue waiting...", swap.Id);
                return false;
            }

            // save all discovered refund transactions to db
            // todo: transaction count control?
            await UpsertTransactionsAsync(
                transactions: refundTxs.Select(t => (tx: t, type: PartyTransactionType.Refund)),
                swap: swap,
                swapParty: swapParty,
                currency: soldCurrency,
                cancellationToken: cancellationToken);

            // is refunded by txs control
            if (!await IsSpentByTxsAsync(swap, swapParty, refundTxs, cancellationToken))
                return false;

            // set status to Refunded or Jackpot (=Refunded + Redeemed) for party
            var partyStatus = party.Status == PartyStatus.Redeemed || party.Status == PartyStatus.Jackpot
                ? PartyStatus.Jackpot
                : PartyStatus.Refunded;

            if (party.Status != partyStatus)
                await UpdatePartyStatusAsync(party, partyStatus, cancellationToken);

            // if party has status Jackpot, then counterparty has status Lost
            if (party.Status == PartyStatus.Jackpot && counterParty.Status != PartyStatus.Lost)
                await UpdatePartyStatusAsync(counterParty, PartyStatus.Lost, cancellationToken);

            // notify party about refund
            if (!IsNotificationTimeoutReached(swap.TimeStamp))
            {
                _logger.LogDebug(
                    "[Tracker] Ready to send push about refund tx for {@party} swap {@swap}",
                    swap.Initiator == party ? "initiator" : "acceptor",
                    swap.Id);

                //await _pushService.NotifyAsync(
                //    userId: party.UserId,
                //    swapId: swap.Id,
                //    symbol: swap.Symbol,
                //    txType: PartyTransactionType.Refund,
                //    type: PushType.Confirmed,
                //    cancellationToken: cancellationToken);
            }

            return true;
        }
    }
}