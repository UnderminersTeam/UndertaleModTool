using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using ImageMagick;

EnsureDataLoaded();

// At this point, this just imports the sprites.
string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

string[] dirFiles = Directory.GetFiles(importFolder);
List<(string filename, string strippedFilename, string spriteName, UndertaleSprite sprite, int frame)> images = new();

await Task.Run(() =>
{
    // Stop the script if there's missing sprite entries or w/e.
    foreach (string file in dirFiles)
    {
        string filenameWithExtension = Path.GetFileName(file);
        if (!filenameWithExtension.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) || !filenameWithExtension.Contains("_"))
        {
            // Skip all non-images
            continue;
        }

        string stripped = Path.GetFileNameWithoutExtension(file);
        int lastUnderscore = stripped.LastIndexOf('_');
        string spriteName;
        try
        {
            spriteName = stripped.Substring(0, lastUnderscore);
        }
        catch
        {
            throw new ScriptException($"Getting the sprite name of {filenameWithExtension} failed.");
        }

        // Reject non-existing sprites
        UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
        if (sprite is null)
        {
            throw new ScriptException($"{filenameWithExtension} could not be imported, as the sprite \"{spriteName}\" does not exist.");
        }

        // Parse and validate frame number
        if (!int.TryParse(stripped.Substring(lastUnderscore + 1), out int frame))
        {
            throw new ScriptException($"The frame index of {filenameWithExtension} could not be determined (should be an integer).");
        }
        if (frame < 0)
        {
            throw new ScriptException($"The frame index of {filenameWithExtension} appears to be negative (should be 0 or greater).");
        }
        if (frame >= sprite.Textures.Count)
        {
            throw new ScriptException($"The frame index of {filenameWithExtension} is too large (sprite in the data only has {sprite.Textures.Count} frames).");
        }

        // Check that the previous frame exists, if not the first frame
        if (frame > 0)
        {
            int prevframe = frame - 1;
            string prevFrameName = $"{spriteName}_{prevframe}.png";
            if (!File.Exists(Path.Combine(importFolder, prevFrameName)))
            {
                throw new ScriptException($"{spriteName} is missing image index {prevframe} (failed to find {prevFrameName}).");
            }
        }

        // Add to validated image list
        images.Add((file, stripped, spriteName, sprite, frame));
    }
});

SetProgressBar(null, "Files", 0, dirFiles.Length);
StartProgressBarUpdater();

bool errored = false;
await Task.Run(() => 
{
    foreach ((string filename, string strippedFilename, string spriteName, UndertaleSprite sprite, int frame) in images)
    {
        IncrementProgress();

        try
        {
            using MagickImage image = TextureWorker.ReadBGRAImageFromFile(filename);
            UndertaleTexturePageItem item = sprite.Textures[frame].Texture;
            if (image.Width != item.TargetWidth || image.Height != item.TargetHeight)
            {
                // Generic error message when the width/height mismatch
                string error = $"Incorrect dimensions of {strippedFilename}; should be {item.TargetWidth}x{item.TargetHeight}, to fit on the texture page." +
                               "\n\nStopping early. Some sprites may already be modified.";
                if (image.Width == sprite.Width && image.Height == sprite.Height)
                {
                    // Sprite was likely exported with padding - give a more helpful error message
                    error = $"{strippedFilename} appears to be exported with padding. The resulting sprite would be too large to fit in the same space on the texture page. " +
                            "Export the sprite without padding, or use ImportGraphics.csx to import sprites of arbitrary dimensions, on new texture pages." +
                            "\n\nStopping early. Some sprites may already be modified.";
                }
                ScriptError(error, "Unexpected texture dimensions");
                SetUMTConsoleText(error);
                SetFinishedMessage(false);
                errored = true;
                return;
            }

            // Actually replace texture
            item.ReplaceTexture(image);
        }
        catch
        {
            string error = $"{filename} encountered an unknown error during import. " +
                           "Contact the Underminers discord with as much information as possible, the file, and this error message. Aborting!";
            ScriptError(error, "Sprite Error");
            SetUMTConsoleText(error);
            SetFinishedMessage(false);
            errored = true;
            return;
        }
    }
});

await StopProgressBarUpdater();
HideProgressBar();
if (!errored)
{
    ScriptMessage("Import complete!");
}