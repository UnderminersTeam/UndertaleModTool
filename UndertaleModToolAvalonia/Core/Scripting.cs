using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.CodeAnalysis;
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
        try
        {
            MainVM.IsEnabled = false;

            Script<object?> script = CSharpScript.Create(text, ScriptOptions.Default
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
                .WithEmitDebugInformation(true),
                typeof(IScriptInterface));

            ImmutableArray<Diagnostic> diagnostics = await Task.Run(() => script.Compile());

            IEnumerable<Diagnostic> errors = diagnostics.Where((Diagnostic diagnostic) => diagnostic.Severity == DiagnosticSeverity.Error);
            if (errors.Any())
            {
                string message = String.Join("\n", errors);
                await MainVM.ShowMessageDialog(message, title: "Script compilation error");

                return null;
            }

            ScriptGlobals scripting = new(this, filePath);

            try
            {
                ScriptState<object?> state = await script.RunAsync(scripting);
                return state.ReturnValue;
            }
            catch (ScriptException e)
            {
                await MainVM.ShowMessageDialog(e.Message, title: "Error from script");
            }
            catch (Exception e)
            {
                await MainVM.ShowMessageDialog(e.ToString(), title: "Script execution error");
            }
        }
        finally
        {
            MainVM.IsEnabled = true;
        }

        return null;
    }
}

public class ScriptGlobals : IScriptInterface
{
    private readonly MainViewModel mainVM;
    private readonly string? scriptPath;

    private LoaderWindow? loaderWindow;

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

    public string? ExePath => Path.GetDirectoryName(Environment.ProcessPath);

    public string ScriptErrorType => throw new NotImplementedException();

    public bool IsAppClosed => throw new NotImplementedException();

    public void AddProgress(int amount)
    {
        loaderWindow!.SetValue(loaderWindow!.Value + amount);
    }

    public void AddProgressParallel(int amount)
    {
        Interlocked.Add(ref loaderWindow!.Value, amount);

        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow!.SetValue(loaderWindow!.Value);
        }, DispatcherPriority.Background);
    }

    public void ChangeSelection(object newSelection, bool inNewTab = false)
    {
        // TODO: Implement
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
        // TODO: Implement
    }

    public void EnableUI()
    {
        mainVM.IsEnabled = true;
    }

    public string GetDecompiledText(string codeName, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
    {
        return GetDecompiledText(mainVM.Data!.Code.ByName(codeName), context, settings);
    }

    public string GetDecompiledText(UndertaleCode code, GlobalDecompileContext? context = null, IDecompileSettings? settings = null)
    {
        context ??= new(mainVM.Data);
        settings ??= mainVM.Data!.ToolInfo.DecompilerSettings;

        return new Underanalyzer.Decompiler.DecompileContext(context, code, settings).DecompileToString();
    }

    public string GetDisassemblyText(string codeName)
    {
        return GetDisassemblyText(mainVM.Data!.Code.ByName(codeName));
    }

    public string GetDisassemblyText(UndertaleCode code)
    {
        return code.Disassemble(mainVM.Data!.Variables, mainVM.Data!.CodeLocals?.For(code));
    }

    public int GetProgress()
    {
        return loaderWindow!.Value;
    }

    public void HideProgressBar()
    {
        loaderWindow?.Close();
        loaderWindow = null;
    }

    public void IncrementProgress()
    {
        loaderWindow!.SetValue(loaderWindow!.Value + 1);
    }

    public void IncrementProgressParallel()
    {
        Interlocked.Increment(ref loaderWindow!.Value);

        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow!.SetValue(loaderWindow!.Value);
        }, DispatcherPriority.Background);
    }

    public void InitializeScriptDialog()
    {
        // TODO: Implement
    }

    public bool LintUMTScript(string path)
    {
        throw new NotImplementedException();
    }

    public bool MakeNewDataFile()
    {
        return mainVM.NewData().Result;
    }

    public string? PromptChooseDirectory()
    {
        IReadOnlyList<IStorageFolder> folders = Task.Run(() => mainVM.View!.OpenFolderDialog(new()
        {
            Title = "Select directory",
        })).Result;

        if (folders.Count != 1)
            return null;

        return folders[0].TryGetLocalPath();
    }

    public string? PromptLoadFile(string? defaultExt, string? filter)
    {
        // TODO: filter
        var files = Task.Run(() => mainVM.View!.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Load file",
            FileTypeFilter = [
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
        })).Result;

        if (files.Count != 1)
            return null;

        return files[0].TryGetLocalPath();
    }

    public string? PromptSaveFile(string defaultExt, string filter)
    {
        // TODO: filter
        var file = Task.Run(() => mainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = [
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
            DefaultExtension = defaultExt,
        })).Result;

        if (file is null)
            return null;
        
        return file.TryGetLocalPath();
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

    public string? ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
    {
        // TODO: cancelText, submitText, preventClose
        return mainVM.View!.TextBoxDialog(label, defaultInput, title: title, isMultiline: isMultiline).WaitOnDispatcherFrame();
    }

    public void ScriptMessage(string message)
    {
        mainVM.ShowMessageDialog(message, title: "Script message").WaitOnDispatcherFrame();
    }

    public void ScriptOpenURL(string url)
    {
        mainVM.View!.LaunchUriAsync(new(url)).Wait();
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
        // TODO: Implement
    }

    public void SetProgress(int value)
    {
        loaderWindow!.SetValue(value);
    }

    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        loaderWindow ??= mainVM.View!.LoaderOpen();
        loaderWindow.EnsureShown();
        loaderWindow.SetMessage(message);
        loaderWindow.SetStatus(status);
        loaderWindow.SetValue((int)progressValue);
        loaderWindow.SetMaximum((int)maxValue);
    }

    public void SetProgressBar()
    {
        loaderWindow ??= mainVM.View!.LoaderOpen();
        loaderWindow.EnsureShown();
    }

    public void SetUMTConsoleText(string message)
    {
        mainVM.CommandTextBoxText = message;
    }

    public string? SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
    {
        // TODO: showDialog
        return mainVM.View!.TextBoxDialog(label, defaultValue, title: title, isMultiline: allowMultiline).WaitOnDispatcherFrame();
    }

    public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
    {
        mainVM.View!.TextBoxDialog(label, message, title: title, isMultiline: allowMultiline, isReadOnly: true).WaitOnDispatcherFrame();
    }

    public void StartProgressBarUpdater()
    {
        // TODO: Implement
    }

    public Task StopProgressBarUpdater()
    {
        // TODO: Implement
        return Task.CompletedTask;
    }

    public void SyncBinding(string resourceType, bool enable)
    {
        // TODO: Implement
    }

    public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
    {
        SetProgressBar(message, status, progressValue, maxValue);
    }

    public void UpdateProgressStatus(string status)
    {
        loaderWindow!.SetTextToMessageAndStatus(status: status);
    }

    public void UpdateProgressValue(double progressValue)
    {
        loaderWindow!.SetValue((int)progressValue);
    }
}
