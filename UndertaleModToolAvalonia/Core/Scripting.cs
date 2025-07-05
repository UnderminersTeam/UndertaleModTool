using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Core;

public class Scripting
{
    private readonly IServiceProvider serviceProvider;
    private readonly MainViewModel mainVM;

    public Scripting(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        mainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    public async Task<object?> RunScript(string text, string? filePath = null)
    {
        ScriptGlobals scripting = new(serviceProvider);

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
            await mainVM.ShowMessageDialog($"Error compiling script: {e.Message}");
        }
        catch (ScriptException e)
        {
            await mainVM.ShowMessageDialog($"Error: {e.Message}");
        }
        catch (Exception e)
        {
            await mainVM.ShowMessageDialog($"Error in script:\n{e}");
        }

        return null;
    }
}

public class ScriptGlobals : IScriptInterface
{
    private readonly MainViewModel mainVM;

    public ScriptGlobals(IServiceProvider serviceProvider)
    {
        mainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    public UndertaleData? Data => mainVM.Data;

    public string? FilePath => mainVM.DataPath;

    public string ScriptPath => throw new NotImplementedException();

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
        throw new NotImplementedException();
    }

    public void AddProgressParallel(int amount)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void IncrementProgress()
    {
        throw new NotImplementedException();
    }

    public void IncrementProgressParallel()
    {
        throw new NotImplementedException();
    }

    public void InitializeScriptDialog()
    {
        throw new NotImplementedException();
    }

    public bool LintUMTScript(string path)
    {
        throw new NotImplementedException();
    }

    public bool MakeNewDataFile()
    {
        throw new NotImplementedException();
    }

    public string PromptChooseDirectory()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
    {
        throw new NotImplementedException();
    }

    public void ScriptMessage(string message)
    {
        throw new NotImplementedException();
    }

    public void ScriptOpenURL(string url)
    {
        throw new NotImplementedException();
    }

    public bool ScriptQuestion(string message)
    {
        throw new NotImplementedException();
    }

    public void ScriptWarning(string message)
    {
        throw new NotImplementedException();
    }

    public void SetFinishedMessage(bool isFinishedMessageEnabled)
    {
        throw new NotImplementedException();
    }

    public void SetProgress(int value)
    {
        throw new NotImplementedException();
    }

    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        throw new NotImplementedException();
    }

    public void SetProgressBar()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public Task StopProgressBarUpdater()
    {
        throw new NotImplementedException();
    }

    public void SyncBinding(string resourceType, bool enable)
    {
        throw new NotImplementedException();
    }

    public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
    {
        throw new NotImplementedException();
    }

    public void UpdateProgressStatus(string status)
    {
        throw new NotImplementedException();
    }

    public void UpdateProgressValue(double progressValue)
    {
        throw new NotImplementedException();
    }
}
