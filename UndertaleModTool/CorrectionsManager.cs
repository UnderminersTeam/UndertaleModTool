using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    //Make new GUID helper functions
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public void ReplaceTempWithMain(bool imAnExpertBtw = false)
        {
            try
            {
                if (!imAnExpertBtw && (!(ScriptQuestion("Warning: This may cause desyncs! The intended purpose is for reverting incorrect code corrections.\nOnly use this if you know what you're doing! Continue?"))))
                    return;
                string MainPath = Path.Combine(ProfilesFolder, ProfileHash, "Main");
                string TempPath = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
                if (Directory.Exists(TempPath))
                {
                    Directory.Delete(TempPath, true);
                }
                DirectoryCopy(MainPath, TempPath, true);
            }
            catch (Exception exc)
            {
                MessageBox.Show(Application.Current.MainWindow as MainWindow, "ReplaceTempWithMain error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void ReplaceMainWithTemp(bool imAnExpertBtw = false)
        {
            try
            {
                if (!imAnExpertBtw && (!(ScriptQuestion("Warning: This may cause desyncs! The intended purpose is for pushing code corrections (such as asset resolutions)\nOnly use this if you know what you're doing! Continue?"))))
                    return;
                string MainPath = Path.Combine(ProfilesFolder, ProfileHash, "Main");
                string TempPath = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
                if (Directory.Exists(MainPath))
                {
                    Directory.Delete(MainPath, true);
                }
                DirectoryCopy(TempPath, MainPath, true);
            }
            catch (Exception exc)
            {
                MessageBox.Show(Application.Current.MainWindow as MainWindow, "ReplaceMainWithTemp error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void ReplaceTempWithCorrections(bool imAnExpertBtw = false)
        {
            try
            {
                if (!imAnExpertBtw && (!(ScriptQuestion("If you messed up royally while developing your corrections, you can use this to revert all of the changes to them in your Temp folder to what is in the Corrections folder. Only use this if you know what you're doing! Continue?"))))
                    return;
                string MainPath = Path.Combine(ProfilesFolder, ProfileHash, "Main");
                string TempPath = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
                DirectoryInfo dir = new DirectoryInfo(CorrectionsFolder);
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(TempPath, file.Name);
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                DirectoryCopy(CorrectionsFolder, TempPath, true);
            }
            catch (Exception exc)
            {
                MessageBox.Show(Application.Current.MainWindow as MainWindow, "ReplaceCorrectionsWithTemp error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void ReplaceCorrectionsWithTemp(bool imAnExpertBtw = false)
        {
            try
            {
                if (!imAnExpertBtw && (!(ScriptQuestion("The intended purpose is for pushing your custom made code corrections (your entire temporary profile) to the code corrections folder bundled with UndertaleModTool, making them permanent. Only use this if you know what you're doing! Continue?"))))
                    return;
                Directory.Delete(CorrectionsFolder, true);
                string MainPath = Path.Combine(ProfilesFolder, ProfileHash, "Main");
                string TempPath = Path.Combine(ProfilesFolder, ProfileHash, "Temp");
                DirectoryCopy(TempPath, CorrectionsFolder, true);
            }
            catch (Exception exc)
            {
                MessageBox.Show(Application.Current.MainWindow as MainWindow, "ReplaceCorrectionsWithTemp error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void UpdateCorrections(bool imAnExpertBtw = false)
        {
            if (!imAnExpertBtw && (!(ScriptQuestion("Update Main with Temp, Temp with Main, Corrections with Temp. Only use this if you know what you're doing! Continue?"))))
                return;
            ReplaceMainWithTemp(imAnExpertBtw);
            ReplaceTempWithMain(imAnExpertBtw);
            ReplaceCorrectionsWithTemp(imAnExpertBtw);
        }
    }
}
