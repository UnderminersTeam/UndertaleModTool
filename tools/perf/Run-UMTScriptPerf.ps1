[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $DataFile,

    [Parameter(Mandatory = $true, Position = 1)]
    [string] $Script,

    [string[]] $Answers = @(),

    [ValidateRange(1, 100)]
    [int] $Iterations = 3,

    [string] $Label,

    [string] $Configuration = "Debug",

    [string] $CliProject,

    [string] $OutputRoot,

    [string] $CompareWith,

    [ValidateRange(1, 86400)]
    [int] $TimeoutSeconds = 300,

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
        return "script"
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
    $bytes = ($files | Measure-Object -Property Length -Sum).Sum
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

function Write-AnswerFile([string] $Path, [string[]] $ResolvedAnswers) {
    $text = ($ResolvedAnswers -join "`r`n") + "`r`n"
    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $text, $encoding)
}

function Normalize-Answers([string[]] $RawAnswers) {
    if ($RawAnswers.Count -eq 1 -and $RawAnswers[0].Contains(",")) {
        return @($RawAnswers[0].Split(",") | ForEach-Object { $_.Trim().Trim('"').Trim("'") })
    }

    return @($RawAnswers)
}

function Write-Report([string] $Path, [hashtable] $Summary, [object] $BaselineSummary) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $timing = $Summary.timing

    $lines.Add("# UMT script perf")
    $lines.Add("")
    $lines.Add("- Label: ``$($Summary.label)``")
    $lines.Add("- Script: ``$($Summary.script)``")
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
    $OutputRoot = Join-Path $repoRoot "perf-results\umt-script"
}

$dataFilePath = (Resolve-Path -LiteralPath $DataFile).Path
$scriptPath = (Resolve-Path -LiteralPath $Script).Path
$cliProjectPath = (Resolve-Path -LiteralPath $CliProject).Path
$Answers = Normalize-Answers $Answers

if ([string]::IsNullOrWhiteSpace($Label)) {
    $Label = [System.IO.Path]::GetFileNameWithoutExtension($scriptPath)
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
$scriptOutputRoot = Join-Path $runDirectory "script-output"
New-Item -ItemType Directory -Force -Path $scriptOutputRoot | Out-Null

$runs = @()
for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
    $iterationName = "{0:D3}" -f $iteration
    $runOutputDirectory = Join-Path $scriptOutputRoot "run-$iterationName"
    $logPath = Join-Path $runDirectory "run-$iterationName.log"
    $answersPath = Join-Path $runDirectory "run-$iterationName.answers.txt"
    New-Item -ItemType Directory -Force -Path $runOutputDirectory | Out-Null

    $resolvedAnswers = foreach ($answer in $Answers) {
        $answer.Replace("{OutputDir}", $runOutputDirectory)
    }
    Write-AnswerFile -Path $answersPath -ResolvedAnswers $resolvedAnswers

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $cliArgs = ConvertTo-ArgumentString @("load", $dataFilePath, "--scripts", $scriptPath)
    $command = 'type "' + $answersPath + '" | "' + $cliExe + '" ' + $cliArgs + ' > "' + $logPath + '" 2>&1'
    $psi.FileName = "cmd.exe"
    $psi.Arguments = "/d /c " + $command
    $psi.WorkingDirectory = $repoRoot
    $psi.UseShellExecute = $false

    Write-Host "Running $([System.IO.Path]::GetFileName($scriptPath)) $iteration/$Iterations..."
    $process = [System.Diagnostics.Process]::Start($psi)
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) {
        & taskkill.exe /PID $process.Id /T /F *> $null
    }

    $stopwatch.Stop()

    $outputStats = Get-OutputStats $runOutputDirectory
    $runs += [pscustomobject]@{
        iteration = $iteration
        exitCode = if ($timedOut) { -1 } else { $process.ExitCode }
        timedOut = $timedOut
        elapsedMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 3)
        outputDirectory = $runOutputDirectory
        answersPath = $answersPath
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
    script = $scriptPath
    cliExe = $cliExe
    resultDirectory = $runDirectory
    scriptOutputRoot = $scriptOutputRoot
    iterationsRequested = $Iterations
    iterationsSucceeded = $timing.samples
    timeoutSeconds = $TimeoutSeconds
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
[pscustomobject]$timing | Format-List

if ($timing.samples -eq 0) {
    throw "No successful script runs completed."
}
