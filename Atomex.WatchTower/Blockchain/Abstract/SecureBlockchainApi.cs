using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Atomex.WatchTower.Blockchain.Abstract
{
    public abstract class SecureBlockchainApi : IBlockchainApi
    {
        protected IList<IBlockchainApi> _apis;

        public SecureBlockchainApi()
        {
            _apis = new List<IBlockchainApi>();
        }

        public async Task<BlockchainTransaction> GetTransactionAsync(
            string txId,
            CancellationToken cancellationToken = default)
        {
            foreach (var api in _apis)
            {
                try
                {
                    return await api
                        .GetTransactionAsync(txId, cancellationToken);
                }
                catch (Exception e)
                {
                }
            }

            return null;
        }

        public async Task<BlockchainTransaction> FindLockAsync(
            string secretHash,
            string contractAddress = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            foreach (var api in _apis)
            {
                try
                {
                    return await api.FindLockAsync(
                        secretHash,
                        contractAddress,
                        address,
                        refundAddress,
                        timeStamp,
                        lockTime,
                        secretSize,
                        cancellationToken);
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        public async Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string secretHash,
            string contractAddress = null,
            bool useCacheOnly = false,
            CancellationToken cancellationToken = default)
        {
            foreach (var api in _apis)
            {
                try
                {
                    return await api.FindAdditionalLocksAsync(
                        secretHash,
                        contractAddress,
                        useCacheOnly,
                        cancellationToken);
                }
                catch (Exception)
                {
                }
            }

            return Enumerable.Empty<BlockchainTransaction>();
        }

        public async Task<BlockchainTransaction> FindRedeemAsync(
            string secretHash,
            string contractAddress = null,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            foreach (var api in _apis)
            {
                try
                {
                    return await api.FindRedeemAsync(
                        secretHash,
                        contractAddress,
                        lockTxId,
                        address,
                        refundAddress,
                        timeStamp,
                        lockTime,
                        secretSize,
                        cancellationToken);
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        public async Task<BlockchainTransaction> FindRefundAsync(
            string secretHash,
            string contractAddress = null,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default)
        {
            foreach (var api in _apis)
            {
                try
                {
                    return await api.FindRefundAsync(
                        secretHash,
                        contractAddress,
                        lockTxId,
                        address,
                        refundAddress,
                        timeStamp,
                        lockTime,
                        secretSize,
                        cancellationToken);
                }
                catch (Exception)
                {
                }
            }

            return null;
        }
    }
}