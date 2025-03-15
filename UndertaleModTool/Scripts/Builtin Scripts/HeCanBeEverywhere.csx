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


ScriptMessage("HeCanBeEverywhere mod by krzys_h and Kneesnap\nVersion 3");

// Create code import group
GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = new Underanalyzer.Decompiler.DecompileSettings();
UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data, globalDecompileContext, decompilerSettings)
{
    ThrowOnNoOpFindReplace = true
};

// spr_joker_main has an offset, so we need to make our own one
var spr_joker_main = Data.Sprites.ByName("spr_joker_main");
var spr_joker_enemy = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_joker_enemy"),
    Width = spr_joker_main.Width,
    Height = spr_joker_main.Height,
    MarginLeft = spr_joker_main.MarginLeft,
    MarginRight = spr_joker_main.MarginRight,
    MarginTop = spr_joker_main.MarginTop,
    MarginBottom = spr_joker_main.MarginBottom,
    IsSpecialType = spr_joker_main.IsSpecialType,
    GMS2PlaybackSpeed = spr_joker_main.GMS2PlaybackSpeed,
    GMS2PlaybackSpeedType = spr_joker_main.GMS2PlaybackSpeedType,
};
foreach (var tex in spr_joker_main.Textures)
    spr_joker_enemy.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = tex.Texture });
foreach (var col in spr_joker_main.CollisionMasks)
    spr_joker_enemy.CollisionMasks.Add(col);
Data.Sprites.Add(spr_joker_enemy);

var obj_joker = Data.GameObjects.ByName("obj_joker");

