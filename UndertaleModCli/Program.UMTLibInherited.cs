using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModCli
{
    // Everything that gets inherited (methods, attributes) from IScriptInterface gets put here
    // in order to have the inherited stuff separated from normal stuff.
    public partial class Program : IScriptInterface
    {
        #region Inherited UMTLib Properties

        public UndertaleData Data { get; set; }

        public string FilePath { get; set; }

        public string ScriptPath { get; set; }

        public object Highlighted { get; set; }

        public object Selected { get; set; }

        public bool CanSave { get; set; }

        public bool ScriptExecutionSuccess { get; set; }

        public string ScriptErrorMessage { get; set; }

        public string ExePath { get; set; }

        public string ScriptErrorType { get; set; }

        public bool GMLCacheEnabled => false; //TODO: not implemented yet

        public bool IsAppClosed { get; set; }

        #endregion

        #region Inherited UMTLib Methods

        public void EnsureDataLoaded()
        {
            if (Data is null)
                throw new Exception("No data file is loaded.");
        }

        public async Task<bool> Make_New_File()
        {
            await Task.Delay(1); //dummy await

            Data = UndertaleData.CreateNew();
            Console.WriteLine("New file created.");
            return true;
        }

        public void ScriptMessage(string message)
        {
            Console.WriteLine(message);
            if (IsInteractive) Pause();
        }

        public void SetUMTConsoleText(string message)
        {
            Console.Title = message;
        }

        public bool ScriptQuestion(string message)
        {
            Console.WriteLine(message);
            Console.Write("Input (Y/N)? ");
            var isInputYes = Console.ReadKey(false).Key == ConsoleKey.Y;
            Console.WriteLine();
            return isInputYes;
        }

        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            // no need to care about SetConsoleText if we're in CLI.........
            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine("---------------------ERROR!-----------------------");
            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine(title);
            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine("---------------------ERROR!-----------------------");
            Console.Error.WriteLine("--------------------------------------------------");
            if (IsInteractive) { Pause(); }
        }

        public void ScriptOpenURL(string url)
        {
            Process p;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //TODO: why useShellExecute on Windows, but not on the other OS?
                p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                p = Process.Start("xdg-open", url);  // Works ok on linux, should work on FreeBSD as it's very similar.
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                p = Process.Start("open", url); // Not tested
            }
            else
            {
                throw new InvalidOperationException("Unable to open the browser on this OS.");
            }

            p?.Dispose();
        }

        public bool SendAUMIMessage(IpcMessage_t ipMessage, ref IpcReply_t outReply)
        {
            return false;
        }

        public bool RunUMTScript(string path)
        {
            try
            {
                RunCSharpFile(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AreFilesIdentical(string file1, string file2)
        {
            using var fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs1.Length != fs2.Length) return false; // different size, files can't be the same

            while (true)
            {
                int b1 = fs1.ReadByte();
                int b2 = fs2.ReadByte();
                if (b1 != b2) return false; // different contents, files are not the same
                if (b1 == -1) break; // here both bytes are the same. Thus we only need to check if one is at end-of-file.
            }

            // identical
            return true;
        }

        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            FinishedMessageEnabled = isFinishedMessageEnabled;
        }
        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            var evaledMessage = (String.IsNullOrEmpty(message) ? $"{message}|" : "");
            Console.WriteLine($"[{evaledMessage}{status}] {progressValue} out of {maxValue}");
        }

        public void SetProgressBar(string message, string status, double progressValue, double maxValue)
        {
            savedMsg = message;
            savedStatus = status;
            savedValue = progressValue;
            savedValueMax = maxValue;

            UpdateProgressBar(message, status, progressValue, maxValue);
        }

        //TODO: can be implemented I think
        public void SetProgressBar()
        {
            //no dialog to show
        }

        public void UpdateProgressValue(double progressValue)
        {
            UpdateProgressBar(savedMsg, savedStatus, progressValue, savedValueMax);

            savedValue = progressValue;
        }
        public void UpdateProgressStatus(string status)
        {
            UpdateProgressBar(savedMsg, status, savedValue, savedValueMax);

            savedStatus = status;
        }

        public void AddProgress(int amount)
        {
            progressValue += amount;
        }
        public void IncProgress()
        {
            progressValue++;
        }
        public void AddProgressP(int amount) //P - Parallel (multithreaded)
        {
            Interlocked.Add(ref progressValue, amount); //thread-safe add operation (not the same as "lock ()")
        }
        public void IncProgressP()
        {
            Interlocked.Increment(ref progressValue); //thread-safe increment
        }
        public int GetProgress()
        {
            return progressValue;
        }
        public void SetProgress(int value)
        {
            progressValue = value;
        }

        #region Empty Inherited Methods
        public void HideProgressBar()
        {
            // nothing to hide..
        }
        public void EnableUI()
        {
            // nothing to enable...
        }
        public void SyncBinding(string resourceType, bool enable)
        {
            //there is no UI with any data binding
        }
        public void SyncBinding(bool enable = false)
        {
            //there is no UI with any data binding
        }
        #endregion

        public void StartUpdater()
        {
            if (cTokenSource is not null)
                Console.WriteLine("Warning - there is another progress updater task running (hangs) in the background.");

            cTokenSource = new CancellationTokenSource();
            cToken = cTokenSource.Token;

            updater = Task.Run(ProgressUpdater);
        }
        public async Task StopUpdater() //"async" because "Wait()" blocks UI thread
        {
            if (cTokenSource is not null)
            {
                cTokenSource.Cancel();

                if (await Task.Run(() => !updater.Wait(2000))) //if ProgressUpdater isn't responding
                    Console.WriteLine("Error - stopping the progress updater task is failed.");
                else
                {
                    cTokenSource.Dispose();
                    cTokenSource = null;
                }

                updater.Dispose();
            }
        }

        public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool isSaving = false)
        {
            await Task.Delay(1); //dummy await

            //TODO: not implemented yet

            return false;
        }


        public void ChangeSelection(object newsel)
        {
            Selected = newsel;
        }

        public string PromptChooseDirectory(string prompt)
        {
            Console.WriteLine("Please type a path (or drag and drop) to a directory:");
            Console.Write("Path: ");
            string p = Console.ReadLine();
            return p;
        }

        //TODO: implement all these
        #region todo
        public string PromptLoadFile(string defaultExt, string filter)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool CheckDecompiler = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ImportGMLFile(string fileName, bool doParse = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ImportASMFile(string fileName, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }
        public void ReplaceTextInGML(UndertaleCode code, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public string ScriptInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
        {
            throw new NotImplementedException();
        }

        public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
        {
            throw new NotImplementedException();
        }
        public void SimpleTextOutput(string title, string label, string defaultText, bool allowMultiline)
        {
            throw new NotImplementedException();
        }
        public async Task ClickableTextOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
        {
            await Task.Delay(1); //dummy await
            throw new NotImplementedException();
        }
        public async Task ClickableTextOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
        {
            await Task.Delay(1); //dummy await
            throw new NotImplementedException();
        }

        public bool LintUMTScript(string path)
        {
            throw new NotImplementedException();
        }
        public void InitializeScriptDialog()
        {
            throw new NotImplementedException();
        }
        public void ReapplyProfileCode()
        {
            throw new NotImplementedException();
        }
        public void NukeProfileGML(string codeName)
        {
            throw new NotImplementedException();
        }
        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null)
        {
            throw new NotImplementedException();
        }
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null)
        {
            throw new NotImplementedException();
        }
        public string GetDisassemblyText(string codeName)
        {
            throw new NotImplementedException();
        }
        public string GetDisassemblyText(UndertaleCode code)
        {
            throw new NotImplementedException();
        }

        public void ReplaceTempWithMain(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void ReplaceMainWithTemp(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void ReplaceTempWithCorrections(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void ReplaceCorrectionsWithTemp(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void UpdateCorrections(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }
        #endregion


        public bool DummyBool()
        {
            return false;
        }

        public void DummyVoid()
        {
            // i want hugs so bad :(
        }

        public string DummyString()
        {
            return "";
        }

        #endregion
    }
}