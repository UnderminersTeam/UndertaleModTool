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


List<string> toDump;
toDump = new();
foreach (UndertaleScript code in Data.Scripts)
{
    toDump.Add(code.Name.Content);
}

SetProgressBar(null, "Code Entries", 0, toDump.Count);
StartProgressBarUpdater();

foreach (string scr in toDump)
{
    DumpCode(scr);
}

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);


string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

public class ParentsData
{
    public string name { get; set; }
    public string path { get; set; }
}

public class CodeData
{
    public string resourceType { get; set; }
    public string resourceVersion { get; set; }
    public string name { get; set; }
    public bool isDnD { get; set; }
    public bool isCompatibility { get; set; }
    public ParentsData parent { get; set; }
}
void DumpCode(string code)
{
    if (code is not null && !code.StartsWith("gml_Object_") && !code.StartsWith("gml_Room"))
    {
        string nameScr = code.Replace("gml_Script_", "").Replace("gml_GlobalScript_", "");
        string folderName = Path.Combine(codeFolder, nameScr);
        string gmlPath = Path.Combine(folderName, nameScr + ".gml");
        string yyPath = Path.Combine(folderName, nameScr + ".yy");

        try
        {
            Directory.CreateDirectory(folderName);

            string decompiledCode = $"function {nameScr} () {{\n\n}}";
            File.WriteAllText(gmlPath, decompiledCode);
            
            ParentsData parentdata = new ParentsData()
            {
                name = "Scripts",
                path = "folders/Scripts.yy"
            };
            CodeData codeyyjson = new CodeData()
            {
               resourceType = "GMScript",
               resourceVersion = "1.0",
               name = nameScr,
               isDnD = false,
               isCompatibility = false,
               parent = parentdata,
            };
            string exportedyy = JsonConvert.SerializeObject(codeyyjson, Formatting.Indented);
            File.WriteAllText(yyPath, exportedyy);
        }
        catch (Exception e)
        {
            File.WriteAllText(gmlPath, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
        }
    }

    IncrementProgressParallel();
}
