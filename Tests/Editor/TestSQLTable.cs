/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/21-18:05:56
 */

using System.Linq;
using NUnit.Framework;
using SQLite;

namespace Gilzoide.SqliteNet.Tests.Editor
{
    public class TestSQLTable
    {
        [Test]
        public void AddTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var table = db.GetTable<int, InternalRow>();
            var row = new InternalRow();
            var pk = row.GetPrimaryKey();
            table.Add(row);
            Assert.NotNull(row.InternalConnection);
            Assert.IsTrue(db.Query<InternalRow>($"select * from {table.TableName}").Count == 1);
            Assert.AreNotEqual(pk, row.GetPrimaryKey());
        }

        [Test]
        public void RemoveTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var row = new InternalRow();
            db.Insert(row);
            var table = db.GetTable<int, InternalRow>();
            Assert.IsTrue(table.Remove(row));
            Assert.AreEqual(table.Count, 0);
            Assert.IsTrue(db.Query<InternalRow>($"select * from {table.TableName}").Count == 0);
        }

        [Test]
        public void GetTableTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var row = new InternalRow();
            db.Insert(row);
            var table = db.GetTable<int, InternalRow>();
            Assert.IsTrue(table.Contains(row));
            Assert.AreEqual(table.Count, 1);
        }

        [Test]
        public void ClearTableTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var row = new InternalRow();
            db.Insert(row);
            var table = db.GetTable<int, InternalRow>();
            table.Clear();
            Assert.IsFalse(table.Contains(row));
            Assert.AreEqual(table.Count, 0);
        }

        [Test]
        public void SetRollbackTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var row = new InternalRow();
            db.Insert(row);
            var table = db.GetTable<int, InternalRow>();
            table[row.GetId()] = new InternalRow(row.GetId(), 123);
            var savepoint = db.SaveTransactionPoint();
            table[row.GetId()] = new InternalRow(row.GetId(), 234);
            db.SaveTransactionPoint();
            table[row.GetId()] = new InternalRow(row.GetId(), 345);
            db.SaveTransactionPoint();
            db.RollbackTo(savepoint);
            var raw = db.Query<InternalRow>($"select * from {table.TableName}")[0];
            Assert.AreEqual(123, raw.GetValue());
            Assert.AreEqual(123, table[row.GetId()].GetValue());
        }

        [Test]
        public void AddRollbackTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var table = db.GetTable<int, InternalRow>();
            table.Add(new InternalRow(123));
            var savepoint = db.SaveTransactionPoint();
            table.Add(new InternalRow(234));
            db.SaveTransactionPoint();
            table.Add(new InternalRow(345));
            db.SaveTransactionPoint();
            db.RollbackTo(savepoint);
            var rawTab = db.Query<InternalRow>($"select * from {table.TableName}");
            Assert.AreEqual(1, rawTab.Count);
            Assert.AreEqual(123, rawTab[0].GetValue());
            Assert.AreEqual(123, table[1].GetValue());
        }

        [Test]
        public void RemoveRollbackTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            db.Insert(new InternalRow(123));
            db.Insert(new InternalRow(234));
            db.Insert(new InternalRow(345));
            var table = db.GetTable<int, InternalRow>();
            var savepoint = db.SaveTransactionPoint();
            table.Remove(table.First());
            var tempSavepoint = db.SaveTransactionPoint();
            table.Remove(table.First());
            db.SaveTransactionPoint();
            table.Remove(table.First());
            db.Release(tempSavepoint);
            db.RollbackTo(savepoint);
            var rawTable = db.Query<InternalRow>($"select * from {table.TableName}");
            Assert.AreEqual(3, rawTable.Count);
            Assert.AreEqual(3, table.Count);
            Assert.AreEqual(123, rawTable[0].GetValue());
            Assert.AreEqual(123, table[1].GetValue());
        }

        [Test]
        public void RemoveAllRollbackTest()
        {
            var ir1 = new InternalRow(123);
            var ir2 = new InternalRow(456);
            var ir3 = new InternalRow(789);
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var table = db.GetTable<int, InternalRow>();
            var savePoint = db.SaveTransactionPoint();
            table.Add(ir1);
            table.Add(ir2);
            table.Add(ir3);
            table.Remove(ir1);
            table.Remove(ir2);
            table.Remove(ir3);
            db.RollbackTo(savePoint);
            Assert.IsEmpty(table);
        }

        [Test]
        public void InvalidRollbackTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            var table = db.GetTable<int, InternalRow>();
            var row = new InternalRow(123);
            table.Add(new InternalRow(234));
            table.Add(new InternalRow(789));
            table.Add(row);
            db.BeginTransaction();
            table.Remove(row.GetPrimaryKey());
            db.Rollback();
            Assert.IsTrue(table.Count == 3);
        }

        [Test]
        public void ClearRollbackTest()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<InternalRow>();
            db.Insert(new InternalRow(123));
            db.Insert(new InternalRow(234));
            db.Insert(new InternalRow(345));
            var table = db.GetTable<int, InternalRow>();
            var savepoint = db.SaveTransactionPoint();
            table.Clear();
            table.Add(new InternalRow(456));
            db.SaveTransactionPoint();
            table.Add(new InternalRow(789));
            Assert.AreEqual(2, table.Count);
            db.RollbackTo(savepoint);
            var rawTable = db.Query<InternalRow>($"select * from {table.TableName}");
            Assert.AreEqual(3, rawTable.Count);
            Assert.AreEqual(3, table.Count);
            Assert.AreEqual(123, rawTable[0].GetValue());
            Assert.AreEqual(234, rawTable[1].GetValue());
            Assert.AreEqual(345, rawTable[2].GetValue());
            Assert.AreEqual(123, table[1].GetValue());
            Assert.AreEqual(234, table[2].GetValue());
            Assert.AreEqual(345, table[3].GetValue());
        }
    }
}
