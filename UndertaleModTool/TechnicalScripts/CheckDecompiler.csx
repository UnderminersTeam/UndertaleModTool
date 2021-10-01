//Made by Grossley ( Grossley#2869 on Discord )
//Changes:
//Version 01 (November 13th, 2020): Initial release
//Version 02 (April 21st, 2021): Updated for GMS 2.3, general refactor
//Version 03 (April 29th, 2021): Refactored for the profile system, simplified, removed unnecessary components
//Version 04 (October 1st, 2021): Have CheckDecompiler and CheckDecompiler 2.3 use the same (cleaner) code base and combine them.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

int progress = 0;
int identical_count = 0;
bool Is_GMS_2_3 = (Data.GMS2_3 || Data.GMS2_3_1 || Data.GMS2_3_2);
if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    Is_GMS_2_3 = false;
}

if (!InitialCheck())
    return;

UpdateProgress();

ExportGML("Export_Code_Orig");
ExportASM("Export_Assembly_Orig");
ImportGML("Export_Code_Orig");
ExportASM("Export_Assembly_Recompiled");
FileCompare("Export_Assembly_Orig", "Export_Assembly_Recompiled", "Export_Code_Orig");
ImportASM("Export_Assembly_Orig");

HideProgressBar();
double percentage = ((double)identical_count / (double)Data.Code.Count) * 100;
int non_matching = Data.Code.Count - identical_count;
ScriptMessage("Non-matching Data Generated. Decompiler/Compiler Accuracy: " + percentage.ToString() + "% (" + identical_count.ToString() + "/" + Data.Code.Count.ToString() + "). Number of differences: " + non_matching.ToString() + ". To review these, the differing files are in the game directory.");
return;

bool InitialCheck()
{
    if (Data?.GeneralInfo.BytecodeVersion < 15)
    {
        ScriptError("This script will not work properly on Undertale 1.0 and other bytecode < 15 games.");
        return false;
    }
    if (Directory.Exists(GetSpecialPath("Export_Code_Orig")))
    {
        ScriptError("A code export already exists. Please remove it.", "Error");
        return false;
    }
    else
    {
        Directory.CreateDirectory(GetSpecialPath("Export_Code_Orig"));
    }
    if (Directory.Exists(GetSpecialPath("Export_Assembly_Orig")))
    {
        ScriptError("A code export already exists. Please remove it.", "Error");
        return false;
    }
    else
    {
        Directory.CreateDirectory(GetSpecialPath("Export_Assembly_Orig"));
    }
    if (Directory.Exists(GetSpecialPath("Export_Assembly_Recompiled")))
    {
        ScriptError("A code export already exists. Please remove it.", "Error");
        return false;
    }
    else
    {
        Directory.CreateDirectory(GetSpecialPath("Export_Assembly_Recompiled"));
    }
    return true;
}

void ImportGML(string importFolder)
{
    ImportCode(importFolder, true);
}

void ImportASM(string importFolder)
{
    ImportCode(importFolder, false);
}

void ExportGML(string path)
{
    ExportCode(path, true);
}

void ExportASM(string path)
{
    ExportCode(path, false);
}

void FileCompare(string asm_orig_path, string asm_new_path, string gml_orig_path)
{
    UpdateProgress();
    asm_orig_path = GetSpecialPath(asm_orig_path);
    asm_new_path = GetSpecialPath(asm_new_path);
    gml_orig_path = GetSpecialPath(gml_orig_path);
    for (var i = 0; i < Data.Code.Count; i++)
    {
        UpdateProgressBar(null, "Deleting identical files", progress++, Data.Code.Count);
        string DetCodeName = (Is_GMS_2_3 ? i.ToString() : code.Name.Content);
        string orig_asm_path = Path.Combine(asm_orig_path, DetCodeName + ".asm");
        string new1_asm_path = Path.Combine(asm_new_path, DetCodeName + ".asm");
        string orig_gml_path = Path.Combine(gml_orig_path, DetCodeName + ".gml");
        if (!(File.Exists(orig_asm_path) && File.Exists(new1_asm_path) && File.Exists(orig_gml_path)))
        {
            continue;
        }
        if (AreFilesIdentical(orig_asm_path, new1_asm_path))
        {
            File.Delete(orig_gml_path);
            File.Delete(orig_asm_path);
            File.Delete(new1_asm_path);
            identical_count += 1;
        }
    }
    progress = 0;
}

