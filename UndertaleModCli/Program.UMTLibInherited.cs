using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace UndertaleModCli;

// Everything that gets inherited (methods, attributes) from IScriptInterface gets put here
// in order to have the inherited stuff separated from normal stuff.
public partial class Program : IScriptInterface
{
    #region Inherited UMTLib Properties

    /// <inheritdoc/>
    public UndertaleData Data { get; set; }

    /// <inheritdoc/>
    public string FilePath { get; set; }

    /// <inheritdoc/>
    public string ScriptPath { get; set; }

    /// <inheritdoc/>
    public object Highlighted { get; set; }

    /// <inheritdoc/>
    public object Selected { get; set; }

    /// <inheritdoc/>
    public bool CanSave { get; set; }

    /// <inheritdoc/>
    public bool ScriptExecutionSuccess { get; set; }

    /// <inheritdoc/>
    public string ScriptErrorMessage { get; set; }

    /// <inheritdoc/>
    public string ExePath { get; set; }

    /// <inheritdoc/>
    public string ScriptErrorType { get; set; }

    /// <inheritdoc/>
    public bool GMLCacheEnabled => false; //TODO: not implemented yet, due to no code editing

    /// <inheritdoc/>
    public bool IsAppClosed { get; set; }

    #endregion

    #region Inherited UMTLib Methods

    /// <inheritdoc/>
    public void EnsureDataLoaded()
    {
        if (Data is null)
            throw new ScriptException("No data file is currently loaded!");
    }

    /// <inheritdoc/>
    public async Task<bool> MakeNewDataFile()
    {
        // This call has no use except to suppress the "method is not doing anything async" warning
        await Task.Delay(1);

        Data = UndertaleData.CreateNew();
        Console.WriteLine("New file created.");
        return true;
    }

    /// <inheritdoc/>
    public void ScriptMessage(string message)
    {
        Console.WriteLine(message);
        if (IsInteractive) Pause();
    }

    /// <inheritdoc/>
    public void SetUMTConsoleText(string message)
    {
        // Since the UMTConsole text messages are literally just messages that are shown,
        // I'm giving them the same behaviour as the normal ScriptMessage
        ScriptMessage(message);
    }

    /// <inheritdoc/>
    public bool ScriptQuestion(string message)
    {
        Console.Write($"{message} (Y/N) ");
        bool isInputYes = Console.ReadKey(false).Key == ConsoleKey.Y;
        Console.WriteLine();
        return isInputYes;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void SetFinishedMessage(bool isFinishedMessageEnabled)
    {
        FinishedMessageEnabled = isFinishedMessageEnabled;
    }

    /// <inheritdoc/>
    public void UpdateProgressBar(string message, string status, double currentValue, double maxValue)
    {
        string evaluatedMessage = !String.IsNullOrEmpty(message) ? $"{message}|" : "";
        Console.WriteLine($"[{evaluatedMessage}{status}] {currentValue} out of {maxValue}");
    }

    /// <inheritdoc/>
    public void SetProgressBar(string message, string status, double currentValue, double maxValue)
    {
        savedMsg = message;
        savedStatus = status;
        savedValue = currentValue;
        savedValueMax = maxValue;

        UpdateProgressBar(message, status, currentValue, maxValue);
    }

    /// <inheritdoc/>
    public void UpdateProgressValue(double currentValue)
    {
        UpdateProgressBar(savedMsg, savedStatus, currentValue, savedValueMax);

        savedValue = currentValue;
    }

    /// <inheritdoc/>
    public void UpdateProgressStatus(string status)
    {
        UpdateProgressBar(savedMsg, status, savedValue, savedValueMax);

        savedStatus = status;
    }

    /// <inheritdoc/>
    public void AddProgress(int amount)
    {
        progressValue += amount;
    }

    /// <inheritdoc/>
    public void IncrementProgress()
    {
        progressValue++;
    }

    /// <inheritdoc/>
    public void AddProgressParallel(int amount) //P - Parallel (multi-threaded)
    {
        Interlocked.Add(ref progressValue, amount); //thread-safe add operation (not the same as "lock ()")
    }

    /// <inheritdoc/>
    public void IncrementProgressParallel()
    {
        Interlocked.Increment(ref progressValue); //thread-safe increment
    }

    /// <inheritdoc/>
    public int GetProgress()
    {
        return progressValue;
    }

    /// <inheritdoc/>
    public void SetProgress(int value)
    {
        progressValue = value;
    }


    #region Empty Inherited Methods

    /// <inheritdoc/>
    public void InitializeScriptDialog()
    {
        // CLI has no dialogs to initialize
    }

    /// <inheritdoc/>
    //TODO: revisit this once CLI gets code editor functionality
    public void ReapplyProfileCode()
    {
        //CLI does not have any code editing tools (yet), nor a profile Mode thus since is completely useless
    }

    /// <inheritdoc/>
    public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool isSaving = false)
    {
        await Task.Delay(1); //dummy await

        //TODO: not implemented yet, due to no code editing / profile mode.

        return false;
    }

