using Atomex.Services;

namespace Atomex.Currencies
{
    public class Usdt : Erc20
    {
        public Usdt(CurrencySettings settings)
            : base(settings)
        { }
    }
}