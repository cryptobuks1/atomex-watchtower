using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

using Atomex.Abstract;
using Atomex.Common;
using Atomex.WatchTower.Entities;
using Atomex.WatchTower.Services.Abstract;
using Atomex.WatchTower.Services.Searchers;

namespace Atomex.WatchTower.Services
{
    public class TrackerSettings
    {
        public string ApiToken { get; set; }
        public string ApiUrl { get; set; }
    }

    public class TrackerService : IHostedService
    {
        private const int SwapTimeOutSec = 12 * 60 * 60; // 12 hours
        private const int SwapWaitingIntervalSec = 20;
        private const int UpdateFromDbInterval = 10;
        private const int UpdateFromServerInterval = 10;

        private readonly ILogger<TrackerService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataRepository _dataRepository;
        private readonly TransactionsSearcher _transactionsSearcher;
        private readonly IOptionsMonitor<TrackerSettings> _settingsMonitor;

        private readonly int _workersCount = 1; // todo: 4 workers in production
        private List<Task> _swapsHandlers;
        private CancellationTokenSource _cts;

        private readonly AsyncQueue<Swap> _activeSwaps;
        private readonly AsyncQueue<Swap> _failedSwaps;
        private readonly AsyncQueue<(Swap swap, DateTimeOffset timeStamp)> _waitingSwaps;

        private readonly HttpClient _httpClient;

