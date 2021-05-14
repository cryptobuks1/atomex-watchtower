using System;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

using Atomex.Abstract;
using Atomex.Common;
using Atomex.Swaps;
using Atomex.Wallet;
using Atomex.WatchTower.Blockchain.Abstract;
using Atomex.WatchTower.Entities;

namespace Atomex.WatchTower.Services
{
    public class WalletSettings
    {
        public string PathToWallet { get; set; }
        public string Password { get; set; }
    }

    public enum WalletStatus
    {
        Loading,
        Ready
    }

    public class WalletService : IHostedService
    {
        private readonly Account _account;
        private readonly IOptionsMonitor<WalletSettings> _settingsMonitor;
        private readonly ICurrenciesProvider _currenciesProvider;
        
        public WalletStatus Status { get; private set; }

        public WalletService(
            IOptionsMonitor<WalletSettings> settingsMonitor,
            ICurrenciesProvider currenciesProvider)
        {
            //_settingsMonitor = settingsMonitor;
            //_currenciesProvider = currenciesProvider;

            //var settings = _settingsMonitor.CurrentValue;

            //_account = Account.LoadFromFile(
            //    settings.PathToWallet,
            //    settings.Password.ToSecureString(),
            //    _currenciesProvider,
            //    ClientType.Unknown);

            //Status = WalletStatus.Loading;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //Task.Run(async () =>
            //{
            //    await new HdWalletScanner(_account).ScanAsync();

            //    Status = WalletStatus.Ready;
            //});

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task RedeemAsync(
            Swap swap,
            BlockchainTransaction initiatorRedeemTx,
            CancellationToken cancellationToken = default)
        {
            string purchasedCurrency = swap.Symbol.PurchasedCurrency(swap.Acceptor.Side);

            var currency = _currenciesProvider
                .GetCurrencies(_account.Network)
                .GetByName(purchasedCurrency);

            var currencySwap = CurrencySwapCreator.Create(currency, _account);

            try
            {
                await currencySwap.RedeemForPartyAsync(new Core.Swap
                {
                    // todo: 
                },
                cancellationToken);


            }
            catch (Exception e)
            {

            }

            // todo: get currency
            // todo: currencyHelper.createtx
            // todo: currencyHelper.sign
            // todo: currencyHelper.broadcast

            // todo: redeem!
            throw new NotImplementedException();
        }

        public Task RefundAsync(
            Swap swap,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
            // todo: refund!
        }
    }
}

// tezos helper
//    public static Tx CreateRedeemTx(_account, params...)
//    public static Tx SignRedeemTx(_account, Tx tx)
//    public static bool BroadcastTx(_account, Tx tx);

