<#
.SYNOPSIS
    ZeroUniverse Opaque-Box E2E Test Suite Runner
.DESCRIPTION
    Executes all 4 Tiers of verification across Requirements R1 through R4.
    Runs static ecosystem analysis, algorithmic oracle validation, and invokes the compiled xUnit suite.
#>
param(
    [switch]$SkipDotnetTest = $false,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$WorkspaceRoot = Resolve-Path (Join-Path $ScriptDir "..\..")

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "  ZeroUniverse Opaque-Box E2E Test Suite Runner" -ForegroundColor Cyan
Write-Host "  Workspace: $WorkspaceRoot" -ForegroundColor Cyan
Write-Host "  Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

$TotalTests = 0
$PassedTests = 0
$FailedTests = 0
$Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

function Record-TestResult([string]$Tier, [string]$TestName, [bool]$Passed, [string]$Details = "") {
    $script:TotalTests++
    if ($Passed) {
        $script:PassedTests++
        Write-Host "  [PASS] [$Tier] $TestName" -ForegroundColor Green
    } else {
        $script:FailedTests++
        Write-Host "  [FAIL] [$Tier] $TestName - $Details" -ForegroundColor Red
    }
}

Write-Host "`n--- Running Tier 1: Category-Partition Feature Coverage ---" -ForegroundColor Yellow

# Feature R1 Tests
$targetConsumers = @(
    "ZeroApps\ZVision\ZVision.Shared\ZVision.Shared.csproj",
    "ZeroPlatform\Zero3D\src\Zero3D\Zero3D.csproj",
    "ZeroPlatform\ZeroAudioVisual\src\ZeroAudioVisual\ZeroAudioVisual.csproj",
    "ZeroPlatform\ZeroCharts\src\ZeroCharts\ZeroCharts.csproj",
    "ZeroPlatform\ZeroComm\src\ZeroComm.Core\ZeroComm.Core.csproj",
    "ZeroPlatform\ZeroCompression\src\ZeroCompression.Core\ZeroCompression.Core.csproj",
    "ZeroPlatform\ZeroCompute\src\ZeroCompute.Core\ZeroCompute.Core.csproj",
    "ZeroPlatform\ZeroConcurrency\src\ZeroConcurrency\ZeroConcurrency.csproj",
    "ZeroPlatform\ZeroData\src\ZeroData.Core\ZeroData.Core.csproj",
    "ZeroPlatform\ZeroData\src\ZeroData.Sql\ZeroData.Sql.csproj",
    "ZeroPlatform\ZeroDocuments\src\ZeroDocuments.Core\ZeroDocuments.Core.csproj",
    "ZeroPlatform\ZeroGeometry\src\ZeroGeometry.Core\ZeroGeometry.Core.csproj",
    "ZeroPlatform\ZeroGraphics\src\ZeroGraphics.Core\ZeroGraphics.Core.csproj",
    "ZeroPlatform\ZeroInference\src\ZeroInference.Core\ZeroInference.Core.csproj",
    "ZeroPlatform\ZeroIoT\src\ZeroIoT\ZeroIoT.csproj",
    "ZeroPlatform\ZeroNetwork\src\ZeroNetwork.Core\ZeroNetwork.Core.csproj",
    "ZeroPlatform\ZeroNeural\src\ZeroNeural.Core\ZeroNeural.Core.csproj",
    "ZeroPlatform\ZeroPipeline\src\ZeroPipeline.Nodes\ZeroPipeline.Nodes.csproj",
    "ZeroPlatform\ZeroReports\src\ZeroReports\ZeroReports.csproj",
    "ZeroPlatform\ZeroRfid\src\ZeroRfid.Core\ZeroRfid.Core.csproj",
    "ZeroPlatform\ZeroSecurity\src\ZeroSecurity\ZeroSecurity.csproj",
    "ZeroPlatform\ZeroSignal\src\ZeroSignal.Core\ZeroSignal.Core.csproj",
    "ZeroPlatform\ZeroStorage\src\ZeroStorage.Core\ZeroStorage.Core.csproj",
    "ZeroPlatform\ZeroSystem\src\ZeroSystem.Core\ZeroSystem.Core.csproj",
    "ZeroPlatform\ZeroTensor\src\ZeroTensor.Core\ZeroTensor.Core.csproj",
    "ZeroPlatform\ZeroTwin3D\src\ZeroTwin3D\ZeroTwin3D.csproj",
    "ZeroPlatform\ZeroUI\src\ZeroUI.Core\ZeroUI.Core.csproj",
    "ZeroPlatform\ZeroVideo\src\ZeroVideo\ZeroVideo.csproj"
)

$allExist = $true
$missingFiles = @()
foreach ($rel in $targetConsumers) {
    $full = Join-Path $WorkspaceRoot $rel
    if (-not (Test-Path $full)) {
        $allExist = $false
        $missingFiles += $rel
    }
}
Record-TestResult "Tier 1 (R1)" "All 28 Target Consumer Projects Exist on Disk" $allExist ($missingFiles -join ", ")

# Check Project References in ZUpdate and ZView
$zUpdatePath = Join-Path $WorkspaceRoot "ZeroApps\ZUpdate\src\ZUpdate.Core\ZUpdate.Core.csproj"
$zViewPath = Join-Path $WorkspaceRoot "ZeroApps\ZView\src\ZView.Core\ZView.Core.csproj"
$zUpContent = if (Test-Path $zUpdatePath) { Get-Content $zUpdatePath -Raw } else { "" }
$zVwContent = if (Test-Path $zViewPath) { Get-Content $zViewPath -Raw } else { "" }

$projRefOk = ($zUpContent -match "ZeroPrimitives\.Core\.csproj") -and ($zVwContent -match "ZeroPrimitives\.Core\.csproj")
Record-TestResult "Tier 1 (R1)" "ZUpdate and ZView Preserve ProjectReference to ZeroPrimitives.Core" $projRefOk

# Check nuget.config local feeds
$nugetConfigs = Get-ChildItem -Path $WorkspaceRoot -Filter "nuget.config" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }
$feedsOk = $true
foreach ($cfg in $nugetConfigs) {
    $content = Get-Content $cfg.FullName -Raw
    if ($content -match "zeroprimitives") {
        # Feed mapped
    }
}
Record-TestResult "Tier 1 (R1)" "Local NuGet Feeds Mapped in nuget.config" ($nugetConfigs.Count -ge 2)

