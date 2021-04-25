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
            string ProfilesLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar;
            string LastEditedLocation = ProfilesLocation + "LastEdited.txt";
            if ((File.Exists(LastEditedLocation) && (Data == null)))
            {
                DidUMTCrashWhileEditing = true;
                string LastEditedContents = File.ReadAllText(LastEditedLocation);
                string[] CrashRecoveryData = LastEditedContents.Split('\n');
                string DataRecoverLocation = ProfilesLocation + CrashRecoveryData[0] + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;
                string ProfileHashOfCrashedFile;
                string ReportedHashOfCrashedFile = CrashRecoveryData[0];
                string PathOfCrashedFile = CrashRecoveryData[1];
                string PathOfRecoverableCode = ProfilesLocation + ReportedHashOfCrashedFile + System.IO.Path.DirectorySeparatorChar;
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
                        LoadFileSync(PathOfCrashedFile);
                        string[] dirFiles = Directory.GetFiles(DataRecoverLocation);
                        int progress = 0;
                        LoaderDialog CodeLoadDialog = new LoaderDialog("Script in progress...", "Please wait...");
                        CodeLoadDialog.Update(null, "Code entries processed: ", progress++, dirFiles.Length);
                        CodeLoadDialog.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { })); // Updates the UI, so you can see the progress.
                        foreach (string file in dirFiles)
                        {
                            ImportGML(file);
                            CodeLoadDialog.Update(null, "Code entries processed: ", progress++, dirFiles.Length);
                            CodeLoadDialog.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { })); // Updates the UI, so you can see the progress.
                        }
                        CodeLoadDialog.TryHide();
                        MessageBox.Show("Completed.");
                    }
                    else
                    {
                        MessageBox.Show("Your code can be recovered from the \"Recovered\" folder at any time.");
                        string RecoveredDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Recovered" + System.IO.Path.DirectorySeparatorChar + ReportedHashOfCrashedFile;
                        Directory.CreateDirectory(RecoveredDir);
                        if (Directory.Exists(RecoveredDir))
                            Directory.Delete(RecoveredDir, true);
                        Directory.Move(PathOfRecoverableCode, RecoveredDir);
                    }
                }
                else
                {
                    MessageBox.Show("A crash has been detected from last session. Please check the Profiles folder for recoverable data now.");
                }
            }
        }
        public void LoadFileSync(string filename)
        {
            LoaderDialog CodeLoadDialog = new LoaderDialog("Loading", "Loading, please wait...");
            this.Dispatcher.Invoke(() =>
            {
                CommandBox.Text = "";
            });
            CodeLoadDialog.Update("Data file is loading");
            CodeLoadDialog.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { })); // Updates the UI, so you can see the progress.
            bool hadWarnings = false;
            UndertaleData data = null;
            try
            {
                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    data = UndertaleIO.Read(stream, warning =>
                    {
                        MessageBox.Show(warning, "Loading warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        hadWarnings = true;
                    });
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occured while trying to load:\n" + e.Message, "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Dispatcher.Invoke(() =>
            {
                if (data != null)
                {
                    if (data.UnsupportedBytecodeVersion)
                    {
                        MessageBox.Show("Only bytecode versions 14 to 17 are supported for now, you are trying to load " + data.GeneralInfo.BytecodeVersion + ". A lot of code is disabled and will likely break something. Saving/exporting is disabled.", "Unsupported bytecode version", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CanSave = false;
                        CanSafelySave = false;
                    }
                    else if (hadWarnings)
                    {
                        MessageBox.Show("Warnings occurred during loading. Data loss will likely occur when trying to save!", "Loading problems", MessageBoxButton.OK, MessageBoxImage.Warning);
                        CanSave = true;
                        CanSafelySave = false;
                    }
                    else
                    {
                        CanSave = true;
                        CanSafelySave = true;
                        if (SettingsWindow.DecompileOnceCompileManyEnabled == "True")
                            CodeLoadDialog.TryHide();
                        UpdateProfile(data, filename);
                    }
                    if (data.GMS2_3)
                    {
                        MessageBox.Show("This game was built using GameMaker Studio 2.3 (or above). Support for this version is a work in progress, and you will likely run into issues decompiling code or in other places.", "GMS 2.3", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    if (data.IsYYC())
                    {
                        MessageBox.Show("This game uses YYC (YoYo Compiler), which means the code is embedded into the game executable. This configuration is currently not fully supported; continue at your own risk.", "YYC", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    if (System.IO.Path.GetDirectoryName(FilePath) != System.IO.Path.GetDirectoryName(filename))
                        CloseChildFiles();
                    this.Data = data;
                    this.FilePath = filename;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsGMS2"));
                    ChangeSelection(Highlighted = new DescriptionView("Welcome to UndertaleModTool!", "Double click on the items on the left to view them!"));
                    SelectionHistory.Clear();
                    CodeLoadDialog.TryHide();
                }
            });
        }

        public void CreateUMTLastEdited(string filename)
        {
            string LastEdited = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + "LastEdited.txt";
            File.WriteAllText(LastEdited, ProfileHash + "\n" + filename);
        }
        public void DestroyUMTLastEdited()
        {
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + "LastEdited.txt"))
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + "LastEdited.txt");
        }
        public void RevertProfile()
        {
            //We need to do this regardless, as the "Temp" folder can still change in non-profile mode.
            //If we don't, it could cause desynchronization between modes.
            string MainFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar;
            Directory.CreateDirectory(MainFolder);
            string TempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;
            Directory.Delete(TempFolder, true);
            DirectoryCopy(MainFolder, TempFolder, true);
        }
        public void SaveTempToMainProfile()
        {
            //This extra step needs to happen for non-profile mode because the "Temp" folder can be modified in non-profile mode.
            //If we don't, it could cause desynchronization between modes.
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "False")
            {
                string MainFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar;
                string TempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;
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
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar);
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar);
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "True" && data.GMS2_3)
            {
                MessageBox.Show("The profile feature is not currently supported for GameMaker 2.3 games.");
                return;
            }
            else if (SettingsWindow.DecompileOnceCompileManyEnabled == "True" && (!(data.IsYYC())))
            {
                Directory.CreateDirectory(ProfilesFolder);
                string ProfDir;
                ProfDir = ProfilesFolder + ProfileHash + System.IO.Path.DirectorySeparatorChar;
                if (Directory.Exists(ProfDir))
                {
                    if ((!(Directory.Exists(ProfDir + "Temp"))) && (Directory.Exists(ProfDir + "Main")))
                    {
                        // Get the subdirectories for the specified directory.
                        DirectoryInfo dir = new DirectoryInfo(ProfDir + "Main");
                        Directory.CreateDirectory(ProfDir + "Temp");
                        // Get the files in the directory and copy them to the new location.
                        FileInfo[] files = dir.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            string tempPath = System.IO.Path.Combine(ProfDir + "Temp", file.Name);
                            file.CopyTo(tempPath, false);
                        }
                    }
                    else if ((!(Directory.Exists(ProfDir + "Main"))) && (Directory.Exists(ProfDir + "Temp")))
                    {
                        // Get the subdirectories for the specified directory.
                        DirectoryInfo dir = new DirectoryInfo(ProfDir + "Temp");
                        Directory.CreateDirectory(ProfDir + "Main");
                        // Get the files in the directory and copy them to the new location.
                        FileInfo[] files = dir.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            string tempPath = System.IO.Path.Combine(ProfDir + "Main", file.Name);
                            file.CopyTo(tempPath, false);
                        }
                    }
                }
                //First generation no longer exists, it will be generated on demand while you edit.
                Directory.CreateDirectory(ProfDir);
                Directory.CreateDirectory(ProfDir + "Main");
                Directory.CreateDirectory(ProfDir + "Temp");
                if (!Directory.Exists(ProfDir) || !Directory.Exists(ProfDir + "Main") || !Directory.Exists(ProfDir + "Temp"))
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
        public bool CheckHashForCorrections()
        {
            List<String> CorrectionsAvailableMD5List = new List<String>();
            CorrectionsAvailableMD5List.Add("d0822e279464db858682ca99ec4cbbff");
            CorrectionsAvailableMD5List.Add("cd48b89b6ac6b2d3977f2f82726e5f12");
            CorrectionsAvailableMD5List.Add("88ae093aa1ae0c90da0d3ff1e15aa724");
            CorrectionsAvailableMD5List.Add("856219e69dd39e76deca0586a7f44307");
            CorrectionsAvailableMD5List.Add("0bf582aa180983a9ffa721aa2be2f273");
            CorrectionsAvailableMD5List.Add("582795ad2037d06cdc8db0c72d9360d5");
            CorrectionsAvailableMD5List.Add("5903fc5cb042a728d4ad8ee9e949c6eb");
            CorrectionsAvailableMD5List.Add("427520a97db28c87da4220abb3a334c1");
            CorrectionsAvailableMD5List.Add("cf8f7e3858bfbc46478cc155b78fb170");
            CorrectionsAvailableMD5List.Add("113ef42e8cb91e5faf780c426679ec3a");
            CorrectionsAvailableMD5List.Add("a88a2db3a68c714ca2b1ff57ac08a032");
            CorrectionsAvailableMD5List.Add("56305194391ad7c548ee55a8891179cc");
            CorrectionsAvailableMD5List.Add("741ad8ab49a08226af7e1b13b64d4e55");
            CorrectionsAvailableMD5List.Add("6e1abb8e627c7a36cd8e6db11a829889");
            CorrectionsAvailableMD5List.Add("b6825187ca2e32c618e4899e6d0c4c50");
            CorrectionsAvailableMD5List.Add("cf6517bfa3b7b7e96c21b6c1a41f8415");
            CorrectionsAvailableMD5List.Add("5c8f4533f6e0629d45766830f5f5ca72");
            if (CorrectionsAvailableMD5List.Contains(ProfileHash))
                return true;
            else
                return false;
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
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar);
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + "UndertaleModTool" + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar);
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
                string MD5DirNameNew;
                string MD5DirPathNew;
                if (CopyProfile)
                {
                    MD5DirNameOld = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                    MD5DirPathOld = ProfilesFolder + MD5DirNameOld + System.IO.Path.DirectorySeparatorChar;
                    MD5DirNameNew = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                    MD5DirPathNew = ProfilesFolder + MD5DirNameNew + System.IO.Path.DirectorySeparatorChar;
                    DirectoryCopy(MD5DirPathOld, MD5DirPathNew, true);
                    if (Directory.Exists(MD5DirPathOld + "Main") && Directory.Exists(MD5DirPathOld + "Temp"))
                    {
                        Directory.Delete(MD5DirPathOld + "Temp", true);
                    }
                    DirectoryCopy(MD5DirPathOld + "Main", MD5DirPathOld + "Temp", true);
                }
                MD5PreviouslyLoaded = MD5CurrentlyLoaded;
                // Get the subdirectories for the specified directory.
                MD5DirNameOld = BitConverter.ToString(MD5CurrentlyLoaded).Replace("-", "").ToLowerInvariant();
                MD5DirPathOld = ProfilesFolder + MD5DirNameOld + System.IO.Path.DirectorySeparatorChar;
                string MD5DirPathOldMain = MD5DirPathOld + "Main";
                string MD5DirPathOldTemp = MD5DirPathOld + "Temp";
                if ((Directory.Exists(MD5DirPathOldMain)) && (Directory.Exists(MD5DirPathOldTemp)))
                {
                    Directory.Delete(MD5DirPathOldMain, true);
                }
                DirectoryCopy(MD5DirPathOldTemp, MD5DirPathOldMain, true);

                CopyProfile = false;
                ProfileHash = BitConverter.ToString(MD5PreviouslyLoaded).Replace("-", "").ToLowerInvariant();
                ProfDir = ProfilesFolder + ProfileHash + System.IO.Path.DirectorySeparatorChar;
                Directory.CreateDirectory(ProfDir);
                Directory.CreateDirectory(ProfDir + "Main");
                Directory.CreateDirectory(ProfDir + "Temp");
                MessageBox.Show("Profile saved successfully to" + ProfileHash);
            }
        }
        public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = System.IO.Path.Combine(destDirName, file.Name);
                try
                {
                    file.CopyTo(tempPath, false);
                }
                catch
                {
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
