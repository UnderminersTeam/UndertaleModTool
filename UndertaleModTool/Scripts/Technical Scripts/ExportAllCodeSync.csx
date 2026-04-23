using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

string codeFolder = Path.Join(Path.GetDirectoryName(FilePath), "Export_Code");
if (Directory.Exists(codeFolder))
{
    codeFolder = Path.Join(Path.GetDirectoryName(FilePath), "Export_Code_2");
}

Directory.CreateDirectory(codeFolder);

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

List<UndertaleCode> toDump = Data.Code.Where(c => c.ParentEntry is null)
                                      .ToList();

SetProgressBar(null, "Code Entries", 0, toDump.Count);
StartProgressBarUpdater();

int failed = 0;
await Task.Run(DumpCode);

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder + " " + failed.ToString() + " failed");


void DumpCode()
{
    foreach (UndertaleCode code in toDump)
    {
        string path = Paths.JoinVerifyWithinDirectory(codeFolder, code.Name.Content + ".gml");
        if (code.ParentEntry == null)
        {
            try
            {
                File.WriteAllText(path, (code != null 
                    ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString() 
                    : ""));
            }
            catch (Exception e)
            {
                string failedFolder = Path.Join(codeFolder, "Failed");
                if (!Directory.Exists(failedFolder))
                {
                    Directory.CreateDirectory(failedFolder);
                }
                path = Paths.JoinVerifyWithinDirectory(failedFolder, code.Name.Content + ".gml");
                File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                failed += 1;
            }
        }
        else
        {
            string duplicatesFolder = Path.Join(codeFolder, "Duplicates");
            if (!Directory.Exists(duplicatesFolder))
            {
                Directory.CreateDirectory(duplicatesFolder);
            }
            try
            {
                path = Paths.JoinVerifyWithinDirectory(duplicatesFolder, code.Name.Content + ".gml");
                File.WriteAllText(path, (code != null
                    ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString()
                    : ""));
            }
            catch (Exception e)
            {
                string duplicatesFailed = Path.Join(duplicatesFolder, "Failed");
                if (!Directory.Exists(duplicatesFailed))
                {
                    Directory.CreateDirectory(duplicatesFailed);
                }
                path = Paths.JoinVerifyWithinDirectory(duplicatesFailed, code.Name.Content + ".gml");
                File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                failed += 1;
            }
        }

        IncrementProgress();
    }
}

