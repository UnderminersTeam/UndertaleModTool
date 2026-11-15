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
using UndertaleModLib.Project;
using UndertaleModLib.Scripting;

namespace UndertaleModToolAvalonia;

public class Scripting
{
    public readonly MainViewModel MainVM;

    // State of the last script run, read by scripts through IScriptInterface.
    public bool ScriptExecutionSuccess { get; private set; } = true;
    public string ScriptErrorMessage { get; private set; } = "";
    public string ScriptErrorType { get; private set; } = "";

    // A script can turn off the "finished" message for its own run only.
    public bool FinishedMessageEnabled = true;

    public bool IsAppClosed;

    public Scripting(IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
    }

    /// <summary>
    /// Runs a script and shows its result (or "finished" message, or error) in the command text box.
    /// </summary>
    public async Task<object?> RunScript(string text, string? filePath = null)
    {
        ClearError();

        // A script can run another script with RunUMTScript, and the UI must stay disabled until the outer one is done.
        bool wasEnabled = MainVM.IsEnabled;

        try
        {
            MainVM.IsEnabled = false;

            Script<object?> script = CreateScript(text, filePath);

            if (!await CompileScript(script))
                return null;

            ScriptGlobals scripting = new(this, filePath);

            try
            {
                ScriptState<object?> state = await script.RunAsync(scripting);

                // A nested script may have failed and set the error state; this script itself succeeded.
                ClearError();

                string defaultMessage = filePath is not null ? $"{Path.GetFileName(filePath)} finished!" : "";
                ShowFinishedMessage(state.ReturnValue?.ToString() ?? defaultMessage);

                return state.ReturnValue;
            }
            catch (ScriptException e)
            {
                SetError(e.Message, "Exception");
                await MainVM.View!.MessageDialog(e.Message, title: "Error from script");
            }
            catch (Exception e)
            {
                SetError(e.Message, "Exception");
                await MainVM.View!.MessageDialog(e.ToString(), title: "Script execution error");
            }
            finally
            {
                scripting.Dispose();
            }
        }
        finally
        {
            MainVM.IsEnabled = wasEnabled;
        }

        return null;
    }

    /// <summary>
    /// Compiles a script without running it. Returns whether it compiled without errors.
    /// </summary>
    public async Task<bool> LintScript(string text, string? filePath = null)
    {
        ClearError();

        return await CompileScript(CreateScript(text, filePath));
    }

    static Script<object?> CreateScript(string text, string? filePath)
    {
        return CSharpScript.Create(text, ScriptOptions.Default
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
                "UndertaleModLib",
                // Used by bundled scripts: System.Timers (AutoBackup) and System.Text.Json (the JSON importers and exporters).
                "System.ComponentModel.TypeConverter",
                "System.Text.Json",
                "System.Text.Encodings.Web")
            .WithFilePath(filePath)
            .WithFileEncoding(Encoding.Default)
            .WithEmitDebugInformation(true),
            typeof(IScriptInterface));
    }

    async Task<bool> CompileScript(Script<object?> script)
    {
        ImmutableArray<Diagnostic> diagnostics = await Task.Run(() => script.Compile());

        IEnumerable<Diagnostic> errors = diagnostics.Where((Diagnostic diagnostic) => diagnostic.Severity == DiagnosticSeverity.Error);
        if (!errors.Any())
            return true;

        string message = String.Join("\n", errors);
        SetError(message, "CompilationErrorException");
        await MainVM.View!.MessageDialog(message, title: "Script compilation error");

        return false;
    }

    void ClearError()
    {
        ScriptExecutionSuccess = true;
        ScriptErrorMessage = "";
        ScriptErrorType = "";
    }

    void SetError(string message, string type)
    {
        ScriptExecutionSuccess = false;
        ScriptErrorMessage = message;
        ScriptErrorType = type;
        MainVM.CommandTextBoxText = message;
    }

    void ShowFinishedMessage(string message)
    {
        if (FinishedMessageEnabled)
            MainVM.CommandTextBoxText = message;
        else
            FinishedMessageEnabled = true;
    }
}

