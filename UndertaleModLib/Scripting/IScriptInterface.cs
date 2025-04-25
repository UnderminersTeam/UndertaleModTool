﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Underanalyzer.Decompiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Scripting;

/// <summary>
/// The exception that is thrown when trivial errors happen during runtime of UndertaleModTool scripts. <br/>
/// This exception does not contain a stacktrace and should more be handled like an error message that stops execution of the currently running script.
/// </summary>
/// <example><code>if (Data is null) throw new ScriptException("Please load data.win first!);</code></example>
public class ScriptException : Exception
{
    /// <summary>
    /// Initializes a new instance of the IOException class with its message string set to the empty string ("").
    /// </summary>
    public ScriptException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the IOException class with its message string set to <paramref name="msg"/>.
    /// </summary>
    /// <param name="msg">A <see cref="String"/> that describes the error. The content of <paramref name="msg"/> is intended to be understood by humans.</param>
    public ScriptException(string msg) : base(msg)
    {
    }
}

/// <summary>
/// Defines a generalized set of attributes and methods that a value type or class implements
/// to be able to interact with UndertaleModTool-Scripts.
/// </summary>
public interface IScriptInterface
{
    /// <summary>
    /// The data file.
    /// </summary>
    UndertaleData Data { get; }

    /// <summary>
    /// The file path where <see cref="Data"/> resides.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// The path of the current executed script.
    /// </summary>
    string ScriptPath { get; }

    /// <summary>
    /// The object that's currently highlighted in the GUI.
    /// </summary>
    object Highlighted { get; }

    /// <summary>
    /// The object that's currently selected in the GUI.
    /// </summary>
    object Selected { get; }

    /// <summary>
    /// Indicates whether saving is currently enabled.
    /// </summary>
    bool CanSave { get; }

    /// <summary>
    /// Indicates whether the last script executed successfully or not.
    /// </summary>
    bool ScriptExecutionSuccess { get; }

    /// <summary>
    /// Error message of the last executed script. Will be <c>""</c> (<see cref="String.Empty"/>) if no error occurred.
    /// </summary>
    string ScriptErrorMessage { get; }

    /// <summary>
    /// Path of the main executable that's currently running.
    /// </summary>
    /// <remarks>For example <c>C://Users/me/UMT/UMT.exe</c> or <c>/bin/UMTCLI</c>.</remarks>
    string ExePath { get; }

    /// <summary>
    /// A string, detailing the type of the last encountered error.
    /// </summary>
    string ScriptErrorType { get; }

    /// <summary>
    /// Indicating whether the Program is currently closed.
    /// //TODO: Only GUI + ExportAllRoomsToPng.csx uses this, but nothing should ever need to access this value.
    /// <c>"somehow Dispatcher.Invoke() in a loop creates executable code queue that doesn't clear on app closing."</c>
    /// </summary>
    bool IsAppClosed { get; }

    /// <summary>
    /// Ensures that a valid data file (<see cref="Data"/>) is loaded. An exception should be thrown if it isn't.
    /// </summary>
    void EnsureDataLoaded()
    {
        if (Data is null)
            throw new ScriptException("No data file is currently loaded!");
    }

    /// <summary>
    /// Creates a new Data file asynchronously.
    /// </summary>
    /// <returns><see langword="true"/> if successful, <see langword="false"/> if not.</returns>
    bool MakeNewDataFile();

    /// <summary>
    /// Used in Scripts in order to show a message to the user.
    /// </summary>
    /// <param name="message">The message to show.</param>
    void ScriptMessage(string message);

    //TODO: currently should get repurposed/renamed?
    /// <summary>
    /// Sets the message of the variable holding text from the console. Currently only used in GUI.
    /// </summary>
    /// <param name="message">The message to set it to.</param>
    void SetUMTConsoleText(string message);

    /// <summary>
    /// Used in Scripts in order to ask a yes/no question to the user which they can answer.
    /// </summary>
    /// <param name="message">The message to ask.</param>
    /// <returns><see langword="true"/> if user affirmed the question, <see langword="false"/> if not.</returns>
    bool ScriptQuestion(string message);

