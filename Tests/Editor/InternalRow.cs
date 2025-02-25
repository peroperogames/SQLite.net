/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/02/21-18:07:16
 */

using SQLite;

namespace Gilzoide.SqliteNet.Tests.Editor
{
    public partial class InternalRow : SQLObject<int>
    {
        [PrimaryKey, AutoIncrement]
        private int Id { get; set; }
        private int Value { get; set; }
    }
}
