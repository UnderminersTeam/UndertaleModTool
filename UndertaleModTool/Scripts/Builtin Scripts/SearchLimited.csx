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
using System.Text.RegularExpressions;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("You cannot do a code search on a YYC game! There is no code to search!");
    return;
}

int resultCount = 0;
StringBuilder results = new();
string searchNames = "";
Dictionary<string, List<(int, string)>> resultsDict = new();
List<string> failedList = new();
List<string> codeToDump = new();
List<string> gameObjectCandidates = new();

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
searchNames = SimpleTextInput("Menu", "Enter object/code names", searchNames, true);
if (String.IsNullOrEmpty(searchNames) || String.IsNullOrWhiteSpace(searchNames))
{
    ScriptError("Names list cannot be empty or null.");
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

string[] searchNamesList = searchNames.Split('\n', StringSplitOptions.RemoveEmptyEntries);
for (int i = 0; i < searchNamesList.Length; i++)
{
    searchNamesList[i] = searchNamesList[i].Trim();
}

for (var j = 0; j < searchNamesList.Length; j++)
{
    foreach (UndertaleGameObject obj in Data.GameObjects)
    {
        if (obj is null)
            continue;
        if (searchNamesList[j].ToLower() == obj.Name.Content.ToLower())
        {
            gameObjectCandidates.Add(obj.Name.Content);
        }
    }
    foreach (UndertaleCode code in Data.Code)
    {
        if (code is null)
            continue;
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

SetProgressBar(null, "Code Entries", 0, codeToDump.Count);
StartProgressBarUpdater();

await Task.Run(() =>
{
    for (var j = 0; j < codeToDump.Count; j++)
    {
        DumpCode(Data.Code.ByName(codeToDump[j]));
    }
});

await StopProgressBarUpdater();

UpdateProgressStatus("Generating result list...");
await ClickableSearchOutput("Search results.", keyword, resultCount, resultsDict, true, failedList);

HideProgressBar();
EnableUI();

bool RegexContains(in string s)
{
    return keywordRegex.Match(s).Success;
}
void DumpCode(UndertaleCode code)
{
    try
    {
        if (code.ParentEntry is null)
        {
            int lineNumber = 1;
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
                        resultsDict.Add(code.Name.Content, new List<(int, string)>());
                        nameWritten = true;
                    }
                    resultsDict[code.Name.Content].Add((lineNumber, lineInt));
                    resultCount += 1;
                }
                lineNumber += 1;
            }
        }
    }
    catch (Exception e)
    {
        failedList.Add(code.Name.Content);
    }

    IncrementProgress();
}