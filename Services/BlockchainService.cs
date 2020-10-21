using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

using Atomex.Guard.Blockchain.Abstract;
using Atomex.Guard.Blockchain.Bitcoin;
using Atomex.Guard.Blockchain.Ethereum;
using Atomex.Guard.Blockchain.Ethereum.Erc20;
using Atomex.Guard.Blockchain.Litecoin;
using Atomex.Guard.Blockchain.Tezos;
using Atomex.Guard.Blockchain.Tezos.Fa12;
using Atomex.Guard.Services.Abstract;
using Atomex.Services.Abstract;
using Atomex.Guard.Blockchain.Tezos.Fa2;
using Atomex.Guard.Blockchain.Tezos.Nyx;

namespace Atomex.Guard.Services
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
            ICurrenciesProvider currenciesProvider)
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

            if (settings.Fa2 != null)
                _blockchainApi.Add("FA2", new Fa2Api("FA2", currenciesProvider, settings.Fa2));

            if (settings.Nyx != null)
                _blockchainApi.Add("NYX", new NyxApi("NYX", currenciesProvider, settings.Nyx));
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