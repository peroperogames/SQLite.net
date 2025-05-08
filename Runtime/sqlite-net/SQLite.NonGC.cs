/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/20-15:35:36
 */

using System.Collections.Generic;

namespace SQLite
{
    public partial interface ISQLiteConnection
    {
        void Query<T>(string query, List<T> col, params object[] args) where T : new();
        void Query(TableMapping map, string query, List<object> col, params object[] args);
        void QueryScalars<T>(string query, List<T> col, params object[] args);
    }

    public partial class SQLiteConnection
    {
        public void Query<T>(string query, List<T> col, object[] args) where T : new()
        {
            col.Clear();
            var cmd = CreateCommand(query, args);
            foreach (var result in cmd.ExecuteDeferredQuery<T>())
            {
                col.Add(result);
            }
            RecycleCommand(cmd);
        }

        public void Query(TableMapping map, string query, List<object> col, object[] args)
        {
            col.Clear();
            var cmd = CreateCommand(query, args);
            foreach (var item in cmd.ExecuteDeferredQuery<object>(map))
            {
                col.Add(item);
            }
            RecycleCommand(cmd);
        }

        public void QueryScalars<T>(string query, List<T> col, object[] args)
        {
            col.Clear();
            var cmd = CreateCommand(query, args);
            foreach (var item in cmd.ExecuteQueryScalars<T>())
            {
                col.Add(item);
            }
            RecycleCommand(cmd);
        }
    }
}
