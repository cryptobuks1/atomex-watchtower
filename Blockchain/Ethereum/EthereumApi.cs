using Atomex.Guard.Blockchain.Abstract;
using Atomex.Services.Abstract;
using Eth = Atomex.Currencies.Ethereum;

namespace Atomex.Guard.Blockchain.Ethereum
{
    public class EthereumSettings
    {
        public EtherScanSettings EtherScan { get; set; }
    }

    public class EthereumApi : SecureBlockchainApi
    {
        public EthereumApi(
            ICurrenciesProvider currenciesProvider,
            EthereumSettings settings)
        {
            _apis.Add(new EtherScanApi(
                currency: currenciesProvider.GetByName<Eth>("ETH"),
                settings: settings.EtherScan));
        }
    }
}