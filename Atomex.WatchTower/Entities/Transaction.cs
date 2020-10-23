using System.Collections.Generic;

namespace Atomex.WatchTower.Entities
{
    public enum TransactionStatus
    {
        /// <summary>
        /// Transaction in mempool
        /// </summary>
        Pending,
        /// <summary>
        /// Transaction has at least one confirmation
        /// </summary>
        Confirmed,
        /// <summary>
        /// Transaction canceled, removed or backtracked
        /// </summary>
        Canceled
    }

    public class Transaction
    {
        public long Id { get; set; }
        public string Currency { get; set; }
        public string TxId { get; set; }
        public long BlockHeight { get; set; }
        public long Confirmations { get; set; }
        public TransactionStatus Status { get; set; }
        public List<PartyTransaction> Parties { get; set; }

        public Transaction ShallowCopy() => (Transaction)MemberwiseClone();
    }
}