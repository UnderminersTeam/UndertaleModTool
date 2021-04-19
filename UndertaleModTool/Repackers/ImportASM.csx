// Script by Jockeholm based off of a script by Kneesnap.

// Major help and edited by Samuel Roy

using System;
using System.IO;
using UndertaleModLib.Util;

EnsureDataLoaded();

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
    throw new System.Exception("The import folder was not set.");

// Ask whether they want to link code. If no, will only generate code entry.
// If yes, will try to add code to objects and scripts depending upon its name
bool doParse = ScriptQuestion("Do you want to automatically attempt to link imported code?");

int progress = 0;
string[] dirFiles = Directory.GetFiles(importFolder);
foreach (string file in dirFiles) 
{
    UpdateProgressBar(null, "Files", progress++, dirFiles.Length);
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
ScriptMessage("All files successfully imported.");