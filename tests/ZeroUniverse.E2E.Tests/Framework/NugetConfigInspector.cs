using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace ZeroUniverse.E2E.Tests.Framework
{
    public sealed class NugetSourceInfo
    {
        public string ConfigPath { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsLocalDirectory { get; set; }
        public bool DirectoryExists { get; set; }
    }

    public static class NugetConfigInspector
    {
        public static List<NugetSourceInfo> InspectAllConfigs(string? rootPath = null)
        {
            rootPath ??= EcosystemScanner.ResolveWorkspaceRoot();
            var results = new List<NugetSourceInfo>();

            string[] configs = Directory.GetFiles(rootPath, "nuget.config", SearchOption.AllDirectories);

            foreach (var cfg in configs)
            {
                if (cfg.Contains(@"\obj\") || cfg.Contains(@"\bin\"))
                    continue;

                string cfgDir = Path.GetDirectoryName(cfg) ?? string.Empty;

                try
                {
                    var doc = XDocument.Load(cfg);
                    var packageSources = doc.Root?.Element("packageSources");
                    if (packageSources != null)
                    {
                        foreach (var add in packageSources.Elements("add"))
                        {
                            string key = add.Attribute("key")?.Value ?? string.Empty;
                            string val = add.Attribute("value")?.Value ?? string.Empty;

                            bool isLocal = !val.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                                           !val.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

                            bool exists = false;
                            if (isLocal)
                            {
                                string full = Path.GetFullPath(Path.Combine(cfgDir, val));
                                exists = Directory.Exists(full);
                            }

                            results.Add(new NugetSourceInfo
                            {
                                ConfigPath = cfg,
                                Key = key,
                                Value = val,
                                IsLocalDirectory = isLocal,
                                DirectoryExists = exists
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading nuget.config at {cfg}: {ex.Message}");
                }
            }

            return results;
        }
    }
}
