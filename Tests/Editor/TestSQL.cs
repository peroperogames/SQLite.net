/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 */

using System.Collections.Generic;
using NUnit.Framework;
using SQLite;

namespace Gilzoide.SqliteNet.Tests.Editor
{
    public class CopyFromTestBaseRow
    {
        [PrimaryKey] public int Id { get; set; }
    }

    [Table("CopyFromTestDerived")]
    public class CopyFromTestDerivedRow : CopyFromTestBaseRow { }

    public class CopyFromTestLargeRow
    {
        [PrimaryKey] public int Id { get; set; }
        public string Data { get; set; }
    }

    public class TestSQL
    {
        [Test]
        public void CopyFromUsesGivenTypeMappingInsteadOfRuntimeType()
        {
            using var db = new SQLiteConnection("");

            // CopyFrom creates the table for typeof(CopyFromTestBaseRow), so the
            // insert must target that same table, not the runtime type's table.
            var copied = db.CopyFrom(typeof(CopyFromTestBaseRow), new List<object>
            {
                new CopyFromTestDerivedRow { Id = 1 },
            });

            Assert.AreEqual(1, copied);
            Assert.AreEqual(1, db.ExecuteScalar<int>("select count(*) from \"CopyFromTestBaseRow\""));
        }

        [Test]
        public void CopyFromThrowsWhenCreateTableFails()
        {
            using var db = new SQLiteConnection("");
            db.Execute("PRAGMA max_page_count=1");

            var ex = Assert.Throws<SQLiteException>(() => db.CopyFrom(new List<CopyFromTestDerivedRow>
            {
                new() { Id = 1 },
            }));

            // The create must fail loudly at CreateTable instead of being swallowed
            // and surfacing later as "no such table" from the first insert.
            Assert.AreEqual(SQLite3.Result.Full, ex.Result);
            StringAssert.Contains("create table", ex.Message);
            StringAssert.Contains("database or disk is full", ex.Message);
            StringAssert.DoesNotContain("no such table", ex.Message);
        }

        [Test]
        public void CopyFromWorksWithMatchingType()
        {
            using var db = new SQLiteConnection("");

            var copied = db.CopyFrom(new List<CopyFromTestDerivedRow>
            {
                new() { Id = 1 }
            });

            Assert.AreEqual(1, copied);
            Assert.AreEqual(1, db.ExecuteScalar<int>("select count(*) from \"CopyFromTestDerived\""));
        }

        [Test]
        public void CopyFromReplacesRowsWithDuplicatePrimaryKeys()
        {
            using var db = new SQLiteConnection("");

            db.CopyFrom(new List<CopyFromTestBaseRow>
            {
                new() { Id = 1 },
                new() { Id = 1 }
            });

            Assert.AreEqual(1, db.ExecuteScalar<int>("select count(*) from \"CopyFromTestBaseRow\""));
        }

        [Test]
        public void CopyFromThrowsWhenInsertFails()
        {
            using var db = new SQLiteConnection("");
            db.Execute("PRAGMA max_page_count=6");

            var rows = new List<CopyFromTestLargeRow>();
            for (var i = 0; i < 100; i++)
            {
                rows.Add(new CopyFromTestLargeRow { Id = i, Data = new string('x', 4000) });
            }

            // No transaction here: a failing rollback can mask the original insert error.
            var ex = Assert.Throws<SQLiteException>(() => db.CopyFrom(rows, runInTransaction: false));

            Assert.AreEqual(SQLite3.Result.Full, ex.Result);
            StringAssert.Contains("insert into", ex.Message);
            StringAssert.Contains("database or disk is full", ex.Message);
        }

        [Test]
        public void CreateIndexThrowsWhenWriteFails()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<CopyFromTestBaseRow>();
            db.Insert(new CopyFromTestBaseRow { Id = 1 });
            db.Execute($"PRAGMA max_page_count={db.ExecuteScalar<int>("PRAGMA page_count")}");

            var ex = Assert.Throws<SQLiteException>(() =>
                db.CreateIndex("ix_copyfrom_id", "CopyFromTestBaseRow", "Id"));

            Assert.AreEqual(SQLite3.Result.Full, ex.Result);
            StringAssert.Contains("create index", ex.Message);
            StringAssert.Contains("database or disk is full", ex.Message);
        }

        [Test]
        public void QueryThrowsWhenStepFails()
        {
            using var db = new SQLiteConnection("");
            db.CreateTable<CopyFromTestBaseRow>();

            // The statement prepares fine, but stepping it raises "integer overflow".
            var ex = Assert.Throws<SQLiteException>(() =>
                db.Query<CopyFromTestBaseRow>("select abs(-9223372036854775808) as Id"));

            Assert.AreEqual(SQLite3.Result.Error, ex.Result);
            StringAssert.Contains("integer overflow", ex.Message);
        }
    }
}
