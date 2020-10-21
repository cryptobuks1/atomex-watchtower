using System.Numerics;

using Atomex.Entities;

namespace Atomex.Guard.Blockchain.Abstract
{
    public abstract class BlockchainTransaction : Transaction
    {
        public abstract bool IsConfirmed { get; }

        public abstract decimal GetAmount(
            string secretHash = null,
            string participantAddress = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32);

        public abstract string GetSecret(
            string secretHash = null,
            int secretSize = 32);
    }
}