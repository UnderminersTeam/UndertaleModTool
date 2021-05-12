// Enables debug mode
EnsureDataLoaded();
bool enable = ScriptQuestion("Debug Manager by krzys-h and Kneesnap\n\nYes = Enable Debug\nNo = Disable Debug");

var scr_debug = Data.Scripts.ByName("scr_debug")?.Code;
if (scr_debug != null) // Deltarune debug check script.
{
    scr_debug.ReplaceGML(@"return global.debug;", Data);
    ChangeSelection(scr_debug); // Show.
    ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");
    return;
}
var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART", true)?.Code;
if (SCR_GAMESTART != null) // Undertale debug check script.
{
    ReplaceTextInGML("gml_Script_SCR_GAMESTART", "global.debug = 0", "global.debug = 1");
    ChangeSelection(SCR_GAMESTART); // Show.
    ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");
    return;
}
