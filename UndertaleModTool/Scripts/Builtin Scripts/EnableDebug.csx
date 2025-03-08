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

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true
};

var scr_debug = Data.Scripts.ByName("scr_debug")?.Code;
if (scr_debug is not null) // Deltarune debug check script.
{
    importGroup.QueueReplace(scr_debug, "global.debug = " + (enable ? "1" : "0") + ";" + "\r\n" + "return global.debug;");
    importGroup.Import();
    ChangeSelection(scr_debug); // Show.
    ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");
    return;
}

var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART", true)?.Code;
if (SCR_GAMESTART is not null) // Undertale debug check script.
{
    importGroup.QueueFindReplace(SCR_GAMESTART, "global.debug = ", "global.debug = " + (enable ? "1;" : "0;") + "//");
    importGroup.Import();
    ChangeSelection(SCR_GAMESTART); // Show.
    ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");
    return;
}