    /// <inheritdoc/>
    public void NukeProfileGML(string codeName)
    {
        //CLI does not have any code editing tools (yet), nor a profile Mode thus since is completely useless
    }

    /// <inheritdoc/>
    public void SetProgressBar()
    {
        //no progress bar that can be setup to show
    }

    /// <inheritdoc/>
    public void HideProgressBar()
    {
        // nothing to hide..
    }

    /// <inheritdoc/>
    public void EnableUI()
    {
        // nothing to enable...
    }

    /// <inheritdoc/>
    public void SyncBinding(string resourceType, bool enable)
    {
        //there is no UI with any data binding
    }

    /// <inheritdoc/>
    public void DisableAllSyncBindings()
    {
        //there is no UI with any data binding
    }

    #endregion

    /// <inheritdoc/>
    public void StartProgressBarUpdater()
    {
        if (cTokenSource is not null)
            Console.WriteLine("Warning - there is another progress updater task running (hangs) in the background.");

        cTokenSource = new CancellationTokenSource();
        cToken = cTokenSource.Token;

        updater = Task.Run(ProgressUpdater);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void ChangeSelection(object newSelection)
    {
        //this does *not* make sense, as CLI does not have any selections
        //however, since Selection is a public object, it could potentially be used by scripts
        Selected = newSelection;
    }

    /// <inheritdoc/>
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
        } while (!directoryInfo.Exists);
        return path;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public string GetDecompiledText(string codeName, GlobalDecompileContext context = null)
    {
        return GetDecompiledText(Data.Code.ByName(codeName), context);
    }

