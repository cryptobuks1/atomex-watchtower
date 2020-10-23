using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Atomex.WatchTower.Blockchain.Ethereum.Erc20.Dto
{
    [Event("Redeemed")]
    public class Erc20RedeemedEventDto : IEventDTO
    {
        [Parameter("bytes32", "_hashedSecret", 1, true)]
        public byte[] HashedSecret { get; set; }

        [Parameter("bytes32", "_secret", 2, false)]
        public byte[] Secret { get; set; }
    }
}