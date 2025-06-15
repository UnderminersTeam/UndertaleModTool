using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Setup root export folder.
string embeddedTexturesPath = PromptChooseDirectory();
if (embeddedTexturesPath is null)
{
    throw new ScriptException("The import folder was not set.");
}

string subPath = embeddedTexturesPath;
int i = 0;
foreach (UndertaleEmbeddedTexture target in Data.EmbeddedTextures) 
{
    if (target is null)
    {
        i++;
        continue;
    }
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
