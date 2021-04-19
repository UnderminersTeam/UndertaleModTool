// By colinator27 and BenjaminUrquhart, ImportGML at end from UndertaleModTool sample scripts

using System;
using System.IO;
using UndertaleModLib.Util;
using System.Linq;

EnsureDataLoaded();

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
        } else if (inst.Kind == UndertaleInstruction.Opcode.Pop &&
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
    }
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// GML implementations


void ClearCustomGML()
{
    ImportGML("gml_Object___obj_executionorder___Destroy_0", @"");
    ImportGML("gml_Object___obj_executionorder___Create_0", @"");
    ImportGML("gml_Object___obj_executionorder___Draw_64", @"");
    ImportGML("gml_Script___scr_eventrun__", @"");
    ImportGML("gml_Script___scr_eventend__", @"");
    ImportGML("gml_Script___scr_setinteract__", @"");
    ImportGML("gml_Script___scr_getinteract__", @"");
}

SetUpCustomGML();

void SetUpCustomGML()
{
    // __obj_executionorder__
    ImportGML("gml_Object___obj_executionorder___Destroy_0", @"
    ds_stack_destroy(stack);
    ");

    ImportGML("gml_Object___obj_executionorder___Create_0", @"
    global.interact = 0; // prevents error on obj_time create from missing globals
    events[1024, 4] = 0;
    stack = ds_stack_create();
    i = 0;
    depth = -99999999;
    delay = 60;
    maxwidth = 0;
    ");
    string str;
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
    ImportGML("gml_Object___obj_executionorder___Draw_64" /* draw gui */, str);
    var objt = Data.GameObjects.ByName("__obj_executionorder__");
    objt.Persistent = true;
    Data.GeneralInfo.RoomOrder.First().Resource.GameObjects.Insert(0, new UndertaleRoom.GameObject()
    {
        InstanceID = Data.GeneralInfo.LastObj++,
        ObjectDefinition = objt
    });
    
    // Script implementations
    ImportGML("gml_Script___scr_eventrun__", @"
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
    
    ImportGML("gml_Script___scr_eventend__", @"
    with (__obj_executionorder__) 
    {
        var _i = ds_stack_pop(stack);
        
        // Set interact
        if (events[_i, 1])
            events[_i, 0] += ("" ("" + string(events[_i, 3]) + "" -> "" + string(events[_i, 4]) + "")"");
    }
    ");
    
    ImportGML("gml_Script___scr_setinteract__", @"
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
    
    ImportGML("gml_Script___scr_getinteract__", @"
    with (__obj_executionorder__)
    {
        if (ds_stack_size(stack) > 0)
            events[ds_stack_top(stack), 2] = true;
    }
    return global.interact;
    ");
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Import the GML
enum EventTypes {
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
    Gestures,
    PreCreate
}

void ImportGML(string codeName, string gmlCode) {
    if (Data.Code.ByName(codeName) == null) { // Should keep from adding duplicate scripts; haven't tested
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
        // This portion links code.
        if (codeName.Substring(0, 10).Equals("gml_Script")) {
            // Add code to scripts section.
            UndertaleScript scr = new UndertaleScript();
            scr.Name = Data.Strings.MakeString(codeName.Substring(11));
            scr.Code = code;
            Data.Scripts.Add(scr);
        } 
        else if (codeName.Substring(0, 10).Equals("gml_Object")) {
            // Add code to object methods.
            string afterPrefix = codeName.Substring(11);
            // Dumb substring shite, don't mess with this.
            int underCount = 0;
            string methodNumberStr = "", methodName = "", objName = "";
            for (int i = afterPrefix.Length - 1; i >= 0; i--) {
                if (afterPrefix[i] == '_') {
                    underCount++;
                    if (underCount == 1) {
                        methodNumberStr = afterPrefix.Substring(i + 1);
                    } else if (underCount == 2) {
                        objName = afterPrefix.Substring(0, i);
                        methodName = afterPrefix.Substring(i + 1, afterPrefix.Length - objName.Length - methodNumberStr.Length - 2);
                        break;
                    }
                }
            }
            int methodNumber = Int32.Parse(methodNumberStr);
            UndertaleGameObject obj = Data.GameObjects.ByName(objName);
            if (obj == null) {
                UndertaleGameObject gameObj = new UndertaleGameObject();
                gameObj.Name = Data.Strings.MakeString(objName);
                Data.GameObjects.Add(gameObj);
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
