using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

int progress = 0;
string codeFolder = GetFolder(FilePath) + "Export_Code" + Path.DirectorySeparatorChar;
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

if (Directory.Exists(codeFolder))
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

var toDump = new List<UndertaleCode>();
foreach (UndertaleCode code in Data.Code)
{
    if (code.ParentEntry != null)
        continue;
    toDump.Add(code);
}

UpdateProgress();

await DumpCode();

HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);


void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress, toDump.Count);
    Interlocked.Increment(ref progress); //"thread-safe" increment
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


async Task DumpCode()
{
    var data = DECOMPILE_CONTEXT.Value.Data;
    if (data?.KnownSubFunctions is null) //if we run script before opening any code
        Decompiler.BuildSubFunctionCache(data);

    await Task.Run(() => Parallel.ForEach(toDump, DumpCode));

    UpdateProgress();
    progress--;
}

void DumpCode(UndertaleCode code)
{
    string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
    try
    {
        File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
    }
    catch (Exception e)
    {
        File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
    }

    UpdateProgress();
}