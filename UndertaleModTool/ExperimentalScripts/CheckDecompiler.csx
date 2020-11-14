using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

//Made by Grossley ( Grossley#2869 on Discord )
//Changes:
//Version 01 (November 13th, 2020): Initial release

ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));
if (Data?.GeneralInfo.BytecodeVersion < 15)
{
    ScriptError("This script will not work properly on Undertale 1.0 and other bytecode < 15 games.", "Error");
    return;
}

if (Directory.Exists(GetFolder(FilePath) + "Export_Code_Orig" + Path.DirectorySeparatorChar)) 
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}
else
{
    Directory.CreateDirectory(GetFolder(FilePath) + "Export_Code_Orig" + Path.DirectorySeparatorChar);
}
if (Directory.Exists(GetFolder(FilePath) + "Export_Assembly_Orig" + Path.DirectorySeparatorChar)) 
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}
else
{
    Directory.CreateDirectory(GetFolder(FilePath) + "Export_Assembly_Orig" + Path.DirectorySeparatorChar);
}
if (Directory.Exists(GetFolder(FilePath) + "Export_Assembly_Recompiled" + Path.DirectorySeparatorChar)) 
{
    ScriptError("A code export already exists. Please remove it.", "Error");
    return;
}
else
{
    Directory.CreateDirectory(GetFolder(FilePath) + "Export_Assembly_Recompiled" + Path.DirectorySeparatorChar);
}

int progress = 0;
int identical_count = 0;
UpdateProgress();
await DumpCodeOrig();
progress = 0;
UpdateProgress();
string codeFolder = GetFolder(FilePath) + "Export_Assembly_Orig" + Path.DirectorySeparatorChar;
await DumpCode();
progress = 0;
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
    Gesture,
    Asynchronous,
    PreCreate
}
string importFolder = GetFolder(FilePath) + "Export_Code_Orig" + Path.DirectorySeparatorChar;
bool doParse = true;
string[] dirFiles = Directory.GetFiles(importFolder);
foreach (string file in dirFiles) 
{
    UpdateProgressBar(null, "Import Files", progress++, dirFiles.Length);

    string fileName = Path.GetFileName(file);
    if (!fileName.EndsWith(".gml") || !fileName.Contains("_")) // Perhaps drop the underscore check?
        continue; // Restarts loop if file is not a valid code asset.
    if (fileName.EndsWith("PreCreate_0.gml") && (Data.GeneralInfo.Major < 2))
        continue; // Restarts loop if file is not a valid code asset.

    string gmlCode = File.ReadAllText(file);
    string codeName = Path.GetFileNameWithoutExtension(file);
    if (Data.Code.ByName(codeName) == null) // Should keep from adding duplicate scripts; haven't tested
    {
        UndertaleCode code = new UndertaleCode();
        code.Name = Data.Strings.MakeString(codeName);
        Data.Code.Add(code);

        UndertaleCodeLocals locals = new UndertaleCodeLocals();
        locals.Name = code.Name;

        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
        argsLocal.Name = Data.Strings.MakeString("arguments");
        argsLocal.Index = 0;

        locals.Locals.Add(argsLocal);

        code.LocalsCount = 1;
        code.GenerateLocalVarDefinitions(code.FindReferencedLocalVars(), locals); // Dunno if we actually need this line, but it seems to work?
        Data.CodeLocals.Add(locals);

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
                        } else if (underCount == 2)
                        {
                            objName = afterPrefix.Substring(0, i);
                            methodName = afterPrefix.Substring(i + 1, afterPrefix.Length - objName.Length - methodNumberStr.Length - 2);
                            break;
                        }
                    }
                }

                int methodNumber = Int32.Parse(methodNumberStr);
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
                        Data.Code.ByName(codeName).ReplaceGML(gmlCode, Data);
                        continue;
                    }
                }

                obj = Data.GameObjects.ByName(objName);
                int eventIdx = (int)Enum.Parse(typeof(EventTypes), methodName);

                UndertalePointerList<UndertaleGameObject.Event> eventList = obj.Events[eventIdx];
                UndertaleGameObject.EventAction action = new UndertaleGameObject.EventAction();
                UndertaleGameObject.Event evnt = new UndertaleGameObject.Event();

                action.ActionName = code.Name;
                action.CodeId = code;
                evnt.EventSubtype = (uint)methodNumber;
                evnt.Actions.Add(action);
                eventList.Add(evnt);
            }
            // Code which does not match these criteria cannot link, but are still added to the code section.
        }
    }
    Data.Code.ByName(codeName).ReplaceGML(gmlCode, Data);
}
progress = 0;
UpdateProgress();
codeFolder = GetFolder(FilePath) + "Export_Assembly_Recompiled" + Path.DirectorySeparatorChar;
await DumpCode();
progress = 0;
UpdateProgress();
await FileCompare();
// Check code directory.
importFolder = GetFolder(FilePath) + "Export_Assembly_Orig" + Path.DirectorySeparatorChar;
doParse = true;
progress = 0;
dirFiles = Directory.GetFiles(importFolder);
foreach (string file in dirFiles) 
{
    UpdateProgressBar(null, "Reapply original ASM files", progress++, dirFiles.Length);
    string fileName = Path.GetFileName(file);
    if (!fileName.EndsWith(".asm") || !fileName.Contains("_")) // Perhaps drop the underscore check?
        continue; // Restarts loop if file is not a valid code asset.
    if (fileName.EndsWith("PreCreate_0.asm") && (Data.GeneralInfo.Major < 2))
        continue; // Restarts loop if file is not a valid code asset.
    string asmCode = File.ReadAllText(file);
    string codeName = Path.GetFileNameWithoutExtension(file);
    if (Data.Code.ByName(codeName) == null) // Should keep from adding duplicate scripts; haven't tested
    {
        UndertaleCode code = new UndertaleCode();
        code.Name = Data.Strings.MakeString(codeName);
        Data.Code.Add(code);
        UndertaleCodeLocals locals = new UndertaleCodeLocals();
        locals.Name = code.Name;
        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
        argsLocal.Name = Data.Strings.MakeString("arguments");
        argsLocal.Index = 0;
        locals.Locals.Add(argsLocal);
        code.LocalsCount = 1;
        code.GenerateLocalVarDefinitions(code.FindReferencedLocalVars(), locals); // Dunno if we actually need this line, but it seems to work?
        Data.CodeLocals.Add(locals);
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
                        } else if (underCount == 2) 
                        {
                            objName = afterPrefix.Substring(0, i);
                            methodName = afterPrefix.Substring(i + 1, afterPrefix.Length - objName.Length - methodNumberStr.Length - 2);
                            break;
                        }
                    }
                }
                int methodNumber = Int32.Parse(methodNumberStr);
                UndertaleGameObject obj = Data.GameObjects.ByName(objName);
                if (obj == null) 
                {
                    bool doNewObj = ScriptQuestion("Object " + objName + " was not found.\nAdd new object called " + objName + "?");
                    if (doNewObj) 
                    {
                        UndertaleGameObject gameObj = new UndertaleGameObject();
                        gameObj.Name = Data.Strings.MakeString(objName);
                        Data.GameObjects.Add(gameObj);
                    } else 
                    {
                        try
                        {
                            var instructions = Assembler.Assemble(asmCode, Data);
                            Data.Code.ByName(codeName).Replace(instructions);
                        }
                        catch(Exception ex)
                        {
                            ScriptMessage("Assembler error at file: " + codeName);
                            return;
                        }
                        continue;
                    }
                }
                obj = Data.GameObjects.ByName(objName);
                int eventIdx = (int)Enum.Parse(typeof(EventTypes), methodName);
                UndertalePointerList<UndertaleGameObject.Event> eventList = obj.Events[eventIdx];
                UndertaleGameObject.EventAction action = new UndertaleGameObject.EventAction();
                UndertaleGameObject.Event evnt = new UndertaleGameObject.Event();
                action.ActionName = code.Name;
                action.CodeId = code;
                evnt.EventSubtype = (uint)methodNumber;
                evnt.Actions.Add(action);
                eventList.Add(evnt);
            }
            // Code which does not match these criteria cannot linked, but are still added to the code section.
        }
    }
    try
    {
        var instructions = Assembler.Assemble(asmCode, Data);
        Data.Code.ByName(codeName).Replace(instructions);
    }
    catch(Exception ex)
    {
        ScriptMessage("Assembler error at code: " + codeName);
        return;
    }
}
HideProgressBar();
double percentage = ((double)identical_count/(double)Data.Code.Count)*100;
int non_matching = Data.Code.Count - identical_count;
ScriptMessage("Non-matching Data Generated. Decompiler/Compiler Accuracy: " + percentage.ToString() + "% (" + identical_count.ToString() + "/" + Data.Code.Count.ToString() + "). Number of differences: " + non_matching.ToString() + ". To review these, the differing files are in the game directory.");

