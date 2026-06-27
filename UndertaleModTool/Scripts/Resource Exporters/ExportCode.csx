using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib.Util;
using UndertaleModLib.Scripting;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("The opened game uses YYC: no code is available.");
    return;
}

var builder = CreateScriptOptionsBuilder()
    .AddDirectory("folder", "Output Folder:")
    .AddText("patterns", "Names (one per line, leave empty for all):", multiline: true)
    .AddRadio("filterMode", "Filter mode:", "Exact", "Regex", "Wildcard")
    .AddBool("caseSensitive", "Case-sensitive", defaultValue: true)
    .AddBool("exportAssembly", "Export assembly (.asm) instead of decompiled code (.gml)?");

var result = ShowScriptOptionsDialog("Export Code", builder);
if (result is null) return;

string codeFolder = result["folder"] as string;
if (!Directory.Exists(codeFolder))
{
    ScriptError("The specified output folder does not exist.");
    return;
}
bool exportAssembly = result["exportAssembly"] as bool? == true;
string rawPatterns = result["patterns"] as string;
bool exportAll = string.IsNullOrWhiteSpace(rawPatterns);
string[] patterns = rawPatterns.Split("\n", StringSplitOptions.RemoveEmptyEntries);
NameFilterMode filterMode = Enum.Parse<NameFilterMode>(result["filterMode"] as string);
bool caseSensitive = result["caseSensitive"] as bool? == true;

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

if (exportAll)
{
    List<UndertaleCode> toDump = Data.Code.Where(c => c.ParentEntry is null).ToList();

    SetProgressBar(null, "Code Entries", 0, toDump.Count);
    StartProgressBarUpdater();

    await Task.Run(() => Parallel.ForEach(toDump, entry => DumpCode(entry)));

    await StopProgressBarUpdater();
    HideProgressBar();
}
else
{
    List<String> codeToDump = new();
    List<String> gameObjectCandidates = new();
    List<String> splitStringsList = new();

    foreach (var line in patterns)
        splitStringsList.Add(line.Trim());

    for (var j = 0; j < splitStringsList.Count; j++)
    {
        string name = splitStringsList[j];

        foreach (UndertaleGameObject obj in Data.GameObjects)
        {
            if (obj is null) continue;
            if (NameFilter.IsMatch(obj.Name.Content, name, filterMode, caseSensitive))
                gameObjectCandidates.Add(obj.Name.Content);
        }

        foreach (UndertaleScript scr in Data.Scripts)
        {
            if (scr is null || scr.Code == null) continue;
            if (NameFilter.IsMatch(scr.Name.Content, name, filterMode, caseSensitive))
                codeToDump.Add(scr.Code.Name.Content);
        }

        foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
        {
            if (globalInit is null || globalInit.Code == null) continue;
            if (NameFilter.IsMatch(globalInit.Code.Name.Content, name, filterMode, caseSensitive))
                codeToDump.Add(globalInit.Code.Name.Content);
        }

        foreach (UndertaleCode code in Data.Code)
        {
            if (code is null) continue;
            if (NameFilter.IsMatch(code.Name.Content, name, filterMode, caseSensitive))
                codeToDump.Add(code.Name.Content);
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
        catch { }
    }

    codeToDump = codeToDump.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();

    SetProgressBar(null, "Code Entries", 0, codeToDump.Count);
    StartProgressBarUpdater();

    await Task.Run(() =>
    {
        for (var j = 0; j < codeToDump.Count; j++)
            DumpCode(Data.Code.ByName(codeToDump[j]));
    });

    await StopProgressBarUpdater();
    HideProgressBar();
}

void DumpCode(UndertaleCode code)
{
    if (code is null) { IncrementProgressParallel(); return; }

    if (code.ParentEntry != null)
    {
        IncrementProgressParallel();
        return;
    }

    string ext = exportAssembly ? ".asm" : ".gml";
    string path = Paths.JoinVerifyWithinDirectory(codeFolder, code.Name.Content + ext);

    try
    {
        if (exportAssembly)
        {
            File.WriteAllText(path, code.Disassemble(Data.Variables, Data.CodeLocals?.For(code)));
        }
        else
        {
            File.WriteAllText(path, new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString());
        }
    }
    catch (Exception e)
    {
        WriteFailureFile(code, e.ToString());
    }

    IncrementProgressParallel();
}

void WriteFailureFile(UndertaleCode code, string errorMessage)
{
    string failedDir = Path.Join(codeFolder, "Failed");
    Directory.CreateDirectory(failedDir);
    string ext = exportAssembly ? ".asm" : ".gml";
    string path = Paths.JoinVerifyWithinDirectory(failedDir, code.Name.Content + ext);
    string label = exportAssembly ? "DISASSEMBLY" : "DECOMPILER";
    File.WriteAllText(path, $"/*\n{label} FAILED!\n\n{errorMessage}\n*/");
}
