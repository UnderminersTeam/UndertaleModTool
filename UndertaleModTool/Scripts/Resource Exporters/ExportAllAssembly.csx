using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

EnsureDataLoaded();

if (Data.IsYYC())
{
    ScriptError("The opened game uses YYC: no code is available.");
    return;
}

string codeFolder = PromptChooseDirectory();
if (codeFolder is null)
{
    return;
}

List<UndertaleCode> toDump = Data.Code.Where(c => c.ParentEntry is null).ToList();

SetProgressBar(null, "Code Entries", 0, toDump.Count);
StartProgressBarUpdater();

await DumpCode();

await StopProgressBarUpdater();
HideProgressBar();

async Task DumpCode()
{
    await Task.Run(() => Parallel.ForEach(toDump, DumpCode));
}

void DumpCode(UndertaleCode code)
{
    if (code is not null)
    {
        string path = Path.Combine(codeFolder, $"{code.Name.Content}.asm");
        try
        {
            File.WriteAllText(path, code.Disassemble(Data.Variables, Data.CodeLocals?.For(code)));
        }
        catch (Exception e)
        {
            File.WriteAllText(path, $"/*\nDISASSEMBLY FAILED!\n\n{e}\n*/");
        }
    }

    IncrementProgressParallel();
}