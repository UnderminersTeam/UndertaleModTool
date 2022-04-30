using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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

        public async Task<bool> MakeNewDataFile()
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

        public void IncrementProgress()
        {
            progressValue++;
        }

        public void AddProgressParallel(int amount) //P - Parallel (multithreaded)
        {
            Interlocked.Add(ref progressValue, amount); //thread-safe add operation (not the same as "lock ()")
        }

        public void IncrementProgressParallel()
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

        public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool isSaving = false)
        {
            await Task.Delay(1); //dummy await

            //TODO: not implemented yet, due to no code editing / profile mode.

            return false;
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

        public void DisableAllSyncBindings()
        {
            //there is no UI with any data binding
        }

        #endregion

        public void StartProgressBarUpdater()
        {
            if (cTokenSource is not null)
                Console.WriteLine("Warning - there is another progress updater task running (hangs) in the background.");

            cTokenSource = new CancellationTokenSource();
            cToken = cTokenSource.Token;

            updater = Task.Run(ProgressUpdater);
        }
        public async Task StopProgressBarUpdater() //"async" because "Wait()" blocks UI thread
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

        public string PromptChooseDirectory()
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
                bool isEnterWithoutShiftPressed = false;
                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey();
                    //result += keyInfo.KeyChar;

                    // If Enter is pressed without shift
                    if (((keyInfo.Modifiers & ConsoleModifiers.Shift) == 0) && (keyInfo.Key == ConsoleKey.Enter))
                        isEnterWithoutShiftPressed = true;

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

                } while (!isEnterWithoutShiftPressed);
            }

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("-----------------------INPUT----------------------");
            Console.WriteLine("--------------------------------------------------");

            return result;
        }

        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
        {
            await ClickableSearchOutput(title, query, resultsCount, resultsDict.ToDictionary(pair => pair.Key, pair => pair.Value), editorDecompile, failedList);
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
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

        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool replaceWithEmptyStringOnFail = false)
        {
            ImportCode(codeName, gmlCode, true, doParse, true, replaceWithEmptyStringOnFail);
        }

        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool nukeProfile = true, bool replaceWithEmptyStringOnFail = false)
        {
            ImportCode(codeName, gmlCode, false, doParse, nukeProfile, replaceWithEmptyStringOnFail);
        }

        public void ImportGMLFile(string fileName, bool doParse = true, bool replaceWithEmptyStringOnFail = false, bool throwOnError = false)
        {
            ImportCodeFromFile(fileName, true, doParse, true, replaceWithEmptyStringOnFail, throwOnError);
        }

        public void ImportASMFile(string fileName, bool doParse = true, bool nukeProfile = true, bool replaceWithEmptyStringOnFail = false, bool throwOnError = false)
        {
            ImportCodeFromFile(fileName, false, doParse, nukeProfile, replaceWithEmptyStringOnFail, throwOnError);
        }

        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            UndertaleCode code = Data.Code.ByName(codeName);
            if (code is null)
                throw new ScriptException($"No code named \"{codeName}\" was found!");

            ReplaceTextInGML(code, keyword, replacement, case_sensitive, isRegex, context);
        }
        public void ReplaceTextInGML(UndertaleCode code, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            EnsureDataLoaded();

            string passBack = "";
            string codeName = code.Name.Content;
            GlobalDecompileContext DECOMPILE_CONTEXT = context is null ? new(Data, false) : context;

            if (Data.ToolInfo.ProfileMode == false || Data.GMS2_3)
            {
                try
                {
                    passBack = GetPassBack((code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT ) : ""), keyword, replacement, case_sensitive, isRegex);
                    code.ReplaceGML(passBack, Data);
                }
                catch (Exception exc)
                {
                    throw new Exception("Error during GML code replacement:\n" + exc.ToString());
                }
            }
            else if (Data.ToolInfo.ProfileMode && !Data.GMS2_3)
            {
                try
                {
                    try
                    {
                        if (context is null)
                            passBack = GetPassBack((code != null ? Decompiler.Decompile(code, new GlobalDecompileContext(Data, false)) : ""), keyword, replacement, case_sensitive, isRegex);
                        else
                            passBack = GetPassBack((code != null ? Decompiler.Decompile(code, context) : ""), keyword, replacement, case_sensitive, isRegex);
                        code.ReplaceGML(passBack, Data);
                    }
                    catch (Exception exc)
                    {
                        throw new Exception("Error during GML code replacement:\n" + exc.ToString());
                    }
                }
                catch (Exception exc)
                {
                    throw new Exception("Error during writing of GML code to profile:\n" + exc.ToString() + "\n\nCode:\n\n" + passBack);
                }
            }
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

        // Copy-pasted from GUI. TODO: Should probably get shared at one point, but there are some GUI only attributes accessed there and I don't want to touch the methods.
        #region Helper functions for Code replacing

        void ImportCode(string codeName, string gmlCode, bool IsGML = true, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            bool SkipPortions = false;
            UndertaleCode code = Data.Code.ByName(codeName);
            if (code is null)
            {
                code = new UndertaleCode();
                code.Name = Data.Strings.MakeString(codeName);
                Data.Code.Add(code);
            }
            if (Data?.GeneralInfo.BytecodeVersion > 14 && Data.CodeLocals.ByName(codeName) == null)
            {
                UndertaleCodeLocals locals = new UndertaleCodeLocals();
                locals.Name = code.Name;

                UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                argsLocal.Name = Data.Strings.MakeString("arguments");
                argsLocal.Index = 0;

                locals.Locals.Add(argsLocal);

                code.LocalsCount = 1;
                code.GenerateLocalVarDefinitions(code.FindReferencedLocalVars(), locals); // Dunno if we actually need this line, but it seems to work?
                Data.CodeLocals.Add(locals);
            }
            if (doParse)
            {
                // This portion links code.
                if (codeName.Substring(0, 10).Equals("gml_Script"))
                {
                    // Add code to scripts section.
                    if (Data.Scripts.ByName(codeName.Substring(11)) == null)
                    {
                        UndertaleScript scr = new UndertaleScript();
                        scr.Name = Data.Strings.MakeString(codeName.Substring(11));
                        scr.Code = code;
                        Data.Scripts.Add(scr);
                    }
                    else
                    {
                        UndertaleScript scr = Data.Scripts.ByName(codeName.Substring(11));
                        scr.Code = code;
                    }
                }
                else if (codeName.Substring(0, 16).Equals("gml_GlobalScript"))
                {
                    // Add code to global init section.
                    UndertaleGlobalInit init_entry = null;
                    // This doesn't work, have to do it the hard way: UndertaleGlobalInit init_entry = Data.GlobalInitScripts.ByName(scr_dup_code_name_con);
                    foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
                    {
                        if (globalInit.Code.Name.Content == codeName)
                        {
                            init_entry = globalInit;
                            break;
                        }
                    }
                    if (init_entry == null)
                    {
                        UndertaleGlobalInit NewInit = new UndertaleGlobalInit();
                        NewInit.Code = code;
                        Data.GlobalInitScripts.Add(NewInit);
                    }
                    else
                    {
                        UndertaleGlobalInit NewInit = init_entry;
                        NewInit.Code = code;
                    }
                }
                else if (codeName.Substring(0, 10).Equals("gml_Object"))
                {
                    string afterPrefix = codeName.Substring(11);
                    int underCount = 0;
                    string methodNumberStr = "", methodName = "", objName = "";
                    for (int i = afterPrefix.Length - 1; i >= 0; i--)
                    {
                        if (afterPrefix[i] == '_')
                        {
                            underCount++;
                            if (underCount == 1)
                            {
                                methodNumberStr = afterPrefix.Substring(i + 1);
                            }
                            else if (underCount == 2)
                            {
                                objName = afterPrefix.Substring(0, i);
                                methodName = afterPrefix.Substring(i + 1, afterPrefix.Length - objName.Length - methodNumberStr.Length - 2);
                                break;
                            }
                        }
                    }
                    int methodNumber = 0;
                    try
                    {
                        methodNumber = int.Parse(methodNumberStr);
                        if (methodName == "Collision" && (methodNumber >= Data.GameObjects.Count || methodNumber < 0))
                        {
                            bool doNewObj = ScriptQuestion("Object of ID " + methodNumber.ToString() + " was not found.\nAdd new object?");
                            if (doNewObj)
                            {
                                UndertaleGameObject gameObj = new UndertaleGameObject();
                                gameObj.Name = Data.Strings.MakeString(SimpleTextInput("Enter object name", "Enter object name", "This is a single text line input box test.", false));
                                Data.GameObjects.Add(gameObj);
                            }
                            else
                            {
                                // It *needs* to have a valid value, make the user specify one.
                                List<uint> possible_values = new List<uint>();
                                possible_values.Add(uint.MaxValue);
                                methodNumber = (int)ReduceCollisionValue(possible_values);
                            }
                        }
                    }
                    catch
                    {
                        if (afterPrefix.LastIndexOf("_Collision_") != -1)
                        {
                            string s2 = "_Collision_";
                            objName = afterPrefix.Substring(0, (afterPrefix.LastIndexOf("_Collision_")));
                            methodNumberStr = afterPrefix.Substring(afterPrefix.LastIndexOf("_Collision_") + s2.Length, afterPrefix.Length - (afterPrefix.LastIndexOf("_Collision_") + s2.Length));
                            methodName = "Collision";
                            // GMS 2.3+ use the object name for the one colliding, which is rather useful.
                            if (Data.GMS2_3)
                            {
                                if (Data.GameObjects.ByName(methodNumberStr) != null)
                                {
                                    for (var i = 0; i < Data.GameObjects.Count; i++)
                                    {
                                        if (Data.GameObjects[i].Name.Content == methodNumberStr)
                                        {
                                            methodNumber = i;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    bool doNewObj = ScriptQuestion("Object " + objName + " was not found.\nAdd new object called " + objName + "?");
                                    if (doNewObj)
                                    {
                                        UndertaleGameObject gameObj = new UndertaleGameObject();
                                        gameObj.Name = Data.Strings.MakeString(objName);
                                        Data.GameObjects.Add(gameObj);
                                    }
                                }
                                if (Data.GameObjects.ByName(methodNumberStr) != null)
                                {
                                    // It *needs* to have a valid value, make the user specify one, silly.
                                    List<uint> possible_values = new List<uint>();
                                    possible_values.Add(uint.MaxValue);
                                    ReassignGUIDs(methodNumberStr, ReduceCollisionValue(possible_values));
                                }
                            }
                            else
                            {
                                // Let's try to get this going
                                methodNumber = (int)ReduceCollisionValue(GetCollisionValueFromCodeNameGUID(codeName));
                                ReassignGUIDs(methodNumberStr, ReduceCollisionValue(GetCollisionValueFromCodeNameGUID(codeName)));
                            }
                        }
                    }
                    UndertaleGameObject obj = Data.GameObjects.ByName(objName);
                    if (obj == null)
                    {
                        bool doNewObj = ScriptQuestion("Object " + objName + " was not found.\nAdd new object called " + objName + "?");
                        if (doNewObj)
                        {
                            UndertaleGameObject gameObj = new UndertaleGameObject();
                            gameObj.Name = Data.Strings.MakeString(objName);
                            Data.GameObjects.Add(gameObj);
                        }
                        else
                        {
                            SkipPortions = true;
                        }
                    }

                    if (!(SkipPortions))
                    {
                        obj = Data.GameObjects.ByName(objName);
                        int eventIdx = (int)Enum.Parse(typeof(EventType), methodName);
                        bool duplicate = false;
                        try
                        {
                            foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                            {
                                foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                                {
                                    if (action.CodeId?.Name?.Content == codeName)
                                        duplicate = true;
                                }
                            }
                        }
                        catch
                        {
                            // Something went wrong, but probably because it's trying to check something non-existent
                            // Just keep going
                        }
                        if (duplicate == false)
                        {
                            UndertalePointerList<UndertaleGameObject.Event> eventList = obj.Events[eventIdx];
                            UndertaleGameObject.EventAction action = new UndertaleGameObject.EventAction();
                            UndertaleGameObject.Event evnt = new UndertaleGameObject.Event();

                            action.ActionName = code.Name;
                            action.CodeId = code;
                            evnt.EventSubtype = (uint)methodNumber;
                            evnt.Actions.Add(action);
                            eventList.Add(evnt);
                        }
                    }
                }
            }
            SafeImport(codeName, gmlCode, IsGML, destroyASM, CheckDecompiler, throwOnError);
        }

        void ImportCodeFromFile(string file, bool IsGML = true, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            try
            {
                if (!Path.GetFileName(file).ToLower().EndsWith(IsGML ? ".gml" : ".asm"))
                    return;
                if (Path.GetFileName(file).ToLower().EndsWith("cleanup_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                    return;
                if (Path.GetFileName(file).ToLower().EndsWith("precreate_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                    return;
                string codeName = Path.GetFileNameWithoutExtension(file);
                string gmlCode = File.ReadAllText(file);
                ImportCode(codeName, gmlCode, IsGML, doParse, destroyASM, CheckDecompiler, throwOnError);
            }
            catch (ScriptException exc) when (throwOnError && exc.Message == "*codeImportError*")
            {
                throw new ScriptException("Code files importation stopped because of error(s).");
            }
            catch (Exception exc)
            {
                if (!CheckDecompiler)
                {
                    Console.Error.WriteLine("Import" + (IsGML ? "GML" : "ASM") + "File error! Send the following error to Grossley#2869 (Discord) and make an issue on Github:\n\n" + exc.ToString());

                    if (throwOnError)
                        throw new ScriptException("Code files importation stopped because of error(s).");
                }
                else
                    throw new Exception("Error!");
            }
        }

        public void ReassignGUIDs(string GUID, uint ObjectIndex)
        {
            int eventIdx = (int)Enum.Parse(typeof(EventType), "Collision");
            for (var i = 0; i < Data.GameObjects.Count; i++)
            {
                UndertaleGameObject obj = Data.GameObjects[i];
                try
                {
                    foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                    {
                        foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                        {
                            if (action.CodeId.Name.Content.Contains(GUID))
                            {
                                evnt.EventSubtype = ObjectIndex;
                            }
                        }
                    }
                }
                catch
                {
                    // Silently ignore, some values can be null along the way
                }
            }
        }

        public uint ReduceCollisionValue(List<uint> possible_values)
        {
            if (possible_values.Count == 1)
            {
                if (possible_values[0] != uint.MaxValue)
                    return possible_values[0];

                // Nothing found, pick new one
                bool obj_found = false;
                uint obj_index = 0;
                while (!obj_found)
                {
                    string object_index = SimpleTextInput("Object could not be found. Please enter it below:",
                                                            "Object enter box.", "", false).ToLower();
                    for (var i = 0; i < Data.GameObjects.Count; i++)
                    {
                        if (Data.GameObjects[i].Name.Content.ToLower() == object_index)
                        {
                            obj_found = true;
                            obj_index = (uint)i;
                        }
                    }
                }
                return obj_index;
            }

            if (possible_values.Count != 0)
            {
                // 2 or more possible values, make a list to choose from

                string gameObjectNames = "";
                foreach (uint objID in possible_values)
                    gameObjectNames += Data.GameObjects[(int)objID].Name.Content + "\n";

                bool obj_found = false;
                uint obj_index = 0;
                while (!obj_found)
                {
                    string object_index = SimpleTextInput("Multiple objects were found. Select only one object below from the set, or, if none below match, some other object name:",
                                                          "Object enter box.", gameObjectNames, true).ToLower();
                    for (var i = 0; i < Data.GameObjects.Count; i++)
                    {
                        if (Data.GameObjects[i].Name.Content.ToLower() == object_index)
                        {
                            obj_found = true;
                            obj_index = (uint)i;
                        }
                    }
                }
                return obj_index;
            }

            return 0;
        }

        public List<uint> GetCollisionValueFromCodeNameGUID(string codeName)
        {
            int eventIdx = (int)Enum.Parse(typeof(EventType), "Collision");
            List<uint> possible_values = new List<uint>();
            for (var i = 0; i < Data.GameObjects.Count; i++)
            {
                UndertaleGameObject obj = Data.GameObjects[i];
                try
                {
                    foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                    {
                        foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                        {
                            if (action.CodeId.Name.Content == codeName)
                            {
                                if (Data.GameObjects[(int)evnt.EventSubtype] != null)
                                {
                                    possible_values.Add(evnt.EventSubtype);
                                    return possible_values;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Silently ignore, some values can be null along the way
                }
            }
            possible_values = GetCollisionValueFromGUID(GetGUIDFromCodeName(codeName));
            return possible_values;
        }

        public List<uint> GetCollisionValueFromGUID(string GUID)
        {
            int eventIdx = (int)Enum.Parse(typeof(EventType), "Collision");
            List<uint> possible_values = new List<uint>();
            for (var i = 0; i < Data.GameObjects.Count; i++)
            {
                UndertaleGameObject obj = Data.GameObjects[i];
                try
                {
                    foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                    {
                        foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                        {
                            if (action.CodeId.Name.Content.Contains(GUID))
                            {
                                if (!possible_values.Contains(evnt.EventSubtype))
                                {
                                    possible_values.Add(evnt.EventSubtype);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Silently ignore, some values can be null along the way
                }
            }

            if (possible_values.Count == 0)
            {
                possible_values.Add(uint.MaxValue);
                return possible_values;
            }
            else
            {
                return possible_values;
            }
        }

        public string GetGUIDFromCodeName(string codeName)
        {
            string afterPrefix = codeName.Substring(11);
            if (afterPrefix.LastIndexOf("_Collision_") != -1)
            {
                string s2 = "_Collision_";
                return afterPrefix.Substring(afterPrefix.LastIndexOf("_Collision_") + s2.Length, afterPrefix.Length - (afterPrefix.LastIndexOf("_Collision_") + s2.Length));
            }
            else
                return "Invalid";
        }

        void SafeImport(string codeName, string gmlCode, bool IsGML, bool destroyASM = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            UndertaleCode code = Data.Code.ByName(codeName);
            try
            {
                if (IsGML)
                {
                    code.ReplaceGML(gmlCode, Data);
                }
                else
                {
                    var instructions = Assembler.Assemble(gmlCode, Data);
                    code.Replace(instructions);
                    if (destroyASM)
                        NukeProfileGML(codeName);
                }
            }
            catch (Exception ex)
            {
                if (!CheckDecompiler)
                {
                    string errorText = $"Code import error at {(IsGML ? "GML" : "ASM")} code \"{codeName}\":\n\n{ex.Message}";
                    Console.Error.WriteLine(errorText);

                    if (throwOnError)
                        throw new ScriptException("*codeImportError*");
                }
                else
                {
                    code.ReplaceGML("", Data);
                }
            }
        }

        public string GetPassBack(string decompiled_text, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false)
        {
            keyword = keyword.Replace("\r\n", "\n");
            replacement = replacement.Replace("\r\n", "\n");
            string passBack;
            if (!isRegex)
            {
                if (case_sensitive)
                    passBack = decompiled_text.Replace(keyword, replacement);
                else
                    passBack = Regex.Replace(decompiled_text, Regex.Escape(keyword), replacement.Replace("$", "$$"), RegexOptions.IgnoreCase);
            }
            else
            {
                if (case_sensitive)
                    passBack = Regex.Replace(decompiled_text, keyword, replacement, RegexOptions.None);
                else
                    passBack = Regex.Replace(decompiled_text, keyword, replacement, RegexOptions.IgnoreCase);
            }
            return passBack;
        }

        #endregion

    }
}