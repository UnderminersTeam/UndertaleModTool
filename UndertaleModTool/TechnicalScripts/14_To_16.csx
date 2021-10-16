using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;

// By Grossley, with the help of Colinator and Creepersbane

DoLongErrorMessages(false);
EnsureDataLoaded();

string currentBytecodeVersion = Data?.GeneralInfo.BytecodeVersion.ToString();
string game_name = Data.GeneralInfo.Name.Content;
bool TriggerOnce = false;

if (!(Data?.GeneralInfo.BytecodeVersion == 14))
{
    ScriptError("Invalid!", "Invalid!");
    return;
}
if (Data?.GeneralInfo.BytecodeVersion == 14)
{
    ScriptMessage(@"This script is experimental, please be advised.
Use at your own risk. Unexpected issues may occur.
If they do, you may file an issue on GitHub, but no warranty is given."); // Warning
    if (!ScriptQuestion("Upgrade bytecode from 14 to 16?\nCurrent bytecode: " + currentBytecodeVersion))
    {
        ScriptMessage("Cancelled.");
        return;
    }
    // Convert variables
    int id = 0;
    UndertaleVariable variable;
    BuiltinList list = new BuiltinList();
    list.Initialize(Data);
    foreach (var code in Data.Code)
    {
        for (int j = 0; j < code.Instructions.Count; j++)
        {
            var curr = code.Instructions[j];
            if (curr.Destination != null)// && curr.Destination.Type == UndertaleInstruction.VariableType.Array)
            {
                List<UndertaleInstruction> before = code.Instructions.Take(j).ToList();
                before.Reverse();
                UndertaleInstruction.InstanceType type = UndertaleInstruction.InstanceType.Self;
                int stackCounter = (curr.Kind == UndertaleInstruction.Opcode.Pop && curr.Type1 == UndertaleInstruction.DataType.Int32 ? 3 : 2);
                foreach (var i in before)
                {
                    if (stackCounter == 1) // This needs to be here because otherwise sth[aaa].another[bbb] doesn't work (damn this workaround is getting crazy, CHAOS, CHAOS)
                    {
                        if (i.Kind == UndertaleInstruction.Opcode.Push)
                        {
                            type = UndertaleInstruction.InstanceType.Self; // This is probably an instance variable then (e.g. pushi.e 1337; push.v self.someinstance; conv.v.i; pushi.e 0; pop.v.v [array]alarm)
                            break;
                        }
                        else if (i.Kind == UndertaleInstruction.Opcode.PushLoc)
                        {
                            type = UndertaleInstruction.InstanceType.Local;
                            break;
                        }
                    }
                    //int old = stackCounter;
                    stackCounter -= CalculateStackDiff(i);
                    //Debug.WriteLine(i.ToString() + "; " + old + " -> " + stackCounter);
                    if (stackCounter == 0)
                    {
                        if (i.Kind == UndertaleInstruction.Opcode.PushI)
                        {
                            type = (UndertaleInstruction.InstanceType)(short)i.Value;
                            break;
                        }
                        else if (i.Kind == UndertaleInstruction.Opcode.Dup)
                            stackCounter += 1 + i.Extra; // Keep looking for the value that was duplicated
                        else
                        {
                            //throw new Exception("My workaround still sucks " + code.Name.Content + " " + j);
                        }
                    }
                }
                // Do what you want to with `type` here
                variable = curr.Destination?.Target;
                if(variable != null && true)
                {
                    if (curr.Destination.Type == UndertaleInstruction.VariableType.StackTop && curr.Destination.Target.InstanceType == UndertaleInstruction.InstanceType.Undefined)
                    {
                        curr.Destination.Target.InstanceType = UndertaleInstruction.InstanceType.Self;
                    }
                    if (list.GlobalNotArray.ContainsKey(curr.Destination?.Target.Name.Content))
                    {
                        if (curr.Kind == UndertaleInstruction.Opcode.Push)
                            curr.Kind = UndertaleInstruction.Opcode.PushBltn;
                        variable.InstanceType = UndertaleInstruction.InstanceType.Self;
                        variable.VarID = -6;
                    }
                    if (list.GlobalArray.ContainsKey(curr.Destination?.Target.Name.Content))
                    {
                        variable.InstanceType = UndertaleInstruction.InstanceType.Self;
                        variable.VarID = -6;
                    }
                    if (list.Instance.ContainsKey(curr.Destination?.Target.Name.Content))
                    {
                        variable.InstanceType = UndertaleInstruction.InstanceType.Self;
                        variable.VarID = -6;
                    }
                    else
                    {
                        variable.VarID = id++;
                        if ((short)curr.TypeInst != 0)
                        {
                            variable.InstanceType = curr.TypeInst;
                        }
                        else
                        {
                            variable.InstanceType = type;
                        }
                    }
                }
            }
        }
    }
    foreach (UndertaleVariable vari in Data.Variables)
    {
        if ((list.GlobalNotArray.ContainsKey(vari.Name.Content)) || (list.GlobalArray.ContainsKey(vari.Name.Content)) || (list.Instance.ContainsKey(vari.Name.Content)))
        {
            vari.InstanceType = (UndertaleModLib.Models.UndertaleInstruction.InstanceType)(-1);
            vari.VarID = -6;
        }
    }
    foreach (var code in Data.Code)
    {
        for (int j = 0; j < code.Instructions.Count; j++)
        {
            if (code.Instructions[j].Value != null)
            {
                var evalme = code.Instructions[j].Value.ToString().Replace("\"", "").Replace("@", "");
                if ((list.GlobalNotArray.ContainsKey(evalme)) || (list.GlobalArray.ContainsKey(evalme)))
                {
                    code.Instructions[j].Kind = UndertaleInstruction.Opcode.PushBltn;
                }
            }
        }
    }
    Data.GeneralInfo.Build = 1804;
    //var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
    //Data.FORM.EXTN.productIdData.Add(newProductID);
    Data.Options.Constants.Clear();
    Data.Strings.IndexOf(Data.GeneralInfo.DisplayName);
    //ChangeSelection(Data.Strings[Data.Strings.IndexOf(Data.GeneralInfo.DisplayName)]);
    newString = new UndertaleString(0xFFFFFFFF.ToString());
    Data.Strings.Insert(Data.Strings.IndexOf(Data.GeneralInfo.DisplayName) + 1, newString);
    newString = new UndertaleString("@@DrawColour");
    Data.Strings.Insert(Data.Strings.IndexOf(Data.GeneralInfo.DisplayName) + 1, newString);
    newString = new UndertaleString("@@SleepMargin");
    Data.Strings.Insert(Data.Strings.IndexOf(Data.GeneralInfo.DisplayName) + 1, newString);
    Data.Options.Constants.Add(new UndertaleOptions.Constant() { Name = Data.Strings.MakeString("@@SleepMargin"), Value = Data.Strings.MakeString(1.ToString()) });
    Data.Options.Constants.Add(new UndertaleOptions.Constant() { Name = Data.Strings.MakeString("@@DrawColour"), Value = Data.Strings.MakeString(0xFFFFFFFF.ToString()) });
    Data.FORM.Chunks["LANG"] = new UndertaleChunkLANG();
    Data.FORM.LANG.Object = new UndertaleLanguage();
    Data.FORM.Chunks["GLOB"] = new UndertaleChunkGLOB();
    String[] order = { "GEN8", "OPTN", "LANG", "EXTN", "SOND", "AGRP", "SPRT", "BGND", "PATH", "SCPT", "GLOB", "SHDR", "FONT", "TMLN", "OBJT", "ROOM", "DAFL", "TPAG", "CODE", "VARI", "FUNC", "STRG", "TXTR", "AUDO" };
    Dictionary<string, UndertaleChunk> newChunks = new Dictionary<string, UndertaleChunk>();
    foreach (String name in order)
        newChunks[name] = Data.FORM.Chunks[name];
    Data.FORM.Chunks = newChunks;
    Data.GeneralInfo.BytecodeVersion = 16;
}

// Add 3 bytecode 16 variables at the top, reorganize

UndertaleVariable prototype = new UndertaleVariable();
UndertaleString newString = new UndertaleString("prototype");
Data.Strings.Insert(0, newString);
prototype.Name = Data.Strings.MakeString("prototype");
prototype.InstanceType = UndertaleModLib.Models.UndertaleInstruction.InstanceType.Self;
prototype.VarID = 0;
prototype.NameStringID = 0;
Data.Variables.Insert(0, prototype);

UndertaleVariable special_array = new UndertaleVariable();
newString = new UndertaleString("@@array@@");
Data.Strings.Insert(1, newString);
special_array.Name = Data.Strings.MakeString("@@array@@");
special_array.InstanceType = UndertaleModLib.Models.UndertaleInstruction.InstanceType.Self;
special_array.VarID = 1;
special_array.NameStringID = 0;
Data.Variables.Insert(1, special_array);

UndertaleVariable arguments = new UndertaleVariable();
newString = new UndertaleString("arguments");
Data.Strings.Insert(2, newString);
arguments.Name = Data.Strings.MakeString("arguments");
arguments.InstanceType = UndertaleModLib.Models.UndertaleInstruction.InstanceType.Local;
arguments.VarID = 0;
arguments.NameStringID = 0;
Data.Variables.Insert(2, arguments);

// Fix variables

Data.GeneralInfo.DisableDebugger = true; 
Data.MaxLocalVarCount = 1; 
int globalNum = 0;
int selfNum = 0;
foreach(var vari in Data.Variables)
{
    if (vari.InstanceType == UndertaleInstruction.InstanceType.Global)
    {
        vari.VarID = globalNum++;
    }
    else if ((vari.InstanceType == UndertaleInstruction.InstanceType.Self) && (vari.VarID >= 0))
    {
        vari.VarID = selfNum++;
    }
}
Data.VarCount1 = (uint)selfNum;
Data.VarCount2 = (uint)selfNum;
Data.DifferentVarCounts = false;

for (var i = 0; i < Data.Code.Count; i++)
{
    UndertaleCodeLocals locals = new UndertaleCodeLocals();
    locals.Name = Data.Code[i].Name;

    UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
    argsLocal.Name = Data.Strings.MakeString("arguments");
    argsLocal.Index = 0;

    locals.Locals.Add(argsLocal);

    Data.Code[i].LocalsCount = 1;
    Data.Code[i].GenerateLocalVarDefinitions(Data.Code[i].FindReferencedLocalVars(), locals); // Dunno if we actually need this line, but it seems to work?
    Data.Code[i].WeirdLocalFlag = false;
    Data.CodeLocals.Add(locals);
}
foreach (UndertaleGameObject obj in Data.GameObjects)
{
    for (var i = 12; i < 13; i++)
    {
        obj.Events.Add(new UndertalePointerList<UndertaleGameObject.Event>());
    }
}
if (Data.Code.ByName("gml_Script_SCR_TEXTTYPE") != null)
{
    Data.Strings.MakeString("script_execute").Content = "script_execute_wrapper";
    string SCR_TEXTTYPE = GetDecompiledText("gml_Script_SCR_TEXTTYPE");
    SCR_TEXTTYPE = SCR_TEXTTYPE.Replace("if (", "else if (");
    SCR_TEXTTYPE = SCR_TEXTTYPE.Replace("else if (argument0 != 0)", "if (argument0 != 0)");
    SCR_TEXTTYPE = SCR_TEXTTYPE.Replace("else if (global.typer == 1)", "if (global.typer == 1)");
    SCR_TEXTTYPE += @"else
    script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 101, 8, 18)";
    ImportGMLString("gml_Script_SCR_TEXTTYPE", SCR_TEXTTYPE);
    ImportGMLString("gml_Script_array_create_wrapper", @"
    var _arr, i;
    _arr[(argument0 - 1)] = 0
    if (argument_count > 1)
    {
        for (i = 0; i < argument0; i++)
            _arr[i] = argument1
    }
    return _arr;
    ");
    ImportGMLString("gml_Script_script_execute_wrapper", @"
    var args = array_create_wrapper(16, 0)
    for (var i = 0; i < argument_count; i++)
        args[i] = argument[i];
    script_execute(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12], args[13], args[14], args[15])");
}

ScriptMessage("Upgraded from " + currentBytecodeVersion + " to 16 successfully. Save the game to apply the changes.");


public static int CalculateStackDiff(UndertaleInstruction instr)
{
    switch (instr.Kind)
    {
        // TODO! Opcode.CallV

        case UndertaleInstruction.Opcode.Neg:
        case UndertaleInstruction.Opcode.Not:
            return 0;

        case UndertaleInstruction.Opcode.Dup:
            return 1 + instr.Extra;

        case UndertaleInstruction.Opcode.Ret:
            return -1;

        case UndertaleInstruction.Opcode.Exit:
            return 0;

        case UndertaleInstruction.Opcode.Popz:
            return -1;

        case UndertaleInstruction.Opcode.Conv:
            return 0;

        case UndertaleInstruction.Opcode.Mul:
        case UndertaleInstruction.Opcode.Div:
        case UndertaleInstruction.Opcode.Rem:
        case UndertaleInstruction.Opcode.Mod:
        case UndertaleInstruction.Opcode.Add:
        case UndertaleInstruction.Opcode.Sub:
        case UndertaleInstruction.Opcode.And:
        case UndertaleInstruction.Opcode.Or:
        case UndertaleInstruction.Opcode.Xor:
        case UndertaleInstruction.Opcode.Shl:
        case UndertaleInstruction.Opcode.Shr:
        case UndertaleInstruction.Opcode.Cmp:
            return -2 + 1;

        case UndertaleInstruction.Opcode.B:
            return 0;
        case UndertaleInstruction.Opcode.Bt:
        case UndertaleInstruction.Opcode.Bf:
        case UndertaleInstruction.Opcode.PushEnv:
            return -1;
        case UndertaleInstruction.Opcode.PopEnv:
            return 0;

        case UndertaleInstruction.Opcode.Pop:
            if (instr.Destination == null)
                return instr.SwapExtra - 6;
            if (instr.Destination.Type == UndertaleModLib.Models.UndertaleInstruction.VariableType.StackTop)
                return -1 - 1;
            if (instr.Destination.Type == UndertaleModLib.Models.UndertaleInstruction.VariableType.Array)
                return -1 - 2;
            return -1;

        case UndertaleInstruction.Opcode.Push:
        case UndertaleInstruction.Opcode.PushLoc:
        case UndertaleInstruction.Opcode.PushGlb:
        case UndertaleInstruction.Opcode.PushBltn:
        case UndertaleInstruction.Opcode.PushI:
            if (instr.Value is UndertaleModLib.Models.UndertaleInstruction.Reference<UndertaleVariable>)
            {
                if ((instr.Value as UndertaleModLib.Models.UndertaleInstruction.Reference<UndertaleVariable>).Type == UndertaleModLib.Models.UndertaleInstruction.VariableType.StackTop)
                    return 1 - 1;
                if ((instr.Value as UndertaleModLib.Models.UndertaleInstruction.Reference<UndertaleVariable>).Type == UndertaleModLib.Models.UndertaleInstruction.VariableType.Array)
                    return 1 - 2;
            }
            return 1;

        case UndertaleInstruction.Opcode.Call:
            return -instr.ArgumentsCount + 1;

        case UndertaleInstruction.Opcode.Break:
            return 0;

        default:
            throw new IOException("Unknown opcode " + instr.Kind.ToString().ToUpper());
    }
}

