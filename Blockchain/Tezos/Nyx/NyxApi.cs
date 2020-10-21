using Atomex.Guard.Blockchain.Abstract;
using Atomex.Services.Abstract;
using NYX = Atomex.Currencies.Nyx;

namespace Atomex.Guard.Blockchain.Tezos.Nyx
{
    public class NyxApi : SecureBlockchainApi
    {
        public NyxApi(
            string currency,
            ICurrenciesProvider currenciesProvider,
            TezosSettings settings)
        {
            if (settings.Tzkt != null)
                _apis.Add(new NyxTzktApi(
                    currency: currenciesProvider.GetByName<NYX>(currency),
                    settings: settings.Tzkt));
        }
    }
}