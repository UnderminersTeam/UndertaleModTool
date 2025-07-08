using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;
using UndertaleModToolAvalonia.Controls;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Core;

public class Scripting
{
    public readonly MainViewModel MainVM;

    public Scripting(IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    public async Task<object?> RunScript(string text, string? filePath = null)
    {
        ScriptGlobals scripting = new(this, filePath);

        try
        {
            ScriptState<object> state = await CSharpScript.RunAsync(text, ScriptOptions.Default
                .AddImports(
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.IO",
                    "System.Text",
                    "System.Text.RegularExpressions",
                    "System.Threading.Tasks",
                    "UndertaleModLib",
                    "UndertaleModLib.Compiler",
                    "UndertaleModLib.Decompiler",
                    "UndertaleModLib.Models",
                    "UndertaleModLib.Scripting")
                .AddReferences(
                    "System.Core",
                    "UndertaleModLib")
                .WithFilePath(filePath)
                .WithFileEncoding(Encoding.Default)
                .WithEmitDebugInformation(true), scripting, typeof(IScriptInterface));

            return state.ReturnValue;
        }
        catch (CompilationErrorException e)
        {
            await MainVM.ShowMessageDialog($"Error compiling script: {e.Message}");
        }
        catch (ScriptException e)
        {
            await MainVM.ShowMessageDialog($"Error: {e.Message}");
        }
        catch (Exception e)
        {
            await MainVM.ShowMessageDialog($"Error in script:\n{e}");
        }

        return null;
    }
}

public class ScriptGlobals : IScriptInterface
{
    private readonly MainViewModel mainVM;
    private readonly string? scriptPath;

    public ScriptGlobals(Scripting scripting, string? scriptPath)
    {
        mainVM = scripting.MainVM;
        this.scriptPath = scriptPath;
    }

    public UndertaleData? Data => mainVM.Data;

    public string? FilePath => mainVM.DataPath;

    public string? ScriptPath => scriptPath;

    public object Highlighted => throw new NotImplementedException();

    public object Selected => throw new NotImplementedException();

    public bool CanSave => throw new NotImplementedException();

    public bool ScriptExecutionSuccess => throw new NotImplementedException();

    public string ScriptErrorMessage => throw new NotImplementedException();

    public string ExePath => throw new NotImplementedException();

    public string ScriptErrorType => throw new NotImplementedException();

    public bool IsAppClosed => throw new NotImplementedException();

    public void AddProgress(int amount)
    {
        // TODO
    }

    public void AddProgressParallel(int amount)
    {
        // TODO
    }

    public void ChangeSelection(object newSelection, bool inNewTab = false)
    {
        throw new NotImplementedException();
    }

    public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string>? failedList = null)
    {
        throw new NotImplementedException();
    }

    public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string>? failedList = null)
    {
        throw new NotImplementedException();
    }

    public void DisableAllSyncBindings()
    {
        throw new NotImplementedException();
    }

    public void EnableUI()
    {
        throw new NotImplementedException();
    }

    public string GetDecompiledText(string codeName, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
    {
        throw new NotImplementedException();
    }

    public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
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

    public int GetProgress()
    {
        throw new NotImplementedException();
    }

    public void HideProgressBar()
    {
        // TODO
    }

    public void IncrementProgress()
    {
        // TODO
    }

    public void IncrementProgressParallel()
    {
        // TODO
    }

    public void InitializeScriptDialog()
    {
        // TODO
    }

    public bool LintUMTScript(string path)
    {
        throw new NotImplementedException();
    }

    public bool MakeNewDataFile()
    {
        throw new NotImplementedException();
    }

    public string? PromptChooseDirectory()
    {
        IReadOnlyList<IStorageFolder> folders = Task.Run(() => mainVM.OpenFolderDialog!(new()
        {
            Title = "Select directory",
        })).Result;

        if (folders.Count != 1)
            return null;

        return folders[0].TryGetLocalPath();
    }

    public string PromptLoadFile(string defaultExt, string filter)
    {
        throw new NotImplementedException();
    }

    public string PromptSaveFile(string defaultExt, string filter)
    {
        throw new NotImplementedException();
    }

    public bool RunUMTScript(string path)
    {
        throw new NotImplementedException();
    }

    public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        mainVM.ShowMessageDialog(error, title).WaitOnDispatcherFrame();

        if (SetConsoleText)
        {
            mainVM.CommandTextBoxText = error;
        }
    }

    public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
    {
        throw new NotImplementedException();
    }

    public void ScriptMessage(string message)
    {
        mainVM.ShowMessageDialog(message, title: "Script message").WaitOnDispatcherFrame();
    }

    public void ScriptOpenURL(string url)
    {
        throw new NotImplementedException();
    }

    public bool ScriptQuestion(string message)
    {
        return mainVM.ShowMessageDialog(message, "Script question", ok: false, yes: true, no: true).WaitOnDispatcherFrame() == MessageWindow.Result.Yes;
    }

    public void ScriptWarning(string message)
    {
        mainVM.ShowMessageDialog(message, title: "Script warning").WaitOnDispatcherFrame();
    }

    public void SetFinishedMessage(bool isFinishedMessageEnabled)
    {
        throw new NotImplementedException();
    }

    public void SetProgress(int value)
    {
        // TODO
    }

    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        // TODO
    }

    public void SetProgressBar()
    {
        // TODO
    }

    public void SetUMTConsoleText(string message)
    {
        throw new NotImplementedException();
    }

    public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
    {
        throw new NotImplementedException();
    }

    public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
    {
        throw new NotImplementedException();
    }

    public void StartProgressBarUpdater()
    {
        // TODO
    }

    public Task StopProgressBarUpdater()
    {
        // TODO
        return Task.CompletedTask;
    }

    public void SyncBinding(string resourceType, bool enable)
    {
        throw new NotImplementedException();
    }

    public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
    {
        // TODO
    }

    public void UpdateProgressStatus(string status)
    {
        // TODO
    }

    public void UpdateProgressValue(double progressValue)
    {
        // TODO
    }
}
