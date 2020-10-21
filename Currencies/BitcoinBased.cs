using System;
using NBitcoin;

using Atomex.Cryptography;
using Atomex.Currencies.Abstract;

namespace Atomex.Currencies
{
    public class BitcoinBased : ICurrency
    {
        public string Name { get; }
        public Network Network { get; protected set; }
        public decimal DigitsMultiplier { get; }

        public BitcoinBased(
            string name,
            Network network,
            decimal digitsMultiplier)
        {
            Name = name;
            Network = network;
            DigitsMultiplier = digitsMultiplier;
        }

        public bool IsAddressFromKey(string address, byte[] publicKey)
        {
            try
            {
                var pubKey = new PubKey(publicKey);

                var legacyAddress = pubKey
                    .GetAddress(ScriptPubKeyType.Legacy, Network)
                    .ToString();

                if (address == legacyAddress)
                    return true;

                var segwitAddress = pubKey
                    .GetAddress(ScriptPubKeyType.Segwit, Network)
                    .ToString();

                if (address == segwitAddress)
                    return true;

                var segwitP2SHAddress = pubKey
                    .GetAddress(ScriptPubKeyType.SegwitP2SH, Network)
                    .ToString();

                return address == segwitP2SHAddress;
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidAddress(string address)
        {
            try
            {
                BitcoinAddress.Create(address, Network);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool Verify(byte[] data, byte[] sign, byte[] publicKey)
        {
            var verifyHash = new PubKey(publicKey).VerifyMessage(data, Convert.ToBase64String(sign));

            if (verifyHash)
                return true;

            return Ecdsa.Verify(data, sign, publicKey, Curves.Secp256K1, Algorithms.Sha256WithEcdsa);
        }
    }
}
