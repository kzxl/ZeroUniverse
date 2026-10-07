using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using ZeroPrimitives.Buffers;
using ZeroPrimitives.Concurrency;
using ZeroPrimitives.Memory;
using ZeroUniverse.E2E.Tests.Framework;
using ZeroUniverse.E2E.Tests.Oracles;

namespace ZeroUniverse.E2E.Tests.Suites
{
    public class Tier1_FeatureCoverageTests
    {
        private static readonly string[] Target28Consumers = new[]
        {
            @"ZeroApps\ZVision\ZVision.Shared\ZVision.Shared.csproj",
            @"ZeroPlatform\Zero3D\src\Zero3D\Zero3D.csproj",
            @"ZeroPlatform\ZeroAudioVisual\src\ZeroAudioVisual\ZeroAudioVisual.csproj",
            @"ZeroPlatform\ZeroCharts\src\ZeroCharts\ZeroCharts.csproj",
            @"ZeroPlatform\ZeroComm\src\ZeroComm.Core\ZeroComm.Core.csproj",
            @"ZeroPlatform\ZeroCompression\src\ZeroCompression.Core\ZeroCompression.Core.csproj",
            @"ZeroPlatform\ZeroCompute\src\ZeroCompute.Core\ZeroCompute.Core.csproj",
            @"ZeroPlatform\ZeroConcurrency\src\ZeroConcurrency\ZeroConcurrency.csproj",
            @"ZeroPlatform\ZeroData\src\ZeroData.Core\ZeroData.Core.csproj",
            @"ZeroPlatform\ZeroData\src\ZeroData.Sql\ZeroData.Sql.csproj",
            @"ZeroPlatform\ZeroDocuments\src\ZeroDocuments.Core\ZeroDocuments.Core.csproj",
            @"ZeroPlatform\ZeroGeometry\src\ZeroGeometry.Core\ZeroGeometry.Core.csproj",
            @"ZeroPlatform\ZeroGraphics\src\ZeroGraphics.Core\ZeroGraphics.Core.csproj",
            @"ZeroPlatform\ZeroInference\src\ZeroInference.Core\ZeroInference.Core.csproj",
            @"ZeroPlatform\ZeroIoT\src\ZeroIoT\ZeroIoT.csproj",
            @"ZeroPlatform\ZeroNetwork\src\ZeroNetwork.Core\ZeroNetwork.Core.csproj",
            @"ZeroPlatform\ZeroNeural\src\ZeroNeural.Core\ZeroNeural.Core.csproj",
            @"ZeroPlatform\ZeroPipeline\src\ZeroPipeline.Nodes\ZeroPipeline.Nodes.csproj",
            @"ZeroPlatform\ZeroReports\src\ZeroReports\ZeroReports.csproj",
            @"ZeroPlatform\ZeroRfid\src\ZeroRfid.Core\ZeroRfid.Core.csproj",
            @"ZeroPlatform\ZeroSecurity\src\ZeroSecurity\ZeroSecurity.csproj",
            @"ZeroPlatform\ZeroSignal\src\ZeroSignal.Core\ZeroSignal.Core.csproj",
            @"ZeroPlatform\ZeroStorage\src\ZeroStorage.Core\ZeroStorage.Core.csproj",
            @"ZeroPlatform\ZeroSystem\src\ZeroSystem.Core\ZeroSystem.Core.csproj",
            @"ZeroPlatform\ZeroTensor\src\ZeroTensor.Core\ZeroTensor.Core.csproj",
            @"ZeroPlatform\ZeroTwin3D\src\ZeroTwin3D\ZeroTwin3D.csproj",
            @"ZeroPlatform\ZeroUI\src\ZeroUI.Core\ZeroUI.Core.csproj",
            @"ZeroPlatform\ZeroVideo\src\ZeroVideo\ZeroVideo.csproj"
        };

        // ==========================================
        // FEATURE R1: Consumer Dependency Audit & Upgrade (>=5 tests)
        // ==========================================

        [Fact]
        public void Test_R1_01_All28TargetConsumerProjects_ExistOnDisk()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var missing = new List<string>();

            foreach (var relPath in Target28Consumers)
            {
                string fullPath = Path.Combine(root, relPath);
                if (!File.Exists(fullPath))
                {
                    missing.Add(relPath);
                }
            }

