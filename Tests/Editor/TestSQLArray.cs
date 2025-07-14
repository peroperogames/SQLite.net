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
                Assert.NotNull(row.InternalConnection);
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

        [Test]
        public void SetRollbackTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var row = new InternalRow();
                db.Insert(row);
                var array = db.GetArray<InternalRow>();
                array[0] = new InternalRow(row.GetId(), 123);
                var savepoint = db.SaveTransactionPoint();
                array[0] = new InternalRow(row.GetId(), 234);
                db.SaveTransactionPoint();
                array[0] = new InternalRow(row.GetId(), 345);
                db.SaveTransactionPoint();
                db.RollbackTo(savepoint);
                var raw = db.Query<InternalRow>($"select * from {array.TableName}")[0];
                Assert.AreEqual(123, raw.GetValue());
                Assert.AreEqual(123, array[0].GetValue());
            }
        }

        [Test]
        public void AddRollbackTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var array = db.GetArray<InternalRow>();
                array.Add(new InternalRow(123));
                var savepoint = db.SaveTransactionPoint();
                array.Add(new InternalRow(234));
                db.SaveTransactionPoint();
                array.Add(new InternalRow(345));
                db.SaveTransactionPoint();
                db.RollbackTo(savepoint);
                var rawArray = db.Query<InternalRow>($"select * from {array.TableName}");
                Assert.AreEqual(1, rawArray.Count);
                Assert.AreEqual(123, rawArray[0].GetValue());
                Assert.AreEqual(123, array[0].GetValue());
            }
        }

        [Test]
        public void RemoveRollbackTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                db.Insert(new InternalRow(123));
                db.Insert(new InternalRow(234));
                db.Insert(new InternalRow(345));
                var array = db.GetArray<InternalRow>();
                var savepoint = db.SaveTransactionPoint();
                array.RemoveAt(0);
                var tempSavepoint = db.SaveTransactionPoint();
                array.RemoveAt(0);
                db.SaveTransactionPoint();
                array.RemoveAt(0);
                db.Release(tempSavepoint);
                db.RollbackTo(savepoint);
                var rawArray = db.Query<InternalRow>($"select * from {array.TableName}");
                Assert.AreEqual(3, rawArray.Count);
                Assert.AreEqual(3, array.Count);
                Assert.AreEqual(123, rawArray[0].GetValue());
                Assert.AreEqual(123, array[0].GetValue());
            }
        }

        [Test]
        public void ClearRollbackTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                db.Insert(new InternalRow(123));
                db.Insert(new InternalRow(234));
                db.Insert(new InternalRow(345));
                var array = db.GetArray<InternalRow>();
                var savepoint = db.SaveTransactionPoint();
                array.Clear();
                db.SaveTransactionPoint();
                array.Add(new InternalRow(456));
                db.SaveTransactionPoint();
                array.Add(new InternalRow(789));
                db.RollbackTo(savepoint);
                var rawArray = db.Query<InternalRow>($"select * from {array.TableName}");
                Assert.AreEqual(3, rawArray.Count);
                Assert.AreEqual(3, array.Count);
                Assert.AreEqual(123, rawArray[0].GetValue());
                Assert.AreEqual(234, rawArray[1].GetValue());
                Assert.AreEqual(345, rawArray[2].GetValue());
                Assert.AreEqual(123, array[0].GetValue());
                Assert.AreEqual(234, array[1].GetValue());
                Assert.AreEqual(345, array[2].GetValue());
            }
        }
    }
}
