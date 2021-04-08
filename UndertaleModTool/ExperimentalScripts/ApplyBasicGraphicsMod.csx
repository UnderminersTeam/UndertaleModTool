//To-do: Add checks to prevent importing incorrect size sprites

using System;
using System.IO;
using UndertaleModLib.Util;

// At this point, this just imports the sprites.
string importFolder = PromptChooseDirectory("Import From Where");
if (importFolder == null)
    throw new System.Exception("The import folder was not set.");

int progress = 0;
string[] dirFiles = Directory.GetFiles(importFolder);
foreach (string file in dirFiles)
{
    UpdateProgressBar(null, "Files", progress++, dirFiles.Length);
    string fileName = Path.GetFileName(file);
    if (!fileName.EndsWith(".png") || !fileName.Contains("_"))
        continue; // Not an image.
    
    string stripped = Path.GetFileNameWithoutExtension(file);
    
    int lastUnderscore = stripped.LastIndexOf('_');
    string spriteName = stripped.Substring(0, lastUnderscore);
    int frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
    
    UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
    if (spriteName == null) {
        string error = "Could not find sprite named '" + spriteName + "'. Aborting!";
        ScriptError(error, "Sprite Error");
        SetUMTConsoleText(error);
        SetFinishedMessage(false);
        return;
    }
    
    if (frame < sprite.Textures[frame].Length)
    {
        try
        {
            sprite.Textures[frame].Texture.ReplaceTexture(TextureWorker.ReadImageFromFile(file));
        }
        catch
        {
            string error = file + " encountered an unknown error during import. Contact the Underminers discord with as much information as possible, the file, and this error message. Aborting!";
            ScriptError(error, "Sprite Error");
            SetUMTConsoleText(error);
            SetFinishedMessage(false);
            return;
        }
    }
    else
    {
        string error = fileName + ": Index out of range. Index " + frame.ToString() + " exceeds maximum index (" + ((sprite.Textures[frame].Length) - 1).ToString() + ") of " + spriteName + ". Aborting!";
        ScriptError(error, "Sprite Error");
        SetUMTConsoleText(error);
        SetFinishedMessage(false);
        return;
    }
}

HideProgressBar();
ScriptMessage("Import Complete!");