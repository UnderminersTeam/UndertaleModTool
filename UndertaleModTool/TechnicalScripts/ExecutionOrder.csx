// By colinator27 and BenjaminUrquhart, ImportGMLString at end from UndertaleModTool sample scripts
// Removed built in ImportGML, that has been integrated into the tool itself now - Grossley
// Reworked for Profile Mode by Grossley - 04/29/2021

using System;
using System.IO;
using UndertaleModLib.Util;
using System.Linq;

EnsureDataLoaded();

int progress = 0;

// This script profile mode compatible now. - Grossley
// But it takes like 5 minutes to run instead of 5 seconds now. - colinator27
ScriptMessage("This script is profile mode compatible. It may take a few minutes to complete.");
bool profileChoice = ScriptQuestion(@"Would you like to run this under GML editing mode or ASM mode?

Warning: All comments, decompilation corrections, and other relevant changes will be deleted from your profile in ASM mode.
ASM mode is recommended ONLY for games without code corrections or GMS 2.3 games.");

// Helper function for defining functions
UndertaleFunction DefineFunc(string name)
{
    var str = Data.Strings.MakeString(name);
    var func = new UndertaleFunction()
    {
        Name = str,
        UnknownChainEndingValue = Data.Strings.IndexOf(str)
    };
    Data.Functions.Add(func);
    return func;
}

// Define script functions so that we can use them in bytecode
// Actual implementations come later
UndertaleFunction func = Data.Functions.ByName("__scr_eventrun__"), endFunc, getInteractFunc, setInteractFunc;
UndertaleString eventStr, getInteractStr, setInteractStr;
if (func == null)
{
    func = DefineFunc("__scr_eventrun__");
    endFunc = DefineFunc("__scr_eventend__");
    getInteractFunc = DefineFunc("__scr_getinteract__");
    setInteractFunc = DefineFunc("__scr_setinteract__");
}
else
{
    if (ScriptQuestion(@"It cannot be removed, but it can be made invisible.
Select 'YES' to make it invisible.
If it is already invisible, select 'NO' to toggle the profiler back on."))
    {
        ClearCustomGML();
        return;
    }
    else
    {
        SetUpCustomGML();
        return;
    }
}

if (profileChoice)
{
    if (ScriptQuestion(@"This will make changes across all of the code! Are you sure you'd like to continue?
Note: this may break GML code if code corrections aren't present."))
    {
        ProfileModeOperations();
    }
    else
        return;
}
else
{
    ProfileModeExempt();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// GML implementations


void ClearCustomGML()
{
    ImportGMLString("gml_Object___obj_executionorder___Destroy_0", @"");
    ImportGMLString("gml_Object___obj_executionorder___Create_0", @"");
    ImportGMLString("gml_Object___obj_executionorder___Draw_64", @"");
    ImportGMLString("gml_Script___scr_eventrun__", @"");
    ImportGMLString("gml_Script___scr_eventend__", @"");
    ImportGMLString("gml_Script___scr_setinteract__", @"");
    ImportGMLString("gml_Script___scr_getinteract__", @"");
}

SetUpCustomGML();

void SetUpCustomGML()
{
    // __obj_executionorder__
    ImportGMLString("gml_Object___obj_executionorder___Destroy_0", @"
    ds_stack_destroy(stack);
    ");

    ImportGMLString("gml_Object___obj_executionorder___Create_0", @"
    global.interact = 0; // prevents error on obj_time create from missing globals
    events[1024, 4] = 0;
    stack = ds_stack_create();
    i = 0;
    depth = -99999999;
    delay = 60;
    maxwidth = 0;
    ");
    string str = "";
    if (Data.Fonts.ByName("fnt_maintext") != null)
        str = "draw_set_font(fnt_maintext);";
    else if (Data.Fonts.ByName("fnt_main") != null)
        str = "draw_set_font(fnt_main);";
    else if (Data.Fonts.Count != 0)
        str = "draw_set_font(" + Data.Fonts[0].Name.Content + ");";

    str += @"
    // Find duplicates and remove them by making them undefined
    var dupcount = 0;
    for (var j = 1; j <= i; j++)
    {
        if (j != i &&
            events[j, 0] == events[j - 1, 0] &&
            events[j, 1] == events[j - 1, 1] &&
            events[j, 2] == events[j - 1, 2] &&
            events[j, 3] == events[j - 1, 3] &&
            events[j, 4] == events[j - 1, 4])
        {
            dupcount++;
        } else if (dupcount > 0)
        {
            // Remove those duplicates now
            for (var k = j - dupcount; k < j; k++)
                events[k, 0] = undefined;
            events[j - dupcount - 1, 0] += "" (x"" + string(dupcount + 1) + "")"";
            dupcount = 0;
        }
    }
    ";

    if (Data.Sprites.ByName("spr_pixwht") != null)
    {
        str += @"
    var mw = 0;
    for (var j = 0; j < i; j++)
    {
        if (!is_undefined(events[j, 0]))
            mw = max(mw, string_width(events[j, 0]));
    }
    if (mw > maxwidth || delay++ >= 60)
    {
        maxwidth = mw + (string_width(""AAA""));
        delay = 0;
    }
    draw_sprite_ext(spr_pixwht, 0, 0, 0, maxwidth * 0.5, display_get_gui_height() * 0.5, 0, c_ltgray, 0.1);
    ";
    }

    str += @"
    // Actually draw events now

    var h = floor(string_height(""A""));
    draw_set_color(c_black);
    var k = 0;
    for (var j = 0; j < i; j++)
    {
        var curr = events[j, 0];
        if (!is_undefined(curr))
        {
            draw_text(6, 31 + (k * h), curr);
            draw_text(6, 29 + (k * h), curr);
            draw_text(4, 29 + (k * h), curr);
            draw_text(4, 31 + (k * h), curr);
            k++;
        }
    }
    k = 0;
    for (var j = 0; j < i; j++)
    {
        var curr = events[j, 0];
        if (!is_undefined(curr))
        {
            if (events[j, 1]) // setinteract
            {
                if (events[j, 2]) // getinteract
                    draw_set_color(merge_color(c_green, c_lime, 0.5));
                else
                    draw_set_color(c_lime);
            } else
            {
                if (events[j, 2]) // getinteract
                    draw_set_color(c_teal);
                else
                    draw_set_color(c_yellow);
            }
            draw_text(5, 30 + (k * h), curr);
            k++;
        }
    }

    // Reset for next frame
    i = 0;
    ds_stack_clear(stack);
    ";
    ImportGMLString("gml_Object___obj_executionorder___Draw_64" /* draw gui */, str);
    var objt = Data.GameObjects.ByName("__obj_executionorder__");
    objt.Persistent = true;
    Data.GeneralInfo.RoomOrder.First().Resource.GameObjects.Insert(0, new UndertaleRoom.GameObject()
    {
        InstanceID = Data.GeneralInfo.LastObj++,
        ObjectDefinition = objt
    });

    // Script implementations
    ImportGMLString("gml_Script___scr_eventrun__", @"
    with (__obj_executionorder__) 
    {
        var recursion = """";
        for (var j = ds_stack_size(stack); j > 0; j--)
            recursion += ""> "";
        events[i, 0] = recursion + argument0;
        events[i, 1] = false; // set interact
        events[i, 2] = false; // get interact
        events[i, 3] = global.interact; // first interact value
        events[i, 4] = global.interact; // second interact value
        ds_stack_push(stack, i);
        i++;
    }
    ");

    ImportGMLString("gml_Script___scr_eventend__", @"
    with (__obj_executionorder__) 
    {
        var _i = ds_stack_pop(stack);
        
        // Set interact
        if (events[_i, 1])
            events[_i, 0] += ("" ("" + string(events[_i, 3]) + "" -> "" + string(events[_i, 4]) + "")"");
    }
    ");

    ImportGMLString("gml_Script___scr_setinteract__", @"
    with (__obj_executionorder__)
    {
        if (ds_stack_size(stack) > 0)
        {
            events[ds_stack_top(stack), 1] = true;
            events[ds_stack_top(stack), 4] = argument0;
        }
    }
    global.interact = argument0;
    ");

    ImportGMLString("gml_Script___scr_getinteract__", @"
    with (__obj_executionorder__)
    {
        if (ds_stack_size(stack) > 0)
            events[ds_stack_top(stack), 2] = true;
    }
    return global.interact;
    ");
}

///////////////////////////////////////////////////////////////////////////

void ProfileModeOperations()
{
    // Process GML in the following fashion:
    // Always put a call to scr_eventrun, with the name of the code from which it was called, at line 1.
    // At end of file, also put scr_eventend.
    // Replace all interacts with corresponding calls.
    // Call scr_eventend immediately before a "return" or "exit" in order to make sure it is always called at end.
    // This avoids the hacky ASM solution of forcing the code to jump to the end of the file.
    // It also makes sense from a GML standpoint.
    // This will only work for games with corrections, and UT/DR, for which GML can be guaranteed safe.
    // Otherwise, the original (hacky) solution will need to be done.
    // But this GML solution is so simple, inserting the call to scr_eventend similarly seems more than possible.
    // Sadly, I don't know how to do such an equivalent action myself.

    //TODO: reimplement ASM solution w/o hacky asm breaking

    foreach (UndertaleCode c in Data.Code)
    {
        UpdateProgressBar(null, "Code entries processed", progress++, Data.Code.Count);
        string gmlCode = GetDecompiledText(c.Name.Content);
        gmlCode = gmlCode.Replace("\r\n", "\n");
        gmlCode = Regex.Replace(gmlCode, "global\\.interact = (\\d+)", "__scr_setinteract__\\(\\1\\)");
        gmlCode = gmlCode.Replace("global.interact", "__scr_getinteract__()");
        if (c.Name.Content.StartsWith("gml_Object"))
        {
            gmlCode = ("__scr_eventrun__(\"" + c.Name.Content.Substring(11) + "\")\n" + gmlCode + "\n__scr_eventend__()");
            gmlCode = gmlCode.Replace("return;\n", "{__scr_eventend__();return;}\n");
            gmlCode = gmlCode.Replace("exit\n", "{__scr_eventend__();exit;}\n");
        }
        c.ReplaceGML(gmlCode, Data);
    }
}
void ProfileModeExempt()
{
    // Process bytecode, patching in script calls where needed
    foreach (UndertaleCode c in Data.Code)
    {
        // global.interact get/set patches
        for (int i = 0; i < c.Instructions.Count; i++)
        {
            UndertaleInstruction inst = c.Instructions[i];
            if (inst.Kind == UndertaleInstruction.Opcode.PushGlb &&
                ((UndertaleInstruction.Reference<UndertaleVariable>)inst.Value).Target.Name.Content == "interact")
            {
                // global.interact getter
                c.Instructions[i] = new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Call,
                    Type1 = UndertaleInstruction.DataType.Int32,
                    ArgumentsCount = 0,
                    Function = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = getInteractFunc }
                };
                NukeProfileGML(c.Name.Content);
            }
            else if (inst.Kind == UndertaleInstruction.Opcode.Pop &&
              ((UndertaleInstruction.Reference<UndertaleVariable>)inst.Destination).Target.Name.Content == "interact")
            {
                // global.interact setter
                c.Instructions[i] = new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Conv,
                    Type1 = UndertaleInstruction.DataType.Int32,
                    Type2 = UndertaleInstruction.DataType.Variable
                };
                c.Instructions.Insert(i + 1, new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Popz,
                    Type1 = UndertaleInstruction.DataType.Variable
                });
                c.Instructions.Insert(i + 1, new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Call,
                    Type1 = UndertaleInstruction.DataType.Int32,
                    ArgumentsCount = 1,
                    Function = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = setInteractFunc }
                });

                // Now that instructions were inserted, adjust jump offsets in
                // surrounding goto instructions to properly reflect that
                for (int j = 0; j < i; j++)
                {
                    var currPatch = c.Instructions[j];
                    if (UndertaleInstruction.GetInstructionType(currPatch.Kind) == UndertaleInstruction.InstructionType.GotoInstruction)
                    {
                        if (currPatch.Address + currPatch.JumpOffset > inst.Address)
                            currPatch.JumpOffset += 2;
                    }
                }
                for (int j = i + 3; j < c.Instructions.Count; j++)
                {
                    var currPatch = c.Instructions[j];
                    if (UndertaleInstruction.GetInstructionType(currPatch.Kind) == UndertaleInstruction.InstructionType.GotoInstruction)
                    {
                        if (currPatch.Address + currPatch.JumpOffset <= inst.Address)
                            currPatch.JumpOffset -= 2;
                    }
                }
                NukeProfileGML(c.Name.Content);
            }
        }

        if (c.Name.Content.StartsWith("gml_Object"))
        {
            // Insert function call to __scr_eventrun__ with entry name at the beginning
            var newString = Data.Strings.MakeString(c.Name.Content.Substring(11));
            c.Instructions.InsertRange(0, new List<UndertaleInstruction>()
            {
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Push,
                    Type1 = UndertaleInstruction.DataType.String,
                    Value = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>() { Resource = newString, CachedId = Data.Strings.IndexOf(newString) }
                },
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Conv,
                    Type1 = UndertaleInstruction.DataType.String,
                    Type2 = UndertaleInstruction.DataType.Variable
                },
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Call,
                    Type1 = UndertaleInstruction.DataType.Int32,
                    ArgumentsCount = 1,
                    Function = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = func }
                },
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Popz,
                    Type1 = UndertaleInstruction.DataType.Variable
                }
            });

            // Patch every exit instruction to instead branch to the end of the code
            c.UpdateAddresses();
            var last = c.Instructions.Last();
            uint endAddr = last.Address + last.CalculateInstructionSize();
            for (int i = 4; i < c.Instructions.Count; i++)
            {
                if (c.Instructions[i].Kind == UndertaleInstruction.Opcode.Exit)
                {
                    c.Instructions[i] = new UndertaleInstruction()
                    {
                        Kind = UndertaleInstruction.Opcode.B,
                        JumpOffset = (int)(endAddr - c.Instructions[i].Address)
                    };
                }
            }

            // At the end of the code, insert function call to __scr_eventend__
            c.Instructions.AddRange(new List<UndertaleInstruction>()
            {
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Call,
                    Type1 = UndertaleInstruction.DataType.Int32,
                    ArgumentsCount = 0,
                    Function = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = endFunc }
                },
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Popz,
                    Type1 = UndertaleInstruction.DataType.Variable
                }
            });
            NukeProfileGML(c.Name.Content);
        }
    }
}