//Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

int failed = 0;

string codeFolder = PromptChooseDirectory("Export to where");
if (codeFolder == null)
    throw new System.Exception("The export folder was not set.");
Directory.CreateDirectory(codeFolder + "/Code/");
codeFolder = codeFolder + "/Code/";

List<String> codeToDump = new List<String>();
List<String> gameObjectCandidates = new List<String>();
List<String> splitStringsList = new List<String>();
string abc123 = "";
string removed = "";
abc123 = SimpleTextInput("Menu", "Enter object names", abc123, true);
string[] subs = abc123.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
foreach (var sub in subs)
{
    splitStringsList.Add(sub);
}
for (var j = 0; j < splitStringsList.Count; j++)
{
    foreach (UndertaleGameObject obj in Data.GameObjects)
    {
        if (splitStringsList[j].ToLower() == obj.Name.Content.ToLower())
        {
            gameObjectCandidates.Add(obj.Name.Content);
        }
    }
}
foreach (UndertaleCode code in Data.Code)
{
    for (var j = 0; j < gameObjectCandidates.Count; j++)
    {
        if (code.Name.Content.Contains(gameObjectCandidates[j]))
        {
            codeToDump.Add(code.Name.Content);
        }
    }
}

int progress = 0;
int codesLeft = codeToDump.Count;
UpdateProgress();

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, codesLeft);
}

for (var j = 0; j < codeToDump.Count; j++)
{
    DumpCode(Data.Code.ByName(codeToDump[j]));
}

void DumpCode(UndertaleCode code) 
{
    string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
    if (!(code.DuplicateEntry))
    {
        if (path.Length > 150)
        {
            path = path.Substring(0, 150) + ".gml";
        }
        try 
        {
            File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value).Replace("@@This@@()", "self/*@@This@@()*/") : ""));
        }
        catch (Exception e) 
        {
            if (!(Directory.Exists(codeFolder + "/Failed/")))
            {
                Directory.CreateDirectory(codeFolder + "/Failed/");
            }
            if (path.Length > 150)
            {
                path = path.Substring(0, 150) + ".gml";
            }
            path = Path.Combine(codeFolder + "/Failed/", code.Name.Content + ".gml");
            File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
            failed += 1;
        }
    }
    else
    {
        if (!(Directory.Exists(codeFolder + "/Duplicates/")))
        {
            Directory.CreateDirectory(codeFolder + "/Duplicates/");
        }
        if (path.Length > 150)
        {
            path = path.Substring(0, 150) + ".gml";
        }
        try 
        {
            path = Path.Combine(codeFolder + "/Duplicates/", code.Name.Content + ".gml");
            File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value).Replace("@@This@@()", "self/*@@This@@()*/") : ""));
        }
        catch (Exception e) 
        {
            if (!(Directory.Exists(codeFolder + "/Duplicates/Failed/")))
            {
                Directory.CreateDirectory(codeFolder + "/Duplicates/Failed/");
            }
            if (path.Length > 150)
            {
                path = path.Substring(0, 150) + ".gml";
            }
            path = Path.Combine(codeFolder + "/Duplicates/Failed/", code.Name.Content + ".gml");
            File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
            failed += 1;
        }
    }
    UpdateProgress();
}
