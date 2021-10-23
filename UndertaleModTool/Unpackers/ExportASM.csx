using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

int progress = 0;
string codeFolder = GetFolder(FilePath) + "Export_Assembly" + Path.DirectorySeparatorChar;
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

if (Directory.Exists(codeFolder)) 
{
    ScriptError("An assembly export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

UpdateProgress();

await DumpCode();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);


void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress, Data.Code.Count);
    Interlocked.Increment(ref progress); //"thread-safe" increment
}

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


async Task DumpCode()
{
    await Task.Run(() => Parallel.ForEach(Data.Code, DumpCode));

    progress--;
}

void DumpCode(UndertaleCode code) 
{
    string path = Path.Combine(codeFolder, code.Name.Content + ".asm");
    try 
    {
        File.WriteAllText(path, (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : ""));
    }
    catch (Exception e) 
    {
        File.WriteAllText(path, "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
    }

    UpdateProgress();
}