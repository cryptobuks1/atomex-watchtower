using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Services.Abstract
{
    public interface IBlockchainService
    {
        Task<BlockchainTransaction> GetTransactionAsync(
            string currency,
            string txId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<BlockchainTransaction>> FindLocksAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<BlockchainTransaction>> FindAdditionalLocksAsync(
            string currency,
            string secretHash,
            string contractAddress,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<BlockchainTransaction>> FindRedeemsAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<BlockchainTransaction>> FindRefundsAsync(
            string currency,
            string secretHash,
            string contractAddress,
            string address = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32,
            CancellationToken cancellationToken = default);
    }
}