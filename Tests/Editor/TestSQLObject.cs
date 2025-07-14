/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/24-11:24:09
 */

using System;
using NUnit.Framework;
using SQLite;
using UnityEngine;

namespace Gilzoide.SqliteNet.Tests.Editor
{
    public partial class InternalRow2 : SQLObject<int>
    {
        [PrimaryKey] public int Id { get; set; }
        private int Value { get; set; }
    }

    public class TestSQLObject
    {
        [Test]
        public void UpdateTest()
        {
            const int value = 23;
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var row = new InternalRow
                {
                    InternalConnection = db
                };
                db.Insert(row);
                row.SetValue(value);
                Assert.AreEqual(row.GetValue(), value);
                Assert.AreEqual(db.Get<InternalRow>(row.GetPrimaryKey()).GetValue(), value);
            }
        }

        [Test]
        public void UpdateWithTransactionTest()
        {
            const int value = 23;
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow>();
                var row = new InternalRow
                {
                    InternalConnection = db
                };
                db.BeginTransaction();
                db.Insert(row);
                row.SetValue(value);
                Assert.AreEqual(row.GetValue(), value);
                Assert.AreEqual(db.Get<InternalRow>(row.GetPrimaryKey()).GetValue(), value);
            }
        }

        [Test]
        public void UpdateWithTransactionFailedTest()
        {
            const int value = 23;
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow2>();
                var row = new InternalRow2
                {
                    Id                 = 1,
                    InternalConnection = db
                };
                db.BeginTransaction();
                try
                {
                    row.SetValue(value);
                    db.InsertAll(new[] {row, new InternalRow2 {Id = 1}}); // Insert a duplicate row that causes a conflict.
                    Assert.AreNotEqual(row.GetValue(), value);
                }
                catch (Exception) { }

                db.Commit();
                Assert.AreEqual(row.GetValue(), 0);
            }
        }

        [Test]
        public void RollbackTest()
        {
            using (var db = new SQLiteConnection(""))
            {
                db.CreateTable<InternalRow2>();
                var row = new InternalRow2
                {
                    Id                 = 1,
                    InternalConnection = db
                };
                db.Insert(row);
                row.SetValue(123);
                var savepoint = db.SaveTransactionPoint();
                row.SetValue(234);
                db.SaveTransactionPoint();
                row.SetValue(345);
                db.SaveTransactionPoint();
                db.RollbackTo(savepoint);
                var raw = db.Query<InternalRow2>($"select * from {nameof(InternalRow2)}")[0];
                Assert.AreEqual(123, row.GetValue());
                Assert.AreEqual(123, raw.GetValue());
            }
        }
    }
}
