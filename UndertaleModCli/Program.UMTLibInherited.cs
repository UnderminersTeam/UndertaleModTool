using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.VisualBasic;
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

        public bool GMLCacheEnabled => false; //TODO: not implemented yet, due to no code editing

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
            // Since the UMTConsole text messages are literally just messages that are shown,
            // I'm giving them the same behaviour as the normal ScriptMessage
            ScriptMessage(message);
        }

        public bool ScriptQuestion(string message)
        {
            Console.Write($"{message} (Y/N) ");
            bool isInputYes = Console.ReadKey(false).Key == ConsoleKey.Y;
            Console.WriteLine();
            return isInputYes;
        }

        public void ScriptError(string error, string title = "Error", bool setConsoleText = true)
        {
            // No need to care about setConsoleText if we're in CLI.

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
            string evaluatedMessage = !String.IsNullOrEmpty(message) ? $"{message}|" : "";
            Console.WriteLine($"[{evaluatedMessage}{status}] {currentValue} out of {maxValue}");
        }

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

        //TODO: revisit this once CLI gets code editor functionality
        public void ReapplyProfileCode()
        {
            //CLI does not have any code editing tools (yet), nor a profile Mode thus since is completely useless
        }

        public void NukeProfileGML(string codeName)
        {
            //CLI does not have any code editing tools (yet), nor a profile Mode thus since is completely useless
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
            string path;
            DirectoryInfo directoryInfo;
            do
            {
                Console.WriteLine("Please type a path (or drag and drop) to a valid directory:");
                Console.Write("Path: ");
                path = RemoveQuotes(Console.ReadLine());
                directoryInfo = new DirectoryInfo(path);
            } while (directoryInfo.Exists);
            return path;
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            string path;
            FileInfo directoryInfo;
            do
            {
                Console.WriteLine("Please type a path (or drag and drop) to a valid file:");
                Console.Write("Path: ");
                path = RemoveQuotes(Console.ReadLine());
                directoryInfo = new FileInfo(path);
            } while (directoryInfo.Exists);
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

        public string ScriptInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
        {
            // I'll ignore the cancelButtonText and submitButtonText as they don't have much use.
            return SimpleTextInput(titleText, labelText, defaultInputBoxText, isMultiline, preventClose);
        }

        public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
        {
            // default value gets ignored, as it doesn't really have a use in CLI.

            string result = "";

            Console.WriteLine("-----------------------INPUT----------------------");
            Console.WriteLine(title);
            Console.WriteLine(label + (allowMultiline ? " (Multiline, hit SHIFT+ENTER to insert newline)" : ""));
            Console.WriteLine("--------------------------------------------------");

            if (!allowMultiline)
            {
                result = Console.ReadLine();
            }
            else
            {
                bool isShiftAndEnterPressed = false;
                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey();
                    //result += keyInfo.KeyChar;

                    // If Enter is pressed without shift
                    if (((keyInfo.Modifiers & ConsoleModifiers.Shift) == 0) && (keyInfo.Key == ConsoleKey.Enter))
                        isShiftAndEnterPressed = true;

                    else
                    {
                        // If we have Enter + any other modifier pressed, append newline. Otherwise, just the content.
                        if (keyInfo.Key == ConsoleKey.Enter)
                        {
                            result += "\n";
                            Console.WriteLine();
                        }
                        // If backspace, display new empty char and move one back
                        // TODO: There's some weird bug with ctrl+backspace, i'll ignore it for now.
                        // Also make some of the multiline-backspace better.
                        else if ((keyInfo.Key == ConsoleKey.Backspace) && (result.Length > 0))
                        {
                            Console.Write(' ');
                            Console.SetCursorPosition(Console.CursorLeft-1, Console.CursorTop);
                            result = result.Remove(result.Length - 1);
                        }
                        else
                            result += keyInfo.KeyChar;
                    }

                } while (!isShiftAndEnterPressed);
            }

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("-----------------------INPUT----------------------");
            Console.WriteLine("--------------------------------------------------");

            return result;
        }

        public async Task ClickableTextOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
        {
            await ClickableTextOutput(title, query, resultsCount, resultsDict.ToDictionary(pair => pair.Key, pair => pair.Value), editorDecompile, failedList);
        }
        public async Task ClickableTextOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
        {
            await Task.Delay(1); //dummy await

            // If we have failed entries...
            if (failedList is not null)
            {
                // ...Print them all out
                Console.ForegroundColor = ConsoleColor.Red;
                if (failedList.Count() == 1)
                    Console.Error.WriteLine("There is 1 code entry that encountered an error while searching:");
                else
                    Console.Error.WriteLine($"There are {failedList.Count()} code entries that encountered an error while searching");

                foreach (var failedEntry in failedList)
                    Console.Error.WriteLine(failedEntry);

                Console.ResetColor();
                Console.WriteLine();
            }

            Console.WriteLine($"{resultsCount} results in {resultsDict.Count} code entries for \"{query}\".");
            Console.WriteLine();

            // Print in a pattern of:
            // results in code_file
            // line3: code
            // line6: code
            //
            // results in a codefile2
            //etc.
            foreach (var dictEntry in resultsDict)
            {
                Console.WriteLine($"Results in {dictEntry.Key}:");
                foreach (var resultEntry in dictEntry.Value)
                    Console.WriteLine(resultEntry);

                Console.WriteLine();
            }

            if (IsInteractive) Pause();
        }

        public bool LintUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            try
            {
                CancellationTokenSource source = new CancellationTokenSource(100);
                CancellationToken token = source.Token;
                object test = CSharpScript.EvaluateAsync(File.ReadAllText(path), CliScriptOptions, this, typeof(IScriptInterface), token);
            }
            catch (CompilationErrorException exc)
            {
                ScriptError(exc.Message, "Script compile error");
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

        //TODO: implement all these
        #region todo
        public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool isSaving = false)
        {
            await Task.Delay(1); //dummy await

            //TODO: not implemented yet, due to no code editing / profile mode.

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

        #region Some dangerous functions I don't know what they do

        //TODO: ask what these do and implement them.
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