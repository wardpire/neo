// Copyright (C) 2015-2024 The Neo Project.
//
// ProtocolSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Neo
{
    /// <summary>
    /// Simple appsettings bindable object, that can be implictly casted into ProtocolSettings
    /// </summary>
    public class ProtocolSettingsOptions
    {
        /// <summary>
        /// The magic number of the NEO network.
        /// </summary>
        public uint Network { get; set; } = ProtocolSettings.Default.Network;

        /// <summary>
        /// The address version of the NEO system.
        /// </summary>
        public byte AddressVersion { get; set; } = ProtocolSettings.Default.AddressVersion;

        /// <summary>
        /// The public keys of the standby committee members.
        /// </summary>
        public IReadOnlyList<string> StandbyCommittee { get; set; } = [];

        /// <summary>
        /// The number of the validators in NEO system.
        /// </summary>
        public int ValidatorsCount { get; set; } = ProtocolSettings.Default.ValidatorsCount;

        /// <summary>
        /// The default seed nodes list.
        /// </summary>
        public string[] SeedList { get; set; } = ProtocolSettings.Default.SeedList;

        /// <summary>
        /// Indicates the time in milliseconds between two blocks.
        /// </summary>
        public uint MillisecondsPerBlock { get; set; } = ProtocolSettings.Default.MillisecondsPerBlock;

        /// <summary>
        /// Indicates the maximum number of transactions that can be contained in a block.
        /// </summary>
        public uint MaxTransactionsPerBlock { get; set; } = ProtocolSettings.Default.MaxTransactionsPerBlock;

        /// <summary>
        /// Indicates the maximum number of transactions that can be contained in the memory pool.
        /// </summary>
        public int MemoryPoolMaxTransactions { get; set; } = ProtocolSettings.Default.MemoryPoolMaxTransactions;

        /// <summary>
        /// Indicates the maximum number of blocks that can be traced in the smart contract.
        /// </summary>
        public uint MaxTraceableBlocks { get; set; } = ProtocolSettings.Default.MaxTraceableBlocks;

        /// <summary>
        /// Sets the block height from which a hardfork is activated.
        /// </summary>
        public ImmutableDictionary<string, uint>? Hardforks { get; set; } = default;

        /// <summary>
        /// Indicates the amount of gas to distribute during initialization.
        /// </summary>
        public ulong InitialGasDistribution { get; set; } = ProtocolSettings.Default.InitialGasDistribution;

        public static implicit operator ProtocolSettings(ProtocolSettingsOptions options) => ProtocolSettings.Load(options);
    }
}
