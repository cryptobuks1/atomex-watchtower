using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Atomex.WatchTower.Blockchain.Ethereum.Dto
{
    [Event("Refunded")]
    public class RefundedEventDto : IEventDTO
    {
        [Parameter("bytes32", "_hashedSecret", 1, true)]
        public byte[] HashedSecret { get; set; }
    }
}