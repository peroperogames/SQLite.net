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
        }

        public void Query(TableMapping map, string query, List<object> col, object[] args)
        {
            col.Clear();
            var cmd = CreateCommand(query, args);
            foreach (var item in cmd.ExecuteDeferredQuery<object>(map))
            {
                col.Add(item);
            }
        }

        public void QueryScalars<T>(string query, List<T> col, object[] args)
        {
            col.Clear();
            var cmd = CreateCommand(query, args);
            foreach (var item in cmd.ExecuteQueryScalars<T>())
            {
                col.Add(item);
            }
        }
    }
}
