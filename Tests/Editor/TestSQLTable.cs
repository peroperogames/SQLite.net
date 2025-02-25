/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/21-18:05:56
 */

using NUnit.Framework;
using SQLite;

namespace Gilzoide.SqliteNet.Tests.Editor
{
    public class TestSQLTable
    {
        [Test]
        public void AddTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var table = db.GetTable<int, InternalRow>();
                var row = new InternalRow();
                var pk = row.GetPrimaryKey();
                table.Add(row);
                Assert.IsTrue(db.Query<InternalRow>($"select * from {table.TableName}").Count == 1);
                Assert.AreNotEqual(pk, row.GetPrimaryKey());
            }
        }

        [Test]
        public void RemoveTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var row = new InternalRow();
                db.Insert(row);
                var table = db.GetTable<int, InternalRow>();
                Assert.IsTrue(table.Remove(row));
                Assert.AreEqual(table.Count, 0);
                Assert.IsTrue(db.Query<InternalRow>($"select * from {table.TableName}").Count == 0);
            }
        }

        [Test]
        public void GetTableTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var row = new InternalRow();
                db.Insert(row);
                var table = db.GetTable<int, InternalRow>();
                Assert.IsTrue(table.Contains(row));
                Assert.AreEqual(table.Count, 1);
            }
        }

        [Test]
        public void ClearTableTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var row = new InternalRow();
                db.Insert(row);
                var table = db.GetTable<int, InternalRow>();
                table.Clear();
                Assert.IsFalse(table.Contains(row));
                Assert.AreEqual(table.Count, 0);
            }
        }
    }
}
