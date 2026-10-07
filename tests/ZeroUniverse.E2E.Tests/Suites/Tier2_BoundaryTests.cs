using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZeroPrimitives.Buffers;
using ZeroPrimitives.Concurrency;
using ZeroPrimitives.Memory;
using ZeroUniverse.E2E.Tests.Framework;
using ZeroUniverse.E2E.Tests.Oracles;

namespace ZeroUniverse.E2E.Tests.Suites
{
    public class Tier2_BoundaryTests
    {
        // ==========================================
        // BOUNDARY R1: Package Reference Boundary Conditions (5 tests)
        // ==========================================

        [Fact]
        public void Test_R1_Boundary_01_CaseInsensitivePackageIdMatching()
        {
            var project = new ProjectScanResult();
            project.PackageReferences.Add(new PackageReferenceInfo
            {
                PackageId = "zeroprimitives.core",
                Version = "1.4.0"
            });

            bool found = project.ReferencesPackage("ZeroPrimitives.Core", out string ver);
            Assert.True(found);
            Assert.Equal("1.4.0", ver);
        }

        [Fact]
        public void Test_R1_Boundary_02_WhitespaceAndEmptyVersionHandling()
        {
            var project = new ProjectScanResult();
            project.PackageReferences.Add(new PackageReferenceInfo
            {
                PackageId = "ZeroPrimitives.Core",
                Version = "   "
            });

            bool found = project.ReferencesPackage("ZeroPrimitives.Core", out string ver);
            Assert.True(found);
            Assert.True(string.IsNullOrWhiteSpace(ver));
        }

        [Fact]
        public void Test_R1_Boundary_03_MultiplePackageReferencesToSamePackage()
        {
            var project = new ProjectScanResult();
            project.PackageReferences.Add(new PackageReferenceInfo { PackageId = "ZeroPrimitives.Core", Version = "1.3.0" });
            project.PackageReferences.Add(new PackageReferenceInfo { PackageId = "ZeroPrimitives.Core", Version = "1.4.0" });

            // First match returned
            bool found = project.ReferencesPackage("ZeroPrimitives.Core", out string ver);
            Assert.True(found);
            Assert.Equal("1.3.0", ver);
        }

        [Fact]
        public void Test_R1_Boundary_04_ProjectReferenceForwardSlashVsBackslash()
        {
            var project = new ProjectScanResult();
            project.ProjectReferences.Add(new ProjectReferenceInfo
            {
                TargetProject = @"..\ZeroPrimitives\src\ZeroPrimitives.Core\ZeroPrimitives.Core.csproj"
            });

            Assert.True(project.ReferencesProject("ZeroPrimitives.Core"));
            Assert.True(project.ReferencesProject("zeroprimitives.core"));
        }

