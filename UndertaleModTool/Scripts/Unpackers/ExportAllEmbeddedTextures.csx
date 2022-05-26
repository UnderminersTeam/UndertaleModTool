// Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading.Tasks;

EnsureDataLoaded();

const string texturesName = "EmbeddedTextures";

// The folder data.win is located in.
string dataFolder = Path.GetDirectoryName(FilePath);
// The folder to write the image data to.
string texturesFolder = Path.Combine(FilePath, texturesName);

if (!CanOverwrite())
    return;

MakeFolder(texturesFolder);

SetProgressBar(null, texturesName, 0, Data.EmbeddedTextures.Count);
StartProgressBarUpdater();

await Task.Run(() =>
{
    for (int i = 0; i < Data.EmbeddedTextures.Count; i++)
    {
        try
        {
            File.WriteAllBytes(Path.Combine(texturesFolder, i + ".png"), Data.EmbeddedTextures[i].TextureData.TextureBlob);
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
ScriptMessage("Export Complete.\n\nLocation: " + texturesFolder);

// Helper functions below. //

// Gets the full directory of a file path
string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

// Creates the folder, if it does not exist already
void MakeFolder(string folder)
{
    if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);
}

// Tries to delete the texturesFolder if it doesn't exist. Returns false if the user does not want the folder deleted.
bool CanOverwrite()
{
    // Overwrite Folder Check One
    if (!Directory.Exists(texturesFolder)) return true;

    bool overwriteCheckOne = ScriptQuestion($"An '{texturesName}' folder already exists.\nWould you like to remove it? This may some time.\n\nNote: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.");
    if (!overwriteCheckOne)
    {
        ScriptError($"An '{texturesName}' folder already exists. Please remove it.", "Export already exists.");
        return false;
    }
    Directory.Delete(texturesFolder, true);
    return true;
}