// Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading.Tasks;

EnsureDataLoaded();

const string texturesName = "EmbeddedTextures";

// The folder to write the image data to.
string texturesFolder = Path.Combine(Path.GetDirectoryName(FilePath), texturesName);

if (!CanOverwrite())
    return;

Directory.CreateDirectory(texturesFolder);

SetProgressBar(null, texturesName, 0, Data.EmbeddedTextures.Count);
StartProgressBarUpdater();

await Task.Run(() =>
{
    for (int i = 0; i < Data.EmbeddedTextures.Count; i++)
    {
        try
        {
            using FileStream fs = new(Path.Combine(texturesFolder, $"{i}.png"), FileMode.Create);
            Data.EmbeddedTextures[i].TextureData.Image.SavePng(fs);
        }
        catch (Exception ex)
        {
            ScriptMessage($"Failed to export file: {ex.Message}");
        }

        IncrementProgress();
    }
});

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage($"Export Complete.\n\nLocation: {texturesFolder}");

// Tries to delete the texturesFolder if it doesn't exist. Returns false if the user does not want the folder deleted.
bool CanOverwrite()
{
    // If folder doesn't exist, we're not overwriting anything
    if (!Directory.Exists(texturesFolder)) 
        return true;

    // Prompt user to see if we should delete the folder
    bool overwriteCheckOne = ScriptQuestion($"An '{texturesName}' folder already exists.\nWould you like to remove it? This may some time.\n\nNote: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.");
    if (!overwriteCheckOne)
    {
        ScriptError($"An '{texturesName}' folder already exists. Please remove it.", "Export already exists.");
        return false;
    }
    Directory.Delete(texturesFolder, true);
    return true;
}