async Task DumpCodeOrig() 
{
    await Task.Run(() => Parallel.ForEach(Data.Code, DumpCodeOrig));
}

void DumpCodeOrig(UndertaleCode code)
{
    string path = Path.Combine(GetFolder(FilePath) + "Export_Code_Orig" + Path.DirectorySeparatorChar, code.Name.Content + ".gml");
    try 
    {
        File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
    }
    catch (Exception e) 
    {
        File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
    }
    UpdateProgressBar(null, "Dumping Original Code Entries", progress++, Data.Code.Count);
}

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

async Task DumpCode()
{
    await Task.Run(() => Parallel.ForEach(Data.Code, DumpCode));
}

void DumpCode(UndertaleCode code) 
{
    string path = Path.Combine(codeFolder, code.Name.Content + ".asm");
    try 
    {
        File.WriteAllText(path, (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : ""));
    }
    catch (Exception e) 
    {
        File.WriteAllText(path, "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
    }
    UpdateProgressBar(null, "Dumping Code Disassembly", progress++, Data.Code.Count);
}

async Task FileCompare()
{
    await Task.Run(() => Parallel.ForEach(Data.Code, FileCompare));
}

void FileCompare(UndertaleCode code)
{
    UpdateProgressBar(null, "Deleting identical files", progress++, Data.Code.Count);
    string orig_gml_path = GetFolder(FilePath) + "Export_Code_Orig" + Path.DirectorySeparatorChar + code.Name.Content + ".gml";
    string orig_asm_path = GetFolder(FilePath) + "Export_Assembly_Orig" + Path.DirectorySeparatorChar + code.Name.Content + ".asm";
    string new1_asm_path = GetFolder(FilePath) + "Export_Assembly_Recompiled" + Path.DirectorySeparatorChar + code.Name.Content + ".asm";
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
