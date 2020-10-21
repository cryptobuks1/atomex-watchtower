using Atomex.Currencies;
using Atomex.Guard.Blockchain.Abstract;
using Atomex.Guard.Blockchain.Bitcoin;
using Atomex.Services.Abstract;

namespace Atomex.Guard.Blockchain.Litecoin
{
    public class LitecoinSettings
    {
        public InsightSettings Insight { get; set; }
    }

    public class LitecoinApi : SecureBlockchainApi
    {
        public LitecoinApi(
            ICurrenciesProvider currenciesProvider,
            LitecoinSettings settings)
        {
            _apis.Add(new InsightApi(
                currency: currenciesProvider.GetByName<BitcoinBased>("LTC"),
                settings: settings.Insight));
        }
    }
}