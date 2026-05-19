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
using System.Threading.Tasks;
using UndertaleModTool.Localization;

namespace UndertaleModTool
{
    // Adding misc. scripting functions here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public bool RunUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(string.Format(LocalizationSource.GetString("Msg_ScriptDoesNotExist"), path));
                return false;
            }
            RunScript(path).GetAwaiter().GetResult();
            if (!ScriptExecutionSuccess)
                ScriptError(string.Format(LocalizationSource.GetString("Msg_ScriptErrorWithType"), ScriptErrorType, ScriptErrorMessage), ScriptErrorType);
            return ScriptExecutionSuccess;
        }
        public void InitializeScriptDialog()
        {
            if (scriptDialog == null)
            {
                scriptDialog = new LoaderDialog(LocalizationSource.GetString("Dialog_ScriptInProgress"), LocalizationSource.GetString("Msg_ScriptPleaseWait"));
                scriptDialog.Owner = this;
                scriptDialog.PreventClose = true;
            }
        }
        public bool LintUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(string.Format(LocalizationSource.GetString("Msg_ScriptDoesNotExist"), path));
                return false;
            }
            try
            {
                CancellationTokenSource source = new CancellationTokenSource(100);
                CancellationToken token = source.Token;
                object test = CSharpScript.EvaluateAsync(File.ReadAllText(path), scriptOptions, this, typeof(IScriptInterface), token);
            }
            catch (CompilationErrorException exc)
            {
                ScriptError(exc.Message, LocalizationSource.GetString("Dialog_ScriptCompileError"));
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "CompilationErrorException";
                return false;
            }
            catch (Exception)
            {
                // Using the 100 MS timer it can time out before successfully running, compilation errors are fast enough to get through.
                ScriptExecutionSuccess = true;
                ScriptErrorMessage = "";
                ScriptErrorType = "";
                return true;
            }
            return true;
        }
    }
}
