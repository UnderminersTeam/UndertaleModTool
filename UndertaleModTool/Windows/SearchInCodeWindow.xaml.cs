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

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for SearchInCodeWindow.xaml
    /// </summary>
    public partial class SearchInCodeWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private static bool isSearchInProgress = false;

        private bool isCaseSensitive, isRegexSearch, isInAssembly;
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

            if (query is not null)
            {
                if (query.Length > 256 || query.Count(x => x == '\n') > 16)
                    return; // Ignore if the query is longer than 256 characters or 16 lines.

                SearchTextBox.Text = query;
                SearchTextBox.SelectAll();
            }

            InAssemblyCheckBox.IsChecked = inAssembly;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await Search();
        }

        private async Task Search()
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

            text = SearchTextBox.Text.Replace("\r\n", "\n");

            if (String.IsNullOrEmpty(text))
                return;

            if (isSearchInProgress)
            {
                this.ShowError("Can't search while another search is in progress.");
                return;
            }

            isCaseSensitive = CaseSensitiveCheckBox.IsChecked ?? false;
            isRegexSearch = RegexSearchCheckBox.IsChecked ?? false;
            isInAssembly = InAssemblyCheckBox.IsChecked ?? false;

            bool filterByName = FilterByNameExpander.IsExpanded;
            bool nameIsCaseSensitive, nameIsRegex;
            string name;

            IList<UndertaleCode> codeEntriesToSearch = mainWindow.Data.Code;

            if (isRegexSearch)
            {
                try
                {
                    keywordRegex = new(text, isCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
                catch (ArgumentException e)
                {
                    this.ShowError($"Invalid Regex: {e.Message}");
                    return;
                }
            }

            if (filterByName)
            {
                name = NameFilterTextBox.Text;
                if (!String.IsNullOrEmpty(name))
                {
                    nameIsCaseSensitive = NameCaseSensitiveCheckBox.IsChecked ?? false;
                    nameIsRegex = NameRegexSearchCheckBox.IsChecked ?? false;

                    if (nameIsRegex)
                    {
                        try
                        {
                            nameRegex = new(name, nameIsCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            codeEntriesToSearch = mainWindow.Data.Code.Where(c => !String.IsNullOrEmpty(c.Name.Content)
                                                                                  && nameRegex.IsMatch(c.Name.Content))
                                                                      .ToList();
                        }
                        catch (ArgumentException e)
                        {
                            this.ShowError($"Invalid name Regex: {e.Message}");
                            filterByName = false;
                        }
                    }
                    else
                    {
                        var comparison = nameIsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                        codeEntriesToSearch = mainWindow.Data.Code.Where(c => !String.IsNullOrEmpty(c.Name.Content)
                                                                              && c.Name.Content.Contains(name, comparison))
                                                                  .ToList();
                    }
                }
            }

            if (codeEntriesToSearch.Count == 0)
            {
                this.ShowMessage("There are no code entries that match the name filter.");
                return;
            }

            mainWindow.IsEnabled = false;
            this.IsEnabled = false;

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

        private void SearchInUndertaleCode(UndertaleCode code)
        {
            try
            {
                if (code is not null && code.ParentEntry is null)
                {
                    var codeText = isInAssembly
                        ? code.Disassemble(mainWindow.Data.Variables, mainWindow.Data.CodeLocals?.For(code), mainWindow.Data.CodeLocals is null)
                        : TryGetProfileModeGML(code.Name.Content)
                            ?? new Underanalyzer.Decompiler.DecompileContext(decompileContext, code, mainWindow.Data.ToolInfo.DecompilerSettings).DecompileToString();
                    SearchInCodeText(code.Name.Content, codeText);
                }
                
            }
            // TODO: Look at specific exceptions
            catch (Exception)
            {
                failedList.Add(code.Name.Content);
            }

            Interlocked.Increment(ref progressCount);
            Dispatcher.Invoke(() => loaderDialog.ReportProgress(progressCount));
        }

        static string TryGetProfileModeGML(string codeName)
        {
            if (SettingsWindow.ProfileModeEnabled && mainWindow.ProfileHash is not null)
            {
                string path = Path.Join(Settings.ProfilesFolder, mainWindow.ProfileHash, "Temp", codeName + ".gml");
                if (File.Exists(path))
                    return File.ReadAllText(path).Replace("\r\n", "\n");
            }
            return null;
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
                StringComparison comparisonType = isCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

                int index = 0;
                while ((index = codeText.IndexOf(text, index, comparisonType)) != -1)
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
            string[] codeNames = mainWindow.Data.Code.Select(x => x.Name.Content).ToArray();

            resultsDictSorted = resultsDict.OrderBy(c => Array.IndexOf(codeNames, c.Key));
            failedListSorted = failedList.OrderBy(c => Array.IndexOf(codeNames, c));
        }

        public void ShowResults()
        {
            static string GetWordEnding(int quantity, bool isResults)
            {
                if (isResults)
                    return quantity != 1 ? "s" : "";
                
                return quantity != 1 ? "ies" : "y";
            }

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

            string str = $"{resultCount} result{GetWordEnding(resultCount, true)} found in {resultsSorted.Length} code entr{GetWordEnding(resultsSorted.Length, false)}.";
            if (failedSorted.Length > 0)
            {
                str += $" {failedSorted.Length} code entr{GetWordEnding(failedSorted.Length, false)} with an error.";
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
