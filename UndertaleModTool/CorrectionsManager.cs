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
        public void ReplaceTempWithMain()
        {
            try
            {
                if (!(ScriptQuestion("Warning: This may cause desyncs! The intended purpose is for reverting incorrect code corrections.\nOnly use this if you know what you're doing! Continue?")))
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
                MessageBox.Show("ReplaceTempWithMain error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void ReplaceMainWithTemp()
        {
            try
            {
                if (!(ScriptQuestion("Warning: This may cause desyncs! The intended purpose is for pushing code corrections (such as asset resolutions)\nOnly use this if you know what you're doing! Continue?")))
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
                MessageBox.Show("ReplaceMainWithTemp error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
            }
        }
        public void ReplaceTempWithCorrections()
        {
            ScriptMessage("Unimplemented!");
        }
        public void ReplaceCorrectionsWithTemp()
        {
            ScriptMessage("Unimplemented!");
        }
        public void UpdateCorrections()
        {
            ScriptMessage("Unimplemented!");
        }
    }
}
