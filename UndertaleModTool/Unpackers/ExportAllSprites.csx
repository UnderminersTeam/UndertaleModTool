using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

if (!(ScriptQuestion("Export all sprites unpadded?")))
{
    bool x = RunUMTScript(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "HelperScripts", "ExportAllSpritesWithPadding.csx"));
    if (x == false)
        ScriptError("ExportAllSpritesWithPadding.csx failed!");
    return;
}

int progress = 0;
string texFolder = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();

if (Directory.Exists(texFolder))
{
    ScriptError("A sprites export already exists. Please remove it.", "Error");
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
            worker.ExportAsPNG(sprite.Textures[i].Texture, texFolder + sprite.Name.Content + "_" + i + ".png");

    UpdateProgress();
}