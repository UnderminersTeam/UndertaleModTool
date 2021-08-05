using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Scripting
{
    public interface IScriptInterface
    {
        UndertaleData Data { get; }
        string FilePath { get; }
        string ScriptPath { get; }
        object Highlighted { get; }
        object Selected { get; }
        bool CanSave { get; }
        bool ScriptExecutionSuccess { get; }
        string ScriptErrorMessage { get; }
        string ExePath { get; }
        string ScriptErrorType { get; }

        void EnsureDataLoaded();
        void ReplaceTempWithMain(bool ImAnExpertBTW = false);
        void ReplaceMainWithTemp(bool ImAnExpertBTW = false);
        void ReplaceTempWithCorrections(bool ImAnExpertBTW = false);
        void ReplaceCorrectionsWithTemp(bool ImAnExpertBTW = false);
        void UpdateCorrections(bool ImAnExpertBTW = false);

        void ScriptMessage(string message);
        void SetUMTConsoleText(string message);
        bool ScriptQuestion(string message);
        void ScriptError(string error, string title = "Error", bool SetConsoleText = true);
        void ScriptOpenURL(string url);
        bool SendAUMIMessage(IpcMessage_t ipMessage, ref IpcReply_t outReply);
        byte[] CreateGMLBytecode(string GMLText, bool UseAUMI = false);
        bool RunUMTScript(string path);
        bool LintUMTScript(string path);
        void InitializeScriptDialog();
        void ReapplyProfileCode();
        void NukeProfileGML(string codeName);
        string GetDecompiledText(string codeName);
        string GetDisassemblyText(string codeName);
        bool AreFilesIdentical(string File01, string File02);
        string ScriptInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose);
        string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline);
        void SetFinishedMessage(bool isFinishedMessageEnabled);
        void UpdateProgressBar(string message, string status, double progressValue, double maxValue);
        void HideProgressBar();

        void ChangeSelection(object newsel);

        string PromptChooseDirectory(string prompt);

        string PromptLoadFile(string defaultExt, string filter);
        void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool CheckDecompiler = false);
        void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false);
        void ImportGMLFile(string fileName, bool doParse = true, bool CheckDecompiler = false);
        void ImportASMFile(string fileName, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false);
        void ReplaceTextInGML(string codeName, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false);
    }
}
