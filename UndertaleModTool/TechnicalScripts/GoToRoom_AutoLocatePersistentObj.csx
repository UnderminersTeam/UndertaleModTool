// Replaces the debug mode "Create system_information_962" option with "Go to room"

EnsureDataLoaded();
ScriptMessage("Add 'Go to room' dialog under F3\nby krzys-h, Kneesnap");

bool pers = false;
UndertaleGameObject obj_pers = null;
foreach (UndertaleGameObject obj in Data.GameObjects)
{
    if (obj.Persistent)
    {
        pers = true;
        obj_pers = obj;
        break;
    }
}
if (!pers)
{
    ScriptMessage("Impossible to run, no persistent object!");
    return;
}

var code = obj_pers.EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f3, Data.Strings, Data.Code, Data.CodeLocals);

Data.Functions.EnsureDefined("get_integer", Data.Strings);
code.ReplaceGML(@"
room_goto(get_integer(""Go to room"", room));
", Data);

ChangeSelection(code);
ScriptMessage("Patched!");