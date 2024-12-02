using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for SearchInCodeWindow.xaml
    /// </summary>
    public partial class SearchInCodeWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        bool isCaseSensitive;
        bool isRegexSearch;
        bool isInAssembly;
        string text;

        bool usingGMLCache;

        int progressCount = 0;
        int resultCount = 0;

        public readonly record struct Result(string Code, int LineNumber, string LineText);

        public ObservableCollection<Result> Results { get; set; } = new();

        ConcurrentDictionary<string, List<(int, string)>> resultsDict;
        ConcurrentBag<string> failedList;
        IEnumerable<KeyValuePair<string, List<(int, string)>>> resultsDictSorted;
        IEnumerable<string> failedListSorted;
        
        Regex keywordRegex;

        ThreadLocal<GlobalDecompileContext> decompileContext;

        LoaderDialog loaderDialog;

        private UndertaleCodeEditor.CodeEditorTab editorTab;

        public SearchInCodeWindow()
        {
            InitializeComponent();
        }

        public void ActivateAndFocusOnTextBox()
        {
            Activate();
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await Search();
        }

        async Task Search()
        {
            // TODO: Allow this be cancelled, probably make loader inside this window itself.

            if (mainWindow.Data == null)
            {
                this.ShowError("No data.win loaded.");
                return;
            }

            if (mainWindow.Data.IsYYC())
            {
                this.ShowError("Can't search code in YYC game, there's no code to search.");
                return;
            }

            text = SearchTextBox.Text;

            if (String.IsNullOrEmpty(text))
                return;

            isCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? false;
            isRegexSearch = RegexSearchCheckBox.IsChecked ?? false;
            isInAssembly = InAssemblyCheckBox.IsChecked ?? false;

            if (isRegexSearch)
            {
                keywordRegex = new(text, isCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            mainWindow.IsEnabled = false;
            this.IsEnabled = false;

            loaderDialog = new("Searching...", null);
            loaderDialog.Owner = this;
            loaderDialog.PreventClose = true;
            loaderDialog.Show();

            Results.Clear();

            resultsDict = new();
            failedList = new();
            resultsDictSorted = null;
            failedListSorted = null;
            progressCount = 0;
            resultCount = 0;

            if (!isInAssembly)
            {
                decompileContext = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(mainWindow.Data, false));

                // HACK: This could be problematic
                usingGMLCache = await mainWindow.GenerateGMLCache(decompileContext, loaderDialog);

                // If we run script before opening any code
                if (!usingGMLCache && mainWindow.Data.KnownSubFunctions is null)
                {
                    loaderDialog.Maximum = null;
                    loaderDialog.Update("Building the cache of all sub-functions...");

                    await Task.Run(() => Decompiler.BuildSubFunctionCache(mainWindow.Data));
                }
            }

            loaderDialog.SavedStatusText = "Code entries";
            loaderDialog.Update(null, "Code entries", 0, mainWindow.Data.Code.Count);

            if (!isInAssembly && usingGMLCache)
            {
                await Task.Run(() => Parallel.ForEach(mainWindow.Data.GMLCache, SearchInGMLCache));
            }
            else
            {
                await Task.Run(() => Parallel.ForEach(mainWindow.Data.Code, SearchInUndertaleCode));
            }

            await Task.Run(SortResults);

            loaderDialog.Maximum = null;
            loaderDialog.Update("Generating result list...");

            editorTab = isInAssembly ? UndertaleCodeEditor.CodeEditorTab.Disassembly : UndertaleCodeEditor.CodeEditorTab.Decompiled;

            ShowResults();

            loaderDialog.PreventClose = false;
            loaderDialog.Close();
            loaderDialog = null;

            mainWindow.IsEnabled = true;
            this.IsEnabled = true;
        }

        void SearchInGMLCache(KeyValuePair<string, string> code)
        {
            SearchInCodeText(code.Key, TryGetProfileModeGML(code.Key) ?? code.Value);

            Interlocked.Increment(ref progressCount);
            Dispatcher.Invoke(() => loaderDialog.ReportProgress(progressCount));
        }

        void SearchInUndertaleCode(UndertaleCode code)
        {
            try
            {
                if (code is not null && code.ParentEntry is null)
                {
                    var codeText = isInAssembly
                        ? code.Disassemble(mainWindow.Data.Variables, mainWindow.Data.CodeLocals.For(code))
                        : TryGetProfileModeGML(code.Name.Content)
                            ?? Decompiler.Decompile(code, decompileContext.Value);
                    SearchInCodeText(code.Name.Content, codeText);
                }
                
            }
            // TODO: Look at specific exceptions
            catch (Exception e)
            {
                failedList.Add(code.Name.Content);
            }

            Interlocked.Increment(ref progressCount);
            Dispatcher.Invoke(() => loaderDialog.ReportProgress(progressCount));
        }

        static string TryGetProfileModeGML(string codeName)
        {
            if (SettingsWindow.ProfileModeEnabled)
            {
                string path = Path.Join(Settings.ProfilesFolder, mainWindow.ProfileHash, "Temp", codeName + ".gml");
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }
            return null;
        }

        void SearchInCodeText(string codeName, string codeText)
        {
            var lineNumber = 0;
            StringReader codeTextReader = new(codeText);
            bool nameWritten = false;
            string lineText;
            while ((lineText = codeTextReader.ReadLine()) is not null)
            {
                lineNumber += 1;
                if (lineText == string.Empty)
                    continue;

                if (((isRegexSearch && keywordRegex.Match(lineText).Success) || ((!isRegexSearch && isCaseSensitive) ? lineText.Contains(text) : lineText.Contains(text, StringComparison.CurrentCultureIgnoreCase))))
                {
                    if (nameWritten == false)
                    {
                        resultsDict[codeName] = new List<(int, string)>();
                        nameWritten = true;
                    }
                    resultsDict[codeName].Add((lineNumber, lineText));
                    Interlocked.Increment(ref resultCount);
                }
            }
        }

        void SortResults()
        {
            string[] codeNames = mainWindow.Data.Code.Select(x => x.Name.Content).ToArray();

            resultsDictSorted = resultsDict.OrderBy(c => Array.IndexOf(codeNames, c.Key));
            failedListSorted = failedList;

            if (!isInAssembly && mainWindow.Data.GMLCacheFailed?.Count > 0)
                failedListSorted = failedListSorted.Concat(mainWindow.Data.GMLCacheFailed);

            failedListSorted = failedListSorted.OrderBy(c => Array.IndexOf(codeNames, c));
        }

        public void ShowResults()
        {
            foreach (var result in resultsDictSorted)
            {
                var code = result.Key;
                foreach (var (lineText, lineNumber) in result.Value)
                {
                    Results.Add(new(code, lineText, lineNumber));
                }
            }

            string str = $"{resultCount} result{(resultCount > 1 ? "s" : "")} found in {resultsDictSorted.Count()} code entr{(resultsDictSorted.Count() > 1 ? "ies" : "y")}.";
            if (failedListSorted.Count() > 0)
            {
                str += $" {failedListSorted.Count()} code entr{(failedListSorted.Count() > 1 ? "ies" : "y")} with an error.";
            }
            StatusBarTextBlock.Text = str;
        }

        void OpenSelectedListViewItem(bool inNewTab=false)
        {
            foreach (Result result in ResultsListView.SelectedItems)
            {
                mainWindow.OpenCodeEntry(result.Code, result.LineNumber, editorTab, inNewTab);
                // Only first one opens in current tab, the rest go into new tabs.
                inNewTab = true;
            }
        }

        static void CopyListViewItems(IEnumerable items)
        {
            string str = String.Join("\n", items
                .Cast<Result>()
                .Select(result => $"{result.Code}\t{result.LineNumber}\t{result.LineText}"));
            Clipboard.SetText(str);
        }

        private async void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await Search();
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedListViewItem();
        }

        private void ListViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                OpenSelectedListViewItem();
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedListViewItem();
        }

        private void MenuItemOpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedListViewItem(true);
        }

        private void MenuItemCopyAll_Click(object sender, RoutedEventArgs e)
        {
            CopyListViewItems(ResultsListView.Items);
        }

        private void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CopyListViewItems(ResultsListView.SelectedItems.Cast<Result>().OrderBy(item => ResultsListView.Items.IndexOf(item)));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = (loaderDialog is not null);
        }
    }
}
