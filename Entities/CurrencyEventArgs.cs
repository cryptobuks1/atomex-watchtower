using System;

namespace Atomex.Entities
{
    public class CurrencyEventArgs : EventArgs
    {
        public string Currency { get; set; }

        public CurrencyEventArgs(string currency)
        {
            Currency = currency;
        }
    }
}