EnsureDataLoaded();

if (!Data.IsGameMaker2()) 
{
    ScriptMessage("This is not a GMS2 game.");
    return;
}

var gml_Object_obj_time_Step_1 = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Step, EventSubtypeStep.BeginStep, Data.Strings, Data.Code, Data.CodeLocals);

gml_Object_obj_time_Step_1.AppendGML(@"
__view_set(4, 0, ((__view_get(e__VW.Angle, 0) + (sin((global.time / 15)) / 2)) + 1)); // 4 -> e__VW.Angle
__view_set(11, 0, (__view_get(11, 0) + (cos((global.time / 10)) * 1.5))); // 11 -> e__VW.XPort
__view_set(12, 0, (__view_get(12, 0) + (sin((global.time / 10)) * 1.5))); // 12 -> e__VW.YPort", Data);

ChangeSelection(gml_Object_obj_time_Step_1);
ScriptMessage("* The whole world is spinning, spinning\n\nJEVIL HARDMODE CHAOS LOADED\nI CAN DO ANYTHING\nby krzys_h, Kneesnap");