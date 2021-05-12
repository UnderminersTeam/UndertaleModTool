﻿// Modified with the help of Agentalex9
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

int progress = 0;
string texFolder = GetFolder(FilePath) + "Export_Textures" + Path.DirectorySeparatorChar;
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
    UpdateProgressBar(null, "Sprites", progress++, Data.Sprites.Count);
}

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
    for (int i = 0; i < sprite.Textures.Count; i++)
        if (sprite.Textures[i]?.Texture != null)
            worker.ExportAsPNG(sprite.Textures[i].Texture, texFolder + sprite.Name.Content + "_" + i + ".png", null, true); // Include padding to make sprites look neat!

    UpdateProgress();
}