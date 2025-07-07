// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// Setup root export folder.
string winFolder = Path.GetDirectoryName(FilePath); // The folder data.win is located in.
string embeddedTexturesPath = Path.Combine(winFolder, "EmbeddedTextures");

// Folder Check One
if (!Directory.Exists(embeddedTexturesPath))
{
    ScriptError("There is no 'EmbeddedTextures' folder to import.", "Error: Nothing to import.");
    return;
}

string subPath = embeddedTexturesPath;
int i = 0;
foreach (var txtr in Data.EmbeddedTextures)
{
    UndertaleEmbeddedTexture target = txtr as UndertaleEmbeddedTexture;
    try
    {
        byte[] data = File.ReadAllBytes(Path.Combine(subPath, i + ".png"));
        target.TextureData.TextureBlob = data;
    }
    catch (Exception ex)
    {
        ScriptMessage("Failed to import file number " + i + " due to: " + ex.Message);
    }
    i++;
}