        public TrackerService(
            ILoggerFactory loggerFactory,
            IOptionsMonitor<TrackerSettings> settingsMonitor,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrencies currenciesProvider)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<TrackerService>();
            _settingsMonitor = settingsMonitor;
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));

            _transactionsSearcher = new TransactionsSearcher(
                logger: loggerFactory.CreateLogger<TransactionsSearcher>(),
                dataRepository: dataRepository,
                blockchainService: blockchainService,
                currenciesProvider: currenciesProvider);

            _activeSwaps = new AsyncQueue<Swap>();
            _failedSwaps = new AsyncQueue<Swap>();
            _waitingSwaps = new AsyncQueue<(Swap swap, DateTimeOffset timeStamp)>();

            _httpClient = new HttpClient { BaseAddress = new Uri(_settingsMonitor.CurrentValue.ApiUrl) };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Tracker] Tracker service started");

            _cts = new CancellationTokenSource();

            _swapsHandlers = new List<Task>();

            for (var i = 0; i < _workersCount; ++i)
                _swapsHandlers.Add(HandleActiveSwapsAsync(_cts.Token));

            _ = HandleWaitingSwapsAsync(_cts.Token);
            _ = UpdateSwapsFromDbAsync(_cts.Token);
            _ = UpdateSwapsFromServer(_cts.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_swapsHandlers.Any(t => !t.IsCompleted))
                {
                    try
                    {
                        _cts.Cancel();
                    }
                    finally
                    {
                        await Task.WhenAll(_swapsHandlers);
                    }
                }
            }
            finally
            {
                _logger.LogInformation("[Tracker] Tracker service stopped");
            }
        }

        private async Task HandleActiveSwapsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var swap = await _activeSwaps.TakeAsync(cancellationToken);

                    // if not sync mode, swap not initiated and timeout reached => skip abandoned swap
                    if (!IsInitiated(swap) && IsSwapTimeoutReached(swap))
                    {
                        _logger.LogDebug("[Tracker] Swap {@id} is abandoned, will not wait any longer", swap.Id);

                        _failedSwaps.Add(swap);
                        continue;
                    }

                    var isSwapCompleted = await HandleSwapAsync(swap, cancellationToken);

                    if (isSwapCompleted) // swap handled, nothing to do
                        continue;

                    if (IsSwapTimeoutReached(swap))
                    {
                        _logger.LogDebug("[Tracker] Swap {@id} is abandoned, will not wait any longer", swap.Id);

                        _failedSwaps.Add(swap);
                    }
                    else
                    {
                        _waitingSwaps.Add((swap, DateTimeOffset.UtcNow));
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // nothing to do
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Tracker] Error handling active swaps.");
            }
            finally
            {
                _logger.LogDebug("[Tracker] Active swaps handling stopped.");
            }
        }

        private async Task HandleWaitingSwapsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!_waitingSwaps.TryPeek(out var item))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        continue;
                    }

                    var waitingInterval = TimeSpan.FromSeconds(SwapWaitingIntervalSec);
                    var difference = DateTimeOffset.UtcNow - item.timeStamp.ToUniversalTime();

                    if (difference < waitingInterval)
                    {
                        await Task.Delay(waitingInterval - difference);
                        continue;
                    }

                    // move swap from waiting list to active list
                    var (waitingSwap, _) = await _waitingSwaps.TakeAsync(cancellationToken);

                    _activeSwaps.Add(await GetSwap(waitingSwap.Id, cancellationToken));
                }
            }
            catch (TaskCanceledException)
            {
                // nothing to do
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Tracker] Error handling waiting swaps.");
            }
            finally
            {
                _logger.LogDebug("[Tracker] Waiting's swaps handling stopped.");
            }
        }

        private async Task<bool> HandleSwapAsync(
            Swap swap,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // firstly find initator lock txs
                var isLockedByInitiator = await _transactionsSearcher
                    .FindLockTxsAsync(swap, SwapParty.Initiator, cancellationToken);

                if (!isLockedByInitiator)
                    return false; // there are no confirmed initiator lock txs -> wait

                // then find acceptor lock txs
                var isLockedByAcceptor = await _transactionsSearcher
                    .FindLockTxsAsync(await GetSwap(swap.Id, cancellationToken), SwapParty.Acceptor, cancellationToken);

                if (!isLockedByAcceptor) // there are no confirmed acceptor lock txs -> wait for refund for initiator
                    return await _transactionsSearcher
                        .FindRefundTxsAsync(await GetSwap(swap.Id, cancellationToken), SwapParty.Initiator, cancellationToken);

                // then find initiator redeem txs
                var isRedeemedByInitiator = await _transactionsSearcher
                    .FindRedeemTxsAsync(await GetSwap(swap.Id, cancellationToken), SwapParty.Initiator, cancellationToken);

                // then find acceptor refunds txs
                var isRefundedByAcceptor = !isRedeemedByInitiator && // there are no confirmed initiator redeem txs -> wait for refund for acceptor
                    await _transactionsSearcher 
                        .FindRefundTxsAsync(await GetSwap(swap.Id, cancellationToken), SwapParty.Acceptor, cancellationToken);

                // then find acceptor redeem txs
                var isRedeemedByAcceptor = await _transactionsSearcher
                    .FindRedeemTxsAsync(await GetSwap(swap.Id, cancellationToken), SwapParty.Acceptor, cancellationToken);

                // then find initiator refunds txs
                var isRefundedByInitiator = !isRedeemedByAcceptor && // there are no confirmed acceptor redeem txs -> wait for refund for initiator
                    await _transactionsSearcher
                        .FindRefundTxsAsync(await GetSwap(swap.Id, cancellationToken), SwapParty.Initiator, cancellationToken);

                return (isRedeemedByInitiator && isRedeemedByAcceptor) ||  // redeemed
                       (isRefundedByInitiator && isRefundedByAcceptor) ||  // refunded
                       (isRedeemedByInitiator && isRefundedByInitiator) || // jackpot by initiator
                       (isRedeemedByAcceptor && isRefundedByAcceptor);     // jackpot by acceptor    
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Tracker] Error while handle swap {@swap}", swap.Id);

                return false;
            }
        }

        private bool IsSwapTimeoutReached(Swap swap) =>
            DateTime.UtcNow - swap.TimeStamp.ToUniversalTime() >= TimeSpan.FromSeconds(SwapTimeOutSec);

        private bool IsInitiated(Swap swap) =>
            swap.Initiator.Status >= PartyStatus.Initiated || swap.Acceptor.Status >= PartyStatus.Initiated;

        private Task<Swap> GetSwap(long id, CancellationToken cancellationToken = default) =>
            _dataRepository.GetSwapAsync(id, cancellationToken);

        private async Task UpdateSwapsFromDbAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var lastSwapId = 0L;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var swaps = await GetActiveSwapsAsync(
                        fromId: lastSwapId,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("[Tracker] {@count} new active swaps found in db.", swaps.Count());

                    if (swaps.Any())
                    {
                        foreach (var swap in swaps)
                            _activeSwaps.Add(swap);

                        lastSwapId = swaps.Last().Id;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(UpdateFromDbInterval), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // nothing to do
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Tracker] Error updating swaps from db.");
            }
            finally
            {
                _logger.LogDebug("[Tracker] Swaps updating stopped.");
            }
        }

        private async Task UpdateSwapsFromServer(
            CancellationToken cancellationToken = default)
        {
            try
            {
                const int limit = 100;
                var lastSwapId = 0L;
                var apiToken = _settingsMonitor.CurrentValue.ApiToken;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var response = await _httpClient
                        .GetAsync($"v1/tracker/swaps?active=true&limit={limit}&apiToken={apiToken}");

                    if (!response.IsSuccessStatusCode)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(UpdateFromServerInterval), cancellationToken);
                        continue;
                    }

                    var jsonContent = await response
                        .Content
                        .ReadAsStringAsync();

                    var swaps = JsonConvert
                        .DeserializeObject<List<Swap>>(jsonContent)
                        .Reverse<Swap>();

                    foreach (var swap in swaps)
                    {
                        if (swap.Id < lastSwapId)
                            continue;

                        lastSwapId = Math.Max(lastSwapId, swap.Id);

                        var existSwap = await _dataRepository
                            .GetSwapAsync(swap.Id, cancellationToken);

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
                                    : PartyStatus.Involved,
                            },
                            Acceptor = new Party
                            {
                                Requisites = swap.Acceptor.Requisites,
                                Side       = swap.Acceptor.Side,
                                Status     = swap.Acceptor.Status == PartyStatus.Created || swap.Acceptor.Status == PartyStatus.Involved
                                    ? swap.Acceptor.Status
                                    : PartyStatus.Involved
                            },

                            BaseCurrencyContract  = swap.BaseCurrencyContract,
                            QuoteCurrencyContract = swap.QuoteCurrencyContract,
                            OldId                 = swap.OldId
                        };

                        newSwap.Initiator.InitiatorSwap = newSwap;
                        newSwap.Acceptor.AcceptorSwap = newSwap;

                        await _dataRepository.AddSwapAsync(newSwap, cancellationToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(UpdateFromServerInterval), cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // nothing to do
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[Tracker] Error updating swaps from server.");
            }
            finally
            {
                _logger.LogDebug("[Tracker] Swaps updating from server stopped.");
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