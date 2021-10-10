﻿// Makes it possible to switch debug mode on and off using F1

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

ScriptMessage("Toggle debug mode with F1\nby krzys-h, Kneesnap");

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f1, Data.Strings, Data.Code, Data.CodeLocals);

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
ScriptMessage("F1 will now toggle debug mode.");

