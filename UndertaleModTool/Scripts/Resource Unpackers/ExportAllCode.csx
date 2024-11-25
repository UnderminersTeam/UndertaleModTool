using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

EnsureDataLoaded();

string codeFolder = Path.Combine(Path.GetDirectoryName(FilePath), "Export_Code");
if (Directory.Exists(codeFolder))
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}

Directory.CreateDirectory(codeFolder);

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

bool exportFromCache = false;
if (GMLCacheEnabled && Data.GMLCache is not null)
    exportFromCache = ScriptQuestion("Export from the cache?");

List<UndertaleCode> toDump;
if (!exportFromCache)
{
    toDump = Data.Code.Where(c => c.ParentEntry is null)
                      .ToList();
}

bool cacheGenerated = false;
if (exportFromCache)
{
    cacheGenerated = await GenerateGMLCache(globalDecompileContext);
    await StopProgressBarUpdater();
}

SetProgressBar(null, "Code Entries", 0, exportFromCache ? Data.GMLCache.Count + Data.GMLCacheFailed.Count : toDump.Count);
StartProgressBarUpdater();

await DumpCode();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);

async Task DumpCode()
{
    if (cacheGenerated)
    {
        await Task.Run(() => Parallel.ForEach(Data.GMLCache, DumpCachedCode));

        if (Data.GMLCacheFailed.Count > 0)
        {
            if (Data.GlobalFunctions is null)
            {
                SetProgressBar(null, "Building the cache of all global functions...", 0, 0);
                await Task.Run(() => GlobalDecompileContext.BuildGlobalFunctionCache(Data));
                SetProgressBar(null, "Code Entries", 0, Data.GMLCache.Count + Data.GMLCacheFailed.Count);
            }   

            await Task.Run(() => Parallel.ForEach(Data.GMLCacheFailed, (codeName) => DumpCode(Data.Code.ByName(codeName))));
        }
    }
    else
    {
        if (Data.GlobalFunctions is null)
        {
            SetProgressBar(null, "Building the cache of all global functions...", 0, 0);
            await Task.Run(() => GlobalDecompileContext.BuildGlobalFunctionCache(Data));
            SetProgressBar(null, "Code Entries", 0, toDump.Count);
        }

        await Task.Run(() => Parallel.ForEach(toDump, DumpCode));
    }
}

void DumpCode(UndertaleCode code)
{
    if (code is not null)
    {
        string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
        try
        {
            File.WriteAllText(path, (code != null 
                ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString() 
                : ""));
        }
        catch (Exception e)
        {
            File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
        }
    }

    IncrementProgressParallel();
}
void DumpCachedCode(KeyValuePair<string, string> code)
{
    string path = Path.Combine(codeFolder, code.Key + ".gml");

    File.WriteAllText(path, code.Value);

    IncrementProgressParallel();
}