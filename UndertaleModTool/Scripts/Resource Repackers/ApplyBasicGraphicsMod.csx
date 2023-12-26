using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// At this point, this just imports the sprites.
string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

string[] dirFiles = Directory.GetFiles(importFolder);

//Stop the script if there's missing sprite entries or w/e.
foreach (string file in dirFiles)
{
    string FileNameWithExtension = Path.GetFileName(file);
    if (!FileNameWithExtension.EndsWith(".png"))
        continue; // Restarts loop if file is not a valid mask asset.
    string stripped = Path.GetFileNameWithoutExtension(file);
    int lastUnderscore = stripped.LastIndexOf('_');
    string spriteName = "";
    try
    {
        spriteName = stripped.Substring(0, lastUnderscore);
    }
    catch
    {
        throw new ScriptException("Getting the sprite name of " + FileNameWithExtension + " failed.");
    }
    if (Data.Sprites.ByName(spriteName) == null) // Reject non-existing sprites
    {
        throw new ScriptException(FileNameWithExtension + " could not be imported as the sprite " + spriteName + " does not exist.");
    }
    using (Image img = Image.FromFile(file))
    {
        if ((Data.Sprites.ByName(spriteName).Width != (uint)img.Width) || (Data.Sprites.ByName(spriteName).Height != (uint)img.Height))
        {
            // This check isn't working right
            // throw new ScriptException(FileNameWithExtension + " is not the proper size to be imported! Please correct this before importing! The proper dimensions are width: " + Data.Sprites.ByName(spriteName).Width.ToString() + " px, height: " + Data.Sprites.ByName(spriteName).Height.ToString() + " px.");
        }
    }

    Int32 validFrameNumber = 0;
    try
    {
        validFrameNumber = Int32.Parse(stripped.Substring(lastUnderscore + 1));
    }
    catch
    {
        throw new ScriptException("The index of " + FileNameWithExtension + " could not be determined.");
    }
    int frame = 0;
    try
    {
        frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
    }
    catch
    {
        throw new ScriptException(FileNameWithExtension + " is using letters instead of numbers. The script has stopped for your own protection.");
    }
    int prevframe = 0;
    if (frame != 0)
    {
        prevframe = (frame - 1);
    }
    if (frame < 0)
    {
        throw new ScriptException(spriteName + " is using an invalid numbering scheme. The script has stopped for your own protection.");
    }
    var prevFrameName = spriteName + "_" + prevframe.ToString() + ".png";
    string[] previousFrameFiles = Directory.GetFiles(importFolder, prevFrameName);
    if (previousFrameFiles.Length < 1)
        throw new ScriptException(spriteName + " is missing one or more indexes. The detected missing index is: " + prevFrameName);
}

SetProgressBar(null, "Files", 0, dirFiles.Length);
StartProgressBarUpdater();

await Task.Run(() => {
    foreach (string file in dirFiles)
    {
        IncrementProgress();

        string fileName = Path.GetFileName(file);
        if (!fileName.EndsWith(".png") || !fileName.Contains("_"))
            continue; // Not an image.

        string stripped = Path.GetFileNameWithoutExtension(file);

        int lastUnderscore = stripped.LastIndexOf('_');
        string spriteName = stripped.Substring(0, lastUnderscore);
        int frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));

        UndertaleSprite sprite = Data.Sprites.ByName(spriteName);

        if (frame < sprite.Textures.Count)
        {
            try
            {
                SKBitmap bmp = SKBitmap.Decode(file);
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
});

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Import Complete!");