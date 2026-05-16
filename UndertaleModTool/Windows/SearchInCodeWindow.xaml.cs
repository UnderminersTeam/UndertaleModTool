#pragma warning disable CA1416 // Validate platform compatibility

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
using Underanalyzer.Decompiler;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for SearchInCodeWindow.xaml
    /// </summary>
    public partial class SearchInCodeWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private static bool isSearchInProgress = false;

        private bool isCaseSensitive, isRegexSearch, isMultilineRegex, isInAssembly;
        private string text;

        private int progressCount = 0;
        private int resultCount = 0;

        private ConcurrentDictionary<string, List<(int, string)>> resultsDict;
        private ConcurrentBag<string> failedList;
        private IEnumerable<KeyValuePair<string, List<(int, string)>>> resultsDictSorted;
        private IEnumerable<string> failedListSorted;

        private Regex keywordRegex, nameRegex;
        private GlobalDecompileContext decompileContext;
        private LoaderDialog loaderDialog;
        private UndertaleCodeEditor.CodeEditorTab editorTab;

        public readonly record struct Result(string Code, int LineNumber, string LineText);

        public ObservableCollection<Result> Results { get; set; } = new();

        public SearchInCodeWindow(string query = null, bool inAssembly = false)
        {
            InitializeComponent();

            InAssemblyCheckBox.IsChecked = inAssembly;

            if (query is null) return;
            if (query.Length > 256 || query.Count(x => x == '\n') > 16) {
                this.ShowError("Query is too long (Maximum of 256 chars / 16 lines)");
                return;
            }

            SearchTextBox.Text = query;
            SearchTextBox.SelectAll();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await Search();
        }

        private async Task Search()
        {
            // TODO: Allow this be cancelled, probably make loader inside this window itself.

            if (isSearchInProgress)
            {
                this.ShowError("Can't search while another search is in progress.");
                return;
            }

            if (mainWindow.Data is null)
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
            if (String.IsNullOrEmpty(text)) {
                return;
            }
            text = text.Replace("\r\n", "\n");

            isCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? false;
            isRegexSearch = RegexSearchCheckBox.IsChecked ?? false;
            isMultilineRegex = MultilineRegexCheckBox.IsChecked ?? false;
            isInAssembly = InAssemblyCheckBox.IsChecked ?? false;

            bool filterByName = FilterByNameExpander.IsExpanded;
            bool nameIsCaseSensitive, nameIsRegex;

            IList<UndertaleCode> codeEntriesToSearch = mainWindow.Data.Code;

            if (isRegexSearch)
            {
                RegexOptions options = RegexOptions.Compiled;
                if (!isCaseSensitive)
                {
                    options |= RegexOptions.IgnoreCase;
                }
                if (isMultilineRegex)
                {
                    options |= RegexOptions.Multiline;
                }
                try
                {
                    keywordRegex = new(text, options);
                }
                catch (ArgumentException e)
                {
                    this.ShowError($"Invalid Regex: {e.Message}");
                    return;
                }
            }

            if (filterByName && NameFilterTextBox.Text is string name && name != "")
            {
                nameIsCaseSensitive = NameCaseSensitiveCheckBox.IsChecked ?? false;
                nameIsRegex = NameRegexSearchCheckBox.IsChecked ?? false;

                if (nameIsRegex)
                {
                    RegexOptions options = new();
                    if (!nameIsCaseSensitive) {
                        options |= RegexOptions.IgnoreCase;
                    }

                    try
                    {
                        nameRegex = new(name, options);
                    }
                    catch (ArgumentException e)
                    {
                        this.ShowError($"Invalid name Regex: {e.Message}");
                    }

                    codeEntriesToSearch = mainWindow.Data.Code
                        .Where(code => !String.IsNullOrEmpty(code?.Name?.Content))
                        .Where(code => nameRegex.IsMatch(code.Name.Content))
                        .ToList();
                }
                else
                {
                    var cmp = nameIsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    codeEntriesToSearch = mainWindow.Data.Code
                        .Where(code => !String.IsNullOrEmpty(code?.Name?.Content))
                        .Where(code => code.Name.Content.Contains(name, cmp))
                        .ToList();
                }
            }

            if (codeEntriesToSearch.Count == 0)
            {
                this.ShowMessage("There are no code entries that match the name filter.");
                return;
            }

            mainWindow.IsEnabled = false;
            IsEnabled = false;

            isSearchInProgress = true;

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
                decompileContext = new GlobalDecompileContext(mainWindow.Data);
            }

            loaderDialog.SavedStatusText = "Code entries";
            loaderDialog.Update(null, "Code entries", 0, codeEntriesToSearch.Count);

            await Task.Run(() => Parallel.ForEach(codeEntriesToSearch, SearchInUndertaleCode));
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

            isSearchInProgress = false;
        }

        string GetCodeString(UndertaleCode code)
        {
            // First, try to retrieve source from project (if available)
            if (mainWindow.Project is null || !mainWindow.Project.TryGetCodeSource(code, out string decompiled))
            {
                // Source isn't available - perform decompile
                decompiled = new Underanalyzer.Decompiler.DecompileContext(decompileContext, code, mainWindow.Data.ToolInfo.DecompilerSettings).DecompileToString();
            }
            return decompiled;
        }

        private void SearchInUndertaleCode(UndertaleCode code)
        {
            if (code is null) return;

            // Child code entries do not contain any instructions.
            if (code.ParentEntry is not null) return;

            UndertaleData data = mainWindow.Data;

            // TODO: Look at specific exceptions

            if (isInAssembly) {
                try
                {
                    var codeText = isInAssembly
                        ? code.Disassemble(mainWindow.Data.Variables, mainWindow.Data.CodeLocals?.For(code), mainWindow.Data.CodeLocals is null)
                        : GetCodeString(code);
                    SearchInCodeText(code.Name.Content, codeText);
                }
                catch (Exception)
                {
                    failedList.Add(code.Name.Content);
                }
            }
            else
            {
                try
                {

                    var codeText = TryGetProfileModeGML(code.Name.Content);
                    if (codeText is null) {
                        DecompileContext ctx = new(decompileContext, code, data.ToolInfo.DecompilerSettings);
                        codeText = ctx.DecompileToString();
                    }
                    SearchInCodeText(code.Name.Content, codeText);
                }
                catch (Exception)
                {
                    failedList.Add(code.Name.Content);
                }
            }

            Interlocked.Increment(ref progressCount);
            Dispatcher.Invoke(() => loaderDialog.ReportProgress(progressCount));
        }

        private void SearchInCodeText(string codeName, string codeText)
        {
            List<int> results = new();

            if (isRegexSearch)
            {
                MatchCollection matches = keywordRegex.Matches(codeText);
                foreach (Match match in matches)
                {
                    results.Add(match.Index);
                }
            }
            else
            {
                StringComparison cmp = isCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

                int index = 0;
                while ((index = codeText.IndexOf(text, index, cmp)) != -1)
                {
                    results.Add(index);
                    index += text.Length;
                }
            }

            bool nameWritten = false;

            int lineNumber = 0;
            int lineStartIndex = 0;

            foreach (int index in results)
            {
                // Continue from previous line count since results are in order
                for (int i = lineStartIndex; i < index; ++i)
                {
                    if (codeText[i] == '\n')
                    {
                        lineNumber++;
                        lineStartIndex = i + 1;
                    }
                }

                // Start at match.Index so it's only one line in case the search was multiline
                int lineEndIndex = codeText.IndexOf('\n', index);
                lineEndIndex = lineEndIndex == -1 ? codeText.Length : lineEndIndex;

                string lineText;

                // Limit the displayed line length to 128
                if (lineEndIndex - lineStartIndex > 128)
                {
                    lineEndIndex = lineStartIndex + 128;
                    lineText = codeText[lineStartIndex..lineEndIndex] + "...";
                }
                else
                {
                    lineText = codeText[lineStartIndex..lineEndIndex];
                }

                if (nameWritten == false)
                {
                    resultsDict[codeName] = new List<(int, string)>();
                    nameWritten = true;
                }
                resultsDict[codeName].Add((lineNumber + 1, lineText));

                Interlocked.Increment(ref resultCount);
            }
        }

        private void SortResults()
        {
            string[] codeNames = mainWindow.Data.Code.Select(c => c.Name.Content).ToArray();

            resultsDictSorted = resultsDict.OrderBy(c => Array.IndexOf(codeNames, c.Key));
            failedListSorted = failedList.OrderBy(c => Array.IndexOf(codeNames, c));
        }

        public void ShowResults()
        {
            var resultsSorted = resultsDictSorted.ToArray();
            var failedSorted = failedListSorted.ToArray();
            foreach (var result in resultsSorted)
            {
                var code = result.Key;
                foreach (var (lineText, lineNumber) in result.Value)
                {
                    Results.Add(new(code, lineText, lineNumber));
                }
            }

            int codeEntryCount = resultsSorted.Length;
            int failedCount = failedSorted.Length;

            string wResults = resultCount == 1 ? "result" : "results";
            string wEntries = codeEntryCount == 1 ? "entry" : "entries";
            string wEntriesFailed = failedCount == 1 ? "entry" : "entries";

            string str = $"{resultCount} {wResults} found in {codeEntryCount} code {wEntries}.";
            if (failedCount > 0)
            {
                str += $" {failedCount} code {wEntriesFailed} encountered errors while searching.";
            }
            StatusBarTextBlock.Text = str;
        }

        private void OpenSelectedListViewItem(bool inNewTab = false, Result resultToOpen = default)
        {
            if (isSearchInProgress)
            {
                this.ShowError("Can't open results while a search is in progress.");
                return;
            }

            if (resultToOpen != default)
            {
                mainWindow.OpenCodeEntry(resultToOpen.Code, resultToOpen.LineNumber, editorTab, inNewTab);
            }
            else
            {
                foreach (Result result in ResultsListView.SelectedItems)
                {
                    mainWindow.OpenCodeEntry(result.Code, result.LineNumber, editorTab, inNewTab);
                    // Only first one opens in current tab, the rest go into new tabs.
                    inNewTab = true;
                }
            }

            // So it activates the window after it finished processing
            // (otherwise it doesn't work sometimes)
            _ = Task.Run(() =>
            {
                Dispatcher.Invoke(mainWindow.Activate);
            });
        }

        private void CopyListViewItems(IEnumerable items)
        {
            string str = String.Join('\n', items
                .Cast<Result>()
                .Select(result => $"{result.Code}\t{result.LineNumber}\t{result.LineText}"));
            try
            {
                Clipboard.SetText(str);
            }
            catch (Exception ex)
            {
                this.ShowError("Can't copy the item name to clipboard due to this error:\n" +
                               ex.Message + ".\nYou probably should try again.");
            }
        }

        private async void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await Search();
            }
        }

        private void ListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed
                && e.ChangedButton == MouseButton.Middle)
            {
                if (e.Source is not FrameworkElement elem
                    || elem.DataContext is not Result res)
                    return;

                OpenSelectedListViewItem(true, res);  
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedListViewItem();
        }

        private void ListViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OpenSelectedListViewItem();
                e.Handled = true;
            }
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

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = (loaderDialog is not null);
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