public class ScriptGlobals : IScriptInterface, IDisposable
{
    private readonly Scripting scripting;
    private readonly MainViewModel mainVM;
    private readonly string? scriptPath;

    private ILoaderWindow? loaderWindow;
    private int loaderValue;

    // While the progress bar updater runs, progress changes aren't posted one by one to the UI thread;
    // a timer shows the latest value instead, which is cheaper for scripts that make many changes.
    private DispatcherTimer? progressBarUpdater;
    private volatile bool isProgressBarUpdaterRunning;

    public ScriptGlobals(Scripting scripting, string? scriptPath)
    {
        this.scripting = scripting;
        mainVM = scripting.MainVM;
        this.scriptPath = scriptPath;
    }

    public void Dispose()
    {
        progressBarUpdater?.Stop();
        progressBarUpdater = null;
        isProgressBarUpdaterRunning = false;

        loaderWindow?.Close();
        loaderWindow = null;
    }

    public UndertaleData? Data => mainVM.Data;

    public ProjectContext? Project => mainVM.Project;

    public string? FilePath => mainVM.DataPath;

    public string? ScriptPath => scriptPath;

    // The item selected in the data explorer tree.
    public object? Highlighted => mainVM.DataExplorer.HighlightedValue;

    // The resource open in the current tab. Tabs that aren't a resource, like General info, give null.
    public object? Selected => (mainVM.Tabs.TabSelected?.Content as IUndertaleResourceViewModel)?.Resource;

    public bool CanSave => mainVM.Data is not null;

    public bool ScriptExecutionSuccess => scripting.ScriptExecutionSuccess;

    public string ScriptErrorMessage => scripting.ScriptErrorMessage;

    public string? ExePath => Path.GetDirectoryName(Environment.ProcessPath);

    public string ScriptErrorType => scripting.ScriptErrorType;

    public bool IsAppClosed => scripting.IsAppClosed;

    public Action<Action> MainThreadAction => Dispatcher.UIThread.Invoke;

