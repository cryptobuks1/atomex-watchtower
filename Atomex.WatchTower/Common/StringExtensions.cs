namespace Atomex.Common
{
    public static class StringExtensions
    {
        public static bool IsHex(this string s)
        {
            bool isHex;

            foreach (var c in s)
            {
                isHex = (c >= '0' && c <= '9') ||
                        (c >= 'a' && c <= 'f') ||
                        (c >= 'A' && c <= 'F');

                if (!isHex)
                    return false;
            }

            return true;
        }
    }
}