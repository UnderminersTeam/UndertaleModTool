[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $DataFile,

    [Parameter(Mandatory = $true, Position = 1)]
    [ValidateSet("Sounds", "Sprites", "Textures", "Strings")]
    [string] $DumpKind,

    [ValidateRange(1, 100)]
    [int] $Iterations = 3,

    [string] $Label,

    [string] $Configuration = "Release",

    [string] $CliProject,

    [string] $OutputRoot,

    [string] $CompareWith,

    [ValidateRange(1, 86400)]
    [int] $TimeoutSeconds = 300,

    [switch] $CopyExternalAudio,

    [switch] $GroupSoundsByAudioGroup,

    [switch] $VerboseDump,

    [switch] $NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
}

function Get-SafeName([string] $Name) {
    $safe = $Name -replace "[^A-Za-z0-9._-]", "-"
    $safe = $safe.Trim("-")
    if ([string]::IsNullOrWhiteSpace($safe)) {
        return "dump"
    }

    return $safe
}

function Get-Median([double[]] $Values) {
    if ($Values.Count -eq 0) {
        return $null
    }

    $sorted = @($Values | Sort-Object)
    $middle = [int][Math]::Floor($sorted.Count / 2)
    if (($sorted.Count % 2) -eq 1) {
        return [Math]::Round($sorted[$middle], 3)
    }

    return [Math]::Round(($sorted[$middle - 1] + $sorted[$middle]) / 2, 3)
}

function Get-RunSummary([object[]] $Runs) {
    $successfulRuns = @($Runs | Where-Object { -not $_.timedOut -and $_.exitCode -eq 0 })
    $elapsedValues = @($successfulRuns | ForEach-Object { [double] $_.elapsedMs })
    if ($elapsedValues.Count -eq 0) {
        return [ordered]@{
            samples = 0
            medianMs = $null
            meanMs = $null
            minMs = $null
            maxMs = $null
        }
    }

    $sum = 0.0
    foreach ($value in $elapsedValues) {
        $sum += $value
    }

    return [ordered]@{
        samples = $elapsedValues.Count
        medianMs = Get-Median $elapsedValues
        meanMs = [Math]::Round($sum / $elapsedValues.Count, 3)
        minMs = [Math]::Round(($elapsedValues | Measure-Object -Minimum).Minimum, 3)
        maxMs = [Math]::Round(($elapsedValues | Measure-Object -Maximum).Maximum, 3)
    }
}

function Read-Summary([string] $Path) {
    $summaryPath = $Path
    if (Test-Path -LiteralPath $Path -PathType Container) {
        $summaryPath = Join-Path $Path "summary.json"
    }

    if (-not (Test-Path -LiteralPath $summaryPath -PathType Leaf)) {
        throw "Could not find summary.json at '$Path'."
    }

    return Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
}

function Format-Ms([object] $Value) {
    if ($null -eq $Value) {
        return ""
    }

    return ([double] $Value).ToString("N3", [System.Globalization.CultureInfo]::InvariantCulture)
}

function Add-MarkdownTableRow([System.Collections.Generic.List[string]] $Lines, [string[]] $Cells) {
    $escaped = foreach ($cell in $Cells) {
        if ($null -eq $cell) {
            ""
        }
        else {
            $cell -replace "\|", "\\|"
        }
    }

    $Lines.Add("| " + ($escaped -join " | ") + " |")
}

function Get-OutputStats([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return [ordered]@{
            files = 0
            bytes = 0
        }
    }

    $files = @(Get-ChildItem -LiteralPath $Path -File -Recurse)
    $measure = $files | Measure-Object -Property Length -Sum
    $bytes = $measure.Sum
    if ($null -eq $bytes) {
        $bytes = 0
    }

    return [ordered]@{
        files = $files.Count
        bytes = [long] $bytes
    }
}

function ConvertTo-ArgumentString([string[]] $Arguments) {
    $quoted = foreach ($argument in $Arguments) {
        '"' + ($argument -replace '"', '""') + '"'
    }

    return $quoted -join " "
}

function Get-DumpArguments([string] $DataFilePath, [string] $RunOutputDirectory) {
    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.Add("dump")
    $arguments.Add($DataFilePath)
    if ($VerboseDump) {
        $arguments.Add("-v")
    }
    $arguments.Add("-o")
    $arguments.Add($RunOutputDirectory)

    switch ($DumpKind) {
        "Sounds" {
            $arguments.Add("--sounds")
            if ($CopyExternalAudio) {
                $arguments.Add("--copy-external-audio")
            }
            if ($GroupSoundsByAudioGroup) {
                $arguments.Add("--group-sounds-by-audio-group")
            }
        }
        "Sprites" { $arguments.Add("--sprites") }
        "Textures" { $arguments.Add("--textures") }
        "Strings" { $arguments.Add("--strings") }
    }

    return $arguments.ToArray()
}

