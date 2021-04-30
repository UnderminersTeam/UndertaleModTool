using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    //Import GML file system.
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
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
        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool CheckDecompiler = false)
        {
            ImportCode(codeName, gmlCode, true, doParse, true, CheckDecompiler);
        }
        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            ImportCode(codeName, gmlCode, false, doParse, destroyASM, CheckDecompiler);
        }
        public void ImportGMLFile(string fileName, bool doParse = true, bool CheckDecompiler = false)
        {
            ImportCodeFromFile(fileName, true, doParse, true, CheckDecompiler);
        }
        public void ImportASMFile(string fileName, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            ImportCodeFromFile(fileName, false, doParse, destroyASM, CheckDecompiler);
        }
        public string GetPassBack(string decompiled_text, string keyword, string replacement, bool case_sensitive = false)
        {
            keyword = keyword.Replace("\r\n", "\n");
            replacement = replacement.Replace("\r\n", "\n");
            string PassBack;
            if (case_sensitive)
                PassBack = decompiled_text.Replace(keyword, replacement);
            else
                PassBack = Regex.Replace(decompiled_text, Regex.Escape(keyword), replacement, RegexOptions.IgnoreCase);
            return PassBack;
        }
        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool case_sensitive = false)
        {
            UndertaleCode code;
            string PassBack;
            EnsureDataLoaded();
            if (Data.Code.ByName(codeName) != null)
                code = Data.Code.ByName(codeName);
            else
            {
                ScriptError("No code named " + codeName + " was found!");
                return;
            }
            if (Data.ProfileMode == false || Data.GMS2_3)
            {
                ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));
                try
                {
                    PassBack = GetPassBack((code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""), keyword, replacement);
                    code.ReplaceGML(PassBack, Data);
                }
                catch (Exception exc)
                {
                    throw new Exception("Error during GML code replacement:\n" + exc.ToString());
                }
            }
            else if ((Data.ProfileMode == true) && (!Data.GMS2_3))
            {
                try
                {
                    string TempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModTool", "Profiles", Data.CurrentMD5);
                    if (File.Exists(Path.Combine(TempPath, codeName + ".gml")))
                    {
                        PassBack = GetPassBack(File.ReadAllText(Path.Combine(TempPath, codeName + ".gml")), keyword, replacement, case_sensitive);
                        File.WriteAllText(Path.Combine(TempPath, codeName + ".gml"), PassBack);
                        code.ReplaceGML(PassBack, Data);
                    }
                    else
                    {
                        ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));
                        try
                        {
                            PassBack = GetPassBack((code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""), keyword, replacement);
                            code.ReplaceGML(PassBack, Data);
                        }
                        catch (Exception exc)
                        {
                            throw new Exception("Error during GML code replacement:\n" + exc.ToString());
                        }
                    }
                }
                catch (Exception exc)
                {
                    throw new Exception("Error during writing of GML code to profile:\n" + exc.ToString());
                }
            }
        }
        void ImportCodeFromFile(string file, bool IsGML = true, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            try
            {
                if (!(Path.GetFileName(file).EndsWith(IsGML ? ".gml" : ".asm")))
                    return;
                if (Path.GetFileName(file).EndsWith("CleanUp_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                    return;
                if (Path.GetFileName(file).EndsWith("PreCreate_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                    return;
                string codeName = Path.GetFileNameWithoutExtension(file);
                string gmlCode = File.ReadAllText(file);
                ImportCode(codeName, gmlCode, IsGML, doParse, destroyASM, CheckDecompiler);
            }
            catch (Exception exc)
            {
                if (!CheckDecompiler)
                    MessageBox.Show("Import" + (IsGML ? "GML" : "ASM") + "File error! Send this to Grossley#2869 and make an issue on Github\n" + exc.ToString());
                else
                    throw new System.Exception("Error!");
            }
        }
        void ImportCode(string codeName, string gmlCode, bool IsGML = true, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            bool SkipPortions = false;
            UndertaleCode code = Data.Code.ByName(codeName);
            if (Data.Code.ByName(codeName) == null)
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
                    string afterPrefix = codeName.Substring(11);
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
                        if (methodName == "Collision" && (methodNumber >= Data.GameObjects.Count || methodNumber < 0))
                        {
                            bool doNewObj = ScriptQuestion("Object of ID " + methodNumber.ToString() + " was not found.\nAdd new object?");
                            if (doNewObj)
                            {
                                UndertaleGameObject gameObj = new UndertaleGameObject();
                                gameObj.Name = Data.Strings.MakeString(SimpleTextInput("Enter object name", "Enter object name", "This is a single text line input box test.", false));
                                Data.GameObjects.Add(gameObj);
                            }
                            else
                            {
                                //It *needs* to have a valid value, make the user specify one.
                                List<uint> possible_values = new List<uint>();
                                possible_values.Add(999999);
                                methodNumber = (int)ReduceCollisionValue(possible_values);
                            }
                        }
                    }
                    catch
                    {
                        if (afterPrefix.LastIndexOf("_Collision_") != -1)
                        {
                            string s2 = "_Collision_";
                            objName = afterPrefix.Substring(0, (afterPrefix.LastIndexOf("_Collision_")));
                            methodNumberStr = afterPrefix.Substring(afterPrefix.LastIndexOf("_Collision_") + s2.Length, afterPrefix.Length - (afterPrefix.LastIndexOf("_Collision_") + s2.Length));
                            methodName = "Collision";
                            //GMS 2.3+ use the object name for the one colliding, which is rather useful.
                            if (Data.GMS2_3)
                            {
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
                                else
                                {
                                    bool doNewObj = ScriptQuestion("Object " + objName + " was not found.\nAdd new object called " + objName + "?");
                                    if (doNewObj)
                                    {
                                        UndertaleGameObject gameObj = new UndertaleGameObject();
                                        gameObj.Name = Data.Strings.MakeString(objName);
                                        Data.GameObjects.Add(gameObj);
                                    }
                                }
                                if (Data.GameObjects.ByName(methodNumberStr) != null)
                                {
                                    //It *needs* to have a valid value, make the user specify one, silly.
                                    List<uint> possible_values = new List<uint>();
                                    possible_values.Add(999999);
                                    ReassignGUIDs(methodNumberStr, ReduceCollisionValue(possible_values));
                                }
                            }
                            else
                            {
                                //Lets try to get this going
                                methodNumber = (int)ReduceCollisionValue(GetCollisionValueFromCodeNameGUID(codeName));
                                ReassignGUIDs(methodNumberStr, ReduceCollisionValue(GetCollisionValueFromCodeNameGUID(codeName)));
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
            }
            SafeImport(codeName, gmlCode, IsGML, destroyASM, CheckDecompiler);
        }
        void SafeImport(string codeName, string gmlCode, bool IsGML, bool destroyASM = true, bool CheckDecompiler = false)
        {
            try
            {
                string GMLPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModTool", "Profiles", Data.CurrentMD5, "Temp");
                if (IsGML)
                {
                    Data.Code.ByName(codeName).ReplaceGML(gmlCode, Data);
                    if (File.Exists(Path.Combine(GMLPath, codeName + ".gml")))
                    {
                        File.WriteAllText(GetDecompiledText(codeName), Path.Combine(GMLPath, codeName + ".gml"));
                    }
                }
                else
                {
                    var instructions = Assembler.Assemble(gmlCode, Data);
                    Data.Code.ByName(codeName).Replace(instructions);
                    if (File.Exists(Path.Combine(GMLPath, codeName + ".gml")) && destroyASM)
                    {
                        File.Delete(Path.Combine(GMLPath, codeName + ".gml"));
                        File.WriteAllText(GetDecompiledText(codeName), Path.Combine(GMLPath, codeName + ".gml"));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!CheckDecompiler)
                {
                    string ErrorText = "Error at " + (IsGML ? "GML code: " : "ASM code: ") + codeName + @"': " + gmlCode + "\nError: " + ex.ToString();
                    MessageBox.Show(ErrorText, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                    throw new System.Exception("Error!");
            }
        }
    }
}

