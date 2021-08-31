using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

if (Data.IsYYC())
{
    ScriptError("You cannot do a code search on a YYC game! There is no code to search!");
    return;
}

int progress = 0;
string results = "";
int result_count = 0;
int code_count = 0;
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

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
HideProgressBar();
//GC.Collect();
string results_message = result_count.ToString() + " results in " + code_count.ToString() + " code entries.";
SimpleTextInput("Search results.", results_message, results_message + "\n\n" + results, true);

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