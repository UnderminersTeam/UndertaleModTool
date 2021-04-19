// Disables Dogcheck for all Undertale and Deltarune versions.
// Made by Grossley.

EnsureDataLoaded();

ScriptMessage(@"This script disables Dogcheck for
all Undertale and Deltarune versions.");

// Removes the invoking of the dog check script and the actual check itself from "gml_Script_scr_load".
var scr_load = Data.Code.ByName("gml_Script_scr_load");
string scr_load_code = scr_load.Disassemble(Data.Variables, Data.CodeLocals.For(scr_load));
if ((Data.GeneralInfo.Name.Content == "NXTALE") || (Data.GeneralInfo.Name.Content == "UNDERTALE")) 
{
    scr_load_code = scr_load_code.Replace("00414: call.i scr_dogcheck(argc=0)", "");
    scr_load_code = scr_load_code.Replace("00416: popz.v", "");
    scr_load_code = scr_load_code.Replace("00417: push.v self.dogcheck", "");
    scr_load_code = scr_load_code.Replace("00419: pushi.e 1", "");
    scr_load_code = scr_load_code.Replace("00420: cmp.i.v EQ", "");
    scr_load_code = scr_load_code.Replace("00421: bf func_end", "");
}
else if (Data.GeneralInfo.DisplayName.Content == "SURVEY_PROGRAM")
{
    scr_load_code = scr_load_code.Replace("01015: pop.v.v self.__loadedroom", "");
    scr_load_code = scr_load_code.Replace("01017: call.i scr_dogcheck(argc=0)", "");
    scr_load_code = scr_load_code.Replace("01019: conv.v.b", "");
    scr_load_code = scr_load_code.Replace("01020: bf 01024", "");
    scr_load_code = scr_load_code.Replace("01021: pushi.e 131", "");
}
else if (Data.GeneralInfo.DisplayName.Content == "DELTARUNE Chapter 1")
{
    scr_load_code = scr_load_code.Replace("01527: pop.v.v self.__loadedroom", "");
    scr_load_code = scr_load_code.Replace("01529: call.i scr_dogcheck(argc=0)", "");
    scr_load_code = scr_load_code.Replace("01531: conv.v.b", "");
    scr_load_code = scr_load_code.Replace("01532: bf 01536", "");
    scr_load_code = scr_load_code.Replace("01533: pushi.e 131", "");
}
else
{
    ScriptError("This script can only be used with\nUndertale or Deltarune.", "Not Undertale or Deltarune");
    return;
}

scr_load.Replace(Assembler.Assemble(scr_load_code, Data));

// Done.
ChangeSelection(Data.Scripts.ByName("scr_load", true)?.Code); // Show.
ScriptMessage(@"Dogcheck is now disabled.");
