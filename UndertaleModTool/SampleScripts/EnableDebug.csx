// Enables debug mode
EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}

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
