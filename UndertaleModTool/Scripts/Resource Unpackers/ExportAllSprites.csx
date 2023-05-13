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
    if (sprite.Textures.Count == 1)
    {
        if (sprite.Textures[0]?.Texture != null)
            worker.ExportAsPNG(sprite.Textures[0].Texture, texFolder + sprite.Name.Content + ".png", null, padded); // Include padding to make sprites look neat!
    }
    else
    {
        Directory.CreateDirectory(texFolder + sprite.Name.Content + Path.DirectorySeparatorChar);
        for (int i = 0; i < sprite.Textures.Count; i++)
            if (sprite.Textures[i]?.Texture != null)
                worker.ExportAsPNG(sprite.Textures[i].Texture, texFolder + sprite.Name.Content + Path.DirectorySeparatorChar + sprite.Name.Content + "_" + i + ".png", null, padded);
    }
    IncrementProgressParallel();
}