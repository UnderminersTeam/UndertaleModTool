using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

EnsureDataLoaded();

int progress = 0;
string results = "";
int result_count = 0;
int code_count = 0;
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

UpdateProgress();
if (!ScriptQuestion("This will make changes across all of the code! Are you sure you'd like to continue?"))
{
    return;
}
bool case_sensitive = ScriptQuestion("Case sensitive?"); 
String keyword = SimpleTextInput("Enter search terms", "Search box below", "", false);
String replacement = SimpleTextInput("Enter replacement term", "Search box below", "", false);

foreach(UndertaleCode code in Data.Code)
{
    DumpCode(code);
}

HideProgressBar();
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

void DumpCode(UndertaleCode code)
{
    try 
    {
        var line_number = 1;
        string decompiled_text = (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : "");
        string[] splitted = decompiled_text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        bool name_written = false;
        string PassBack = "";
        foreach (string lineInt in splitted)
        {
            if (case_sensitive && lineInt.Contains(keyword))
            {
                if (name_written == false)
                {
                    results += "Results in " + code.Name.Content + ": \n==========================\n";
                    name_written = true;
                    code_count += 1;
                }
                results += "Line " + line_number.ToString() + ": " + lineInt.Replace(keyword, replacement) + "\n";
                PassBack += (lineInt.Replace(keyword, replacement) + "\n");
                result_count += 1;
            }
            else if ((!(case_sensitive)) && lineInt.ToLower().Contains(keyword.ToLower()))
            {
                string lineInt2 = Regex.Replace(lineInt, keyword, replacement, RegexOptions.IgnoreCase);
                if (name_written == false)
                {
                    results += "Results in " + code.Name.Content + ": \n==========================\n";
                    name_written = true;
                    code_count += 1;
                }
                results += "Line " + line_number.ToString() + ": " + lineInt2 + "\n";
                result_count += 1;
                PassBack += (lineInt2 + "\n");
            }
            else
                PassBack += (lineInt + "\n");
            line_number += 1;
        }
        try
        {
            code.ReplaceGML(PassBack, Data);
        }
        catch (Exception ex)
        {
            string errorMSG = "Error in " +  code.Name.Content + ":\r\n" + ex.ToString() + "\r\nAborted";
            ScriptMessage(errorMSG);
            SetUMTConsoleText(errorMSG);
            SetFinishedMessage(false);
            return;
        }
        if (name_written == true)
            results += "\n";
        
    } 
    catch (Exception e) 
    {
    }

    UpdateProgress();
}