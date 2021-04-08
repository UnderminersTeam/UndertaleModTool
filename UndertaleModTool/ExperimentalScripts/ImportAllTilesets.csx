// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.

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

string subPath = winFolder + "Export_Tilesets";
int i = 0;
foreach (var tileset in Data.Backgrounds) 
{
    UndertaleBackground target = tileset as UndertaleBackground;
    try 
    {
        //byte[] data = File.ReadAllBytes(subPath + "\\" + i + ".png");
        Bitmap img = new Bitmap(subPath + "\\" + tileset.Name.Content + ".png");
        target.Texture.ReplaceTexture((Image)img);
    }
    catch (Exception ex) 
    {
	    ScriptMessage("Failed to import file number " + i + " due to: " + ex.Message);
    }
    i++;
}
