using System.Threading;
using System.Threading.Tasks;
using Atomex.Entities;

namespace Atomex.Services.Abstract
{
    public interface IAddressResolver
    {
        /// <summary>
        /// Get wallet's address for <paramref name="currency"/> by <paramref name="address"/>
        /// </summary>
        /// <param name="currency">Currency</param>
        /// <param name="address">Address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Wallet's address</returns>
        Task<WalletAddress> GetAddressAsync(
            string currency,
            string address,
            CancellationToken cancellationToken = default);
    }
}