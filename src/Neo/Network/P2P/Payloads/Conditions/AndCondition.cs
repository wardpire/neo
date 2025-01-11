// Copyright (C) 2015-2024 The Neo Project.
//
// AndCondition.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Network.P2P.Payloads.Conditions
{
    /// <summary>
    /// Represents the condition that all conditions must be met.
    /// </summary>
    public class AndCondition : WitnessCondition, IEquatable<AndCondition>
    {
        /// <summary>
        /// The expressions of the condition.
        /// </summary>
        public WitnessCondition[] Expressions;

        public override int Size => base.Size + Expressions.GetVarSize();
        public override WitnessConditionType Type => WitnessConditionType.And;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AndCondition other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null) return false;
            return
                Type == other.Type &&
                Expressions.SequenceEqual(other.Expressions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is AndCondition ac && Equals(ac);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Expressions);
        }

        protected override void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth)
        {
            if (maxNestDepth <= 0) throw new FormatException();
            Expressions = DeserializeConditions(ref reader, maxNestDepth - 1);
            if (Expressions.Length == 0) throw new FormatException();
        }

        public override bool Match(ApplicationEngine engine)
        {
            return Expressions.All(p => p.Match(engine));
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Expressions);
        }

        private protected override void ParseJson(JObject json, int maxNestDepth)
        {
            if (maxNestDepth <= 0) throw new FormatException();
            JArray expressions = (JArray)json["expressions"];
            if (expressions.Count > MaxSubitems) throw new FormatException();
            Expressions = expressions.Select(p => FromJson((JObject)p, maxNestDepth - 1)).ToArray();
            if (Expressions.Length == 0) throw new FormatException();
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["expressions"] = Expressions.Select(p => p.ToJson()).ToArray();
            return json;
        }

        public override StackItem ToStackItem(IReferenceCounter referenceCounter)
        {
            var result = (VM.Types.Array)base.ToStackItem(referenceCounter);
            result.Add(new VM.Types.Array(referenceCounter, Expressions.Select(p => p.ToStackItem(referenceCounter))));
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AndCondition left, AndCondition right)
        {
            if (left is null || right is null)
                return Equals(left, right);

            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AndCondition left, AndCondition right)
        {
            if (left is null || right is null)
                return !Equals(left, right);

            return !left.Equals(right);
        }
    }
}