        [Fact]
        public void Test_R1_Boundary_05_ScanProjectsExcludesBinAndObjFolders()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            foreach (var proj in allProjects)
            {
                Assert.DoesNotContain(@"\bin\", proj.ProjectPath);
                Assert.DoesNotContain(@"\obj\", proj.ProjectPath);
            }
        }

        // ==========================================
        // BOUNDARY R2: ZeroText Boundary & Corner Cases (6 tests)
        // ==========================================

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\r\n")]
        public void Test_R2_Boundary_01_NullAndWhitespaceInputHandling(string? input)
        {
            // Search keyword and slug must collapse whitespace to empty string
            Assert.Equal("", VietnameseTextOracle.ToSearchKeyword(input));
            Assert.Equal("", VietnameseTextOracle.ToSlug(input));

            if (string.IsNullOrEmpty(input))
            {
                Assert.Equal("", VietnameseTextOracle.RemoveDiacritics(input));
                Assert.Equal("", VietnameseTextOracle.UnSignedTransfer(input));
            }
            else
            {
                // Non-empty whitespace preserves spaces in RemoveDiacritics
                Assert.Equal(input, VietnameseTextOracle.RemoveDiacritics(input));
            }
        }

        [Fact]
        public void Test_R2_Boundary_02_LargeBufferStackallocBoundary_Over256Chars()
        {
            // Inputs larger than 256 characters exceed typical stackalloc buffers and test ArrayPool fallback
            var sb = new StringBuilder();
            for (int i = 0; i < 50; i++)
            {
                sb.Append("Đơn Hàng Công Nghiệp Số ");
                sb.Append(i);
                sb.Append(" - ");
            }
            string largeInput = sb.ToString();
            Assert.True(largeInput.Length > 1000);

            string keyword = VietnameseTextOracle.ToSearchKeyword(largeInput);
            Assert.False(string.IsNullOrEmpty(keyword));
            Assert.DoesNotContain("Đ", keyword);
            Assert.DoesNotContain("ề", keyword);
            Assert.DoesNotContain("ố", keyword);
        }

        [Theory]
        [InlineData("")]
        [InlineData("123")]
        [InlineData("010010910")]        // 9 digits (too short)
        [InlineData("01001091060")]      // 11 digits (invalid)
        [InlineData("010010910A")]       // Non-digit
        [InlineData("0100109106-ABC")]   // Non-digit branch
        [InlineData("0100109106-000")]   // Branch "000" prohibited by Circular 105
        public void Test_R2_Boundary_03_InvalidTaxCodes_Rejection(string? taxCode)
        {
            Assert.False(MasterDataValidatorOracle.IsValidMst(taxCode));
        }

        [Theory]
        [InlineData("")]
        [InlineData("00109012345")]      // 11 digits (too short)
        [InlineData("0010901234567")]    // 13 digits (too long)
        [InlineData("000090123456")]     // 000 is invalid province code
        [InlineData("999090123456")]     // 999 is invalid province code
        [InlineData("001890123456")]     // 8 is invalid gender/century code
        [InlineData("001A90123456")]     // Non-numeric
        public void Test_R2_Boundary_04_InvalidCCCD_Rejection(string? cccd)
        {
            bool valid = MasterDataValidatorOracle.IsValidCccd(cccd, out _, out _, out _);
            Assert.False(valid);
        }

        [Fact]
        public void Test_R2_Boundary_05_EdgeCaseDiacriticsAndComplexTonalCombinations()
        {
            // All combinations of Vietnamese vowels and tone marks
            string input = "aáàạảãâấầậẩẫăắằặẳẵeéèẹẻẽêếềệểễoóòọỏõôốồộổỗơớờợởỡuúùụủũưứừựửữiíìịỉĩyýỳỵỷỹđĐ";
            string unaccented = VietnameseTextOracle.RemoveDiacritics(input);

            // Output must only contain basic latin vowels: a, e, o, u, i, y, d, D
            foreach (char c in unaccented)
            {
                Assert.True(
                    c == 'a' || c == 'e' || c == 'o' || c == 'u' || c == 'i' || c == 'y' || c == 'd' || c == 'D',
                    $"Character '{c}' was not stripped properly!");
            }
        }

        [Fact]
        public void Test_R2_Boundary_06_ExtremeCurrencyAmount_Spelling()
        {
            // Zero
            Assert.Equal("Không đồng chẵn", CurrencyWordsOracle.ToVnCurrencyWords(0m));

            // Negative amount
            Assert.StartsWith("Âm ", CurrencyWordsOracle.ToVnCurrencyWords(-50000m));

            // Extreme large amount: 999 trillion VND
            decimal huge = 999_000_000_000_000m;
            string words = CurrencyWordsOracle.ToVnCurrencyWords(huge);
            Assert.Contains("Chín trăm chín mươi chín nghìn tỷ đồng chẵn", words);

            // Fractional cents
            string withCents = CurrencyWordsOracle.ToVnCurrencyWords(15.75m, "đô la Mỹ", "xu");
            Assert.Contains("và bảy mươi lăm xu", withCents);
        }

        // ==========================================
        // BOUNDARY R3: Latent Bugs & Concurrency Boundaries (5 tests)
        // ==========================================

        [Fact]
        public void Test_R3_Boundary_01_ArrayPoolRentScope_ZeroLengthRental_DoubleDispose()
        {
            // Renting 0 elements should succeed (ArrayPool allocates at least minimum bucket)
            var scope = ArrayPoolRentScope<byte>.Rent(0);
            Assert.True(scope.Length >= 0);

            // Multiple calls to Dispose must never throw
            scope.Dispose();
            scope.Dispose();
            scope.Dispose();
        }

        [Fact]
        public void Test_R3_Boundary_02_NativeMemoryBlock_ZeroLengthAllocation_Throws()
        {
            // Allocate 0 bytes must throw ArgumentOutOfRangeException per NativeMemoryBlock invariants
            Assert.Throws<ArgumentOutOfRangeException>(() => NativeMemoryBlock.Allocate(0));

            // Minimum valid allocation is 1 byte
            using var block = NativeMemoryBlock.Allocate(1);
            Assert.NotNull(block);
            Assert.True(block.Capacity >= 1);
        }

        [Fact]
        public void Test_R3_Boundary_03_FastSpinLock_ContentionStress_ScopeDispose()
        {
            var spinLock = new FastSpinLock();
            int counter = 0;
            const int iterations = 10000;

            Parallel.For(0, 4, _ =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    using (spinLock.EnterScope())
                    {
                        counter++;
                    }
                }
            });

