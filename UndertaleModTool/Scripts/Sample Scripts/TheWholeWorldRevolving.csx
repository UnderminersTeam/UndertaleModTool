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

if (!Data.IsGameMaker2()) 
{
    ScriptMessage("This is not a GMS2 game.");
    return;
}

var gml_Object_obj_time_Step_1 = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Step, EventSubtypeStep.BeginStep, Data);

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
importGroup.QueueAppend(gml_Object_obj_time_Step_1, @"
__view_set(4, 0, ((__view_get(e__VW_append.Angle, 0) + (sin((global.time / 15)) / 2)) + 1));
__view_set(11, 0, (__view_get(e__VW_append.XPort, 0) + (cos((global.time / 10)) * 1.5)));
__view_set(12, 0, (__view_get(e__VW_append.YPort, 0) + (sin((global.time / 10)) * 1.5)));

enum e__VW_append
{
    Angle = 4,
    XPort = 11,
    YPort = 12
}
");
importGroup.Import();

ChangeSelection(gml_Object_obj_time_Step_1);
ScriptMessage("* The whole world is spinning, spinning\n\nJEVIL HARDMODE CHAOS LOADED\nI CAN DO ANYTHING\nby krzys_h, Kneesnap");
