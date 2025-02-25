/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/20-17:26:31
 */

using System;

namespace SQLite
{
    public abstract class SQLObject
    {
        [Ignore] protected SQLiteConnection Connection { get; private set; }

        [Ignore]
        internal SQLiteConnection InternalConnection
        {
            get => Connection;
            set => Connection = value;
        }
    }

    public abstract class SQLObject<TPK> : SQLObject, IEquatable<SQLObject<TPK>>
    {
        public abstract TPK GetPrimaryKey();
        public bool Equals(SQLObject<TPK> other) => GetPrimaryKey().Equals(other.GetPrimaryKey());

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SQLObject<TPK>) obj);
        }

        public override int GetHashCode() => GetPrimaryKey().GetHashCode();
    }
}
