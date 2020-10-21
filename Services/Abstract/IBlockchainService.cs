using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Guard.Blockchain.Abstract;

namespace Atomex.Guard.Services.Abstract
{
    public interface IBlockchainService
    {
        Task<BlockchainTransaction> GetTransactionAsync(
            string currency,
            string txId,
            CancellationToken cancellationToken = default);

        Task<BlockchainTransaction> FindLockAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address,
            ulong timeStamp,
            ulong lockTime,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string currency,
            string secretHash,
            string contractAddress,
            CancellationToken cancellationToken = default);

        Task<BlockchainTransaction> FindRedeemAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default);

        Task<BlockchainTransaction> FindRefundAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string lockTxId = null,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default);
    }
}