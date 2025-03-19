using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

EnsureDataLoaded();

bool is2_3 = false;
if (Data.IsVersionAtLeast(2, 3))
{
    is2_3 = true;
    ScriptMessage("This script is for GMS 2.3 games, because some code names get so long that Windows cannot write them adequately.");
}
else
{
    ScriptError("Use the regular ExportAllCode please!", "Incompatible");
}

string codeFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Code");
GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

if (Directory.Exists(codeFolder))
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

List<UndertaleCode> toDump = Data.Code.Where(c => c.ParentEntry is null)
                                      .ToList();
object lockObj = new();

SetProgressBar(null, "Code Entries", 0, toDump.Count);
StartProgressBarUpdater();

int failed = 0;
await DumpCode();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder + "\n" + failed.ToString() + " failed");

async Task DumpCode()
{
    // Because 2.3 code names get way too long, we're gonna convert it to an index based system, starting with a lookup system
    string indexPath = Path.Combine(codeFolder, "LookUpTable.txt");
    StringBuilder indexText = new("This is zero indexed, index 0 starts at line 2.");
    for (var i = 0; i < toDump.Count; i++)
        indexText.Append($"\n{toDump[i].Name.Content}");

    File.WriteAllText(indexPath, indexText.ToString());

    if (Data.GlobalFunctions is null) // if we run script before opening any code
    {
        SetProgressBar(null, "Building the cache of all global functions...", 0, 0);
        await Task.Run(() => GlobalDecompileContext.BuildGlobalFunctionCache(Data));
        SetProgressBar(null, "Code Entries", 0, toDump.Count);
    }

    await Task.Run(() => Parallel.For(0, toDump.Count - 1, (i, _) =>
    {
        UndertaleCode code = toDump[i];
        string path = Path.Combine(codeFolder, i.ToString() + ".gml");
        try
        {
            File.WriteAllText(path, (code != null 
                ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString()
                : ""));
        }
        catch (Exception e)
        {
            string failedFolder = Path.Combine(codeFolder, "Failed");
            lock (lockObj)
            {
                if (!Directory.Exists(failedFolder))
                {
                    Directory.CreateDirectory(failedFolder);
                }
            }
            
            path = Path.Combine(failedFolder, i.ToString() + ".gml");
            File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
            failed += 1;
        }

        IncrementProgressParallel();
    }));
}
