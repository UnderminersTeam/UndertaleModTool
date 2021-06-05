ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.monstertype[0] = 47", "", true, false);
ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.monstertype[1] = 0", "", true, false);
ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.monstertype[2] = 0", "", true, false);
ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.batmusic = caster_load(""music/battle1.ogg"")", "", true, false);
ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.actfirst = 0", "", true, false);
ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.extraintro = 0", "", true, false);
ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.battlelv = 0", "", true, false);
ReplaceTextInGML("gml_Script_scr_battlegroup", @"global.msc = 0", "", true, false);
string prepend = @"global.monstertype[0] = 47
global.monstertype[1] = 0
global.monstertype[2] = 0
global.batmusic = caster_load(""music/battle1.ogg"")
global.actfirst = 0
global.extraintro = 0
global.battlelv = 0
global.msc = 0
";
ImportGMLString("gml_Script_scr_battlegroup", prepend + GetDecompiledText("gml_Script_scr_battlegroup"));
