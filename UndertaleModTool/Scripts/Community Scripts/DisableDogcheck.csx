// Disables Dogcheck for all Undertale and Deltarune versions.
// Made by Grossley.

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


ScriptMessage(@"This script disables Dogcheck for
all Undertale and Deltarune versions.");

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = new Underanalyzer.Decompiler.DecompileSettings();

// Removes the invoking of the dog check script and the actual check itself from "gml_Script_scr_load".
var scr_load = "gml_Script_scr_load";
if (Data.GeneralInfo.Name.Content == "NXTALE" || Data.GeneralInfo.Name.Content.StartsWith("UNDERTALE")) 
{
    ReplaceTextInGML(scr_load, @"scr_dogcheck();
if (dogcheck == 1)", "", false, false, globalDecompileContext, decompilerSettings);
}
else if (Data.GeneralInfo.DisplayName.Content == "SURVEY_PROGRAM" || Data.GeneralInfo.DisplayName.Content == "DELTARUNE Chapter 1")
{
    ReplaceTextInGML(scr_load, @"scr_dogcheck()", "0", false, false, globalDecompileContext, decompilerSettings);
}
else
{
    ScriptError("This script can only be used with\nUndertale or Deltarune.", "Not Undertale or Deltarune");
    return;
}

// Done.
ChangeSelection(Data.Scripts.ByName("scr_load", true)?.Code); // Show.
ScriptMessage(@"Dogcheck is now disabled.");