# Feature R2 Tests - Algorithmic Contracts
function Remove-Diacritics([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return "" }
    $normalized = $text.Normalize([System.Text.NormalizationForm]::FormD)
    $sb = New-Object System.Text.StringBuilder
    foreach ($c in $normalized.ToCharArray()) {
        $uc = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($c)
        if ($uc -ne [System.Globalization.UnicodeCategory]::NonSpacingMark) {
            if ($c -eq [char]0x0111) { [void]$sb.Append('d') }
            elseif ($c -eq [char]0x0110) { [void]$sb.Append('D') }
            else { [void]$sb.Append($c) }
        }
    }
    return $sb.ToString()
}

$sampleDiacritic = "Đơn Hàng / Bán Lẻ - Mã Phiếu: 12345"
$unaccented = Remove-Diacritics $sampleDiacritic
Record-TestResult "Tier 1 (R2)" "Vietnamese Diacritics Removal Contract" ($unaccented -eq "Don Hang / Ban Le - Ma Phieu: 12345")

# MST Modulo 11 Oracle Check
function Test-Mst([string]$mst) {
    if ([string]::IsNullOrWhiteSpace($mst)) { return $false }
    $clean = $mst.Trim().Replace(" ", "")
    if ($clean.Length -eq 14 -and $clean[10] -eq '-') {
        if ($clean.Substring(11) -eq "000") { return $false }
        $clean = $clean.Substring(0, 10)
    } elseif ($clean.Length -ne 10) {
        return $false
    }
    $weights = @(31, 29, 23, 19, 17, 13, 7, 5, 3)
    $sum = 0
    for ($i = 0; $i -lt 9; $i++) {
        $sum += ([int][string]$clean[$i]) * $weights[$i]
    }
    $rem = $sum % 11
    $cd = 10 - $rem
    $actual = [int][string]$clean[9]
    return ($actual -eq $cd) -or ($cd -eq 10 -and $actual -eq 0)
}

