EnsureDataLoaded();

string name = Data.GeneralInfo?.Name?.Content.ToLower();
if (!name.StartsWith("undertale"))
{
    if (name == "nxtale")
    {
        ScriptError("This script does not work for the Nintendo Switch or Xbox One version of Undertale.", "Unsupported Version");
    }
    else
    {
        ScriptError("This script only works for Undertale.", "Unsupported Game");
    }
    return;
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    MainThreadAction = MainThreadAction
};

// Enable the entrance to the Dog Shrine
importGroup.QueueFindReplace("gml_Object_obj_kitchenchecker_Create_0", "global.osflavor == 4", "true");
importGroup.QueueFindReplace("gml_Object_obj_kitchenchecker_Alarm_2", "global.osflavor == 4 && ", "");

// Enable the donation box trash in Waterfall
importGroup.QueueFindReplace("gml_Object_obj_npc_room_Create_0", "global.osflavor != 4 || ", "");

// Disable dogcheck
importGroup.QueueFindReplace("gml_Object_obj_dogshrine_Step_0", "global.osflavor != 4", "false");

importGroup.Import();
