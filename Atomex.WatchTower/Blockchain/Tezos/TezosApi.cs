using Atomex.Abstract;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Blockchain.Tezos
{
    public class TezosSettings
    {
        public TzktSettings Tzkt { get; set; }
    }

    public class TezosApi : SecureBlockchainApi
    {
        public TezosApi(
            ICurrencies currencies,
            TezosSettings settings)
        {
            _apis.Add(new TzktApi(
                currency: currencies.Get<Atomex.Tezos>("XTZ"),
                settings: settings.Tzkt));
        }
    }
}