//Adapted from original script by Grossley
//Repaired by VladiStep :)
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

int progress = 0;
int codesLeft = 0;
int result_count = 0;
StringBuilder results = new();
string searchNames = "";
Dictionary<string, List<string>> resultsDict = new();
List<string> failedList = new();
List<string> codeToDump = new();
List<string> gameObjectCandidates = new();

ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("You cannot do a code search on a YYC game! There is no code to search!");
    return;
}

bool case_sensitive = ScriptQuestion("Case sensitive?");
bool regex_check = ScriptQuestion("Regex search?");
string keyword = SimpleTextInput("Enter your search", "Search box below", "", false);
if (String.IsNullOrEmpty(keyword) || String.IsNullOrWhiteSpace(keyword))
{
    ScriptError("Search cannot be empty or null.");
    return;
}
searchNames = SimpleTextInput("Menu", "Enter object/code names", searchNames, true);
if (String.IsNullOrEmpty(searchNames) || String.IsNullOrWhiteSpace(searchNames))
{
    ScriptError("Names list cannot be empty or null.");
    return;
}
string[] searchNamesList = searchNames.Split('\n', StringSplitOptions.RemoveEmptyEntries);
for (int i = 0; i < searchNamesList.Length; i++)
{
    searchNamesList[i] = searchNamesList[i].Trim();
}

for (var j = 0; j < searchNamesList.Length; j++)
{
    foreach (UndertaleGameObject obj in Data.GameObjects)
    {
        if (searchNamesList[j].ToLower() == obj.Name.Content.ToLower())
        {
            gameObjectCandidates.Add(obj.Name.Content);
        }
    }
    foreach (UndertaleCode code in Data.Code)
    {
        if (searchNamesList[j].ToLower() == code.Name.Content.ToLower())
        {
            codeToDump.Add(code.Name.Content);
        }
    }
}

for (var j = 0; j < gameObjectCandidates.Count; j++)
{
    try
    {
        UndertaleGameObject obj = Data.GameObjects.ByName(gameObjectCandidates[j]);
        for (var i = 0; i < obj.Events.Count; i++)
        {
            foreach (UndertaleGameObject.Event evnt in obj.Events[i])
            {
                foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                {
                    if (action.CodeId?.Name?.Content != null)
                        codeToDump.Add(action.CodeId?.Name?.Content);
                }
            }
        }
    }
    catch
    {
        // Something went wrong, but probably because it's trying to check something non-existent
        // Just keep going
    }
}

codesLeft = codeToDump.Count;
UpdateProgress();

for (var j = 0; j < codeToDump.Count; j++)
{
    DumpCode(Data.Code.ByName(codeToDump[j]));
}
GetSortedResults();

HideProgressBar();
EnableUI();
string results_message = $"{result_count} results in {resultsDict.Count} code entries.";
SimpleTextOutput("Search results.", results_message, results.ToString(), true);

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, codesLeft);
}

void GetSortedResults() //not sure that it's necessary to sort it, but just in case
{
    int failedCount = failedList.Count;
    if (failedCount > 0)
    {
        if (failedCount == 1)
            results.Append("There is 1 code entry that encountered an error while searching:");
        else
            results.Append($"There is {failedCount} code entries that encountered an error while searching:");

        results.Append('\n' + string.Join(",\n", failedList.OrderBy(c => Data.Code.IndexOf(Data.Code.ByName(c)))));
        results.Append(".\n\n\n");
    }
    results.Append($"{result_count} results in {resultsDict.Count} code entries.\n\n");

    foreach (var result in resultsDict.OrderBy(c => Data.Code.IndexOf(Data.Code.ByName(c.Key))))
    {
        results.Append($"Results in {result.Key}:\n==========================\n");
        results.Append(string.Join('\n', result.Value) + "\n\n");
    }
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

bool RegexContains(string s, string sPattern, bool isCaseInsensitive)
{
    if (isCaseInsensitive)
        return System.Text.RegularExpressions.Regex.IsMatch(s, sPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return System.Text.RegularExpressions.Regex.IsMatch(s, sPattern);
}
void DumpCode(UndertaleCode code)
{
    try
    {
        var line_number = 1;
        string decompiled_text = (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : "");
        string[] splitted = decompiled_text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool name_written = false;
        foreach (string lineInt in splitted)
        {
            if (((regex_check && RegexContains(lineInt, keyword, case_sensitive)) || ((!regex_check && case_sensitive) ? lineInt.Contains(keyword) : lineInt.ToLower().Contains(keyword.ToLower()))))
            {
                if (name_written == false)
                {
                    resultsDict.Add(code.Name.Content, new List<string>());
                    name_written = true;
                }
                resultsDict[code.Name.Content].Add($"Line {line_number}: {lineInt}");
                result_count += 1;
            }
            line_number += 1;
        }
    }
    catch (Exception e)
    {
        failedList.Add(code.Name.Content);
    }

    UpdateProgress();
}
