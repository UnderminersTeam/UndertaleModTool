// Makes it possible to switch debug mode on and off using F1

EnsureDataLoaded();

string internalName = Data.GeneralInfo.Name.Content;
string displayName = Data.GeneralInfo.DisplayName.Content;
if (internalName != "NXTALE" && !internalName.StartsWith("UNDERTALE"))
{
    ScriptError("Unsupported game version.");
    return;
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f1, Data);
importGroup.QueueReplace(code,
    """
    if (global.debug) 
    {
        global.debug = 0;
    }
    else
    {
        global.debug = 1;
    }
    """);

importGroup.Import();

ChangeSelection(code);
ScriptMessage("F1 will now toggle debug mode.");

