using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZeroPrimitives.Buffers;
using ZeroPrimitives.Memory;
using ZeroUniverse.E2E.Tests.Framework;
using ZeroUniverse.E2E.Tests.Oracles;

namespace ZeroUniverse.E2E.Tests.Suites
{
    public class Tier4_RealWorldScenarios
    {
        // ==========================================
        // SCENARIO 1: Comprehensive Ecosystem Dependency & Upgrade Audit
        // ==========================================

        [Fact]
        public void Scenario1_CompleteEcosystemDependencyAndUpgradeAudit()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allProjects = EcosystemScanner.ScanAllProjects(root);

            Assert.True(allProjects.Count >= 100, $"Total projects scanned: {allProjects.Count}");

            var consumers = allProjects.Where(p =>
                p.ReferencesPackage("ZeroPrimitives.Core", out _)).ToList();

            var projectRefConsumers = allProjects.Where(p =>
                p.ReferencesProject("ZeroPrimitives.Core")).ToList();

            // Total consumers must match survey
            Assert.True(consumers.Count >= 28, $"Found {consumers.Count} PackageReference consumers");
            Assert.True(projectRefConsumers.Count >= 2, $"Found {projectRefConsumers.Count} ProjectReference consumers");

            // Verify local feeds mapped in nuget.config
            var sources = NugetConfigInspector.InspectAllConfigs(root);
            Assert.NotEmpty(sources);
        }

        // ==========================================
        // SCENARIO 2: Full E-Invoice Accounting & Regulatory Pipeline
        // ==========================================

        [Fact]
        public void Scenario2_FullEInvoiceAccountingAndRegulatoryPipeline()
        {
            // Step 1: Validate Enterprise MST
            string enterpriseMst = "0100109106";
            Assert.True(MasterDataValidatorOracle.IsValidMst(enterpriseMst), "Enterprise MST must be valid Modulo 11");

            // Step 2: Validate Branch MST
            string branchMst = "0100109106-001";
            Assert.True(MasterDataValidatorOracle.IsValidMst(branchMst), "Branch MST must be valid");

            // Step 3: Decode Legal Representative Citizen Identity Card (CCCD)
            string repCccd = "001085006789";
            bool cccdValid = MasterDataValidatorOracle.IsValidCccd(repCccd, out int birthYear, out bool isMale, out string? province);
            Assert.True(cccdValid);
            Assert.Equal("Hà Nội", province);
            Assert.Equal(1985, birthYear);
            Assert.True(isMale);

            // Step 4: Validate and normalize phone contact
            string rawPhone = "(090) 123-4567";
            Assert.True(MasterDataValidatorOracle.IsValidPhone(rawPhone));
            string? normalizedPhone = MasterDataValidatorOracle.NormalizePhone(rawPhone, international: true);
            Assert.Equal("+84901234567", normalizedPhone);

            // Step 5: Generate Statutory Currency Words (Circular 200)
            decimal totalAmount = 35680000m;
            string currencyWords = CurrencyWordsOracle.ToVnCurrencyWords(totalAmount);
            Assert.Equal("Ba mươi lăm triệu sáu trăm tám mươi nghìn đồng chẵn", currencyWords);

            // Step 6: Generate Search Keyword and Slug for Invoice Indexing
            string invoiceTitle = "Hóa Đơn Giá Trị Gia Tăng - Thiết Bị Tự Động Hóa Zero (Lô #99)";
            string keyword = VietnameseTextOracle.ToSearchKeyword(invoiceTitle);
            string slug = VietnameseTextOracle.ToSlug(invoiceTitle);

            Assert.Equal("hoa don gia tri gia tang thiet bi tu dong hoa zero lo 99", keyword);
            Assert.Equal("hoa-don-gia-tri-gia-tang-thiet-bi-tu-dong-hoa-zero-lo-99", slug);
        }

        // ==========================================
        // SCENARIO 3: High-Throughput Memory Allocation & Disposal Stress
        // ==========================================

        [Fact]
        public void Scenario3_HighThroughputMemoryAllocationAndDisposalStress()
        {
            const int threads = 4;
            const int iterationsPerThread = 2500;

            Parallel.For(0, threads, _ =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    // Scope 1: ArrayPoolRentScope with explicit double-dispose
                    using (var scope = ArrayPoolRentScope<byte>.Rent(256))
                    {
                        var span = scope.Span;
                        span[0] = 0xAA;
                        span[255] = 0xBB;
                        Assert.Equal(0xAA, span[0]);
                        Assert.Equal(0xBB, span[255]);

                        // Explicit inner dispose (testing double-dispose safety on exit)
                        scope.Dispose();
                    }

                    // Scope 2: NativeMemoryBlock allocation and disposal
                    using (var block = NativeMemoryBlock.Allocate(128))
                    {
                        var span = block.Span;
                        span[0] = 0x11;
                        span[127] = 0x22;
                        Assert.Equal(0x11, span[0]);
                        Assert.Equal(0x22, span[127]);
                    }
                }
            });
        }

        // ==========================================
        // SCENARIO 4: Master Solution Topology & Build Sanity Check
        // ==========================================

        [Fact]
        public void Scenario4_MasterSolutionTopologyAndBuildSanityCheck()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            string slnxPath = Path.Combine(root, @"ZeroPlatform\ZeroPlatform.slnx");
            Assert.True(File.Exists(slnxPath));

            var result = EcosystemScanner.ParseSolution(slnxPath);

            // Audit the master solution projects
            Assert.True(result.DeclaredProjectPaths.Count >= 70,
                $"Master solution should contain >= 70 projects, found {result.DeclaredProjectPaths.Count}");

            // Verify that all declared paths are inspected
            int existingProjects = result.DeclaredProjectPaths.Count - result.MissingProjectPaths.Count;
            Assert.True(existingProjects >= 70,
                $"Expected at least 70 existing project files in master solution, found {existingProjects}");
        }

        // ==========================================
        // SCENARIO 5: Subsystem Solutions Audit & Readiness
        // ==========================================

        [Fact]
        public void Scenario5_SubsystemSolutionsAuditAndReadiness()
        {
            string root = EcosystemScanner.ResolveWorkspaceRoot();
            var allSolutions = EcosystemScanner.ScanAllSolutions(root);

            // Verify key subsystem solutions exist and are valid:
            string[] coreSubsystems = new[]
            {
                "ZeroPrimitives.slnx",
                "ZeroConcurrency.slnx",
                "ZeroData.slnx",
                "ZeroSecurity.slnx",
                "ZeroSystem.slnx",
                "ZeroUI.slnx",
                "ZVision.slnx"
            };

            foreach (var sub in coreSubsystems)
            {
                var match = allSolutions.FirstOrDefault(s =>
                    string.Equals(s.SolutionName, sub, StringComparison.OrdinalIgnoreCase));

                Assert.NotNull(match);
                Assert.True(match.IsValid, $"Subsystem solution {sub} has missing project files: {string.Join(", ", match.MissingProjectPaths)}");
            }
        }
    }
}
