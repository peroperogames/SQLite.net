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
                        m_Conn.TryGetSavePoint(out var savepoint);
                        m_Conn.RegisterRollbackHandler(SetRollbackHandler.Require(Table, key, Table[key]), savepoint);
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
            var typeInfo = typeof(TObject).GetTypeInfo();
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
                if (m_Conn.IsInTransaction && Table.ContainsKey(item.GetPrimaryKey()))
                {
                    m_Conn.TryGetSavePoint(out var savepoint);
                    m_Conn.RegisterRollbackHandler(RemoveRollbackHandler.Require(Table, item.GetPrimaryKey(), item), savepoint);
                }
                return Table.Remove(item.GetPrimaryKey());
            }

            return false;
        }

        public bool Remove(TPK pk)
        {
            if (m_Conn.Delete<TObject>(pk) > 0)
            {
                if (m_Conn.IsInTransaction && Table.TryGetValue(pk, out var item))
                {
                    m_Conn.TryGetSavePoint(out var savepoint);
                    m_Conn.RegisterRollbackHandler(RemoveRollbackHandler.Require(Table, pk, item), savepoint);
                }
                return Table.Remove(pk);
            }

            return false;
        }

        public void Add(TObject item)
        {
            if (m_Conn.Insert(item) > 0)
            {
                if (m_Conn.IsInTransaction)
                {
                    m_Conn.TryGetSavePoint(out var savepoint);
                    m_Conn.RegisterRollbackHandler(AddRollbackHandler.Require(Table, item.GetPrimaryKey()), savepoint);
                }
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
            if (m_Conn.IsInTransaction)
            {
                m_Conn.TryGetSavePoint(out var savepoint);
                m_Conn.RegisterRollbackHandler(ClearRollbackHandler.Require(Table), savepoint);
            }
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

        private sealed class SetRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<SetRollbackHandler> m_Pool = new();
            private                 Dictionary<TPK, TObject>  m_Table;
            private                 TPK                       m_PK;
            private                 TObject                   m_Origin;

            public static SetRollbackHandler Require(Dictionary<TPK, TObject> table, TPK pk, TObject origin)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new SetRollbackHandler();
                }

                handler.m_Origin = origin;
                handler.m_PK     = pk;
                handler.m_Table  = table;
                return handler;
            }

            public void OnRollback()
            {
                m_Table[m_PK] = m_Origin;
                m_Table       = null;
                m_Origin      = null;
                m_PK          = default;
                m_Pool.Push(this);
            }
        }

        private sealed class AddRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<AddRollbackHandler> m_Pool = new();
            private                 Dictionary<TPK, TObject>  m_Table;
            private                 TPK                       m_PK;

            public static AddRollbackHandler Require(Dictionary<TPK, TObject> table, TPK pk)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new AddRollbackHandler();
                }

                handler.m_PK    = pk;
                handler.m_Table = table;
                return handler;
            }

            public void OnRollback()
            {
                m_Table.Remove(m_PK);
                m_Table = null;
                m_PK    = default;
                m_Pool.Push(this);
            }
        }

        private sealed class RemoveRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<RemoveRollbackHandler> m_Pool = new();
            private                 Dictionary<TPK, TObject>     m_Table;
            private                 TPK                          m_PK;
            private                 TObject                      m_Item;

            public static RemoveRollbackHandler Require(Dictionary<TPK, TObject> table, TPK pk, TObject item)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new RemoveRollbackHandler();
                }

                handler.m_PK    = pk;
                handler.m_Table = table;
                handler.m_Item  = item;
                return handler;
            }

            public void OnRollback()
            {
                m_Table.Add(m_PK, m_Item);
                m_Table = null;
                m_Item  = null;
                m_PK    = default;
                m_Pool.Push(this);
            }
        }

        private sealed class ClearRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<ClearRollbackHandler> m_Pool = new();
            private Dictionary<TPK, TObject> m_Table;
            private Dictionary<TPK, TObject> m_Cache;

            public static ClearRollbackHandler Require(Dictionary<TPK, TObject> table)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new ClearRollbackHandler();
                }

                handler.m_Table = table;
                handler.m_Cache = DictionaryPool<TPK, TObject>.Shared.Take();
                foreach (var item in table)
                {
                    handler.m_Cache.Add(item.Key, item.Value);
                }
                return handler;
            }

            public void OnRollback()
            {
                foreach (var item in m_Cache)
                {
                    m_Table.Add(item.Key, item.Value);
                }
                DictionaryPool<TPK, TObject>.Shared.Return(m_Cache);
                m_Table = null;
                m_Cache = null;
                m_Pool.Push(this);
            }
        }
    }
}
