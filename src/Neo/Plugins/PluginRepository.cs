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
using Neo.Persistence;
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
    public class PluginRepository
    {
        /// <summary>
        /// A list of all loaded plugins.
        /// </summary>
        public readonly List<Plugin> Plugins = new();

        /// <summary>
        /// The directory containing the plugin folders. Files can be contained in any subdirectory.
        /// </summary>
        public readonly string PluginsDirectory = Combine(GetDirectoryName(System.AppContext.BaseDirectory), "Plugins");

        private readonly FileSystemWatcher configWatcher;

        public PluginRepository()
        {
            if (!Directory.Exists(PluginsDirectory)) return;
            configWatcher = new FileSystemWatcher(PluginsDirectory)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            configWatcher.Changed += ConfigWatcher_Changed;
            configWatcher.Created += ConfigWatcher_Changed;
            configWatcher.Renamed += ConfigWatcher_Changed;
            configWatcher.Deleted += ConfigWatcher_Changed;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private void ConfigWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            switch (GetExtension(e.Name))
            {
                case ".json":
                case ".dll":
                    Utility.Log(nameof(Plugin), LogLevel.Warning, $"File {e.Name} is {e.ChangeType}, please restart node.");
                    break;
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains(".resources"))
                return null;

            AssemblyName an = new(args.Name);

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name) ??
                                AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == an.Name);
            if (assembly != null) return assembly;

            string filename = an.Name + ".dll";
            string path = filename;
            if (!File.Exists(path)) path = Combine(GetDirectoryName(System.AppContext.BaseDirectory), filename);
            if (!File.Exists(path)) path = Combine(PluginsDirectory, filename);
            if (!File.Exists(path)) path = Combine(PluginsDirectory, args.RequestingAssembly.GetName().Name, filename);
            if (!File.Exists(path)) return null;

            try
            {
                return Assembly.Load(File.ReadAllBytes(path));
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(Plugin), LogLevel.Error, ex);
                return null;
            }
        }

        private void LoadPlugin(Assembly assembly)
        {
            foreach (Type type in assembly.ExportedTypes)
            {
                if (!type.IsSubclassOf(typeof(Plugin))) continue;
                if (type.IsAbstract) continue;

                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                try
                {
                    Plugins.Add((Plugin)constructor?.Invoke(null)!);
                }
                catch (Exception ex)
                {
                    Utility.Log(nameof(Plugin), LogLevel.Error, ex);
                }
            }
        }

        public void FindAndLoadPlugins()
        {
            if (!Directory.Exists(PluginsDirectory)) return;
            List<Assembly> assemblies = new();
            foreach (string rootPath in Directory.GetDirectories(PluginsDirectory))
            {
                foreach (var filename in Directory.EnumerateFiles(rootPath, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        assemblies.Add(Assembly.Load(File.ReadAllBytes(filename)));
                    }
                    catch { }
                }
            }
            foreach (Assembly assembly in assemblies)
            {
                LoadPlugin(assembly);
            }
        }

        /// <summary>
        /// Sends a message to all plugins. It can be handled by <see cref="OnMessage"/>.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns><see langword="true"/> if the <paramref name="message"/> is handled by a plugin; otherwise, <see langword="false"/>.</returns>
        public bool SendMessage(object message)
        {
            return Plugins.Any(plugin => plugin.OnMessage(message));
        }

        private MemoryStoreProvider? _memoryStoreProvider;

        /// <summary>
        /// Get store provider by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Store provider</returns>
        public IStoreProvider? GetStoreProvider(string name)
        {
            if (string.IsNullOrEmpty(name) || name.ToLowerInvariant() == nameof(MemoryStore).ToLowerInvariant())
            {
                return _memoryStoreProvider ??= new MemoryStoreProvider();
            }
            return Plugins.Where(x => x is IStoreProvider storeProvider && storeProvider.Name == name).Select(x => x as IStoreProvider).FirstOrDefault();
        }

        /// <summary>
        /// Get store from name
        /// </summary>
        /// <param name="storageProvider">The storage engine used to create the <see cref="IStore"/> objects. If this parameter is <see langword="null"/>, a default in-memory storage engine will be used.</param>
        /// <param name="path">The path of the storage. If <paramref name="storageProvider"/> is the default in-memory storage engine, this parameter is ignored.</param>
        /// <returns>The storage engine.</returns>
        public IStore? GetStore(string storageProvider, string path)
        {
            return GetStoreProvider(storageProvider)?.GetStore(path);
        }
    }
}
