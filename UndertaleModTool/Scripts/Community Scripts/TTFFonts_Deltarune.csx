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


// Remove all current fonts
// This is necessary because I need to add fonts under IDs that are normally used by these resources
// TODO: Japanese fonts won't work at all because I didn""t add support for that
Data.Fonts.Clear();

var obj_time = Data.GameObjects.ByName("obj_time");

Data.Functions.EnsureDefined("font_add", Data.Strings);
obj_time.EventHandlerFor(EventType.Create, Data).AppendGML(@"
// NOTE: According to GMS documentation the font ranges are ignored with ttf fonts, and that seems to be indeed the case
font_add(""wingding.ttf"", 8, false, false, 32, 127);
font_add(""8bitoperator_jve.ttf"", 10, false, false, 32, 127);
font_add(""8bitoperator_jve.ttf"", 8, false, false, 32, 127);
font_add(""CryptOfTomorrow.ttf"", 6, false, false, 32, 127);
font_add(""DotumChe.ttf"", 8, true, false, 32, 127);
font_add(""DotumChe.ttf"", 12, true, false, 32, 127);
font_add(""hachicro.ttf"", 10, true, false, 32, 127);
font_add(""Mars Needs Cunnilingus.ttf"", 9, false, false, 32, 127);
font_add(""comic.ttf"", 10, true, false, 32, 127);
font_add(""PAPYRUS.TTF"", 8, true, false, 32, 127);
", Data);

ChangeSelection(obj_time);
