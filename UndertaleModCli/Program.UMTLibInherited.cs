using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
            // This call has no use except to suppress the "method is not doing anything async" warning
            await Task.Delay(1);

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
            //TODO: seems to do nothing on Linux / Macos? Could be terminal dependant.
            Console.Title = message;
        }

        public bool ScriptQuestion(string message)
        {
            Console.WriteLine(message);
            Console.Write("Input (Y/N)? ");
            bool isInputYes = Console.ReadKey(false).Key == ConsoleKey.Y;
            Console.WriteLine();
            return isInputYes;
        }

        public void ScriptError(string error, string title = "Error", bool setConsoleText = true)
        {
            // no need to care about setConsoleText if we're in CLI...
            // Although we could copy SetUMTConsoleText and change the console.title as well
            // potential TODO?

            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine("----------------------ERROR!----------------------");
            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine(title);
            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine(error);
            Console.Error.WriteLine("--------------------------------------------------");
            Console.Error.WriteLine("----------------------ERROR!----------------------");
            Console.Error.WriteLine("--------------------------------------------------");
            if (IsInteractive) { Pause(); }
        }

        public void SimpleTextOutput(string title, string label, string defaultText, bool allowMultiline)
        {
            // In order to be similar to GUI output, we strip everything past a newline in "defaultValue" should multiline be disabled
            if (!allowMultiline)
                defaultText = defaultText.Remove(defaultText.IndexOf('\n'));

            Console.WriteLine("----------------------OUTPUT----------------------");
            Console.WriteLine(title);
            Console.WriteLine(label);
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(defaultText);
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("----------------------OUTPUT----------------------");
            Console.WriteLine("--------------------------------------------------");

            if (IsInteractive) Pause();
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
                throw new InvalidOperationException("Unable to open the browser on this OS: " +  RuntimeInformation.OSDescription);
            }

            p?.Dispose();
        }

        public bool SendAUMIMessage(IpcMessage_t ipMessage, ref IpcReply_t outReply)
        {
            // Implementation Copy-pasted from UndertaleModTool/MainWindow.xaml.cs

            // By Archie
            const int replySize = 132;

            // Create the pipe
            using var pPipeServer = new NamedPipeServerStream("AUMI-IPC", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            // Wait 1/8th of a second for AUMI to connect.
            // If it doesn't connect in time (which it should), just return false to avoid a deadlock.
            if (!pPipeServer.IsConnected)
            {
                pPipeServer.WaitForConnectionAsync();
                Thread.Sleep(125);
                if (!pPipeServer.IsConnected)
                {
                    pPipeServer.DisposeAsync();
                    return false;
                }
            }

            try
            {
                //Send the message
                pPipeServer.Write(ipMessage.RawBytes());
                pPipeServer.Flush();
            }
            catch (Exception e)
            {
                // Catch any errors that might arise if the connection is broken
                ScriptError("Could not write data to the pipe!\nError: " + e.Message);
                return false;
            }

            // Read the reply, the length of which is always a pre-set amount of bytes.
            byte[] bBuffer = new byte[replySize];
            pPipeServer.Read(bBuffer, 0, replySize);

            outReply = IpcReply_t.FromBytes(bBuffer);
            return true;
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
            using FileStream fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read, FileShare.Read);
            using FileStream fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read, FileShare.Read);

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

        public void UpdateProgressBar(string message, string status, double currentValue, double maxValue)
        {
            string evaluatedMessage = String.IsNullOrEmpty(message) ? $"{message}|" : "";
            Console.WriteLine($"[{evaluatedMessage}{status}] {currentValue} out of {maxValue}");
        }

        //TODO: why do these function need/save attributes?
        public void SetProgressBar(string message, string status, double currentValue, double maxValue)
        {
            savedMsg = message;
            savedStatus = status;
            savedValue = currentValue;
            savedValueMax = maxValue;

            UpdateProgressBar(message, status, currentValue, maxValue);
        }

        public void UpdateProgressValue(double currentValue)
        {
            UpdateProgressBar(savedMsg, savedStatus, currentValue, savedValueMax);

            savedValue = currentValue;
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

        public void InitializeScriptDialog()
        {
            // CLI has no dialogs to initialize
        }

        public void ReapplyProfileCode()
        {
            //CLI does not have any code editing tools, nor a profile Mode thus since is completely useless
        }

        public void NukeProfileGML(string codeName)
        {
            //CLI does not have any code editing tools, nor a profile Mode thus since is completely useless
        }

        public void SetProgressBar()
        {
            //no progress bar that can be setup to show
        }

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
            if (cTokenSource is null) return;


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

        public void ChangeSelection(object newSelection)
        {
            //this does *not* make sense, as CLI does not have any selections
            //however, since Selection is a public object, it could potentially be used by scripts
            Selected = newSelection;
        }

        public string PromptChooseDirectory(string prompt)
        {
            Console.WriteLine("Please type a path (or drag and drop) to a directory:");
            Console.Write("Path: ");
            //TODO: should probably trim quotes in order to not have funky stuff
            string path = Console.ReadLine();
            return path;
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            Console.WriteLine("Please type a path (or drag and drop) to a file:");
            Console.Write("Path: ");
            //TODO: should probably trim quotes in order to not have funky stuff
            string path = Console.ReadLine();
            return path;
        }

        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null)
        {
            return GetDecompiledText(Data.Code.ByName(codeName), context);
        }
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null)
        {
            GlobalDecompileContext DECOMPILE_CONTEXT = context is null ? new(Data, false) : context;
            try
            {
                return code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT) : "";
            }
            catch (Exception e)
            {
                return "/*\nDECOMPILER FAILED!\n\n" + e + "\n*/";
            }
        }

        public string GetDisassemblyText(string codeName)
        {
            return GetDisassemblyText(Data.Code.ByName(codeName));
        }
        public string GetDisassemblyText(UndertaleCode code)
        {
            try
            {
                return code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : "";
            }
            catch (Exception e)
            {
                return "/*\nDISASSEMBLY FAILED!\n\n" + e + "\n*/"; // Please don't
            }
        }

        //TODO: implement all these
        #region todo
        public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool isSaving = false)
        {
            await Task.Delay(1); //dummy await

            //TODO: not implemented yet

            return false;
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
            /*
             * TextInputDialog dlg = new TextInputDialog(titleText, labelText, defaultInputBoxText, cancelButtonText, submitButtonText, isMultiline, preventClose);
            bool? dlgResult = dlg.ShowDialog();

            if (!dlgResult.HasValue || dlgResult == false)
            {
                // returns null (not an empty!!!) string if the dialog has been closed, or an error has occured.
                return null;
            }

            // otherwise just return the input (it may be empty aka .Length == 0).
            return dlg.InputText;
             */

            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
        {
            //TODO: not for CLI but for GUI: what exactly is the use of showdialog? in which weird case do you want to show an input prompt, that does not "accept" any input?
            /*
            TextInput input = new TextInput(labelText, titleText, defaultInputBoxText, isMultiline);

            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;
            if (showDialog)
            {
                result = input.ShowDialog();
                input.Dispose();

                if (result == System.Windows.Forms.DialogResult.OK)
                    return input.ReturnString;            //values preserved after close
                else
                    return null;
            }
            else //if we don't need to wait for result
            {
                input.Show();
                return null;
                //no need to call input.Dispose(), because if form wasn't shown modally, Form.Close() (or closing it with "X") also calls Dispose()
            }
             */

            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public async Task ClickableTextOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
        {
            //will likely just call textoutput, as making it clickable is not really feasable.
            await Task.Delay(1); //dummy await
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }
        public async Task ClickableTextOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
        {
            //will likely just call textoutput, as making it clickable is not really feasable.
            await Task.Delay(1); //dummy await
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public bool LintUMTScript(string path)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ReplaceTempWithMain(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ReplaceMainWithTemp(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ReplaceTempWithCorrections(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void ReplaceCorrectionsWithTemp(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
        }

        public void UpdateCorrections(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
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