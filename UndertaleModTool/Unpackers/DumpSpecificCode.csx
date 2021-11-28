//Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("You cannot do a code dump of a YYC game! There is no code to dump!");
    return;
}

ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

int failed = 0;

string codeFolder = PromptChooseDirectory("Export to where");
if (codeFolder == null)
    throw new ScriptException("The export folder was not set.");
Directory.CreateDirectory(Path.Combine(codeFolder, "Code"));
codeFolder = Path.Combine(codeFolder, "Code");

List<String> codeToDump = new List<String>();
List<String> gameObjectCandidates = new List<String>();
List<String> splitStringsList = new List<String>();
string InputtedText = "";
InputtedText = SimpleTextInput("Menu", "Enter object, script, or code entry names", InputtedText, true);
string[] IndividualLineArray = InputtedText.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
foreach (var OneLine in IndividualLineArray)
{
    splitStringsList.Add(OneLine.Trim());
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
    foreach (UndertaleScript scr in Data.Scripts)
    {
        if (scr.Code == null)
            continue;
        if (splitStringsList[j].ToLower() == scr.Name.Content.ToLower())
        {
            codeToDump.Add(scr.Code.Name.Content);
        }
    }
    foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
    {
        if (globalInit.Code == null)
            continue;
        if (splitStringsList[j].ToLower() == globalInit.Code.Name.Content.ToLower())
        {
            codeToDump.Add(globalInit.Code.Name.Content);
        }
     }
    foreach (UndertaleCode code in Data.Code)
    {
        if (splitStringsList[j].ToLower() == code.Name.Content.ToLower())
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
StartUpdater();

await Task.Run(() => {
    for (var j = 0; j < codeToDump.Count; j++)
    {
        DumpCode(Data.Code.ByName(codeToDump[j]));
    }
});

await StopUpdater();

void DumpCode(UndertaleCode code) 
{
    string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
    if (code.ParentEntry == null)
    {
        try 
        {
            File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
        }
        catch (Exception e) 
        {
            if (!(Directory.Exists(Path.Combine(codeFolder, "Failed"))))
            {
                Directory.CreateDirectory(Path.Combine(codeFolder, "Failed"));
            }
            path = Path.Combine(codeFolder, "Failed", code.Name.Content + ".gml");
            File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
            failed += 1;
        }
    }
    else
    {
        if (!(Directory.Exists(Path.Combine(codeFolder, "Duplicates"))))
        {
            Directory.CreateDirectory(Path.Combine(codeFolder, "Duplicates"));
        }
        try 
        {
            path = Path.Combine(codeFolder, "Duplicates", code.Name.Content + ".gml");
            File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value).Replace("@@This@@()", "self/*@@This@@()*/") : ""));
        }
        catch (Exception e) 
        {
            if (!(Directory.Exists(Path.Combine(codeFolder, "Duplicates", "Failed"))))
            {
                Directory.CreateDirectory(Path.Combine(codeFolder, "Duplicates", "Failed"));
            }
            path = Path.Combine(codeFolder, "Duplicates", "Failed", code.Name.Content + ".gml");
            File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
            failed += 1;
        }
    }
    IncProgress();
}
