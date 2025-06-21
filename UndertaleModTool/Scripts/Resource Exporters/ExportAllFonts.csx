using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using System.Linq;

EnsureDataLoaded();

string fntFolder = PromptChooseDirectory();
if (fntFolder is null)
{
    return;
}

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
    if (font is not null)
    {
        worker.ExportAsPNG(font.Texture, Path.Combine(fntFolder, $"{font.Name.Content}.png"));
        using (StreamWriter writer = new(Path.Combine(fntFolder, $"glyphs_{font.Name.Content}.csv")))
        {
            writer.WriteLine($"{font.DisplayName};{font.EmSize};{font.Bold};{font.Italic};{font.Charset};{font.AntiAliasing};{font.ScaleX};{font.ScaleY}");

            foreach (var g in font.Glyphs)
            {
                writer.WriteLine($"{g.Character};{g.SourceX};{g.SourceY};{g.SourceWidth};{g.SourceHeight};{g.Shift};{g.Offset}");
            }
        }
    }

    IncrementProgressParallel();
}
