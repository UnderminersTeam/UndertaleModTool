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
ConcurrentDictionary<string, List<string>> resultsDict = new();
ConcurrentBag<string> failedList = new();
IOrderedEnumerable<string> failedSorted;                              //failedList.OrderBy()
IOrderedEnumerable<KeyValuePair<string, List<string>>> resultsSorted; //resultsDict.OrderBy()
int result_count = 0;
bool case_sensitive = ScriptQuestion("Case sensitive?");
bool regex_check = ScriptQuestion("Regex search?");
string keyword = SimpleTextInput("Enter your search", "Search box below", "", false);
if (String.IsNullOrEmpty(keyword) || String.IsNullOrWhiteSpace(keyword))
{
    ScriptError("Search cannot be empty or null.");
    return;
}

Regex keywordRegex;
if (regex_check)
{
    if (case_sensitive)
        keywordRegex = new(keyword, RegexOptions.Compiled);
    else
        keywordRegex = new(keyword, RegexOptions.Compiled | RegexOptions.IgnoreCase);
}

SetProgressBar(null, "Code Entries", 0, Data.Code.Count);
StartProgressBarUpdater();

await DumpCode();

await StopProgressBarUpdater();

await Task.Run(SortResults);

UpdateProgressStatus("Generating result list...");
await ClickableTextOutput("Search results.", keyword, result_count, resultsSorted, false, failedSorted);

HideProgressBar();
EnableUI();


string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DumpCode()
{
    await Task.Run(() => Parallel.ForEach(Data.Code, DumpCode));
}

void SortResults()
{
    string[] codeNames = Data.Code.Select(x => x.Name.Content).ToArray();

    if (failedList.Count > 0)
        failedSorted = failedList.OrderBy(c => Array.IndexOf(codeNames, c));

    resultsSorted = resultsDict.OrderBy(c => Array.IndexOf(codeNames, c.Key));
}

bool RegexContains(in string s)
{
    return keywordRegex.Match(s).Success;
}
void DumpCode(UndertaleCode code)
{
    if (code.ParentEntry is null)
    {
        try
        {
            var line_number = 1;
            StringReader assemblyText = new(code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : "");
            bool name_written = false;
            string lineInt;
            while ((lineInt = assemblyText.ReadLine()) is not null)
            {
                if (lineInt == string.Empty)
                    continue;

                if (((regex_check && RegexContains(in lineInt)) || ((!regex_check && case_sensitive) ? lineInt.Contains(keyword) : lineInt.ToLower().Contains(keyword.ToLower()))))
                {
                    if (name_written == false)
                    {
                        resultsDict[code.Name.Content] = new List<string>();
                        name_written = true;
                    }
                    resultsDict[code.Name.Content].Add($"Line {line_number}: {lineInt}");
                    Interlocked.Increment(ref result_count);
                }
                line_number += 1;
            }
        }
        catch (Exception e)
        {
            failedList.Add(code.Name.Content);
        }
    }

    IncrementProgressParallel();
}