var obj_chaseenemy = Data.GameObjects.ByName("obj_chaseenemy");
importGroup.QueueAppend(obj_chaseenemy.EventHandlerFor(EventType.Create, Data), @"
myencounter = 25; // Jevil.
touchsprite = spr_joker_enemy;
sprite_index = spr_joker_enemy;
chasetype = 0;
pacetype = 1;");

var obj_testoverworldenemy = Data.GameObjects.ByName("obj_testoverworldenemy");
var obj_testoverworldenemy_User0 = obj_testoverworldenemy.EventHandlerFor(EventType.Other, EventSubtypeOther.User0, Data);
importGroup.QueueAppend(obj_testoverworldenemy_User0, "snd_play(snd_joker_laugh0);");
importGroup.Import();
obj_testoverworldenemy_User0.Instructions[3].JumpOffset += 5; // Ugly hack to redirect exit of if()

var obj_joker_User10 = obj_joker.EventHandlerFor(EventType.Other, EventSubtypeOther.User10, Data);
for (int i = 0; i < obj_joker_User10.Instructions.Count; i++)
{
    if (obj_joker_User10.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && obj_joker_User10.Instructions[i].ValueVariable.Target.Name.Content == "skipvictory")
    {
        obj_joker_User10.Instructions[i-1].ValueShort = 0;
    }
    if (obj_joker_User10.Instructions[i].Kind == UndertaleInstruction.Opcode.Call && obj_joker_User10.Instructions[i].ValueFunction.Target.Name.Content == "snd_free_all")
    {
        obj_joker_User10.Instructions[i].ValueFunction.Target = Data.Functions.ByName("scr_84_debug"); // just redirect it to something useless
    }
}
importGroup.QueueAppend(obj_joker_User10, @"
if (room != room_cc_joker)
    instance_destroy(obj_joker_body);");

var scr_encountersetup = Data.Scripts.ByName("scr_encountersetup");
importGroup.QueueAppend(scr_encountersetup.Code, @"
if (argument0 == 25) {
    if (global.flag[241] < 1)
        global.flag[241] = 1;
    
    global.battlemsg[0] = choose(""* HE REALLY CAN BE ANYTHING"", ""* THE JOKER CARD REPLACED ANOTHER ENEMY"", ""* IT'S JUST A SIMPLE CHAOS"", ""* ANOTHER ONE OF THESE"", ""* JEVIL BLOCKED YOUR WAY"");
    if (room == room_cc_joker) {
        global.jevilizer = random(1);
        if (global.jevilizer > 0.95)
            global.battlemsg[0] = choose(""* THAT WAS TOO UNFAIR, TO BE HONEST"", ""* JEVIL DECIDED TO GIVE YOU A CHANCE"");
        else if (global.jevilizer > 0.75) {
            global.monsterinstancetype[0] = 284;
            global.monstertype[0] = 20;
            global.monstermakex[0] = (xx + 480);
            global.monstermakey[0] = (yy + 20);
            global.monsterinstancetype[1] = 284;
            global.monstertype[1] = 20;
            global.monstermakex[1] = (xx + 460);
            global.monstermakey[1] = (yy + 220);
            global.battlemsg[0] = choose(""* IT'S THE DOUBLE CHAOS"", ""* HOW ABOUT WE MAKE IT A TWO?"");
        } else {
            global.monsterinstancetype[0] = 284;
            global.monstertype[0] = 20;
            global.monstermakex[0] = (xx + 480);
            global.monstermakey[0] = (yy + 20);
            global.monsterinstancetype[1] = 284;
            global.monstertype[1] = 20;
            global.monstermakex[1] = (xx + 500);
            global.monstermakey[1] = (yy + 120);
            global.monsterinstancetype[2] = 284;
            global.monstertype[2] = 20;
            global.monstermakex[2] = (xx + 460);
            global.monstermakey[2] = (yy + 220);
            global.battlemsg[0] = choose(""* BIRDS ARE SINGING, JEVILS ARE CHAOSING"", ""* THREE JEVILS BLOCKED YOUR WAY"", ""* IT'S THE TRIPLE CHAOS"", ""* NONONONONONO RUN!!!! YOU ARE GONNA DIE"");
        }
        if (global.jevilizer < 0.15)
            global.battlemsg[0] = choose(""* JEVIL TURNED ON THE CAROUSEL FOR REAL"", ""* WHO KEEPS SPINNING THE WORLD AROUND"", ""* LOOKS LIKE THE WORLD IS LITERALLY REVOLVING"");
    }
}");

var scr_text = Data.Scripts.ByName("scr_text");
importGroup.QueueAppend(scr_text.Code, @"
if ((argument0 == 405) || (argument0 == 410)) {
    if (global.flag[241] < 5)
        global.flag[241] = 5;
    scr_ralface(0, 0);
    global.msg[1] = ""* I think JEVIL might have escaped.../"";
    scr_susface(2, 2);
    global.msg[3] = ""* You THINK so? I KNOW he escaped! Didn't you notice him yet?/"";
    global.msg[4] = ""* He is everywhere!/"";
    scr_ralface(5, 0);
    global.msg[6] = ""* Do you think he is still inside too?/"";
    scr_noface(7);
    global.msg[8] = ""\\TJ* I JUST WANTED TO PLAY A GAME^1, GAME./"";
    global.msg[9] = ""* YOU CAME BECAUSE YOU WANT TO PLAY WITH ME^1, ME./"";
    global.msg[10] = ""* A MARVELLOUS CHAOS IS ABOUT TO BREAK FREE./"";
    global.msg[11] = ""* WON'T YOU LET YOURSELF INSIDE?/"";
    global.msg[12] = ""* (You will regret this. Don't say I didn't warn you.)/%"";
    with(obj_event_room)
        con = 10;
}");

var spr_jokerdoor = Data.Sprites.ByName("spr_jokerdoor");
spr_jokerdoor.Textures[0].Texture = spr_jokerdoor.Textures[2].Texture;
spr_jokerdoor.Textures[1].Texture = spr_jokerdoor.Textures[2].Texture;


var obj_doorX_musfade = Data.GameObjects.ByName("obj_doorX_musfade");
importGroup.QueueReplace(obj_doorX_musfade.EventHandlerFor(EventType.Other, EventSubtypeOther.User9, Data), @"
if (room == room_cc_prison_prejoker && global.flag[241] < 6) {
    global.typer = 666;
    global.fc = 0;
    global.msg[0] = ""* WHAT ARE YOU DOING? ALREADY FORGOT?/"";
    global.msg[1] = ""* YOUR CHOICES DON'T MATTER/"";
    global.msg[2] = ""* SINCE WHEN DOES THIS GAME GIVE YOU A CHOICE?/%"";
    instance_create(0, 0, obj_dialoguer);
    instance_destroy();
    return;
}
global.interact = 3;
mus_volume(global.currentsong[1], 0, 14);
instance_create(0, 0, obj_fadeout);
if (touched == 0) {
    alarm[2] = 15;
    alarm[3] = 14;
    touched = 1;
}");

var obj_jokerbattleevent = Data.GameObjects.ByName("obj_jokerbattleevent");
importGroup.QueueAppend(obj_jokerbattleevent.EventHandlerFor(EventType.Step, EventSubtypeStep.Step, Data), @"
if (instance_exists(obj_battlecontroller) && global.jevilizer < 0.15) {
    __view_set(4, 0, ((__view_get(4, 0) + (sin((global.time / 15)) / 2)) + 1));
    __view_set(11, 0, (__view_get(11, 0) + (cos((global.time / 10)) * 1.5)));
    __view_set(12, 0, (__view_get(12, 0) + (sin((global.time / 10)) * 1.5)));
}
if (con == 4)
    con = 1337;
if (con == 1338) {
    snd_play(snd_joker_laugh1);
    global.typer = 35;
    global.fc = 0;
    global.msg[0] = ""* UEE HEE^1!&* VISITORS^1, VISITORS^1!&* NOW WE CAN PLAY^1, PLAY!/"";
    global.msg[1] = ""* I CAN BE IN MULTIPLE PLACES AT ONCE TODAY!/"";
    global.msg[2] = ""* BUT TO HAVE SOME MORE CHAOS^1, CHAOS/"";
    global.msg[3] = ""* WOULDN'T IT BE MORE FUN TO HAVE MULTIPLE OF ME IN ONE PLACE?/%"";
    instance_create(0, 0, obj_dialoguer)
    con = 1339
}
if ((con == 1339) && (!d_ex())) {
    image_speed = 0.5;
    snd_play(snd_joker_laugh0);
    con = 1340;
    alarm[4] = 10;
}
if (con == 1341) {
    snd_play(snd_dooropen);
    snd_play(snd_locker);
    con = 1342;
    alarm[4] = 10;
}
if (con == 1343) {
    snd_play(snd_save);
    scr_save();
    con = 1344;
    alarm[4] = 10;
}
if (con == 1345) {
    image_speed = 0;
    global.typer = 30;
    global.fe = 0;
    global.typer = 35;
    global.fc = 0;
    scr_noface(0);
    global.msg[1] = ""* (Another JEVIL closed the door behind you.)/"";
    scr_susface(2, 0);
    global.msg[3] = ""* So that's the kinda game you wanna play^1, huh...?/"";
    global.msg[4] = ""\\E2* Then^1, I gotta warn you.../%"";
    instance_create(0, 0, obj_dialoguer);
    con = 15.1;
}");

importGroup.QueueAppend(obj_jokerbattleevent.EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data), @"
if (global.debug) {
    draw_set_color(0xFFFF);
    draw_text(50, 50, global.jevilizer);
}");

importGroup.QueueAppend(obj_jokerbattleevent.EventHandlerFor(EventType.Create, Data), @"
if (!snd_is_playing(global.currentsong[1])) {
    global.currentsong[0] = snd_init(""prejoker.ogg"");
    global.currentsong[1] = mus_loop_ext(global.currentsong[0], 1, 0.85);
}");

var scr_roomname = Data.Scripts.ByName("scr_roomname");
importGroup.QueueFindReplace(scr_roomname.Code, "return roomname;", @"
if (argument0 == room_cc_joker)
    roomname = ""CHAOS, CHAOS!"";
return roomname;");

var obj_shop1 = Data.GameObjects.ByName("obj_shop1");
var obj_shop1_Draw = obj_shop1.EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data);
var obj_shop1_Patches = new Dictionary<string, string>()
{
    { "obj_shop1_slash_Draw_0_gml_474_0", @"\E3* ... I see^1.&* After all the trouble I went through to lock him up^1, he managed to escape?/" },
    { "obj_shop1_slash_Draw_0_gml_480_0", @"\E0* Please, go and figure out how he managed to escape./" },
    { "obj_shop1_slash_Draw_0_gml_482_0", @"\E0* I hope you will not regret going there.../%" },

    { "obj_shop1_slash_Draw_0_gml_461_0", @"\E2* Well, Jevil didn't have any problem opening it./%" },

    { "obj_shop1_slash_Draw_0_gml_501_0", @"\E0* I see..^1. perhaps you three may truly be ""CHEATERS"" after all.../" },
    { "obj_shop1_slash_Draw_0_gml_502_0", @"\E1* There is literally no way you could have done that/" },
    { "obj_shop1_slash_Draw_0_gml_503_0", @"\E0* But one day soon.../" },
};
for (int i = 0; i < obj_shop1_Draw.Instructions.Count; i++)
{
    if (obj_shop1_Draw.Instructions[i].Kind == UndertaleInstruction.Opcode.Push && obj_shop1_Draw.Instructions[i].Type1 == UndertaleInstruction.DataType.String)
    {
        string id = obj_shop1_Draw.Instructions[i].ValueString.Resource.Content;
        if (obj_shop1_Patches.ContainsKey(id))
        {
            obj_shop1_Draw.Instructions[i + 0] = Assembler.AssembleOne(@"push.s """"", Data);
            obj_shop1_Draw.Instructions[i + 1] = Assembler.AssembleOne(@"popz.s", Data);
            obj_shop1_Draw.Instructions[i + 2] = Assembler.AssembleOne(@"push.s """"", Data);
            obj_shop1_Draw.Instructions[i + 2].ValueString = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>() { Resource = Data.Strings.MakeString(obj_shop1_Patches[id]) };
            obj_shop1_Draw.Instructions[i + 5] = Assembler.AssembleOne(@"pop.v.s [array]global.msg", Data);
        }
    }
}
obj_shop1_Draw.UpdateLength();
obj_shop1_Draw.Replace(Assembler.Assemble(obj_shop1_Draw.Disassemble(Data.Variables, Data.CodeLocals?.For(obj_shop1_Draw)), Data)); // TODO: no idea why this is needed

importGroup.QueueReplace(Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f6, Data), @"
if (global.debug == 1)
    global.flag[241] = 1;");

importGroup.QueueReplace(Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f7, Data), @"
if (global.debug == 1)
    global.flag[241] = 7;");

importGroup.QueueReplace(Data.GameObjects.ByName("obj_jokerbg_triangle_real").EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data), @"
if (room == room_cc_joker) {
    on = (instance_number(obj_joker) != 0);
    rotspeed = (instance_number(obj_joker) * instance_number(obj_joker));
}");

importGroup.QueueFindReplace("gml_Object_obj_joker_Draw_0", "global.flag[241] = 6", "");

importGroup.Import();

ChangeSelection(spr_joker_enemy);

ScriptMessage("* I'M FREE!\n\nDebug mode may be required for this to work.");