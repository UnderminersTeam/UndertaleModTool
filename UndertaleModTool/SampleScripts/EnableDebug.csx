// Enables debug mode
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

bool enable = ScriptQuestion("Debug Manager by krzys-h and Kneesnap\n\nYes = Enable Debug\nNo = Disable Debug");

var scr_debug = Data.Scripts.ByName("scr_debug")?.Code;
if (scr_debug != null) // Deltarune debug check script.
{
    scr_debug.ReplaceGML((enable ? "return global.debug;" : "return false;"), Data);
    ChangeSelection(scr_debug); // Show.
    ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");
    return;
}
var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART", true)?.Code;
if (SCR_GAMESTART != null) // Undertale debug check script.
{
    ReplaceTextInGML("gml_Script_SCR_GAMESTART", "global.debug = ", "global.debug = " + (enable ? "true;" : "false;") + "//");
    ChangeSelection(SCR_GAMESTART); // Show.
    ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");
    return;
}
