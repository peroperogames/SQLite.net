/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/05/20-16:54:09
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Text;

namespace SQLite
{
    public partial class SQLiteCommand
    {
        public string ToRealSQLText()
        {
            var result = ZString.CreateStringBuilder();
            var isUpdate = CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase);
            var index = isUpdate ? 0 : _bindings.Count - 1;
            for (var i = 0; i < CommandText.Length; i++)
            {
                if (CommandText[i] == '?')
                {
                    var binding = _bindings[index];
                    result.Append('\'');
                    if (binding.Value is IBox box)
                    {
                        result.AppendBox(box);
                    }
                    else
                    {
                        result.Append(binding.Value);
                    }
                    result.Append('\'');

                    if (isUpdate)
                    {
                        index++;
                    }
                    else
                    {
                        index--;
                    }
                }
                else
                {
                    result.Append(CommandText[i]);
                }
            }

            var sql = result.ToString();
            result.Dispose();
            return sql;
        }
    }

    partial class PreparedSqlLiteInsertCommand
    {
        public string ToRealSQLText(IReadOnlyList<object> bindings)
        {
            var result = ZString.CreateStringBuilder();
            var index = 0;
            for (var i = 0; i < CommandText.Length; i++)
            {
                if (CommandText[i] == '?')
                {
                    var value = bindings[index++];
                    result.Append('\'');
                    if (value is IBox box)
                    {
                        result.AppendBox(box);
                    }
                    else
                    {
                        result.Append(value);
                    }

                    result.Append('\'');
                }
                else
                {
                    result.Append(CommandText[i]);
                }
            }

            var sql = result.ToString();
            result.Dispose();
            return sql;
        }
    }

    public static class Utf16ValueStringBuilderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AppendBox(ref this Utf16ValueStringBuilder builder, IBox box)
        {
            switch (box)
            {
                case Box<int> theBox:            builder.Append(theBox.Value); break;
                case Box<uint> theBox:           builder.Append(theBox.Value); break;
                case Box<short> theBox:          builder.Append(theBox.Value); break;
                case Box<ushort> theBox:         builder.Append(theBox.Value); break;
                case Box<long> theBox:           builder.Append(theBox.Value); break;
                case Box<ulong> theBox:          builder.Append(theBox.Value); break;
                case Box<char> theBox:           builder.Append(theBox.Value); break;
                case Box<bool> theBox:           builder.Append(theBox.Value); break;
                case Box<byte> theBox:           builder.Append(theBox.Value); break;
                case Box<sbyte> theBox:          builder.Append(theBox.Value); break;
                case Box<float> theBox:          builder.Append(theBox.Value); break;
                case Box<double> theBox:         builder.Append(theBox.Value); break;
                case Box<decimal> theBox:        builder.Append(theBox.Value); break;
                case Box<DateTime> theBox:       builder.Append(theBox.Value); break;
                case Box<DateTimeOffset> theBox: builder.Append(theBox.Value); break;
                case Box<TimeSpan> theBox:       builder.Append(theBox.Value); break;
                case Box<Guid> theBox:           builder.Append(theBox.Value); break;
                default:                         builder.Append(box.Unbox()); break;
            }
        }
    }
}
