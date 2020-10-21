using Atomex.Services;

namespace Atomex.Currencies
{
    public class Erc20 : Ethereum
    {
        public Erc20(CurrencySettings settings)
            : base(settings)
        { }
    }
}