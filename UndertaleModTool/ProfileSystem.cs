using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using System.Security.Cryptography;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;
using System.Threading.Tasks;
using System.Text;
using Underanalyzer.Decompiler;

namespace UndertaleModTool
{
    // Handles a majority of profile-system functionality

    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null, IDecompileSettings settings = null)
        {
            return GetDecompiledText(Data.Code.ByName(codeName), context, settings);
        }
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null, IDecompileSettings settings = null)
        {
            if (code.ParentEntry is not null)
                return $"// This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", decompile that instead.";

            GlobalDecompileContext globalDecompileContext = context is null ? new(Data) : context;
            try
            {
                return code != null 
                    ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, settings ?? Data.ToolInfo.DecompilerSettings).DecompileToString()
                    : "";
            }
            catch (Exception e)
            {
                return "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/";
            }
        }

        public string GetDisassemblyText(UndertaleCode code)
        {
            if (code.ParentEntry is not null)
                return $"; This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", disassemble that instead.";

            try
            {
                return code != null ? code.Disassemble(Data.Variables, Data.CodeLocals?.For(code), Data.CodeLocals is null) : "";
            }
            catch (Exception e)
            {
                return "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"; // Please don't
            }
        }
        public string GetDisassemblyText(string codeName)
        {
            return GetDisassemblyText(Data.Code.ByName(codeName));
        }

        public void CrashCheck()
        {
            if (!SettingsWindow.ProfileModeEnabled)
            {
                return;
            }

            try
            {
                string lastEditedLocation = Path.Combine(ProfilesFolder, "LastEdited.txt");
                if (Data == null && File.Exists(lastEditedLocation))
                {
                    CrashedWhileEditing = true;
                    string[] crashRecoveryData = File.ReadAllText(lastEditedLocation).Split('\n');
                    string dataRecoverLocation = Path.Combine(ProfilesFolder, crashRecoveryData[0].Trim(), "Temp");
                    string profileHashOfCrashedFile;
                    string reportedHashOfCrashedFile = crashRecoveryData[0].Trim();
                    string pathOfCrashedFile = crashRecoveryData[1];
                    string pathOfRecoverableCode = Path.Combine(ProfilesFolder, reportedHashOfCrashedFile);
                    if (File.Exists(pathOfCrashedFile))
                    {
                        using (var md5Instance = MD5.Create())
                        {
                            using (var stream = File.OpenRead(pathOfCrashedFile))
                            {
                                profileHashOfCrashedFile = BitConverter.ToString(md5Instance.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                            }
                        }
                        if (Directory.Exists(Path.Combine(ProfilesFolder, reportedHashOfCrashedFile)) &&
                            profileHashOfCrashedFile == reportedHashOfCrashedFile)
                        {
                            if (this.ShowQuestion("UndertaleModTool crashed during usage last time while editing " + pathOfCrashedFile + ". Profile mode code from that session still exists. Would you like to move the code to the \"Recovered\" folder now? Any previous code there will be cleared!") == MessageBoxResult.Yes)
                            {
                                this.ShowMessage("Your code can be recovered from the \"Recovered\" folder at any time.");
                                string recoveredDir = Path.Combine(AppDataFolder, "Recovered", reportedHashOfCrashedFile);
                                if (!Directory.Exists(Path.Combine(AppDataFolder, "Recovered")))
                                    Directory.CreateDirectory(Path.Combine(AppDataFolder, "Recovered"));
                                if (Directory.Exists(recoveredDir))
                                    Directory.Delete(recoveredDir, true);
                                Directory.Move(pathOfRecoverableCode, recoveredDir);
                                ApplyCorrections();
                            }
                            else
                            {
                                this.ShowWarning("A crash has been detected from last session. Please check the Profiles folder for recoverable data now.");
                            }
                        }
                    }
                    else
                    {
                        this.ShowWarning("A crash has been detected from last session. Please check the Profiles folder for recoverable data now.");
                    }
                }
            }
            catch (Exception exc)
            {
                this.ShowError("CrashCheck error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public void ApplyCorrections()
        {
            if (!SettingsWindow.ProfileModeEnabled)
            {
                return;
            }

            try
            {
                DirectoryCopy(CorrectionsFolder, ProfilesFolder, true);
            }
            catch (Exception exc)
            {
                this.ShowError("ApplyCorrections error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public void CreateUMTLastEdited(string filename)
        {
            if (!SettingsWindow.ProfileModeEnabled || ProfileHash is null)
            {
                return;
            }

            try
            {
                File.WriteAllText(Path.Combine(ProfilesFolder, "LastEdited.txt"), ProfileHash + "\n" + filename);
            }
            catch (Exception exc)
            {
                this.ShowError("CreateUMTLastEdited error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public void DestroyUMTLastEdited()
        {
            if (!SettingsWindow.ProfileModeEnabled)
            {
                return;
            }

            try
            {
                string path = Path.Combine(ProfilesFolder, "LastEdited.txt");
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception exc)
            {
                this.ShowError("DestroyUMTLastEdited error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }

        public void RevertProfile()
        {
            if (!SettingsWindow.ProfileModeEnabled || ProfileHash is null)
            {
                return;
            }

            try
            {
                string mainFolder = Path.Combine(ProfilesFolder, ProfileHash, "Main");
                Directory.CreateDirectory(mainFolder);
                string tempFolder = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
                DirectoryCopy(mainFolder, tempFolder, true);
            }
            catch (Exception exc)
            {
                this.ShowError("RevertProfile error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }
        public async Task UpdateProfile(UndertaleData data, string filename)
        {
            if (!SettingsWindow.ProfileModeEnabled)
            {
                return;
            }

            string fileext = Path.GetExtension(filename);

            //this.ShowMessage(filename);

            if (fileext != ".dat") try
            {
                FileMessageEvent?.Invoke("Calculating MD5 hash...");

                await Task.Run(() =>
                {
                    using (var md5Instance = MD5.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                            MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                            remMD5 = MD5PreviouslyLoaded;

                            String Input_text = "";
                            if (SettingsWindow.ProfileModeEnabled && SettingsWindow.CustomProfileName == true)
                            {
                                if (SettingsWindow.RememberProfileName)
                                {
                                    var MD5DirName = CurProfileName;
                                    var MD5DirPath = Path.Combine(ProfilesFolder, MD5DirName);
                                    var FileDir = "";
                                    string[] iwishiwasbetteratnames = filename.Split(new char[] { '\\' });
                                    var directoriesamt = iwishiwasbetteratnames.Length;
                                    for (var i = 0; i < directoriesamt - 1; i++)
                                    {
                                        FileDir += iwishiwasbetteratnames[i] + "\\";
                                    }
                                    if (File.Exists(FileDir + "Profiles\\directory.txt"))
                                    { 
                                        FileDir += "Profiles\\directory.txt";

                                        var GetThisDir = File.ReadAllText(FileDir);

                                        if (File.Exists(GetThisDir))
                                            Input_text = File.ReadAllText(GetThisDir);
                                        else
                                            Input_text = SimpleTextInput("Loading Profile, please enter a Profile name.", "(Leaving this blank will name the profile with the data's MD5 hash.)", Input_text, true);
                                        //this.ShowMessage(Input_text);
                                    }
                                    else
                                        Input_text = SimpleTextInput("Loading Profile, please enter a Profile name.", "(Leaving this blank will name the profile with the data's MD5 hash.)", Input_text, true);
                                }
                                else
                                    Input_text = SimpleTextInput("Loading Profile, please enter a Profile name.", "(Leaving this blank will name the profile with the data's MD5 hash.)", Input_text, true);
                            }

                            if (Input_text == "")
                            {
                                ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                                is_string = false;
                            }
                            else
                            {
                                byte[] idk = Encoding.ASCII.GetBytes(Input_text);
                                MD5PreviouslyLoaded = idk;
                                MD5CurrentlyLoaded = idk;

                                ProfileHash = Input_text;
                                is_string = true;
                            }
                            CurProfileName = ProfileHash;
                            //this.ShowMessage(CurProfileName);
                            CurrentProfileName = "- Current Profile: " + "\"" + CurProfileName + "\"";
                        }
                    }
                });

                /*await Task.Run(() =>
                {
                    using (var md5Instance = MD5.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                            MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                            remMD5 = MD5PreviouslyLoaded;

                            String Input_text = "";
                            if (SettingsWindow.ProfileModeEnabled && SettingsWindow.CustomProfileName == true)
                                Input_text = SimpleTextInput("Loading Profile, please enter a Profile name.", "(Leaving this blank will name the profile with the data's MD5 hash.)", Input_text, true);
                            ProfileHash = Input_text;
                            CurProfileName = ProfileHash;
                            if (ProfileHash == "")
                            {
                                ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                                CurProfileName = ProfileHash;
                                is_string = false;
                            }
                            else
                            {
                                byte[] idk = Encoding.ASCII.GetBytes(Input_text);
                                MD5PreviouslyLoaded = idk;
                                MD5CurrentlyLoaded = idk;
                                ProfileHash = Input_text;
                                is_string = true;
                            }
                            CurrentProfileName = "- Current Profile: " + "\"" + CurProfileName + "\"";
                            this.ShowMessage(ProfileHash);
                        }
                    }
                });*/

                string profDir = Path.Combine(ProfilesFolder, ProfileHash);
                string profDirTemp = Path.Combine(profDir, "Temp");
                string profDirMain = Path.Combine(profDir, "Main");
                Directory.CreateDirectory(ProfilesFolder);
                    if (SettingsWindow.ProfileModeEnabled)
                    {
                        if (Directory.Exists(profDir))
                        {
                            string[] Files = Directory.GetFiles(profDir + "\\Temp");
                            if (!Directory.Exists(profDir + "\\Temp"))
                                Directory.CreateDirectory(profDir + "\\Temp");
                            for (var i = 0; i < Files.Length; i++)
                            {
                                if (File.Exists(Files[i]))
                                    File.Delete(Files[i]);
                            }
                            DirectoryCopy(Path.Combine(profDir, "Main"), Path.Combine(profDir, "Temp"), true);
                        }
                        if (!Directory.Exists(profDirTemp) && Directory.Exists(profDirMain))
                        {
                            // Get the subdirectories for the specified directory.
                            DirectoryInfo dir = new DirectoryInfo(profDirMain);
                            Directory.CreateDirectory(profDirTemp);
                            // Get the files in the directory and copy them to the new location.
                            FileInfo[] files = dir.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                string tempPath = Path.Combine(profDirTemp, file.Name);
                                file.CopyTo(tempPath, false);
                            }
                        }
                        else if (!Directory.Exists(profDirMain) && Directory.Exists(profDirTemp))
                        {
                            // Get the subdirectories for the specified directory.
                            DirectoryInfo dir = new DirectoryInfo(profDirTemp);
                            Directory.CreateDirectory(profDirMain);
                            // Get the files in the directory and copy them to the new location.
                            FileInfo[] files = dir.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                string tempPath = Path.Combine(profDirMain, file.Name);
                                file.CopyTo(tempPath, false);
                            }
                        }
                    }

                // First generation no longer exists, it will be generated on demand while you edit.
                Directory.CreateDirectory(profDir);
                Directory.CreateDirectory(profDirMain);
                Directory.CreateDirectory(profDirTemp);
                if (!Directory.Exists(profDir) || !Directory.Exists(profDirMain) || !Directory.Exists(profDirTemp))
                {
                    this.ShowWarning("Profile should exist, but does not. Insufficient permissions? Profile mode is disabled.");
                    SettingsWindow.ProfileModeEnabled = false;
                    return;
                }

                if (!SettingsWindow.ProfileMessageShown)
                {
                    this.ShowMessage(@"The profile for your game loaded successfully!

Using the profile system, many new features are available to you!
For example, the code is fully editable (you can even add comments)
and it will be saved exactly as you wrote it. In addition, if the
program crashes or your computer loses power during editing, your
code edits will be recovered automatically the next time you start
the program.

The profile system can be toggled on or off at any time by going
to the ""File"" tab at the top and then opening the ""Settings""
(the ""Enable profile mode"" option toggles it on or off).
You may wish to disable it for purposes such as collaborative
modding projects, or when performing technical operations.
Be warned that scripts are likely to mess with this system,
and that enabling the profile mode setting won't have an immediate
effect. (You must re-open a game first.)

It should be noted that this system is somewhat experimental, so
should you encounter any problems, please let us know or leave
an issue on GitHub.");
                    SettingsWindow.ProfileMessageShown = true;
                }
                CreateUMTLastEdited(filename);
            }
            catch (Exception exc)
            {
                this.ShowError("UpdateProfile error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }
        public async Task ProfileSaveEvent(UndertaleData data, string filename)
        {
            if (!SettingsWindow.ProfileModeEnabled || ProfileHash is null)
            {
                return;
            }

            string fileext = Path.GetExtension(filename);

            if (fileext != ".dat") try
            {
                String Input_text = "";
                if (SettingsWindow.ProfileModeEnabled == true && SettingsWindow.CustomProfileName == true)
                {
                    if (this.ShowQuestion("Do you want to save to the current Profile?") == MessageBoxResult.No)
                    {
                        Input_text = SimpleTextInput("Saving Profile, please enter a Profile name.", "(Leaving this blank will name the profile with the data's MD5 hash.)", Input_text, true);
                    }
                    else
                    {
                        Input_text = CurProfileName;
                    }
                }
                is_string = false;
                if (Input_text != "")
                {
                    is_string = true;
                    byte[] __name = Encoding.ASCII.GetBytes(CurProfileName);
                    MD5PreviouslyLoaded = __name;
                }
                string deleteIfModeActive = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                bool copyProfile = false;
                await Task.Run(() =>
                {
                    using var md5Instance = MD5.Create();
                    using var stream = File.OpenRead(filename);
                    MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                    if (!BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").Equals(BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", ""), StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (var stream2 = File.OpenRead(filename))
                        {
                            MD5CurrentlyLoaded = md5Instance.ComputeHash(stream2);
                            if (is_string == true)
                            {
                                byte[] idk = Encoding.ASCII.GetBytes(Input_text);
                                MD5CurrentlyLoaded = idk;
                            }
                            if (deleteIfModeActive != BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant())
                            {
                                copyProfile = true;
                            }
                        }
                    }
                });

                Directory.CreateDirectory(Path.Combine(ProfilesFolder, ProfileHash, "Main"));
                Directory.CreateDirectory(Path.Combine(ProfilesFolder, ProfileHash, "Temp"));
                var Oldname = "";

                string profDir;
                string MD5DirNameOld;
                string MD5DirPathOld;
                string MD5DirPathOldMain;
                string MD5DirPathOldTemp;
                string MD5DirNameNew;
                string MD5DirPathNew;
                string MD5DirPathNewTemp;

                if (!SettingsWindow.ProfileModeEnabled || data.IsYYC())
                {
                    MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                    if (is_string == true)
                    {
                        byte[] idk = Encoding.ASCII.GetBytes(CurProfileName);
                        MD5PreviouslyLoaded = idk;
                    }
                    ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                    return;
                }
                else if (SettingsWindow.ProfileModeEnabled)
                {
                    Directory.CreateDirectory(ProfilesFolder);
                    bool old_is_string = is_string;
                    // Get the subdirectories for the specified directory.
                    if (!is_string)
                    {
                        MD5DirNameOld = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                        MD5DirPathOld = Path.Combine(ProfilesFolder, MD5DirNameOld);
                        MD5DirPathOldMain = Path.Combine(MD5DirPathOld, "Main");
                        MD5DirPathOldTemp = Path.Combine(MD5DirPathOld, "Temp");
                    }
                    else
                    {
                        MD5DirNameOld = CurProfileName;
                        MD5DirPathOld = Path.Combine(ProfilesFolder, MD5DirNameOld);
                        MD5DirPathOldMain = Path.Combine(MD5DirPathOld, "Main");
                        MD5DirPathOldTemp = Path.Combine(MD5DirPathOld, "Temp");
                    }
                    if ((Directory.Exists(MD5DirPathOldMain)) && (Directory.Exists(MD5DirPathOldTemp)) && copyProfile)
                    {
                        Directory.Delete(MD5DirPathOldMain, true);
                    }
                    DirectoryCopy(MD5DirPathOldTemp, MD5DirPathOldMain, true);
                    if (copyProfile)
                    {
                        if (!is_string)
                            MD5DirNameOld = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                        else
                            MD5DirNameOld = CurProfileName;
                        MD5DirPathOld = Path.Combine(ProfilesFolder, MD5DirNameOld);
                        MD5DirPathOldMain = Path.Combine(MD5DirPathOld, "Main");
                        MD5DirPathOldTemp = Path.Combine(MD5DirPathOld, "Temp");
                        if (!is_string)
                            MD5DirNameNew = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                        else
                            MD5DirNameNew = Input_text;
                        MD5DirPathNew = Path.Combine(ProfilesFolder, MD5DirNameNew);
                        MD5DirPathNewTemp = Path.Combine(MD5DirPathNew, "Temp");
                        DirectoryCopy(MD5DirPathOld, MD5DirPathNew, true);
                        if (Directory.Exists(MD5DirPathOldMain) && Directory.Exists(MD5DirPathOldTemp))
                        {
                            Directory.Delete(MD5DirPathOldTemp, true);
                        }
                        DirectoryCopy(MD5DirPathOldMain, MD5DirPathOldTemp, true);
                    }
                    MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                    ProfileHash = Input_text;
                    Oldname = CurProfileName;
                    CurProfileName = ProfileHash;
                    if (ProfileHash == "")
                    {
                        ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                        CurProfileName = ProfileHash;
                        is_string = false;
                    }
                    else
                    {
                        byte[] idk = Encoding.ASCII.GetBytes(Input_text);
                        MD5PreviouslyLoaded = idk;
                        ProfileHash = Input_text;
                        is_string = true;
                    }
                    CurrentProfileName = "- Current Profile: " + "\"" + CurProfileName + "\"";
                    profDir = Path.Combine(ProfilesFolder, ProfileHash);
                    Directory.CreateDirectory(profDir);
                    Directory.CreateDirectory(Path.Combine(profDir, "Main"));
                    Directory.CreateDirectory(Path.Combine(profDir, "Temp"));
                    string[] Files = Directory.GetFiles(profDir + "\\Main");
                    for (var i = 0; i < Files.Length; i++)
                    {
                        File.Delete(Files[i]);
                    }
                    DirectoryCopy(Path.Combine(profDir, "Temp"), Path.Combine(profDir, "Main"), true);
                    this.ShowMessage("Profile saved successfully to " + "\"" + CurProfileName + "\"");
                }
                if (SettingsWindow.DeleteOldProfileOnSave && copyProfile)
                {
                    //this.ShowMessage(Oldname);
                    Directory.Delete(Path.Combine(ProfilesFolder, Oldname), true);
                }
            }
            catch (Exception exc)
            {
                this.ShowError("ProfileSaveEvent error! (Note that profile mode is highly experimental.)\n" + exc);
            }
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
                this.ShowError("DirectoryCopy error! (Note that profile mode is highly experimental.)\n" + exc);
            }
        }
    }
}
