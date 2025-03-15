// By colinator27 and BenjaminUrquhart, ImportGMLString at end from UndertaleModTool sample scripts
// Reworked to be a profiler and stack tracer (to identify freeze locations) by Grossley
// Removed built in ImportGML, that has been integrated into the tool itself now, replace with ImportGMLString - Grossley
// Reworked for Profile Mode by Grossley - 04/29/2021
// Rewritten a bunch to use new compiler, and profile mode operations removed

using System;
using System.IO;
using UndertaleModLib.Util;
using System.Linq;

EnsureDataLoaded();

int progress = 0;

if (!(ScriptQuestion(@"This script is irreversible
and cannot be removed. 
Press ""NO"" to cancel now without changes applied.
Otherwise, press ""YES"" to continue")))
{
    ScriptMessage("Cancelled!");
    return;
}

if (!(ScriptQuestion(@"THIS SCRIPT IS FOR DEBUGGING USE ONLY
THIS SCRIPT IS ALSO IRREVERSIBLE, AND CANNOT BE REMOVED.

As such, a SEPARATE CLEAN COPY of the game should
be kept, and use this copy as reference and for 
DEBUGGING ONLY. If you do not have a separate clean
copy of the game build, or are not willing to
manually transfer any fixes or changes over to the
clean version, then: 

SELECT ""NO"" NOW to cancel the script without changes applied.")))
{
    ScriptMessage("Cancelled!");
    return;
}

if (!(ScriptQuestion(@"Profiler data will be located in the save directory of your game.
A stacktrace is also available in the event your game freezes or 
crashes without warning.

WARNING: profiler may cause lag due to large amounts of file i/o.
This is normal. To fix this, please send a more competent
programmer to redesign this script.

YES to apply
NO to cancel script without changes")))
{
    ScriptMessage("Cancelled!");
    return;
}

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

ProfileModeExempt();

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// GML implementations


void ClearCustomGML()
{
    UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
    importGroup.QueueReplace("gml_Object___obj_executionorder___Destroy_0", "");
    importGroup.QueueReplace("gml_Object___obj_executionorder___Create_0", "");
    importGroup.QueueReplace("gml_Object___obj_executionorder___Draw_64", "");
    importGroup.QueueReplace("gml_Object_obj_grossley_persist_Create_0", "");
    importGroup.QueueReplace("gml_Object_obj_grossley_persist_Step_0", "");
    importGroup.QueueReplace("gml_Object_obj_grossley_persist_Draw_64", "");
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
    PersistentObjectSetup("obj_grossley_persist");
    importGroup.QueueReplace("gml_Object_obj_grossley_persist_Create_0", @"
    ");
    importGroup.QueueReplace("gml_Object_obj_grossley_persist_Step_0", @"
    global.grossley_timer = get_timer();
    ");
    importGroup.QueueReplace("gml_Object_obj_grossley_persist_Draw_64", @"
    var i, file;
    var time_spent_total = 0;
    var times_called_total = 0;
    var avg_spent_total = 0;
    //Time spent
    file = file_text_open_write(working_directory + ""/Profiler_Time_Spent.txt"");
    file_text_write_string(file, ""Time spent:"");
    file_text_writeln(file);
    for (i = 0; i < array_length_1d(time_spent_array); i++)
    {
        time_spent_total += time_spent_array[i];
    }
    for (i = 0; i < array_length_1d(time_spent_array); i++)
    {
        if (time_spent_array[i] != 0)
        {
            percentage = string((time_spent_array[i]/time_spent_total)*100) + ""%""
            file_text_write_string(file, object_get_name(i) + "" ("" + percentage + "" of total instantaneous time)"" + "": "" + "" "" + string(time_spent_array[i]) + "" microseconds"" + "", "" + string(time_spent_array[i]/1000000) + "" seconds"");
            file_text_writeln(file);
        }
    }
    file_text_close(file);
    //Times called
    file = file_text_open_write(working_directory + ""/Profiler_Times_Called.txt"");
    file_text_write_string(file, ""Times called:"");
    file_text_writeln(file);
    for (i = 0; i < array_length_1d(times_called_array); i++)
    {
        times_called_total += times_called_array[i];
    }
    for (i = 0; i < array_length_1d(times_called_array); i++)
    {
        if (times_called_array[i] != 0)
        {
            percentage = string((times_called_array[i]/times_called_total)*100)+ ""%""
            file_text_write_string(file, object_get_name(i) + "" ("" + percentage + "" of total calls)"" + "": "" + "" "" + string(times_called_array[i]) + "" calls"");
            file_text_writeln(file);
        }
    }
    file_text_close(file);
    //Times on average
    file = file_text_open_write(working_directory + ""/Profiler_Averages.txt"");
    file_text_write_string(file, ""Averages:"");
    file_text_writeln(file);
    for (i = 0; i < array_length_1d(averages_array); i++)
    {
        avg_spent_total += averages_array[i];
    }
    for (i = 0; i < array_length_1d(averages_array); i++)
    {
        if (averages_array[i] != 0)
        {
            percentage = string((averages_array[i]/avg_spent_total)*100) + ""%""
            file_text_write_string(file, object_get_name(i) + "" ("" + percentage + "" of total average time)"" + "": "" + "" "" + string(averages_array[i]) + "" microseconds"" + "", "" + string(averages_array[i]/1000000) + "" seconds"");
            file_text_writeln(file);
        }
    }
    file_text_close(file);
    //Times on average
    file = file_text_open_write(working_directory + ""/Profiler_Averages.txt"");
    file_text_write_string(file, ""Averages:"");
    file_text_writeln(file);
    for (i = 0; i < array_length_1d(averages_array); i++)
    {
        avg_spent_total += averages_array[i];
    }
    for (i = 0; i < array_length_1d(averages_array); i++)
    {
        if (averages_array[i] != 0)
        {
            percentage = string((averages_array[i]/avg_spent_total)*100) + ""%""
            file_text_write_string(file, object_get_name(i) + "" ("" + percentage + "" of total average time)"" + "": "" + "" "" + string(averages_array[i]) + "" microseconds"" + "", "" + string(averages_array[i]/1000000) + "" seconds"");
            file_text_writeln(file);
        }
    }
    file_text_close(file);
    b = 0
    if (b)
    {
        draw_text(0, 0, ""Time spent:"" + string(time_spent_array));
        draw_text(0, 16, ""Times called:"" +  string(times_called_array));
        draw_text(0, 32, ""Averages:"" + string(averages_array));
    }
    ");

    importGroup.QueueReplace("gml_Script___scr_eventrun__", @"
    if (!instance_exists(__obj_executionorder__))
        instance_activate_object(__obj_executionorder__);
    if (!instance_exists(obj_grossley_persist))
        instance_activate_object(obj_grossley_persist);
    stacktrace = file_text_open_append(working_directory + ""/Stacktrace.txt"");
    file_text_write_string(stacktrace, argument0);
    file_text_writeln(stacktrace);
    file_text_close(stacktrace);
    stacktrace = file_text_open_read((working_directory + ""/Stacktrace.txt""))
    line_count = 0
    while (!file_text_eof(stacktrace))
    {
        file_text_readln(stacktrace)
        line_count++
    }
    file_text_close(stacktrace)
    if (line_count > 100)
    {
        stacktrace = file_text_open_write((working_directory + ""/Stacktrace.txt""))
        file_text_write_string(stacktrace, argument0)
        file_text_writeln(stacktrace)
        file_text_close(stacktrace)
    }
    stacktrace = file_text_open_write(working_directory + ""/Stacktrace_Last.txt"");
    file_text_write_string(stacktrace, argument0);
    file_text_writeln(stacktrace);
    file_text_close(stacktrace);
    global.grossley_obj_ind = object_index;
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
    if (!(instance_exists(obj_grossley_persist)))
    {
        instance_create(0, 0, obj_grossley_persist)
        with (obj_grossley_persist)
        {
            depth = -99999999;
            global.grossley_timer = 0
            global.grossley_obj_ind = 0
            array_lower_bound = 0;
            array_upper_bound = 0;
            for (i = 0; object_exists(i); i++)
            {
                array_upper_bound = i;
            }
            for (i = 0; i <= array_upper_bound; i++)
            {
                timer_last_array[i] = 0;// array_create(array_upper_bound + 1);
                timer_current_array[i] = 0;// array_create(array_upper_bound + 1);
                time_difference_array[i] = 0;// array_create(array_upper_bound + 1);
                time_spent_array[i] = 0;// array_create(array_upper_bound + 1);
                times_called_array[i] = 0;// array_create(array_upper_bound + 1);
                averages_array[i] = 0;// array_create(array_upper_bound + 1);
            }
        }
    }
    with(obj_grossley_persist)
    {
        timer_last_array[global.grossley_obj_ind] = get_timer();
    }
    ".Replace("instance_create(0, 0, obj_grossley_persist)", ((Data.GeneralInfo.Major < 2) ? "instance_create(0, 0, obj_grossley_persist)" : "instance_create_depth(0, 0, -99999999, obj_grossley_persist)")));

    importGroup.QueueReplace("gml_Script___scr_eventend__", @"
    global.grossley_obj_ind = object_index;
    with (__obj_executionorder__) 
    {
        var _i = ds_stack_pop(stack);
        
        // Set interact
        if (events[_i, 1])
            events[_i, 0] += ("" ("" + string(events[_i, 3]) + "" -> "" + string(events[_i, 4]) + "")"");
    }
    with(obj_grossley_persist)
    {
        timer_current_array[global.grossley_obj_ind] = get_timer();
        time_difference_array[global.grossley_obj_ind] = (timer_current_array[global.grossley_obj_ind] - timer_last_array[global.grossley_obj_ind]);
        time_spent_array[global.grossley_obj_ind] += time_difference_array[global.grossley_obj_ind];
        times_called_array[global.grossley_obj_ind] += 1;
        averages_array[global.grossley_obj_ind] = ((time_spent_array[global.grossley_obj_ind])/(times_called_array[global.grossley_obj_ind]));
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
        // global.interact get/set patches
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
                    ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = endFunc }
                },
                new UndertaleInstruction()
                {
                    Kind = UndertaleInstruction.Opcode.Popz,
                    Type1 = UndertaleInstruction.DataType.Variable
                }
            });
        }
    }
}

void PersistentObjectSetup(string objectName)
{
    var obj = Data.GameObjects.ByName(objectName);
    if (obj == null)
    {
        obj = new UndertaleGameObject() { Name = Data.Strings.MakeString(objectName), Persistent = true };
        Data.GameObjects.Add(obj);
    }
    if (Data.GeneralInfo.Name.Content.StartsWith("UNDERTALE"))
    {
        UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
        importGroup.QueueReplace(Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, (uint)114, Data), "");
        importGroup.Import();
    }
    bool gms2 = Data.IsVersionAtLeast(2, 0, 0, 0);
    var entry_room = Data.GeneralInfo.RoomOrder[0].Resource;
    var object_list = entry_room.GameObjects;
    //Intentionally set to false, we're just setting up the object
    //Remind me to make adding to the room automatically an option later
    bool add_to_room = false;
    if (gms2)
    {
        UndertaleRoom.Layer target_layer = null;
        foreach (var layer in entry_room.Layers)
        {
            if (layer.LayerType == UndertaleRoom.LayerType.Instances)
            {
                foreach (var layer_obj in layer.InstancesData.Instances)
                {
                    if (layer_obj.ObjectDefinition == obj)
                    {
                        add_to_room = false;
                        break;
                    }
                }
                if (!add_to_room)
                {
                    break;
                }
                if (target_layer == null || target_layer.LayerDepth > layer.LayerDepth)
                {
                    target_layer = layer;
                }
            }
        }
        if (add_to_room)
        {
            if (target_layer == null)
            {
                uint layer_id = 0;
                foreach (var room in Data.Rooms)
                {
                    foreach (var layer in room.Layers)
                    {
                        if (layer.LayerId > layer_id)
                        {
                            layer_id = (uint)layer.LayerId;
                        }
                    }
                }
                target_layer = new UndertaleRoom.Layer();
                target_layer.LayerName = Data.Strings.MakeString("Persistent_Object_Layer");
                target_layer.Data = new UndertaleRoom.Layer.LayerInstancesData();
                target_layer.LayerType = UndertaleRoom.LayerType.Instances;
                target_layer.LayerDepth = -1000;
                target_layer.LayerId = layer_id;
                target_layer.IsVisible = true;
                entry_room.Layers.Add(target_layer);
            }
            var obj_to_add = new UndertaleRoom.GameObject();
            obj_to_add.InstanceID = Data.GeneralInfo.LastObj++;
            obj_to_add.ObjectDefinition = obj;
            obj_to_add.X = 0;
            obj_to_add.Y = 0;
            target_layer.InstancesData.Instances.Add(obj_to_add);
            object_list.Add(obj_to_add);
        }
    }
    else
    {
        foreach (var room_obj in object_list)
        {
            if (room_obj.ObjectDefinition == obj)
            {
                add_to_room = false;
                break;
            }
        }
        if (add_to_room)
        {
            var obj_to_add = new UndertaleRoom.GameObject();
            obj_to_add.InstanceID = Data.GeneralInfo.LastObj++;
            obj_to_add.ObjectDefinition = obj;
            obj_to_add.X = 0;
            obj_to_add.Y = 0;
            object_list.Add(obj_to_add);
        }
    }
}