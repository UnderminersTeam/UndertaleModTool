ScriptMessage("Press F12 to reload the current JSON");
var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f12, Data.Strings, Data.Code, Data.CodeLocals);
code.ReplaceGML(@"
scr_84_lang_load()
", Data);
ChangeSelection(code);
ScriptMessage("Patched!");