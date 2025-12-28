using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class SearchInCodeViewModel
{
    // Set this when testing.
    public IView? View;

    public MainViewModel MainVM { get; }

    [Notify]
    private bool _IsEnabled = true;

    [Notify]
    private string _SearchText = "";
    [Notify]
    private bool _IsCaseSensitive = false;
    [Notify]
    private bool _IsRegexSearch = false;
    [Notify]
    private bool _IsInAssembly = false;
    [Notify]
    private ObservableCollection<SearchResult> _Results = [];
    [Notify]
    private string _StatusBarText = "Ready.";

    string searchText = null!;
    Regex searchTextRegex = null!;

    GlobalDecompileContext? globalDecompileContext;

    ConcurrentDictionary<UndertaleCode, List<(int, string)>> resultsByCodeDict = new();
    int resultCount = 0;
    int failedCount = 0;

    ILoaderWindow? loaderWindow;
    int currentCodeEntriesCount = 0;
    bool postToLoader = true;

    public SearchInCodeViewModel(IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();
    }

    public async void Search()
    {
        if (MainVM.Data is null)
        {
            StatusBarText = "Error: No data file loaded.";
            return;
        }

        if (MainVM.Data.IsYYC())
        {
            StatusBarText = "Error: Can't search code in YYC game, there's no code to search.";
            return;
        }

        searchText = SearchText.Replace("\r\n", "\n");

        if (String.IsNullOrEmpty(searchText))
        {
            StatusBarText = "Error: No text to search.";
            return;
        }

        if (IsRegexSearch)
        {
            try
            {
                searchTextRegex = new(searchText, IsCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            catch (ArgumentException e)
            {
                StatusBarText = $"Error: Invalid regex ({e.Message})";
                return;
            }
        }

        // Set up loader window
        loaderWindow = View!.LoaderOpen();
        loaderWindow.SetMaximum(MainVM.Data.Code.Count);
        loaderWindow.SetValue(0);
        loaderWindow.SetMessage("Searching...");
        loaderWindow.EnsureShown();

        IsEnabled = false;
        MainVM.IsEnabled = false;

        // Search codes in parallel
        globalDecompileContext = new(MainVM.Data);

        await Task.Run(() => Parallel.ForEach(MainVM.Data.Code, SearchInUndertaleCode));

        // Sort results
        loaderWindow.SetText("Sorting...");

        List<SearchResult> sortedResultsList = new(resultCount);
        
        await Task.Run(() =>
        {
            var sortedResultsByCodeDict = resultsByCodeDict.OrderBy(entry => MainVM.Data.Code.IndexOf(entry.Key));

            foreach (var result in sortedResultsByCodeDict)
            {
                UndertaleCode code = result.Key;
                foreach (var (lineNumber, lineText) in result.Value)
                {
                    sortedResultsList.Add(new(code, lineNumber, lineText));
                }
            }
        });

        Results = [.. sortedResultsList];

        // Set status bar text
        string str = $"{resultCount} result{(resultCount != 1 ? "s" : "")} found in {resultsByCodeDict.Count} code entr{(resultsByCodeDict.Count != 1 ? "ies" : "y")}.";
        if (failedCount > 0)
        {
            str += $" {failedCount} code entr{(failedCount != 1 ? "ies" : "y")} with an error.";
        }
        StatusBarText = str;

        // Reset variables
        resultsByCodeDict = new();
        resultCount = 0;
        failedCount = 0;
        currentCodeEntriesCount = 0;
        postToLoader = true;

        // Close loader window
        loaderWindow.Close();

        IsEnabled = true;
        MainVM.IsEnabled = true;
    }

    void SearchInUndertaleCode(UndertaleCode code)
    {
        if (postToLoader)
        {
            postToLoader = false;
            Dispatcher.UIThread.Post(() =>
            {
                loaderWindow!.SetValue(currentCodeEntriesCount);
                postToLoader = true;
            }, DispatcherPriority.Background);
        }

        if (code is not null && code.ParentEntry is null)
        {
            string codeText = String.Empty;

            if (!IsInAssembly)
            {
                try
                {
                    codeText = new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext!, code, MainVM.Data!.ToolInfo.DecompilerSettings).DecompileToString();
                }
                catch (Underanalyzer.Decompiler.DecompilerException)
                {
                    Interlocked.Increment(ref failedCount);
                    return;
                }
            }
            else
            {
                try
                {
                    codeText = code.Disassemble(MainVM.Data!.Variables, MainVM.Data!.CodeLocals?.For(code));
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failedCount);
                    return;
                }
            }

            List<int> results = [];

            if (IsRegexSearch)
            {
                MatchCollection matches = searchTextRegex.Matches(codeText);
                foreach (Match match in matches)
                {
                    results.Add(match.Index);
                }
            }
            else
            {
                StringComparison comparisonType = IsCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

                int index = 0;
                while ((index = codeText.IndexOf(searchText, index, comparisonType)) != -1)
                {
                    results.Add(index);
                    index += searchText.Length;
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

                string lineText = codeText[lineStartIndex..lineEndIndex];

                if (nameWritten == false)
                {
                    resultsByCodeDict[code] = [];
                    nameWritten = true;
                }
                resultsByCodeDict[code].Add((lineNumber + 1, lineText));

                Interlocked.Increment(ref resultCount);
            }
        }

        Interlocked.Increment(ref currentCodeEntriesCount);
    }

    public void OpenSearchResult(SearchResult searchResult, bool inNewTab = false)
    {
        var tab = MainVM.TabOpen(searchResult.Code, inNewTab);
        if (tab is not null && tab.Content is UndertaleCodeViewModel vm)
        {
            vm.LastGoToLocation = (!IsInAssembly ? UndertaleCodeViewModel.Tab.GML : UndertaleCodeViewModel.Tab.ASM, searchResult.LineNumber);
        }
    }

    public class SearchResult
    {
        public string Location { get; set; }
        public string Text { get; set; }

        public UndertaleCode Code;
        public int LineNumber;

        public SearchResult(UndertaleCode code, int lineNumber, string text)
        {
            Code = code;
            LineNumber = lineNumber;

            Location = code.Name.Content + ":" + lineNumber;
            Text = text.Trim();
        }
    }
}
