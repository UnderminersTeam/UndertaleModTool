// By Creepersbane
// v1.0 - 08/15/2020

EnsureDataLoaded();

ScriptMessage("Adds a Gaster Blaster (+ generator) that isn't tied to Sans.\nOnly useful for modders.\nCreated by BenjaminUrquhart for Cairns.");

// Blaster
var obj_safeblaster = Data.GameObjects.ByName("obj_safeblaster");

if (obj_safeblaster == null)
{
    obj_safeblaster = new UndertaleGameObject()
    {
        Name = Data.Strings.MakeString("obj_safeblaster"),
        Sprite = Data.Sprites.ByName("spr_gasterblaster")
    };
    Data.GameObjects.Add(obj_safeblaster);
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

importGroup.QueueReplace(obj_safeblaster.EventHandlerFor(EventType.Create, Data), @"
con = 0
idealx = 200
idealy = 200
idealrot = 90
image_xscale = 1
image_yscale = 1
image_speed = 0
skip = 1
pause = 8
col_o = 0
bt = 0
btimer = 0
fade = 1
terminal = 10
bb = 0
bbsiner = 0
dmg = 9

beamsfx = caster_load(""music/sfx/sfx_rainbowbeam_1.ogg"")
beamsfx_a = caster_load(""music/sfx/sfx_a_gigatalk.ogg"")
beam_up_sfx = caster_load(""music/sfx/sfx_segapower.ogg"")

");


importGroup.QueueReplace(obj_safeblaster.EventHandlerFor(EventType.Draw, Data), @"

draw_sprite_ext(sprite_index, image_index, x, y, image_xscale, image_yscale, image_angle, image_blend, image_alpha)
if(con == 0) {
    caster_play(beam_up_sfx, 0.8, 1.2)
    con = 1
}
if ((con == 1) && (skip == 0))
{
    x += floor(((idealx - x) / 3))
    y += floor(((idealy - y) / 3))
    if (x < idealx)
        x += 1
    if (y < idealy)
        y += 1
    if (x > idealx)
        x -= 1
    if (y > idealy)
        y -= 1
    if (abs((x - idealx)) < 3)
        x = idealx
    if (abs((y - idealy)) < 3)
        y = idealy
    if ((abs((x - idealx)) < 0.1) && (abs((y - idealy)) < 0.1))
    {
        con = 2
        alarm[4] = 2
    }
}
if ((con == 1) && (skip == 1))
{
    x += floor(((idealx - x) / 3))
    y += floor(((idealy - y) / 3))
    if (x < idealx)
        x += 1
    if (y < idealy)
        y += 1
    if (x > idealx)
        x -= 1
    if (y > idealy)
        y -= 1
    if (abs((x - idealx)) < 3)
        x = idealx
    if (abs((y - idealy)) < 3)
        y = idealy
    image_angle += floor(((idealrot - image_angle) / 3))
    if (image_angle < idealrot)
        image_angle += 1
    if (image_angle > idealrot)
        image_angle -= 1
    if (abs((image_angle - idealrot)) < 3)
        image_angle = idealrot
    if ((abs((x - idealx)) < 0.1) && ((abs((y - idealy)) < 0.1) && (abs((idealrot - image_angle)) < 0.01)))
    {
        con = 4
        alarm[4] = pause
    }
}
if (con == 3)
{
    image_angle += floor(((idealrot - image_angle) / 3))
    if (image_angle < idealrot)
        image_angle += 1
    if (image_angle > idealrot)
        image_angle -= 1
    if (abs((image_angle - idealrot)) < 3)
        image_angle = idealrot
    if (abs((idealrot - image_angle)) < 0.01)
    {
        con = 4
        alarm[4] = pause
    }
}
if (con == 5)
{
    con = 6
    alarm[4] = 4
}
if (con == 6)
    image_index += 1
if (con == 7)
{
    if (image_index == 4)
        image_index = 5
    else if (image_index == 5)
        image_index = 4
    direction = (idealrot + 90)
    if (btimer == 0)
    {
        caster_play(beamsfx, 0.8, 1.2)
        caster_play(beamsfx_a, 0.6, 1.2)
    }
    btimer += 1
    if (btimer < 5)
    {
        speed += 1
        bt += floor(((35 * image_xscale) / 4))
    }
    else
        speed += 4
    if (btimer > (5 + terminal))
    {
        bt *= 0.8
        fade -= 0.1
        draw_set_alpha(fade)
        if (bt <= 2)
            instance_destroy()
    }
    if (x < (-sprite_width))
        speed = 0
    if (x > (room_width + sprite_width))
        speed = 0
    if (y > (room_height + sprite_height))
        speed = 0
    if (x < (-sprite_height))
        speed = 0
    bbsiner += 1
    bb = ((sin((bbsiner / 1.5)) * bt) / 4)
    xx = (lengthdir_x(70, (image_angle - 90)) * (image_xscale / 2))
    yy = (lengthdir_y(70, (image_angle - 90)) * (image_xscale / 2))
    rr = (random(2) - random(2))
    rr2 = (random(2) - random(2))
    xxx = lengthdir_x(1000, (image_angle - 90))
    yyy = lengthdir_y(1000, (image_angle - 90))
    draw_set_color(c_white)
    draw_line_width(((x + xx) + rr), ((y + yy) + rr2), ((x + xxx) + rr), ((y + yyy) + rr2), (bt + bb))
    xxa = (lengthdir_x(50, (image_angle - 90)) * (image_xscale / 2))
    yya = (lengthdir_y(50, (image_angle - 90)) * (image_xscale / 2))
    xxb = (lengthdir_x(60, (image_angle - 90)) * (image_xscale / 2))
    yyb = (lengthdir_y(60, (image_angle - 90)) * (image_xscale / 2))
    draw_line_width(((x + xx) + rr), ((y + yy) + rr2), ((x + xxa) + rr), ((y + yya) + rr2), ((bt / 2) + bb))
    draw_line_width(((x + xx) + rr), ((y + yy) + rr2), ((x + xxb) + rr), ((y + yyb) + rr2), ((bt / 1.25) + bb))
    nx_factor = lengthdir_x(1, image_angle)
    ny_factor = lengthdir_y(1, image_angle)

    if ((col_o == 1) && (fade >= 0.8))
    {
        for (cl = 0; cl < 4; cl += 1)
        {
            if collision_line(((x + xx) - (((nx_factor * bt) / 2) * (cl / 4))), ((y + yy) - (((ny_factor * bt) / 2) * (cl / 4))), ((x + xxx) - (((nx_factor * bt) / 2) * (cl / 4))), ((y + yyy) - (((ny_factor * bt) / 2) * (cl / 4))), obj_heart, 0, 1)
                scr_damagestandard_x()
        }
        for (cl = 0; cl < 4; cl += 1)
        {
            if collision_line(((x + xx) + (((nx_factor * bt) / 2) * (cl / 4))), ((y + yy) + (((ny_factor * bt) / 2) * (cl / 4))), ((x + xxx) + (((nx_factor * bt) / 2) * (cl / 4))), ((y + yyy) + (((ny_factor * bt) / 2) * (cl / 4))), obj_heart, 0, 1)
                scr_damagestandard_x()
        }
    }
    if (col_o == 0)
        col_o = 1
    draw_set_alpha(1)
}

");

importGroup.QueueReplace(obj_safeblaster.EventHandlerFor(EventType.Alarm, (uint)4, Data), "con += 1");

// Removes unneeded caster_free calls from earlier versions
importGroup.QueueReplace(obj_safeblaster.EventHandlerFor(EventType.Destroy, Data), "");


// Generator
var obj_safebl_gen = Data.GameObjects.ByName("obj_safebl_gen");

if (obj_safebl_gen == null)
{
    obj_safebl_gen = new UndertaleGameObject()
    {
        Name = Data.Strings.MakeString("obj_safebl_gen")
    };
    Data.GameObjects.Add(obj_safebl_gen);
}

importGroup.QueueReplace(obj_safebl_gen.EventHandlerFor(EventType.Create, Data), @"
type = 0
num = 0
");

var obj_gasterbl_gen = Data.GameObjects.ByName("obj_gasterbl_gen");
UndertaleCode codeToDecompile = obj_gasterbl_gen.EventHandlerFor(EventType.Alarm, (uint)0, Data);
string decomp = GetDecompiledText(codeToDecompile, null, new Underanalyzer.Decompiler.DecompileSettings());
importGroup.QueueReplace(obj_safebl_gen.EventHandlerFor(EventType.Alarm, (uint)0, Data), 
                         decomp.Replace("obj_gasterblaster", "obj_safeblaster"));

importGroup.Import();

ScriptMessage("Done! Use obj_safeblaster & obj_safebl_gen where you would normally use obj_gasterblaster & obj_safebl_gen.\nYou can set the amount of damage an individual blaster would do by setting the 'dmg' variable on the blaster instance (defaults to 9)");


