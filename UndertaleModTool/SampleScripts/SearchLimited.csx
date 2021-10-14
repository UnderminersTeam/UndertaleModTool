//Adapted from original script by Grossley
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

int progress = 0;
string results = "";
int result_count = 0;
int code_count = 0;
int failed = 0;
List<String> codeToDump = new List<String>();
List<String> gameObjectCandidates = new List<String>();
List<String> splitStringsList = new List<String>();
string abc123 = "";
string removed = "";

ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

if (Data.IsYYC())
{
    ScriptError("You cannot do a code search on a YYC game! There is no code to search!");
    return;
}

UpdateProgress();

bool case_sensitive = ScriptQuestion("Case sensitive?");
bool regex_check = ScriptQuestion("Regex search?");
String keyword = SimpleTextInput("Enter your search", "Search box below", "", false);
if (String.IsNullOrEmpty(keyword) || String.IsNullOrWhiteSpace(keyword))
{
    ScriptError("Search cannot be empty or null.");
    return;
}
abc123 = SimpleTextInput("Menu", "Enter object/code names", abc123, true);
string[] subs = abc123.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
foreach (var sub in subs)
{
    splitStringsList.Add(sub.Trim());
}
for (var j = 0; j < splitStringsList.Count; j++)
{
    foreach (UndertaleGameObject obj in Data.GameObjects)
    {
        if (splitStringsList[j].ToLower() == obj.Name.Content.ToLower())
        {
            gameObjectCandidates.Add(obj.Name.Content);
        }
    }
    foreach (UndertaleCode code in Data.Code)
    {
        if (splitStringsList[j].ToLower() == code.Name.Content.ToLower())
        {
            gameObjectCandidates.Add(code.Name.Content);
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

int progress = 0;
int codesLeft = codeToDump.Count;
UpdateProgress();

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, codesLeft);
}

for (var j = 0; j < codeToDump.Count; j++)
{
    DumpCode(Data.Code.ByName(codeToDump[j]));
}
HideProgressBar();
EnableUI();
string results_message = result_count.ToString() + " results in " + code_count.ToString() + " code entries.";
SimpleTextInput("Search results.", results_message, results_message + "\n\n" + results, true, false);

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
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
        string[] splitted = decompiled_text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        bool name_written = false;
        foreach (string lineInt in splitted)
        {
            if (((regex_check && RegexContains(lineInt, keyword, case_sensitive)) || ((!regex_check && case_sensitive) ? lineInt.Contains(keyword) : lineInt.ToLower().Contains(keyword.ToLower()))))
            {
                if (name_written == false)
                {
                    results += "Results in " + code.Name.Content + ": \n==========================\n";
                    name_written = true;
                    code_count += 1;
                }
                results += "Line " + line_number.ToString() + ": " + lineInt + "\n";
                result_count += 1;
            }
            line_number += 1;
        }
        if (name_written == true)
            results += "\n";

    }
    catch (Exception e)
    {
    }
    UpdateProgress();
}
