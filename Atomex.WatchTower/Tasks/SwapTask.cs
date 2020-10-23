using System;
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
    public enum SwapParty
    {
        Initiator,
        Acceptor
    }

    public abstract class SwapTask : LoopedTask<Swap>
    {
        protected const int SwapTimeOutSec = 12 * 60 * 60;          // 12 hours
        protected const int ConfirmationsTimeOutSec = 24 * 60 * 60; // 24 hours

        protected const int RequisitesWaitingIntervalSec = 10;      // 10 seconds
        protected const int LockWaitingIntervalSec = 10;            // 10 seconds
        protected const int ConfirmationWaitingIntervalSec = 20;    // 20 seconds

        protected readonly ILogger<SwapTask> _logger;
        protected readonly IDataRepository _dataRepository;
        protected readonly IBlockchainService _blockchainService;
        protected readonly ICurrencies _currencies;
        protected readonly SwapParty _party;

        protected SwapTask(
            ILogger<SwapTask> logger,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrencies currencies,
            SwapParty party)
        {
            _logger = logger;
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
            _party = party;
        }

        protected override Task<Swap> UpdateValueAsync(
            Swap value,
            CancellationToken cancellationToken = default)
        {
            return _dataRepository.GetSwapAsync(value.Id, cancellationToken);
        }

        protected TaskResult<Swap> Fail(Swap swap) =>
            new TaskResult<Swap>
            {
                Status = Status.Failed,
                Value = swap
            };

        protected TaskResult<Swap> Wait(Swap swap, int interval) =>
            new TaskResult<Swap>
            {
                Status = Status.Wait,
                Value = swap,
                Delay = TimeSpan.FromSeconds(interval)
            };

        protected TaskResult<Swap> Pass(Swap swap) =>
            new TaskResult<Swap>
            {
                Status = Status.Passed,
                Value = swap
            };

        protected async Task<TaskResult<Swap>> UpdatePartyStatus(
            Swap swap,
            Party party,
            Party counterParty,
            decimal lockedAmount,
            CancellationToken cancellationToken = default)
        {
            var soldCurrency = swap.Symbol.SoldCurrency(party.Side);
            var isBaseCurrency = swap.Symbol.BaseCurrency() == soldCurrency;

            party.Transactions = null;

            var digitsMultiplier = _currencies
                .GetByName(soldCurrency)
                .DigitsMultiplier;

            var requiredReward = counterParty.Requisites.RewardForRedeem * digitsMultiplier;
            var requiredAmount = AmountHelper.RoundAmount(isBaseCurrency ? swap.Qty : swap.Qty * swap.Price, digitsMultiplier);

            party.Status = lockedAmount >= requiredAmount - requiredReward
                ? PartyStatus.Initiated
                : PartyStatus.PartiallyInitiated;

            await _dataRepository.UpdatePartyAsync(
                party: party,
                cancellationToken: cancellationToken);

            return Pass(await UpdateValueAsync(swap, cancellationToken));
        }
    }
}