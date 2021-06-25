﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UndertaleModLib;
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
        public string ScriptErrorType => throw new NotImplementedException();

        public void ChangeSelection(object newsel)
        {
        }

        public void EnsureDataLoaded()
        {
        }
        public void ReplaceTempWithMain(bool ImAnExpertBTW = false)
        {
        }
        public void ReplaceMainWithTemp(bool ImAnExpertBTW = false)
        {
        }
        public void ReplaceTempWithCorrections(bool ImAnExpertBTW = false)
        {
        }
        public void ReplaceCorrectionsWithTemp(bool ImAnExpertBTW = false)
        {
        }
        public void UpdateCorrections(bool ImAnExpertBTW = false)
        {
        }
        public void ReapplyProfileCode()
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
        public byte[] CreateGMLBytecode(string GMLText, bool UseAUMI = false)
        {
            return new byte[0];
        }
        public bool SendAUMIMessage(IpcMessage_t ipMessage, ref IpcReply_t outReply)
        {
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
            Console.WriteLine("Update Progress: " + progressValue + " / " + maxValue + ", Message: " + message + ", Status: " + status);
        }

        public string ScriptInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
        {
            Console.Write(labelText + " ");
            string ret = Console.ReadLine();

            return ret;
        }
        public string SimpleTextInput(string titleText, string labelText, string defaultInputBoxText, bool isMultiline)
        {
            return ScriptInputDialog(titleText, labelText, defaultInputBoxText, "Cancel", "Submit", isMultiline, false);
        }

        public void SetUMTConsoleText(string message)
        {
            Console.Write("SetUMTConsoleText(): " + message);
        }
        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false)
        {
            Console.Write("ReplaceTextInGML(): " + codeName + ", " + keyword + ", " + replacement + ", " + case_sensitive.ToString() + ", " + isRegex.ToString());
        }
        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool CheckDecompiler = false)
        {
            Console.Write("ImportGMLString(): " + codeName + ", " + gmlCode + ", " + doParse.ToString());
        }
        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            Console.Write("ImportASMString(): " + codeName + ", " + gmlCode + ", " + doParse.ToString());
        }
        public void ImportGMLFile(string fileName, bool doParse = true, bool CheckDecompiler = false)
        {
            Console.Write("ImportGMLFile(): " + fileName + ", " + doParse.ToString());
        }
        public void ImportASMFile(string fileName, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            Console.Write("ImportASMFile(): " + fileName + ", " + doParse.ToString());
        }

        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            Console.Write("SetFinishedMessage(): " + isFinishedMessageEnabled.ToString());
        }

        public void HideProgressBar()
        {
            Console.WriteLine("Hiding Progress Bar.");
        }

        protected async Task<object> RunScript(string path)
        {
            string scriptpath = Path.Combine("../../../UndertaleModTool/SampleScripts/", path);
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

        public string PromptChooseDirectory(string prompt)
        {
            throw new NotImplementedException();
        }

        public string GetDecompiledText(string codeName)
        {
            string output = "GetDecompiledText(): " + codeName;
            Console.Write(output);
            return output;
        }
        public string GetDisassemblyText(string codeName)
        {
            string output = "GetDisassemblyText(): " + codeName;
            Console.Write(output);
            return output;
        }
        public bool AreFilesIdentical(string File01, string File02)
        {
            string output = "AreFilesIdentical(): " + File01 + ", " + File02;
            Console.Write(output);
            return true;
        }
        public string PromptLoadFile(string defaultExt, string filter)
        {
            throw new NotImplementedException();
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
