using Atomex.Abstract;
using Atomex.TezosTokens;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Blockchain.Tezos.Fa2
{
    public class Fa2Api : SecureBlockchainApi
    {
        public Fa2Api(
            string currency,
            ICurrencies currencies,
            TezosSettings settings)
        {
            if (settings.Tzkt != null)
                _apis.Add(new Fa2TzktApi(
                    currency: currencies.Get<FA2>(currency),
                    settings: settings.Tzkt));
        }
    }
}