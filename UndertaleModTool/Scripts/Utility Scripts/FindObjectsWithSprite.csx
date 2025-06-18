// Written by VladiStep (VladStepu2001#3453 on Discord)

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
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

EnsureDataLoaded();

string[] spriteNames;
ConcurrentBag<string> resultList = new();

bool caseSensitive = ScriptQuestion("Case sensitive?");
bool regexCheck = ScriptQuestion("Regex search?");
string qText = regexCheck ? "Enter RegEx of sprite name(s)" : "Enter sprite name(s)";
string searchQuery = SimpleTextInput(qText, "Search box below", "", !regexCheck);

if (String.IsNullOrEmpty(searchQuery) || String.IsNullOrWhiteSpace(searchQuery))
{
    ScriptError("Search query cannot be empty or null.");
    return;
}

SetProgressBar(null, "Game objects", 0, Data.GameObjects.Count);
StartProgressBarUpdater();

Regex searchRegex;
if (regexCheck)
{
    if (caseSensitive)
        searchRegex = new(searchQuery, RegexOptions.Compiled);
    else
        searchRegex = new(searchQuery, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    await Task.Run(() => Parallel.ForEach(Data.GameObjects, CheckObjectRegex));
}
else
{
    spriteNames = searchQuery.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    for (int i = 0; i < spriteNames.Length; i++)
        spriteNames[i] = spriteNames[i].Trim();

    await Task.Run(() => Parallel.ForEach(Data.GameObjects, CheckObject));
}

string label = "Objects with ";
if (spriteNames?.Length > 1)
    label += "any of entered sprite";
else
    label += "a sprite" + (regexCheck ? $" whose name matches \"{searchQuery}\" (RegEx)" : $" named \"{searchQuery}\"");

string[] objectNames = Data.GameObjects.Select(x => x.Name?.Content).ToArray(); // for "OrderBy()" acceleration

await StopProgressBarUpdater();
SimpleTextOutput("Search results.", label, string.Join('\n', resultList.Distinct().OrderBy(x => Array.IndexOf(objectNames, x))), true);

EnableUI();


void CheckObject(UndertaleGameObject obj)
{
    if (obj is not null)
    {
        string sprName = obj.Sprite?.Name?.Content;

        if (sprName is not null && spriteNames.Contains(sprName))
            resultList.Add(obj.Name.Content);
    }

    IncrementProgressParallel();
}
void CheckObjectRegex(UndertaleGameObject obj)
{
    if (obj is not null)
    {
        string sprName = obj.Sprite?.Name?.Content;

        if (sprName is not null && searchRegex.Match(sprName).Success)
            resultList.Add(obj.Name.Content);
    }

    IncrementProgressParallel();
}