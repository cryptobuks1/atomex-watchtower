using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Atomex.Abstract;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Blockchain.Bitcoin;
using Atomex.WatchTower.Blockchain.Ethereum;
using Atomex.WatchTower.Blockchain.Ethereum.Erc20;
using Atomex.WatchTower.Blockchain.Litecoin;
using Atomex.WatchTower.Blockchain.Tezos;
using Atomex.WatchTower.Blockchain.Tezos.Fa12;
using Atomex.WatchTower.Blockchain.Tezos.Fa2;
using Atomex.WatchTower.Blockchain.Tezos.Nyx;
using Atomex.WatchTower.Services.Abstract;

namespace Atomex.WatchTower.Services
{
    public class BlockchainSettings
    {
        public BitcoinSettings Bitcoin { get; set; }
        public LitecoinSettings Litecoin { get; set; }
        public EthereumSettings Ethereum { get; set; }
        public TezosSettings Tezos { get; set; }
        public EthereumSettings Usdt { get; set; }
        public TezosSettings TzBtc { get; set; }
        public TezosSettings Kusd { get; set; }
        public TezosSettings Fa2 { get; set; }
        public TezosSettings Nyx { get; set; }
        public EthereumSettings Tbtc { get; set; }
        public EthereumSettings Wbtc { get; set; }
    }

    public class BlockchainService : IBlockchainService
    {
        private readonly IDictionary<string, IBlockchainApi> _blockchainApi;

        public BlockchainService(
            IOptionsMonitor<BlockchainSettings> settingsMonitor,
            ICurrencies currenciesProvider)
        {
            var settings = settingsMonitor.CurrentValue;

            _blockchainApi = new Dictionary<string, IBlockchainApi>();

            if (settings.Bitcoin != null)
                _blockchainApi.Add("BTC", new BitcoinApi(currenciesProvider, settings.Bitcoin));

            if (settings.Litecoin != null)
                _blockchainApi.Add("LTC", new LitecoinApi(currenciesProvider, settings.Litecoin));

            if (settings.Ethereum != null)
                _blockchainApi.Add("ETH", new EthereumApi(currenciesProvider, settings.Ethereum));

            if (settings.Tezos != null)
                _blockchainApi.Add("XTZ", new TezosApi(currenciesProvider, settings.Tezos));

            if (settings.Usdt != null)
                _blockchainApi.Add("USDT", new Erc20Api("USDT", currenciesProvider, settings.Usdt));

            if (settings.TzBtc != null)
                _blockchainApi.Add("TZBTC", new Fa12Api("TZBTC", currenciesProvider, settings.TzBtc));

            if (settings.TzBtc != null)
                _blockchainApi.Add("KUSD", new Fa12Api("KUSD", currenciesProvider, settings.Kusd));

            if (settings.Fa2 != null)
                _blockchainApi.Add("FA2", new Fa2Api("FA2", currenciesProvider, settings.Fa2));

            if (settings.Nyx != null)
                _blockchainApi.Add("NYX", new NyxApi("NYX", currenciesProvider, settings.Nyx));

            if (settings.Tbtc != null)
                _blockchainApi.Add("TBTC", new Erc20Api("TBTC", currenciesProvider, settings.Tbtc));

            if (settings.Wbtc != null)
                _blockchainApi.Add("WBTC", new Erc20Api("WBTC", currenciesProvider, settings.Wbtc));
        }

        public async Task<BlockchainTransaction> GetTransactionAsync(
            string currency,
            string txId,
            CancellationToken cancellationToken = default)
        {
            return await _blockchainApi[currency].GetTransactionAsync(
                txId: txId,
                cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<BlockchainTransaction>> FindLocksAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var tx = await _blockchainApi[currency].FindLockAsync(
                secretHash: secretHash,
                contractAddress: contractAddress,
                address: address,
                refundAddress: refundAddress,
                timeStamp: timeStamp,
                lockTime: lockTime,
                secretSize: secretSize,
                cancellationToken: cancellationToken);

            return tx != null
                ? new BlockchainTransaction[] { tx }
                : Enumerable.Empty<BlockchainTransaction>();
        }

        public async Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string currency,
            string secretHash,
            string contractAddress,
            CancellationToken cancellationToken = default)
        {
            return await _blockchainApi[currency].FindAdditionalLocksAsync(
                secretHash: secretHash,
                contractAddress: contractAddress,
                cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<BlockchainTransaction>> FindRedeemsAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var tx = await _blockchainApi[currency].FindRedeemAsync(
                secretHash: secretHash,
                contractAddress: contractAddress,
                address: address,
                refundAddress: refundAddress,
                timeStamp: timeStamp,
                lockTime: lockTime,
                secretSize: secretSize,
                cancellationToken: cancellationToken);

            return tx != null
                ? new BlockchainTransaction[] { tx }
                : Enumerable.Empty<BlockchainTransaction>();
        }

        public async Task<IEnumerable<BlockchainTransaction>> FindRefundsAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            var tx = await _blockchainApi[currency].FindRefundAsync(
                secretHash: secretHash,
                contractAddress: contractAddress,
                address: address,
                refundAddress: refundAddress,
                timeStamp: timeStamp,
                lockTime: lockTime,
                secretSize: secretSize,
                cancellationToken: cancellationToken);

            return tx != null
                ? new BlockchainTransaction[] { tx }
                : Enumerable.Empty<BlockchainTransaction>();
        }
    }
}