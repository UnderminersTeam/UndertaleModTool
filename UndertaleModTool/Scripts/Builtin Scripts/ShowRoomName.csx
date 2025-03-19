// Adds room ID and name display under the debug mode timer display
EnsureDataLoaded();
ScriptMessage("Show room name and ID in debug mode\nby krzys_h, Kneesnap\n");
Data.Functions.EnsureDefined("room_get_name", Data.Strings); // required for Deltarune
string displayName = Data?.GeneralInfo?.DisplayName?.Content.ToLower();
bool isDeltarune = displayName.StartsWith("deltarune chapter");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
var gml_Object_obj_time_Draw_64 = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data);
if (isDeltarune)
{
    importGroup.QueueAppend(gml_Object_obj_time_Draw_64, @"
    if (scr_debug())
    {
        draw_set_color(c_yellow);
        draw_text(10, 30, room);
        draw_text(50, 30, room_get_name(room));
    }");
}
else
{
    importGroup.QueueAppend(gml_Object_obj_time_Draw_64, @"
    if (global.debug)
    {
        draw_set_color(c_yellow);
        draw_text(10, 30, room);
        draw_text(50, 30, room_get_name(room));
    }");
}
importGroup.Import();

ChangeSelection(gml_Object_obj_time_Draw_64);
ScriptMessage("Patched!");
