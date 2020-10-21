using Atomex.Guard.Blockchain.Abstract;
using Atomex.Services.Abstract;
using FA2 = Atomex.Currencies.Fa2;

namespace Atomex.Guard.Blockchain.Tezos.Fa2
{
    public class Fa2Api : SecureBlockchainApi
    {
        public Fa2Api(
            string currency,
            ICurrenciesProvider currenciesProvider,
            TezosSettings settings)
        {
            if (settings.Tzkt != null)
                _apis.Add(new Fa2TzktApi(
                    currency: currenciesProvider.GetByName<FA2>(currency),
                    settings: settings.Tzkt));
        }
    }
}