// Makes it possible to switch debug mode on and off using F12

EnsureDataLoaded();
ScriptMessage("Toggle debug mode with F12\nby krzys-h, Kneesnap");


var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f12, Data.Strings, Data.Code, Data.CodeLocals);

// Toggle global.debug.
code.ReplaceGML(@"
if (global.debug) {
    global.debug = false;
} else{
    global.debug = true;
}", Data);

// Deltarune.
var scr_debug = Data.Scripts.ByName("scr_debug")?.Code;
if (scr_debug != null) // Deltarune. scr_debug will always return false for global.debug, so we modify it to return global.debug
    scr_debug.ReplaceGML(@"return global.debug;", Data);

ChangeSelection(code);
ScriptMessage("F12 will now toggle debug mode.");

