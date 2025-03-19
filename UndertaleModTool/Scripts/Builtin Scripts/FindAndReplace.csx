using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

EnsureDataLoaded();

if (!ScriptQuestion("This will make changes across all of the code! Are you sure you'd like to continue?"))
{
    return;
}
bool caseSensitive = ScriptQuestion("Case sensitive?");
bool multiline = ScriptQuestion("Multi-line search?");
bool isRegex = ScriptQuestion("Is regex search?");
String keyword = SimpleTextInput("Enter search terms", "Search box below", "", multiline);
String replacement = SimpleTextInput("Enter replacement term", "Search box below", "", multiline);

List<UndertaleCode> toDump = Data.Code.Where(c => c.ParentEntry is null).ToList();

SetProgressBar(null, "Code Entries", 0, toDump.Count);
StartProgressBarUpdater();

SyncBinding("Strings, Variables, Functions", true);
await Task.Run(() =>
{
    UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data, null, Data.ToolInfo.DecompilerSettings);
    foreach (UndertaleCode code in toDump)
    {
        if (isRegex)
        {
            importGroup.QueueRegexFindReplace(code, keyword, replacement, caseSensitive);
        }
        else
        {
            importGroup.QueueFindReplace(code, keyword, replacement, caseSensitive);
        }
        IncrementProgress();
    }
    SetProgressBar(null, "Final code import...", toDump.Count, toDump.Count);
    importGroup.Import();
});
DisableAllSyncBindings();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Completed");