using System;

using Atomex.Entities;

namespace Atomex.Guard.Common
{
    public static class AmountHelper
    {
        public static decimal AmountToQty(
            Side side,
            decimal amount,
            decimal price,
            decimal digitsMultiplier) =>
            RoundDown(side == Side.Buy ? amount / price : amount, digitsMultiplier);

        public static decimal QtyToAmount(
            Side side,
            decimal qty,
            decimal price,
            decimal digitsMultiplier) => 
            RoundDown(side == Side.Buy ? qty * price : qty, digitsMultiplier);

        public static decimal RoundDown(decimal d, decimal digitsMultiplier) =>
            Math.Floor(d * digitsMultiplier) / digitsMultiplier;

        public static decimal RoundAmount(decimal value, decimal digitsMultiplier) =>
            Math.Floor(value * digitsMultiplier);
    }
}