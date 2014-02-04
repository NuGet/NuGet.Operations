using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    internal static class EnumerableExtensions
    {
        public static void AddRange<T>(this IList<T> self, IEnumerable<T> items)
        {
            var l = self as List<T>;
            if (l != null)
            {
                // List<T>.AddRange is optimized and works with the internal structure
                // So use it if available
                l.AddRange(items);
            }
            else
            {
                foreach (var item in items)
                {
                    self.Add(item);
                }
            }
        }

        public static IDictionary<K, V> ToDictionaryByFirstItemWithKey<K, V>(this IEnumerable<V> self, Func<V, K> keySelector)
        {
            return ToDictionaryByFirstItemWithKey(self, keySelector, EqualityComparer<K>.Default);
        }

        public static IDictionary<K, V> ToDictionaryByFirstItemWithKey<K, V>(this IEnumerable<V> self, Func<V, K> keySelector, IEqualityComparer<K> comparer)
        {
            return self.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.First(), comparer);
        }
    }
}
