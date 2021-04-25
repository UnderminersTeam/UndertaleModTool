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
        public void CreateUMTLastEdited(string filename)
        {
            string LastEdited = System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + "LastEdited.txt";
            File.WriteAllText(LastEdited, ProfileHash + "\n" + filename);
        }
        public void DestroyUMTLastEdited()
        {
            if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + "LastEdited.txt"))
                File.Delete(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + "LastEdited.txt");
        }
        public void RevertProfile()
        {
            //We need to do this regardless, as the "Temp" folder can still change in non-profile mode.
            //If we don't, it could cause desynchronization between modes.
            string MainFolder = System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar;
            Directory.CreateDirectory(MainFolder);
            string TempFolder = System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;
            Directory.Delete(TempFolder, true);
            DirectoryCopy(MainFolder, TempFolder, true);
        }
        public void SaveTempToMainProfile()
        {
            //This extra step needs to happen for non-profile mode because the "Temp" folder can be modified in non-profile mode.
            //If we don't, it could cause desynchronization between modes.
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "False")
            {
                string MainFolder = System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar;
                string TempFolder = System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;
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
            Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar);
            Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar);
            if (SettingsWindow.DecompileOnceCompileManyEnabled == "True" && data.GMS2_3)
            {
                MessageBox.Show("The profile feature is not currently supported for GameMaker 2.3 games.");
                return;
            }
            else if (SettingsWindow.DecompileOnceCompileManyEnabled == "True" && (!(data.IsYYC())))
            {
                Directory.CreateDirectory(ProfilesFolder);
                string ProfDir;
                bool FirstGeneration = false;
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
                if (Directory.GetFiles(ProfDir, "*.gml", SearchOption.AllDirectories).Length < 1 || (!Directory.Exists(ProfDir)))
                {
                    MessageBox.Show("Profile generation will now occur. It may take a few minutes to complete and may appear frozen during this time. This is normal. Please wait for the profile creation process to finish.");
                    FirstGeneration = true;
                }
                if (((Directory.GetFiles(ProfDir, "*.gml", SearchOption.AllDirectories).Length) < 200) && (CheckHashForCorrections()))
                {
                    MessageBox.Show("Profile generation will now occur. It may take a few minutes to complete and may appear frozen during this time. This is normal. Please wait for the profile creation process to finish.");
                    FirstGeneration = true;
                }
                Directory.CreateDirectory(ProfDir);
                Directory.CreateDirectory(ProfDir + "Main");
                Directory.CreateDirectory(ProfDir + "Temp");
                if (Directory.Exists(ProfDir))
                {
                    ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(data, false));
                    foreach (UndertaleCode code in data.Code)
                    {
                        string path = System.IO.Path.Combine(ProfDir + "Main" + System.IO.Path.DirectorySeparatorChar, code.Name.Content + ".gml");
                        if (!File.Exists(path))
                        {
                            try
                            {
                                File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Unable to complete writing of files for profile!\n" + ex.ToString());
                                    return;
                                }
                            }
                        }
                    }
                    foreach (UndertaleCode code in data.Code)
                    {
                        string path = System.IO.Path.Combine(ProfDir + "Temp" + System.IO.Path.DirectorySeparatorChar, code.Name.Content + ".gml");
                        if (!File.Exists(path))
                        {
                            try
                            {
                                File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
                            }
                            catch (Exception e)
                            {
                                try
                                {
                                    File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Unable to complete writing of files for profile!\n" + ex.ToString());
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Profile should exist, but does not. Insufficient permissions??? (Try running in Administrator mode)");
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
            Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar);
            Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + System.IO.Path.DirectorySeparatorChar + "Profiles" + System.IO.Path.DirectorySeparatorChar + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar);
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
                file.CopyTo(tempPath, false);
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
