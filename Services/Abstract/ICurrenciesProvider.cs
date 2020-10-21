using Atomex.Currencies.Abstract;

namespace Atomex.Services.Abstract
{
    public interface ICurrenciesProvider
    {
        bool Contains(string currency);
        ICurrency GetByName(string name);
        T GetByName<T>(string name) where T : class, ICurrency;
        bool IsBitcoinBased(string currency);
    }
}