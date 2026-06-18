# UndertaleModTool perf tools

`Run-UMTScriptPerf.ps1` runs an UndertaleModTool `.csx` script through `UndertaleModCli` repeatedly and writes raw logs plus a summary report under `perf-results/umt-script/`.

`Run-UMTDumpPerf.ps1` runs native `UndertaleModCli dump` exports repeatedly and writes the same kind of raw logs plus a summary report under `perf-results/umt-dump/`.

The script is intended for local before/after performance checks of real user scripts, such as asset exporters. It does not modify the input game data file unless the `.csx` script itself does so.

Use `{OutputDir}` in scripted answers when a script prompts for an export directory. The runner creates a fresh directory for each iteration.

## Export all sounds example

```powershell
.\tools\perf\Run-UMTScriptPerf.ps1 `
  "C:\Program Files (x86)\Steam\steamapps\common\Undertale\data.win" `
  ".\UndertaleModTool\Scripts\Resource Exporters\ExportAllSounds.csx" `
  -Answers "{OutputDir},y,n" `
  -Iterations 5 `
  -Label export-all-sounds
```

To compare a later run with a previous summary:

```powershell
.\tools\perf\Run-UMTScriptPerf.ps1 `
  "C:\Program Files (x86)\Steam\steamapps\common\Undertale\data.win" `
  ".\UndertaleModTool\Scripts\Resource Exporters\ExportAllSounds.csx" `
  -Answers "{OutputDir},y,n" `
  -Iterations 5 `
  -Label after-audio-export-change `
  -CompareWith .\perf-results\umt-script\20260615-210000-export-all-sounds
```

Use the median values in `summary.md` for discussion. Raw per-run logs stay in the same directory when a timing looks suspicious.

## Faster native sound export

The CLI also has a native sound dump path that avoids C# script startup overhead:

```powershell
dotnet build .\UndertaleModCli\UndertaleModCli.csproj -c Release

.\UndertaleModCli\bin\Release\net10.0\UndertaleModCli.exe dump `
  "C:\Program Files (x86)\Steam\steamapps\common\Undertale\data.win" `
  --sounds `
  --copy-external-audio `
  -o .\perf-results\native-dump-sounds
```

This is the better path when the goal is just exporting audio. Keep the `.csx` runner for testing script behavior specifically.

To measure that native path with a reusable report:

```powershell
.\tools\perf\Run-UMTDumpPerf.ps1 `
  "C:\Program Files (x86)\Steam\steamapps\common\Undertale\data.win" `
  Sounds `
  -Iterations 5 `
  -Label native-sounds
```

To compare native dump output against an older script summary:

```powershell
.\tools\perf\Run-UMTDumpPerf.ps1 `
  "C:\Program Files (x86)\Steam\steamapps\common\Undertale\data.win" `
  Sounds `
  -Iterations 5 `
  -Label native-sounds-after-change `
  -CompareWith .\perf-results\umt-script\20260615-210000-export-all-sounds\summary.json
```

Add `-VerboseDump` when you need to profile or inspect verbose CLI output; the runner captures stdout and stderr without blocking on noisy logs.
