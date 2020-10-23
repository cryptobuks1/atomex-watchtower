using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Atomex.Common;
using Atomex.WatchTower.Services.Abstract;
using Atomex.WatchTower.Tasks;
using Atomex.WatchTower.Common;
using Atomex.WatchTower.Entities;
using Atomex.Abstract;

namespace Atomex.WatchTower.Services
{
    public class GuardService : IHostedService
    {
        private const int SwapsUpdateIntervalSec = 30;

        private Task _workTask;
        private CancellationTokenSource _workCts;

        private readonly ILogger<GuardService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataRepository _dataRepository;
        private readonly IBlockchainService _blockchainService;
        private readonly ICurrencies _currencies;
        private readonly WalletService _walletService;

        public GuardService(
            ILoggerFactory loggerFactory,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrencies currencies,
            WalletService walletService)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<GuardService>();

            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
            _walletService = walletService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _workCts = new CancellationTokenSource();
            _workTask = DoWorkAsync(_workCts.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_workTask.IsCompleted)
            {
                try
                {
                    _workCts.Cancel();
                }
                finally
                {
                    await Task.WhenAny(_workTask, Task.Delay(Timeout.Infinite, cancellationToken));
                }
            }
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken = default)
        {
            var lastSwapId = 0L;

            while (!cancellationToken.IsCancellationRequested)
            {
                await GetNewSwapsAsync(cancellationToken);

                lastSwapId = await HandleSwapsAsync(lastSwapId, cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(SwapsUpdateIntervalSec), cancellationToken);
            }
        }

        private async Task GetNewSwapsAsync(CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api.test.atomex.me/")
            };

            var response = await httpClient.GetAsync("v1/guard/swaps?active=true");

            if (!response.IsSuccessStatusCode)
            {
                // todo: log error
                return;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            var swaps = JsonConvert.DeserializeObject<List<Swap>>(jsonResponse);

            //var hasNewSwaps = false;

            foreach (var swap in swaps)
            {
                var existSwap = await _dataRepository.GetSwapAsync(swap.Id, cancellationToken);

                if (existSwap != null)
                    continue;

                var newSwap = new Swap
                {
                    Id         = swap.Id,
                    Symbol     = swap.Symbol,
                    TimeStamp  = swap.TimeStamp,
                    Price      = swap.Price,
                    Qty        = swap.Qty,
                    SecretHash = swap.SecretHash,
                    Initiator  = new Party
                    {
                        Requisites = swap.Initiator.Requisites,
                        Side       = swap.Initiator.Side,
                        Status     = swap.Initiator.Status == PartyStatus.Created || swap.Initiator.Status == PartyStatus.Involved
                            ? swap.Initiator.Status
                            : PartyStatus.Involved
                    },
                    Acceptor = new Party
                    {
                        Requisites = swap.Acceptor.Requisites,
                        Side       = swap.Acceptor.Side,
                        Status     = swap.Acceptor.Status == PartyStatus.Created || swap.Acceptor.Status == PartyStatus.Involved
                            ? swap.Acceptor.Status
                            : PartyStatus.Involved
                    }
                };

                newSwap.Initiator.InitiatorSwap = newSwap;
                newSwap.Acceptor.AcceptorSwap = newSwap;

                await _dataRepository.AddSwapAsync(newSwap, cancellationToken);
            }
        }

        private async Task<long> HandleSwapsAsync(long lastSwapId, CancellationToken cancellationToken = default)
        {
            var swaps = await GetActiveSwapsAsync(
                fromId: lastSwapId,
                cancellationToken: cancellationToken);

            //_logger.LogInformation("{@count} active swaps found.", swaps.Count());

            foreach (var swap in swaps)
                _ = HandleSwapAsync(swap, cancellationToken);

            if (swaps.Any())
                lastSwapId = swaps.Last().Id;

            return lastSwapId;
        }

        private async Task HandleSwapAsync(
            Swap swap,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var taskFactory = new SwapTaskFactory(
                    _loggerFactory,
                    _dataRepository,
                    _blockchainService,
                    _currencies,
                    _walletService);

                await new Sheduler<Swap>()
                    .AddTask(taskFactory.FindLock(SwapParty.Initiator))                               // firstly find initator lock tx                                                                                               
                    .AddTask(taskFactory.FindAdditionalLocks(SwapParty.Initiator),                    // then find initiator additional lock txs if need
                        onFailure: new Sheduler<Swap>()
                            .AddTask(taskFactory.FindRefundOrRedeem(SwapParty.Initiator)))            // if failed find initiator refund
                    .AddTask(taskFactory.FindLock(SwapParty.Acceptor),                                // then find acceptor lock tx
                        onFailure: new Sheduler<Swap>()
                            .AddTask(taskFactory.FindRefundOrRedeem(SwapParty.Initiator)))            // if failed fund initiator refund
                    .AddTask(taskFactory.FindAdditionalLocks(SwapParty.Acceptor), FailureAction.Pass) // then find acceptor additional lock txs
                    .AddTask(taskFactory.FindRefundOrRedeem(SwapParty.Acceptor), FailureAction.Pass)  // then find acceptor refund or initiator redeem
                    .AddTask(taskFactory.FindRefundOrRedeem(SwapParty.Initiator))                     // then finnally find initiator refund or acceptor redeem
                    .RunAsync(swap, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while handle swap {@swap}", swap.Id);
            }
        }

        private Task<IEnumerable<Swap>> GetActiveSwapsAsync(
            long fromId,
            CancellationToken cancellationToken = default)
        {
            var predicates = new List<Expression<Func<Swap, bool>>>
            {
                s => s.Id > fromId,
                SwapFilters.Active
            };

            return _dataRepository.GetSwapsAsync(
                predicates: predicates,
                sort: (int)Sort.Asc,
                offset: 0,
                limit: int.MaxValue,
                cancellationToken: cancellationToken);
        }
    }
}