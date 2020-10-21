using System;

namespace Atomex.Common
{
    public static class ArrayExtensions
    {
        public static void Clear<T>(this T[] array)
        {
            if (array != null)
                Array.Clear(array, 0, array.Length);
        }

        public static T[] ConcatArrays<T>(this T[] arr1, T[] arr2)
        {
            var result = new T[arr1.Length + arr2.Length];
            Buffer.BlockCopy(arr1, 0, result, 0, arr1.Length);
            Buffer.BlockCopy(arr2, 0, result, arr1.Length, arr2.Length);

            return result;
        }

        public static byte[] SubArray(this byte[] arr, int start, int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(arr, start, result, 0, length);

            return result;
        }

        public static byte[] SubArray(this byte[] arr, int start)
        {
            return SubArray(arr, start, arr.Length - start);
        }
    }
}