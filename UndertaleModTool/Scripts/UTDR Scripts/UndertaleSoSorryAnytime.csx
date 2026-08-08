// Removes the date and time check for the So Sorry
// Encounter

using System.IO;
using System;
using UndertaleModLib.Util;

EnsureDataLoaded();

ScriptMessage(@"This script disables the So Sorry encounter datetime check for
all Undertale versions.");

if (Data.GeneralInfo.Name.Content == "NXTALE" || Data.GeneralInfo.Name.Content.StartsWith("UNDERTALE"))
{
    UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
    importGroup.QueueFindReplace("gml_Object_obj_artclass_sign_Step_0", "(current_month == 10 && current_day == 10)", "(true)");
    importGroup.QueueFindReplace("gml_Object_obj_artclass_sign_Step_0", "(current_hour == 8 || current_hour == 20)", "(true)");
    importGroup.Import();
}
else
{
    ScriptError("This script can only be used with Undertale!", "Not Undertale");
    return;
}

// Done.
ChangeSelection(Data.Code.ByName("gml_Object_obj_artclass_sign_Step_0")); // Show.
ScriptMessage("So Sorry Encounter now available at any point.");