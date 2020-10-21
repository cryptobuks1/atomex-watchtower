using Atomex.Entities;
using Atomex.Services.Abstract;

namespace Atomex.Common
{
    public static class SymbolExtensions
    {
        public static string PurchasedCurrency(this Symbol symbol, Side side)
        {
            return side == Side.Buy
                ? symbol.Base
                : symbol.Quote;
        }

        public static string PurchasedCurrency(this string symbol, Side side)
        {
            return side == Side.Buy
                ? symbol.BaseCurrency()
                : symbol.QuoteCurrency();
        }

        public static string SoldCurrency(this Symbol symbol, Side side)
        {
            return side == Side.Buy
                ? symbol.Quote
                : symbol.Base;
        }

        public static string SoldCurrency(this string symbol, Side side)
        {
            return side == Side.Buy
                ? symbol.QuoteCurrency()
                : symbol.BaseCurrency();
        }

        public static string BaseCurrency(this string symbol)
        {
            return symbol.Substring(0, symbol.IndexOf('/'));
        }

        public static string QuoteCurrency(this string symbol)
        {
            return symbol.Substring(symbol.IndexOf('/') + 1);
        }
    }
}