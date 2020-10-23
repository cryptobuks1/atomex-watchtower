using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Atomex.WatchTower.Blockchain.Ethereum.Erc20.Dto
{
    [Event("Refunded")]
    public class Erc20RefundedEventDTO : IEventDTO
    {
        [Parameter("bytes32", "_hashedSecret", 1, true)]
        public byte[] HashedSecret { get; set; }
    }
}