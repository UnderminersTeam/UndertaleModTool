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
    .AddRadio("filterMode", "Filter mode:", "Exact", "Regex", "Wildcard")
    .AddBool("caseSensitive", "Case-sensitive", defaultValue: true);

var result = ShowScriptOptionsDialog("Export Masks", builder);
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
bool caseSensitive = result["caseSensitive"] as bool? == true;

SetProgressBar(null, "Sprite masks", 0, Data.Sprites.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpSprites();
}

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
}

void DumpSprite(UndertaleSprite sprite)
{
    if (sprite is null)
    {
        IncrementProgressParallel();
        return;
    }

    if (!exportAll)
    {
        bool match = false;
        foreach (string pattern in patterns)
        {
            if (NameFilter.IsMatch(sprite.Name.Content, pattern, filterMode, caseSensitive))
            {
                match = true;
                break;
            }
        }
        if (!match) { IncrementProgressParallel(); return; }
    }

    for (int i = 0; i < sprite.CollisionMasks.Count; i++)
    {
        if (sprite.CollisionMasks[i]?.Data is not null)
        {
            (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(Data);
            TextureWorker.ExportCollisionMaskPNG(sprite.CollisionMasks[i], Paths.JoinVerifyWithinDirectory(texFolder, $"{sprite.Name.Content}_{i}.png"), maskWidth, maskHeight);
        }
    }

    IncrementProgressParallel();
}
