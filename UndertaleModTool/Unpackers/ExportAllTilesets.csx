using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

int progress = 0;
string texFolder = GetFolder(FilePath) + "Export_Tilesets" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
CancellationToken token = cancelTokenSource.Token;

if (Directory.Exists(texFolder))
{
    ScriptError("A texture export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

Task.Run(ProgressUpdater);

await DumpTilesets();
worker.Cleanup();

cancelTokenSource.Cancel(); //stop ProgressUpdater
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + texFolder);


void UpdateProgress()
{
    UpdateProgressBar(null, "Tilesets", progress, Data.Backgrounds.Count);
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


async Task DumpTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpTileset));

    progress--;
}

void DumpTileset(UndertaleBackground tileset) 
{
    if (tileset.Texture != null)
        worker.ExportAsPNG(tileset.Texture, texFolder + tileset.Name.Content + ".png");

    IncProgress();
}