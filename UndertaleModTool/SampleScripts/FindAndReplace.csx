using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}

int progress = 0;

UpdateProgress();
if (!ScriptQuestion("This will make changes across all of the code! Are you sure you'd like to continue?"))
{
    return;
}
bool case_sensitive = ScriptQuestion("Case sensitive?"); 
bool multiline = ScriptQuestion("Multi-line search?"); 
bool isRegex = ScriptQuestion("Is regex search?");
String keyword = SimpleTextInput("Enter search terms", "Search box below", "", multiline);
String replacement = SimpleTextInput("Enter replacement term", "Search box below", "", multiline);

foreach(UndertaleCode code in Data.Code)
{
    ReplaceTextInGML(code.Name.Content, keyword, replacement, case_sensitive, isRegex);
    UpdateProgress();
}

HideProgressBar();
ScriptMessage("Completed");

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

