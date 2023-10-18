using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    // System for importing GML files with more ease than other functions
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

        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool checkDecompiler = false)
        {
            ImportCode(codeName, gmlCode, true, doParse, true, checkDecompiler);
        }

        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool nukeProfile = true, bool checkDecompiler = false)
        {
            ImportCode(codeName, gmlCode, false, doParse, nukeProfile, checkDecompiler);
        }

        public void ImportGMLFile(string fileName, bool doParse = true, bool checkDecompiler = false, bool throwOnError = false)
        {
            ImportCodeFromFile(fileName, true, doParse, true, checkDecompiler, throwOnError);
        }

        public void ImportASMFile(string fileName, bool doParse = true, bool nukeProfile = true, bool checkDecompiler = false, bool throwOnError = false)
        {
            ImportCodeFromFile(fileName, false, doParse, nukeProfile, checkDecompiler, throwOnError);
        }

        public void NukeProfileGML(string codeName)
        {
            // This is written as intended
            string path = Path.Combine(ProfilesFolder, Data.ToolInfo.CurrentMD5, "Temp", codeName + ".gml");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        public void ReapplyProfileCode()
        {
            foreach (UndertaleCode code in Data.Code)
            {
                string path = Path.Combine(ProfilesFolder, Data.ToolInfo.CurrentMD5, "Temp", code.Name.Content + ".gml");
                if (File.Exists(path))
                {
                    ImportGMLFile(path, false, false);
                }
            }
        }

        public string GetPassBack(string decompiled_text, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false)
        {
            keyword = keyword.Replace("\r\n", "\n");
            replacement = replacement.Replace("\r\n", "\n");
            string passBack;
            if (!isRegex)
            {
                if (case_sensitive)
                    passBack = decompiled_text.Replace(keyword, replacement);
                else
                    passBack = Regex.Replace(decompiled_text, Regex.Escape(keyword), replacement.Replace("$", "$$"), RegexOptions.IgnoreCase);
            }
            else
            {
                if (case_sensitive)
                    passBack = Regex.Replace(decompiled_text, keyword, replacement, RegexOptions.None);
                else
                    passBack = Regex.Replace(decompiled_text, keyword, replacement, RegexOptions.IgnoreCase);
            }
            return passBack;
        }

        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            UndertaleCode code = Data.Code.ByName(codeName);
            if (code is null)
                throw new ScriptException($"No code named \"{codeName}\" was found!");

            ReplaceTextInGML(code, keyword, replacement, caseSensitive, isRegex, context);
        }
        public void ReplaceTextInGML(UndertaleCode code, string keyword, string replacement, bool caseSensitive = false, bool isRegex = false, GlobalDecompileContext context = null)
        {
            if (code.ParentEntry is not null)
                return;

            EnsureDataLoaded();

            string passBack = "";
            string codeName = code.Name.Content;
            GlobalDecompileContext DECOMPILE_CONTEXT = context is null ? new(Data, false) : context;

            if (!Data.ToolInfo.ProfileMode)
            {
                try
                {
                    passBack = GetPassBack((code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT ) : ""), keyword, replacement, caseSensitive, isRegex);
                    code.ReplaceGML(passBack, Data);
                }
                catch (Exception exc)
                {
                    throw new Exception("Error during GML code replacement:\n" + exc.ToString());
                }
            }
            else
            {
                try
                {
                    string path = Path.Combine(ProfilesFolder, Data.ToolInfo.CurrentMD5, "Temp", codeName + ".gml");
                    if (File.Exists(path))
                    {
                        passBack = GetPassBack(File.ReadAllText(path), keyword, replacement, caseSensitive, isRegex);
                        File.WriteAllText(path, passBack);
                        code.ReplaceGML(passBack, Data);
                    }
                    else
                    {
                        try
                        {
                            if (context is null)
                                passBack = GetPassBack((code != null ? Decompiler.Decompile(code, new GlobalDecompileContext(Data, false)) : ""), keyword, replacement, caseSensitive, isRegex);
                            else
                                passBack = GetPassBack((code != null ? Decompiler.Decompile(code, context) : ""), keyword, replacement, caseSensitive, isRegex);
                            code.ReplaceGML(passBack, Data);
                        }
                        catch (Exception exc)
                        {
                            throw new Exception("Error during GML code replacement:\n" + exc.ToString());
                        }
                    }
                }
                catch (Exception exc)
                {
                    throw new Exception("Error during writing of GML code to profile:\n" + exc.ToString() + "\n\nCode:\n\n" + passBack);
                }
            }
        }

        void ImportCodeFromFile(string file, bool IsGML = true, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            try
            {
                if (!Path.GetFileName(file).ToLower().EndsWith(IsGML ? ".gml" : ".asm"))
                    return;
                if (Path.GetFileName(file).ToLower().EndsWith("cleanup_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                    return;
                if (Path.GetFileName(file).ToLower().EndsWith("precreate_0" + (IsGML ? ".gml" : ".asm")) && (Data.GeneralInfo.Major < 2))
                    return;
                string codeName = Path.GetFileNameWithoutExtension(file);
                string gmlCode = File.ReadAllText(file);
                ImportCode(codeName, gmlCode, IsGML, doParse, destroyASM, CheckDecompiler, throwOnError);
            }
            catch (ScriptException exc) when (throwOnError && exc.Message == "*codeImportError*")
            {
                throw new ScriptException("Code files importation stopped because of error(s).");
            }
            catch (Exception exc)
            {
                if (!CheckDecompiler)
                {
                    this.ShowError("Import" + (IsGML ? "GML" : "ASM") + "File error! Send the following error to Grossley#2869 (Discord) and make an issue on Github:\n\n" + exc.ToString());

                    if (throwOnError)
                        throw new ScriptException("Code files importation stopped because of error(s).");
                }
                else
                    throw new Exception("Error!");
            }
        }

        /// <summary>
        /// The type of the GML code being imported
        /// </summary>
        public enum ImportCodeType
        {
            /// <summary>
            /// Literal means that what is being imported is exactly what will be in the code after it is imported, thus, if this code already existed it will be completely replaced
            /// </summary>
            Literal,
            /// <summary>
            /// Modified code follows the shorthand syntax for replacing and inserting code into existing code
            /// </summary>
            Modified
        }

        /// <summary>
        /// The type of command used in "modified" imported code
        /// </summary>
        public enum ModifiedCommandType
        {
            /// <summary>
            /// Default for when no command is currently found
            /// </summary>
            None,
            /// <summary>
            /// After command takes original code and then places new code after it
            /// </summary>
            After,
            /// <summary>
            /// Replace command takes original code and replaces it with new code
            /// </summary>
            Replace,
            /// <summary>
            /// Insert command takes a number and inserts new code AFTER that line number (0 would mean it'd be the first line, and so forth)
            /// </summary>
            Insert,
            /// <summary>
            /// Append command takes new code and add it to the end of the file
            /// </summary>
            Append,
            /// <summary>
            /// Prepend command takes new code and add it to the start of the file
            /// </summary>
            Prepend
        }

        /// <summary>
        /// Exception thrown when an unknown command is found in "modified" imported code
        /// </summary>
        public class ModifiedCommandException : Exception
        {
            public ModifiedCommandException(string line) : base("Unknown command in modified code: " + line) { }
        }

        void ImportCode(string codeName, string gmlCode, bool IsGML = true, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            ImportCodeType codeType = gmlCode.StartsWith("/// MODIFIED") ? ImportCodeType.Modified : ImportCodeType.Literal;
            bool SkipPortions = false;
            UndertaleCode code = Data.Code.ByName(codeName);
            if (code is null)
            {
                if (codeType == ImportCodeType.Modified)
                {
                    throw new Exception("Modified code cannot be imported without a base code to modify.");
                }
                code = new UndertaleCode();
                code.Name = Data.Strings.MakeString(codeName);
                Data.Code.Add(code);
            }
            else if (code.ParentEntry is not null)
                return;
            // apply all "modified" commands
            else if (codeType == ImportCodeType.Modified)
            {
                if (Data.KnownSubFunctions is null) Decompiler.BuildSubFunctionCache(Data);
                string oldCode = Decompiler.Decompile(code, new GlobalDecompileContext(Data, false));
                
                // ignoring the first line, since it only contains the MODIFIED tag
                gmlCode = gmlCode.Substring(gmlCode.IndexOf('\n') + 1);
                string[] lines = gmlCode.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                // these two variables keep track of the text in the current command block, with original being the text in the original code and new being the text to be placed/replaced
                List<string> originalText = new();
                List<string> newText = new();

                // if the method requires an original and new text entry, this signifies we are in the first entry
                // otherwise it is irrelevant
                bool inOriginalText = true;
                
                // only relevant if the current command is INSERT
                int insertArgument = 0;

                ModifiedCommandType currentCommand = ModifiedCommandType.None;

                foreach (string line in lines)
                {
                    if (currentCommand != ModifiedCommandType.None)
                    {
                        if (line.StartsWith("///"))
                        {
                            if (Regex.IsMatch(line, @"\bCODE\b"))
                            {
                                inOriginalText = false;
                            }
                            else if (Regex.IsMatch(line, @"\bEND\b"))
                            {
                                inOriginalText = true;
                                string originalTextString = string.Join("\n", originalText);
                                string newTextString = string.Join("\n", newText);
                                switch (currentCommand)
                                {
                                    case ModifiedCommandType.After:
                                        int placeIndex = oldCode.IndexOf(originalTextString) + originalTextString.Length;
                                        oldCode = oldCode.Insert(placeIndex, "\n" + newTextString);
                                        break;
                                    case ModifiedCommandType.Replace:
                                        oldCode = oldCode.Replace(originalTextString, newTextString);
                                        break;
                                    case ModifiedCommandType.Insert:
                                        List<string> oldCodeLines = oldCode.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
                                        oldCodeLines.Insert(insertArgument, newTextString);
                                        oldCode = string.Join("\n", oldCodeLines);
                                        break;
                                    case ModifiedCommandType.Append:    
                                        oldCode = oldCode + "\n" + newTextString;
                                        break;
                                    case ModifiedCommandType.Prepend:
                                        oldCode = newTextString + "\n" + oldCode;
                                        break;
                                }
                                currentCommand = ModifiedCommandType.None;
                                newText = new List<string>();
                                originalText = new List<string>();
                            }
                            else
                            {
                                throw new ModifiedCommandException(line);
                            }
                        }
                        else
                        {
                            if (inOriginalText)
                            {
                                originalText.Add(line);
                            }
                            else
                            {
                                newText.Add(line);
                            }
                        }
                    }
                    else
                    {
                        if (line.StartsWith("///"))
                        {
                            if (Regex.IsMatch(line, @"\bAFTER\b"))
                            {
                                currentCommand = ModifiedCommandType.After;
                            }
                            else if (Regex.IsMatch(line, @"\bREPLACE\b"))
                            {
                                currentCommand = ModifiedCommandType.Replace;
                            }
                            else if (Regex.IsMatch(line, @"\bINSERT\b"))
                            {
                                inOriginalText = false;
                                currentCommand = ModifiedCommandType.Insert;
                                Match argMatch = Regex.Match(line, @"(?<=\bINSERT\b\s*)\d+");
                                if (argMatch.Value == "")
                                {
                                    throw new Exception("INSERT modified command requires a number after it.");
                                }
                                insertArgument = int.Parse(argMatch.Value);
                            }
                            else if (Regex.IsMatch(line, @"\bAPPEND\b"))
                            {
                                inOriginalText = false;
                                currentCommand = ModifiedCommandType.Append;
                            }
                            else if (Regex.IsMatch(line, @"\bPREPEND\b"))
                            {
                                inOriginalText = false;
                                currentCommand = ModifiedCommandType.Prepend;
                            }
                            else
                            {
                                throw new ModifiedCommandException(line);
                            }
                        }
                    }
                }
                gmlCode = oldCode;
            }

            if (Data?.GeneralInfo.BytecodeVersion > 14 && Data.CodeLocals.ByName(codeName) == null)
            {
                UndertaleCodeLocals locals = new UndertaleCodeLocals();
                locals.Name = code.Name;

                UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                argsLocal.Name = Data.Strings.MakeString("arguments");
                argsLocal.Index = 0;

                locals.Locals.Add(argsLocal);

                code.LocalsCount = 1;
                Data.CodeLocals.Add(locals);
            }
            if (doParse)
            {
                // This portion links code.
                if (codeName.StartsWith("gml_Script"))
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
                else if (codeName.StartsWith("gml_GlobalScript"))
                {
                    // Add code to global init section.
                    UndertaleGlobalInit init_entry = null;
                    // This doesn't work, have to do it the hard way: UndertaleGlobalInit init_entry = Data.GlobalInitScripts.ByName(scr_dup_code_name_con);
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
                else if (codeName.StartsWith("gml_Object"))
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
                        methodNumber = int.Parse(methodNumberStr);
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
                                // It *needs* to have a valid value, make the user specify one.
                                List<uint> possible_values = new List<uint>();
                                possible_values.Add(uint.MaxValue);
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
                            // GMS 2.3+ use the object name for the one colliding, which is rather useful.
                            if (Data.IsVersionAtLeast(2, 3))
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
                                    // It *needs* to have a valid value, make the user specify one, silly.
                                    List<uint> possible_values = new List<uint>();
                                    possible_values.Add(uint.MaxValue);
                                    ReassignGUIDs(methodNumberStr, ReduceCollisionValue(possible_values));
                                }
                            }
                            else
                            {
                                // Let's try to get this going
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
                                    if (action.CodeId?.Name?.Content == codeName)
                                        duplicate = true;
                                }
                            }
                        }
                        catch
                        {
                            // Something went wrong, but probably because it's trying to check something non-existent
                            // Just keep going
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
            SafeImport(codeName, gmlCode, IsGML, destroyASM, CheckDecompiler, throwOnError);
        }

        void SafeImport(string codeName, string gmlCode, bool IsGML, bool destroyASM = true, bool CheckDecompiler = false, bool throwOnError = false)
        {
            UndertaleCode code = Data.Code.ByName(codeName);
            if (code?.ParentEntry is not null)
                return;

            try
            {
                if (IsGML)
                {
                    code.ReplaceGML(gmlCode, Data);

                    // Write to profile if necessary.
                    string path = Path.Combine(ProfilesFolder, Data.ToolInfo.CurrentMD5, "Temp", codeName + ".gml");
                    if (File.Exists(path))
                        File.WriteAllText(path, GetDecompiledText(code));
                }
                else
                {
                    var instructions = Assembler.Assemble(gmlCode, Data);
                    code.Replace(instructions);
                    if (destroyASM)
                        NukeProfileGML(codeName);
                }
            }
            catch (Exception ex)
            {
                if (!CheckDecompiler)
                {
                    string errorText = $"Code import error at {(IsGML ? "GML" : "ASM")} code \"{codeName}\":\n\n{ex.Message}";
                    this.ShowWarning(errorText);

                    if (throwOnError)
                        throw new ScriptException("*codeImportError*");
                }
                else
                {
                    code.ReplaceGML("", Data);
                }
            }
        }
    }
}