using System;
using Microsoft.Extensions.Logging;

using Atomex.WatchTower.Services;
using Atomex.WatchTower.Services.Abstract;
using Atomex.Abstract;

namespace Atomex.WatchTower.Tasks
{
    public class SwapTaskFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDataRepository _dataRepository;
        private readonly IBlockchainService _blockchainService;
        private readonly ICurrencies _currencies;
        private readonly WalletService _walletService;

        public SwapTaskFactory(
            ILoggerFactory loggerFactory,
            IDataRepository dataRepository,
            IBlockchainService blockchainService,
            ICurrencies currencies,
            WalletService walletService)
        {
            _loggerFactory = loggerFactory;
            _dataRepository = dataRepository ?? throw new ArgumentNullException(nameof(dataRepository));
            _blockchainService = blockchainService ?? throw new ArgumentNullException(nameof(blockchainService));
            _currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
            _walletService = walletService;
        }

        public FindLockTask FindLock(SwapParty party) =>
            new FindLockTask(
                logger: _loggerFactory.CreateLogger<FindLockTask>(),
                dataRepository: _dataRepository,
                blockchainService: _blockchainService,
                currencies: _currencies,
                party: party);
        public FindAdditionalLocksTask FindAdditionalLocks(SwapParty party) =>
            new FindAdditionalLocksTask(
                logger: _loggerFactory.CreateLogger<FindAdditionalLocksTask>(),
                dataRepository: _dataRepository,
                blockchainService: _blockchainService,
                currencies: _currencies,
                party: party);

        public FindRefundOrRedeemTask FindRefundOrRedeem(SwapParty party) =>
            new FindRefundOrRedeemTask(
                logger: _loggerFactory.CreateLogger<FindRefundOrRedeemTask>(),
                dataRepository: _dataRepository,
                blockchainService: _blockchainService,
                currencies: _currencies,
                party: party,
                walletService: _walletService);
    }
}