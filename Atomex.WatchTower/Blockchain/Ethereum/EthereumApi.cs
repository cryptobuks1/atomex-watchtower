using Atomex.Abstract;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Blockchain.Ethereum
{
    public class EthereumSettings
    {
        public EtherScanSettings EtherScan { get; set; }
    }

    public class EthereumApi : SecureBlockchainApi
    {
        public EthereumApi(
            ICurrencies currencies,
            EthereumSettings settings)
        {
            _apis.Add(new EtherScanApi(
                currency: currencies.Get<Atomex.Ethereum>("ETH"),
                settings: settings.EtherScan));
        }
    }
}