    /// <inheritdoc/>
    public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null)
    {
        if (code.ParentEntry is not null)
            return $"// This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", decompile that instead.";

        GlobalDecompileContext decompileContext = context is null ? new(Data, false) : context;
        try
        {
            return code != null ? Decompiler.Decompile(code, decompileContext) : "";
        }
        catch (Exception e)
        {
            return "/*\nDECOMPILER FAILED!\n\n" + e + "\n*/";
        }
    }

    /// <inheritdoc/>
    public string GetDisassemblyText(string codeName)
    {
        return GetDisassemblyText(Data.Code.ByName(codeName));
    }

    /// <inheritdoc/>
    public string GetDisassemblyText(UndertaleCode code)
    {
        if (code.ParentEntry is not null)
            return $"; This code entry is a reference to an anonymous function within \"{code.ParentEntry.Name.Content}\", disassemble that instead.";

        try
        {
            return code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : "";
        }
        catch (Exception e)
        {
            return "/*\nDISASSEMBLY FAILED!\n\n" + e + "\n*/"; // Please don't
        }
    }

    /// <inheritdoc/>
    public string ScriptInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
    {
        // I'll ignore the cancelButtonText and submitButtonText as they don't have much use.
        return SimpleTextInput(titleText, labelText, defaultInputBoxText, isMultiline, preventClose);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool editorDecompile, IOrderedEnumerable<string> failedList = null)
    {
        await ClickableSearchOutput(title, query, resultsCount, resultsDict.ToDictionary(pair => pair.Key, pair => pair.Value), editorDecompile, failedList);
    }

    /// <inheritdoc/>
    public async Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool editorDecompile, IEnumerable<string> failedList = null)
    {
        await Task.Delay(1); //dummy await

        // If we have failed entries...
        if (failedList is not null)
        {
            // Convert list to array first
            string[] failedArray = failedList.ToArray();

            // ...Print them all out
            Console.ForegroundColor = ConsoleColor.Red;
            if (failedArray.Length == 1)
                Console.Error.WriteLine("There is 1 code entry that encountered an error while searching:");
            else
                Console.Error.WriteLine($"There are {failedArray.Length} code entries that encountered an error while searching");

            foreach (string failedEntry in failedArray)
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

    /// <inheritdoc/>
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
            CSharpScript.EvaluateAsync(File.ReadAllText(path), CliScriptOptions, this, typeof(IScriptInterface), token);
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

    /// <inheritdoc/>
    public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool checkDecompiler = false)
    {
        ImportCode(codeName, gmlCode, true, doParse, true, checkDecompiler);
    }

    /// <inheritdoc/>
    public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool destroyASM = true, bool checkDecompiler = false)
    {
        ImportCode(codeName, gmlCode, false, doParse, destroyASM, checkDecompiler);
    }

    /// <inheritdoc/>
    public void ImportGMLFile(string fileName, bool doParse = true, bool checkDecompiler = false, bool throwOnError = false)
    {
        ImportCodeFromFile(fileName, true, doParse, true, checkDecompiler, throwOnError);
    }

    /// <inheritdoc/>
    public void ImportASMFile(string fileName, bool doParse = true, bool destroyASM = true, bool checkDecompiler = false, bool throwOnError = false)
    {
        ImportCodeFromFile(fileName, false, doParse, destroyASM, checkDecompiler, throwOnError);
    }

    /// <inheritdoc/>
    public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
    {
        UndertaleCode code = Data.Code.ByName(codeName);
        if (code is null)
            throw new ScriptException($"No code named \"{codeName}\" was found!");

        ReplaceTextInGML(code, keyword, replacement, caseSensitive, isRegex, context);
    }

    /// <inheritdoc/>
    public void ReplaceTextInGML(UndertaleCode code, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
    {
        if (code == null) throw new ArgumentNullException(nameof(code));
        if (code.ParentEntry is not null)
            return;

        EnsureDataLoaded();

        string passBack = "";
        GlobalDecompileContext decompileContext = context is null ? new(Data, false) : context;

        if (!Data.ToolInfo.ProfileMode)
        {
            try
            {
                passBack = GetPassBack(Decompiler.Decompile(code, decompileContext), keyword, replacement, caseSensitive, isRegex);
                code.ReplaceGML(passBack, Data);
            }
            catch (Exception exc)
            {
                throw new Exception("Error during GML code replacement:\n" + exc);
            }
        }
        else if (Data.ToolInfo.ProfileMode)
        {
            try
            {
                try
                {
                    if (context is null)
                        passBack = GetPassBack(Decompiler.Decompile(code, new GlobalDecompileContext(Data, false)), keyword, replacement, caseSensitive, isRegex);
                    else
                        passBack = GetPassBack(Decompiler.Decompile(code, context), keyword, replacement, caseSensitive, isRegex);
                    code.ReplaceGML(passBack, Data);
                }
                catch (Exception exc)
                {
                    throw new Exception("Error during GML code replacement:\n" + exc);
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Error during writing of GML code to profile:\n" + exc + "\n\nCode:\n\n" + passBack);
            }
        }
    }

    #region Some dangerous functions I don't know what they do

    //TODO: ask what these do and implement them.
    /// <inheritdoc/>
    public void ReplaceTempWithMain(bool imAnExpertBtw = false)
    {
        throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
    }

    /// <inheritdoc/>
    public void ReplaceMainWithTemp(bool imAnExpertBtw = false)
    {
        throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
    }

    /// <inheritdoc/>
    public void ReplaceTempWithCorrections(bool imAnExpertBtw = false)
    {
        throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
    }

    /// <inheritdoc/>
    public void ReplaceCorrectionsWithTemp(bool imAnExpertBtw = false)
    {
        throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
    }

    /// <inheritdoc/>
    public void UpdateCorrections(bool imAnExpertBtw = false)
    {
        throw new NotImplementedException("Sorry, this hasn't been implemented yet!");
    }
    #endregion

    #endregion

    /// <inheritdoc/>
    public bool DummyBool()
    {
        return false;
    }

    /// <inheritdoc/>
    public void DummyVoid()
    {
        // i want hugs so bad :(
    }

    /// <inheritdoc/>
    public string DummyString()
    {
        return "";
    }

    // Copy-pasted from GUI. TODO: Should probably get shared at one point, but there are some GUI only attributes accessed there and I don't want to touch the methods.
    #region Helper functions for Code replacing

    void ImportCode(string codeName, string gmlCode, bool isGML = true, bool doParse = true, bool destroyASM = true, bool checkDecompiler = false, bool throwOnError = false)
    {
        bool skipPortions = false;
        UndertaleCode code = Data.Code.ByName(codeName);
        if (code is null)
        {
            code = new UndertaleCode();
            code.Name = Data.Strings.MakeString(codeName);
            Data.Code.Add(code);
        }
        else if (code.ParentEntry is not null)
            return;

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
            if (codeName.StartsWith("gml_Script"))
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
            else if (codeName.StartsWith("gml_GlobalScript"))
            {
                // Add code to global init section.
                UndertaleGlobalInit initEntry = null;
                // This doesn't work, have to do it the hard way: UndertaleGlobalInit init_entry = Data.GlobalInitScripts.ByName(scr_dup_code_name_con);
                foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
                {
                    if (globalInit.Code.Name.Content == codeName)
                    {
                        initEntry = globalInit;
                        break;
                    }
                }
                if (initEntry == null)
                {
                    UndertaleGlobalInit newInit = new UndertaleGlobalInit();
                    newInit.Code = code;
                    Data.GlobalInitScripts.Add(newInit);
                }
                else
                {
                    UndertaleGlobalInit NewInit = initEntry;
                    NewInit.Code = code;
                }
            }
            else if (codeName.StartsWith("gml_Object"))
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
                            List<uint> possibleValues = new List<uint>();
                            possibleValues.Add(uint.MaxValue);
                            methodNumber = (int)ReduceCollisionValue(possibleValues);
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
                        if (Data.IsVersionAtLeast(2, 3))
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
                                List<uint> possibleValues = new List<uint>();
                                possibleValues.Add(uint.MaxValue);
                                ReassignGUIDs(methodNumberStr, ReduceCollisionValue(possibleValues));
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
                        skipPortions = true;
                    }
                }

                if (!(skipPortions))
                {
                    obj = Data.GameObjects.ByName(objName);
                    int eventIdx = Convert.ToInt32(Enum.Parse(typeof(EventType), methodName));
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
        SafeImport(codeName, gmlCode, isGML, destroyASM, checkDecompiler, throwOnError);
    }

    void ImportCodeFromFile(string file, bool isGML = true, bool doParse = true, bool destroyASM = true, bool checkDecompiler = false, bool throwOnError = false)
    {
        try
        {
            if (!Path.GetFileName(file).ToLower().EndsWith(isGML ? ".gml" : ".asm"))
                return;
            if (Path.GetFileName(file).ToLower().EndsWith("cleanup_0" + (isGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                return;
            if (Path.GetFileName(file).ToLower().EndsWith("precreate_0" + (isGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                return;
            string codeName = Path.GetFileNameWithoutExtension(file);
            string gmlCode = File.ReadAllText(file);
            ImportCode(codeName, gmlCode, isGML, doParse, destroyASM, checkDecompiler, throwOnError);
        }
        catch (ScriptException exc) when (throwOnError && exc.Message == "*codeImportError*")
        {
            throw new ScriptException("Code files importation stopped because of error(s).");
        }
        catch (Exception exc)
        {
            if (!checkDecompiler)
            {
                Console.Error.WriteLine("Import" + (isGML ? "GML" : "ASM") + "File error! Send the following error to Grossley#2869 (Discord) and make an issue on Github:\n\n" + exc);

                if (throwOnError)
                    throw new ScriptException("Code files importation stopped because of error(s).");
            }
            else
                throw new Exception("Error!");
        }
    }

    public void ReassignGUIDs(string guid, uint objectIndex)
    {
        int eventIdx = Convert.ToInt32(EventType.Collision);
        for (var i = 0; i < Data.GameObjects.Count; i++)
        {
            UndertaleGameObject obj = Data.GameObjects[i];
            try
            {
                foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                {
                    foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                    {
                        if (action.CodeId.Name.Content.Contains(guid))
                        {
                            evnt.EventSubtype = objectIndex;
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

    public uint ReduceCollisionValue(List<uint> possibleValues)
    {
        if (possibleValues.Count == 1)
        {
            if (possibleValues[0] != uint.MaxValue)
                return possibleValues[0];

            // Nothing found, pick new one
            bool objFound = false;
            uint objIndex = 0;
            while (!objFound)
            {
                string objectIndex = SimpleTextInput("Object could not be found. Please enter it below:",
                    "Object enter box.", "", false).ToLower();
                for (var i = 0; i < Data.GameObjects.Count; i++)
                {
                    if (Data.GameObjects[i].Name.Content.ToLower() == objectIndex)
                    {
                        objFound = true;
                        objIndex = (uint)i;
                    }
                }
            }
            return objIndex;
        }

        if (possibleValues.Count != 0)
        {
            // 2 or more possible values, make a list to choose from

            string gameObjectNames = "";
            foreach (uint objID in possibleValues)
                gameObjectNames += Data.GameObjects[(int)objID].Name.Content + "\n";

            bool objFound = false;
            uint objIndex = 0;
            while (!objFound)
            {
                string objectIndex = SimpleTextInput("Multiple objects were found. Select only one object below from the set, or, if none below match, some other object name:",
                    "Object enter box.", gameObjectNames, true).ToLower();
                for (var i = 0; i < Data.GameObjects.Count; i++)
                {
                    if (Data.GameObjects[i].Name.Content.ToLower() == objectIndex)
                    {
                        objFound = true;
                        objIndex = (uint)i;
                    }
                }
            }
            return objIndex;
        }

        return 0;
    }

    public List<uint> GetCollisionValueFromCodeNameGUID(string codeName)
    {
        int eventIdx = Convert.ToInt32(EventType.Collision);
        List<uint> possibleValues = new List<uint>();
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
                                possibleValues.Add(evnt.EventSubtype);
                                return possibleValues;
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
        possibleValues = GetCollisionValueFromGUID(GetGUIDFromCodeName(codeName));
        return possibleValues;
    }

    public List<uint> GetCollisionValueFromGUID(string guid)
    {
        int eventIdx = Convert.ToInt32(EventType.Collision);
        List<uint> possibleValues = new List<uint>();
        for (var i = 0; i < Data.GameObjects.Count; i++)
        {
            UndertaleGameObject obj = Data.GameObjects[i];
            try
            {
                foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                {
                    foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                    {
                        if (action.CodeId.Name.Content.Contains(guid))
                        {
                            if (!possibleValues.Contains(evnt.EventSubtype))
                            {
                                possibleValues.Add(evnt.EventSubtype);
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

        if (possibleValues.Count == 0)
        {
            possibleValues.Add(uint.MaxValue);
            return possibleValues;
        }
        else
        {
            return possibleValues;
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

    void SafeImport(string codeName, string gmlCode, bool isGML, bool destroyASM = true, bool checkDecompiler = false, bool throwOnError = false)
    {
        UndertaleCode code = Data.Code.ByName(codeName);
        if (code?.ParentEntry is not null)
            return;

        try
        {
            if (isGML)
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
            if (!checkDecompiler)
            {
                string errorText = $"Code import error at {(isGML ? "GML" : "ASM")} code \"{codeName}\":\n\n{ex.Message}";
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

    public string GetPassBack(string decompiledText, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false)
    {
        keyword = keyword.Replace("\r\n", "\n");
        replacement = replacement.Replace("\r\n", "\n");
        string passBack;
        if (!isRegex)
        {
            if (caseSensitive)
                passBack = decompiledText.Replace(keyword, replacement);
            else
                passBack = Regex.Replace(decompiledText, Regex.Escape(keyword), replacement.Replace("$", "$$"), RegexOptions.IgnoreCase);
        }
        else
        {
            passBack = Regex.Replace(decompiledText, keyword, replacement, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
        }
        return passBack;
    }

    #endregion

}