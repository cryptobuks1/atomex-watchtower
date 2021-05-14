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
        public async Task<bool> FindRedeemTxsAsync(
            Swap swap,
            SwapParty swapParty,
            CancellationToken cancellationToken = default)
        {
            var party = swap.GetCounterParty(swapParty); // lock tx sender
            var counterParty = swap.GetParty(swapParty); // redeem tx sender

            // skip if already redeemed
            if (counterParty.Status == PartyStatus.Redeemed ||
                (counterParty.Status == PartyStatus.Jackpot && party.Status == PartyStatus.Lost))
                return true;

            // skip if already refunded
            if (party.Status == PartyStatus.Refunded ||
                party.Status == PartyStatus.Jackpot ||
                counterParty.Status == PartyStatus.Lost)
                return true;

            _logger.LogDebug("[Tracker] Try to find redeem for swap {@swapId} party {@party}",
                swap.Id,
                swap.Initiator == party ? "initiator" : "acceptor");

            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var contractAddress = swap.ContractByCurrency(soldCurrency);

            var redeemTxs = await _blockchainService.FindRedeemsAsync(
                currency: soldCurrency,
                secretHash: swap.SecretHash,
                contractAddress: contractAddress,
                address: counterParty.Requisites.ReceivingAddress,
                refundAddress: party.Requisites.RefundAddress,
                timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                lockTime: party.Requisites.LockTime,
                secretSize: SecretSize,
                cancellationToken: cancellationToken);

            if (redeemTxs == null || !redeemTxs.Any())
            {
                _logger.LogDebug("[Tracker] No redeem transactions found for swap {@id}, continue waiting...", swap.Id);
                return false;
            }

            // save all discovered redeem transactions to db
            // todo: transaction count control?
            await UpsertTransactionsAsync(
                transactions: redeemTxs.Select(t => (tx: t, type: PartyTransactionType.Redeem)),
                swap: swap,
                swapParty: swapParty, // the redeem tx was sent by the counterparty
                currency: soldCurrency,
                cancellationToken: cancellationToken);

            if (!await IsSpentByTxsAsync(swap, swapParty.CounterParty(), redeemTxs, cancellationToken))
                return false;

            // set status to Redeemed or Jackpot (=Refunded + Redeemed) for counterparty
            var counterPartyStatus = counterParty.Status == PartyStatus.Refunded || counterParty.Status == PartyStatus.Jackpot
                ? PartyStatus.Jackpot
                : PartyStatus.Redeemed;

            if (counterParty.Status != counterPartyStatus)
                await UpdatePartyStatusAsync(counterParty, counterPartyStatus, cancellationToken);

            // if counterparty has status Jackpot, then party has status Lost
            if (counterParty.Status == PartyStatus.Jackpot && party.Status != PartyStatus.Lost)
                await UpdatePartyStatusAsync(party, PartyStatus.Lost, cancellationToken);

            // save secret
            var secret = redeemTxs.First().GetSecret(swap.SecretHash, SecretSize);

            await _dataRepository.AddSecretAsync(swap.Id, secret, cancellationToken);

            // notify party about redeem
            if (!IsNotificationTimeoutReached(swap.TimeStamp))
            {
                _logger.LogDebug(
                    "[Tracker] Ready to send push about redeem tx for {@party} swap {@swap}",
                    swap.Initiator == party ? "initiator" : "acceptor",
                    swap.Id);

                //await _pushService.NotifyAsync(
                //    userId: party.UserId,
                //    swapId: swap.Id,
                //    symbol: swap.Symbol,
                //    txType: PartyTransactionType.Redeem,
                //    type: PushType.Confirmed,
                //    cancellationToken: cancellationToken);
            }

            return true;
        }
    }
}