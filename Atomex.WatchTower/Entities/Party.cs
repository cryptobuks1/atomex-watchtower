using System.Collections.Generic;

using Atomex.Core;

namespace Atomex.WatchTower.Entities
{
    public enum PartyStatus
    {
        /// <summary>
        /// Swap created but details and payments have not yet sent
        /// </summary>
        Created,
        /// <summary>
        /// Requisites sent
        /// </summary>
        Involved,
        /// <summary>
        /// Own funds partially sent
        /// </summary>
        PartiallyInitiated,
        /// <summary>
        /// Own funds completely sent
        /// </summary>
        Initiated,
        /// <summary>
        /// Counterparty funds are already redeemed
        /// </summary>
        Redeemed,
        /// <summary>
        /// Own funds are already refunded
        /// </summary>
        Refunded,
        /// <summary>
        /// Own funds lost
        /// </summary>
        Lost,
        /// <summary>
        /// Own funds are already refunded and counterparty funds are already redeemed
        /// </summary>
        Jackpot
    }

    public class Party
    {
        public long Id { get; set; }
        public Requisites Requisites { get; set; }
        public Side Side { get; set; }
        public PartyStatus Status { get; set; }
        public List<PartyTransaction> Transactions { get; set; }
        public Swap InitiatorSwap { get; set; }
        public Swap AcceptorSwap { get; set; }

        public Party ShallowCopy() => (Party)MemberwiseClone();
    }
}