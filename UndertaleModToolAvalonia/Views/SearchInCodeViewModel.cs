using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class SearchInCodeViewModel
{
    public MainViewModel MainVM { get; }

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
    int resultCount;
    int failedCount;

    public SearchInCodeViewModel(IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();
    }

    public void Search()
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

        globalDecompileContext = new(MainVM.Data);

        Parallel.ForEach(MainVM.Data.Code, SearchInUndertaleCode);

        // Sort results
        var sortedResultsByCodeDict = resultsByCodeDict.OrderBy(entry => MainVM.Data.Code.IndexOf(entry.Key));

        List<SearchResult> sortedResultsList = [];
        foreach (var result in sortedResultsByCodeDict)
        {
            UndertaleCode code = result.Key;
            foreach (var (lineNumber, lineText) in result.Value)
            {
                sortedResultsList.Add(new(code, lineNumber, lineText));
            }
        }

        Results = [.. sortedResultsList];

        // Set status bar text
        string str = $"{resultCount} result{(resultCount != 1 ? "s" : "")} found in {sortedResultsByCodeDict.Count()} code entr{(sortedResultsByCodeDict.Count() != 1 ? "ies" : "y")}.";
        if (failedCount > 0)
        {
            str += $" {failedCount} code entr{(failedCount != 1 ? "ies" : "y")} with an error.";
        }
        StatusBarText = str;

        // Reset variables
        resultsByCodeDict = new();
        resultCount = 0;
        failedCount = 0;
    }

    void SearchInUndertaleCode(UndertaleCode code)
    {
        if (code is not null && code.ParentEntry is null)
        {
            string codeText = String.Empty;

            if (!IsInAssembly)
            {
                try
                {
                    // TODO: Decompiler settings
                    codeText = new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext!, code).DecompileToString();
                }
                catch (Underanalyzer.Decompiler.DecompilerException)
                {
                    Interlocked.Increment(ref failedCount);
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
