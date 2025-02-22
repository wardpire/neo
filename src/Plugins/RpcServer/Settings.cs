// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neo.Plugins.RpcServer
{
    class Settings : PluginSettings
    {
        public IReadOnlyList<RpcServerSettings> Servers { get; init; }

        public Settings(IConfigurationSection section, NativeContractRepository nativeContractRepository) : base(section)
        {
            Servers = section.GetSection(nameof(Servers)).GetChildren().Select(p => RpcServerSettings.Load(p, nativeContractRepository)).ToArray();
        }
    }

    public record RpcServerSettings
    {
        public uint Network { get; init; }
        public IPAddress BindAddress { get; init; }
        public ushort Port { get; init; }
        public string SslCert { get; init; }
        public string SslCertPassword { get; init; }
        public string[] TrustedAuthorities { get; init; }
        public int MaxConcurrentConnections { get; init; }
        public int MaxRequestBodySize { get; init; }
        public string RpcUser { get; init; }
        public string RpcPass { get; init; }
        public bool EnableCors { get; init; }
        public string[] AllowOrigins { get; init; }
        public int KeepAliveTimeout { get; init; }
        public uint RequestHeadersTimeout { get; init; }
        // In the unit of datoshi, 1 GAS = 10^8 datoshi
        public long MaxGasInvoke { get; init; }
        // In the unit of datoshi, 1 GAS = 10^8 datoshi
        public long MaxFee { get; init; }
        public int MaxIteratorResultItems { get; init; }
        public int MaxStackSize { get; init; }
        public string[] DisabledMethods { get; init; }
        public bool SessionEnabled { get; init; }
        public TimeSpan SessionExpirationTime { get; init; }
        public int FindStoragePageSize { get; init; }

        private static RpcServerSettings _default;
        public static RpcServerSettings GetDefault(NativeContractRepository nativeContractRepository) => _default ??= new RpcServerSettings
        {
            Network = 5195086u,
            BindAddress = IPAddress.None,
            SslCert = string.Empty,
            SslCertPassword = string.Empty,
            MaxGasInvoke = (long)new BigDecimal(10M, nativeContractRepository.GAS.Decimals).Value,
            MaxFee = (long)new BigDecimal(0.1M, nativeContractRepository.GAS.Decimals).Value,
            TrustedAuthorities = Array.Empty<string>(),
            EnableCors = true,
            AllowOrigins = Array.Empty<string>(),
            KeepAliveTimeout = 60,
            RequestHeadersTimeout = 15,
            MaxIteratorResultItems = 100,
            MaxStackSize = ushort.MaxValue,
            DisabledMethods = Array.Empty<string>(),
            MaxConcurrentConnections = 40,
            MaxRequestBodySize = 5 * 1024 * 1024,
            SessionEnabled = false,
            SessionExpirationTime = TimeSpan.FromSeconds(60),
            FindStoragePageSize = 50
        };

        public static RpcServerSettings Load(IConfigurationSection section, NativeContractRepository nativeContractRepository) => new()
        {
            Network = section.GetValue("Network", GetDefault(nativeContractRepository).Network),
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value),
            Port = ushort.Parse(section.GetSection("Port").Value),
            SslCert = section.GetSection("SslCert").Value,
            SslCertPassword = section.GetSection("SslCertPassword").Value,
            TrustedAuthorities = section.GetSection("TrustedAuthorities").GetChildren().Select(p => p.Get<string>()).ToArray(),
            RpcUser = section.GetSection("RpcUser").Value,
            RpcPass = section.GetSection("RpcPass").Value,
            EnableCors = section.GetValue(nameof(EnableCors), GetDefault(nativeContractRepository).EnableCors),
            AllowOrigins = section.GetSection(nameof(AllowOrigins)).GetChildren().Select(p => p.Get<string>()).ToArray(),
            KeepAliveTimeout = section.GetValue(nameof(KeepAliveTimeout), GetDefault(nativeContractRepository).KeepAliveTimeout),
            RequestHeadersTimeout = section.GetValue(nameof(RequestHeadersTimeout), GetDefault(nativeContractRepository).RequestHeadersTimeout),
            MaxGasInvoke = (long)new BigDecimal(section.GetValue<decimal>("MaxGasInvoke", GetDefault(nativeContractRepository).MaxGasInvoke), nativeContractRepository.GAS.Decimals).Value,
            MaxFee = (long)new BigDecimal(section.GetValue<decimal>("MaxFee", GetDefault(nativeContractRepository).MaxFee), nativeContractRepository.GAS.Decimals).Value,
            MaxIteratorResultItems = section.GetValue("MaxIteratorResultItems", GetDefault(nativeContractRepository).MaxIteratorResultItems),
            MaxStackSize = section.GetValue("MaxStackSize", GetDefault(nativeContractRepository).MaxStackSize),
            DisabledMethods = section.GetSection("DisabledMethods").GetChildren().Select(p => p.Get<string>()).ToArray(),
            MaxConcurrentConnections = section.GetValue("MaxConcurrentConnections", GetDefault(nativeContractRepository).MaxConcurrentConnections),
            MaxRequestBodySize = section.GetValue("MaxRequestBodySize", GetDefault(nativeContractRepository).MaxRequestBodySize),
            SessionEnabled = section.GetValue("SessionEnabled", GetDefault(nativeContractRepository).SessionEnabled),
            SessionExpirationTime = TimeSpan.FromSeconds(section.GetValue("SessionExpirationTime", (int)GetDefault(nativeContractRepository).SessionExpirationTime.TotalSeconds)),
            FindStoragePageSize = section.GetValue("FindStoragePageSize", GetDefault(nativeContractRepository).FindStoragePageSize)
        };
    }
}
