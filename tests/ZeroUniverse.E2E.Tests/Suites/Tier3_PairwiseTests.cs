using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ZeroPrimitives.Buffers;
using ZeroPrimitives.Memory;
using ZeroUniverse.E2E.Tests.Framework;
using ZeroUniverse.E2E.Tests.Oracles;

namespace ZeroUniverse.E2E.Tests.Suites
{
    public class Tier3_PairwiseTests
    {
        // ==========================================
        // PAIRWISE 1: ZeroUI + ZeroPrimitives + ZeroText Interaction
        // ==========================================

        [Fact]
        public void Test_Tier3_01_ZeroUI_Pairwise_ZeroPrimitives_And_ZeroText_Wiring()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string projPath = Path.Combine(root, @"ZeroPlatform\ZeroUI\src\ZeroUI.Core\ZeroUI.Core.csproj");
            Assert.True(File.Exists(projPath), "ZeroUI.Core.csproj must exist");

            var scan = EcosystemScanner.ParseProject(projPath, root);

            // Must reference ZeroPrimitives.Core
            bool hasPrim = scan.ReferencesPackage("ZeroPrimitives.Core", out string primVer);
            Assert.True(hasPrim, "ZeroUI.Core must declare reference to ZeroPrimitives.Core");

            // Must have SearchFilterEngine calling VietnameseSearchNormalizer
            string engineFile = Path.Combine(root, @"ZeroPlatform\ZeroUI\src\ZeroUI.Core\Data\SearchFilterEngine.cs");
            Assert.True(File.Exists(engineFile));
            string code = File.ReadAllText(engineFile);

            Assert.Contains("VietnameseSearchNormalizer.ToSearchKeyword", code);
        }

        // ==========================================
        // PAIRWISE 2: ZVision + ZeroPrimitives + ZeroText Interaction
        // ==========================================

        [Fact]
        public void Test_Tier3_02_ZVision_Pairwise_ZeroPrimitives_And_ZeroText_Wiring()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string projPath = Path.Combine(root, @"ZeroApps\ZVision\ZVision.Shared\ZVision.Shared.csproj");
            Assert.True(File.Exists(projPath), "ZVision.Shared.csproj must exist");

            var scan = EcosystemScanner.ParseProject(projPath, root);

            // Must reference ZeroPrimitives.Core
            bool hasPrim = scan.ReferencesPackage("ZeroPrimitives.Core", out string primVer);
            Assert.True(hasPrim, "ZVision.Shared must declare reference to ZeroPrimitives.Core");

            // Must have KeywordHelper calling VietnameseSearchNormalizer
            string helperFile = Path.Combine(root, @"ZeroApps\ZVision\ZVision.Shared\KeywordHelper.cs");
            Assert.True(File.Exists(helperFile));
            string code = File.ReadAllText(helperFile);

            Assert.Contains("VietnameseSearchNormalizer.ToSearchKeyword", code);
        }

        // ==========================================
        // PAIRWISE 3: Multi-Targeting Matrix (net8.0 x net462 x netstandard2.0)
        // ==========================================

        [Fact]
        public void Test_Tier3_03_MultiTargeting_Matrix_Net8_Net462_NetStandard20()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            // Identify all Tier-0 and Tier-1 foundational libraries
            var crossTargetLibs = allProjects.Where(p =>
                p.TargetFrameworks.Contains("netstandard2.0") &&
                p.TargetFrameworks.Contains("net462") &&
                p.TargetFrameworks.Contains("net8.0")).ToList();

            // At least ZeroPrimitives.Core and other cross-platform platform libraries must support all three targets
            Assert.True(crossTargetLibs.Count >= 5,
                $"Expected at least 5 multi-targeted platform libraries, found {crossTargetLibs.Count}");
        }

        // ==========================================
        // PAIRWISE 4: ZeroVideo + SlabAllocator + NativeMemoryBlock Lifetimes
        // ==========================================

        [Fact]
        public void Test_Tier3_04_ZeroVideo_Pairwise_SlabAllocator_And_NativeMemoryBlock_Lifetimes()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string poolFile = Path.Combine(root, @"ZeroPlatform\ZeroVideo\src\ZeroVideo\Core\VideoFramePool.cs");

            Assert.True(File.Exists(poolFile), "VideoFramePool.cs must exist");
            string code = File.ReadAllText(poolFile);

            // VideoFramePool consumes SlabAllocator and produces NativeVideoFrameBuffer
            Assert.Contains("SlabAllocator", code);
            Assert.Contains("NativeMemoryBlock", code);
        }

        // ==========================================
        // PAIRWISE 5: NuGet Local Feed Pairwise Resolution
        // ==========================================

        [Fact]
        public void Test_Tier3_05_NuGet_LocalFeed_PairwiseResolution_Packages()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string primPackDir = Path.Combine(root, @"ZeroPlatform\ZeroPrimitives\dist\packages");

            // ZeroPrimitives.Core.1.4.0.nupkg should exist in packages dir
            Assert.True(Directory.Exists(primPackDir), "ZeroPrimitives dist/packages directory must exist");

            string[] nupkgs = Directory.GetFiles(primPackDir, "*.nupkg");
            Assert.NotEmpty(nupkgs);

            var primNupkg = nupkgs.FirstOrDefault(f => f.Contains("ZeroPrimitives.Core.1.4.0"));
            Assert.NotNull(primNupkg);
        }

        // ==========================================
        // PAIRWISE 6: SearchFilterEngine Simulation with Diacritic Normalizer
        // ==========================================

        [Fact]
        public void Test_Tier3_06_SearchFilterEngine_PairwiseWith_DiacriticNormalizer()
        {
            // Simulate SearchFilterEngine's diacritic-insensitive search logic
            var items = new List<string>
            {
                "Máy Hàn Điện Tử Công Nghiệp Riland",
                "Máy Cắt Plasma CNC Siêu Chuẩn",
                "Biến Tần Động Cơ Ba Pha",
                "Cảm Biến Áp Suất Không Khí"
            };

            string searchToken = "may han";
            string normQuery = VietnameseTextOracle.ToSearchKeyword(searchToken);

            var matched = items.Where(item =>
            {
                string normItem = VietnameseTextOracle.ToSearchKeyword(item);
                return normItem.Contains(normQuery, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            Assert.Single(matched);
            Assert.Equal("Máy Hàn Điện Tử Công Nghiệp Riland", matched[0]);
        }
    }
}