    void PostProgressValue(DispatcherPriority priority)
    {
        if (isProgressBarUpdaterRunning)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow?.SetValue(loaderValue);
        }, priority);
    }

    public void AddProgress(int amount)
    {
        loaderValue += amount;
        PostProgressValue(DispatcherPriority.Default);
    }

    public void AddProgressParallel(int amount)
    {
        Interlocked.Add(ref loaderValue, amount);
        PostProgressValue(DispatcherPriority.Background);
    }

    public void ChangeSelection(object newSelection, bool inNewTab = false)
    {
        // Wait so the script continues only after the tab is open (which may ask to save the current tab), like the WPF version.
        WaitOnUIThread(() => mainVM.Tabs.TabOpen(newSelection, inNewTab));
    }

    public Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string>? failedList = null)
    {
        ShowSearchResults(title, query, resultsDict, showInDecompiledView, failedList);
        return Task.CompletedTask;
    }

    public Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string>? failedList = null)
    {
        ShowSearchResults(title, query, resultsDict, showInDecompiledView, failedList);
        return Task.CompletedTask;
    }

    // Shows the results in a "Search in code" window, where they can be opened like the window's own results.
    // Search scripts may gather results inside Task.Run and call this from there, but windows must be created on the UI thread.
    void ShowSearchResults(string title, string query, IEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> results, bool showInDecompiledView, IEnumerable<string>? failedList)
    {
        if (mainVM.View is not MainView mainView)
            return;

        WaitOnUIThread(() =>
        {
            SearchInCodeViewModel searchVM = new(mainVM.ServiceProvider);
            searchVM.ShowResults(query, results, inAssembly: !showInDecompiledView, failedCount: failedList?.Count() ?? 0);
            mainView.OpenSearchInCode(searchVM, title);
            return Task.FromResult(true);
        });
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

        return new DecompileContext(context, code, settings).DecompileToString();
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
        return loaderValue;
    }

    public void HideProgressBar()
    {
        loaderWindow?.Close();
        loaderWindow = null;
    }

    public void IncrementProgress()
    {
        loaderValue++;
        PostProgressValue(DispatcherPriority.Default);
    }

    public void IncrementProgressParallel()
    {
        Interlocked.Increment(ref loaderValue);
        PostProgressValue(DispatcherPriority.Background);
    }

    public void InitializeScriptDialog()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            loaderWindow ??= mainVM.View!.LoaderOpen();
            loaderWindow.SetMessage("Script in progress...");
            loaderWindow.SetStatus("Please wait...");
        });
    }

    public bool LintUMTScript(string path)
    {
        if (!File.Exists(path))
        {
            ScriptError(path + " does not exist!");
            return false;
        }

        string text = File.ReadAllText(path);
        return WaitOnUIThread(() => scripting.LintScript(text, path));
    }

    // Scripts can call this from a background thread (e.g. LintAllScripts.csx lints inside Task.Run), but scripts and dialogs run on the UI thread.
    static T WaitOnUIThread<T>(Func<Task<T>> action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            return action().WaitOnDispatcherFrame();

        return Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }

    public bool MakeNewDataFile()
    {
        mainVM.NewData();
        return true;
    }

    public string? PromptChooseDirectory()
    {
        IReadOnlyList<IStorageFolder> folders = WaitOnUIThread(() => mainVM.View!.OpenFolderDialog(new()
        {
            Title = "Select directory",
        }));

        if (folders.Count != 1)
            return null;

        return folders[0].TryGetLocalPath();
    }

    public string? PromptLoadFile(string? defaultExt, string? filter)
    {
        var files = WaitOnUIThread(() => mainVM.View!.OpenFileDialog(new FilePickerOpenOptions()
        {
            Title = "Load file",
            FileTypeFilter = ScriptFileTypes(filter),
        }));

        if (files.Count != 1)
            return null;

        return files[0].TryGetLocalPath();
    }

    public string? PromptSaveFile(string defaultExt, string filter)
    {
        var file = WaitOnUIThread(() => mainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save file",
            FileTypeChoices = ScriptFileTypes(filter),
            DefaultExtension = ScriptDefaultExtension(defaultExt),
        }));

        if (file is null)
            return null;

        return file.TryGetLocalPath();
    }

    // Scripts that pass no filter get the data file filter, like in the WPF version.
    static IReadOnlyList<FilePickerFileType> ScriptFileTypes(string? filter)
    {
        if (filter is null)
            return FilePickerFileTypes.Data;

        List<FilePickerFileType> types = FilePickerFileTypes.FromWindowsFilter(filter);
        if (types.Count == 0)
            return FilePickerFileTypes.All;

        return types;
    }

    // Scripts pass extensions both with and without the dot ("txt", ".txt"), and null means a data file, like in the WPF version.
    static string? ScriptDefaultExtension(string? defaultExt)
    {
        if (defaultExt is null)
            return ".win";
        if (defaultExt == "")
            return null;

        return defaultExt.StartsWith('.') ? defaultExt : "." + defaultExt;
    }

    public bool RunUMTScript(string path)
    {
        if (!File.Exists(path))
        {
            ScriptError(path + " does not exist!");
            return false;
        }

        // Errors are already shown by RunScript, so unlike the WPF version, no second error message is shown here.
        string text = File.ReadAllText(path);
        WaitOnUIThread(() => scripting.RunScript(text, path));
        return scripting.ScriptExecutionSuccess;
    }

    public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
    {
        mainVM.View!.MessageDialog(error, title).WaitOnDispatcherFrame();

        if (SetConsoleText)
        {
            mainVM.CommandTextBoxText = error;
        }
    }

    public string? ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose)
    {
        return mainVM.View!.TextBoxDialog(label, defaultInput, title: title, isMultiline: isMultiline,
            okText: submitText, cancelText: cancelText, preventClose: preventClose).WaitOnDispatcherFrame();
    }

    public void ScriptMessage(string message)
    {
        mainVM.View!.MessageDialog(message, title: "Script message").WaitOnDispatcherFrame();
    }

    public void ExportRoomAsPNG(UndertaleRoom room, string filePath)
    {
        ArgumentNullException.ThrowIfNull(room);

        // The room renderer shares the app's image cache with the open editors, and that cache isn't thread-safe.
        // It renders into memory first so a failed render doesn't leave a broken PNG behind, or replace a good one.
        MemoryStream png = new();
        WaitOnUIThread(async () =>
        {
            await ImportExport.ExportRoomAsPNG(room, png);
            return true;
        });
        File.WriteAllBytes(filePath, png.ToArray());
    }

    public void ScriptOpenURL(string url)
    {
        mainVM.View!.LaunchUriAsync(new(url)).Wait();
    }

    public bool ScriptQuestion(string message)
    {
        return mainVM.View!.MessageDialog(message, "Script question", MessageWindow.Buttons.YesNo).WaitOnDispatcherFrame() == MessageWindow.Result.Yes;
    }

    public void ScriptWarning(string message)
    {
        mainVM.View!.MessageDialog(message, title: "Script warning").WaitOnDispatcherFrame();
    }

    public void SetFinishedMessage(bool isFinishedMessageEnabled)
    {
        scripting.FinishedMessageEnabled = isFinishedMessageEnabled;
    }

    public void SetProgress(int value)
    {
        loaderValue = value;
        PostProgressValue(DispatcherPriority.Default);
    }

    public void SetProgressBar(string message, string status, double progressValue, double maxValue)
    {
        loaderValue = (int)progressValue;

        Dispatcher.UIThread.Invoke(() =>
        {
            loaderWindow ??= mainVM.View!.LoaderOpen();
            loaderWindow.EnsureShown();
            loaderWindow.SetMessage(message);
            loaderWindow.SetStatus(status);
            loaderWindow.SetValue(loaderValue);
            loaderWindow.SetMaximum((int)maxValue);
        });
    }

    public void SetProgressBar()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            loaderWindow ??= mainVM.View!.LoaderOpen();
            loaderWindow.EnsureShown();
        });
    }

    public void SetUMTConsoleText(string message)
    {
        mainVM.CommandTextBoxText = message;
    }

    public string? SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true)
    {
        // Like the WPF version, a non-blocking input window lets the script continue right away, so there's no input to return.
        if (!showDialog)
        {
            mainVM.View!.TextBoxShow(label, defaultValue, title: title, isMultiline: allowMultiline);
            return null;
        }

        return mainVM.View!.TextBoxDialog(label, defaultValue, title: title, isMultiline: allowMultiline).WaitOnDispatcherFrame();
    }

    public void SimpleTextOutput(string title, string label, string message, bool allowMultiline)
    {
        mainVM.View!.TextBoxDialog(label, message, title: title, isMultiline: allowMultiline, isReadOnly: true).WaitOnDispatcherFrame();
    }

    public void StartProgressBarUpdater()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (progressBarUpdater is not null)
                return;

            // About 30 times per second, like the WPF version.
            progressBarUpdater = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Background,
                (_, _) => loaderWindow?.SetValue(loaderValue));
            progressBarUpdater.Start();
            isProgressBarUpdaterRunning = true;
        });
    }

    public Task StopProgressBarUpdater()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            if (progressBarUpdater is null)
                return;

            progressBarUpdater.Stop();
            progressBarUpdater = null;
            isProgressBarUpdaterRunning = false;

            // Show the final value, which the timer may not have shown yet.
            loaderWindow?.SetValue(loaderValue);
        });

        return Task.CompletedTask;
    }

    public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
    {
        SetProgressBar(message, status, progressValue, maxValue);
    }

    public void UpdateProgressStatus(string status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            loaderWindow?.SetTextToMessageAndStatus(status: status);
        });
    }

    public void UpdateProgressValue(double progressValue)
    {
        loaderValue = (int)progressValue;
        PostProgressValue(DispatcherPriority.Default);
    }
}
