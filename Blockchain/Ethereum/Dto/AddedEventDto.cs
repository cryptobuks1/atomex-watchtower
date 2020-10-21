﻿using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Atomex.Guard.Blockchain.Ethereum.Dto
{
    [Event("Added")]
    public class AddedEventDto : IEventDTO
    {
        [Parameter("bytes32", "_hashedSecret", 1, true)]
        public byte[] HashedSecret { get; set; }

        [Parameter("address", "_initiator", 2, false)]
        public string Initiator { get; set; }

        [Parameter("uint256", "_value", 3, false)]
        public BigInteger Value { get; set; }
    }
}