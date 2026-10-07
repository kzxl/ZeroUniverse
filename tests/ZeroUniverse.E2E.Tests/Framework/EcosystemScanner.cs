using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ZeroUniverse.E2E.Tests.Framework
{
    public sealed class ProjectReferenceInfo
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string TargetProject { get; set; } = string.Empty;
    }

    public sealed class PackageReferenceInfo
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    public sealed class ProjectScanResult
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string ProjectName => Path.GetFileNameWithoutExtension(ProjectPath);
        public string RelativePath { get; set; } = string.Empty;
        public string TargetFrameworks { get; set; } = string.Empty;
        public List<PackageReferenceInfo> PackageReferences { get; } = new();
        public List<ProjectReferenceInfo> ProjectReferences { get; } = new();

        public bool ReferencesPackage(string packageId, out string version)
        {
            var match = PackageReferences.FirstOrDefault(p =>
                string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                version = match.Version;
                return true;
            }
            version = string.Empty;
            return false;
        }

        public bool ReferencesProject(string projectName)
        {
            return ProjectReferences.Any(p =>
                p.TargetProject.IndexOf(projectName, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    public sealed class SolutionScanResult
    {
        public string SolutionPath { get; set; } = string.Empty;
        public string SolutionName => Path.GetFileName(SolutionPath);
        public List<string> DeclaredProjectPaths { get; } = new();
        public List<string> MissingProjectPaths { get; } = new();
        public bool IsValid => MissingProjectPaths.Count == 0;
    }

    public static class EcosystemScanner
    {
        public static string ResolveWorkspaceRoot()
        {
            // Start from AppContext.BaseDirectory and traverse upward looking for ZeroPlatform and ZeroApps
            string current = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                if (Directory.Exists(Path.Combine(current, "ZeroPlatform")) &&
                    Directory.Exists(Path.Combine(current, "ZeroApps")))
                {
                    return current;
                }
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }

            // Fallback default
            if (Directory.Exists(@"E:\15. Other\ZeroUniverse"))
                return @"E:\15. Other\ZeroUniverse";

            throw new DirectoryNotFoundException("Could not locate ZeroUniverse workspace root.");
        }

        public static List<ProjectScanResult> ScanAllProjects(string? rootPath = null)
        {
            rootPath ??= ResolveWorkspaceRoot();
            var results = new List<ProjectScanResult>();

            string[] projectFiles = Directory.GetFiles(rootPath, "*.csproj", SearchOption.AllDirectories);

            foreach (var projFile in projectFiles)
            {
                // Skip temporary obj / bin files
                if (projFile.Contains(@"\obj\") || projFile.Contains(@"\bin\"))
                    continue;

                var result = ParseProject(projFile, rootPath);
                results.Add(result);
            }

            return results;
        }

        public static ProjectScanResult ParseProject(string filePath, string rootPath)
        {
            var scan = new ProjectScanResult
            {
                ProjectPath = filePath,
                RelativePath = Path.GetRelativePath(rootPath, filePath)
            };

            try
            {
                var doc = XDocument.Load(filePath);
                var root = doc.Root;
                if (root == null) return scan;

                // Extract TargetFramework(s)
                var tfElement = root.Descendants("TargetFramework").FirstOrDefault();
                var tfsElement = root.Descendants("TargetFrameworks").FirstOrDefault();
                scan.TargetFrameworks = tfsElement?.Value ?? tfElement?.Value ?? string.Empty;

                // Extract PackageReferences
                foreach (var pr in root.Descendants("PackageReference"))
                {
                    string pkgId = pr.Attribute("Include")?.Value ?? pr.Attribute("Update")?.Value ?? string.Empty;
                    string version = pr.Attribute("Version")?.Value ?? pr.Element("Version")?.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(pkgId))
                    {
                        scan.PackageReferences.Add(new PackageReferenceInfo
                        {
                            ProjectPath = filePath,
                            PackageId = pkgId,
                            Version = version
                        });
                    }
                }

                // Extract ProjectReferences
                foreach (var pr in root.Descendants("ProjectReference"))
                {
                    string target = pr.Attribute("Include")?.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(target))
                    {
                        scan.ProjectReferences.Add(new ProjectReferenceInfo
                        {
                            ProjectPath = filePath,
                            TargetProject = target
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing project {filePath}: {ex.Message}");
            }

            return scan;
        }

        public static List<SolutionScanResult> ScanAllSolutions(string? rootPath = null)
        {
            rootPath ??= ResolveWorkspaceRoot();
            var results = new List<SolutionScanResult>();

            string[] slnxFiles = Directory.GetFiles(rootPath, "*.slnx", SearchOption.AllDirectories);
            string[] slnFiles = Directory.GetFiles(rootPath, "*.sln", SearchOption.AllDirectories);

            foreach (var file in slnxFiles.Concat(slnFiles))
            {
                if (file.Contains(@"\obj\") || file.Contains(@"\bin\"))
                    continue;

                results.Add(ParseSolution(file));
            }

            return results;
        }

        public static SolutionScanResult ParseSolution(string solutionPath)
        {
            var result = new SolutionScanResult { SolutionPath = solutionPath };
            string solDir = Path.GetDirectoryName(solutionPath) ?? string.Empty;

            if (solutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var doc = XDocument.Load(solutionPath);
                    foreach (var projElem in doc.Descendants("Project"))
                    {
                        string pathAttr = projElem.Attribute("Path")?.Value ?? string.Empty;
                        if (!string.IsNullOrEmpty(pathAttr))
                        {
                            result.DeclaredProjectPaths.Add(pathAttr);
                            string normalized = pathAttr.Replace('/', Path.DirectorySeparatorChar)
                                                        .Replace('\\', Path.DirectorySeparatorChar);
                            string fullPath = Path.GetFullPath(Path.Combine(solDir, normalized));
                            if (!File.Exists(fullPath))
                            {
                                result.MissingProjectPaths.Add(pathAttr);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.MissingProjectPaths.Add($"[PARSE_ERROR: {ex.Message}]");
                }
            }
            else if (solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var lines = File.ReadAllLines(solutionPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Project(", StringComparison.OrdinalIgnoreCase))
                        {
                            // Format: Project("{FAE04EC0-...}") = "Name", "Relative\Path.csproj", "{GUID}"
                            var parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                string relPath = parts[1].Trim(' ', '"', '\t');
                                if (relPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                                    relPath.EndsWith(".vcxproj", StringComparison.OrdinalIgnoreCase))
                                {
                                    result.DeclaredProjectPaths.Add(relPath);
                                    string fullPath = Path.GetFullPath(Path.Combine(solDir, relPath));
                                    if (!File.Exists(fullPath))
                                    {
                                        result.MissingProjectPaths.Add(relPath);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.MissingProjectPaths.Add($"[PARSE_ERROR: {ex.Message}]");
                }
            }

            return result;
        }
    }
}
