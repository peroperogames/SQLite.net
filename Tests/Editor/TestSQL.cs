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
            StringAssert.Contains("create table", ex.Message);
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
    }
}
