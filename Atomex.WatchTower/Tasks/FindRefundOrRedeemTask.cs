using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

using Atomex.Common;
using Atomex.WatchTower.Services;
using Atomex.WatchTower.Services.Abstract;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Entities;
using Atomex.WatchTower.Common;
using Atomex.Abstract;

namespace Atomex.WatchTower.Tasks
{
    public class FindRefundOrRedeemTask : SwapTask
    {
        private const int SecretSize = 32;

        private WalletService _walletService;

        public FindRefundOrRedeemTask(
            ILogger<FindRefundOrRedeemTask> logger,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrencies currencies,
            SwapParty party,
            WalletService walletService)
                : base(logger, dataRepository, blockchainService, currencies, party)
        {
            _walletService = walletService;
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

            _logger.LogDebug("Try to find refund or redeem for swap {@swapId} party {@party}",
                swap.Id,
                swap.Initiator == party ? "initiator" : "acceptor");

            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var isBaseCurrency = swap.Symbol.BaseCurrency() == soldCurrency;
            var contractAddress = isBaseCurrency
                ? swap.BaseCurrencyContract
                : swap.QuoteCurrencyContract;

            var lockTxId = party.Transactions
                .First(t => t.Type == PartyTransactionType.Lock)
                .Transaction
                .TxId;

            var result = await FindTxAsync(
                type: PartyTransactionType.Redeem,
                soldCurrency: soldCurrency,
                swap: swap,
                party: counterParty,
                counterParty: party,
                txProvider: () =>
                {
                    return _blockchainService.FindRedeemAsync(
                        currency: soldCurrency,
                        secretHash: swap.SecretHash,
                        contractAddress: contractAddress,
                        lockTxId: lockTxId,
                        address: counterParty.Requisites.ReceivingAddress,
                        refundAddress: party.Requisites.RefundAddress,
                        timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                        lockTime: party.Requisites.LockTime,
                        secretSize: SecretSize,
                        cancellationToken: cancellationToken);
                },
                txFound: async tx =>
                {
                    try
                    {
                        if (_party == SwapParty.Acceptor)
                            await _walletService.RedeemAsync(swap, tx, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        // todo: error
                    }
                },
                cancellationToken: cancellationToken);

            if (result.Status != Status.Wait)
                return result;

            // if redeem is not confirmed and refund time has not been reached
            if (swap.TimeStamp.ToUniversalTime().AddSeconds(party.Requisites.LockTime) > DateTime.UtcNow)
                return Wait(result.Value, ConfirmationWaitingIntervalSec);

            if (_party == SwapParty.Acceptor)
                await _walletService.RefundAsync(swap, cancellationToken);

            result = await FindTxAsync(
                type: PartyTransactionType.Refund,
                soldCurrency: soldCurrency,
                swap: swap,
                party: party,
                counterParty: counterParty,
                txProvider: () =>
                {
                    return _blockchainService.FindRefundAsync(
                        currency: soldCurrency,
                        secretHash: swap.SecretHash,
                        contractAddress: contractAddress,
                        lockTxId: lockTxId,
                        address: counterParty.Requisites.ReceivingAddress,
                        refundAddress: party.Requisites.RefundAddress,
                        timeStamp: (ulong)swap.TimeStamp.ToUnixTimeSeconds(),
                        lockTime: party.Requisites.LockTime,
                        secretSize: SecretSize,
                        cancellationToken: cancellationToken);
                },
                cancellationToken: cancellationToken);

            if (result.Status != Status.Wait)
                return result;

            if (DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(SwapTimeOutSec))
            {
                _logger.LogDebug("Skip refund transaction waiting for swap {@id}", swap.Id);

                return Fail(result.Value);
            }

            return result;
        }

        private async Task<TaskResult<Swap>> FindTxAsync(
            PartyTransactionType type,
            string soldCurrency,
            Swap swap,
            Party party,
            Party counterParty,
            Func<Task<BlockchainTransaction>> txProvider,
            Action<BlockchainTransaction> txFound = null,
            CancellationToken cancellationToken = default)
        {
            var partyTx = party.Transactions
                .FirstOrDefault(t => t.Type == type &&
                                     t.Transaction.Status != TransactionStatus.Canceled);

            if (partyTx != null &&
                partyTx.Transaction.Status == TransactionStatus.Confirmed)
            {
                return Pass(swap);
            }

            var tx = await txProvider();

            if (tx != null)
                txFound?.Invoke(tx);

            // if party tx already exists and txId equal => update if confirmed
            if (partyTx != null && tx != null && partyTx.Transaction.TxId == tx.TxId)
            {
                if (!tx.IsConfirmed)
                    return Wait(swap, ConfirmationWaitingIntervalSec);

                await _dataRepository.UpdateTransactionAsync(
                    transaction: new Transaction
                    {
                        Id = partyTx.Transaction.Id,
                        Currency = tx.Currency,
                        TxId = tx.TxId,
                        BlockHeight = tx.BlockHeight,
                        Confirmations = tx.Confirmations,
                        Status = tx.Status
                    },
                    cancellationToken: cancellationToken);

                // notify parties about confirmed tx
                //foreach (var userId in new[] { party.UserId, counterParty.UserId })
                //    await _pushService.NotifyAsync(
                //        userId: userId,
                //        swapId: swap.Id,
                //        currency: soldCurrency,
                //        txId: tx.TxId,
                //        txType: type,
                //        pushType: PushType.Confirmed,
                //        cancellationToken: cancellationToken);
            }
            else
            {
                if (partyTx != null)
                {
                    await _dataRepository.RemovePartyTransactionAsync(
                        id: partyTx.Id,
                        cancellationToken: cancellationToken);

                    await _dataRepository.RemoveTransactionAsync(
                        id: partyTx.Transaction.Id,
                        cancellationToken: cancellationToken);
                }

                if (tx == null)
                    return Wait(swap, ConfirmationWaitingIntervalSec);

                // check if transaction already exists in database
                var existsTx = await _dataRepository.GetTransactionAsync(
                    txId: tx.TxId,
                    currency: soldCurrency,
                    cancellationToken: cancellationToken);

                partyTx = new PartyTransaction
                {
                    PartyId = party.Id,
                    Type = type,
                    Transaction = new Transaction
                    {
                        Id = existsTx?.Id ?? 0,
                        Currency = tx.Currency,
                        TxId = tx.TxId,
                        BlockHeight = tx.BlockHeight,
                        Confirmations = tx.Confirmations,
                        Status = tx.Status
                    },
                    TransactionId = existsTx?.Id ?? 0, // if exists use id
                };

                await _dataRepository.AddPartyTransactionAsync(
                    transaction: partyTx,
                    cancellationToken: cancellationToken);

                // notify parties about transaction
                //foreach (var userId in new[] { party.UserId, counterParty.UserId })
                //    await _pushService.NotifyAsync(
                //        userId: userId,
                //        swapId: swap.Id,
                //        currency: soldCurrency,
                //        txId: tx.TxId,
                //        txType: type,
                //        pushType: tx.IsConfirmed
                //            ? PushType.Confirmed
                //            : PushType.Discovered,
                //        cancellationToken: cancellationToken);

                if (!tx.IsConfirmed)
                    return Wait(swap, ConfirmationWaitingIntervalSec);
            }

            swap = await UpdatePartiesAsync(
                id: swap.Id,
                party: party,
                counterParty: counterParty,
                status: type == PartyTransactionType.Redeem
                    ? PartyStatus.Redeemed
                    : PartyStatus.Refunded,
                cancellationToken: cancellationToken);

            return type == PartyTransactionType.Redeem
                ? Pass(await UpdateSecretAsync(swap, tx.GetSecret(swap.SecretHash, SecretSize)))
                : Pass(swap);
        }

        private async Task<Swap> UpdatePartiesAsync(
            long id,
            Party party,
            Party counterParty,
            PartyStatus status,
            CancellationToken cancellationToken = default)
        {
            var alterStatus = status == PartyStatus.Redeemed
                ? PartyStatus.Refunded
                : PartyStatus.Redeemed;

            // update party status to Status or Jackpot
            party.Transactions = null;
            party.Status = party.Status != alterStatus && party.Status != PartyStatus.Jackpot
                ? status
                : PartyStatus.Jackpot;

            await _dataRepository.UpdatePartyAsync(
                party: party,
                cancellationToken: cancellationToken);

            // update counter party status to Lost if party status is Jackpot
            if (party.Status == PartyStatus.Jackpot && counterParty.Status != PartyStatus.Lost)
            {
                counterParty.Transactions = null;
                counterParty.Status = PartyStatus.Lost;

                await _dataRepository.UpdatePartyAsync(
                    party: counterParty,
                    cancellationToken: cancellationToken);
            }

            return await _dataRepository.GetSwapAsync(
                id: id,
                cancellationToken: cancellationToken);
        }

        private async Task<Swap> UpdateSecretAsync(
            Swap swap,
            string secret,
            CancellationToken cancellationToken = default)
        {
            swap.Secret = secret;

            await _dataRepository.UpdateSwapAsync(swap, cancellationToken);

            return swap;
        }
    }
}