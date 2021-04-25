if (sprite_index == spr_movedogeheada)
    return;
if (specialx == 1)
{
    alarm[2] = 2
    if (global.mnfight == 2)
        alarm[2] = (round(random(10)) + 8)
    else
    {
        if instance_exists(blcon)
        {
            with (blcon)
                instance_destroy()
        }
        if instance_exists(blconwd)
        {
            with (blconwd)
                instance_destroy()
        }
        return;
        specialx = 0
        alarm[2] = -1
    }
    gg = floor(random(3))
    if instance_exists(blcon)
    {
        with (blcon)
            instance_destroy()
    }
    if instance_exists(blconwd)
    {
        with (blconwd)
            instance_destroy()
    }
    if (gg == 0)
    {
        xx = ((x - random((sprite_width / 2))) - 40)
        yy = (y + random((sprite_height / 3)))
    }
    if (gg == 1)
    {
        xx = ((x + random((sprite_width / 3))) + 100)
        yy = (y + random((sprite_height / 3)))
    }
    if (gg == 2)
    {
        xx = ((x + random(sprite_width)) - (sprite_width / 2))
        yy = (y - 18)
    }
    blcon = instance_create(xx, yy, obj_blconsm)
    blcon.depth = 14
    blcon.sprite_index = spr_blcontiny
    if (gg == 0)
        global.msg[0] = scr_gettext("obj_movedogehead_185")
    if (gg == 1)
        global.msg[0] = scr_gettext("obj_movedogehead_186")
    if (gg == 2)
        global.msg[0] = scr_gettext("obj_movedogehead_187")
    global.msg[1] = scr_gettext("obj_movedogehead_188")
    global.typer = 2
    blconwd = instance_create((blcon.x + 15), (blcon.y + 10), OBJ_NOMSCWRITER)
    blconwd.depth = 13
}