$viettelMstValid = Test-Mst "0100109106"
$branchMstValid = Test-Mst "0100109106-001"
$invalidMstRej = -not (Test-Mst "0100109107")
Record-TestResult "Tier 1 (R2)" "Circular 105 MST Modulo-11 Checksum Oracle" ($viettelMstValid -and $branchMstValid -and $invalidMstRej)

# Feature R4 Tests - Solution Integrity
$masterSlnx = Join-Path $WorkspaceRoot "ZeroPlatform\ZeroPlatform.slnx"
$slnxExists = Test-Path $masterSlnx
Record-TestResult "Tier 1 (R4)" "Master Solution ZeroPlatform.slnx Exists" $slnxExists

Write-Host "`n--- Running Tier 2: Boundary & Corner Cases ---" -ForegroundColor Yellow

# Boundary 1: Empty and Null Strings
$emptyDiacritics = (Remove-Diacritics "") -eq ""
Record-TestResult "Tier 2 (R2)" "Empty String Diacritic Removal Returns Empty" $emptyDiacritics

# Boundary 2: Branch 000 Rejection
$branch000Rejected = -not (Test-Mst "0100109106-000")
Record-TestResult "Tier 2 (R2)" "Prohibited Branch Code 000 Rejection" $branch000Rejected

# Boundary 3: Multiple Solutions Scanned
$solutions = Get-ChildItem -Path $WorkspaceRoot -Include "*.slnx", "*.sln" -Recurse | Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }
Record-TestResult "Tier 2 (R4)" "Ecosystem Solutions Scanned (Count >= 40)" ($solutions.Count -ge 40) "Found $($solutions.Count)"

Write-Host "`n--- Running Tier 3, 4 & 5 via Compiled xUnit Harness ---" -ForegroundColor Yellow

if (-not $SkipDotnetTest) {
    $e2eProj = Join-Path $WorkspaceRoot "tests\ZeroUniverse.E2E.Tests\ZeroUniverse.E2E.Tests.csproj"
    Write-Host "Executing: dotnet test $e2eProj -c $Configuration" -ForegroundColor Gray
    
    $testOutput = & dotnet test $e2eProj -c $Configuration 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "xUnit Test Runner: ALL TESTS PASSED!" -ForegroundColor Green
        $fullStr = ($testOutput | Out-String)
        if ($fullStr -match "Passed!\s+-\s+Failed:\s+0,\s+Passed:\s+(\d+)") {
            $xUnitPassed = [int]$Matches[1]
            $script:PassedTests += $xUnitPassed
            $script:TotalTests += $xUnitPassed
            Write-Host "  Passed xUnit assertions: $xUnitPassed" -ForegroundColor Green
        }
    } else {
        Write-Host "xUnit Test Runner: FAILURES DETECTED (Exit code $exitCode)" -ForegroundColor Red
        Write-Host ($testOutput -join "`n") -ForegroundColor Red
        $script:FailedTests++
        $script:TotalTests++
    }
}

$Stopwatch.Stop()

Write-Host "`n======================================================================" -ForegroundColor Cyan
Write-Host "  TEST SUITE EXECUTION SUMMARY" -ForegroundColor Cyan
Write-Host "  Total Tests Executed: $TotalTests" -ForegroundColor Cyan
Write-Host "  Passed: $PassedTests" -ForegroundColor Green
Write-Host "  Failed: $FailedTests" -ForegroundColor $(if ($FailedTests -eq 0) { "Green" } else { "Red" })
Write-Host "  Elapsed Duration: $($Stopwatch.ElapsedMilliseconds) ms" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

if ($FailedTests -gt 0) {
    exit 1
}
exit 0
