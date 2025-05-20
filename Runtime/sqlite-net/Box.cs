/*
 * Copyright (c) PeroPeroGames Co., Ltd.
 * Author: Gatongone
 * Created On: 2025/05/07-14:20:00
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SQLite
{
    public interface IBox
    {
        object Unbox();
    }
    public class Box<T> : IBox, IEquatable<T> where T : struct
    {
        public T Value;
        private Box(T value) => Value = value;
        private static Stack<Box<T>> _pool = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Box<T> Take(T value)
        {
            if (!_pool.TryPop(out var box))
            {
                box = new Box<T>(value);
            }
            else
            {
                box.Value = value;
            }

            return box;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(Box<T> box) => _pool.Push(box);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool Equals(Box<T> other) => Value.Equals(other.Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T other) => Value.Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Box<T>) obj);
        }

        public override int GetHashCode() => Value.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Unbox() => Value;
    }
}
