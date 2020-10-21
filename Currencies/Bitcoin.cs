using System;
using NBitcoin;

using Atomex.Services;

namespace Atomex.Currencies
{
    public class Bitcoin : BitcoinBased
    {
        public Bitcoin(CurrencySettings settings)
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
                return Network.Main;

            if (invariantNetwork == "testnet")
                return Network.TestNet;

            throw new NotSupportedException($"Network {network} not supported for bitcoin.");
        }
    }
}