/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/21-15:00:26
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SQLite
{
    public class SQLArray<TObject> : ICollection<TObject> where TObject : SQLObject, new()
    {
        private readonly  SQLiteConnection m_Conn;
        internal readonly List<TObject>    Table = new();
        public int Count => Table.Count;
        public bool IsReadOnly => false;
        public readonly string TableName;

        public TObject this[int index]
        {
            get => Table[index];
            set
            {
                if (m_Conn.IsInTransaction)
                {
                    var origin = Table[index];
                    m_Conn.Rollbacked += () => { Table[index] = origin; };
                    if (m_Conn.Delete(origin) > 0 && m_Conn.Insert(value) > 0)
                    {
                        Table[index] = value;
                    }
                }
                else if (m_Conn.Update(value) > 0)
                {
                    Table[index] = value;
                }
            }
        }

        public SQLArray(SQLiteConnection conn)
        {
            m_Conn = conn;
            var cache = ListPool<TObject>.Shared.Take();
            var typeInfo = typeof(TObject);
#if ENABLE_IL2CPP
			var tableAttr = typeInfo.GetCustomAttribute<TableAttribute>();
#else
            TableAttribute tableAttr = null;
            foreach (var attr in typeInfo.CustomAttributes)
            {
                if (attr.AttributeType != typeof(TableAttribute)) continue;
                tableAttr = (TableAttribute) Orm.InflateAttribute(attr);
            }
#endif
            TableName = tableAttr != null && !string.IsNullOrEmpty(tableAttr.Name) ? tableAttr.Name : typeInfo.Name;
            conn.Query($"select * from {TableName}", cache, Array.Empty<object>());
            foreach (var item in cache)
            {
                item.InternalConnection = conn;
                Table.Add(item);
            }

            ListPool<TObject>.Shared.Return(cache);
        }

        public void CopyTo(TObject[] array, int arrayIndex) => Table.CopyTo(array, arrayIndex);

        public bool Remove(TObject item)
        {
            if (m_Conn.Delete(item) > 0)
            {
                return Table.Remove(item);
            }

            return false;
        }

        public bool RemoveAt(int index)
        {
            var item = Table[index];
            if (m_Conn.Delete(item) > 0)
            {
                return Table.Remove(item);
            }

            return false;
        }

        public void Add(TObject item)
        {
            if (m_Conn.Insert(item) > 0)
            {
                Table.Add(item);
            }
            else
            {
                throw new InvalidOperationException("Failed to insert item into the database.");
            }
        }

        public bool TryAdd(TObject item)
        {
            if (m_Conn.Insert(item) > 0)
            {
                Table.Add(item);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            m_Conn.DeleteAll<TObject>();
            Table.Clear();
        }

        public bool Contains(TObject item) => Table.Contains(item);

        public List<TObject>.Enumerator GetEnumerator() => Table.GetEnumerator();
        IEnumerator<TObject> IEnumerable<TObject>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
