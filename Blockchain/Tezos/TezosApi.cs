using Atomex.Guard.Blockchain.Abstract;
using Atomex.Services.Abstract;
using Xtz = Atomex.Currencies.Tezos;

namespace Atomex.Guard.Blockchain.Tezos
{
    public class TezosSettings
    {
        public TzktSettings Tzkt { get; set; }
    }

    public class TezosApi : SecureBlockchainApi
    {
        public TezosApi(
            ICurrenciesProvider currenciesProvider,
            TezosSettings settings)
        {
            _apis.Add(new TzktApi(
                currency: currenciesProvider.GetByName<Xtz>("XTZ"),
                settings: settings.Tzkt));
        }
    }
}