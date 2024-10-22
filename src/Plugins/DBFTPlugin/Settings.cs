// Copyright (C) 2015-2024 The Neo Project.
//
// Settings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;

namespace Neo.Plugins.DBFTPlugin
{
    public class Settings : PluginSettings
    {
        public string RecoveryLogs { get; set; } = "ConsensusState";
        public bool IgnoreRecoveryLogs { get; set; } = false;
        public bool AutoStart { get; set; } = false;
        public uint Network { get; set; } = 5195086u;
        public uint MaxBlockSize { get; set; } = 262144u;
        public long MaxBlockSystemFee { get; set; } = 150000000000L;

        public Settings() : base(UnhandledExceptionPolicy.Ignore)
        {
        }

        public Settings(IConfigurationSection section) : base(section)
        {
            RecoveryLogs = section.GetValue("RecoveryLogs", "ConsensusState");
            IgnoreRecoveryLogs = section.GetValue("IgnoreRecoveryLogs", false);
            AutoStart = section.GetValue("AutoStart", false);
            Network = section.GetValue("Network", 5195086u);
            MaxBlockSize = section.GetValue("MaxBlockSize", 262144u);
            MaxBlockSystemFee = section.GetValue("MaxBlockSystemFee", 150000000000L);
        }
    }
}
