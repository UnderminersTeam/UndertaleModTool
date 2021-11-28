// Made by Grossley
// Version 1
// 12/07/2020

using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Get import folder
string importFolder = PromptChooseDirectory("Import From Where");
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
            throw new ScriptException(FileNameWithExtension + " is not the proper size to be imported! Please correct this before importing! The proper dimensions are width: " + Data.Sprites.ByName(spriteName).Width.ToString() + " px, height: " + Data.Sprites.ByName(spriteName).Height.ToString() + " px.");
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
StartUpdater();

await Task.Run(() => {
    foreach (string file in dirFiles)
    {
        IncProgress();

        string FileNameWithExtension = Path.GetFileName(file);
        if (!FileNameWithExtension.EndsWith(".png"))
            continue; // Restarts loop if file is not a valid mask asset.
        string stripped = Path.GetFileNameWithoutExtension(file);
        int lastUnderscore = stripped.LastIndexOf('_');
        string spriteName = stripped.Substring(0, lastUnderscore);
        int frame = Int32.Parse(stripped.Substring(lastUnderscore + 1));
        UndertaleSprite sprite = Data.Sprites.ByName(spriteName);
        int collision_mask_count = sprite.CollisionMasks.Count;
        while (collision_mask_count <= frame)
        {
            sprite.CollisionMasks.Add(sprite.NewMaskEntry());
            collision_mask_count += 1;
        }
        try
        {
            sprite.CollisionMasks[frame].Data = TextureWorker.ReadMaskData(file);
        }
        catch
        {
            throw new ScriptException(FileNameWithExtension + " has an error that prevents its import and so the operation has been aborted! Please correct this before trying again!");
        }
    }
});

await StopUpdater();
HideProgressBar();
ScriptMessage("Import Complete!");
