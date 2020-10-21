using System;
using Microsoft.Extensions.Logging;

using Atomex.Guard.Services.Abstract;
using Atomex.Services.Abstract;
using Atomex.WatchTower.Services;

namespace Atomex.Guard.Tasks
{
    public class SwapTaskFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataRepository _dataRepository;
        private readonly IBlockchainService _blockchainService;
        private readonly ICurrenciesProvider _currenciesProvider;
        private readonly RedeemService _redeemService;
        private readonly RefundService _refundService;

        public SwapTaskFactory(
            ILoggerFactory loggerFactory,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrenciesProvider currenciesProvider,
            RedeemService redeemService,
            RefundService refundService)
        {
            _loggerFactory = loggerFactory;
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _currenciesProvider = currenciesProvider ?? throw new ArgumentNullException(nameof(currenciesProvider));
            _redeemService = redeemService;
            _refundService = refundService;
        }

        public FindLockTask FindLock(SwapParty party) =>
            new FindLockTask(
                logger: _loggerFactory.CreateLogger<FindLockTask>(),
                dataRepository: _dataRepository,
                blockchainService: _blockchainService,
                currenciesProvider: _currenciesProvider,
                party: party);
        public FindAdditionalLocksTask FindAdditionalLocks(SwapParty party) =>
            new FindAdditionalLocksTask(
                logger: _loggerFactory.CreateLogger<FindAdditionalLocksTask>(),
                dataRepository: _dataRepository,
                blockchainService: _blockchainService,
                currenciesProvider: _currenciesProvider,
                party: party);

        public FindRefundOrRedeemTask FindRefundOrRedeem(SwapParty party) =>
            new FindRefundOrRedeemTask(
                logger: _loggerFactory.CreateLogger<FindRefundOrRedeemTask>(),
                dataRepository: _dataRepository,
                blockchainService: _blockchainService,
                currenciesProvider: _currenciesProvider,
                party: party,
                redeemService: _redeemService,
                refundService: _refundService);
    }
}