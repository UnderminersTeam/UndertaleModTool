using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("You cannot do a code search on a YYC game! There is no code to search!");
    return;
}

int progress = 0;
StringBuilder results = new();
Dictionary<string, List<string>> resultsDict = new();
int result_count = 0;
int code_count = 0;

UpdateProgress();

bool case_sensitive = ScriptQuestion("Case sensitive?");
bool regex_check = ScriptQuestion("Regex search?");
String keyword = SimpleTextInput("Enter your search", "Search box below", "", false);
if (String.IsNullOrEmpty(keyword) || String.IsNullOrWhiteSpace(keyword))
{
    ScriptError("Search cannot be empty or null.");
    return;
}

await DumpCode();
GetSortedResults();
HideProgressBar();
//GC.Collect();
EnableUI();
string results_message = $"{result_count} results in {code_count} code entries.";
SimpleTextOutput("Search results.", results_message, results_message + "\n\n" + results, true);

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


async Task DumpCode()
{
    await Task.Run(() => Parallel.ForEach(Data.Code, DumpCode));
}

void GetSortedResults()
{
    foreach (var result in resultsDict.OrderBy(c => Data.Code.IndexOf(Data.Code.ByName(c.Key))))
    {
        results.Append($"Results in {result.Key}:\n==========================\n");
        results.Append(string.Join('\n', result.Value) + "\n\n");
    }
}

bool RegexContains(string s, string sPattern, bool isCaseInsensitive)
{
    if (isCaseInsensitive)
        return System.Text.RegularExpressions.Regex.IsMatch(s, sPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return System.Text.RegularExpressions.Regex.IsMatch(s, sPattern);
}
void DumpCode(UndertaleCode code)
{
    string DISASMTEXT = "";
    try
    {
        DISASMTEXT = (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : "");
    }
    catch (Exception e)
    {
        DISASMTEXT = "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"; // Please don't
    }
    try
    {
        var line_number = 1;
        string decompiled_text = DISASMTEXT;
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
                    code_count += 1;
                }
                resultsDict[code.Name.Content].Add($"Line {line_number}: {lineInt}");
                result_count += 1;
            }
            line_number += 1;
        }
    }
    catch (Exception e)
    {
    }

    UpdateProgress();
}
