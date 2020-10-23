using System;

namespace Atomex.WatchTower.Entities
{
    public class Swap
    {
        public long Id { get; set; }
        public string Symbol { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal Price { get; set; }
        public decimal Qty { get; set; }
        public string Secret { get; set; }
        public string SecretHash { get; set; }
        public long InitiatorId { get; set; }
        public Party Initiator { get; set; }
        public long AcceptorId { get; set; }
        public Party Acceptor { get; set; }
        public string BaseCurrencyContract { get; set; }
        public string QuoteCurrencyContract { get; set; }
        public long OldId { get; set; }

        public Swap ShallowCopy() => (Swap)MemberwiseClone();
    }
}