using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    public static class EnumerableExtensions
    {
        internal static void Each<T>(this IEnumerable<T> source, Action<T> fn)
        {
            foreach (T item in source)
            {
                fn.Invoke(item);
            }
        }

        internal static void Each<T>(this IEnumerable<T> source, Action<T, int> fn)
        {
            int index = 0;
            foreach (T item in source)
            {
                fn.Invoke(item, index);
                index++;
            }
        }

        public static IEnumerable<T> FromItems<T>(params T[] items)
        {
            foreach (T item in items)
            {
                yield return item;
            }
        }
    }
}