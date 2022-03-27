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

string texFolder = GetFolder(FilePath) + "Export_Masks" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
if (Directory.Exists(texFolder))
{
    ScriptError("A texture export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

SetProgressBar(null, "Sprites", 0, Data.Sprites.Count);
StartProgressBarUpdater();

await DumpSprites();
worker.Cleanup();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);


string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DumpSprites()
{
    await Task.Run(() => Parallel.ForEach(Data.Sprites, DumpSprite));
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

    IncrementProgressParallel();
}
