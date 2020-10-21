using Atomex.Guard.Blockchain.Abstract;
using Atomex.Services.Abstract;
using FA12 = Atomex.Currencies.Fa12;

namespace Atomex.Guard.Blockchain.Tezos.Fa12
{
    public class Fa12Api : SecureBlockchainApi
    {
        public Fa12Api(
            string currency,
            ICurrenciesProvider currenciesProvider,
            TezosSettings settings)
        {
            if (settings.Tzkt != null)
                _apis.Add(new Fa12TzktApi(
                    currency: currenciesProvider.GetByName<FA12>(currency),
                    settings: settings.Tzkt));
        }
    }
}