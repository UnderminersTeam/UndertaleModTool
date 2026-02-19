using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModLib.Project;

partial class ProjectContext : IScriptInterface
{
    /// <inheritdoc/>
    public ProjectContext Project => this;

    /// <inheritdoc/>
    public string FilePath => LoadDataPath;

    /// <inheritdoc/>
    public string ScriptPath => _scriptPath;

    /// <inheritdoc/>
    public object Highlighted => throw new NotImplementedException();

    /// <inheritdoc/>
    public object Selected => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool CanSave => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool ScriptExecutionSuccess => throw new NotImplementedException();

    /// <inheritdoc/>
    public string ScriptErrorMessage => throw new NotImplementedException();

    /// <inheritdoc/>
    public string ExePath => Path.GetDirectoryName(Environment.ProcessPath);

    /// <inheritdoc/>
    public string ScriptErrorType => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool IsAppClosed => throw new NotImplementedException();

    /// <inheritdoc/>
    UndertaleData IScriptInterface.Data => Data;

    /// <inheritdoc/>
    public void AddProgress(int amount)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void AddProgressParallel(int amount)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ChangeSelection(object newSelection, bool inNewTab = false)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void DisableAllSyncBindings()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void EnableUI()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string GetDecompiledText(string codeName, GlobalDecompileContext context = null, IDecompileSettings settings = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null, IDecompileSettings settings = null)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string GetDisassemblyText(string codeName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string GetDisassemblyText(UndertaleCode code)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public int GetProgress()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void HideProgressBar()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void IncrementProgress()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void IncrementProgressParallel()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void InitializeScriptDialog()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool LintUMTScript(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool MakeNewDataFile()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string PromptChooseDirectory()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string PromptLoadFile(string defaultExt, string filter)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string PromptSaveFile(string defaultExt, string filter)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool RunUMTScript(string path)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        throw new ScriptException(error);
    }

    /// <inheritdoc/>
    public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ScriptMessage(string message)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ScriptOpenURL(string url)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool ScriptQuestion(string message)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ScriptWarning(string message)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetFinishedMessage(bool isFinishedMessageEnabled)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetProgress(int value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetProgressBar()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetUMTConsoleText(string message)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void StartProgressBarUpdater()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task StopProgressBarUpdater()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SyncBinding(string resourceType, bool enable)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void UpdateProgressStatus(string status)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void UpdateProgressValue(double progressValue)
    {
        throw new NotImplementedException();
    }
}
