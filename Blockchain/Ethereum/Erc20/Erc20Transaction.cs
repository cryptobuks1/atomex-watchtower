using System.Numerics;

using Atomex.Entities;
using Atomex.Guard.Blockchain.Abstract;
using Atomex.Guard.Blockchain.Ethereum.Dto;
using Atomex.Guard.Blockchain.Ethereum.Messages;

namespace Atomex.Guard.Blockchain.Ethereum
{
    public class Erc20Transaction : BlockchainTransaction
    {
        public string Input { get; set; }

        public override bool IsConfirmed => Status == TransactionStatus.Confirmed;

        public override decimal GetAmount(
            string secretHash = null,
            string participantAddress = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32)
        {
            if (Erc20InitiateMessage.TryParse(Input, out var initiate))
                return (decimal)initiate.Value;

            return 0;
        }

        public override string GetSecret(
            string secretHash = null,
            int secretSize = 32)
        {
            if (Erc20RedeemMessage.TryParse(Input, out var message))
                return message.Secret;

            return null;
        }
    }
}