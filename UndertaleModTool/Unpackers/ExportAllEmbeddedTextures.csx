// Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading.Tasks;

EnsureDataLoaded();

string winFolder = GetFolder(FilePath); // The folder data.win is located in.
string EmbFolder = Path.Combine(winFolder, "EmbeddedTextures"); // The folder to write the image data to.

if (!CanOverwrite())
    return;

MakeFolder("EmbeddedTextures");

SetProgressBar(null, "Embedded textures", 0, Data.EmbeddedTextures.Count);
StartProgressBarUpdater();

await Task.Run(() => {
    for (var i = 0; i < Data.EmbeddedTextures.Count; i++)
    {
        try
        {
            File.WriteAllBytes(Path.Combine(EmbFolder, i + ".png"), Data.EmbeddedTextures[i].TextureData.TextureBlob);
        }
        catch (Exception ex)
        {
            ScriptMessage("Failed to export file: " + ex.Message);
        }

        IncrementProgress();
    }
});

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + EmbFolder);

/* Helper functions below.
*/

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

void MakeFolder(String folderName)
{
    string MakeFolderPath = Path.Combine(winFolder, folderName);
    if (!Directory.Exists(MakeFolderPath))
        Directory.CreateDirectory(MakeFolderPath);
}

bool CanOverwrite()
{
    // Overwrite Folder Check One
    if (Directory.Exists(EmbFolder))
    {
        bool overwriteCheckOne = ScriptQuestion("An 'EmbeddedTextures' folder already exists.\r\nWould you like to remove it? This may some time.\r\n\r\nNote: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.\r\n");
        if (!overwriteCheckOne)
        {
            ScriptError("An 'EmbeddedTextures' folder already exists. Please remove it.", "Error: Export already exists.");
            return false;
        }
        Directory.Delete(EmbFolder, true);
        return true;
    }
    return true;
}