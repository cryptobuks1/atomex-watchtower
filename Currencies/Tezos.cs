using System;

using Atomex.Cryptography;
using Atomex.Currencies.Abstract;
using Atomex.Services;

namespace Atomex.Currencies
{
    public class Tezos : ICurrency
    {
        private static readonly byte[] Tz1 = { 6, 161, 159 };
        private static readonly byte[] Tz2 = { 6, 161, 161 };
        private static readonly byte[] Tz3 = { 6, 161, 164 };
        private static readonly byte[] KT = { 2, 90, 121 };
        private const int PkHashSize = 20 * 8;

        public string Name { get; }
        public decimal DigitsMultiplier { get; }

        public Tezos() {}

        public Tezos(CurrencySettings settings)
        {
            Name = settings.Name;
            DigitsMultiplier = settings.DigitsMultiplier;
        }

        public bool IsAddressFromKey(string address, byte[] publicKey)
        {
            var prefix = address switch
            {
                _ when address.StartsWith("tz1") => Tz1,
                _ when address.StartsWith("tz2") => Tz2,
                _ when address.StartsWith("tz3") => Tz3,
                _ when address.StartsWith("KT") => KT,
                _ => throw new NotImplementedException(),
            };

            var addressFromKey = Base58Check.Encode(Blake2b.Compute(publicKey, PkHashSize), prefix)
                .ToLowerInvariant();

            return addressFromKey == address.ToLowerInvariant();
        }

        public bool IsValidAddress(string address) =>
            CheckAddress(address, Tz1) ||
            CheckAddress(address, Tz2) ||
            CheckAddress(address, Tz3) ||
            CheckAddress(address, KT);

        public bool Verify(byte[] data, byte[] sign, byte[] publicKey) =>
            Ed25519.Verify(data, sign, publicKey);

        private static bool CheckAddress(string address, byte[] prefix)
        {
            try
            {
                Base58Check.Decode(address, prefix);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}