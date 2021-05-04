// Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

void MakeFolder(String folderName) 
{
    if (!Directory.Exists(winFolder + folderName + "/"))
        Directory.CreateDirectory(winFolder + folderName + "/");
}

// Overwrite Folder Check One
if (Directory.Exists(winFolder + "EmbeddedTextures\\"))
{
    bool overwriteCheckOne = ScriptQuestion(@"An 'EmbeddedTextures' folder already exists. 
Would you like to remove it? This may some time. 

Note: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.
");
    if (overwriteCheckOne)
        Directory.Delete(winFolder + "EmbeddedTextures\\", true);
    if (!overwriteCheckOne)
    {
        ScriptError("An 'EmbeddedTextures' folder already exists. Please remove it.", "Error: Export already exists.");
        return;
    }
}

MakeFolder("EmbeddedTextures");
string subPath = winFolder + "EmbeddedTextures";
var i = 0;
foreach(var txtr in Data.EmbeddedTextures) 
{
    UndertaleEmbeddedTexture target = txtr as UndertaleEmbeddedTexture;
    try
    {
        File.WriteAllBytes(subPath + "\\" + i + ".png", target.TextureData.TextureBlob);
    }
    catch (Exception ex) 
    {
        ScriptMessage("Failed to export file: " + ex.Message);
    }
    i++;
}