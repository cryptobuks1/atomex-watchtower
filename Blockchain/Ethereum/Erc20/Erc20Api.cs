using Atomex.Guard.Blockchain.Abstract;
using Atomex.Services.Abstract;
using ERC20 = Atomex.Currencies.Erc20;

namespace Atomex.Guard.Blockchain.Ethereum.Erc20
{
    public class Erc20Api : SecureBlockchainApi
    {
        public Erc20Api(
            string currency,
            ICurrenciesProvider currenciesProvider,
            EthereumSettings settings)
        {
            _apis.Add(new Erc20EtherScanApi(
                currency: currenciesProvider.GetByName<ERC20>(currency),
                settings: settings.EtherScan));
        }
    }
}