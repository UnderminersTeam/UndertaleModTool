using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModTests
{
    public abstract class GameScriptTestBase : GameTestBase, IScriptInterface
    {
        public GameScriptTestBase(string path, string md5) : base(path, md5)
        {
        }

        public UndertaleData Data => data;
        public string FilePath => path;
        public object Highlighted => throw new NotImplementedException();
        public object Selected => throw new NotImplementedException();
        public bool CanSave => throw new NotImplementedException();
        public string ScriptPath => throw new NotImplementedException();
        public bool ScriptExecutionSuccess => throw new NotImplementedException();
        public string ScriptErrorMessage => throw new NotImplementedException();
        public string ExePath => throw new NotImplementedException();
        public string ScriptErrorType => throw new NotImplementedException();
        public bool GMLCacheEnabled => throw new NotImplementedException();

        public bool IsAppClosed => throw new NotImplementedException();

        public void ChangeSelection(object newSelection, bool inNewTab)
        {
        }

        public void EnsureDataLoaded()
        {
        }
        public async Task<bool> MakeNewDataFile()
        {
            await Task.Delay(1); //dummy await
            return true;
        }
        public void ReplaceTempWithMain(bool imAnExpertBtw = false)
        {
        }
        public void ReplaceMainWithTemp(bool imAnExpertBtw = false)
        {
        }
        public void ReplaceTempWithCorrections(bool imAnExpertBtw = false)
        {
        }
        public void ReplaceCorrectionsWithTemp(bool imAnExpertBtw = false)
        {
        }
        public void UpdateCorrections(bool imAnExpertBtw = false)
        {
        }
        public void ReapplyProfileCode()
        {
        }
        public void InitializeScriptDialog()
        {
        }
        public bool RunUMTScript(string path)
        {
            Console.WriteLine(path);
            return true;
        }
        public bool LintUMTScript(string path)
        {
            Console.WriteLine(path);
            return true;
        }

        public void ScriptMessage(string message)
        {
            Console.WriteLine(message);
        }

        public bool ScriptQuestion(string message)
        {
            Console.WriteLine(message);
            return true;
        }
        public void ScriptOpenURL(string url)
        {
            Console.WriteLine("Open: " + url);
        }
        public void NukeProfileGML(string codeName)
        {
            Console.WriteLine("NukeProfileGML(): " + codeName);
        }
        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            Console.WriteLine("UpdateProgressBar(): " + progressValue + " / " + maxValue + ", Message: " + message + ", Status: " + status);
        }
        public void SetProgressBar(string message, string status, double progressValue, double maxValue)
        {
            Console.WriteLine("SetProgressBar(): " + progressValue + " / " + maxValue + ", Message: " + message + ", Status: " + status);
        }
        public void SetProgressBar()
        {
            Console.WriteLine("SetProgressBar()");
        }
        public void UpdateProgressValue(double progressValue)
        {
            Console.WriteLine($"UpdateProgressValue(): {progressValue}");
        }
        public void UpdateProgressStatus(string status)
        {
            Console.WriteLine($"UpdateProgressStatus(): {status}");
        }

        public void AddProgress(int amount)
        {
            Console.WriteLine($"AddProgress(): {amount}");
        }
        public void IncrementProgress()
        {
            Console.WriteLine("IncrementProgress()");
        }
        public void AddProgressParallel(int amount)
        {
            Console.WriteLine($"AddProgressParallel(): {amount}");
        }
        public void IncrementProgressParallel()
        {
            Console.WriteLine("IncrementProgressParallel()");
        }
        public int GetProgress()
        {
            Console.WriteLine("GetProgress()");
            return -1;
        }
        public void SetProgress(int value)
        {
            Console.WriteLine($"SetProgress(): {value}");
        }

        public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
        {
            Console.Write(label + " ");
            string ret = Console.ReadLine();

            return ret;
        }
        public string SimpleTextInput(string titleText, string labelText, string defaultInputBoxText, bool isMultiline, bool showDialog = true)
        {
            return ScriptInputDialog(titleText, labelText, defaultInputBoxText, "Cancel", "Submit", isMultiline, false);
        }
        public void SimpleTextOutput(string titleText, string labelText, string message, bool isMultiline)
        {
            Console.WriteLine($"SimpleTextOutput(): \"{titleText}\", \"{labelText}\", *defaultInputBoxText* (length - {message.Length}), {isMultiline}");
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null)
        {
            Console.WriteLine($"ClickableSearchOutput(): \"{title}\", \"{query}\", {resultsCount}, *resultsDict* (length - {resultsDict.Count()}), {showInDecompiledView.ToString().ToLower()}"
                              + failedList is not null ? $", *failedList* (length - {failedList.Count()})" : string.Empty);
            await Task.Delay(1); //dummy await
        }
        public async Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<string>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null)
        {
            Console.WriteLine($"ClickableSearchOutput(): \"{title}\", \"{query}\", {resultsCount}, *resultsDict* (length - {resultsDict.Count}), {showInDecompiledView.ToString().ToLower()}"
                              + failedList is not null ? $", *failedList* (length - {failedList.Count()})" : string.Empty);
            await Task.Delay(1); //dummy await
        }

        public void SetUMTConsoleText(string message)
        {
            Console.Write("SetUMTConsoleText(): " + message);
        }
        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            Console.Write("ReplaceTextInGML(): " + codeName + ", " + keyword + ", " + replacement + ", " + caseSensitive.ToString() + ", " + isRegex.ToString() + ", " + context?.ToString());
        }
        public void ReplaceTextInGML(UndertaleCode code, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            Console.Write("ReplaceTextInGML(): " + code.ToString() + ", " + keyword + ", " + replacement + ", " + caseSensitive.ToString() + ", " + isRegex.ToString() + ", " + context?.ToString());
        }
        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool checkDecompiler = false)
        {
            Console.Write("ImportGMLString(): " + codeName + ", " + gmlCode + ", " + doParse.ToString());
        }
        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool nukeProfile = true, bool checkDecompiler = false)
        {
            Console.Write("ImportASMString(): " + codeName + ", " + gmlCode + ", " + doParse.ToString());
        }
        public void ImportGMLFile(string fileName, bool doParse = true, bool checkDecompiler = false, bool throwOnError = false)
        {
            Console.Write($"ImportGMLFile(): \"{fileName}\", {doParse}, {checkDecompiler}, {throwOnError}");
        }
        public void ImportASMFile(string fileName, bool doParse = true, bool nukeProfile = true, bool checkDecompiler = false, bool throwOnError = false)
        {
            Console.Write($"ImportASMFile(): \"{fileName}\", {doParse}, {nukeProfile}, {checkDecompiler}, {throwOnError}");
        }

        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            Console.Write("SetFinishedMessage(): " + isFinishedMessageEnabled.ToString());
        }

        public void HideProgressBar()
        {
            Console.WriteLine("Hiding Progress Bar.");
        }

        public void EnableUI()
        {
            Console.WriteLine("Enabling UI.");
        }
        public void SyncBinding(string resourceType, bool enable)
        {
            Console.WriteLine($"SyncBinding(): \"{resourceType}\", {enable}");
        }
        public void DisableAllSyncBindings()
        {
            Console.WriteLine($"Disabling all enabled synced bindings.");
        }
        public void StartProgressBarUpdater()
        {
            Console.WriteLine("Starting progress bar updater...");
        }
        public async Task StopProgressBarUpdater()
        {
            Console.WriteLine("Stopping progress bar updater...");
            await Task.Delay(1); //dummy await
        }

        public async Task<bool> GenerateGMLCache(ThreadLocal<GlobalDecompileContext> decompileContext = null, object dialog = null, bool clearGMLEditedBefore = false)
        {
            Console.WriteLine(String.Format("GenerateGMLCache(): *decompileContext*{0}, *dialog*{1}, {2}",
                                            decompileContext is null ? " (null)" : "",
                                            dialog is null ? " (null)" : "",
                                            clearGMLEditedBefore.ToString().ToLower())
                              );

            await Task.Delay(1); //dummy await

            return false;
        }

        protected async Task<object> RunScript(string path)
        {
            string scriptpath = Path.Combine("../../../UndertaleModTool/Scripts/Builtin Scripts/", path);
            using (var loader = new InteractiveAssemblyLoader())
            {
                loader.RegisterDependency(typeof(UndertaleObject).GetTypeInfo().Assembly);

                var script = CSharpScript.Create<object>(File.ReadAllText(scriptpath), ScriptOptions.Default
                    .WithImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "UndertaleModLib.Scripting", "System", "System.IO", "System.Collections.Generic")
                    .WithReferences(typeof(UndertaleObject).GetTypeInfo().Assembly),
                    typeof(IScriptInterface), loader);

                return await script.RunAsync(this);
            }
        }

        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            throw new NotImplementedException();
        }

        public string PromptChooseDirectory()
        {
            throw new NotImplementedException();
        }

        public string GetDecompiledText(string codeName, GlobalDecompileContext context = null)
        {
            string output = "GetDecompiledText(): " + codeName;
            Console.Write(output);
            return output;
        }
        public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null)
        {
            string output = "GetDecompiledText(): " + code?.ToString();
            Console.Write(output);
            return output;
        }
        public string GetDisassemblyText(string codeName)
        {
            string output = "GetDisassemblyText(): " + codeName;
            Console.Write(output);
            return output;
        }
        public string GetDisassemblyText(UndertaleCode code)
        {
            string output = "GetDisassemblyText(): " + code?.ToString();
            Console.Write(output);
            return output;
        }
        public bool AreFilesIdentical(string file1, string file2)
        {
            string output = "AreFilesIdentical(): " + file1 + ", " + file2;
            Console.Write(output);
            return true;
        }
        public string PromptLoadFile(string defaultExt, string filter)
        {
            throw new NotImplementedException();
        }
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

    [TestClass]
    public class UndertaleScriptTest : GameScriptTestBase
    {
        public UndertaleScriptTest() : base(GamePaths.UNDERTALE_PATH, GamePaths.UNDERTALE_MD5)
        {
        }

        [TestMethod]
        public async Task EnableDebug()
        {
            await RunScript("EnableDebug.csx");
        }

        [TestMethod]
        public async Task DebugToggler()
        {
            await RunScript("DebugToggler.csx");
        }

        [TestMethod]
        public async Task GoToRoom()
        {
            await RunScript("GoToRoom.csx");
        }

        [TestMethod]
        public async Task ShowRoomName()
        {
            await RunScript("ShowRoomName.csx");
        }

        [TestMethod]
        [Ignore] // TODO: path problems
        public async Task BorderEnabler()
        {
            await RunScript("BorderEnabler.csx");
        }

        [TestMethod]
        public async Task testing()
        {
            await RunScript("testing.csx");
        }

        [TestMethod]
        public async Task RoomOfDetermination()
        {
            await RunScript("RoomOfDetermination.csx");
        }

        [TestMethod]
        public async Task TTFFonts()
        {
            await RunScript("TTFFonts.csx");
        }

        [TestMethod]
        public async Task MixMod()
        {
            await RunScript("MixMod.csx");
        }
    }

    [TestClass]
    public class UndertaleSwitchScriptTest : GameScriptTestBase
    {
        public UndertaleSwitchScriptTest() : base(GamePaths.UNDERTALE_SWITCH_PATH, GamePaths.UNDERTALE_SWITCH_MD5)
        {
        }

        [TestMethod]
        public async Task EnableDebug()
        {
            await RunScript("EnableDebug.csx");
        }

        [TestMethod]
        public async Task DebugToggler()
        {
            await RunScript("DebugToggler.csx");
        }

        [TestMethod]
        public async Task GoToRoom()
        {
            await RunScript("GoToRoom.csx");
        }

        [TestMethod]
        public async Task ShowRoomName()
        {
            await RunScript("ShowRoomName.csx");
        }
    }

    [TestClass]
    public class DeltaruneScriptTest : GameScriptTestBase
    {
        public DeltaruneScriptTest() : base(GamePaths.DELTARUNE_PATH, GamePaths.DELTARUNE_MD5)
        {
        }

        [TestMethod]
        public async Task EnableDebug()
        {
            await RunScript("EnableDebug.csx");
        }

        [TestMethod]
        public async Task DebugToggler()
        {
            await RunScript("DebugToggler.csx");
        }

        [TestMethod]
        public async Task GoToRoom()
        {
            await RunScript("GoToRoom.csx");
        }

        [TestMethod]
        public async Task ShowRoomName()
        {
            await RunScript("ShowRoomName.csx");
        }

        [TestMethod]
        public async Task DeltaHATE()
        {
            await RunScript("DeltaHATE.csx");
        }

        [TestMethod]
        public async Task DeltaMILK()
        {
            await RunScript("DeltaMILK.csx");
        }

        [TestMethod]
        public async Task TheWholeWorldRevolving()
        {
            await RunScript("TheWholeWorldRevolving.csx");
        }

        [TestMethod]
        public async Task DebugMsg()
        {
            await RunScript("DebugMsg.csx");
        }

        [TestMethod]
        public async Task HeCanBeEverywhere()
        {
            await RunScript("HeCanBeEverywhere.csx");
        }
    }
}