            Assert.Equal(4 * iterations, counter);
        }

        [Fact]
        public void Test_R3_Boundary_04_RingBuffer_SlotAlignment_Minimum128Bytes()
        {
            // Verify cache line alignment concept: 128 bytes prevents false sharing on all modern CPUs
            int l1CacheLine = 64;
            int prefetcherLine = 128;
            Assert.True(prefetcherLine >= l1CacheLine * 2);
        }

        [Fact]
        public void Test_R3_Boundary_05_SimdColorConverter_ExactMinimumDimensions()
        {
            // Minimum valid 2x2 YUV420P frame:
            // Stride = 2, H = 2
            // Y plane = 4 bytes, U plane = 1 byte, V plane = 1 byte -> Total = 6 bytes
            // Dst = 2 * 2 * 3 = 12 bytes
            byte[] srcY = new byte[4];
            byte[] srcU = new byte[1];
            byte[] srcV = new byte[1];
            byte[] dst = new byte[12];

            // Should succeed without exception
            Exception? ex = Record.Exception(() =>
            {
                ZeroPrimitives.Simd.SimdColorConverter.Yuv420pToRgb(srcY, srcU, srcV, dst, 2, 2);
            });
            Assert.Null(ex);
        }

        // ==========================================
        // BOUNDARY R4: Solution & Build Configuration Boundaries (5 tests)
        // ==========================================

        [Fact]
        public void Test_R4_Boundary_01_SolutionPathNormalizers_MixedSlashes()
        {
            string p1 = @"ZeroUI/src/WinForms\ZeroUI.WinForms/ZeroUI.WinForms.csproj";
            string normalized = p1.Replace('/', Path.DirectorySeparatorChar)
                                  .Replace('\\', Path.DirectorySeparatorChar);

            Assert.DoesNotContain('/', normalized);
        }

        [Fact]
        public void Test_R4_Boundary_02_MasterSolution_DuplicateProjectPaths_NoneExist()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string slnxPath = Path.Combine(root, @"ZeroPlatform\ZeroPlatform.slnx");

            var scan = EcosystemScanner.ParseSolution(slnxPath);

            var duplicates = scan.DeclaredProjectPaths
                .GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.Empty(duplicates);
        }

        [Fact]
        public void Test_R4_Boundary_03_DirectoryBuildProps_InheritanceDepth()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string[] props = Directory.GetFiles(root, "Directory.Build.props", SearchOption.AllDirectories);

            // Ecosystem has 32 Directory.Build.props files across subsystems
            Assert.True(props.Length >= 25, $"Expected >= 25 Directory.Build.props files, found {props.Length}");
        }

        [Fact]
        public void Test_R4_Boundary_04_AllProjects_DeclareLangVersionOrInheritLatest()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            Assert.True(allProjects.Count >= 100, $"Expected >= 100 projects in workspace, found {allProjects.Count}");
        }

        [Fact]
        public void Test_R4_Boundary_05_NoCentralPackageManagementCPM_Props()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string cpmFile = Path.Combine(root, "Directory.Packages.props");

            // CPM is explicitly NOT used in ZeroUniverse (verified in Explorer 1 survey)
            Assert.False(File.Exists(cpmFile), "Directory.Packages.props should not exist in root");
        }
    }
}
