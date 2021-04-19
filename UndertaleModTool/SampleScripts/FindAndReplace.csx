using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

EnsureDataLoaded();

int progress = 0;
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

UpdateProgress();
if (!ScriptQuestion("This will make changes across all of the code! Are you sure you'd like to continue?"))
{
    return;
}
bool case_sensitive = ScriptQuestion("Case sensitive?"); 
bool multiline = ScriptQuestion("Multi-line search?"); 
String keyword = SimpleTextInput("Enter search terms", "Search box below", "", multiline);
String replacement = SimpleTextInput("Enter replacement term", "Search box below", "", multiline);

foreach(UndertaleCode code in Data.Code)
{
    ReplaceTextInGML(code, keyword, replacement, case_sensitive);
}

HideProgressBar();
ScriptMessage("Completed");

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

void ReplaceTextInGML(UndertaleCode code, string keyword, string replacement, bool case_sensitive = false)
{
    keyword = keyword.Replace("\r\n", "\n");
    replacement = replacement.Replace("\r\n", "\n");
    try
    {
        string decompiled_text = (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : "");
        string PassBack = "";
        if (case_sensitive)
            PassBack = decompiled_text.Replace(keyword, replacement);
        else
            PassBack = Regex.Replace(decompiled_text, Regex.Escape(keyword), replacement, RegexOptions.IgnoreCase);
        try
        {
            code.ReplaceGML(PassBack, Data);
        }
        catch (Exception ex)
        {
            string errorMSG = "Error in " + code.Name.Content + ":\r\n" + ex.ToString() + "\r\nAborted" + "\r\nAttempting the following code: \r\n\r\n" + PassBack;
            SetUMTConsoleText(errorMSG);
            SetFinishedMessage(false);
            return;
        }
    }
    catch (Exception e)
    {
        string errorMSG = "An unknown error occurred while attempting to do find and replace. Aborted!\r\n" + e.ToString();
        SetUMTConsoleText(errorMSG);
        SetFinishedMessage(false);
        return;
    }
    UpdateProgress();
}