function Write-Report([string] $Path, [hashtable] $Summary, [object] $BaselineSummary) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $timing = $Summary.timing

    $lines.Add("# UMT dump perf")
    $lines.Add("")
    $lines.Add("- Label: ``$($Summary.label)``")
    $lines.Add("- Dump kind: ``$($Summary.dumpKind)``")
    $lines.Add("- Data file: ``$($Summary.dataFile)``")
    $lines.Add("- Result directory: ``$($Summary.resultDirectory)``")
    $lines.Add("- Iterations: $($Summary.iterationsSucceeded)/$($Summary.iterationsRequested)")
    $lines.Add("- Median: ``$(Format-Ms $timing.medianMs) ms``")
    $lines.Add("")

    $lines.Add("## Runs")
    $lines.Add("")
    Add-MarkdownTableRow $lines @("Run", "Exit", "Timed out", "Elapsed ms", "Files", "Bytes", "Log")
    Add-MarkdownTableRow $lines @("---:", "---:", "---", "---:", "---:", "---:", "---")
    foreach ($run in $Summary.runs) {
        Add-MarkdownTableRow $lines @(
            [string] $run.iteration,
            [string] $run.exitCode,
            [string] $run.timedOut,
            (Format-Ms $run.elapsedMs),
            [string] $run.outputFiles,
            [string] $run.outputBytes,
            $run.logPath
        )
    }

    if ($null -ne $BaselineSummary) {
        $baselineMedian = [double] $BaselineSummary.timing.medianMs
        $currentMedian = [double] $timing.medianMs
        if ($baselineMedian -gt 0 -and $currentMedian -gt 0) {
            $reduction = (($baselineMedian - $currentMedian) / $baselineMedian) * 100.0
            $speedup = $baselineMedian / $currentMedian

            $lines.Add("")
            $lines.Add("## Comparison")
            $lines.Add("")
            Add-MarkdownTableRow $lines @("Baseline median", "Current median", "Time change", "Speedup")
            Add-MarkdownTableRow $lines @("---:", "---:", "---:", "---:")
            Add-MarkdownTableRow $lines @(
                (Format-Ms $baselineMedian),
                (Format-Ms $currentMedian),
                ($reduction.ToString("N1", [System.Globalization.CultureInfo]::InvariantCulture) + "%"),
                ($speedup.ToString("N2", [System.Globalization.CultureInfo]::InvariantCulture) + "x")
            )
        }
    }

    Set-Content -LiteralPath $Path -Value $lines -Encoding UTF8
}

$repoRoot = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($CliProject)) {
    $CliProject = Join-Path $repoRoot "UndertaleModCli\UndertaleModCli.csproj"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "perf-results\umt-dump"
}

$dataFilePath = (Resolve-Path -LiteralPath $DataFile).Path
$cliProjectPath = (Resolve-Path -LiteralPath $CliProject).Path

if ([string]::IsNullOrWhiteSpace($Label)) {
    $Label = "dump-$($DumpKind.ToLowerInvariant())"
}

if (-not $NoBuild) {
    Write-Host "Building $cliProjectPath ($Configuration)..."
    & dotnet build $cliProjectPath -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }
}

$cliDirectory = Split-Path -Parent $cliProjectPath
$cliExe = Join-Path $cliDirectory "bin\$Configuration\net10.0\UndertaleModCli.exe"
if (-not (Test-Path -LiteralPath $cliExe -PathType Leaf)) {
    throw "Could not find CLI executable at '$cliExe'."
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runDirectory = Join-Path $OutputRoot "$timestamp-$(Get-SafeName $Label)"
$dumpOutputRoot = Join-Path $runDirectory "dump-output"
New-Item -ItemType Directory -Force -Path $dumpOutputRoot | Out-Null

$runs = @()
for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
    $iterationName = "{0:D3}" -f $iteration
    $runOutputDirectory = Join-Path $dumpOutputRoot "run-$iterationName"
    $logPath = Join-Path $runDirectory "run-$iterationName.log"
    New-Item -ItemType Directory -Force -Path $runOutputDirectory | Out-Null

    $cliArgs = ConvertTo-ArgumentString (Get-DumpArguments -DataFilePath $dataFilePath -RunOutputDirectory $runOutputDirectory)
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $cliExe
    $psi.Arguments = $cliArgs
    $psi.WorkingDirectory = $repoRoot
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true

    Write-Host "Running $DumpKind dump $iteration/$Iterations..."
    $process = [System.Diagnostics.Process]::Start($psi)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) {
        & taskkill.exe /PID $process.Id /T /F *> $null
        $process.WaitForExit()
    }

    $stopwatch.Stop()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    [System.IO.File]::WriteAllText($logPath, $stdout + $stderr)

    $outputStats = Get-OutputStats $runOutputDirectory
    $runs += [pscustomobject]@{
        iteration = $iteration
        exitCode = if ($timedOut) { -1 } else { $process.ExitCode }
        timedOut = $timedOut
        elapsedMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 3)
        outputDirectory = $runOutputDirectory
        outputFiles = $outputStats.files
        outputBytes = $outputStats.bytes
        logPath = $logPath
    }
}

$timing = Get-RunSummary $runs
$summary = [ordered]@{
    label = $Label
    startedUtc = (Get-Date).ToUniversalTime().ToString("O")
    dataFile = $dataFilePath
    dumpKind = $DumpKind
    cliExe = $cliExe
    resultDirectory = $runDirectory
    dumpOutputRoot = $dumpOutputRoot
    iterationsRequested = $Iterations
    iterationsSucceeded = $timing.samples
    timeoutSeconds = $TimeoutSeconds
    copyExternalAudio = [bool] $CopyExternalAudio
    groupSoundsByAudioGroup = [bool] $GroupSoundsByAudioGroup
    verboseDump = [bool] $VerboseDump
    timing = $timing
    runs = $runs
}

$summaryPath = Join-Path $runDirectory "summary.json"
$reportPath = Join-Path $runDirectory "summary.md"
$summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

$baselineSummary = $null
if (-not [string]::IsNullOrWhiteSpace($CompareWith)) {
    $baselineSummary = Read-Summary $CompareWith
}

Write-Report -Path $reportPath -Summary $summary -BaselineSummary $baselineSummary

Write-Host ""
Write-Host "Wrote $summaryPath"
Write-Host "Wrote $reportPath"
Write-Host ""
Write-Host "Timing:"
[pscustomobject] $timing | Format-List

if ($timing.samples -eq 0) {
    throw "No successful dump runs completed."
}
