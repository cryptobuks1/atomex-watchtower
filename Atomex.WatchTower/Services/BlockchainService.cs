using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

using Atomex.WatchTower.Services.Abstract;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Blockchain.Bitcoin;
using Atomex.WatchTower.Blockchain.Litecoin;
using Atomex.WatchTower.Blockchain.Tezos;
using Atomex.WatchTower.Blockchain.Tezos.Nyx;
using Atomex.WatchTower.Blockchain.Tezos.Fa2;
using Atomex.WatchTower.Blockchain.Tezos.Fa12;
using Atomex.WatchTower.Blockchain.Ethereum;
using Atomex.WatchTower.Blockchain.Ethereum.Erc20;
using Atomex.Abstract;

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
        public TezosSettings Fa2 { get; set; }
        public TezosSettings Nyx { get; set; }
    }

    public class BlockchainService : IBlockchainService
    {
        private readonly IDictionary<string, IBlockchainApi> _blockchainApi;

        public BlockchainService(
            IOptionsMonitor<BlockchainSettings> settingsMonitor,
            ICurrencies currencies)
        {
            var settings = settingsMonitor.CurrentValue;

            _blockchainApi = new Dictionary<string, IBlockchainApi>();

            if (settings.Bitcoin != null)
                _blockchainApi.Add("BTC", new BitcoinApi(currencies, settings.Bitcoin));

            if (settings.Litecoin != null)
                _blockchainApi.Add("LTC", new LitecoinApi(currencies, settings.Litecoin));

            if (settings.Ethereum != null)
                _blockchainApi.Add("ETH", new EthereumApi(currencies, settings.Ethereum));

            if (settings.Tezos != null)
                _blockchainApi.Add("XTZ", new TezosApi(currencies, settings.Tezos));

            if (settings.Usdt != null)
                _blockchainApi.Add("USDT", new Erc20Api("USDT", currencies, settings.Usdt));

            if (settings.TzBtc != null)
                _blockchainApi.Add("TZBTC", new Fa12Api("TZBTC", currencies, settings.TzBtc));

            if (settings.Fa2 != null)
                _blockchainApi.Add("FA2", new Fa2Api("FA2", currencies, settings.Fa2));

            if (settings.Nyx != null)
                _blockchainApi.Add("NYX", new NyxApi("NYX", currencies, settings.Nyx));
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

        public async Task<BlockchainTransaction> FindLockAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address,
            ulong timeStamp,
            ulong lockTime,
            CancellationToken cancellationToken = default)
        {
            return await _blockchainApi[currency].FindLockAsync(
                secretHash: secretHash,
                contractAddress: contractAddress,
                address: address,
                timeStamp: timeStamp,
                lockTime: lockTime,
                cancellationToken: cancellationToken);
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

        public async Task<BlockchainTransaction> FindRedeemAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            return await _blockchainApi[currency].FindRedeemAsync(
                secretHash: secretHash,
                contractAddress: contractAddress,
                lockTxId: lockTxId,
                address: address,
                refundAddress: refundAddress,
                timeStamp: timeStamp,
                lockTime: lockTime,
                secretSize: secretSize,
                cancellationToken: cancellationToken);
        }

        public async Task<BlockchainTransaction> FindRefundAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            return await _blockchainApi[currency].FindRefundAsync(
                secretHash: secretHash,
                contractAddress: contractAddress,
                lockTxId: lockTxId,
                address: address,
                refundAddress: refundAddress,
                timeStamp: timeStamp,
                lockTime: lockTime,
                secretSize: secretSize,
                cancellationToken: cancellationToken);
        }
    }
}