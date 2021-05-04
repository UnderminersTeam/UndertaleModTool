using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

int progress = 0;
string codeFolder = GetFolder(FilePath) + "Export_Code" + Path.DirectorySeparatorChar;
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

if (Directory.Exists(codeFolder)) 
{
    codeFolder = GetFolder(FilePath) + "Export_Code_2" + Path.DirectorySeparatorChar;
}

Directory.CreateDirectory(codeFolder);

UpdateProgress();
int failed = 0;
DumpCode();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder + " " + failed.ToString() + " failed");

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


void DumpCode() 
{
    foreach(UndertaleCode code in Data.Code)
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
                File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
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
                File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
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
}

