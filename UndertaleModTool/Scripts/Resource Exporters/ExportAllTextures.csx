using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using UndertaleModLib.Scripting;

EnsureDataLoaded();

var builder = CreateScriptOptionsBuilder()
    .AddDirectory("folder", "Output Folder:")
    .AddBool("exportSprites", "Export sprites", defaultValue: true)
    .AddBool("exportFonts", "Export fonts", defaultValue: true)
    .AddBool("exportBackgrounds", "Export backgrounds", defaultValue: true)
    .AddBool("groupBySubdir", "Group sprites/fonts/backgrounds into subdirectories?");

var result = ShowScriptOptionsDialog("Export Textures", builder);
if (result is null) return;

string texFolder = result["folder"] as string;
bool exportSprites = result["exportSprites"] as bool? == true;
bool exportFonts = result["exportFonts"] as bool? == true;
bool exportBackgrounds = result["exportBackgrounds"] as bool? == true;
bool groupBySubdir = result["groupBySubdir"] as bool? == true;

if (!Directory.Exists(texFolder))
{
    ScriptError("The specified output folder does not exist.");
    return;
}

if (!exportSprites && !exportFonts && !exportBackgrounds)
{
    ScriptError("Nothing to export, select at least one category (sprites, fonts, backgrounds).");
    return;
}

// Create subdirectories.
string sprFolder = Path.Join(texFolder, "Sprites");
string fntFolder = Path.Join(texFolder, "Fonts");
string bgrFolder = Path.Join(texFolder, "Backgrounds");

if (exportSprites)
{
    Directory.CreateDirectory(sprFolder);
}
if (exportFonts)
{
    Directory.CreateDirectory(fntFolder);
}
if (exportBackgrounds)
{
    Directory.CreateDirectory(bgrFolder);
}

SetProgressBar(null, "Textures", 0, Data.TexturePageItems.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    if (exportSprites)
    {
        await DumpSprites();
    }
    if (exportFonts)
    {
        await DumpFonts();
    }
    if (exportBackgrounds)
    {
        await DumpBackgrounds();
    }
}

await StopProgressBarUpdater();
HideProgressBar();

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
    if (sprite is null)
    {
        return;
    }

    string outDir = sprFolder;
    if (groupBySubdir)
    {
        outDir = Paths.JoinVerifyWithinDirectory(sprFolder, sprite.Name.Content);
        Directory.CreateDirectory(outDir);
    }

    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture is not null)
        {
            UndertaleTexturePageItem tex = sprite.Textures[i].Texture;
            worker.ExportAsPNG(tex, Paths.JoinVerifyWithinDirectory(outDir, $"{sprite.Name.Content}_{i}.png"));
        }
    }

    AddProgressParallel(sprite.Textures.Count);
}

void DumpFont(UndertaleFont font)
{
    if (font?.Texture is null)
    {
        return;
    }

    string outDir = fntFolder;
    if (groupBySubdir)
    {
        outDir = Paths.JoinVerifyWithinDirectory(fntFolder, font.Name.Content);
        Directory.CreateDirectory(outDir);
    }

    UndertaleTexturePageItem tex = font.Texture;
    worker.ExportAsPNG(tex, Paths.JoinVerifyWithinDirectory(outDir, $"{font.Name.Content}_0.png"));

    IncrementProgressParallel();
}

void DumpBackground(UndertaleBackground background)
{
    if (background?.Texture is null)
    {
        return;
    }

    string outDir = bgrFolder;
    if (groupBySubdir)
    {
        outDir = Paths.JoinVerifyWithinDirectory(bgrFolder, background.Name.Content);
        Directory.CreateDirectory(outDir);
    }

    UndertaleTexturePageItem tex = background.Texture;
    worker.ExportAsPNG(tex, Paths.JoinVerifyWithinDirectory(outDir, $"{background.Name.Content}_0.png"));

    IncrementProgressParallel();
}
