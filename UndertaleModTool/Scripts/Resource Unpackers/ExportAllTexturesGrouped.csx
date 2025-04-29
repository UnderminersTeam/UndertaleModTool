using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Start export of all existing textures

string texFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Textures");
if (Directory.Exists(texFolder))
{
    ScriptError("A sprites export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);
string sprFolder = Path.Combine(texFolder, "Sprites");
Directory.CreateDirectory(sprFolder);
string fntFolder = Path.Combine(texFolder, "Fonts");
Directory.CreateDirectory(fntFolder);
string bgrFolder = Path.Combine(texFolder, "Backgrounds");
Directory.CreateDirectory(bgrFolder);

SetProgressBar(null, "Textures Exported", 0, Data.TexturePageItems.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpSprites();
    await DumpFonts();
    await DumpBackgrounds();
}

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage($"Export Complete.\n\nLocation: {texFolder}");

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
}

async Task DumpBackgrounds()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpBackground));
}

async Task DumpFonts()
{
    await Task.Run(() => Parallel.ForEach(Data.Fonts, DumpFont));
}

void DumpSprite(UndertaleSprite sprite)
{
    if (sprite is not null)
    {
        for (int i = 0; i < sprite.Textures.Count; i++)
        {
            if (sprite.Textures[i]?.Texture != null)
            {
                UndertaleTexturePageItem tex = sprite.Textures[i].Texture;
                string sprFolder2 = Path.Combine(sprFolder, sprite.Name.Content);
                Directory.CreateDirectory(sprFolder2);
                worker.ExportAsPNG(tex, Path.Combine(sprFolder2, $"{sprite.Name.Content}_{i}.png"));
            }
        }

        AddProgressParallel(sprite.Textures.Count);
    }
}

void DumpFont(UndertaleFont font)
{
    if (font is null)
        return;
    if (font.Texture != null)
    {
        UndertaleTexturePageItem tex = font.Texture;
        string fntFolder2 = Path.Combine(fntFolder, font.Name.Content);
        Directory.CreateDirectory(fntFolder2);
        worker.ExportAsPNG(tex, Path.Combine(fntFolder2, $"{font.Name.Content}_0.png"));
        IncrementProgressParallel();
    }
}

void DumpBackground(UndertaleBackground background)
{
    if (background is null)
        return;
    if (background.Texture != null)
    {
        UndertaleTexturePageItem tex = background.Texture;
        string bgrFolder2 = Path.Combine(bgrFolder, background.Name.Content);
        Directory.CreateDirectory(bgrFolder2);
        worker.ExportAsPNG(tex, Path.Combine(bgrFolder2, $"{background.Name.Content}_0.png"));
        IncrementProgressParallel();
    }
}
