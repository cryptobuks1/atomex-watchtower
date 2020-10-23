using Atomex.Abstract;
using Atomex.EthereumTokens;
using Atomex.WatchTower.Blockchain.Abstract;

namespace Atomex.WatchTower.Blockchain.Ethereum.Erc20
{
    public class Erc20Api : SecureBlockchainApi
    {
        public Erc20Api(
            string currency,
            ICurrencies currencies,
            EthereumSettings settings)
        {
            _apis.Add(new Erc20EtherScanApi(
                currency: currencies.Get<ERC20>(currency),
                settings: settings.EtherScan));
        }
    }
}