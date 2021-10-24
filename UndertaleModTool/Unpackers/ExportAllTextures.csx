using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Start export of all existing textures

int progress = 0;
string texFolder = Path.Combine(GetFolder(FilePath), "Export_Textures");
CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
CancellationToken token = cancelTokenSource.Token;

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
TextureWorker worker = new TextureWorker();

Task.Run(ProgressUpdater);

await DumpSprites();
await DumpFonts();
await DumpBackgrounds();
worker.Cleanup();

cancelTokenSource.Cancel(); //stop ProgressUpdater
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);


void UpdateProgress()
{
    UpdateProgressBar(null, "Textures Exported", progress, Data.TexturePageItems.Count);
}
void AddProgress(int updateAmount)
{
    Interlocked.Add(ref progress, updateAmount); //"thread-safe" add operation
}
async Task ProgressUpdater()
{
    while (true)
    {
        if (token.IsCancellationRequested)
            return;

        UpdateProgress();

        await Task.Delay(100); //10 times per second
    }
}

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
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
        {
            UndertaleTexturePageItem tex = sprite.Textures[i].Texture;
            worker.ExportAsPNG(tex, Path.Combine(sprFolder, sprite.Name.Content + "_" + i + ".png"));
        }
    }

    AddProgress(sprite.Textures.Count);
}

void DumpFont(UndertaleFont font)
{
    if (font.Texture != null)
    {
        UndertaleTexturePageItem tex = font.Texture;
        worker.ExportAsPNG(tex, Path.Combine(fntFolder, font.Name.Content + "_0.png"));

        AddProgress(1);
    }
}

void DumpBackground(UndertaleBackground background)
{
    if (background.Texture != null)
    {
        UndertaleTexturePageItem tex = background.Texture;
        worker.ExportAsPNG(tex, Path.Combine(bgrFolder, background.Name.Content + "_0.png"));

        AddProgress(1);
    }
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}
