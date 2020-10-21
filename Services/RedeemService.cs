using System;
using System.Threading;
using System.Threading.Tasks;

using Atomex.Entities;
using Atomex.Guard.Blockchain.Abstract;

namespace Atomex.WatchTower.Services
{
    public class RedeemService
    {
        public Task RedeemAsync(
            Swap swap,
            BlockchainTransaction initiatorRedeemTx,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();

            // todo: redeem!
        }
    }
}