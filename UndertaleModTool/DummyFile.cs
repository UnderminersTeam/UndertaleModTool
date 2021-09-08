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
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO.Pipes;

namespace UndertaleModTool
{
    // Test code here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public bool DummyBool()
        {
            return true;
        }

        public void DummyVoid()
        {
        }
        public string DummyString()
        {
            return "";
        }
    }
}
