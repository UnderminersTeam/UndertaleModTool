// Script and mod by Agentalex9, with some help from Grossley
// Repurposed these pieces of code:
// https://www.reddit.com/r/gamemaker/comments/60qj6u/extracting_color_values_from_images/
// https://www.zackbanack.com/blog/image-to-level

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

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

string bepisCode = "bepis";

if (Data.GeneralInfo.Major < 2) // Undertale PC (GMS1)
{
    bepisCode = @"
    draw_self()
    w = (sprite_get_width(sprite_index) * image_xscale)
    h = (sprite_get_height(sprite_index) * image_yscale)
    xoff = (sprite_get_xoffset(sprite_index) * image_xscale)
    yoff = (sprite_get_yoffset(sprite_index) * image_yscale)
    x -= xoff
    y -= yoff
    surf = surface_create(w, h)
    pixel = 0
    a = 0
    r = 0
    g = 0
    b = 0
    col = c_black
    delay = 0
    surface_set_target(surf)
    draw_clear_alpha(c_black, 0)
    draw_sprite(sprite_index, image_index, xoff, yoff)
    surface_reset_target()
    buff = buffer_create(((4 * w) * h), buffer_fixed, 1)
    buffer_get_surface(buff, surf, 0, 0, 0)
    surface_free(surf)
    blarg = 0
    if (image_xscale == 2)
        blarg = 1
    else
        blarg = 2
    for (j = 0; j < h; j += blarg)
    {
        for (i = 0; i < w; i += blarg)
        {
            pixel = buffer_peek(buff, (4 * (i + (j * w))), buffer_u32)
            a = ((pixel >> 24) & 255)
            r = (pixel & 255)
            g = ((pixel >> 8) & 255)
            b = ((pixel >> 16) & 255)
            obj = noone
            if (a == 255)
            {
                obj = obj_whtpxlgrav
                col = make_colour_rgb(b, g, r)
            }
            if (obj != noone)
            {
                _obj = instance_create((x + (i * image_xscale)), (y + (j * image_yscale)), obj)
                with (_obj)
                {
                    image_blend = other.col
                    delay = floor((other.delay / 3))
                }
            }
        }
        delay += 1
    }
    buffer_delete(buff)
    instance_destroy()
    ";
}
else // Undertale Switch/Probs other consoles (GMS2)
{
    bepisCode = @"
    draw_self()
    w = (sprite_get_width(sprite_index) * image_xscale)
    h = (sprite_get_height(sprite_index) * image_yscale)
    xoff = (sprite_get_xoffset(sprite_index) * image_xscale)
    yoff = (sprite_get_yoffset(sprite_index) * image_yscale)
    x -= xoff
    y -= yoff
    surf = surface_create(w, h)
    pixel = 0
    a = 0
    r = 0
    g = 0
    b = 0
    col = c_black
    delay = 0
    surface_set_target(surf)
    draw_clear_alpha(c_black, 0)
    draw_sprite(sprite_index, image_index, xoff, yoff)
    surface_reset_target()
    buff = buffer_create(((4 * w) * h), buffer_fixed, 1)
    buffer_get_surface(buff, surf, 0, 0, 0)
    surface_free(surf)
    blarg = 0
    if (image_xscale == 2)
        blarg = 1
    else
        blarg = 2
    for (j = 0; j < h; j += blarg)
    {
        for (i = 0; i < w; i += blarg)
        {
            pixel = buffer_peek(buff, (4 * (i + (j * w))), buffer_u32)
            a = ((pixel >> 24) & 255)
            r = (pixel & 255)
            g = ((pixel >> 8) & 255)
            b = ((pixel >> 16) & 255)
            obj = noone
            if (a == 255)
            {
                obj = obj_whtpxlgrav
                col = make_colour_rgb(r, g, b)
            }
            if (obj != noone)
            {
                _obj = instance_create((x + (i * image_xscale)), (y + (j * image_yscale)), obj)
                with (_obj)
                {
                    image_blend = other.col
                    delay = floor((other.delay / 3))
                }
            }
        }
        delay += 1
    }
    buffer_delete(buff)
    instance_destroy()
    ";
}

// Change this line in the code above to a higher number for faster vaporisation and vice versa:
//
// delay = floor((other.delay / 3)) <--- This number
//

var obj_whtpxlgrav = Data.GameObjects.ByName("obj_whtpxlgrav");

if (obj_whtpxlgrav == null)
{
    ScriptError("This script only works with Undertale!", "Not Undertale");
    return;
}

importGroup.QueueReplace(obj_whtpxlgrav.EventHandlerFor(EventType.Create, Data), @"
image_speed = 0
delay = 0
");

// Remove these lines in the code below if you don't want the colour black to be excluded
// (Causes weird visual effects with the black battle background):
//
// if (image_blend == make_color_rgb(0, 0, 0))
//    instance_destroy()
//
// You can also change/copy it to exclude other colours if you need to

importGroup.QueueReplace(obj_whtpxlgrav.EventHandlerFor(EventType.Step, EventSubtypeStep.Step, Data), @"
if (image_blend == make_color_rgb(0, 0, 0))
    instance_destroy()
if (delay > 0)
    delay -= 1
else
{
    image_speed = 1
    gravity_direction = 90
    gravity = (random(0.5) + 0.2)
    hspeed = (random(4) - 2)
    delay = 9999
}
");

var obj_vaporized = Data.GameObjects.ByName("obj_vaporized");

importGroup.QueueReplace(obj_vaporized.EventHandlerFor(EventType.Create, Data), @"
sprite_index = global.monstersprite
snd_play(snd_vaporized)
");

importGroup.QueueReplace(obj_vaporized.EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data), bepisCode);

importGroup.QueueReplace(obj_vaporized.EventHandlerFor(EventType.Step, EventSubtypeStep.Step, Data), @"

");

importGroup.QueueReplace(obj_vaporized.EventHandlerFor(EventType.Alarm, (uint)0u, Data), @"

");

var obj_vaporized_new = Data.GameObjects.ByName("obj_vaporized_new");

importGroup.QueueReplace(obj_vaporized_new.EventHandlerFor(EventType.Create, Data), @"
snd_play(snd_vaporized)
");

importGroup.QueueReplace(obj_vaporized_new.EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data), bepisCode);

var obj_vaporized_old = Data.GameObjects.ByName("obj_vaporized_old");

importGroup.QueueReplace(obj_vaporized_old.EventHandlerFor(EventType.Create, Data), @"
sprite_index = global.monstersprite
line = 0
finished = 0
ht = sprite_get_height(sprite_index)
wd = sprite_get_width(sprite_index)
snd_play(snd_vaporized)
action_set_alarm(2, 0)
");

importGroup.QueueReplace(obj_vaporized_old.EventHandlerFor(EventType.Alarm, (uint)0u, Data), bepisCode);

importGroup.Import();

ChangeSelection(obj_whtpxlgrav);

ScriptMessage("Done!");