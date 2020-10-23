using System;

namespace Atomex.Common
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTime dateTime) =>
            ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }
}