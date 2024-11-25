// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using ImageMagick;

EnsureDataLoaded();

// Setup root export folder.
string subPath = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Tilesets");
int i = 0;

// Folder Check One
if (!Directory.Exists(subPath))
{
    ScriptError("There is no 'Export_Tilesets' folder to import.", "Error: Nothing to import.");
    return;
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

    IncrementProgress();
}
