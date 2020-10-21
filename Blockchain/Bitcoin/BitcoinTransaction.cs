using System.Collections.Generic;
using System.Numerics;
using NBitcoin;

using Atomex.Common;
using Atomex.Guard.Blockchain.Abstract;
using Atomex.Cryptography;

namespace Atomex.Guard.Blockchain.Bitcoin
{
    public class BitcoinInput
    {
        public uint Index { get; set; }
        public string TxId { get; set; }
        public uint OutputIndex { get; set; }
        public string ScriptSig { get; set; }
    }

    public class BitcoinOutput
    {
        public uint Index { get; set; }
        public long Amount { get; set; }
        public string SpentTxId { get; set; }
        public string ScriptPubKey { get; set; }
    }

    public class BitcoinTransaction : BlockchainTransaction
    {
        public List<BitcoinInput> Inputs { get; set; }
        public List<BitcoinOutput> Outputs { get; set; }
        public Network Network { get; set; }

        public override bool IsConfirmed => Confirmations >= 1;

        public override decimal GetAmount(
            string secretHash = null,
            string participantAddress = null,
            string refundAddress = null,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32)
        {
            var output = GetLockOutput(
                secretHash,
                participantAddress,
                refundAddress,
                timeStamp,
                lockTime,
                secretSize);

            return output?.Amount ?? 0m;
        }

        public override string GetSecret(
            string secretHash = null,
            int secretSize = 32)
        {
            foreach (var input in Inputs)
            {
                if (!BitcoinScript.IsSwapRedeem(input.ScriptSig))
                    continue;

                var pushDatas = BitcoinScript.ExtractAllPushData(input.ScriptSig);

                foreach (var pushData in pushDatas)
                {
                    if (pushData.Length != secretSize)
                        continue;

                    var hash = Sha256
                        .Compute(pushData, iterations: 2)
                        .ToHexString();

                    if (hash == secretHash)
                        return pushData.ToHexString();
                }
            }

            return null;
        }

        private BitcoinOutput GetLockOutput(
            string secretHash,
            string participantAddress,
            string refundAddress,
            ulong timeStamp = 0,
            ulong lockTime = 0,
            int secretSize = 32)
        {
            foreach (var output in Outputs)
            {
                if (!BitcoinScript.IsSwapPayment(
                    output.ScriptPubKey,
                    secretHash,
                    participantAddress,
                    refundAddress,
                    (long)(timeStamp + lockTime),
                    secretSize,
                    Network))
                    continue;

                return output;
            }

            return null;
        }

        public static byte[] TryGetAddressHash(string address, Network network)
        {
            try
            {
                var bitcoinAddress = BitcoinAddress.Create(address, network);

                if (bitcoinAddress is BitcoinPubKeyAddress pubKeyAddress)
                    return pubKeyAddress.Hash.ToBytes();

                if (bitcoinAddress is BitcoinWitPubKeyAddress witPubKeyAddress)
                    return witPubKeyAddress.Hash.ToBytes();

                if (bitcoinAddress is BitcoinScriptAddress scriptAddress)
                    return scriptAddress.Hash.ToBytes();

                if (bitcoinAddress is BitcoinWitScriptAddress witScriptAddress)
                    return witScriptAddress.Hash.ToBytes();
            }
            finally
            {
            }

            return null;
        }
    }
}