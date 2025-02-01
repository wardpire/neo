// Copyright (C) 2015-2025 The Neo Project.
//
// PluginSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Security;
using System;

namespace Neo.Plugins;

public abstract class PluginSettings
{
    private readonly UnhandledExceptionPolicy _unhandledExceptionPolicy;

    protected PluginSettings(UnhandledExceptionPolicy unhandledExceptionPolicy)
    {
        _unhandledExceptionPolicy = unhandledExceptionPolicy;
    }

    protected PluginSettings(IConfigurationSection section)
    {
        var policyString = section?.GetValue(nameof(UnhandledExceptionPolicy), nameof(UnhandledExceptionPolicy.StopNode));
        if (!Enum.TryParse(policyString, true, out UnhandledExceptionPolicy _unhandledExceptionPolicy))
        {
            throw new InvalidParameterException($"{policyString} is not a valid UnhandledExceptionPolicy");
        }
    }

    public UnhandledExceptionPolicy ExceptionPolicy => _unhandledExceptionPolicy;
}
