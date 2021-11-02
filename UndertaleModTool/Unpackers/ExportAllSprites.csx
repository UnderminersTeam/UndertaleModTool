// Modified with the help of Agentalex9
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool padded = (!ScriptQuestion("Export all sprites unpadded?"));

string texFolder = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
if (Directory.Exists(texFolder))
{
    ScriptError("A sprites export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

SetProgressBar(null, "Sprites", 0, Data.Sprites.Count);
StartUpdater();

await DumpSprites();
worker.Cleanup();

await StopUpdater();
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
    for (int i = 0; i < sprite.Textures.Count; i++)
        if (sprite.Textures[i]?.Texture != null)
            worker.ExportAsPNG(sprite.Textures[i].Texture, texFolder + sprite.Name.Content + "_" + i + ".png", null, padded); // Include padding to make sprites look neat!

    IncProgressP();
}