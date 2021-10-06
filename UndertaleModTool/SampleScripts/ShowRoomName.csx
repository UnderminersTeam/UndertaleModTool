// Adds room ID and name display under the debug mode timer display

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}

ScriptMessage("Show room name and ID in debug mode\nby krzys_h, Kneesnap");

Data.Functions.EnsureDefined("room_get_name", Data.Strings); // required for Deltarune

var gml_Object_obj_time_Draw_64 = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals);
gml_Object_obj_time_Draw_64.AppendGML(@"
if (global.debug)
{
    draw_set_color(0xFFFF);
    draw_text(10, 30, room);
    draw_text(50, 30, room_get_name(room));
}", Data);

ChangeSelection(gml_Object_obj_time_Draw_64);
ScriptMessage("Patched!");