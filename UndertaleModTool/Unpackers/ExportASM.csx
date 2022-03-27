using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

string codeFolder = GetFolder(FilePath) + "Export_Assembly" + Path.DirectorySeparatorChar;
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));
if (Directory.Exists(codeFolder))
{
    ScriptError("An assembly export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

SetProgressBar(null, "Code Entries", 0, Data.Code.Count);
StartProgressBarUpdater();

await DumpCode();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);


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
    string path = Path.Combine(codeFolder, code.Name.Content + ".asm");
    try
    {
        File.WriteAllText(path, (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : ""));
    }
    catch (Exception e)
    {
        File.WriteAllText(path, "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
    }

    IncrementProgressParallel();
}