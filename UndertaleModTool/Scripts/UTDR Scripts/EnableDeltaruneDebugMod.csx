EnsureDataLoaded();

ScriptMessage("Enabling debug mode");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

var obj_initializer2 = Data.GameObjects.ByName("obj_initializer2");
if (obj_initializer2 == null)
{
    ScriptError("Could not find obj_initializer2");
    return;
}

var createCode = obj_initializer2.EventHandlerFor(EventType.Create, (uint)0, Data);
if (createCode == null)
{
    ScriptError("Could not find Create event for obj_initializer2");
    return;
}

importGroup.QueueFindReplace(createCode,
    "global.debug = 0;",
    "global.debug = 1;"
);

importGroup.Import();

ChangeSelection(createCode);

ScriptMessage("Debug mode is now permanently enabled! Coded By Cyn-ically");
