using System.Collections.Generic;
using Microsoft.Extensions.Options;

using Atomex.Currencies;
using Atomex.Currencies.Abstract;
using Atomex.Services.Abstract;

namespace Atomex.Services
{
    public class CurrencySettings
    {
        public string Name { get; set; }
        public string Network { get; set; }
        public decimal DigitsMultiplier { get; set; }
    }

    public class CurrenciesSettings : Dictionary<string, CurrencySettings> { };

    public class CurrenciesProvider : ICurrenciesProvider
    {
        private readonly IOptionsMonitor<CurrenciesSettings> _settingsMonitor;

        private CurrenciesSettings CurrentSettings => _settingsMonitor.CurrentValue;

        public CurrenciesProvider(IOptionsMonitor<CurrenciesSettings> settingsMonitor)
        {
            _settingsMonitor = settingsMonitor;
        }

        public ICurrency GetByName(string name)
        {
            if (!Contains(name))
                return null;

            var settings = CurrentSettings[name];

            return name switch
            {
                "BTC" => new Bitcoin(settings),
                "LTC" => new Litecoin(settings),
                "ETH" => new Ethereum(settings),
                "XTZ" => new Tezos(settings),
                "USDT" => new Usdt(settings),
                "TZBTC" => new TzBtc(settings),
                "FA2" => new Fa2(settings),
                "NYX" => new Nyx(settings),
                _ => null
            };
        }

        public T GetByName<T>(string name) where T : class, ICurrency =>
            GetByName(name) as T;

        public bool Contains(string currency) =>
            CurrentSettings.ContainsKey(currency);

        public bool IsBitcoinBased(string currency) =>
            currency == "BTC" ||
            currency == "LTC";
    }
}