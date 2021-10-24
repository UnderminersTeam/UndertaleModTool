// Modified with the help of Agentalex9
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool padded = (!ScriptQuestion("Export all sprites unpadded?"));

int progress = 0;
string texFolder = GetFolder(FilePath) + "Export_Sprites" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
CancellationToken token = cancelTokenSource.Token;

if (Directory.Exists(texFolder))
{
    ScriptError("A sprites export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

Task.Run(ProgressUpdater);

await DumpSprites();
worker.Cleanup();

cancelTokenSource.Cancel(); //stop ProgressUpdater
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);


void UpdateProgress()
{
    UpdateProgressBar(null, "Sprites", progress, Data.Sprites.Count);
}
void IncProgress()
{
    Interlocked.Increment(ref progress); //"thread-safe" increment
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
    for (int i = 0; i < sprite.Textures.Count; i++)
        if (sprite.Textures[i]?.Texture != null)
            worker.ExportAsPNG(sprite.Textures[i].Texture, texFolder + sprite.Name.Content + "_" + i + ".png", null, padded); // Include padding to make sprites look neat!

    IncProgress();
}