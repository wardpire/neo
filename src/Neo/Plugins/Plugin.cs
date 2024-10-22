// Copyright (C) 2015-2024 The Neo Project.
//
// Plugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static System.IO.Path;

namespace Neo.Plugins
{
    /// <summary>
    /// Represents the base class of all plugins. Any plugin should inherit this class.
    /// The plugins are automatically loaded when the process starts.
    /// </summary>
    public abstract class Plugin : IDisposable
    {
        private readonly string _pluginsDir;

        /// <summary>
        /// Indicates the root path of the plugin.
        /// </summary>
        public string RootPath => Combine(_pluginsDir, GetType().Assembly.GetName().Name);

        /// <summary>
        /// Indicates the location of the plugin configuration file.
        /// </summary>
        public virtual string ConfigFile => Combine(RootPath, "config.json");

        /// <summary>
        /// Indicates the name of the plugin.
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Indicates the description of the plugin.
        /// </summary>
        public virtual string Description => "";

        /// <summary>
        /// Indicates the location of the plugin dll file.
        /// </summary>
        public virtual string Path => Combine(RootPath, GetType().Assembly.ManifestModule.ScopeName);

        /// <summary>
        /// Indicates the version of the plugin.
        /// </summary>
        public virtual Version Version => GetType().Assembly.GetName().Version;

        /// <summary>
        /// If the plugin should be stopped when an exception is thrown.
        /// Default is StopNode.
        /// </summary>
        protected internal virtual UnhandledExceptionPolicy ExceptionPolicy { get; init; } = UnhandledExceptionPolicy.StopNode;

        /// <summary>
        /// The plugin will be stopped if an exception is thrown.
        /// But it also depends on <see cref="UnhandledExceptionPolicy"/>.
        /// </summary>
        internal bool IsStopped { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        protected Plugin()
        {
            _pluginsDir = AppContext.BaseDirectory;
            Configure();
        }

        /// <summary>
        /// Called when the plugin is loaded and need to load the configure file,
        /// or the configuration file has been modified and needs to be reconfigured.
        /// </summary>
        protected virtual void Configure()
        {
        }

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Loads the configuration file from the path of <see cref="ConfigFile"/>.
        /// </summary>
        /// <returns>The content of the configuration file read.</returns>
        protected IConfigurationSection GetConfiguration()
        {
            return new ConfigurationBuilder().AddJsonFile(ConfigFile, optional: true).Build()
                .GetSection("PluginConfiguration");
        }

        /// <summary>
        /// Write a log for the plugin.
        /// </summary>
        /// <param name="message">The message of the log.</param>
        /// <param name="level">The level of the log.</param>
        protected void Log(object message, LogLevel level = LogLevel.Info)
        {
            Utility.Log($"{nameof(Plugin)}:{Name}", level, message);
        }

        /// <summary>
        /// Called when a message to the plugins is received. The message is sent by calling <see cref="SendMessage"/>.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <returns><see langword="true"/> if the <paramref name="message"/> has been handled; otherwise, <see langword="false"/>.</returns>
        /// <remarks>If a message has been handled by a plugin, the other plugins won't receive it anymore.</remarks>
        public virtual bool OnMessage(object message)
        {
            return false;
        }

        /// <summary>
        /// Called when a <see cref="NeoSystem"/> is loaded.
        /// </summary>
        /// <param name="system">The loaded <see cref="NeoSystem"/>.</param>
        protected internal virtual void OnSystemLoaded(NeoSystem system)
        {
        }
    }
}
