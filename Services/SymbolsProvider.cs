using System.Collections.Generic;
using Microsoft.Extensions.Options;

using Atomex.Services.Abstract;

namespace Atomex.Services
{
    public class SymbolsSettings : Dictionary<string, Symbol> { };

    public class SymbolsProvider : ISymbolsProvider
    {
        private readonly IOptionsMonitor<SymbolsSettings> _settingsMonitor;
        private SymbolsSettings CurrentSettings => _settingsMonitor.CurrentValue;

        public IEnumerable<Symbol> Symbols => CurrentSettings.Values;

        public SymbolsProvider(IOptionsMonitor<SymbolsSettings> settingsMonitor) =>
            _settingsMonitor = settingsMonitor;

        public Symbol GetByName(string name) =>
            CurrentSettings.TryGetValue(name, out var symbol)
                ? symbol
                : null;

        public bool Contains(string symbol) =>
            CurrentSettings.ContainsKey(symbol);
    }
}