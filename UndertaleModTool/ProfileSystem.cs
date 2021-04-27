using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;
using UndertaleModLib.Scripting;
using UndertaleModTool.Windows;
using System.Security.Cryptography;

namespace UndertaleModTool
{
    //Make new profile system file.
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public void CrashCheck()
        {
            string ProfilesLocation = ProfilesFolder;
            string LastEditedLocation = ProfilesLocation + "LastEdited.txt";
            if ((File.Exists(LastEditedLocation) && (Data == null)))
            {
                DidUMTCrashWhileEditing = true;
                string LastEditedContents = File.ReadAllText(LastEditedLocation);
                string[] CrashRecoveryData = LastEditedContents.Split('\n');
                string DataRecoverLocation = System.IO.Path.Combine(ProfilesLocation, CrashRecoveryData[0], "Temp");
                string ProfileHashOfCrashedFile;
                string ReportedHashOfCrashedFile = CrashRecoveryData[0];
                string PathOfCrashedFile = CrashRecoveryData[1];
                string PathOfRecoverableCode = System.IO.Path.Combine(ProfilesLocation, ReportedHashOfCrashedFile);
                using (var md5Instance = MD5.Create())
                {
                    using (var stream = File.OpenRead(PathOfCrashedFile))
                    {
                        ProfileHashOfCrashedFile = BitConverter.ToString(md5Instance.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    }
                }
                if (Directory.Exists(ProfilesLocation + ReportedHashOfCrashedFile) && (File.Exists(PathOfCrashedFile)) && (ProfileHashOfCrashedFile == ReportedHashOfCrashedFile))
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
                        string RecoveredDir = System.IO.Path.Combine(AppDataFolder, "Recovered", ReportedHashOfCrashedFile);
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
        public void ApplyCorrections()
        {
            DirectoryCopy(CorrectionsFolder, ProfilesFolder, true);
        }
        public void CreateUMTLastEdited(string filename)
        {
            string LastEdited = System.IO.Path.Combine(ProfilesFolder, "LastEdited.txt");
            File.WriteAllText(LastEdited, ProfileHash + "\n" + filename);
        }
        public void DestroyUMTLastEdited()
        {
            if (File.Exists(System.IO.Path.Combine(ProfilesFolder, "LastEdited.txt")))
                File.Delete(System.IO.Path.Combine(ProfilesFolder, "LastEdited.txt"));
        }
        public void RevertProfile()
        {
            //We need to do this regardless, as the "Temp" folder can still change in non-profile mode.
            //If we don't, it could cause desynchronization between modes.
            string MainFolder = System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Main");
            Directory.CreateDirectory(MainFolder);
            string TempFolder = System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Temp");
            Directory.Delete(TempFolder, true);
            DirectoryCopy(MainFolder, TempFolder, true);
        }
        public void SaveTempToMainProfile()
        {
            //This extra step needs to happen for non-profile mode because the "Temp" folder can be modified in non-profile mode.
            //If we don't, it could cause desynchronization between modes.
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "False")
            {
                string MainFolder = System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Main"); 
                string TempFolder = System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Temp"); 
                Directory.CreateDirectory(TempFolder);
                Directory.Delete(MainFolder, true);
                DirectoryCopy(TempFolder, MainFolder, true);
            }
        }
        public void UpdateProfile(UndertaleData data, string filename)
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
            Directory.CreateDirectory(System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Main"));
            Directory.CreateDirectory(System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Temp"));
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "True" && data.GMS2_3)
            {
                MessageBox.Show("The profile feature is not currently supported for GameMaker 2.3 games.");
                return;
            }
            else if (SettingsWindow.DecompileOnceCompileManyEnabled == "True" && (!(data.IsYYC())))
            {
                Directory.CreateDirectory(ProfilesFolder);
                string ProfDir = System.IO.Path.Combine(ProfilesFolder, ProfileHash);
                string ProfDirTemp = System.IO.Path.Combine(ProfDir, "Temp");
                string ProfDirMain = System.IO.Path.Combine(ProfDir, "Main");
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
                            string tempPath = System.IO.Path.Combine(ProfDirTemp, file.Name);
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
                            string tempPath = System.IO.Path.Combine(ProfDirMain, file.Name);
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
                }
                MessageBox.Show(@"Profile loaded successfully!

The code's fully editable (you can even add comments) and will
be preserved exactly as written.

The profile system can be toggled on or off at any time by going
to the ""File"" tab at the top and then opening the ""Settings""
(the ""Enable decompile once compile many"" option toggles it
on or off).");
                CreateUMTLastEdited(filename);
            }
            else if ((SettingsWindow.DecompileOnceCompileManyEnabled == "False") && (!(data.IsYYC())))
            {
                return;
            }
            else if (SettingsWindow.DecompileOnceCompileManyEnabled == "True" && data.IsYYC())
            {
                MessageBox.Show("Profiles are not available for YYC games!");
                return;
            }
        }
        public void ProfileSaveEvent(UndertaleData data, string filename)
        {
            bool CopyProfile = false;
            using (var md5Instance = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    MD5CurrentlyLoaded = md5Instance.ComputeHash(stream);
                    if (MD5PreviouslyLoaded != MD5CurrentlyLoaded)
                    {
                        CopyProfile = true;
                    }
                }
            }
            Directory.CreateDirectory(System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Main"));
            Directory.CreateDirectory(System.IO.Path.Combine(ProfilesFolder, ProfileHash, "Temp"));
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "False" || data.GMS2_3 || data.IsYYC())
            {
                MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                return;
            }
            else if (SettingsWindow.DecompileOnceCompileManyEnabled == "True")
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
                    MD5DirPathOld = System.IO.Path.Combine(ProfilesFolder, MD5DirNameOld);
                    MD5DirPathOldMain = System.IO.Path.Combine(MD5DirPathOld, "Main");
                    MD5DirPathOldTemp = System.IO.Path.Combine(MD5DirPathOld, "Temp");
                    MD5DirNameNew = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                    MD5DirPathNew = System.IO.Path.Combine(ProfilesFolder, MD5DirNameNew);
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
                MD5DirPathOld = System.IO.Path.Combine(ProfilesFolder, MD5DirNameOld);
                MD5DirPathOldMain = System.IO.Path.Combine(MD5DirNameOld, "Main");
                MD5DirPathOldTemp = System.IO.Path.Combine(MD5DirNameOld, "Temp");
                if ((Directory.Exists(MD5DirPathOldMain)) && (Directory.Exists(MD5DirPathOldTemp)))
                {
                    Directory.Delete(MD5DirPathOldMain, true);
                }
                DirectoryCopy(MD5DirPathOldTemp, MD5DirPathOldMain, true);

                CopyProfile = false;
                ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                ProfDir = System.IO.Path.Combine(ProfilesFolder, ProfileHash);
                Directory.CreateDirectory(ProfDir);
                Directory.CreateDirectory(System.IO.Path.Combine(ProfDir, "Main"));
                Directory.CreateDirectory(System.IO.Path.Combine(ProfDir, "Temp"));
                MessageBox.Show("Profile saved successfully to" + ProfileHash);
            }
        }
        public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
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
                string tempPath = System.IO.Path.Combine(destDirName, file.Name);
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
                    string tempPath = System.IO.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
