using Atomex.Currencies;
using Atomex.Guard.Blockchain.Abstract;
using Atomex.Services.Abstract;

namespace Atomex.Guard.Blockchain.Bitcoin
{
    public class BitcoinSettings
    {
        public InsightSettings Insight { get; set; }
        public BlockCypherSettings BlockCypher { get; set; }
    }

    public class BitcoinApi : SecureBlockchainApi
    {
        public BitcoinApi(           
            ICurrenciesProvider currenciesProvider,
            BitcoinSettings settings)
        {
            if (settings.Insight != null)
                _apis.Add(new InsightApi(
                    currency: currenciesProvider.GetByName<BitcoinBased>("BTC"),
                    settings: settings.Insight));

            if (settings.BlockCypher != null)
                _apis.Add(new BlockCypherApi(
                    currency: currenciesProvider.GetByName<BitcoinBased>("BTC"),
                    settings: settings.BlockCypher));
        }
    }
}