using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using UndertaleModLib.Scripting;
using System.Linq;

EnsureDataLoaded();

var builder = CreateScriptOptionsBuilder()
    .AddDirectory("folder", "Output Folder:")
    .AddText("patterns", "Names (one per line, leave empty for all):", multiline: true)
    .AddRadio("filterMode", "Filter mode:", "Exact", "Regex", "Wildcard");

var result = ShowScriptOptionsDialog("Export Fonts", builder);
if (result is null) return;

string fntFolder = result["folder"] as string;
if (!Directory.Exists(fntFolder))
{
    ScriptError("The specified output folder does not exist.");
    return;
}
string rawPatterns = result["patterns"] as string;
bool exportAll = string.IsNullOrWhiteSpace(rawPatterns);
string[] patterns = rawPatterns.Split("\n", StringSplitOptions.RemoveEmptyEntries);
NameFilterMode filterMode = Enum.Parse<NameFilterMode>(result["filterMode"] as string);

SetProgressBar(null, "Fonts", 0, Data.Fonts.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpFonts();
}

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));
}

void DumpFont(UndertaleFont font)
{
    if (font is null)
    {
        IncrementProgressParallel();
        return;
    }

    if (!exportAll)
    {
        bool match = false;
        foreach (string pattern in patterns)
        {
            if (NameFilter.IsMatch(font.Name.Content, pattern, filterMode))
            {
                match = true;
                break;
            }
        }
        if (!match) { IncrementProgressParallel(); return; }
    }

    worker.ExportAsPNG(font.Texture, Paths.JoinVerifyWithinDirectory(fntFolder, $"{font.Name.Content}.png"));
    using (StreamWriter writer = new(Paths.JoinVerifyWithinDirectory(fntFolder, $"glyphs_{font.Name.Content}.csv")))
    {
        writer.WriteLine($"{font.DisplayName};{font.EmSize};{font.Bold};{font.Italic};{font.Charset};{font.AntiAliasing};{font.ScaleX};{font.ScaleY}");

        foreach (var g in font.Glyphs)
        {
            writer.WriteLine($"{g.Character};{g.SourceX};{g.SourceY};{g.SourceWidth};{g.SourceHeight};{g.Shift};{g.Offset}");
        }
    }

    IncrementProgressParallel();
}
