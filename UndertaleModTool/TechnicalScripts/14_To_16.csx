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
    ImportGMLString("gml_Script_SCR_TEXTTYPE", @"
    if (argument0 != 0)
        global.typer = argument0
    if (global.typer == 1)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), (x + (global.idealborder[1] - 55)), 1, 1, 94, 16, 32)
    else if (global.typer == 2)
        script_execute_wrapper(149, 4, 0, x, y, (x + 190), 43, 2, 95, 9, 20)
    else if (global.typer == 3)
        script_execute_wrapper(149, 7, 8421376, x, y, (x + 100), 39, 3, 95, 10, 10)
    else if (global.typer == 4)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 101, 8, 18)
    else if (global.typer == 5)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 95, 8, 18)
    else if (global.typer == 6)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 1, 97, 9, 20)
    else if (global.typer == 7)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 2, 2, 98, 9, 20)
    else if (global.typer == 8)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 1, 101, 9, 20)
    else if (global.typer == 9)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 97, 8, 18)
    else if (global.typer == 10)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 96, 8, 18)
    else if (global.typer == 11)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 2, 94, 9, 18)
    else if (global.typer == 12)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 1, 3, 99, 10, 20)
    else if (global.typer == 13)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 2, 4, 99, 11, 20)
    else if (global.typer == 14)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 3, 5, 99, 14, 20)
    else if (global.typer == 15)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 10, 99, 18, 20)
    else if (global.typer == 16)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 1.2, 2, 98, 8, 18)
    else if (global.typer == 17)
        script_execute_wrapper(149, 8, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 88, 8, 18)
    else if (global.typer == 19)
        global.typer = 18
    else if (global.typer == 18)
        script_execute_wrapper(149, 9, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 87, 11, 18)
    else if (global.typer == 20)
        script_execute_wrapper(149, 5, 0, x, y, (x + 200), 0, 2, 98, 25, 20)
    else if (global.typer == 21)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 4, 96, 10, 18)
    else if (global.typer == 22)
        script_execute_wrapper(149, 9, 0, (x + 10), y, (x + 200), 1, 1, 87, 11, 20)
    else if (global.typer == 23)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 310), 0, 1, 95, 8, 18)
    else if (global.typer == 24)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 310), 0, 1, 65, 8, 18)
    else if (global.typer == 27)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 3, 56, 8, 18)
    else if (global.typer == 28)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 2, 65, 8, 18)
    else if (global.typer == 30)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), 9999, 0, 2, 90, 20, 36)
    else if (global.typer == 31)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), 9999, 0, 2, 90, 12, 18)
    else if (global.typer == 32)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), 9999, 0, 2, 84, 20, 36)
    else if (global.typer == 33)
        script_execute_wrapper(149, 4, 0, x, y, (x + 190), 43, 1, 95, 9, 20)
    else if (global.typer == 34)
        script_execute_wrapper(149, 0, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 3, 71, 16, 18)
    else if (global.typer == 35)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 2, 84, 10, 18)
    else if (global.typer == 36)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 8, 85, 10, 18)
    else if (global.typer == 37)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 78, 8, 18)
    else if (global.typer == 38)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 6, 78, 8, 18)
    else if (global.typer == 39)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 0, 1, 78, 9, 20)
    else if (global.typer == 40)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 1, 2, 78, 9, 20)
    else if (global.typer == 41)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 0, 1, 78, 9, 20)
    else if (global.typer == 42)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 2, 4, 78, 9, 20)
    else if (global.typer == 43)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 2, 4, 80, 9, 20)
    else if (global.typer == 44)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 2, 5, 81, 9, 20)
    else if (global.typer == 45)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 2, 7, 82, 9, 20)
    else if (global.typer == 47)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 83, 8, 18)
    else if (global.typer == 48)
        script_execute_wrapper(149, 8, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 89, 8, 18)
    else if (global.typer == 49)
        script_execute_wrapper(149, 4, 16777215, x, y, (x + 190), 43, 1, 83, 9, 20)
    else if (global.typer == 50)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 10), 999, 0, 3, 56, 8, 18)
    else if (global.typer == 51)
        script_execute_wrapper(149, 4, 0, (x + 20), (y + 16), 999, 0, 3, 56, 8, 18)
    else if (global.typer == 52)
        script_execute_wrapper(149, 4, 0, (x + 20), (y + 20), 999, 0, 1, 83, 8, 18)
    else if (global.typer == 53)
        script_execute_wrapper(149, 4, 0, (x + 20), (y + 10), 999, 1.5, 4, 56, 8, 18)
    else if (global.typer == 54)
        script_execute_wrapper(149, 4, 0, (x + 20), (y + 10), 999, 0, 7, 56, 8, 18)
    else if (global.typer == 55)
        script_execute_wrapper(149, 4, 0, x, y, (x + 999), 0, 2, 96, 9, 20)
    else if (global.typer == 60)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 2, 90, 8, 18)
    else if (global.typer == 61)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), (x + 99999), 0, 2, 96, 16, 32)
    else if (global.typer == 62)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 3, 90, 9, 20)
    else if (global.typer == 63)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 2, 90, 9, 20)
    else if (global.typer == 64)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 2, 3, 90, 9, 20)
    else if (global.typer == 66)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 2, 97, 9, 20)
    else if (global.typer == 67)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), (x + 999), 2, 5, 98, 16, 32)
    else if (global.typer == 68)
        script_execute_wrapper(149, 4, 16777215, x, y, (x + 500), 0, 1, 97, 9, 20)
    else if (global.typer == 69)
        script_execute_wrapper(149, 4, 16777215, x, y, (x + 500), 2, 2, 98, 9, 20)
    else if (global.typer == 70)
        script_execute_wrapper(149, 4, 16777215, x, y, (x + 500), 1, 3, 97, 9, 20)
    else if (global.typer == 71)
        script_execute_wrapper(149, 4, 16777215, x, y, (x + 500), 2, 5, 98, 9, 20)
    else if (global.typer == 72)
        script_execute_wrapper(149, 4, 16777215, x, y, (x + 500), 1, 2, 97, 9, 20)
    else if (global.typer == 73)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), (x + 99999), 0, 5, 96, 16, 32)
    else if (global.typer == 74)
        script_execute_wrapper(149, 4, 0, x, y, (x + 490), 0, 1, 83, 9, 20)
    else if (global.typer == 75)
        script_execute_wrapper(149, 4, 0, x, y, (x + 490), 2, 1, 83, 9, 20)
    else if (global.typer == 76)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 84, 8, 18)
    else if (global.typer == 77)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 4, 98, 9, 20)
    else if (global.typer == 78)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 2, 3, 98, 9, 20)
    else if (global.typer == 79)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 2, 85, 8, 18)
    else if (global.typer == 80)
        script_execute_wrapper(149, 8, 0, x, y, (x + 200), 0, 1, 88, 10, 20)
    else if (global.typer == 81)
        script_execute_wrapper(149, 4, 0, x, y, (x + 190), 0, 1, 78, 9, 20)
    else if (global.typer == 82)
        script_execute_wrapper(149, 4, 0, x, y, (x + 490), 2, 3, 83, 9, 20)
    else if (global.typer == 83)
        script_execute_wrapper(149, 9, 0, (x + 2), y, (x + 200), 1, 3, 87, 11, 20)
    else if (global.typer == 84)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 1, 2, 99, 10, 20)
    else if (global.typer == 85)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 2, 84, 9, 20)
    else if (global.typer == 86)
        script_execute_wrapper(149, 4, 0, (x + 10), y, (x + 200), 0, 1, 85, 9, 20)
    else if (global.typer == 87)
        script_execute_wrapper(149, 4, 0, (x + 10), y, (x + 200), 0, 3, 85, 9, 20)
    else if (global.typer == 88)
        script_execute_wrapper(149, 4, 0, (x + 10), y, (x + 200), 2, 3, 85, 9, 20)
    else if (global.typer == 89)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 84, 8, 18)
    else if (global.typer == 90)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 3, 84, 8, 18)
    else if (global.typer == 91)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), 9999, 0, 3, 101, 10, 18)
    else if (global.typer == 92)
        script_execute_wrapper(149, 4, 16777215, x, y, (x + 190), 43, 1, 95, 9, 20)
    else if (global.typer == 93)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 0, 1, 79, 9, 20)
    else if (global.typer == 94)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 1, 2, 79, 9, 20)
    else if (global.typer == 95)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 2, 3, 79, 9, 20)
    else if (global.typer == 96)
        script_execute_wrapper(149, 4, 0, (x + 16), y, (x + 190), 3, 4, 79, 9, 20)
    else if (global.typer == 97)
        script_execute_wrapper(149, 4, 0, (x + 16), y, 999, 1, 3, 56, 8, 18)
    else if (global.typer == 98)
        script_execute_wrapper(149, 4, 0, (x + 8), y, (x + 200), 0, 1, 97, 9, 20)
    else if (global.typer == 99)
        script_execute_wrapper(149, 4, 0, (x + 8), y, (x + 200), 1, 1, 97, 9, 20)
    else if (global.typer == 100)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 96, 8, 18)
    else if (global.typer == 101)
        script_execute_wrapper(149, 4, 0, (x + 8), y, (x + 200), 1, 2, 97, 9, 20)
    else if (global.typer == 102)
        script_execute_wrapper(149, 4, 0, (x + 8), y, (x + 200), 2, 3, 97, 9, 20)
    else if (global.typer == 103)
        script_execute_wrapper(149, 4, 0, (x + 8), y, (x + 200), 2, 5, 84, 9, 20)
    else if (global.typer == 104)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), 999, 0, 4, 96, 16, 34)
    else if (global.typer == 105)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), 999, 0, 3, 96, 16, 34)
    else if (global.typer == 106)
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), 999, 0, 3, 96, 8, 18)
    else if (global.typer == 107)
        script_execute_wrapper(149, 8, 0, (x + 5), y, (x + 200), 0, 2, 88, 10, 20)
    else if (global.typer == 108)
        script_execute_wrapper(149, 4, 0, x, y, (x + 200), 0, 4, 96, 9, 20)
    else if (global.typer == 109)
        script_execute_wrapper(149, 8, 0, (x + 5), y, (x + 200), 0, 1, 88, 10, 20)
    else if (global.typer == 110)
        script_execute_wrapper(149, 1, 16777215, (x + 20), (y + 20), 9999, 0, 2, 88, 20, 36)
    else if (global.typer == 111)
        script_execute_wrapper(149, 4, 0, x, y, (x + 190), 43, 1, 95, 9, 20)
    else if (global.typer == 666)
        script_execute_wrapper(149, 0, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 1, 4, 71, 16, 18)
    else
        script_execute_wrapper(149, 2, 16777215, (x + 20), (y + 20), (view_xview[view_current] + 290), 0, 1, 101, 8, 18)
    ");
    ImportGMLString("gml_Script_script_execute_wrapper", @"
    if (argument_count == 1)
    {
        script_execute(argument[0], 0, 0, 0, 0, 0, 0);
    }
    if (argument_count == 2)
    {
        script_execute(argument[0], argument[1]);
    }
    if (argument_count == (2 + 1))
    {
        script_execute(argument[0], argument[1], argument[2]);
    }
    if (argument_count == (3 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3]);
    }
    if (argument_count == (4 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4]);
    }
    if (argument_count == (5 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5]);
    }
    if (argument_count == (6 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6]);
    }
    if (argument_count == (7 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7]);
    }
    if (argument_count == (8 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7], argument[8]);
    }
    if (argument_count == (9 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7], argument[8], argument[9]);
    }
    if (argument_count == (10 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7], argument[8], argument[9], argument[10]);
    }
    if (argument_count == (11 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7], argument[8], argument[9], argument[10], argument[11]);
    }
    if (argument_count == (12 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7], argument[8], argument[9], argument[10], argument[11], argument[12]);
    }
    if (argument_count == (13 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7], argument[8], argument[9], argument[10], argument[11], argument[12], argument[13]);
    }
    if (argument_count == (14 + 1))
    {
        script_execute(argument[0], argument[1], argument[2], argument[3], argument[4], argument[5], argument[6], argument[7], argument[8], argument[9], argument[10], argument[11], argument[12], argument[13], argument[14]);
    }
    ");
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

