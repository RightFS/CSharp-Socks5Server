﻿/*
    Socks5 - A full-fledged high-performance socks5 proxy server written in C#. Plugin support included.
    Copyright (C) 2016 ThrDev

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Reflection;

namespace Socks5.Core.Plugin;

public static class PluginLoader
{
    //load plugin statically.

    private static readonly List<Type> _pluginTypes = new()
    {
        typeof(LoginHandler), 
        typeof(DataHandler), 
        typeof(ConnectHandler), 
        typeof(ClientConnectedHandler),
        typeof(ClientDisconnectedHandler),
        typeof(ConnectSocketOverrideHandler)
    };

    private static bool _loaded;
    public static bool LoadPluginsFromDisk { get; set; }

    public static List<object> GetPlugins { get; } = new();

    public static void LoadPlugins()
    {
        if (_loaded) return;
        try
        {
            try
            {
                foreach (var f in Assembly.GetExecutingAssembly().GetTypes())
                    try
                    {
                        if (!CheckType(f))
                        {
                            var type = Activator.CreateInstance(f);
                            if (type is not null)
                            {
                                GetPlugins.Push(type);    
                            }
#if DEBUG
                            Console.WriteLine("Loaded Embedded Plugin {0}.", f.FullName);
#endif
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                foreach (var f in Assembly.GetEntryAssembly()?.GetTypes() ?? []) 
                {
                    try
                    {
                        if (CheckType(f))
                        {
                            continue;
                        }
                        
                        //Console.WriteLine("Loaded type {0}.", f.ToString());
                        var type = Activator.CreateInstance(f);
                        if (type is not null)
                        {
                            GetPlugins.Push(type);    
                        }
#if DEBUG
                        Console.WriteLine("Loaded Plugin {0}.", f.FullName);
#endif
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //load plugins from disk?
            if (LoadPluginsFromDisk)
            {
                var pluginPath = Path.Combine(Environment.CurrentDirectory, "Plugins");
                if (!Directory.Exists(pluginPath)) Directory.CreateDirectory(pluginPath);
                foreach (var filename in Directory.GetFiles(pluginPath))
                {
                    if (!filename.EndsWith(".dll"))
                    {
                        continue;
                    }
                    
                    //Initialize unpacker.
                    var g = Assembly.Load(File.ReadAllBytes(filename));
                    //Test to see if it's a module.
                    foreach (var f in g.GetTypes())
                    {
                        if (CheckType(f))
                        {
                            continue;
                        }
                            
                        var plug = Activator.CreateInstance(f);
                        if (plug is not null)
                        {
                            GetPlugins.Push(plug);    
                        }
                    }
                }
            }
        }
        catch (ReflectionTypeLoadException e)
        {
            foreach (var p in e.LoaderExceptions)
            {
                Console.WriteLine(p?.ToString());
            }
        }

        _loaded = true;
    }

    public static bool LoadCustomPlugin(Type f)
    {
        try
        {
            if (!CheckType(f))
            {
                //Console.WriteLine("Loaded type {0}.", f.ToString());
                var type = Activator.CreateInstance(f);
                if (type is not null)
                {
                    GetPlugins.Push(type);
                }
#if DEBUG
                Console.WriteLine("Loaded Plugin {0}.", f.FullName);
#endif
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return false;
    }

    private static bool CheckType(Type p)
    {
        return _pluginTypes.All(x => !x.IsAssignableFrom(p) || p == x);
    }

    public static List<IGenericPlugin> LoadPlugin(Type assemblyType)
    {
        //make sure plugins are loaded.
        var list = GetPlugins
            .Where(assemblyType.IsInstanceOfType)
            .OfType<IGenericPlugin>()
            .Where(x => x.OnStart() && x.Enabled)
            .ToList();
        return list;
    }

    public static void ChangePluginStatus(bool enabled, Type pluginType)
    {
        foreach (var x in GetPlugins.Where(x => x.GetType() == pluginType))
        {
            //cast to generic type.
            ((IGenericPlugin)x).Enabled = enabled;
            break;
        }
    }
}