    /// <summary>
    /// Used in Scripts in order to show an error to the user.
    /// </summary>
    /// <param name="error">The error message to show.</param>
    /// <param name="title">A short-descriptive title.</param>
    /// <param name="SetConsoleText">Whether to call <see cref="SetUMTConsoleText"/> with <paramref name="error"/>.</param>
    //TODO: setConsoleText should get a *clearer* name
    void ScriptError(string error, string title = "Error", bool SetConsoleText = true);

    /// <summary>
    /// Used in Scripts in order to open a URL in the users' browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    void ScriptOpenURL(string url);

    /// <summary>
    /// Run a C# UndertaleModLib compatible script file.
    /// </summary>
    /// <param name="path">File path to the script file to execute.</param>
    /// <returns>A <see cref="bool"/> that indicates whether the execution of the script was successful.</returns>
    bool RunUMTScript(string path);

    /// <summary>
    /// Lint whether a file is C# UndertaleModLib compatible.
    /// </summary>
    /// <param name="path">File path to the script file to lint.</param>
    /// <returns>A <see cref="bool"/> that indicates whether the linting was successful.</returns>
    bool LintUMTScript(string path);

    /// <summary>
    /// Initializes a Script Dialog with default values
    /// </summary>
    void InitializeScriptDialog();

    /// <summary>
    ///Get the decompiled text from a code entry (like <c>gml_Script_moveTo</c>).
    /// </summary>
    /// <param name="codeName">The name of the code entry from which to get the decompiled code from.</param>
    /// <param name="context">The GlobalDecompileContext to use for decompilation.</param>
    /// <param name="settings">The settings to use for decompilation, or <see langword="null"/> to use the default.</param>
    /// <returns>Decompiled text as a <see cref="string"/>.</returns>
    /// <remarks>This will return a string, even if the decompilation failed! Usually commented out and featuring
    /// <c>DECOMPILER FAILED!</c> .</remarks>
    string GetDecompiledText(string codeName, GlobalDecompileContext context = null, IDecompileSettings settings = null);

    /// <summary>
    /// Get the decompiled text from an <see cref="UndertaleCode"/> object.
    /// </summary>
    /// <param name="code">The object from which to get the decompiled code from.</param>
    /// <param name="context">The GlobalDecompileContext to use for decompilation.</param>
    /// <param name="settings">The settings to use for decompilation, or <see langword="null"/> to use the default.</param>
    /// <returns>Decompiled text as a <see cref="string"/>.</returns>
    /// <remarks>This will return a string, even if the decompilation failed! Usually commented out and featuring
    /// <c>DECOMPILER FAILED!</c> .</remarks>
    string GetDecompiledText(UndertaleCode code, GlobalDecompileContext context = null, IDecompileSettings settings = null);

    /// <summary>
    ///  Get the disassembly from a code entry (like <c>gml_Script_moveTo</c>).
    /// </summary>
    /// <param name="codeName">The name of the code entry from which to get the disassembly from.</param>
    /// <returns>Disassembly as <see cref="string"/>.</returns>
    /// <remarks>This will return a string, even if the disassembly failed! Usually commented out and featuring
    /// <c>DISASSEMBLY FAILED!</c> .</remarks>
    string GetDisassemblyText(string codeName);

    /// <summary>
    /// Get the disassembly from an <see cref="UndertaleCode"/> object.
    /// </summary>
    /// <param name="code">The object from which to get the disassembly from.</param>
    /// <returns>Disassembly as <see cref="string"/>.</returns>
    /// <remarks>This will return a string, even if the disassembly failed! Usually commented out and featuring
    /// <c>DISASSEMBLY FAILED!</c> .</remarks>
    string GetDisassemblyText(UndertaleCode code);

    /// <summary>
    /// Check whether two files are identical.
    /// </summary>
    /// <param name="file1">File path to first file.</param>
    /// <param name="file2">File path to second file.</param>
    /// <returns>A <see cref="bool"/> that indicates whether the files are identical or not.</returns>
    bool AreFilesIdentical(string file1, string file2)
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

