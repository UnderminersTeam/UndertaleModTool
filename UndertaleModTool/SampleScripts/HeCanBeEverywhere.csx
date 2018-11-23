EnsureDataLoaded();

ScriptMessage("HeCanBeEverywhere mod by krzys_h\nVersion 1");

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
obj_chaseenemy.EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
; 25 = Jevil
pushi.e 25
pop.v.i self.myencounter
pushi.e spr_joker_enemy
pop.v.i self.touchsprite
pushi.e spr_joker_enemy
pop.v.i self.sprite_index
pushi.e 0
pop.v.i self.chasetype
pushi.e 1
pop.v.i self.pacetype
", Data));

var obj_testoverworldenemy = Data.GameObjects.ByName("obj_testoverworldenemy");
var obj_testoverworldenemy_User0 = obj_testoverworldenemy.EventHandlerFor(EventType.Other, EventSubtypeOther.User0, Data.Strings, Data.Code, Data.CodeLocals);
obj_testoverworldenemy_User0.Append(Assembler.Assemble(@"
pushi.e snd_joker_laugh0
conv.i.v
call.i snd_play(argc=1)
popz.v
", Data));
obj_testoverworldenemy_User0.Instructions[3].JumpOffset += 5; // Ugly hack to redirect exit of if()

var obj_joker_User10 = obj_joker.EventHandlerFor(EventType.Other, EventSubtypeOther.User10, Data.Strings, Data.Code, Data.CodeLocals);
for (int i = 0; i < obj_joker_User10.Instructions.Count; i++)
{
    if (obj_joker_User10.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && obj_joker_User10.Instructions[i].Destination.Target.Name.Content == "skipvictory")
    {
        obj_joker_User10.Instructions[i-1].Value = (short)0;
    }
    if (obj_joker_User10.Instructions[i].Kind == UndertaleInstruction.Opcode.Call && obj_joker_User10.Instructions[i].Function.Target.Name.Content == "snd_free_all")
    {
        obj_joker_User10.Instructions[i].Function.Target = Data.Functions.ByName("scr_84_debug"); // just redirect it to something useless
    }
}
obj_joker_User10.Append(Assembler.Assemble(@"
pushvar.v self.room
pushi.e room_cc_joker
cmp.i.v NEQ
bf func_end

pushi.e obj_joker_body
conv.i.v
call.i instance_destroy(argc=1)
popz.v
", Data));

Data.Variables.EnsureDefined("jevilizer", UndertaleInstruction.InstanceType.Global, false, Data.Strings, Data);

var scr_encountersetup = Data.Scripts.ByName("scr_encountersetup");
scr_encountersetup.Code.Append(Assembler.Assemble(@"
pushvar.v self.argument0
pushi.e 25
cmp.i.v EQ
bf func_end

; Jevil seen is the same as visited the Jevil room
; so that the shopkeeper interaction works
pushi.e -5
pushi.e 241
push.v [array]flag
pushi.e 1
cmp.i.v LT
bf noflag

pushi.e 1
pushi.e -5
pushi.e 241
pop.v.i [array]flag

noflag: push.s ""* JEVIL BLOCKED YOUR WAY""
conv.s.v
push.s ""* ANOTHER ONE OF THESE""
conv.s.v
push.s ""* IT'S JUST A SIMPLE CHAOS""
conv.s.v
push.s ""* THE JOKER CARD REPLACED ANOTHER ENEMY""
conv.s.v
push.s ""* HE REALLY CAN BE ANYTHING""
conv.s.v
call.i choose(argc=5)
pushi.e -5
pushi.e 0
pop.v.v [array]battlemsg

pushvar.v self.room
pushi.e room_cc_joker
cmp.i.v EQ
bf func_end

; some randomness would be nice
pushi.e 1
conv.i.v
call.i random(argc=1)
pop.v.v global.jevilizer

pushglb.v global.jevilizer
push.d 0.95
cmp.d.v GT
bf notachance

push.s ""* JEVIL DECIDED TO GIVE YOU A CHANCE""
conv.s.v
push.s ""* THAT WAS TOO UNFAIR, TO BE HONEST""
conv.s.v
call.i choose(argc=2)
pushi.e -5
pushi.e 0
pop.v.v [array]battlemsg

b conti

notachance: pushglb.v global.jevilizer
push.d 0.75
cmp.d.v GT
bf notreally

pushi.e obj_joker
pushi.e -5
pushi.e 0
pop.v.i [array]monsterinstancetype
pushi.e 20
pushi.e -5
pushi.e 0
pop.v.i [array]monstertype
push.v self.xx
pushi.e 480
add.i.v
pushi.e -5
pushi.e 0
pop.v.v [array]monstermakex
push.v self.yy
pushi.e 20
add.i.v
pushi.e -5
pushi.e 0
pop.v.v [array]monstermakey
pushi.e obj_joker
pushi.e -5
pushi.e 1
pop.v.i [array]monsterinstancetype
pushi.e 20
pushi.e -5
pushi.e 1
pop.v.i [array]monstertype
push.v self.xx
pushi.e 460
add.i.v
pushi.e -5
pushi.e 1
pop.v.v [array]monstermakex
push.v self.yy
pushi.e 220
add.i.v
pushi.e -5
pushi.e 1
pop.v.v [array]monstermakey

push.s ""* HOW ABOUT WE MAKE IT A TWO?""
conv.s.v
push.s ""* IT'S THE DOUBLE CHAOS""
conv.s.v
call.i choose(argc=2)
pushi.e -5
pushi.e 0
pop.v.v [array]battlemsg

b conti

notreally: pushi.e obj_joker
pushi.e -5
pushi.e 0
pop.v.i [array]monsterinstancetype
pushi.e 20
pushi.e -5
pushi.e 0
pop.v.i [array]monstertype
push.v self.xx
pushi.e 480
add.i.v
pushi.e -5
pushi.e 0
pop.v.v [array]monstermakex
push.v self.yy
pushi.e 20
add.i.v
pushi.e -5
pushi.e 0
pop.v.v [array]monstermakey
pushi.e obj_joker
pushi.e -5
pushi.e 1
pop.v.i [array]monsterinstancetype
pushi.e 20
pushi.e -5
pushi.e 1
pop.v.i [array]monstertype
push.v self.xx
pushi.e 500
add.i.v
pushi.e -5
pushi.e 1
pop.v.v [array]monstermakex
push.v self.yy
pushi.e 120
add.i.v
pushi.e -5
pushi.e 1
pop.v.v [array]monstermakey
pushi.e obj_joker
pushi.e -5
pushi.e 2
pop.v.i [array]monsterinstancetype
pushi.e 20
pushi.e -5
pushi.e 2
pop.v.i [array]monstertype
push.v self.xx
pushi.e 460
add.i.v
pushi.e -5
pushi.e 2
pop.v.v [array]monstermakex
push.v self.yy
pushi.e 220
add.i.v
pushi.e -5
pushi.e 2
pop.v.v [array]monstermakey

push.s ""* NONONONONONO RUN!!!! YOU ARE GONNA DIE""
conv.s.v
push.s ""* IT'S THE TRIPLE CHAOS""
conv.s.v
push.s ""* THREE JEVILS BLOCKED YOUR WAY""
conv.s.v
push.s ""* BIRDS ARE SINGING, JEVILS ARE CHAOSING""
conv.s.v
call.i choose(argc=4)
pushi.e -5
pushi.e 0
pop.v.v [array]battlemsg

b conti

conti: pushglb.v global.jevilizer
push.d 0.15
cmp.d.v LT
bf func_end

push.s ""* LOOKS LIKE THE WORLD IS LITERALLY REVOLVING""
conv.s.v
push.s ""* WHO KEEPS SPINNING THE WORLD AROUND""
conv.s.v
push.s ""* JEVIL TURNED ON THE CAROUSEL FOR REAL""
conv.s.v
call.i choose(argc=3)
pushi.e -5
pushi.e 0
pop.v.v [array]battlemsg
", Data));

var scr_text = Data.Scripts.ByName("scr_text");
scr_text.Code.Append(Assembler.Assemble(@"
; if (argument0 == 405 or argument0 == 410)
pushvar.v self.argument0
pushi.e 405
cmp.i.v EQ
bt is_ok
pushvar.v self.argument0
pushi.e 410
cmp.i.v EQ
b is_not_ok
is_ok: push.e 1
is_not_ok: bf func_end

pushi.e -5
pushi.e 241
push.v [array]flag
pushi.e 5
cmp.i.v LT
bf noflag
pushi.e 5
pushi.e -5
pushi.e 241
pop.v.i [array]flag

; scr_ralface(0, 0)
noflag: pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
call.i scr_ralface(argc=2)
popz.v

push.s ""* I think JEVIL might have escaped.../""
pushi.e -5
pushi.e 1
pop.v.s [array]msg

; scr_susface(2, 2)
pushi.e 2
conv.i.v
pushi.e 2
conv.i.v
call.i scr_susface(argc=2)
popz.v

push.s ""* You THINK so? I KNOW he escaped! Didn't you notice him yet?/""
pushi.e -5
pushi.e 3
pop.v.s [array]msg

push.s ""* He is everywhere!/""
pushi.e -5
pushi.e 4
pop.v.s [array]msg

; scr_ralface(5, 0)
pushi.e 0
conv.i.v
pushi.e 5
conv.i.v
call.i scr_ralface(argc=2)
popz.v

push.s ""* Do you think he is still inside too?/""
pushi.e -5
pushi.e 6
pop.v.s [array]msg

; scr_noface(7)
pushi.e 7
conv.i.v
call.i scr_noface(argc=1)
popz.v

push.s ""\TJ* I JUST WANTED TO PLAY A GAME^1, GAME./""
pushi.e -5
pushi.e 8
pop.v.s [array]msg

push.s ""* YOU CAME BECAUSE YOU WANT TO PLAY WITH ME^1, ME./""
pushi.e -5
pushi.e 9
pop.v.s [array]msg

push.s ""* A MARVELLOUS CHAOS IS ABOUT TO BREAK FREE./""
pushi.e -5
pushi.e 10
pop.v.s [array]msg

push.s ""* WON'T YOU LET YOURSELF INSIDE?/""
pushi.e -5
pushi.e 11
pop.v.s [array]msg

push.s ""* (You will regret this. Don't say I didn't warn you.)/%""
pushi.e -5
pushi.e 12
pop.v.s [array]msg

pushi.e obj_event_room
pushenv func_end
start_pushenv: pushi.e 10
pop.v.i self.con
popenv start_pushenv
", Data));

var spr_jokerdoor = Data.Sprites.ByName("spr_jokerdoor");
spr_jokerdoor.Textures[0].Texture = spr_jokerdoor.Textures[2].Texture;
spr_jokerdoor.Textures[1].Texture = spr_jokerdoor.Textures[2].Texture;


var obj_doorX_musfade = Data.GameObjects.ByName("obj_doorX_musfade");
obj_doorX_musfade.EventHandlerFor(EventType.Other, EventSubtypeOther.User9, Data.Strings, Data.Code, Data.CodeLocals).Replace(Assembler.Assemble(@"
pushvar.v self.room
pushi.e room_cc_prison_prejoker
cmp.i.v EQ
bf 00000

; already beaten
pushi.e -5
pushi.e 241
push.v [array]flag
pushi.e 6
cmp.i.v LT
bf 00000

pushi.e 666
pop.v.i global.typer
pushi.e 0
pop.v.i global.fc

push.s ""* WHAT ARE YOU DOING? ALREADY FORGOT?/""
pushi.e -5
pushi.e 0
pop.v.s [array]msg

push.s ""* YOUR CHOICES DON'T MATTER/""
pushi.e -5
pushi.e 1
pop.v.s [array]msg

push.s ""* SINCE WHEN DOES THIS GAME GIVE YOU A CHOICE?/%""
pushi.e -5
pushi.e 2
pop.v.s [array]msg

pushi.e obj_dialoguer
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
call.i instance_create(argc=3)
popz.v

call.i instance_destroy(argc=0)
popz.v

exit.v

00000: pushi.e 3
00001: pop.v.i global.interact
00003: pushi.e 14
00004: conv.i.v
00005: pushi.e 0
00006: conv.i.v
00007: pushi.e -5
00008: pushi.e 1
00009: push.v [array]currentsong
00011: call.i mus_volume(argc=3)
00013: popz.v
00014: pushi.e 137
00015: conv.i.v
00016: pushi.e 0
00017: conv.i.v
00018: pushi.e 0
00019: conv.i.v
00020: call.i instance_create(argc=3)
00022: popz.v
00023: push.v self.touched
00025: pushi.e 0
00026: cmp.i.v EQ
00027: bf func_end
00028: pushi.e 15
00029: pushi.e -1
00030: pushi.e 2
00031: pop.v.i [array]alarm
00033: pushi.e 14
00034: pushi.e -1
00035: pushi.e 3
00036: pop.v.i [array]alarm
00038: pushi.e 1
00039: pop.v.i self.touched
", Data));

var obj_jokerbattleevent = Data.GameObjects.ByName("obj_jokerbattleevent");
obj_jokerbattleevent.EventHandlerFor(EventType.Step, EventSubtypeStep.Step, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e obj_battlecontroller
conv.i.v
call.i instance_exists(argc=1)
conv.v.b
bf not_in_battle

pushglb.v global.jevilizer
push.d 0.15
cmp.d.v LT
bf not_in_battle

; 4 = view_angle
pushi.e 0
conv.i.v
pushi.e 4
conv.i.v
call.i __view_get(argc=2)
pushglb.v global.time
push.d 15
div.d.v
call.i sin(argc=1)
push.d 2
div.d.v
add.v.v
push.d 1
add.d.v
pushi.e 0
conv.i.v
pushi.e 4
conv.i.v
call.i __view_set(argc=3)
popz.v

; 11 = view_xport
pushi.e 0
conv.i.v
pushi.e 11
conv.i.v
call.i __view_get(argc=2)
pushglb.v global.time
push.d 10
div.d.v
call.i cos(argc=1)
push.d 1.5
mul.d.v
add.v.v
pushi.e 0
conv.i.v
pushi.e 11
conv.i.v
call.i __view_set(argc=3)
popz.v

; 12 = view_yport
pushi.e 0
conv.i.v
pushi.e 12
conv.i.v
call.i __view_get(argc=2)
pushglb.v global.time
push.d 10
div.d.v
call.i sin(argc=1)
push.d 1.5
mul.d.v
add.v.v
pushi.e 0
conv.i.v
pushi.e 12
conv.i.v
call.i __view_set(argc=3)
popz.v

not_in_battle: push.v self.con
pushi.e 4
cmp.i.v EQ
bf no_detour

pushi.e 1337
pop.v.i self.con

no_detour: push.v self.con
pushi.e 1338
cmp.i.v EQ
bf not_1338

pushi.e snd_joker_laugh1
conv.i.v
call.i snd_play(argc=1)
popz.v

pushi.e 35
pop.v.i global.typer
pushi.e 0
pop.v.i global.fc

push.s ""* UEE HEE^1!&* VISITORS^1, VISITORS^1!&* NOW WE CAN PLAY^1, PLAY!/""
pushi.e -5
pushi.e 0
pop.v.s [array]msg

push.s ""* I CAN BE IN MULTIPLE PLACES AT ONCE TODAY!/""
pushi.e -5
pushi.e 1
pop.v.s [array]msg

push.s ""* BUT TO HAVE SOME MORE CHAOS^1, CHAOS/""
pushi.e -5
pushi.e 2
pop.v.s [array]msg

push.s ""* WOULDN'T IT BE MORE FUN TO HAVE MULTIPLE OF ME IN ONE PLACE?/%""
pushi.e -5
pushi.e 3
pop.v.s [array]msg

pushi.e obj_dialoguer
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
call.i instance_create(argc=3)
popz.v

pushi.e 1339
pop.v.i self.con

not_1338: push.v self.con
pushi.e 1339
cmp.i.v EQ
bf 1339_set_0
call.i d_ex(argc=0)
conv.v.b
not.b
b 1339_check_complete
1339_set_0: push.e 0
1339_check_complete: bf not_1339

push.d 0.5
pop.v.d self.image_speed

pushi.e 119
conv.i.v
call.i snd_play(argc=1)
popz.v

pushi.e 1340
pop.v.i self.con

pushi.e 10
pushi.e -1
pushi.e 4
pop.v.i [array]alarm

not_1339: push.v self.con
pushi.e 1341
cmp.i.v EQ
bf not_1341

pushi.e snd_dooropen
conv.i.v
call.i snd_play(argc=1)
popz.v

pushi.e snd_locker
conv.i.v
call.i snd_play(argc=1)
popz.v

pushi.e 1342
pop.v.i self.con

pushi.e 10
pushi.e -1
pushi.e 4
pop.v.i [array]alarm

not_1341: push.v self.con
pushi.e 1343
cmp.i.v EQ
bf not_1343

pushi.e snd_save
conv.i.v
call.i snd_play(argc=1)
popz.v

; You will regret this
call.i scr_save(argc=0)
popz.v

pushi.e 1344
pop.v.i self.con

pushi.e 10
pushi.e -1
pushi.e 4
pop.v.i [array]alarm

not_1343: push.v self.con
pushi.e 1345
cmp.i.v EQ
bf func_end

pushi.e 0
pop.v.i self.image_speed

pushi.e 30
pop.v.i global.typer
pushi.e 0
pop.v.i global.fe
pushi.e 35
pop.v.i global.typer
pushi.e 0
pop.v.i global.fc

pushi.e 0
conv.i.v
call.i scr_noface(argc=1)
popz.v

push.s ""* (Another JEVIL closed the door behind you.)/""
pushi.e -5
pushi.e 1
pop.v.s [array]msg

pushi.e 0
conv.i.v
pushi.e 2
conv.i.v
call.i scr_susface(argc=2)
popz.v

push.s ""* So that's the kinda game you wanna play^1, huh...?/""
pushi.e -5
pushi.e 3
pop.v.s [array]msg

push.s ""\E2* Then^1, I gotta warn you.../%""
pushi.e -5
pushi.e 4
pop.v.s [array]msg

pushi.e obj_dialoguer
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
call.i instance_create(argc=3)
popz.v

push.d 15.1
pop.v.d self.con
", Data));

obj_jokerbattleevent.EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushglb.v global.debug
pushi.e 1
cmp.i.v EQ
bf func_end

push.i " + 0xFFFF00.ToString() /* TODO: add this syntax to the assembler */ + @"
conv.i.v
call.i draw_set_color(argc=1)
popz.v
pushglb.v global.jevilizer
pushi.e 50
conv.i.v
pushi.e 50
conv.i.v
call.i draw_text(argc=3)
popz.v
", Data));

obj_jokerbattleevent.EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e -5
pushi.e 1
push.v [array]currentsong
call.i snd_is_playing(argc=1)
conv.v.b
not.b
bf func_end

push.s ""prejoker.ogg""
conv.s.v
call.i snd_init(argc=1)
pushi.e -5
pushi.e 0
pop.v.v [array]currentsong

push.d 0.85
conv.d.v
pushi.e 1
conv.i.v
pushi.e -5
pushi.e 0
push.v [array]currentsong
call.i mus_loop_ext(argc=3)
pushi.e -5
pushi.e 1
pop.v.v [array]currentsong

;pushi.e 0
;conv.i.v
;pushi.e 0
;conv.i.v
;pushi.e -5
;pushi.e 1
;push.v [array]currentsong
;call.i mus_volume(argc=3)
;popz.v
", Data));

var scr_roomname = Data.Scripts.ByName("scr_roomname");
scr_roomname.Code.Instructions.RemoveAt(scr_roomname.Code.Instructions.Count - 1); // push.v self.roomname
scr_roomname.Code.Instructions.RemoveAt(scr_roomname.Code.Instructions.Count - 1); // ret.v
scr_roomname.Code.Append(Assembler.Assemble(@"
pushvar.v self.argument0
pushi.e room_cc_joker
cmp.i.v EQ
bf go_ret
push.s ""CHAOS, CHAOS!""
pop.v.s self.roomname

go_ret: push.v self.roomname
ret.v
", Data));

var obj_shop1 = Data.GameObjects.ByName("obj_shop1");
var obj_shop1_Draw = obj_shop1.EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data.Strings, Data.Code, Data.CodeLocals);
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
        string id = ((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)obj_shop1_Draw.Instructions[i].Value).Resource.Content;
        if (obj_shop1_Patches.ContainsKey(id))
        {
            obj_shop1_Draw.Instructions[i + 0] = Assembler.AssembleOne(@"push.s """"", Data);
            obj_shop1_Draw.Instructions[i + 1] = Assembler.AssembleOne(@"popz.s", Data);
            obj_shop1_Draw.Instructions[i + 2] = Assembler.AssembleOne(@"push.s """"", Data);
            obj_shop1_Draw.Instructions[i + 2].Value = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>() { Resource = Data.Strings.MakeString(obj_shop1_Patches[id]) };
            obj_shop1_Draw.Instructions[i + 5] = Assembler.AssembleOne(@"pop.v.s [array]msg", Data);
        }
    }
}
obj_shop1_Draw.UpdateAddresses();
obj_shop1_Draw.Replace(Assembler.Assemble(obj_shop1_Draw.Disassemble(Data.Variables, Data.CodeLocals.For(obj_shop1_Draw)), Data)); // TODO: no idea why this is needed

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f6, Data.Strings, Data.Code, Data.CodeLocals).Replace(Assembler.Assemble(@"
pushglb.v global.debug
pushi.e 1
cmp.i.v EQ
bf func_end

pushi.e 1
pushi.e -5
pushi.e 241
pop.v.i [array]flag
", Data));

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f7, Data.Strings, Data.Code, Data.CodeLocals).Replace(Assembler.Assemble(@"
pushglb.v global.debug
pushi.e 1
cmp.i.v EQ
bf func_end

pushi.e 7
pushi.e -5
pushi.e 241
pop.v.i [array]flag
", Data));

ChangeSelection(spr_joker_enemy);

ScriptMessage("* I'M FREE!");