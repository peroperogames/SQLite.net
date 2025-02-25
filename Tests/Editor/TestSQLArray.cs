/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/24-11:04:57
 */

using System.Linq;
using NUnit.Framework;
using SQLite;

namespace Gilzoide.SqliteNet.Tests.Editor
{
    public class TestSQLArray
    {
        [Test]
        public void AddTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var array = db.GetArray<InternalRow>();
                var row = new InternalRow();
                var pk = row.GetPrimaryKey();
                array.Add(row);
                Assert.IsTrue(db.Query<InternalRow>($"select * from {array.TableName}").Count == 1);
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
                var array = db.GetArray<InternalRow>();
                Assert.IsTrue(array.Remove(row));
                Assert.AreEqual(array.Count, 0);
                Assert.IsTrue(db.Query<InternalRow>($"select * from {array.TableName}").Count == 0);
            }
        }

        [Test]
        public void RemoveAtTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                db.Insert(new InternalRow());
                db.Insert(new InternalRow());
                var array = db.GetArray<InternalRow>();
                Assert.IsTrue(array.RemoveAt(0));
                Assert.IsTrue(array.Count == 1);
                var query = db.Query<InternalRow>($"select * from {array.TableName}");
                Assert.IsTrue(query.Count == 1);
                Assert.AreEqual(query.First().GetId(), 2);
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
                var array = db.GetArray<InternalRow>();
                Assert.IsTrue(array.Contains(row));
                Assert.AreEqual(array.Count, 1);
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
                var array = db.GetArray<InternalRow>();
                array.Clear();
                Assert.IsFalse(array.Contains(row));
                Assert.AreEqual(array.Count, 0);
            }
        }
    }
}
