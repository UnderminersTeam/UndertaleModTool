using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

EnsureDataLoaded();

string codeFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Code");
if (Directory.Exists(codeFolder))
{
    codeFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Code_2");
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
        string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
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
                string failedFolder = Path.Combine(codeFolder, "Failed");
                if (!Directory.Exists(failedFolder))
                {
                    Directory.CreateDirectory(failedFolder);
                }
                path = Path.Combine(failedFolder, code.Name.Content + ".gml");
                File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                failed += 1;
            }
        }
        else
        {
            string duplicatesFolder = Path.Combine(codeFolder, "Duplicates");
            if (!Directory.Exists(duplicatesFolder))
            {
                Directory.CreateDirectory(duplicatesFolder);
            }
            try
            {
                path = Path.Combine(duplicatesFolder, code.Name.Content + ".gml");
                File.WriteAllText(path, (code != null
                    ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString()
                    : ""));
            }
            catch (Exception e)
            {
                string duplicatesFailed = Path.Combine(duplicatesFolder, "Failed");
                if (!Directory.Exists(duplicatesFailed))
                {
                    Directory.CreateDirectory(duplicatesFailed);
                }
                path = Path.Combine(duplicatesFailed, code.Name.Content + ".gml");
                File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                failed += 1;
            }
        }

        IncrementProgress();
    }
}

