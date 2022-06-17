// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

// Folder Check One
if (!Directory.Exists(winFolder + "EmbeddedTextures\\"))
{
    ScriptError("There is no 'EmbeddedTextures' folder to import.", "Error: Nothing to import.");
    return;
}

string subPath = winFolder + "EmbeddedTextures";
int i = 0;
foreach (var txtr in Data.EmbeddedTextures) 
{
    UndertaleEmbeddedTexture target = txtr as UndertaleEmbeddedTexture;
    try 
    {
        byte[] data = File.ReadAllBytes(subPath + "\\" + i + ".png");
        target.TextureData.TextureBlob = data;
    }
    catch (Exception ex) 
    {
        ScriptMessage("Failed to import file number " + i + " due to: " + ex.Message);
    }
    i++;
}
