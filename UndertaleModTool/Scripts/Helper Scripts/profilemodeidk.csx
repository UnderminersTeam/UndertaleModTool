using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();
namespace UndertaleModTool
{
    private static MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
    string ProfileHash = mainWindow.ProfileHash;
    public string MainPath = Path.Combine(Settings.ProfilesFolder, mainWindow.ProfileHash, "Main");
    public string TempPath = Path.Combine(Settings.ProfilesFolder, mainWindow.ProfileHash, "Temp");
    string ProfilesFolder = Path.Combine(Settings.AppDataFolder, "Profiles");
    string Profilefolder = GetFolder(FilePath) + "Profile" + Path.DirectorySeparatorChar;
}
public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
{
    try
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the destination directory doesn't exist, create it.
        Directory.CreateDirectory(destDirName);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destDirName, file.Name);
            if (!File.Exists(tempPath))
            {
                try
                {
                    file.CopyTo(tempPath, false);
                }
                catch (Exception ex)
                {
                    this.ShowError("An exception occurred while processing copying " + tempPath + "\nException: \n" + ex);
                    return;
                }
            }
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }
    catch (Exception exc)
    {
        this.ShowError("DirectoryCopy error! Send this to Grossley#2869 and make an issue on Github\n" + exc);
    }
}
string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

string mainFolder = Path.Combine(ProfilesFolder, ProfileHash, "Main");
string tempFolder = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
DirectoryCopy(mainFolder, Profilefolder, true);
DirectoryCopy(tempFolder, Profilefolder, true);

