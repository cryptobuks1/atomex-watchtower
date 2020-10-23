using System.Numerics;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Blockchain.Ethereum.Messages;
using Atomex.WatchTower.Entities;

namespace Atomex.WatchTower.Blockchain.Ethereum
{
    public class EthereumTransaction : BlockchainTransaction
    {
        private const long GweiInEth = 1000000000;

        public string Input { get; set; }
        public BigInteger Amount { get; set; }

        public override bool IsConfirmed => Status == TransactionStatus.Confirmed;

        public override decimal GetAmount(
            string secretHash = null,
            string participantAddress = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32)
        {
            return (decimal)Amount / GweiInEth;
        }

        public override string GetSecret(
            string secretHash = null,
            int secretSize = 32)
        {
            if (RedeemMessage.TryParse(Input, out var message))
                return message.Secret;

            return null;
        }
    }
}