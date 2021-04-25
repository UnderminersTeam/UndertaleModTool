if (active == true)
{
    if instance_exists(obj_mainchara)
    {
        if (cn == 0 && global.interact == 0 && obj_mainchara.x > x)
        {
            global.interact = 1
            undyne.image_alpha = 0
            cn = 0.1
            ou = instance_create(0, 0, obj_musfadeout)
            ou.fadespeed = 0.05
            snd_play(snd_arrow)
            ar = instance_create((x + 30), -220, obj_npc_marker)
            ar.visible = true
            ar.sprite_index = spr_undynespear
            ar.image_angle = -90
            ar.vspeed = 24
            ar.friction = -0.3
        }
    }
    if (cn == 0.1)
    {
        if (ar.y > 160)
        {
            ar.image_angle = 0
            ar.sprite_index = spr_undynespear_stabbed
            ar.y += ar.vspeed
            ar.vspeed = 0
            snd_play(snd_impact)
            instance_create(0, 0, obj_flasher)
            scr_shake(4, 4, 2)
            cn = 0.2
            alarm[4] = 50
            instance_create(40, 180, obj_solidsmall)
            instance_create(40, 200, obj_solidsmall)
            instance_create(40, 220, obj_solidsmall)
            yad = 0
            repeat (5)
            {
                ar2 = instance_create((50 - yad), (210 - (yad * 6)), obj_npc_marker)
                ar2.visible = true
                ar2.sprite_index = spr_undynespear_stabbed
                yad += 2
            }
        }
    }
    if (cn == 1.2)
    {
        if (ar.image_alpha > 0.02)
            ar.image_alpha -= 0.1
        if (view_yview[0] > 10)
            view_yview -= 5
        else
            cn = 2
    }
    if (cn == 2)
    {
        caster_play(ushock, 1, 1)
        cn = 3
        alarm[4] = 30
    }
    if (cn == 3)
    {
        if (undyne.image_alpha < 1)
            undyne.image_alpha += 0.05
    }
    if (cn == 4)
    {
        global.currentsong = usong
        caster_loop(global.currentsong, 1, 1)
        global.interact = 0
        global.flag[17] = 1
        global.flag[77] = global.armor
        cn = 5
    }
    if (cn == 5)
    {
        undyne.vspeed = 4
        undyne.image_speed = 0.25
        cn = 6
        alarm[4] = 6
    }
    if (cn == 7)
    {
        undyne.image_speed = 0
        undyne.vspeed = 0
        sp = instance_create(undyne.x, undyne.y, obj_uspeargen)
        cn = 8
    }
    if (cn > 7)
    {
        sp.x = (undyne.x + 24)
        sp.y = (undyne.y + 20)
    }
    if (cn == 8)
    {
        if (stk.image_alpha < 1)
            stk.image_alpha += 0.1
        if (stopper == 1)
        {
            alarm[4] = -1
            cn = 10
            stopper = 0
        }
        if (obj_mainchara.x > (undyne.x + 80) && undyne.x < room_width)
        {
            undyne.hspeed = 3
            undyne.image_speed = 0.2
            alarm[4] = 20
            cn = 9
        }
        if (obj_mainchara.x < (undyne.x - 60))
        {
            undyne.hspeed = -3
            undyne.image_speed = 0.2
            alarm[4] = 20
            cn = 9
        }
    }
    if (cn == 10)
    {
        if (obj_mainchara.x > (undyne.x + 80) || obj_mainchara.x < (undyne.x - 60))
            cn = 8
        else
        {
            undyne.image_speed = 0
            undyne.hspeed = 0
            cn = 8
        }
    }
    if (cn == 9)
    {
        if (alarm[4] <= 0)
            cn = 10
    }
}
