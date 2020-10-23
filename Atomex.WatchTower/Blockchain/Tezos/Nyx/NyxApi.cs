using Atomex.Abstract;
using Atomex.TezosTokens;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Blockchain.Tezos.Nyx
{
    public class NyxApi : SecureBlockchainApi
    {
        public NyxApi(
            string currency,
            ICurrencies currencies,
            TezosSettings settings)
        {
            if (settings.Tzkt != null)
                _apis.Add(new NyxTzktApi(
                    currency: currencies.Get<NYX>(currency),
                    settings: settings.Tzkt));
        }
    }
}