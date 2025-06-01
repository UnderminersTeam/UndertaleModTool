// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using ImageMagick;

EnsureDataLoaded();

// Setup root import folder.
string subPath = PromptChooseDirectory();
if (subPath is null)
{
    throw new ScriptException("The import folder was not set.");
}

SetProgressBar(null, "Tilesets", 0, Data.Backgrounds.Count);
StartProgressBarUpdater();

await ImportTilesets();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Import complete.");

async Task ImportTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, ImportTileset));
}

void ImportTileset(UndertaleBackground tileset)
{
    if (tileset is not null)
    {
        string filename = $"{tileset.Name.Content}.png";
        try
        {
            string path = Path.Combine(subPath, filename);
            if (File.Exists(path))
            {
                using MagickImage img = TextureWorker.ReadBGRAImageFromFile(path);
                tileset.Texture.ReplaceTexture(img);
            }
        }
        catch (Exception ex)
        {
            ScriptMessage($"Failed to import {filename} (index {Data.Backgrounds.IndexOf(tileset)}): {ex.Message}");
        }
    }

    IncrementProgress();
}
