/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/21-14:33:49
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SQLite
{
    public sealed class SQLTable<TPK, TObject> : ICollection<TObject> where TObject : SQLObject<TPK>, new()
    {
        internal readonly Dictionary<TPK, TObject> Table = new();
        private readonly  SQLiteConnection         m_Conn;

        public TObject this[TPK key]
        {
            get => Table[key];
            set
            {
                value.InternalConnection = m_Conn;
                if (Table.ContainsKey(value.GetPrimaryKey()))
                {
                    if (m_Conn.IsInTransaction)
                    {
                        var origin = Table[key];
                        m_Conn.Rollbacked += () => { Table[key] = origin; };
                    }

                    if (m_Conn.Update(value) > 0)
                        Table[key] = value;
                }
                else if (m_Conn.Insert(value) > 0)
                {
                    Table[key] = value;
                }
            }
        }

        public int Count => Table.Count;
        public bool IsReadOnly => false;
        public readonly string TableName;

        public SQLTable(SQLiteConnection conn)
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
                Table.Add(item.GetPrimaryKey(), item);
            }

            ListPool<TObject>.Shared.Return(cache);
        }

        public bool TryGetValue(TPK key, out TObject value) => Table.TryGetValue(key, out value);

        public void CopyTo(TObject[] array, int arrayIndex) => Table.Values.CopyTo(array, arrayIndex);

        public bool Remove(TObject item)
        {
            if (m_Conn.Delete(item) > 0)
            {
                return Table.Remove(item.GetPrimaryKey());
            }

            return false;
        }

        public bool Remove(TPK pk)
        {
            if (m_Conn.Delete<TObject>(pk) > 0)
            {
                return Table.Remove(pk);
            }

            return false;
        }

        public void Add(TObject item)
        {
            if (m_Conn.Insert(item) > 0)
            {
                item.InternalConnection = m_Conn;
                Table.Add(item.GetPrimaryKey(), item);
            }
            else
            {
                throw new InvalidOperationException("Failed to insert item into the database.");
            }
        }

        public void Clear()
        {
            m_Conn.DeleteAll<TObject>();
            Table.Clear();
        }

        public bool Contains(TObject item) => Table.ContainsKey(item.GetPrimaryKey());

        public bool ContainsKey(TPK key) => Table.ContainsKey(key);

        public bool TryAdd(TObject item)
        {
            if (m_Conn.Insert(item) > 0)
            {
                item.InternalConnection = m_Conn;
                Table.Add(item.GetPrimaryKey(), item);
                return true;
            }

            return false;
        }

        public Dictionary<TPK, TObject>.ValueCollection.Enumerator GetEnumerator() => Table.Values.GetEnumerator();

        IEnumerator<TObject> IEnumerable<TObject>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
