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

string codeFolder = GetFolder(FilePath) + "Export_Code" + Path.DirectorySeparatorChar;
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

if (Directory.Exists(codeFolder))
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

List<UndertaleCode> toDump = Data.Code.Where(c => c.ParentEntry is null)
                                      .ToList();

SetProgressBar(null, "Code Entries", 0, toDump.Count);
StartProgressBarUpdater();

int failed = 0;
await Task.Run(DumpCode);

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder + " " + failed.ToString() + " failed");

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

void DumpCode()
{
    //Because 2.3 code names get way too long, we're gonna convert it to an index based system, starting with a lookup system
    string index_path = Path.Combine(codeFolder, "LookUpTable.txt");
    string index_text = "This is zero indexed, index 0 starts at line 2.";
    for (var i = 0; i < toDump.Count; i++)
    {
        UndertaleCode code = toDump[i];
        index_text += "\n";
        index_text += code.Name.Content;
    }
    File.WriteAllText(index_path, index_text);
    for (var i = 0; i < toDump.Count; i++)
    {
        UndertaleCode code = toDump[i];
        string path = Path.Combine(codeFolder, i.ToString() + ".gml");
        try
        {
            File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
        }
        catch (Exception e)
        {
            if (!(Directory.Exists(codeFolder + "/Failed/")))
            {
                Directory.CreateDirectory(codeFolder + "/Failed/");
            }
            path = Path.Combine(codeFolder + "/Failed/", i.ToString() + ".gml");
            File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
            failed += 1;
        }

        IncrementProgress();
    }
}
