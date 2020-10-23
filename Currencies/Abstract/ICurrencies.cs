using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Atomex.Currencies.Abstract
{
    public interface ICurrencies : IEnumerable<ICurrency>
    {
        void Update(IConfiguration configuration);
        ICurrency GetByName(string name);
        T Get<T>(string name) where T : ICurrency;
    }
}