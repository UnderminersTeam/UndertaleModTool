// By colinator27 and BenjaminUrquhart, ImportGMLString at end from UndertaleModTool sample scripts
// Removed built in ImportGML, that has been integrated into the tool itself now - Grossley
// Reworked for Profile Mode by Grossley - 04/29/2021
// Rewritten a bunch to use new compiler, and profile mode operations removed

using System;
using System.IO;
using UndertaleModLib.Util;
using System.Linq;

EnsureDataLoaded();

int progress = 0;

// Helper function for defining functions
UndertaleFunction DefineFunc(string name)
{
    UndertaleString str = Data.Strings.MakeString(name, out int id);
    UndertaleFunction func = new()
    {
        Name = str,
        NameStringID = id
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
    if (ScriptQuestion(@"This script cannot be removed, but it can be made invisible.
Select 'YES' to make it invisible.
If it is already invisible, select 'NO' to toggle it back on."))
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

ProfileModeExempt();

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// GML implementations


void ClearCustomGML()
{
    UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
    importGroup.QueueReplace("gml_Object___obj_executionorder___Destroy_0", "");
    importGroup.QueueReplace("gml_Object___obj_executionorder___Create_0", "");
    importGroup.QueueReplace("gml_Object___obj_executionorder___Draw_64", "");
    importGroup.QueueReplace("gml_Script___scr_eventrun__", "");
    importGroup.QueueReplace("gml_Script___scr_eventend__", "");
    importGroup.QueueReplace("gml_Script___scr_setinteract__", "");
    importGroup.QueueReplace("gml_Script___scr_getinteract__", "");
    importGroup.Import();
}

SetUpCustomGML();

void SetUpCustomGML()
{
    UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

    // __obj_executionorder__
    importGroup.QueueReplace("gml_Object___obj_executionorder___Destroy_0", @"
    ds_stack_destroy(stack);
    ");

    importGroup.QueueReplace("gml_Object___obj_executionorder___Create_0", @"
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
    importGroup.QueueReplace("gml_Object___obj_executionorder___Draw_64" /* draw gui */, str);

    var objt = Data.GameObjects.ByName("__obj_executionorder__");
    objt.Persistent = true;
    Data.GeneralInfo.RoomOrder.First().Resource.GameObjects.Insert(0, new UndertaleRoom.GameObject()
    {
        InstanceID = Data.GeneralInfo.LastObj++,
        ObjectDefinition = objt
    });

    // Script implementations
    importGroup.QueueReplace("gml_Script___scr_eventrun__", @"
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

    importGroup.QueueReplace("gml_Script___scr_eventend__", @"
    with (__obj_executionorder__) 
    {
        var _i = ds_stack_pop(stack);
        
        // Set interact
        if (events[_i, 1])
            events[_i, 0] += ("" ("" + string(events[_i, 3]) + "" -> "" + string(events[_i, 4]) + "")"");
    }
    ");

    importGroup.QueueReplace("gml_Script___scr_setinteract__", @"
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

    importGroup.QueueReplace("gml_Script___scr_getinteract__", @"
    with (__obj_executionorder__)
    {
        if (ds_stack_size(stack) > 0)
            events[ds_stack_top(stack), 2] = true;
    }
    return global.interact;
    ");

    importGroup.Import();
}

///////////////////////////////////////////////////////////////////////////

void ProfileModeExempt()
{
    // Process bytecode, patching in script calls where needed
    foreach (UndertaleCode c in Data.Code)
    {
        if (c is null)
            continue;
        // global.interact get/set patches
        uint addr = 0;
        for (int i = 0; i < c.Instructions.Count; i++)
        {
            UndertaleInstruction inst = c.Instructions[i];

            if (inst.Kind == UndertaleInstruction.Opcode.PushGlb &&
                inst.ValueVariable.Target.Name.Content == "interact")
            {
                // global.interact getter
                c.Instructions[i] = new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Call,
                    Type1 = UndertaleInstruction.DataType.Int32,
                    ArgumentsCount = 0,
                    ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = getInteractFunc }
                };
            }
            else if (inst.Kind == UndertaleInstruction.Opcode.Pop && 
                     inst.ValueVariable.Target.Name.Content == "interact")
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
                    ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = setInteractFunc }
                });

                // Now that instructions were inserted, adjust jump offsets in
                // surrounding goto instructions to properly reflect that
                uint adjustAddr = 0;
                for (int j = 0; j < i; j++)
                {
                    var currPatch = c.Instructions[j];
                    if (UndertaleInstruction.GetInstructionType(currPatch.Kind) == UndertaleInstruction.InstructionType.GotoInstruction)
                    {
                        if (adjustAddr + currPatch.JumpOffset > addr)
                            currPatch.JumpOffset += 2;
                    }
                    adjustAddr += currPatch.CalculateInstructionSize();
                }
                for (int j = i; j < i + 3; j++)
                {
                    adjustAddr += c.Instructions[i].CalculateInstructionSize();
                }
                for (int j = i + 3; j < c.Instructions.Count; j++)
                {
                    var currPatch = c.Instructions[j];
                    if (UndertaleInstruction.GetInstructionType(currPatch.Kind) == UndertaleInstruction.InstructionType.GotoInstruction)
                    {
                        if (adjustAddr + currPatch.JumpOffset <= addr)
                            currPatch.JumpOffset -= 2;
                    }
                    adjustAddr += currPatch.CalculateInstructionSize();
                }
            }

            addr += inst.CalculateInstructionSize();
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
                    ValueString = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>() { Resource = newString, CachedId = Data.Strings.IndexOf(newString) }
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
                    ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = func }
                },
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Popz,
                    Type1 = UndertaleInstruction.DataType.Variable
                }
            });

            // Patch every exit instruction to instead branch to the end of the code
            c.UpdateLength();
            uint endAddr = c.Length / 4;
            uint exitPatchAddr = 0;
            for (int i = 0; i < 4; i++)
            {
                exitPatchAddr += c.Instructions[i].CalculateInstructionSize();
            }
            for (int i = 4; i < c.Instructions.Count; i++)
            {
                if (c.Instructions[i].Kind == UndertaleInstruction.Opcode.Exit)
                {
                    c.Instructions[i] = new UndertaleInstruction()
                    {
                        Kind = UndertaleInstruction.Opcode.B,
                        JumpOffset = (int)(endAddr - exitPatchAddr)
                    };
                }
                exitPatchAddr += c.Instructions[i].CalculateInstructionSize();
            }

            // At the end of the code, insert function call to __scr_eventend__
            c.Instructions.AddRange(new List<UndertaleInstruction>()
            {
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Call,
                    Type1 = UndertaleInstruction.DataType.Int32,
                    ArgumentsCount = 0,
                    ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = endFunc }
                },
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Popz,
                    Type1 = UndertaleInstruction.DataType.Variable
                }
            });
            c.UpdateLength();
        }
    }
}
