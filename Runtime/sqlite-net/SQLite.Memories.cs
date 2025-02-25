/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/21-15:31:31
 */

namespace SQLite
{
    public static partial class SQLiteConnectionExtensions
    {
        public static SQLTable<TPK, TObject> GetTable<TPK, TObject>(this SQLiteConnection conn) where TObject : SQLObject<TPK>, new() => new(conn);

        public static SQLArray<TObject> GetArray<TObject>(this SQLiteConnection conn) where TObject : SQLObject, new() => new(conn);
    }
}
