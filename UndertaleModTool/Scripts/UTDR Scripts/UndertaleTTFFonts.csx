EnsureDataLoaded();

var displayName = Data.GeneralInfo?.DisplayName?.Content.ToLower();
if (!(displayName == "undertale" || displayName == "nxtale"))
{
    ScriptError("Error 1: This script only works with Undertale!");
}


// Remove all current fonts
// This is necessary because I need to add fonts under IDs that are normally used by these resources
// TODO: Japanese fonts won't work at all because I didn't add support for that
Data.Fonts.Clear();

Data.Functions.EnsureDefined("font_add", Data.Strings); // Allow font_add.

var obj_time_Create_0 = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Create, Data);

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
importGroup.QueueAppend(obj_time_Create_0, @"
// NOTE: According to GMS documentation the font ranges are ignored with ttf fonts, and that seems to be indeed the case
font_add(""wingding.ttf"", 12, false, false, 32, 127);
font_add(""8bitoperator_jve.ttf"", 24, false, false, 32, 127);
font_add(""8bitoperator_jve.ttf"", 12, false, false, 32, 127);
font_add(""CryptOfTomorrow.ttf"", 6, false, false, 32, 127);
font_add(""DotumChe.ttf"", 12, true, false, 32, 127);
font_add(""DotumChe.ttf"", 48, true, false, 32, 127);
font_add(""hachicro.ttf"", 24, true, false, 32, 127);
font_add(""Mars Needs Cunnilingus.ttf"", 18, false, false, 32, 127);
font_add(""comic.ttf"", 10, true, false, 32, 127);
font_add(""PAPYRUS.TTF"", 8, true, false, 32, 127);
");
importGroup.Import();

ScriptMessage("Successfully externalized fonts for Undertale!");
