using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Get import folder.
string importFolder = PromptChooseDirectory();
if (importFolder is null)
{
    throw new ScriptException("The import folder was not set.");
}

// Find all files in the folder.
string[] dirFiles = Directory.GetFiles(importFolder, "*.png");

// Stop the script if there's missing sprite entries, or invalid data.
foreach (string file in dirFiles)
{
    string fileNameWithExtension = Path.GetFileName(file);

    // Get sprite name from filename.
    string stripped = Path.GetFileNameWithoutExtension(file);
    int lastUnderscore = stripped.LastIndexOf('_');
    string spriteName = "";
    try
    {
        spriteName = stripped.Substring(0, lastUnderscore);
    }
    catch
    {
        throw new ScriptException($"Getting the sprite name of {fileNameWithExtension} failed.");
    }

    // Validate width/height based on existing sprite width/height.
    UndertaleSprite foundSprite = Data.Sprites.ByName(spriteName);
    if (foundSprite is null)
    {
        throw new ScriptException($"{fileNameWithExtension} could not be imported as the sprite {spriteName} does not exist.");
    }
    (int imgWidth, int imgHeight) = TextureWorker.GetImageSizeFromFile(file);
    (int expectedMaskWidth, int expectedMaskHeight) = foundSprite.CalculateMaskDimensions(Data);
    if (expectedMaskWidth != imgWidth || expectedMaskHeight != imgHeight)
    {
        throw new ScriptException($"{fileNameWithExtension} is not the proper size to be imported! Please correct this before importing! The proper dimensions are width: {expectedMaskWidth} px, height: {expectedMaskHeight} px.");
    }

    // Determine frame number and validate it.
    int validFrameNumber = 0;
    try
    {
        validFrameNumber = int.Parse(stripped.Substring(lastUnderscore + 1));
    }
    catch
    {
        throw new ScriptException($"The index of {fileNameWithExtension} could not be determined.");
    }
    int frame = 0;
    try
    {
        frame = int.Parse(stripped.Substring(lastUnderscore + 1));
    }
    catch
    {
        throw new ScriptException($"{fileNameWithExtension} is using letters instead of numbers. The script has stopped for your own protection.");
    }
    if (frame == 0)
    {
        continue;
    }
    if (frame < 0)
    {
        throw new ScriptException($"{spriteName} is using an invalid numbering scheme. The script has stopped for your own protection.");
    }
    int prevFrame = frame - 1;
    string prevFrameName = $"{spriteName}_{prevFrame}.png";
    string[] previousFrameFiles = Directory.GetFiles(importFolder, prevFrameName);
    if (previousFrameFiles.Length < 1)
    {
        throw new ScriptException($"{spriteName} is missing one or more indexes. The detected missing index is: {prevFrameName}");
    }
}

SetProgressBar(null, "Files", 0, dirFiles.Length);
StartProgressBarUpdater();

await Task.Run(() => 
{
    foreach (string file in dirFiles)
    {
        IncrementProgress();

        string fileNameWithExtension = Path.GetFileName(file);

        // Get sprite name from filename.
        string stripped = Path.GetFileNameWithoutExtension(file);
        int lastUnderscore = stripped.LastIndexOf('_');
        string spriteName = stripped.Substring(0, lastUnderscore);
        int frame = int.Parse(stripped.Substring(lastUnderscore + 1));
        UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
        int collisionMaskCount = sprite.CollisionMasks.Count;
        while (collisionMaskCount <= frame)
        {
            sprite.CollisionMasks.Add(sprite.NewMaskEntry(Data));
            collisionMaskCount += 1;
        }

        // Import the mask.
        (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(Data);
        sprite.CollisionMasks[frame].Data = TextureWorker.ReadMaskData(file, maskWidth, maskHeight);
    }
});

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Import Complete!");
