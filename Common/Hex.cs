using HexBouncyCastle = Org.BouncyCastle.Utilities.Encoders.Hex;

namespace Atomex.Common
{
    public unsafe static class Hex
    {
        public static byte[] FromHexToByteArray(this string s, bool prefixed = false) =>
            HexBouncyCastle.Decode(prefixed ? s.Substring(2) : s);

        public static string ToHexString(this byte[] bytes) =>
            HexBouncyCastle.ToHexString(bytes);

        public static string ToHexString(this byte[] bytes, int offset, int count) =>
            HexBouncyCastle.ToHexString(bytes, offset, count);  
    }
}