using System;

namespace Atomex.Guard.Blockchain.Abstract
{
    public class TransactionEventArgs : EventArgs
    {
        public BlockchainTransaction Transaction { get; }

        public TransactionEventArgs(BlockchainTransaction transaction)
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }
    }
}