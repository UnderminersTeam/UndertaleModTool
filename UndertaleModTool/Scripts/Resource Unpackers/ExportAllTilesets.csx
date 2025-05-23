using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string texFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Tilesets");
if (Directory.Exists(texFolder))
{
    ScriptError("A tileset export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(texFolder);

SetProgressBar(null, "Tilesets", 0, Data.Backgrounds.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpTilesets();
}

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage($"Export Complete.\n\nLocation: {texFolder}");

async Task DumpTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpTileset));
}

void DumpTileset(UndertaleBackground tileset)
{
    if (tileset is not null && tileset.Texture != null)
        worker.ExportAsPNG(tileset.Texture, Path.Combine(texFolder, $"{tileset.Name.Content}.png"));

    IncrementProgressParallel();
}