//Made by Grossley ( Grossley#2869 on Discord )
//Changes:
//Version 01 (November 13th, 2020): Initial release
//Version 02 (April 29th, 2021): Reworked to be simpler + utilize the profile system

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

if (!((Data.GMS2_3 == false) && (Data.GMS2_3_1 == false) && (Data.GMS2_3_2 == false)))
{
    bool x = RunUMTScript(Path.Combine(ExePath, "HelperScripts", "CheckDecompiler_2_3.csx"));
    if (x == false)
        ScriptError("CheckDecompiler_2_3.csx failed!");
    return;
}

EnsureDataLoaded();

//TODO: Remove this bool from the data file, it's not strictly necessary.
int progress = 0;
int identical_count = 0;

if (!InitialCheck())
    return;

UpdateProgress();

ExportGML(Path.Combine(GetFolder(FilePath), "Export_Code_Orig"));
ExportASM(Path.Combine(GetFolder(FilePath), "Export_Assembly_Orig"));
ImportGML(Directory.GetFiles(Path.Combine(GetFolder(FilePath), "Export_Code_Orig")));
ExportASM(Path.Combine(GetFolder(FilePath), "Export_Assembly_Recompiled"));
await FileCompare();
ImportASM(Directory.GetFiles(Path.Combine(GetFolder(FilePath), "Export_Assembly_Orig")));

HideProgressBar();
double percentage = ((double)identical_count / (double)Data.Code.Count) * 100;
int non_matching = Data.Code.Count - identical_count;
ScriptMessage("Non-matching Data Generated. Decompiler/Compiler Accuracy: " + percentage.ToString() + "% (" + identical_count.ToString() + "/" + Data.Code.Count.ToString() + "). Number of differences: " + non_matching.ToString() + ". To review these, the differing files are in the game directory.");
return;

bool InitialCheck()
{
    if (Data?.GeneralInfo.BytecodeVersion < 15)
    {
        ScriptError("This script will not work properly on Undertale 1.0 and other bytecode < 15 games.", "Error");
        return false;
    }
    if (Directory.Exists(GetFolder(FilePath) + "Export_Code_Orig" + Path.DirectorySeparatorChar))
    {
        ScriptError("A code export already exists. Please remove it.", "Error");
        return false;
    }
    else
    {
        Directory.CreateDirectory(GetFolder(FilePath) + "Export_Code_Orig" + Path.DirectorySeparatorChar);
    }
    if (Directory.Exists(GetFolder(FilePath) + "Export_Assembly_Orig" + Path.DirectorySeparatorChar))
    {
        ScriptError("A code export already exists. Please remove it.", "Error");
        return false;
    }
    else
    {
        Directory.CreateDirectory(GetFolder(FilePath) + "Export_Assembly_Orig" + Path.DirectorySeparatorChar);
    }
    if (Directory.Exists(GetFolder(FilePath) + "Export_Assembly_Recompiled" + Path.DirectorySeparatorChar))
    {
        ScriptError("A code export already exists. Please remove it.", "Error");
        return false;
    }
    else
    {
        Directory.CreateDirectory(GetFolder(FilePath) + "Export_Assembly_Recompiled" + Path.DirectorySeparatorChar);
    }
    return true;
}

void ImportGML(string[] dirFiles)
{
    foreach (string file in dirFiles)
    {
        UpdateProgressBar(null, "Import Files", progress++, dirFiles.Length);
        string codeName = Path.GetFileNameWithoutExtension(file);
        try
        {
            ImportGMLFile(file, true, true);
        }
        catch
        {
            Data.Code.ByName(codeName).ReplaceGML("", Data);
        }
    }
    progress = 0;
    UpdateProgress();
}

void ImportASM(string[] dirFiles)
{
    progress = 0;
    foreach (string file in dirFiles)
    {
        UpdateProgressBar(null, "Reapply original ASM files", progress++, dirFiles.Length);
        try
        {
            ImportASMFile(file, false, false, true);
        }
        catch (Exception ex)
        {
        }
    }
}

void ExportGML(string codeFolder)
{
    foreach (UndertaleCode code in Data.Code)
    {
        string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
        File.WriteAllText(path, GetDecompiledText(code.Name.Content));
        UpdateProgressBar(null, "Exporting Original Code Entries", progress++, Data.Code.Count);
    }
    progress = 0;
    UpdateProgress();
}

void ExportASM(string codeFolder)
{
    foreach (UndertaleCode code in Data.Code)
    {
        string path = Path.Combine(codeFolder, code.Name.Content + ".asm");
        File.WriteAllText(path, GetDisassemblyText(code.Name.Content));
        UpdateProgressBar(null, "Exporting Code Disassembly", progress++, Data.Code.Count);
    }
    progress = 0;
    UpdateProgress();
}

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task FileCompare()
{
    await Task.Run(() => Parallel.ForEach(Data.Code, FileCompare));
}

void FileCompare(UndertaleCode code)
{
    UpdateProgressBar(null, "Deleting identical files", progress++, Data.Code.Count);
    string orig_gml_path = Path.Combine(GetFolder(FilePath), "Export_Code_Orig", code.Name.Content + ".gml");
    string orig_asm_path = Path.Combine(GetFolder(FilePath), "Export_Assembly_Orig", code.Name.Content + ".asm");
    string new1_asm_path = Path.Combine(GetFolder(FilePath), "Export_Assembly_Recompiled", code.Name.Content + ".asm");
    if (AreFilesIdentical(orig_asm_path, new1_asm_path))
    {
        File.Delete(orig_gml_path);
        File.Delete(orig_asm_path);
        File.Delete(new1_asm_path);
        identical_count += 1;
    }
}