void ImportCode(string importFolder, bool IsGML = true)
{
    progress = 0;
    importFolder = GetSpecialPath(importFolder);
    List<string> CodeList = null;
    if (Is_GMS_2_3)
    {
        CodeList = GetCodeList(importFolder);
        if (CodeList == null)
            return;
    }
    string[] dirFiles = Directory.GetFiles(importFolder);
    foreach (string file in dirFiles)
    {
        UpdateProgressBar(null, (IsGML ? "Import GML Files" : "Reapply original ASM files"), progress++, dirFiles.Length);
        string fileName = Path.GetFileName(file);
        if (!(fileName.EndsWith(IsGML ? ".gml" : ".asm")))
            continue;
        fileName = Path.GetFileNameWithoutExtension(file);
        string codeName = fileName;
        if (Is_GMS_2_3)
        {
            int number;
            bool success = Int32.TryParse(fileName, out number);
            if (success)
            {
                codeName = CodeList[number];
            }
            else
            {
                ScriptError((IsGML ? "GML" : "ASM") + " file not in range of look up table!", "Error");
                return;
            }
        }
        if (File.Exists(file))
        {
            string gmlCode = File.ReadAllText(file);
            try
            {
                if (IsGML)
                    ImportGMLString(codeName, gmlCode, false, true);
                else
                    ImportASMString(codeName, gmlCode, false, true);
            }
            catch
            {
                if (IsGML)
                    Data.Code.ByName(codeName).ReplaceGML("", Data);
            }
        }
    }
    UpdateProgress();
}

void ExportCode(string old_path, bool IsGML = true)
{
    old_path = GetSpecialPath(old_path);
    UpdateProgress();
    if (Is_GMS_2_3)
        SetUpLookUpTable(path);
    for (var i = 0; i < Data.Code.Count; i++)
    {
        UndertaleCode code = Data.Code[i];
        string path = Path.Combine(old_path, (Is_GMS_2_3 ? i.ToString() : code.Name.Content) + (IsGML ? ".gml" : ".asm"));
        if (Data.Code.ByName(code.Name.Content).ParentEntry == null)
            File.WriteAllText(path, (IsGML ? GetDecompiledText(code.Name.Content) : GetDisassemblyText(code.Name.Content)));
        UpdateProgressBar(null, "Exporting " + (IsGML ? "GML" : "Disassembly") + " Code", progress++, Data.Code.Count);
    }
    progress = 0;
    UpdateProgress();
}

string GetSpecialPath(string FolderName)
{
    return GetFolder(FilePath) + FolderName + Path.DirectorySeparatorChar;
}

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

List<string> GetCodeList(string importFolder)
{
    List<string> CodeList = new List<string>();
    string index_path = Path.Combine(importFolder, "LookUpTable.txt");
    if (File.Exists(index_path))
    {
        int counter = 0;
        string line;
        System.IO.StreamReader file = new System.IO.StreamReader(index_path);
        while ((line = file.ReadLine()) != null)
        {
            if ((counter > 0) && (line.Length >= 1))
                CodeList.Add(line);
            counter++;
        }
        file.Close();
    }
    else
    {
        ScriptError("No LookUpTable.txt!", "Error");
        CodeList = null;
    }
    return CodeList;
}

void SetUpLookUpTable(string path)
{
    string index_path = Path.Combine(path, "LookUpTable.txt");
    string index_text = "This is zero indexed, index 0 starts at line 2.\n";
    for (var i = 0; i < Data.Code.Count; i++)
    {
        UndertaleCode code = Data.Code[i];
        index_text += code.Name.Content;
        index_text += "\n";
    }
    File.WriteAllText(index_path, index_text);
}