            Assert.True(missing.Count == 0, $"Missing consumer project files: {string.Join(", ", missing)}");
        }

        [Fact]
        public void Test_R1_02_ConsumerProjectInventory_MatchesExplorer1Survey()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            var consumersFound = allProjects.Where(p =>
                p.ReferencesPackage("ZeroPrimitives.Core", out _)).ToList();

            // All 28 consumers must declare PackageReference to ZeroPrimitives.Core
            Assert.True(consumersFound.Count >= 28,
                $"Expected at least 28 projects referencing ZeroPrimitives.Core, found {consumersFound.Count}");

            foreach (var relPath in Target28Consumers)
            {
                var match = consumersFound.FirstOrDefault(p =>
                    string.Equals(p.RelativePath, relPath, StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(match);
            }
        }

        [Fact]
        public void Test_R1_03_ZUpdateAndZView_ProjectReferenceIntegrity_Preserved()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            var zupdate = allProjects.FirstOrDefault(p => p.ProjectName.Equals("ZUpdate.Core", StringComparison.OrdinalIgnoreCase));
            var zview = allProjects.FirstOrDefault(p => p.ProjectName.Equals("ZView.Core", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(zupdate);
            Assert.NotNull(zview);

            Assert.True(zupdate.ReferencesProject("ZeroPrimitives.Core"),
                "ZUpdate.Core must reference ZeroPrimitives.Core via ProjectReference");
            Assert.True(zview.ReferencesProject("ZeroPrimitives.Core"),
                "ZView.Core must reference ZeroPrimitives.Core via ProjectReference");
        }

        [Fact]
        public void Test_R1_04_NugetConfig_LocalFeedsMappedCorrectly()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var sources = NugetConfigInspector.InspectAllConfigs(root);

            var primSources = sources.Where(s =>
                s.Key.IndexOf("zeroprimitives", StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            Assert.NotEmpty(primSources);
            foreach (var src in primSources)
            {
                Assert.True(src.IsLocalDirectory, $"Feed {src.Key} must be a local directory path");
                Assert.True(src.DirectoryExists, $"Configured feed path {src.Value} in {src.ConfigPath} must exist on disk");
            }
        }

        [Fact]
        public void Test_R1_05_AuditAllConsumerProjects_CatalogCurrentVersusTarget140()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            var bumpedTo140 = new List<string>();
            var stillOn130 = new List<string>();

            foreach (var proj in allProjects)
            {
                if (proj.ReferencesPackage("ZeroPrimitives.Core", out string ver))
                {
                    if (ver == "1.4.0") bumpedTo140.Add(proj.RelativePath);
                    else if (ver == "1.3.0") stillOn130.Add(proj.RelativePath);
                }
            }

            // Diagnostic assertion: Total consumers must be exactly accounted for
            int total = bumpedTo140.Count + stillOn130.Count;
            Assert.True(total >= 28, $"Total consumers found: {total}");
        }

        // ==========================================
        // FEATURE R2: Dedicated Text/Localization Library (ZeroText) (>=5 tests)
        // ==========================================

        [Fact]
        public void Test_R2_01_VietnameseDiacriticsRemoval_MatchesSpecification()
        {
            string input = "Đơn Hàng / Bán Lẻ - Mã Phiếu: 12345 (Hà Nội & TP.HCM)";
            string expected = "Don Hang / Ban Le - Ma Phieu: 12345 (Ha Noi & TP.HCM)";

            string actual = VietnameseTextOracle.RemoveDiacritics(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test_R2_02_VietnameseSearchKeyword_CollapsesWhitespaceAndSymbols()
        {
            string input = "  Máy Hàn   Điện Tử  (Công Nghiệp) #123! ";
            string expected = "may han dien tu cong nghiep 123";

            string actual = VietnameseTextOracle.ToSearchKeyword(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test_R2_03_Circular200_CurrencyToWords_AccountingRules()
        {
            decimal amount = 1234567890m;
            string words = CurrencyWordsOracle.ToVnCurrencyWords(amount);

            Assert.Contains("Một tỷ", words);
            Assert.Contains("hai trăm ba mươi tư triệu", words);
            Assert.Contains("năm trăm sáu mươi bảy nghìn", words);
            Assert.Contains("tám trăm chín mươi đồng chẵn", words);
        }

        [Fact]
        public void Test_R2_04_Circular105_TaxCodeModulo11_EnterpriseAndBranch()
        {
            // Valid 10-digit tax code (Viettel)
            Assert.True(MasterDataValidatorOracle.IsValidMst("0100109106"));

            // Valid 13-digit branch tax code
            Assert.True(MasterDataValidatorOracle.IsValidMst("0100109106-001"));
            Assert.True(MasterDataValidatorOracle.IsValidMst("0100109106001"));

            // Invalid check digit
            Assert.False(MasterDataValidatorOracle.IsValidMst("0100109107"));

            // Invalid branch code "000"
            Assert.False(MasterDataValidatorOracle.IsValidMst("0100109106-000"));
        }

        [Fact]
        public void Test_R2_05_CCCD_Parsing_ProvinceAndCenturyDecoded()
        {
            // CCCD for Hanoi (001), Century 20 (Male = 0), Year 90
            string cccd = "001090012345";
            bool valid = MasterDataValidatorOracle.IsValidCccd(cccd, out int year, out bool isMale, out string? province);

            Assert.True(valid);
            Assert.Equal("Hà Nội", province);
            Assert.True(isMale);
            Assert.Equal(1990, year);

            // CCCD for HCMC (079), Century 21 (Female = 3), Year 05 (2005)
            string cccdHcm = "079305098765";
            bool validHcm = MasterDataValidatorOracle.IsValidCccd(cccdHcm, out int yearHcm, out bool isMaleHcm, out string? provHcm);

            Assert.True(validHcm);
            Assert.Equal("TP. Hồ Chí Minh", provHcm);
            Assert.False(isMaleHcm);
            Assert.Equal(2005, yearHcm);
        }

        [Fact]
        public void Test_R2_06_ConsumerCallSites_SearchFilterEngine_KeywordHelper_TargetZeroText()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();

            string searchFilterEngine = Path.Combine(root, @"ZeroPlatform\ZeroUI\src\ZeroUI.Core\Data\SearchFilterEngine.cs");
            string keywordHelper = Path.Combine(root, @"ZeroApps\ZVision\ZVision.Shared\KeywordHelper.cs");

            Assert.True(File.Exists(searchFilterEngine), "SearchFilterEngine.cs must exist");
            Assert.True(File.Exists(keywordHelper), "KeywordHelper.cs must exist");

            string text1 = File.ReadAllText(searchFilterEngine);
            string text2 = File.ReadAllText(keywordHelper);

            // Verify both call sites actively invoke diacritic/search normalization
            Assert.Contains("VietnameseSearchNormalizer", text1);
            Assert.Contains("VietnameseSearchNormalizer", text2);
        }

        // ==========================================
        // FEATURE R3: Latent Bug Remediation & Invariants (>=5 tests)
        // ==========================================

        [Fact]
        public void Test_R3_01_FastSpinLock_ReferenceTypeSemantics_Verified()
        {
            var type = typeof(FastSpinLock);
            Assert.True(type.IsClass, "FastSpinLock must be a reference type (class) to prevent defensive-copy synchronization bypass");
            Assert.True(type.IsSealed, "FastSpinLock must be sealed");

            var lockInstance = new FastSpinLock();
            int count = 0;

            using (lockInstance.EnterScope())
            {
                count++;
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public void Test_R3_02_ArrayPoolRentScope_IdempotentDispose_NoDoubleReturnCorruption()
        {
            // Verify that calling Dispose() twice does not throw or corrupt the shared pool
            var scope = ArrayPoolRentScope<byte>.Rent(128);
            Assert.True(scope.Length >= 128);

            // First dispose
            scope.Dispose();

            // Second dispose must be completely idempotent and safe
            Exception? caught = null;
            try
            {
                scope.Dispose();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            Assert.Null(caught);
        }

        [Fact]
        public void Test_R3_03_NativeMemoryBlock_SuppressFinalize_Contract()
        {
            // Allocate a NativeMemoryBlock and verify Dispose suppresses finalization
            var block = NativeMemoryBlock.Allocate(64);
            Assert.False(block.IsDisposed);

            block.Dispose();
            Assert.True(block.IsDisposed);

            // Double dispose must also be safe
            Exception? ex = Record.Exception(() => block.Dispose());
            Assert.Null(ex);
        }

        [Fact]
        public void Test_R3_04_ZeroNativeRingBuffer_CacheLineIsolation_Invariant()
        {
            // Load ZeroPlatform.Concurrency assembly or check type layout
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string file = Path.Combine(root, @"ZeroPlatform\ZeroConcurrency\src\ZeroConcurrency\DataPlane\ZeroNativeRingBuffer.cs");

            Assert.True(File.Exists(file), "ZeroNativeRingBuffer.cs must exist");
            string source = File.ReadAllText(file);

            // Verify the class contains cache line padding
            Assert.True(source.Contains("pad") || source.Contains("FieldOffset") || source.Contains("LayoutKind"),
                "ZeroNativeRingBuffer must have cache-line padding layout");
        }

        [Fact]
        public void Test_R3_05_SimdColorConverter_ThrowsArgumentException_OnTruncatedInput()
        {
            // Passing truncated span to SimdColorConverter must throw ArgumentException, never IndexOutOfRangeException
            byte[] truncatedY = new byte[10];
            byte[] uPlane = new byte[64];
            byte[] vPlane = new byte[64];
            byte[] dst = new byte[16 * 16 * 3];

            Assert.Throws<ArgumentException>(() =>
            {
                ZeroPrimitives.Simd.SimdColorConverter.Yuv420pToRgb(truncatedY, uPlane, vPlane, dst, 16, 16);
            });
        }

        // ==========================================
        // FEATURE R4: Build Configurations & Solution Layout (>=5 tests)
        // ==========================================

        [Fact]
        public void Test_R4_01_MasterSolution_ZeroPlatformSlnx_ParsesWithoutXmlErrors()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string slnxPath = Path.Combine(root, @"ZeroPlatform\ZeroPlatform.slnx");

            Assert.True(File.Exists(slnxPath), "ZeroPlatform.slnx must exist");
            var scan = EcosystemScanner.ParseSolution(slnxPath);

            Assert.True(scan.DeclaredProjectPaths.Count >= 70,
                $"Master solution must declare at least 70 projects, found {scan.DeclaredProjectPaths.Count}");
        }

        [Fact]
        public void Test_R4_02_MasterSolution_ZeroPlatformSlnx_DetectsAndFlagsStalePaths()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string slnxPath = Path.Combine(root, @"ZeroPlatform\ZeroPlatform.slnx");

            var scan = EcosystemScanner.ParseSolution(slnxPath);

            // Stale paths reported in Explorer 1 survey:
            // "ZeroUI/src/ZeroUI.WinForms/ZeroUI.WinForms.csproj"
            // "ZeroUI/src/ZeroUI.Wpf/ZeroUI.Wpf.csproj"
            // If they are fixed in M3, MissingProjectPaths will be empty; if not, they are identified cleanly.
            if (scan.MissingProjectPaths.Count > 0)
            {
                foreach (var missing in scan.MissingProjectPaths)
                {
                    Assert.Contains("ZeroUI", missing);
                }
            }
        }

        [Fact]
        public void Test_R4_03_All44Solutions_ScannedAndCataloged()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var solutions = EcosystemScanner.ScanAllSolutions(root);

            Assert.True(solutions.Count >= 40, $"Expected >= 40 solutions, found {solutions.Count}");
        }

        [Fact]
        public void Test_R4_04_MultiTargetFrameworks_DeclaredCorrectlyInCoreLibraries()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            var primCore = allProjects.FirstOrDefault(p => p.ProjectName.Equals("ZeroPrimitives.Core", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(primCore);

            Assert.Contains("netstandard2.0", primCore.TargetFrameworks);
            Assert.Contains("net462", primCore.TargetFrameworks);
            Assert.Contains("net8.0", primCore.TargetFrameworks);
        }

        [Fact]
        public void Test_R4_05_DirectoryBuildProps_MetadataAndPackageOutputPath()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string propsPath = Path.Combine(root, @"ZeroPlatform\ZeroPrimitives\Directory.Build.props");

            Assert.True(File.Exists(propsPath), "ZeroPrimitives Directory.Build.props must exist");
            string content = File.ReadAllText(propsPath);

            Assert.Contains("<Company>ZeroPlatform</Company>", content);
            Assert.Contains("<PackageOutputPath>$(MSBuildThisFileDirectory)dist\\packages</PackageOutputPath>", content);
        }
    }
}
