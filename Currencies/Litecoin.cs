using System;
using NBitcoin;

using Atomex.Services;

namespace Atomex.Currencies
{
    public class Litecoin : BitcoinBased
    {
        public Litecoin(CurrencySettings settings)
            : base(
                settings.Name,
                ResolveNetwork(settings.Network),
                settings.DigitsMultiplier)
        {
        }

        public static Network ResolveNetwork(string network)
        {
            var invariantNetwork = network.ToLowerInvariant();

            if (invariantNetwork == "mainnet")
                return NBitcoin.Altcoins.Litecoin.Instance.Mainnet;

            if (invariantNetwork == "testnet")
                return NBitcoin.Altcoins.Litecoin.Instance.Testnet;

            throw new NotSupportedException($"Network {network} not supported for litecoin.");
        }
    }
}