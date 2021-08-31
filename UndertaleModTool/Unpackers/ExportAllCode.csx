using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

if (!((Data.GMS2_3 == false) && (Data.GMS2_3_1 == false) && (Data.GMS2_3_2 == false)))
{
    //bool x = RunUMTScript(Path.Combine(ExePath, "HelperScripts", "ExportAllCode2_3.csx"));
    //if (x == false)
    ScriptError("Please run \"ExportAllCode2_3.csx\" instead!");
    return;
}

int progress = 0;
string codeFolder = GetFolder(FilePath) + "Export_Code" + Path.DirectorySeparatorChar;
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

if (Directory.Exists(codeFolder))
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

UpdateProgress();
await DumpCode();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);

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