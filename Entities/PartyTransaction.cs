namespace Atomex.Entities
{
    public enum PartyTransactionType
    {
        Lock,
        AdditionalLock,
        Redeem,
        Refund
    }

    public class PartyTransaction
    {
        public long Id { get; set; }
        public long PartyId { get; set; }
        public Party Party { get; set; }
        public long TransactionId { get; set; }
        public Transaction Transaction { get; set; }
        public PartyTransactionType Type { get; set; }
        public string Amount { get; set; }

        public PartyTransaction ShallowCopy() => (PartyTransaction)MemberwiseClone();
    }
}