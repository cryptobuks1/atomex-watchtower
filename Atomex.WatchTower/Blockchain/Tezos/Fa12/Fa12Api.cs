using Atomex.Abstract;
using Atomex.TezosTokens;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Blockchain.Tezos.Fa12
{
    public class Fa12Api : SecureBlockchainApi
    {
        public Fa12Api(
            string currency,
            ICurrencies currencies,
            TezosSettings settings)
        {
            if (settings.Tzkt != null)
                _apis.Add(new Fa12TzktApi(
                    currency: currencies.Get<FA12>(currency),
                    settings: settings.Tzkt));
        }
    }
}