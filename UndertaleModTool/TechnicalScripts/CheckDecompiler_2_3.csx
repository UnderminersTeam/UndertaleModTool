//Made by Grossley ( Grossley#2869 on Discord )
//Changes:
//Version 01 (November 13th, 2020): Initial release
//Version 02 (April 21st, 2021): Updated for GMS 2.3, general refactor

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));
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

enum EventTypes
{
    Create,
    Destroy,
    Alarm,
    Step,
    Collision,
    Keyboard,
    Mouse,
    Other,
    Draw,
    KeyPress,
    KeyRelease,
    Trigger,
    CleanUp,
    Gesture,
    PreCreate
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
        int file1byte;
        int file2byte;
        FileStream fs1;
        FileStream fs2;

        // Open the two files.
        fs1 = new FileStream(orig_asm_path, FileMode.Open);
        fs2 = new FileStream(new1_asm_path, FileMode.Open);

        // Check the file sizes. If they are not the same, the files
        // are not the same.
        if (fs1.Length != fs2.Length)
        {
            // Close the file
            fs1.Close();
            fs2.Close();
            // Return false to indicate files are different
        }
        else
        {
            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is
            // equal to "file2byte" at this point only if the files are
            // the same.
            if ((file1byte - file2byte) == 0)
            {
                File.Delete(orig_gml_path);
                File.Delete(orig_asm_path);
                File.Delete(new1_asm_path);
                identical_count += 1;
            }
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
        string gmlCode = File.ReadAllText(file);
        UndertaleCode code = Data.Code.ByName(codeName);
        if (Data.Code.ByName(codeName) == null) // Should keep from adding duplicate scripts; haven't tested
        {
            code = new UndertaleCode();
            code.Name = Data.Strings.MakeString(codeName);
            Data.Code.Add(code);
        }
        if ((Data?.GeneralInfo.BytecodeVersion > 14) && (Data.CodeLocals.ByName(codeName) == null))
        {
            UndertaleCodeLocals locals = new UndertaleCodeLocals();
            locals.Name = code.Name;

            UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
            argsLocal.Name = Data.Strings.MakeString("arguments");
            argsLocal.Index = 0;

            locals.Locals.Add(argsLocal);

            code.LocalsCount = 1;
            code.GenerateLocalVarDefinitions(code.FindReferencedLocalVars(), locals); // Dunno if we actually need this line, but it seems to work?
            Data.CodeLocals.Add(locals);
        }
        if (doParse)
        {
            // This portion links code.
            if (codeName.Substring(0, 10).Equals("gml_Script")) 
            {
                // Add code to scripts section.
                if (Data.Scripts.ByName(codeName.Substring(11)) == null)
                {
                    UndertaleScript scr = new UndertaleScript();
                    scr.Name = Data.Strings.MakeString(codeName.Substring(11));
                    scr.Code = code;
                    Data.Scripts.Add(scr);
                }
                else
                {
                    UndertaleScript scr = Data.Scripts.ByName(codeName.Substring(11));
                    scr.Code = code;
                }
            }
            else if (codeName.Substring(0, 16).Equals("gml_GlobalScript"))
            {
                // Add code to global init section.
                UndertaleGlobalInit init_entry = null;
                //This doesn't work, have to do it the hard way: UndertaleGlobalInit init_entry = Data.GlobalInitScripts.ByName(scr_dup_code_name_con);
                foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
                {
                    if (globalInit.Code.Name.Content == codeName)
                    {
                        init_entry = globalInit;
                        break;
                    }
                }
                if (init_entry == null)
                {
                    UndertaleGlobalInit NewInit = new UndertaleGlobalInit();
                    NewInit.Code = code;
                    Data.GlobalInitScripts.Add(NewInit);
                }
                else
                {
                    UndertaleGlobalInit NewInit = init_entry;
                    NewInit.Code = code;
                }
            }
            else if (codeName.Substring(0, 10).Equals("gml_Object"))
            {
                // Add code to object methods.
                string afterPrefix = codeName.Substring(11);
                // Dumb substring stuff, don't mess with this.
                int underCount = 0;
                string methodNumberStr = "", methodName = "", objName = "";
                for (int i = afterPrefix.Length - 1; i >= 0; i--) 
                {
                    if (afterPrefix[i] == '_')
                    {
                        underCount++;
                        if (underCount == 1) 
                        {
                            methodNumberStr = afterPrefix.Substring(i + 1);
                        }
                        else if (underCount == 2)
                        {
                            objName = afterPrefix.Substring(0, i);
                            methodName = afterPrefix.Substring(i + 1, afterPrefix.Length - objName.Length - methodNumberStr.Length - 2);
                            break;
                        }
                    }
                }

                int methodNumber = 0;
                try
                {
                    methodNumber = Int32.Parse(methodNumberStr);
                }
                catch
                {
                    if (afterPrefix.LastIndexOf("_Collision_") != -1)
                    {
                        string s2 = "_Collision_";
                        objName = afterPrefix.Substring(0, (afterPrefix.LastIndexOf("_Collision_")));
                        methodNumberStr = afterPrefix.Substring(afterPrefix.LastIndexOf("_Collision_") + s2.Length, afterPrefix.Length - (afterPrefix.LastIndexOf("_Collision_") + s2.Length));
                        methodName = "Collision";
                        if (Data.GameObjects.ByName(methodNumberStr) != null)
                        {
                            for (var i = 0; i < Data.GameObjects.Count; i++)
                            {
                                if (Data.GameObjects[i].Name.Content == methodNumberStr)
                                {
                                    methodNumber = i;
                                    break;
                                }
                            }
                        }
                    }
                }
                UndertaleGameObject obj = Data.GameObjects.ByName(objName);
                if (obj == null) 
                {
                    bool doNewObj = ScriptQuestion("Object " + objName + " was not found.\nAdd new object called " + objName + "?");
                    if (doNewObj) 
                    {
                        UndertaleGameObject gameObj = new UndertaleGameObject();
                        gameObj.Name = Data.Strings.MakeString(objName);
                        Data.GameObjects.Add(gameObj);
                    }
                    else 
                    {
                        SkipPortions = true;
                    }
                }

                if (!(SkipPortions))
                {
                    obj = Data.GameObjects.ByName(objName);
                    int eventIdx = (int)Enum.Parse(typeof(EventTypes), methodName);
                    bool duplicate = false;
                    try
                    {
                        foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                        {
                            foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                            {
                                if (action.CodeId.Name.Content == codeName)
                                    duplicate = true; 
                            }
                        }
                    }
                    catch
                    {
                        //something went wrong, but probably because it's trying to check something non-existent
                        //we're gonna make it so
                        //keep going
                    }
                    if (duplicate == false)
                    {
                        UndertalePointerList<UndertaleGameObject.Event> eventList = obj.Events[eventIdx];
                        UndertaleGameObject.EventAction action = new UndertaleGameObject.EventAction();
                        UndertaleGameObject.Event evnt = new UndertaleGameObject.Event();

                        action.ActionName = code.Name;
                        action.CodeId = code;
                        evnt.EventSubtype = (uint)methodNumber;
                        evnt.Actions.Add(action);
                        eventList.Add(evnt);
                    }
                }
            }
            // Code which does not match these criteria cannot link, but are still added to the code section.
        }
        SafeImport(codeName, gmlCode, IsGML, file);
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
        try
        {
            File.WriteAllText(path, (IsGML ? GetDecompiledText(code) : GetDisassemblyText(code)));
        }
        catch (Exception e)
        {
            File.WriteAllText(path, "/*\n" + (IsGML ? "DECOMPILER" : "DISASSEMBLY") + " FAILED!\n\n" + e.ToString() + "\n*/");
        }
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

void SafeImport(string codeName, string gmlCode, bool IsGML, string file)
{
    try
    {
        if (IsGML)
        {
            Data.Code.ByName(codeName).ReplaceGML(gmlCode, Data);
        }
        else
        {
            var instructions = Assembler.Assemble(gmlCode, Data);
            Data.Code.ByName(codeName).Replace(instructions);
        }
    }
    catch (Exception ex)
    {
        if (IsGML)
        {
            Data.Code.ByName(codeName).ReplaceGML("", Data);
        }
        else
        {
            using (StreamWriter sw = File.AppendText(GetFolder(FilePath) + "Errors.txt"))
            {
                sw.WriteLine("Assembler error at " + (IsGML ? "GML file: " : "ASM file: ") + file + @"
                
    Code '" + codeName + @"':

    " + gmlCode + @"

    ");
                return;
            }
        }
    }
}

string GetDisassemblyText(UndertaleCode code)
{
    string DisassemblyText = (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : "");
    DisassemblyText = DisassemblyText.Replace("break.e -1", "chkindex.e");
    DisassemblyText = DisassemblyText.Replace("break.e -2", "pushaf.e");
    DisassemblyText = DisassemblyText.Replace("break.e -3", "popaf.e");
    DisassemblyText = DisassemblyText.Replace("break.e -4", "pushac.e");
    DisassemblyText = DisassemblyText.Replace("break.e -5", "setowner.e");
    DisassemblyText = DisassemblyText.Replace("break.e -6", "isstaticok.e");
    DisassemblyText = DisassemblyText.Replace("break.e -7", "setstatic.e");
    return DisassemblyText;
}

string GetDecompiledText(UndertaleCode code)
{
    string DecompiledText = (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : "");
    return DecompiledText;
}
