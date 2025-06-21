using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string texFolder = PromptChooseDirectory();
if (texFolder is null)
{
    return;
}

SetProgressBar(null, "Tilesets", 0, Data.Backgrounds.Count);
StartProgressBarUpdater();

TextureWorker worker = null;
using (worker = new())
{
    await DumpTilesets();
}

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, DumpTileset));
}

void DumpTileset(UndertaleBackground tileset)
{
    if (tileset?.Texture is not null)
    {
        worker.ExportAsPNG(tileset.Texture, Path.Combine(texFolder, $"{tileset.Name.Content}.png"));
    }

    IncrementProgressParallel();
}