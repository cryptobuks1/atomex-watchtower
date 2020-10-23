using Atomex.Abstract;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Blockchain.Bitcoin;

namespace Atomex.WatchTower.Blockchain.Litecoin
{
    public class LitecoinSettings
    {
        public InsightSettings Insight { get; set; }
    }

    public class LitecoinApi : SecureBlockchainApi
    {
        public LitecoinApi(
            ICurrencies currencies,
            LitecoinSettings settings)
        {
            _apis.Add(new InsightApi(
                currency: currencies.Get<BitcoinBasedCurrency>("LTC"),
                settings: settings.Insight));
        }
    }
}