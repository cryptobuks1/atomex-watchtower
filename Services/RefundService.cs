using System;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Entities;

namespace Atomex.WatchTower.Services
{
    public class RefundService
    {
        public Task RefundAsync(
            Swap swap,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
            // todo: refund!
        }
    }
}