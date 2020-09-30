// Sets up NXTALE to run well on PC.

EnsureDataLoaded();

if (Data.GeneralInfo.Name.Content != "NXTALE")
{
    ScriptError("This script can only be used with\nThe Nintendo Switch version of Undertale.", "Not NXTALE");
    return;
}

// Enables borders and disables interpolation. It does this by making platform-specifc code run on desktop.
var obj_time_Draw_77 = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.PostDraw, Data.Strings, Data.Code, Data.CodeLocals);
string obj_time_Draw_77_code = obj_time_Draw_77.Disassemble(Data.Variables, Data.CodeLocals.For(obj_time_Draw_77));
obj_time_Draw_77_code = obj_time_Draw_77_code.Replace("00002: pushi.e 3", "00002: pushi.e 1");
obj_time_Draw_77.Replace(Assembler.Assemble(obj_time_Draw_77_code, Data));

// This enables Mad Mew Mew's entrance.
var obj_kitchenchecker_Create_0 = Data.GameObjects.ByName("obj_kitchenchecker").EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals);
string obj_kitchenchecker_Create_0_code = obj_kitchenchecker_Create_0.Disassemble(Data.Variables, Data.CodeLocals.For(obj_kitchenchecker_Create_0));
obj_kitchenchecker_Create_0_code = obj_kitchenchecker_Create_0_code.Replace("00092: pushi.e 4", "00092: pushi.e 1");
obj_kitchenchecker_Create_0.Replace(Assembler.Assemble(obj_kitchenchecker_Create_0_code, Data));

var obj_kitchenchecker_Alarm_2 = Data.GameObjects.ByName("obj_kitchenchecker").EventHandlerFor(EventType.Alarm, 2, Data.Strings, Data.Code, Data.CodeLocals);
string obj_kitchenchecker_Alarm_2_code = obj_kitchenchecker_Alarm_2.Disassemble(Data.Variables, Data.CodeLocals.For(obj_kitchenchecker_Alarm_2));
obj_kitchenchecker_Alarm_2_code = obj_kitchenchecker_Alarm_2_code.Replace("00091: pushi.e 4", "00091: pushi.e 1");
obj_kitchenchecker_Alarm_2.Replace(Assembler.Assemble(obj_kitchenchecker_Alarm_2_code, Data));

var gml_Object_obj_npc_room_Create_0 = Data.Code.ByName("gml_Object_obj_npc_room_Create_0");
string gml_Object_obj_npc_room_Create_0_code = gml_Object_obj_npc_room_Create_0.Disassemble(Data.Variables, Data.CodeLocals.For(gml_Object_obj_npc_room_Create_0));
gml_Object_obj_npc_room_Create_0_code = gml_Object_obj_npc_room_Create_0_code.Replace("00727: pushi.e 4", "00727: pushi.e 9999");
gml_Object_obj_npc_room_Create_0_code = gml_Object_obj_npc_room_Create_0_code.Replace("00728: cmp.i.v NEQ", "00728: cmp.i.v GTE");
gml_Object_obj_npc_room_Create_0_code = gml_Object_obj_npc_room_Create_0_code.Replace("00732: pushi.e 5", "00732: pushi.e 9999");
gml_Object_obj_npc_room_Create_0_code = gml_Object_obj_npc_room_Create_0_code.Replace("00733: cmp.i.v NEQ", "00733: cmp.i.v GTE");
gml_Object_obj_npc_room_Create_0.Replace(Assembler.Assemble(gml_Object_obj_npc_room_Create_0_code, Data));

// Done.
ScriptMessage(@"NXTALE Enabler by Kneesnap

NOTE: You're not done yet!

Copy 'mus_mewmew.ogg', 'mus_sfx_dogseal.ogg', and 'DELTARUNE.exe'
into the folder you will save this game archive.
Use the DELTARUNE runner to run Undertale.

Also if you have the Steam version, you may need to put all the files there instead.");