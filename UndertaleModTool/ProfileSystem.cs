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

namespace UndertaleModTool
{
    //Make new profile system file.
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public string GetDecompiledText(string codeName)
        {
            UndertaleCode code = Data.Code.ByName(codeName);
            ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));
            try
            {
                return code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : "";
            }
            catch (Exception e)
            {
                return "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/";
            }
        }
        public string GetDisassemblyText(string codeName)
        {
            try
            {
                UndertaleCode code = Data.Code.ByName(codeName);
                string DisassemblyText = (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : "");
                DisassemblyText = DisassemblyText.Replace("break.e -1", "chkindex.e");
                DisassemblyText = DisassemblyText.Replace("break.e -2", "pushaf.e");
                DisassemblyText = DisassemblyText.Replace("break.e -3", "popaf.e");
                DisassemblyText = DisassemblyText.Replace("break.e -4", "pushac.e");
                DisassemblyText = DisassemblyText.Replace("break.e -5", "setowner.e");
                DisassemblyText = DisassemblyText.Replace("break.e -6", "isstaticok.e");
                DisassemblyText = DisassemblyText.Replace("break.e -7", "setstatic.e");
                return DisassemblyText;
            }
            catch (Exception e)
            {
                return "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"; // Please don't
            }
        }
        public void CrashCheck()
        {
            try
            {
                string ProfilesLocation = ProfilesFolder;
                string LastEditedLocation = Path.Combine(ProfilesLocation, "LastEdited.txt");
                if ((File.Exists(LastEditedLocation) && (Data == null)))
                {
                    DidUMTCrashWhileEditing = true;
                    string LastEditedContents = File.ReadAllText(LastEditedLocation);
                    string[] CrashRecoveryData = LastEditedContents.Split('\n');
                    string DataRecoverLocation = Path.Combine(ProfilesLocation, CrashRecoveryData[0], "Temp");
                    string ProfileHashOfCrashedFile;
                    string ReportedHashOfCrashedFile = CrashRecoveryData[0];
                    string PathOfCrashedFile = CrashRecoveryData[1];
                    string PathOfRecoverableCode = Path.Combine(ProfilesLocation, ReportedHashOfCrashedFile);
                    using (var md5Instance = MD5.Create())
                    {
                        using (var stream = File.OpenRead(PathOfCrashedFile))
                        {
                            ProfileHashOfCrashedFile = BitConverter.ToString(md5Instance.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                        }
                    }
                    if (Directory.Exists(Path.Combine(ProfilesLocation, ReportedHashOfCrashedFile)) && (File.Exists(PathOfCrashedFile)) && (ProfileHashOfCrashedFile == ReportedHashOfCrashedFile))
                    {
                        if (MessageBox.Show("UndertaleModTool crashed during usage last time while editing " + PathOfCrashedFile + ", would you like to recover your code now?", "UndertaleModTool", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            LoadFile(PathOfCrashedFile).ContinueWith((t) => { });
                            string[] dirFiles = Directory.GetFiles(DataRecoverLocation);
                            int progress = 0;
                            LoaderDialog CodeLoadDialog = new LoaderDialog("Script in progress...", "Please wait...");
                            CodeLoadDialog.Update(null, "Code entries processed: ", progress++, dirFiles.Length);
                            CodeLoadDialog.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { })); // Updates the UI, so you can see the progress.
                            foreach (string file in dirFiles)
                            {
                                ImportGMLFile(file);
                                CodeLoadDialog.Update(null, "Code entries processed: ", progress++, dirFiles.Length);
                                CodeLoadDialog.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { })); // Updates the UI, so you can see the progress.
                            }
                            CodeLoadDialog.TryHide();
                            MessageBox.Show("Completed.");
                        }
                        else if (MessageBox.Show("Would you like to move this code to the \"Recovered\" folder now? Any previous code there will be cleared!", "UndertaleModTool", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            MessageBox.Show("Your code can be recovered from the \"Recovered\" folder at any time.");
                            string RecoveredDir = Path.Combine(AppDataFolder, "Recovered", ReportedHashOfCrashedFile);
                            if (Directory.Exists(RecoveredDir))
                                Directory.Delete(RecoveredDir, true);
                            Directory.CreateDirectory(RecoveredDir);
                            Directory.Move(PathOfRecoverableCode, RecoveredDir);
                            ApplyCorrections();
                        }
                        else
                        {
                            MessageBox.Show("A crash has been detected from last session. Please check the Profiles folder for recoverable data now.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("A crash has been detected from last session. Please check the Profiles folder for recoverable data now.");
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("CrashCheck error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void ApplyCorrections()
        {
            try
            {
                DirectoryCopy(CorrectionsFolder, ProfilesFolder, true);
            }
            catch (Exception exc)
            {
                MessageBox.Show("ApplyCorrections error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void CreateUMTLastEdited(string filename)
        {
            try
            {
                string LastEdited = Path.Combine(ProfilesFolder, "LastEdited.txt");
                File.WriteAllText(LastEdited, ProfileHash + "\n" + filename);
            }
            catch (Exception exc)
            {
                MessageBox.Show("CreateUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void DestroyUMTLastEdited()
        {
            try
            {
                if (File.Exists(Path.Combine(ProfilesFolder, "LastEdited.txt")))
                    File.Delete(Path.Combine(ProfilesFolder, "LastEdited.txt"));
            }
            catch (Exception exc)
            {
                MessageBox.Show("DestroyUMTLastEdited error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void RevertProfile()
        {
            try
            {
                //We need to do this regardless, as the "Temp" folder can still change in non-profile mode.
                //If we don't, it could cause desynchronization between modes.
                string MainFolder = Path.Combine(ProfilesFolder, ProfileHash, "Main");
                Directory.CreateDirectory(MainFolder);
                string TempFolder = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
                if (Directory.Exists(TempFolder))
                    Directory.Delete(TempFolder, true);
                DirectoryCopy(MainFolder, TempFolder, true);
            }
            catch (Exception exc)
            {
                MessageBox.Show("RevertProfile error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void SaveTempToMainProfile()
        {
            try
            {
                //This extra step needs to happen for non-profile mode because the "Temp" folder can be modified in non-profile mode.
                //If we don't, it could cause desynchronization between modes.
                if (SettingsWindow.ProfileModeEnabled == "False")
                {
                    string MainFolder = Path.Combine(ProfilesFolder, ProfileHash, "Main");
                    string TempFolder = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
                    Directory.CreateDirectory(TempFolder);
                    if (Directory.Exists(MainFolder))
                        Directory.Delete(MainFolder, true);
                    DirectoryCopy(TempFolder, MainFolder, true);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("SaveTempToMainProfile error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void UpdateProfile(UndertaleData data, string filename)
        {
            try
            {
                using (var md5Instance = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                        MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                        ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                    }
                }
                Directory.CreateDirectory(Path.Combine(ProfilesFolder, ProfileHash, "Main"));
                Directory.CreateDirectory(Path.Combine(ProfilesFolder, ProfileHash, "Temp"));
                if (SettingsWindow.ProfileModeEnabled == "True" && data.GMS2_3)
                {
                    MessageBox.Show("The profile feature is not currently supported for GameMaker 2.3 games.");
                    return;
                }
                else if (SettingsWindow.ProfileModeEnabled == "True" && (!(data.IsYYC())))
                {
                    Directory.CreateDirectory(ProfilesFolder);
                    string ProfDir = Path.Combine(ProfilesFolder, ProfileHash);
                    string ProfDirTemp = Path.Combine(ProfDir, "Temp");
                    string ProfDirMain = Path.Combine(ProfDir, "Main");
                    if (Directory.Exists(ProfDir))
                    {
                        if ((!(Directory.Exists(ProfDirTemp))) && (Directory.Exists(ProfDirMain)))
                        {
                            // Get the subdirectories for the specified directory.
                            DirectoryInfo dir = new DirectoryInfo(ProfDirMain);
                            Directory.CreateDirectory(ProfDirTemp);
                            // Get the files in the directory and copy them to the new location.
                            FileInfo[] files = dir.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                string tempPath = Path.Combine(ProfDirTemp, file.Name);
                                file.CopyTo(tempPath, false);
                            }
                        }
                        else if ((!(Directory.Exists(ProfDirMain))) && (Directory.Exists(ProfDirTemp)))
                        {
                            // Get the subdirectories for the specified directory.
                            DirectoryInfo dir = new DirectoryInfo(ProfDirTemp);
                            Directory.CreateDirectory(ProfDirMain);
                            // Get the files in the directory and copy them to the new location.
                            FileInfo[] files = dir.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                string tempPath = Path.Combine(ProfDirMain, file.Name);
                                file.CopyTo(tempPath, false);
                            }
                        }
                    }
                    //First generation no longer exists, it will be generated on demand while you edit.
                    Directory.CreateDirectory(ProfDir);
                    Directory.CreateDirectory(ProfDirMain);
                    Directory.CreateDirectory(ProfDirTemp);
                    if (!Directory.Exists(ProfDir) || !Directory.Exists(ProfDirMain) || !Directory.Exists(ProfDirTemp))
                    {
                        MessageBox.Show("Profile should exist, but does not. Insufficient permissions??? (Try running in Administrator mode)");
                        MessageBox.Show("Profile mode is disabled.");
                        SettingsWindow.ProfileMessageShown = "False";
                    }
                    if (SettingsWindow.ProfileMessageShown == "False")
                    {
                        MessageBox.Show(@"The profile for your game loaded successfully!

UndertaleModTool now uses the ""Profile"" system by default for code.
Using the profile system, many new features are available to you!
For example, the code is fully editable (you can even add comments)
and it will be saved exactly as you wrote it. In addition, if the
program crashes or your computer loses power during editing, your
code edits will be recovered automatically the next time you start
the program.

The profile system can be toggled on or off at any time by going
to the ""File"" tab at the top and then opening the ""Settings""
(the ""Enable decompile once compile many"" option toggles it
on or off).");
                        SettingsWindow.ProfileMessageShown = "True";
                    }
                    CreateUMTLastEdited(filename);
                }
                else if ((SettingsWindow.ProfileModeEnabled == "False") && (!(data.IsYYC())))
                {
                    return;
                }
                else if (SettingsWindow.ProfileModeEnabled == "True" && data.IsYYC())
                {
                    MessageBox.Show("Profiles are not available for YYC games!");
                    return;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("UpdateProfile error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void ProfileSaveEvent(UndertaleData data, string filename)
        {
            try
            {
                string DeleteIfModeActive = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                bool CopyProfile = false;
                using (var md5Instance = MD5.Create())
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                        if (BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant() != BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant())
                        {
                            CopyProfile = true;
                        }
                    }
                }
                Directory.CreateDirectory(Path.Combine(ProfilesFolder, ProfileHash, "Main"));
                Directory.CreateDirectory(Path.Combine(ProfilesFolder, ProfileHash, "Temp"));
                if (SettingsWindow.ProfileModeEnabled == "False" || data.GMS2_3 || data.IsYYC())
                {
                    MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                    ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                    return;
                }
                else if (SettingsWindow.ProfileModeEnabled == "True")
                {
                    Directory.CreateDirectory(ProfilesFolder);
                    string ProfDir;
                    string MD5DirNameOld;
                    string MD5DirPathOld;
                    string MD5DirPathOldMain;
                    string MD5DirPathOldTemp;
                    string MD5DirNameNew;
                    string MD5DirPathNew;
                    if (CopyProfile)
                    {
                        MD5DirNameOld = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                        MD5DirPathOld = Path.Combine(ProfilesFolder, MD5DirNameOld);
                        MD5DirPathOldMain = Path.Combine(MD5DirPathOld, "Main");
                        MD5DirPathOldTemp = Path.Combine(MD5DirPathOld, "Temp");
                        MD5DirNameNew = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                        MD5DirPathNew = Path.Combine(ProfilesFolder, MD5DirNameNew);
                        DirectoryCopy(MD5DirPathOld, MD5DirPathNew, true);
                        if (Directory.Exists(MD5DirPathOldMain) && Directory.Exists(MD5DirPathOldTemp))
                        {
                            Directory.Delete(MD5DirPathOldTemp, true);
                        }
                        DirectoryCopy(MD5DirPathOldMain, MD5DirPathOldTemp, true);
                    }
                    MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                    // Get the subdirectories for the specified directory.
                    MD5DirNameOld = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                    MD5DirPathOld = Path.Combine(ProfilesFolder, MD5DirNameOld);
                    MD5DirPathOldMain = Path.Combine(MD5DirPathOld, "Main");
                    MD5DirPathOldTemp = Path.Combine(MD5DirPathOld, "Temp");
                    if ((Directory.Exists(MD5DirPathOldMain)) && (Directory.Exists(MD5DirPathOldTemp)) && CopyProfile)
                    {
                        Directory.Delete(MD5DirPathOldMain, true);
                    }
                    DirectoryCopy(MD5DirPathOldTemp, MD5DirPathOldMain, true);

                    ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                    ProfDir = Path.Combine(ProfilesFolder, ProfileHash);
                    Directory.CreateDirectory(ProfDir);
                    Directory.CreateDirectory(Path.Combine(ProfDir, "Main"));
                    Directory.CreateDirectory(Path.Combine(ProfDir, "Temp"));
                    MessageBox.Show("Profile saved successfully to " + ProfileHash);
                }
                if (SettingsWindow.DeleteOldProfileOnSave == "True" && CopyProfile == true)
                {
                    Directory.Delete(Path.Combine(ProfilesFolder, DeleteIfModeActive), true);
                }
                CopyProfile = false;
            }

            catch (Exception exc)
            {
                MessageBox.Show("ProfileSaveEvent error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
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
                    if (!(File.Exists(tempPath)))
                    {
                        try
                        {
                            file.CopyTo(tempPath, false);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An exception occurred while processing copying " + tempPath + "\nException: \n" + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("DirectoryCopy error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public bool AreFilesIdentical(string File01, string File02)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Open the two files.
            fs1 = new FileStream(File01, FileMode.Open);
            fs2 = new FileStream(File02, FileMode.Open);

            // Check the file sizes. If they are not the same, the files
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();
                // Return false to indicate files are different
                return false;
            }
            else
            {
                // Read and compare a byte from each file until either a
                // non-matching set of bytes is found or until the end of
                // file1 is reached.
                do
                {
                    // Read one byte from each file.
                    file1byte = fs1.ReadByte();
                    file2byte = fs2.ReadByte();
                }
                while ((file1byte == file2byte) && (file1byte != -1));

                // Close the files.
                fs1.Close();
                fs2.Close();

                // Return the success of the comparison. "file1byte" is
                // equal to "file2byte" at this point only if the files are
                // the same.
                if ((file1byte - file2byte) == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
