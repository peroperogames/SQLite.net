/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/21-15:00:26
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SQLite
{
    public sealed class SQLArray<TObject> : ICollection<TObject> where TObject : SQLObject, new()
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
                value.InternalConnection = m_Conn;
                if (m_Conn.IsInTransaction)
                {
                    var origin = Table[index];
                    m_Conn.TryGetSavePoint(out var savepoint);
                    m_Conn.RegisterRollbackHandler(SetRollbackHandler.Require(Table, index, origin), savepoint);
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
                Table.Add(item);
            }

            ListPool<TObject>.Shared.Return(cache);
        }

        public void CopyTo(TObject[] array, int arrayIndex) => Table.CopyTo(array, arrayIndex);

        public bool Remove(TObject item)
        {
            if (m_Conn.Delete(item) > 0)
            {
                if (m_Conn.IsInTransaction)
                {
                    m_Conn.TryGetSavePoint(out var savepoint);
                    m_Conn.RegisterRollbackHandler(RemoveRollbackHandler.Require(Table, item), savepoint);
                }

                return Table.Remove(item);
            }

            return false;
        }

        public bool RemoveAt(int index)
        {
            var item = Table[index];
            if (m_Conn.Delete(item) > 0)
            {
                if (m_Conn.IsInTransaction)
                {
                    m_Conn.TryGetSavePoint(out var savepoint);
                    m_Conn.RegisterRollbackHandler(RemoveRollbackHandler.Require(Table, item, index), savepoint);
                }

                return Table.Remove(item);
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
                    m_Conn.RegisterRollbackHandler(AddRollbackHandler.Require(Table, item), savepoint);
                }

                item.InternalConnection = m_Conn;
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
                if (m_Conn.IsInTransaction)
                {
                    m_Conn.TryGetSavePoint(out var savepoint);
                    m_Conn.RegisterRollbackHandler(AddRollbackHandler.Require(Table, item), savepoint);
                }

                item.InternalConnection = m_Conn;
                Table.Add(item);
                return true;
            }

            return false;
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

        public bool Contains(TObject item) => Table.Contains(item);

        public List<TObject>.Enumerator GetEnumerator() => Table.GetEnumerator();
        IEnumerator<TObject> IEnumerable<TObject>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class SetRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<SetRollbackHandler> m_Pool = new();
            private                 List<TObject>                  m_Table;
            private                 TObject                        m_Origin;
            private                 int                            m_Index;

            public static SetRollbackHandler Require(List<TObject> table, int index, TObject origin)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new SetRollbackHandler();
                }

                handler.m_Origin = origin;
                handler.m_Index  = index;
                handler.m_Table  = table;
                return handler;
            }

            public void OnRollback()
            {
                m_Table[m_Index] = m_Origin;
                m_Table          = null;
                m_Origin         = null;
                m_Index          = -1;
                m_Pool.Push(this);
            }
        }

        private sealed class AddRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<AddRollbackHandler> m_Pool = new();
            private                 List<TObject>             m_Table;
            private                 TObject                   m_Item;

            public static AddRollbackHandler Require(List<TObject> table, TObject item)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new AddRollbackHandler();
                }

                handler.m_Item  = item;
                handler.m_Table = table;
                return handler;
            }

            public void OnRollback()
            {
                m_Table.Remove(m_Item);
                m_Table = null;
                m_Item  = null;
                m_Pool.Push(this);
            }
        }

        private sealed class RemoveRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<RemoveRollbackHandler> m_Pool = new();
            private                 List<TObject>                m_Table;
            private                 TObject                      m_Item;
            private                 int                          m_Index;

            public static RemoveRollbackHandler Require(List<TObject> table, TObject item, int index = -1)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new RemoveRollbackHandler();
                }

                handler.m_Index = index;
                handler.m_Item  = item;
                handler.m_Table = table;
                return handler;
            }

            public void OnRollback()
            {
                if (m_Index == -1)
                    m_Table.Add(m_Item);
                else
                    m_Table.Insert(m_Index, m_Item);
                m_Table = null;
                m_Item  = null;
                m_Pool.Push(this);
            }
        }

        private sealed class ClearRollbackHandler : IRollbackHandler
        {
            private static readonly Stack<ClearRollbackHandler> m_Pool = new();
            private                 List<TObject>               m_Table;
            private                 List<TObject>               m_Cache;

            public static ClearRollbackHandler Require(List<TObject> table)
            {
                if (!m_Pool.TryPop(out var handler))
                {
                    handler = new ClearRollbackHandler();
                }

                handler.m_Table = table;
                handler.m_Cache = ListPool<TObject>.Shared.Take();
                handler.m_Cache.AddRange(table);
                return handler;
            }

            public void OnRollback()
            {
                m_Table.AddRange(m_Cache);
                ListPool<TObject>.Shared.Return(m_Cache);
                m_Table = null;
                m_Cache = null;
                m_Pool.Push(this);
            }
        }
    }
}
