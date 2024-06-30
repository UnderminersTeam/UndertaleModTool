// Adapted from original script by Grossley

// TODO: remove system.drawing from here, used for bitmaps 
using System.Text;
using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.

string subPath = Path.Combine(winFolder, "Export_Tilesets");
int i = 0;

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

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
ScriptMessage("Import Complete.");


async Task ImportTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, ImportTileset));
}

void ImportTileset(UndertaleBackground tileset)
{
    try
    {
        string path = Path.Combine(subPath, tileset.Name.Content + ".png");
        if (File.Exists(path))
        {
            Bitmap img = new Bitmap(path);
            tileset.Texture.ReplaceTexture((Image)img);
        }
    }
    catch (Exception ex)
    {
        ScriptMessage($"Failed to import file {tileset.Name} (index - {Data.Backgrounds.IndexOf(tileset)}) due to: " + ex.Message);
    }

    IncrementProgress();
}
