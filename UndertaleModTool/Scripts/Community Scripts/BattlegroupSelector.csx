EnsureDataLoaded();

string? name = Data?.GeneralInfo?.DisplayName?.Content?.ToLower();

if (name == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (name == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}


if (!name.StartsWith("undertale") && name != "nxtale") {
    ScriptError("This script can only be used on UNDERTALE.");
    return;
}

var selector = Data.GameObjects.ByName("obj_battlegroup_input");

if(selector == null) {
    selector = new UndertaleGameObject() { Name = Data.Strings.MakeString("obj_battlegroup_input") };
    Data.GameObjects.Add(selector);
}

var gms2 = Data.IsGameMaker2();

var XVIEW = gms2 ? "__view_get(0, 0)" : "view_xview[0]";
var YVIEW = gms2 ? "__view_get(1, 0)" : "view_yview[0]";
var WVIEW = gms2 ? "__view_get(2, 0)" : "view_wview[0]";
var HVIEW = gms2 ? "__view_get(3, 0)" : "view_hview[0]";

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

importGroup.QueueReplace(selector.EventHandlerFor(EventType.Create, Data), @"
battlegroup = 0
digits = 0
depth = -1000
");

importGroup.QueueReplace(selector.EventHandlerFor(EventType.Draw, Data), @"
if (view_current != 0)
    return;

draw_set_alpha(1)
if (keyboard_check_pressed(vk_return) || keyboard_check_pressed(vk_home)) {
    if (global.battlegroup == 0) {
        global.interact = 0
        instance_destroy()
        return;
    }
    if (global.battlegroup == 47) {
        global.flag[16] = 1
        global.flag[99] = 0
        global.border = 12
    }
    else {
        global.flag[16] = 0
        global.border = 0
    }
    instance_create(0, 0, obj_battler)
}

if instance_exists(obj_battler) {
    instance_destroy()
    return;
}

x = XVIEW + 4
y = YVIEW + HVIEW - 4
if (keyboard_check_pressed(vk_backspace) && digits > 0) {
    battlegroup /= 10
    battlegroup = floor(battlegroup)
    digits--
}
var i = 0
while (i < 10 && digits < 3) {
    if keyboard_check_pressed(ord(""0"") + i) {
        battlegroup *= 10
        battlegroup += i
        digits++
    }
    i++
}
str = string(floor(battlegroup))

draw_set_color(c_white)
draw_set_font(fnt_maintext)
draw_rectangle(x, y, (x + 30), (y + 1), 0)
draw_rectangle(x, y - string_height(str) - 4, x, y + 1, 0)

draw_set_color(c_lime)
draw_text(x + 4, y - string_height(str) - 4, str)

global.battlegroup = battlegroup
".Replace("XVIEW", XVIEW).Replace("YVIEW", YVIEW).Replace("WVIEW", WVIEW).Replace("HVIEW", HVIEW));

var mainchara = Data.GameObjects.ByName("obj_mainchara");

for(var i = 0; i < 5; i++) {
    importGroup.QueueReplace(
        mainchara.EventHandlerFor(EventType.KeyPress, (uint)(48 + i), Data),
        "if global.debug == 1 && !instance_exists(obj_battlegroup_input) global.filechoice = " + i
    );
}

importGroup.QueueReplace(mainchara.EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_home, Data), @"
if (global.debug == 1 && global.interact == 0 && !instance_exists(obj_battler) && !instance_exists(obj_battlegroup_input)) {
    global.interact = 1
    keyboard_clear(vk_home)
    instance_create(0, 0, obj_battlegroup_input)
}
");

importGroup.Import();

ScriptMessage("Done! Press 'Home' to open the battlegroup selector.");
