﻿using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

EnsureDataLoaded();

string codeFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Assembly");
if (Directory.Exists(codeFolder))
{
    ScriptError("An assembly export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

List<UndertaleCode> toDump = Data.Code.Where(c => c.ParentEntry is null)
                                      .ToList();

SetProgressBar(null, "Code Entries", 0, toDump.Count);
StartProgressBarUpdater();

await DumpCode();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);


async Task DumpCode()
{
    await Task.Run(() => Parallel.ForEach(toDump, DumpCode));
}

void DumpCode(UndertaleCode code)
{
    string path = Path.Combine(codeFolder, code.Name.Content + ".asm");
    try
    {
        File.WriteAllText(path, (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals?.For(code)) : ""));
    }
    catch (Exception e)
    {
        File.WriteAllText(path, "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
    }

    IncrementProgressParallel();
}