// Script by Jockeholm based off of a script by Kneesnap.
// Major help and edited by Samuel Roy

using System;
using System.IO;
using UndertaleModLib.Util;

EnsureDataLoaded();

bool is2_3 = false;
if (!((Data.GMS2_3 == false) && (Data.GMS2_3_1 == false) && (Data.GMS2_3_2 == false)))
{
    is2_3 = true;
    ScriptMessage("This script is for GMS 2.3 games code from \"ExportAllCode2_3.csx\", because some code names get so long that Windows cannot write them adequately.");
}
else
{
    ScriptError("Use the regular ImportGML please!", "Incompatible");
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
    Gesture,
    Asynchronous,
    PreCreate
}

// Check code directory.
string importFolder = PromptChooseDirectory("Import From Where");
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

List<string> CodeList = new List<string>();

if (File.Exists(importFolder + "/LookUpTable.txt"))
{
    int counter = 0;  
    string line;  
    System.IO.StreamReader file = new System.IO.StreamReader(importFolder + "/" + "LookUpTable.txt");  
    while((line = file.ReadLine()) != null)
    {
        if (counter > 0)
            CodeList.Add(line);
        counter++;
    }
    file.Close();
}
else
{
    ScriptError("No LookUpTable.txt!", "Error");
    return;
}

// Ask whether they want to link code. If no, will only generate code entry.
// If yes, will try to add code to objects and scripts depending upon its name
bool doParse = ScriptQuestion("Do you want to automatically attempt to link imported code?");

string[] dirFiles = Directory.GetFiles(importFolder);
bool skipGlobalScripts = true;
bool skipGlobalScriptsPrompted = false;

SetProgressBar(null, "Files", 0, dirFiles.Length);
StartUpdater();

SyncBinding("Strings, Code, CodeLocals, Scripts, GlobalInitScripts, GameObjects", true); //not sure about GlobalInitScripts
await Task.Run(() => {
    foreach (string file in dirFiles)
    {
        IncProgress();

        string fileName = Path.GetFileName(file);
        if (!(fileName.EndsWith(".gml")))
            continue;
        fileName = Path.GetFileNameWithoutExtension(file);
        int number;
        bool success = Int32.TryParse(fileName, out number);
        string codeName;
        if (success)
        {
            codeName = CodeList[number];
            fileName = codeName + ".gml";
        }
        else
        {
            ScriptError("GML file not in range of look up table!", "Error");
            return;
        }
        if (fileName.EndsWith("PreCreate_0.gml") && (Data.GeneralInfo.Major < 2))
            continue; // Restarts loop if file is not a valid code asset.
        string gmlCode = File.ReadAllText(file);
        if (codeName.Substring(0, 17).Equals("gml_GlobalScript_") && is2_3 && (!(skipGlobalScriptsPrompted)))
        {
            skipGlobalScriptsPrompted = true;
            skipGlobalScripts = ScriptQuestion("Skip global scripts parsing?");
        }
        if (codeName.Substring(0, 17).Equals("gml_GlobalScript_") && is2_3 && ((skipGlobalScriptsPrompted)))
        {
            if (skipGlobalScripts)
                continue;
        }
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
                        try
                        {
                            Data.Code.ByName(codeName).ReplaceGML(gmlCode, Data);
                        }
                        catch (Exception ex)
                        {
                            string errorMSG = "Error in " + codeName + ":\r\n" + ex.ToString() + "\r\nAborted";
                            ScriptMessage(errorMSG);
                            SetUMTConsoleText(errorMSG);
                            SetFinishedMessage(false);
                            return;
                        }
                        continue;
                    }
                }

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
            // Code which does not match these criteria cannot link, but are still added to the code section.
        }
        else
        {
            try
            {
                Data.Code.ByName(codeName).ReplaceGML(gmlCode, Data);
            }
            catch (Exception ex)
            {
                string errorMSG = "Error in " + codeName + ":\r\n" + ex.ToString() + "\r\nAborted";
                ScriptMessage(errorMSG);
                SetUMTConsoleText(errorMSG);
                SetFinishedMessage(false);
                return;
            }
        }
    }
});
SyncBinding(false);

await StopUpdater();
HideProgressBar();
ScriptMessage("All files successfully imported.");