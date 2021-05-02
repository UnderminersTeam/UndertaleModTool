// Disables Dogcheck for all Undertale and Deltarune versions.
// Made by Grossley.

EnsureDataLoaded();

ScriptMessage(@"This script disables Dogcheck for
all Undertale and Deltarune versions.");

// Removes the invoking of the dog check script and the actual check itself from "gml_Script_scr_load".
var scr_load = "gml_Script_scr_load";
if (Data.GeneralInfo.Name.Content == "NXTALE" || Data.GeneralInfo.Name.Content.StartsWith("UNDERTALE")) 
{
    ReplaceTextInGML(scr_load, @"scr_dogcheck()
if (dogcheck == 1)", "");
}
else if (Data.GeneralInfo.DisplayName.Content == "SURVEY_PROGRAM" || Data.GeneralInfo.DisplayName.Content == "DELTARUNE Chapter 1")
{
    ReplaceTextInGML(scr_load, @"scr_dogcheck()", "0");
}
else
{
    ScriptError("This script can only be used with\nUndertale or Deltarune.", "Not Undertale or Deltarune");
    return;
}

// Done.
ChangeSelection(Data.Scripts.ByName("scr_load", true)?.Code); // Show.
ScriptMessage(@"Dogcheck is now disabled.");
