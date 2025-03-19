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

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true
};

// Removes the invoking of the dog check script and the actual check itself from "gml_Script_scr_load".
UndertaleCode scr_load = Data.Scripts.ByName("scr_load", true)?.Code;
if (Data.GeneralInfo.Name.Content == "NXTALE" || Data.GeneralInfo.Name.Content.StartsWith("UNDERTALE")) 
{
    importGroup.QueueFindReplace(scr_load, 
        """
        scr_dogcheck();
        if (dogcheck == 1)
        """, 
        "");
}
else if (Data.GeneralInfo.DisplayName.Content == "SURVEY_PROGRAM" || Data.GeneralInfo.DisplayName.Content == "DELTARUNE Chapter 1")
{
    importGroup.QueueFindReplace(scr_load, "scr_dogcheck()", "0");
}
else
{
    ScriptError("This script can only be used with\nUndertale or Deltarune Chapter 1.", "Not Undertale or Deltarune");
    return;
}

importGroup.Import();

// Done.
ChangeSelection(scr_load); // Show.
ScriptMessage(@"Dogcheck is now disabled.");
