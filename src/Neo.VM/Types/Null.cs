// Copyright (C) 2015-2025 The Neo Project.
//
// Null.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Neo.VM.Types
{
    /// <summary>
    /// Represents <see langword="null"/> in the VM.
    /// </summary>
    public class Null : StackItem
    {
        public override StackItemType Type => StackItemType.Any;

        internal Null() { }

        public override StackItem ConvertTo(StackItemType type)
        {
            if (type == StackItemType.Any || !Enum.IsDefined(typeof(StackItemType), type))
                throw new InvalidCastException($"Type {nameof(Null)} can't be converted to StackItemType: {type}");
            return this;
        }

        public override bool Equals(StackItem? other)
        {
            if (ReferenceEquals(this, other)) return true;
            return other is Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool GetBoolean()
        {
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return 0;
        }

        [return: MaybeNull]
        public override T GetInterface<T>()
        {
            return default;
        }

        public override string? GetString()
        {
            return null;
        }

        public override string ToString()
        {
            return "NULL";
        }
    }
}
