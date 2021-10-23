// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.

int progress = 0;
string subPath = winFolder + "Export_Tilesets";
int i = 0;

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

// Folder Check One
if (!Directory.Exists(winFolder + "Export_Tilesets\\"))
{
    ScriptError("There is no 'Export_Tilesets' folder to import.", "Error: Nothing to import.");
    return;
}

UpdateProgress();

await ImportTilesets();

HideProgressBar();
ScriptMessage("Import Complete.");


void UpdateProgress()
{
    UpdateProgressBar(null, "Tilesets", progress, Data.Backgrounds.Count);
    Interlocked.Increment(ref progress); //"thread-safe" increment
}


async Task ImportTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, ImportTileset));

    progress--;
}

void ImportTileset(UndertaleBackground tileset)
{
    try
    {
        string path = subPath + "\\" + tileset.Name.Content + ".png";
        if (File.Exists(path))
        {
            Bitmap img = new Bitmap(path);
            tileset.Texture.ReplaceTexture((Image)img);
        }
    }
    catch (Exception ex)
    {
        ScriptMessage("Failed to import file number " + i + " due to: " + ex.Message);
    }

    Interlocked.Increment(i);

    UpdateProgress();
}
