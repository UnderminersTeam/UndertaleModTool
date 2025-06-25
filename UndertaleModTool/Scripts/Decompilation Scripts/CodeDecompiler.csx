// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

EnsureDataLoaded();

bool wrapFunc = !Data.IsVersionAtLeast(2, 3);

string codeFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "scripts" + Path.DirectorySeparatorChar;
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));
if (Directory.Exists(codeFolder))
{
    Directory.Delete(codeFolder, true);
}

Directory.CreateDirectory(codeFolder);


double toDump = 0;
List<UndertaleCode> codeDumped = new();
List<UndertaleCode> globalInitCodes = new();
foreach (UndertaleScript script in Data.Scripts)
{
    if (script.Code?.ParentEntry != null)
        continue;
    toDump++;
    codeDumped.Add(script.Code);
}
foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
{
    globalInitCodes.Add(globalInit.Code);
    if (codeDumped.Contains(globalInit.Code)) continue;
    toDump++;
}

SetProgressBar(null, "Scripts (and global init)", 0, toDump);
StartProgressBarUpdater();

await Task.Run(() => Parallel.ForEach(Data.Scripts, (UndertaleScript scr) => {
    if (scr.Code?.ParentEntry != null) return;
    DumpScript(scr.Name.Content);
    DumpCode(scr.Code, scr.Name.Content, wrapFunc);
    IncrementProgressParallel();
}));

string globalInitCode = "";
await Task.Run(() => Parallel.ForEach(Data.GlobalInitScripts, (UndertaleGlobalInit globalInit) => {
    UndertaleCode code = globalInit.Code;
    if (codeDumped.Contains(code)) return;
    string gml = DumpGlobalInit(code);
    globalInitCode += $"gml_pragma(\"global\", @'{gml}')\n";
    IncrementProgressParallel();
}));

if (globalInitCode != "") {
    string scriptName = "___global_init";
    string gmlPath = Path.Combine(codeFolder, scriptName, scriptName + ".gml");
    DumpScript(scriptName);
    File.WriteAllText(gmlPath, globalInitCode);
}

await StopProgressBarUpdater();
HideProgressBar();


string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }
}

public class GMScript
{
    public string resourceType { get; set; }
    public string resourceVersion { get; set; }
    public string name { get; set; }
    public bool isDnD { get; set; }
    public bool isCompatibility { get; set; }
    public AssetReference parent { get; set; }
}

void DumpScript(string scriptName)
{
    string folderName = Path.Combine(codeFolder, scriptName);
    string yyPath = Path.Combine(folderName, scriptName + ".yy");

    Directory.CreateDirectory(folderName);

    GMScript scriptData = new GMScript()
    {
       resourceType = "GMScript",
       resourceVersion = "1.0",
       name = scriptName,
       isDnD = false,
       isCompatibility = false,
       parent = new AssetReference()
        {
            name = "Scripts",
            path = "folders/Scripts.yy"
        },
    };
    string exportedyy = JsonConvert.SerializeObject(scriptData, Formatting.Indented);
    File.WriteAllText(yyPath, exportedyy);
}

string Indent(string str, string indent)
{
    return indent + str.Replace("\n", "\n" + indent);
}

void DumpCode(UndertaleCode code, string scriptName, bool wrapFunc)
{
    string folderName = Path.Combine(codeFolder, scriptName);
    string gmlPath = Path.Combine(folderName, scriptName + ".gml");

    // some gameframe functions are like a pre-2.3 script even in 2.3...
    // i'm guessing this is due to a gml extension
    if (
        !wrapFunc && Data.IsVersionAtLeast(2, 3) && 
        !globalInitCodes.Contains(code)
    ) {
        wrapFunc = true;
    }

    if (code != null) {
        try
        {
            Directory.CreateDirectory(folderName);

            if (!wrapFunc)
            {
                File.WriteAllText(gmlPath, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : $""));
            }
            else
            {
                string decompiledCode = (
                    code != null ?
                    $"function {scriptName}() {{\n{Indent(Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value), "    ").TrimEnd()}\n}}" :
                    $"function {scriptName}(){{}}"
                );
                decompiledCode = Regex.Replace(decompiledCode, "argument(\\d+)", "argument[$1]");
                File.WriteAllText(gmlPath, decompiledCode);
            }
        }
        catch (Exception e)
        {
            File.WriteAllText(gmlPath, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
        }
    } else {
        File.WriteAllText(gmlPath, "");
    }
}

string DumpGlobalInit(UndertaleCode code)
{
    string gml;
    try
    {
        gml = Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value);
    }
    catch (Exception e)
    {
        gml = "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/";
    }

    gml = gml.Replace("'", "'+\"'\"+@'").TrimEnd();
    return gml;
}
