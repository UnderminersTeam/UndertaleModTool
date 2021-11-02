//To-do: Add checks to prevent importing incorrect size sprites

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// At this point, this just imports the sprites.
string importFolder = PromptChooseDirectory("Import From Where");
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

string[] dirFiles = Directory.GetFiles(importFolder);

SetProgressBar(null, "Files", 0, dirFiles.Length);
StartUpdater();

foreach (string file in dirFiles)
{
    IncProgress();

    string fileName = Path.GetFileName(file);
    if (!fileName.EndsWith(".png") || !fileName.Contains("_"))
        continue; // Not an image.
    
    string stripped = Path.GetFileNameWithoutExtension(file);
    
    int lastUnderscore = stripped.LastIndexOf('_');
    string spriteName = stripped.Substring(0, lastUnderscore);
    int frame;
    if (!Int32.TryParse(stripped.Substring(lastUnderscore + 1), out frame))
        throw new ScriptException($"Can't import sprite \"{spriteName}\" - frame number is missing.");
    
    UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
    if (spriteName == null) {
        string error = "Could not find sprite named '" + spriteName + "'. Aborting!";
        ScriptError(error, "Sprite Error");
        SetUMTConsoleText(error);
        SetFinishedMessage(false);
        return;
    }
    
    if (frame < sprite.Textures.Count)
    {
        try
        {
            Bitmap bmp;
            using (var ms = new MemoryStream(TextureWorker.ReadTextureBlob(file)))
            {
                bmp = new Bitmap(ms);
            }
            bmp.SetResolution(96.0F, 96.0F);
            var width = (uint)bmp.Width;
            var height = (uint)bmp.Height;
            var CheckWidth = (uint)(sprite.Textures[frame].Texture.TargetWidth);
            var CheckHeight = (uint)(sprite.Textures[frame].Texture.TargetHeight);
            if ((width != CheckWidth) || (height != CheckHeight))
            {
                string error = "Incorrect dimensions of " + stripped + ". Sprite blurring is very likely in game. Aborting!";
                ScriptError(error, "Unexpected texture dimensions");
                SetUMTConsoleText(error);
                SetFinishedMessage(false);
                return;
            }
            sprite.Textures[frame].Texture.ReplaceTexture(bmp);
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
        string error = fileName + ": Index out of range. Index " + frame.ToString() + " exceeds maximum index (" + ((sprite.Textures.Count) - 1).ToString() + ") of " + spriteName + ". Aborting!";
        ScriptError(error, "Sprite Error");
        SetUMTConsoleText(error);
        SetFinishedMessage(false);
        return;
    }
}

await StopUpdater();
HideProgressBar();
ScriptMessage("Import Complete!");