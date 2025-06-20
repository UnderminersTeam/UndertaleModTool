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

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true
};
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", "global.monstertype[0] = 47", "");
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", "global.monstertype[1] = 0", "");
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", "global.monstertype[2] = 0", "");
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", @"global.batmusic = caster_load(""music/battle1.ogg"")", "");
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", "global.actfirst = 0", "");
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", "global.extraintro = 0", "");
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", "global.battlelv = 0", "");
importGroup.QueueFindReplace("gml_Script_scr_battlegroup", "global.msc = 0", "");
importGroup.QueuePrepend("gml_Script_scr_battlegroup", 
    @"
    global.monstertype[0] = 47
    global.monstertype[1] = 0
    global.monstertype[2] = 0
    global.batmusic = caster_load(""music/battle1.ogg"")
    global.actfirst = 0
    global.extraintro = 0
    global.battlelv = 0
    global.msc = 0
    ");
importGroup.Import();