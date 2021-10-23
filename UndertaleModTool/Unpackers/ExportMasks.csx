// Made by Grossley
// Version 1
// 12/07/2020

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

int progress = 0;
string texFolder = GetFolder(FilePath) + "Export_Masks" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();

if (Directory.Exists(texFolder))
{
    ScriptError("A texture export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

UpdateProgress();

await DumpSprites();
worker.Cleanup();

HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);

void UpdateProgress()
{
    UpdateProgressBar(null, "Sprites", progress, Data.Sprites.Count);
    Interlocked.Increment(ref progress); //"thread-safe" increment
}

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));

    progress--;
}

void DumpSprite(UndertaleSprite sprite)
{
    for (int i = 0; i < sprite.CollisionMasks.Count; i++)
    {
        if ((sprite.CollisionMasks[i]?.Data != null))
        {
            TextureWorker.ExportCollisionMaskPNG(sprite, sprite.CollisionMasks[i], texFolder + sprite.Name.Content + "_" + i + ".png");
        }
    }

    UpdateProgress();
}
