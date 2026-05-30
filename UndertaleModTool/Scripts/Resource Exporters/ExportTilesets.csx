using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using UndertaleModLib.Scripting;

EnsureDataLoaded();

var builder = CreateScriptOptionsBuilder()
    .AddDirectory("folder", "Output Folder:")
    .AddText("patterns", "Names (one per line, leave empty for all):", multiline: true)
    .AddRadio("filterMode", "Filter mode:", "Exact", "Regex", "Wildcard");

var result = ShowScriptOptionsDialog("Export Tilesets", builder);
if (result is null) return;

string texFolder = result["folder"] as string;

if (!Directory.Exists(texFolder))
{
    ScriptError("The specified output folder does not exist.");
    return;
}

string rawPatterns = result["patterns"] as string;
bool exportAll = string.IsNullOrWhiteSpace(rawPatterns);
string[] patterns = rawPatterns.Split("\n", StringSplitOptions.RemoveEmptyEntries);
NameFilterMode filterMode = Enum.Parse<NameFilterMode>(result["filterMode"] as string);

SetProgressBar(null, "Tilesets", 0, Data.Backgrounds.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpTilesets();
}

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpTileset));
}

void DumpTileset(UndertaleBackground tileset)
{
    if (tileset?.Texture is null)
    {
        IncrementProgressParallel();
        return;
    }

    if (!exportAll)
    {
        bool match = false;
        foreach (string pattern in patterns)
        {
            if (NameFilter.IsMatch(tileset.Name.Content, pattern, filterMode))
            {
                match = true;
                break;
            }
        }
        if (!match) { IncrementProgressParallel(); return; }
    }

    worker.ExportAsPNG(tileset.Texture, Paths.JoinVerifyWithinDirectory(texFolder, $"{tileset.Name.Content}.png"));

    IncrementProgressParallel();
}
