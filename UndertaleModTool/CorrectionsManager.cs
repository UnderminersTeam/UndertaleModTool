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
    //Make new GUID helper functions
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public void ReplaceTempWithMain()
        {
            //ScriptMessage("Unimplemented, please do it manually, or unload the game without saving.");
            if (!(ScriptQuestion("Warning: This may cause desyncs! The intended purpose is for reverting incorrect code corrections. Continue?")))
                return;
            string MainPath = ProfilesFolder + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar;
            string TempPath = ProfilesFolder + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;
            if (Directory.Exists(TempPath))
            {
                Directory.Delete(TempPath, true);
            }
            DirectoryCopy(MainPath, TempPath, true);
        }
        public void ReplaceMainWithTemp()
        {
            if (!(ScriptQuestion("Warning: This may cause desyncs! The intended purpose is for pushing code corrections (such as asset resolutions). Continue?")))
                return;
            string MainPath = ProfilesFolder + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Main" + System.IO.Path.DirectorySeparatorChar;
            string TempPath = ProfilesFolder + ProfileHash + System.IO.Path.DirectorySeparatorChar + "Temp" + System.IO.Path.DirectorySeparatorChar;
            if (Directory.Exists(MainPath))
            {
                Directory.Delete(MainPath, true);
            }
            DirectoryCopy(TempPath, MainPath, true);
        }
        public void ReplaceTempWithCorrections()
        {
        }
        public void ReplaceCorrectionsWithTemp()
        {
        }
        public void UpdateCorrections()
        {
        }
    }
}
