using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("You cannot do a code search on a YYC game! There is no code to search!");
    return;
}

StringBuilder results = new();
ConcurrentDictionary<string, List<(int, string)>> resultsDict = new();
ConcurrentBag<string> failedList = new();
IOrderedEnumerable<string> failedSorted;                                     //failedList.OrderBy()
IOrderedEnumerable<KeyValuePair<string, List<(int, string)>>> resultsSorted; //resultsDict.OrderBy()
int resultCount = 0;

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

bool caseSensitive = ScriptQuestion("Case sensitive?");
bool regexCheck = ScriptQuestion("Regex search?");
string keyword = SimpleTextInput("Enter your search", "Search box below", "", false);
if (String.IsNullOrEmpty(keyword) || String.IsNullOrWhiteSpace(keyword))
{
    ScriptError("Search cannot be empty or null.");
    return;
}

Regex keywordRegex;
if (regexCheck)
{
    if (caseSensitive)
        keywordRegex = new(keyword, RegexOptions.Compiled);
    else
        keywordRegex = new(keyword, RegexOptions.Compiled | RegexOptions.IgnoreCase);
}

bool cacheGenerated = await GenerateGMLCache(globalDecompileContext);
await StopProgressBarUpdater();

SetProgressBar(null, "Code Entries", 0, Data.Code.Count);
StartProgressBarUpdater();

await DumpCode();

await StopProgressBarUpdater();

await Task.Run(SortResults);

UpdateProgressStatus("Generating result list...");
await ClickableSearchOutput("Search results.", keyword, resultCount, resultsSorted, true, failedSorted);

HideProgressBar();
EnableUI();

async Task DumpCode()
{
    if (cacheGenerated)
    {
        await Task.Run(() => Parallel.ForEach(Data.GMLCache, ScanCode));
    }
    else
    {
        if (Data.GlobalFunctions is null) //if we run script before opening any code
        {
            SetProgressBar(null, "Building the cache of all global functions...", 0, 0);
            await Task.Run(() => GlobalDecompileContext.BuildGlobalFunctionCache(Data));
            SetProgressBar(null, "Code Entries", 0, Data.Code.Count);
        }

        await Task.Run(() => Parallel.ForEach(Data.Code, DumpCode));
    }
}

void SortResults()
{
    string[] codeNames = Data.Code.Select(x => x.Name.Content).ToArray();

    if (Data.GMLCacheFailed?.Count > 0)
        failedSorted = failedList.Concat(Data.GMLCacheFailed).OrderBy(c => Array.IndexOf(codeNames, c));
    else if (failedList.Count > 0)
        failedSorted = failedList.OrderBy(c => Array.IndexOf(codeNames, c));

    resultsSorted = resultsDict.OrderBy(c => Array.IndexOf(codeNames, c.Key));
}

bool RegexContains(in string s)
{
    return keywordRegex.Match(s).Success;
}
void DumpCode(UndertaleCode code)
{
    try
    {
        if (code is not null && code.ParentEntry is null)
        {
            var lineNumber = 1;
            StringReader decompiledText = new(code != null 
                ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString() 
                : "");
            bool nameWritten = false;
            string lineInt;
            while ((lineInt = decompiledText.ReadLine()) is not null)
            {
                if (lineInt == string.Empty)
                {
                    lineNumber += 1;
                    continue;
                }

                if (((regexCheck && RegexContains(in lineInt)) || ((!regexCheck && caseSensitive) ? lineInt.Contains(keyword) : lineInt.ToLower().Contains(keyword.ToLower()))))
                {
                    if (nameWritten == false)
                    {
                        resultsDict[code.Name.Content] = new List<(int, string)>();
                        nameWritten = true;
                    }
                    resultsDict[code.Name.Content].Add((lineNumber, lineInt));
                    Interlocked.Increment(ref resultCount);
                }
                lineNumber += 1;
            }
        }
    }
    catch (Exception e)
    {
        failedList.Add(code.Name.Content);
    }

    IncrementProgressParallel();
}
void ScanCode(KeyValuePair<string, string> code)
{
    try
    {
        var lineNumber = 1;
        StringReader decompiledText = new(code.Value);
        bool nameWritten = false;
        string lineInt;
        while ((lineInt = decompiledText.ReadLine()) is not null)
        {
            if (lineInt == string.Empty)
            {
                lineNumber += 1;
                continue;
            }

            if (((regexCheck && RegexContains(in lineInt)) || ((!regexCheck && caseSensitive) ? lineInt.Contains(keyword) : lineInt.ToLower().Contains(keyword.ToLower()))))
            {
                if (nameWritten == false)
                {
                    resultsDict[code.Key] = new List<(int, string)>();
                    nameWritten = true;
                }
                resultsDict[code.Key].Add((lineNumber, lineInt));
                Interlocked.Increment(ref resultCount);
            }
            lineNumber += 1;
        }
    }
    catch (Exception e)
    {
        failedList.Add(code.Key);
    }

    IncrementProgressParallel();
}
