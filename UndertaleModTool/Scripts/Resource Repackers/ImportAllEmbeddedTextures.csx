// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// Setup root export folder.
string embeddedTexturesPath = Path.Combine(Path.GetDirectoryName(FilePath), "EmbeddedTextures");

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
    string filename = $"{i}.png";
    try
    {
        target.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(Path.Combine(subPath, filename)))
                                          .ConvertToFormat(target.TextureData.Image.Format);
    }
    catch (Exception ex) 
    {
        ScriptMessage($"Failed to import {filename}: {ex.Message}");
    }
    i++;
}

ScriptMessage("Import complete.");
