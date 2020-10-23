using Microsoft.EntityFrameworkCore;

namespace Atomex.WatchTower.Entities
{
    [Owned]
    public class Requisites
    {
        public string SecretHash { get; set; }
        public string ReceivingAddress { get; set; }
        public string RefundAddress { get; set; }
        public decimal RewardForRedeem { get; set; }
        public ulong LockTime { get; set; }
        public string WatchTower { get; set; }

        public bool IsFilled() => ReceivingAddress != null || LockTime != 0;

        public Requisites Copy() => (Requisites)MemberwiseClone();
    }
}