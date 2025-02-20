using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SQLite
{
    internal class CollectionPool<TPool, TCollection, T> where TCollection : ICollection<T>, new() where TPool : CollectionPool<TPool, TCollection, T>, new()
    {
        public static readonly TPool                        Shared = new();
        protected readonly     ConcurrentStack<TCollection> pool   = new();

        public TCollection Take()
        {
            if (pool.TryPop(out var item)) return item;
            return new TCollection();
        }

        public void Return(TCollection item)
        {
            item.Clear();
            pool.Push(item);
        }
    }

    internal class DictionaryPool<K, V> : CollectionPool<DictionaryPool<K, V>, Dictionary<K, V>, KeyValuePair<K, V>> { }

    internal class ListPool<T> : CollectionPool<ListPool<T>, List<T>, T>
    {
        public List<T> Take(IEnumerable<T> items)
        {
            if (pool.TryPop(out var item))
            {
                item.AddRange(items);
                return item;
            }
            return new List<T>(items);
        }
    }

    internal class HashSetPool<T> : CollectionPool<HashSetPool<T>, HashSet<T>, T> { }
}
