using Atomex.Abstract;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Blockchain.Bitcoin
{
    public class BitcoinSettings
    {
        public InsightSettings Insight { get; set; }
        public BlockCypherSettings BlockCypher { get; set; }
    }

    public class BitcoinApi : SecureBlockchainApi
    {
        public BitcoinApi(
            ICurrencies currencies,
            BitcoinSettings settings)
        {
            if (settings.Insight != null)
                _apis.Add(new InsightApi(
                    currency: currencies.Get<BitcoinBasedCurrency>("BTC"),
                    settings: settings.Insight));

            if (settings.BlockCypher != null)
                _apis.Add(new BlockCypherApi(
                    currency: currencies.Get<BitcoinBasedCurrency>("BTC"),
                    settings: settings.BlockCypher));
        }
    }
}