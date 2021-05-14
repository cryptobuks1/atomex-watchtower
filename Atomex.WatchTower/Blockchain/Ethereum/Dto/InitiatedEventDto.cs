using System.Numerics;

using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Atomex.WatchTower.Blockchain.Ethereum.Dto
{
    [Event("Initiated")]
    public class InitiatedEventDto : IEventDTO
    {
        [Parameter("bytes32", "_hashedSecret", 1, true)]
        public byte[] HashedSecret { get; set; }

        [Parameter("address", "_participant", 2, true)]
        public string Participant { get; set; }

        [Parameter("address", "_initiator", 3, false)]
        public string Initiator { get; set; }

        [Parameter("address", "_watcher", 4, false)]
        public string Watcher { get; set; }

        [Parameter("uint256", "_refundTimestamp", 5, false)]
        public BigInteger RefundTimestamp { get; set; }

        [Parameter("uint256", "_watcherDeadline", 6, false)]
        public BigInteger WatcherDeadline { get; set; }

        [Parameter("uint256", "_value", 7, false)]
        public BigInteger Value { get; set; }

        [Parameter("uint256", "_payoff", 8, false)]
        public BigInteger RedeemFee { get; set; }
    }
}