    /// <summary>
    /// Allows the user to input text with the option to cancel it.
    /// </summary>
    /// <param name="title">A short descriptive title.</param>
    /// <param name="label">A label describing what the user should input.</param>
    /// <param name="defaultInput">The default value of the input.</param>
    /// <param name="cancelText">The text of the cancel button.</param>
    /// <param name="submitText">The text of the submit button.</param>
    /// <param name="isMultiline">Whether to allow the input to have multiple lines.</param>
    /// <param name="preventClose">Whether the window is allowed to be closed.
    /// Should this be set to <see langword="false"/>, then there also won't be a close button.</param>
    /// <returns>The text that the user inputted.</returns>
    string ScriptInputDialog(string title, string label, string defaultInput, string cancelText, string submitText, bool isMultiline, bool preventClose);

    /// <summary>
    /// Allows the user to input text in a simple dialog.
    /// </summary>
    /// <param name="title">A short descriptive title.</param>
    /// <param name="label">A label describing what the user should input.</param>
    /// <param name="defaultValue">The default value of the input.</param>
    /// <param name="allowMultiline">Whether to allow the input to have multiple lines.</param>
    /// <param name="showDialog">Whether to block the parent window and only continue after the dialog is cleared.</param>
    /// <returns>The text that the user inputted.</returns>
    string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline, bool showDialog = true);

    /// <summary>
    /// Shows simple output to the user.
    /// </summary>
    /// <param name="title">A short descriptive title.</param>
    /// <param name="label">A label describing the output.</param>
    /// <param name="message">The message to convey to the user.</param>
    /// <param name="allowMultiline">Whether to allow the message to be multiline or not.
    /// Should this be false but <paramref name="message"/> have multiple lines, then only the first line will be shown.</param>
    void SimpleTextOutput(string title, string label, string message, bool allowMultiline);

    /// <summary>
    /// Shows search output with clickable text to the user.
    /// </summary>
    /// <param name="title">A short descriptive title.</param>
    /// <param name="query">The query that was searched for.</param>
    /// <param name="resultsCount">How many results have been found.</param>
    /// <param name="resultsDict">An <see cref="IOrderedEnumerable{TElement}"/> of type <see cref="KeyValuePair{TKey,TValue}"/>,
    /// with TKey being the name of the code entry an TValue being a list of tuples where the first item is the matching code line number and the second one is the code line itself.</param>
    /// <param name="showInDecompiledView">Whether to open the "Decompiled" view or the "Disassembly" view when clicking on an entry name.</param>
    /// <param name="failedList">A list of code entries that encountered an error while searching.</param>
    /// <returns>A task that represents the search output.</returns>
    Task ClickableSearchOutput(string title, string query, int resultsCount, IOrderedEnumerable<KeyValuePair<string, List<(int lineNum, string codeLine)>>> resultsDict, bool showInDecompiledView, IOrderedEnumerable<string> failedList = null);

    /// <summary>
    /// Shows search output with clickable text to the user.
    /// </summary>
    /// <param name="title">A short descriptive title.</param>
    /// <param name="query">The query that was searched for.</param>
    /// <param name="resultsCount">How many results have been found.</param>
    /// <param name="resultsDict">A <see cref="Dictionary{TKey,TValue}"/> with TKey being the name of the code entry and
    /// TValue being a list of tuples where the first item is the matching code line number and the second one is the code line itself.</param>
    /// <param name="showInDecompiledView">Whether to open the "Decompiled" view or the "Disassembly" view when clicking on an entry name.</param>
    /// <param name="failedList">A list of code entries that encountered an error while searching.</param>
    /// <returns>A task that represents the search output.</returns>
    Task ClickableSearchOutput(string title, string query, int resultsCount, IDictionary<string, List<(int lineNum, string codeLine)>> resultsDict, bool showInDecompiledView, IEnumerable<string> failedList = null);

    /// <summary>
    /// Sets whether or not the current script should show a message indicating that it completed.
    /// </summary>
    /// <param name="isFinishedMessageEnabled">True to enable the finished message; false otherwise.</param>
    void SetFinishedMessage(bool isFinishedMessageEnabled);

    /// <summary>
    /// Updates the progress bar. Not to be called directly in scripts! Use <see cref="SetProgressBar(string, string, double, double)"/> instead!
    /// </summary>
    /// <param name="message"></param>
    /// <param name="status"></param>
    /// <param name="progressValue"></param>
    /// <param name="maxValue"></param>
    void UpdateProgressBar(string message, string status, double progressValue, double maxValue);

    /// <summary>
    /// Sets the progress bar dialog to a certain value.
    /// </summary>
    /// <param name="message">What the progress bar is describing.</param>
    /// <param name="status">What the current status is. For example <c>Decompiling...</c>.</param>
    /// <param name="progressValue">The value to set the progress bar to.</param>
    /// <param name="maxValue">The max value of the progress bar.</param>
    void SetProgressBar(string message, string status, double progressValue, double maxValue);

    /// <summary>
    /// Show the progress bar.
    /// </summary>
    void SetProgressBar();

    /// <summary>
    /// Updates the value of the current running progress bar dialog.
    /// </summary>
    /// <param name="progressValue">The new value to set the progress bar to.</param>
    void UpdateProgressValue(double progressValue);

    /// <summary>
    /// Updates the status of the current running progress bar dialog.
    /// </summary>
    /// <param name="status">The new status. For example <c>Decompiling...</c>.</param>
    void UpdateProgressStatus(string status);

    //TODO: considering this forces everything that implements this to have their own progressValue,
    //why not make that a necessary attribute?
    /// <summary>
    /// Adds a certain amount to the variable holding a progress value.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    void AddProgress(int amount);

    /// <summary>
    /// Increments the variable holding a progress value by one.
    /// </summary>
    void IncrementProgress();

    /// <summary>
    /// Adds a certain amount to the variable holding a progress value in.
    /// Used for parallel operations, as it is thread-safe.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    void AddProgressParallel(int amount);

    /// <summary>
    /// Increments the variable holding a progress value by one.
    /// Used for parallel operations, as it is thread-safe.
    /// </summary>
    void IncrementProgressParallel();

    /// <summary>
    /// Gets the value of the variable holding a progress value.
    /// </summary>
    /// <returns>The value as <see cref="int"/>.</returns>
    int GetProgress();

    /// <summary>
    /// Sets the value of the variable holding a progress variable to another value.
    /// </summary>
    /// <param name="value">The new value for the progress variable.</param>
    void SetProgress(int value);

    /// <summary>
    /// Hides the progress bar.
    /// </summary>
    void HideProgressBar();

    /// <summary>
    /// Enables the UI.
    /// </summary>
    void EnableUI();

    /// <summary>
    /// Allows scripts to modify asset lists from the non-UI thread.
    /// If this isn't called before attempting to modify them, a <see cref="NotSupportedException"/> will be thrown.
    /// </summary>
    /// <param name="resourceType">A comma separated list of asset list names. This is case sensitive.</param>
    /// <param name="enable">Whether to enable or disable the synchronization.</param>
    //TODO: Having resourceType as a comma separated list just screams for error. Make it use some array of predefined assets it can use.
    void SyncBinding(string resourceType, bool enable);

    /// <summary>
    /// Stops the synchronization of all previously enabled assets.
    /// </summary>
    void DisableAllSyncBindings();

    /// <summary>
    /// Starts the task that updates a progress bar in parallel.
    /// </summary>
    void StartProgressBarUpdater();

    /// <summary>
    /// Stops the task that updates a progress bar in parallel.
    /// </summary>
    /// <returns>A task that represents the stopped progress updater.</returns>
    Task StopProgressBarUpdater();

    /// <summary>
    /// Changes the currently selected in the GUI.
    /// </summary>
    /// <param name="newSelection">The new object that should now be selected.</param>
    /// <param name="inNewTab">Whether the object should be open in a new tab.</param>
    void ChangeSelection(object newSelection, bool inNewTab = false);

    /// <summary>
    /// Used to prompt the user for a directory.
    /// </summary>
    /// <returns>The directory selected by the user.</returns>
    string PromptChooseDirectory();

    /// <summary>
    /// Used to prompt the user for a file.
    /// </summary>
    /// <param name="defaultExt">The default extension that should be selected.</param>
    /// <param name="filter">The filters used for the file select.</param>
    /// <returns>The file selected by the user.</returns>
    string PromptLoadFile(string defaultExt, string filter);
}