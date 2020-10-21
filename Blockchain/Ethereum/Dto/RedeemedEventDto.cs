using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Atomex.Guard.Blockchain.Ethereum.Dto
{
    [Event("Redeemed")]
    public class RedeemedEventDto : IEventDTO
    {
        [Parameter("bytes32", "_hashedSecret", 1, true)]
        public byte[] HashedSecret { get; set; }

        [Parameter("bytes32", "_secret", 2, false)]
        public byte[] Secret { get; set; }
    }
}