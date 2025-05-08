/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/19-18:21:56
 */

using System.Collections.Generic;

namespace SQLite
{
    internal class CollectionPool<TPool, TCollection, T> where TCollection : ICollection<T>, new() where TPool : CollectionPool<TPool, TCollection, T>, new()
    {
        public static readonly TPool              Shared = new();
        protected readonly     Stack<TCollection> pool   = new();
        protected readonly     object             locker = new();

        public TCollection Take()
        {
            lock (locker)
            {
                if (pool.TryPop(out var item)) return item;
                return new TCollection();
            }
        }

        public void Return(TCollection item)
        {
            lock (locker)
            {
                item.Clear();
                pool.Push(item);
            }
        }
    }

    internal class DictionaryPool<K, V> : CollectionPool<DictionaryPool<K, V>, Dictionary<K, V>, KeyValuePair<K, V>> { }

    internal class ListPool<T> : CollectionPool<ListPool<T>, List<T>, T>
    {
        public List<T> Take(IEnumerable<T> items)
        {
            lock (locker)
            {
                if (pool.TryPop(out var item))
                {
                    item.AddRange(items);
                    return item;
                }

                return new List<T>(items);
            }
        }
    }

    internal class HashSetPool<T> : CollectionPool<HashSetPool<T>, HashSet<T>, T> { }

    internal class ArrayPool<T>
    {
        public static readonly ArrayPool<T>                Shared  = new();
        private readonly       object                      _locker = new();
        private readonly       Dictionary<int, Stack<T[]>> _pool   = new();

        public T[] Take(int size)
        {
            lock (_locker)
            {
                if (!_pool.TryGetValue(size, out var pool))
                {
                    pool        = new Stack<T[]>();
                    _pool[size] = pool;
                }

                return pool.TryPop(out var array) ? array : new T[size];
            }
        }

        public void Return(T[] array)
        {
            lock (_locker)
            {
                if (!_pool.TryGetValue(array.Length, out var pool))
                {
                    pool = new Stack<T[]>();
                }

                for (var i = 0; i < array.Length; i++)
                {
                    array[i] = default;
                }

                pool.Push(array);
            }
        }
    }
}
