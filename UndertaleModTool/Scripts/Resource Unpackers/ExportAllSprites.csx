// Modified with the help of Agentalex9
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool padded = (!ScriptQuestion("Export all sprites unpadded?"));

bool useSubDirectories = ScriptQuestion("Export sprites into subdirectories?");

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
    string outputFolder = texFolder;
    if (useSubDirectories)
        outputFolder = Path.Combine(outputFolder, sprite.Name.Content);
    if (sprite.Textures.Count > 0)
        Directory.CreateDirectory(outputFolder);
        
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
            worker.ExportAsPNG(sprite.Textures[i].Texture, Path.Combine(outputFolder, sprite.Name.Content + "_" + i + ".png"), null, padded); // Include padding to make sprites look neat!
    }

    IncrementProgressParallel();
}
