using Nethereum.Signer;
using Nethereum.Util;

using Atomex.Cryptography;
using Atomex.Currencies.Abstract;
using Atomex.Services;

namespace Atomex.Currencies
{
    public class Ethereum : ICurrency
    {
        public string Name { get; }
        public decimal DigitsMultiplier { get; }

        public Ethereum(CurrencySettings settings)
        {
            Name = settings.Name;
            DigitsMultiplier = settings.DigitsMultiplier;
        }

        public bool IsAddressFromKey(string address, byte[] publicKey)
        {
            var addressFromKey = new EthECKey(publicKey, false)
                .GetPublicAddress()
                .ToLowerInvariant();
            
            return addressFromKey == address.ToLowerInvariant();
        }

        public bool IsValidAddress(string address)
        {
            return new AddressUtil()
                .IsValidEthereumAddressHexFormat(address);
        }

        public bool Verify(byte[] data, byte[] sign, byte[] publicKey)
        {
            var verifyHash = new EthECKey(publicKey, false)
                .Verify(data, EthECDSASignature.FromDER(sign));

            if (verifyHash)
                return true;

            return Ecdsa.Verify(data, sign, publicKey, Curves.Secp256K1, Algorithms.Sha256WithEcdsa);
        }
    }
}