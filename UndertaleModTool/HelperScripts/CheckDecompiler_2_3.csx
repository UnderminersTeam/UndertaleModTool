//Made by Grossley ( Grossley#2869 on Discord )
//Changes:
//Version 01 (November 13th, 2020): Initial release
//Version 02 (April 21st, 2021): Updated for GMS 2.3, general refactor
//Version 02 (April 21st, 2021): Refactored for the profile system, simplified, removed unnecessary components

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));
int progress = 0;
int identical_count = 0;

if (!InitialCheck())
    return;

ExportGML("Export_Code_Orig");
ExportASM("Export_Assembly_Orig");
ImportGML("Export_Code_Orig");
ExportASM("Export_Assembly_Recompiled");
FileCompare("Export_Assembly_Orig", "Export_Assembly_Recompiled", "Export_Code_Orig");
ImportASM("Export_Assembly_Orig");
HideProgressBar();
double percentage = ((double)identical_count / (double)Data.Code.Count) * 100;
int non_matching = Data.Code.Count - identical_count;
ScriptMessage("Non-matching Data Generated. Decompiler/Compiler Accuracy: " + percentage.ToString() + "% (" + identical_count.ToString() + "/" + Data.Code.Count.ToString() + "). Number of differences: " +non_matching.ToString()+ ". To review these, the differing files are in the game directory.");

bool InitialCheck()
{
    if ((Data.GMS2_3 == false) && (Data.GMS2_3_1 == false) && (Data.GMS2_3_2 == false))
    {
        ScriptError("Use the regular CheckDecompiler please!", "Incompatible");
        return false;
    }
    else
    {
        ScriptMessage("This script is for GMS 2.3 games, because some code names get so long that Windows cannot write them adequately.");
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
        string orig_asm_path = asm_orig_path + i.ToString() + ".asm";
        string new1_asm_path = asm_new_path + i.ToString() + ".asm";
        string orig_gml_path = gml_orig_path + i.ToString() + ".gml";
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

List<string> GetCodeList(string importFolder)
{
    List<string> CodeList = new List<string>();
    string index_path = Path.Combine(importFolder, "LookUpTable.txt");
    if (File.Exists(index_path))
    {
        int counter = 0;  
        string line;  
        System.IO.StreamReader file = new System.IO.StreamReader(index_path);  
        while((line = file.ReadLine()) != null)
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

void ImportCode(string importFolder, bool IsGML = true)
{
    importFolder = GetSpecialPath(importFolder);
    bool doParse = true;

    List<string> CodeList = GetCodeList(importFolder);
    if (CodeList == null)
        return;

    string[] dirFiles = Directory.GetFiles(importFolder);
    foreach (string file in dirFiles)
    {
        bool SkipPortions = false;
        UpdateProgressBar(null, "Import Files", progress++, dirFiles.Length);
        string fileName = Path.GetFileName(file);
        if (!(fileName.EndsWith(IsGML ? ".gml" : ".asm")))
            continue;
        fileName = Path.GetFileNameWithoutExtension(file);
        int number;
        bool success = Int32.TryParse(fileName, out number);
        string codeName;
        if (success)
        {
            codeName = CodeList[number];
            fileName = codeName + (IsGML ? ".gml" : ".asm");
        }
        else
        {
            ScriptError((IsGML ? "GML" : "ASM") + " file not in range of look up table!", "Error");
            return;
        }
        if (fileName.EndsWith("CleanUp_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
            continue; // Restarts loop if file is not a valid code asset.
        if (fileName.EndsWith("PreCreate_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
            continue; // Restarts loop if file is not a valid code asset.
        if (File.Exists(file))
        {
            string gmlCode = File.ReadAllText(file);
            if (IsGML)
                ImportGMLString(codeName, gmlCode, false, true);
            else
                ImportASMString(codeName, gmlCode, false, true);
        }
    }
    progress = 0;
}

void ExportCode(string path, bool IsGML = true)
{
    path = GetSpecialPath(path);
    string old_path = path;
    UpdateProgress();
    SetUpLookUpTable(path);
    for (var i = 0; i < Data.Code.Count; i++)
    {
        UndertaleCode code = Data.Code[i];
        path = Path.Combine(old_path, i.ToString() + (IsGML ? ".gml" : ".asm"));
        if (Data.Code.ByName(code.Name.Content).ParentEntry == null)
            File.WriteAllText(path, (IsGML ? GetDecompiledText(code.Name.Content) : GetDisassemblyText(code.Name.Content)));
        UpdateProgressBar(null, "Exporting " + (IsGML ? "GML" : "Disassembly") + " Code", progress++, Data.Code.Count);
    }
    progress = 0;
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
