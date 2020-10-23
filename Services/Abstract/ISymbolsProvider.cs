using System.Collections.Generic;

namespace Atomex.Services.Abstract
{
    public class Symbol
    {
        public string Name { get; set; }
        public decimal MinimumQty { get; set; }
        public string Base => Name.Substring(0, Name.IndexOf('/'));
        public string Quote => Name.Substring(Name.IndexOf('/') + 1);
    }

    public interface ISymbolsProvider
    {
        IEnumerable<Symbol> Symbols { get; }

        bool Contains(string symbol);
        Symbol GetByName(string name);
    }
}