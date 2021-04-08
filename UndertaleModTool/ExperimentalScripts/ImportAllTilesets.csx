// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

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
    UpdateProgressBar(null, "Tilesets", progress++, Data.Backgrounds.Count);
}


async Task ImportTilesets()
{
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, ImportTileset));
}

void ImportTileset(UndertaleBackground tileset)
{
    UndertaleBackground target = tileset as UndertaleBackground;
    try
    {
        Bitmap img = new Bitmap(subPath + "\\" + tileset.Name.Content + ".png");
        target.Texture.ReplaceTexture((Image)img);
    }
    catch (Exception ex)
    {
        ScriptMessage("Failed to import file number " + i + " due to: " + ex.Message);
    }
    i++;
    UpdateProgress();
}
