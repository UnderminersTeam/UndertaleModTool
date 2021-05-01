if (con == 1)
{
    mkid = instance_create((view_xview[0] - 40), obj_mainchara.y, obj_mkid_actor)
    mkid.image_speed = 0
    obj_mainchara.cutscene = true
    view_object[0] = noone
    mkid.sprite_index = mkid.rsprite
    con = 0.1
    alarm[4] = 2
    vol = caster_get_volume(global.currentsong)
    vol2 = vol
}
if (con == 1.1)
{
    con = 0.2
    alarm[4] = 2
}
if (con == 1.2)
{
    con = 1.3
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_124")
    scr_regulartext()
}
if (con == 1.3 && instance_exists(OBJ_WRITER) == 0)
{
    con = 2
    alarm[4] = 50
}
if (con == 2 && instance_exists(OBJ_WRITER) == 0)
{
    if (vol > 0)
        vol -= 0.02
    caster_set_volume(global.currentsong, vol)
    view_xview -= 2
}
if (con == 3)
{
    global.facing = 3
    caster_pause(global.currentsong)
    mkid.image_speed = 0.2
    mkid.hspeed = 1
    con = 4
    alarm[4] = 20
    if (global.tempvalue[13] == 1)
    {
        alarm[4] = 50
        con = 13
        mkid.image_speed = 0.25
        mkid.hspeed = 3
        mkid.sprite_index = mkid.rsprite
    }
}
if (con == 5)
{
    mkid.hspeed = 0
    mkid.image_speed = 0
    mkid.image_index = 0
    con = 6
    alarm[4] = 50
}
if (con == 7)
{
    mkid.hspeed = 1
    mkid.image_speed = 0.2
    con = 8
    alarm[4] = 130
}
if (con == 9)
{
    mkid.hspeed = 0
    mkid.image_speed = 0
    mkid.image_index = 0
    con = 10
    alarm[4] = 30
}
if (con == 10)
{
    mkid.sprite_index = mkid.dtsprite
    con = 11
    alarm[4] = 30
}
if (con == 12)
{
    mkid.sprite_index = mkid.rtsprite
    con = 13
    alarm[4] = 40
}
if (con == 14)
{
    mkid.hspeed = 0
    mkid.image_speed = 0
    mkid.image_index = 0
    mkid.sprite_index = mkid.rtsprite
    global.msc = 623
    global.typer = 5
    global.facechoice = 0
    con = 15
    if (murder == 1)
    {
        con = 150
        global.msc = 0
        global.msg[0] = scr_gettext("obj_monsterkidtrigger7_214")
        global.msg[1] = scr_gettext("obj_monsterkidtrigger7_215")
        global.msg[2] = scr_gettext("obj_monsterkidtrigger7_216")
        global.msg[3] = scr_gettext("obj_monsterkidtrigger7_217")
        global.msg[4] = scr_gettext("obj_monsterkidtrigger7_218")
        global.msg[5] = scr_gettext("obj_monsterkidtrigger7_219")
        global.msg[6] = scr_gettext("obj_monsterkidtrigger7_220")
        global.msg[7] = scr_gettext("obj_monsterkidtrigger7_221")
        if (global.tempvalue[13] == 1)
        {
            global.msg[0] = scr_gettext("obj_monsterkidtrigger7_225")
            global.msg[1] = scr_gettext("obj_monsterkidtrigger7_226")
            con = 157
        }
    }
    instance_create(0, 0, obj_dialoguer)
}
if (con == 15 && instance_exists(OBJ_WRITER) == 0)
{
    mkid.sprite_index = mkid.rsprite
    con = 16
    alarm[4] = 30
}
if (con == 17)
{
    mkid.hspeed = -1
    con = 18
    alarm[4] = 15
    mkid.image_speed = 0.2
}
if (con == 19)
{
    mkid.hspeed = 0
    alarm[4] = 60
    mkid.image_speed = 0
    mkid.image_index = 0
    con = 20
}
if (con == 21)
{
    mkid.sprite_index = mkid.lsprite
    mkid.hspeed = -3
    alarm[4] = 15
    mkid.image_speed = 0.5
    con = 20.1
}
if (con == 21.1)
{
    mkid.hspeed = 0
    mkid.image_index = 0
    mkid.sprite_index = spr_mkid_trip_l
    mkid.image_speed = 0
    mkiddex = 0
    con = 22
    alarm[4] = 18
}
if (con == 22)
{
    mkiddex += 0.25
    if (mkiddex >= 2)
        mkiddex = 0
    if (mkiddex < 1)
        mkid.image_index = 0
    else
        mkid.image_index = 1
}
if (con == 23)
{
    mkid.image_index = 0
    mkid.sprite_index = mkid.usprite
    mkid.hspeed = -1
    mkid.vspeed = 2
    con = 24
    alarm[4] = 15
}
if (con == 25)
{
    mkid.sprite_index = mkid.usprite
    mkid.image_speed = 0
    mkid.hspeed = 0
    mkid.vspeed = 0
    con = 26
    alarm[4] = 10
}
if (con == 27)
{
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_309")
    interact = instance_create(mkid.x, (mkid.y + 10), obj_readable_room1)
    interact.image_xscale = 0.5
    interact.x += 5
    scr_regulartext()
    con = 28
}
if (con == 28 && instance_exists(OBJ_WRITER) == 0)
{
    undyne = instance_create((view_xview[0] - 40), 82, obj_undynea_actor)
    undyne.sprite_index = undyne.rsprite
    undyne.hspeed = 2
    undyne.image_speed = 0.2
    con = 29
    alarm[4] = 20
}
if (con == 30)
{
    undyne.hspeed = 0
    undyne.image_speed = 0
    undyne.image_index = 0
    con = 31
    alarm[4] = 30
}
if (con == 32)
{
    undyne.sprite_index = spr_undyne_armraise
    snd_play(snd_spearappear)
    con = 33
    alarm[4] = 20
}
if (con == 34)
{
    undyne.sprite_index = undyne.rsprite
    global.interact = 0
    con = 35
    global.flag[17] = 1
    doorb = instance_create((view_xview - 20), (obj_mainchara.y + 10), obj_doorB)
    doora = instance_create(((view_xview + view_wview) + 20), (obj_mainchara.y + 10), obj_doorA)
    undynetimer = 0
    finaltimer = 0
    mkidtalk = 0
    charge = 0
    samex = obj_mainchara.x
}
if (con == 35)
{
    ll = 0
    finaltimer += 1
    undynetimer += 1
    if (undynetimer > 60)
    {
        undyne.hspeed = 1
        undyne.image_speed = 0.25
    }
    if (undynetimer > 75)
    {
        undyne.hspeed = 0
        undyne.image_speed = 0
        undynetimer = 0
    }
    if (finaltimer > 150 && global.interact == 0 && mkidtalk == 0 && abs((obj_mainchara.x - samex)) < 10)
    {
        mkidtalk = 1
        global.msg[0] = scr_gettext("obj_monsterkidtrigger7_379")
        scr_regulartext()
        global.interact = 1
    }
    if (mkidtalk == 1 && instance_exists(OBJ_WRITER) == 0)
    {
        global.interact = 0
        mkidtalk = 2
    }
    if (finaltimer > 500)
        ll = 1
    if (obj_mainchara.x < (undyne.x + 40))
    {
        ll = 1
        charge = 1
    }
    able = 0
    if (ll == 1 && global.interact == 0)
        able = 1
    if (ll == 1 && mkidtalk == 1)
        able = 1
    if (able == 1)
    {
        with (obj_dialoguer)
            instance_destroy()
        with (OBJ_WRITER)
            instance_destroy()
        con = 50
        global.interact = 1
    }
}
if (con == 50)
{
    undyne.sprite_index = undyne.dsprite
    undyne.hspeed = 0
    undyne.image_speed = 0
    undyne.image_index = 0
    mkid.image_speed = 0.25
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_417")
    scr_regulartext()
    con = 51
}
if (con == 51 && instance_exists(OBJ_WRITER) == 0)
{
    mkid.vspeed = 0.5
    con = 50.1
    alarm[4] = 20
}
if (con == 51.1)
{
    mkid.vspeed = 0
    con = 50.2
    alarm[4] = 30
}
if (con == 51.2)
{
    blcon = instance_create((undyne.x + 10), (undyne.y - 15), obj_cosmeticblcon)
    mkid.vspeed = 4
    con = 54
    alarm[4] = 30
}
if (con == 55)
{
    with (blcon)
        instance_destroy()
    undyne.sprite_index = spr_undyne_jump0
    con = 56
    alarm[4] = 30
}
if (con == 57)
{
    undyne.sprite_index = spr_undyne_jump1
    snd_play(snd_spearrise)
    undyne.vspeed = -8
    undyne.gravity = 0.6
    undyne.gravity_direction = 270
    undyne.hspeed = 0.5
    con = 58
    alarm[4] = 20
}
if (con == 59)
{
    snd_play(snd_fall)
    con = 60
    alarm[4] = 90
}
if (con == 61)
{
    snd_play(snd_undynestep)
    con = 62
    alarm[4] = 20
}
if (con == 63)
{
    snd_play(snd_grab)
    con = 69
    alarm[4] = 80
}
if (con == 70)
{
    global.facing = 0
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_490")
    global.msg[1] = scr_gettext("obj_monsterkidtrigger7_491")
    global.msg[2] = scr_gettext("obj_monsterkidtrigger7_492")
    global.msg[3] = scr_gettext("obj_monsterkidtrigger7_493")
    global.msg[4] = scr_gettext("obj_monsterkidtrigger7_494")
    if (charge == 1)
        global.msg[4] = scr_gettext("obj_monsterkidtrigger7_495")
    global.msg[5] = scr_gettext("obj_monsterkidtrigger7_496")
    global.msg[6] = scr_gettext("obj_monsterkidtrigger7_497")
    global.msg[7] = scr_gettext("obj_monsterkidtrigger7_498")
    global.msg[8] = scr_gettext("obj_monsterkidtrigger7_499")
    global.msg[9] = scr_gettext("obj_monsterkidtrigger7_500")
    global.msg[10] = scr_gettext("obj_monsterkidtrigger7_501")
    scr_regulartext()
    con = 71
}
if (con == 71 && instance_exists(OBJ_WRITER) == 0)
{
    global.flag[98] = 2
    con = 72
}
if (con == 72 && instance_exists(OBJ_WRITER) == 0)
{
    con = 73
    idealxview = round(((obj_mainchara.x - (view_wview[0] / 2)) + (obj_mainchara.sprite_width / 2)))
    if (idealxview >= (room_width - view_wview[0]))
        idealxview = ((room_width - view_wview[0]) - 2)
    if (view_xview[0] > idealxview)
        xdir = 0
    else
        xdir = 1
    alarm[4] = 40
}
if (con == 74)
{
    if (xdir == 1)
        view_xview[0] += 4
    else
        view_xview[0] -= 4
    if (abs((view_xview[0] - idealxview)) <= 5)
    {
        with (mkid)
            instance_destroy()
        view_object[0] = obj_mainchara
        obj_mainchara.cutscene = false
        with (doora)
            instance_destroy()
        with (doorb)
            instance_destroy()
        global.plot = 120
        caster_free(global.currentsong)
        global.interact = 0
        instance_destroy()
    }
}
if (con == 80)
{
    with (interact)
        instance_destroy()
    global.interact = 1
    con = 81
    alarm[4] = 30
}
if (con == 82)
{
    if (undyne.x > (view_xview[0] + 20))
    {
        undyne.hspeed = -1
        undyne.image_speed = 0.12
    }
    if (mkid.x <= obj_mainchara.x)
        mkid.hspeed = -0.8
    if (mkid.x > obj_mainchara.x)
        mkid.hspeed = 0.8
    mkid.vspeed = -1
    con = 83
    alarm[4] = 28
}
if (con == 84)
{
    undyne.hspeed = 0
    undyne.image_index = 0
    undyne.hspeed = 0
    mkid.vspeed = 0
    mkid.hspeed = 0
    mkid.x = round(mkid.x)
    if (obj_mainchara.x > mkid.x)
        mkid.sprite_index = mkid.rtsprite
    else
        mkid.sprite_index = mkid.ltsprite
    mkid.image_index = 0
    mkid.image_speed = 0
    con = 85
    alarm[4] = 40
}
if (con == 86)
{
    undyne.hspeed = 0.5
    undyne.image_speed = 0.25
    blcon = instance_create(mkid.x, (mkid.y - 10), obj_cosmeticblcon)
    con = 87
    alarm[4] = 16
}
if (con == 88)
{
    undyne.hspeed = 0
    undyne.image_speed = 0
    undyne.image_index = 0
    with (blcon)
        instance_destroy()
    mkid.sprite_index = mkid.lsprite
    con = 89
    alarm[4] = 30
}
if (con == 90)
{
    if (mkid.x > obj_mainchara.x)
        mkid.hspeed = -5
    else
        mkid.hspeed = -0.5
    mkid.image_speed = 0.25
    con = 91
    alarm[4] = 10
}
if (con == 92)
{
    mkid.hspeed = 0
    mkid.image_speed = 0
    con = 93
    alarm[4] = 30
}
if (con == 94)
{
    mkid.sprite_index = mkid.ltsprite
    mkid.image_speed = 0.25
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_643")
    global.msg[1] = scr_gettext("obj_monsterkidtrigger7_644")
    global.msg[2] = scr_gettext("obj_monsterkidtrigger7_645")
    scr_regulartext()
    con = 95
}
if (con == 95 && instance_exists(OBJ_WRITER) == 0)
{
    con = 96
    alarm[4] = 30
}
if (con == 97)
{
    mkid.image_speed = 0
    mkid.image_index = 0
    undyne.hspeed = -0.5
    undyne.image_speed = 0.2
    undyne.image_index = 0
    con = 98
    alarm[4] = 20
}
if (con == 99)
{
    undyne.hspeed = 0
    undyne.image_speed = 0
    undyne.image_index = 0
    con = 98.1
    alarm[4] = 30
}
if (con == 99.1)
{
    undyne.hspeed = -0.5
    undyne.image_speed = 0.2
    undyne.image_index = 0
    con = 98.2
    alarm[4] = 20
}
if (con == 99.2)
{
    undyne.hspeed = 0
    undyne.image_speed = 0
    undyne.image_index = 0
    con = 100
    alarm[4] = 60
}
if (con == 101)
{
    undyne.sprite_index = undyne.lsprite
    undyne.hspeed = -2
    undyne.image_speed = 0.25
    con = 102
    alarm[4] = 90
}
if (con == 103)
{
    with (undyne)
        instance_destroy()
    con = 104
    mkid.sprite_index = mkid.rtsprite
    alarm[4] = 30
}
if (con == 105)
{
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_714")
    global.msg[1] = scr_gettext("obj_monsterkidtrigger7_715")
    global.msg[2] = scr_gettext("obj_monsterkidtrigger7_716")
    global.msg[3] = scr_gettext("obj_monsterkidtrigger7_717")
    global.msg[4] = scr_gettext("obj_monsterkidtrigger7_718")
    global.msg[5] = scr_gettext("obj_monsterkidtrigger7_719")
    con = 106
    scr_regulartext()
}
if (con == 106 && instance_exists(OBJ_WRITER) == 0)
{
    con = 107
    alarm[4] = 20
}
if (con == 108)
{
    mkid.sprite_index = mkid.lsprite
    mkid.image_speed = 0.25
    mkid.hspeed = -1
    con = 109
    alarm[4] = 30
}
if (con == 110)
{
    mkid.image_speed = 0
    mkid.hspeed = 0
    mkid.image_index = 0
    con = 111
    alarm[4] = 30
}
if (con == 112)
{
    mkid.sprite_index = mkid.rtsprite
    con = 113
    alarm[4] = 30
}
if (con == 114)
{
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_757")
    scr_regulartext()
    con = 115
}
if (con == 115 && instance_exists(OBJ_WRITER) == 0)
{
    mkid.hspeed = -4
    mkid.image_speed = 0.5
    mkid.sprite_index = mkid.lsprite
    con = 116
    idealxview = round(((obj_mainchara.x - (view_wview[0] / 2)) + (obj_mainchara.sprite_width / 2)))
    if (view_xview[0] > idealxview)
        xdir = 0
    else
        xdir = 1
    alarm[4] = 40
}
if (con == 117)
{
    if (xdir == 1)
        view_xview[0] += 2
    else
        view_xview[0] -= 2
    if (abs((view_xview[0] - idealxview)) <= 2)
    {
        with (mkid)
            instance_destroy()
        view_object[0] = obj_mainchara
        obj_mainchara.cutscene = false
        with (doora)
            instance_destroy()
        with (doorb)
            instance_destroy()
        global.plot = 120
        global.flag[98] = 1
        caster_free(global.currentsong)
        global.interact = 0
        instance_destroy()
    }
}
if (con == 150 && instance_exists(OBJ_WRITER) == 1)
{
    if (OBJ_WRITER.stringno == 6)
        global.facing = 2
}
if (con == 150 && instance_exists(OBJ_WRITER) == 0)
{
    global.facing = 3
    obj_mainchara.hspeed = -2
    obj_mainchara.moving = 1
    obj_mainchara.image_speed = 0.25
    con = 151
    alarm[4] = 5
}
if (con == 152)
{
    mkid.hspeed = -2
    mkid.sprite_index = spr_mkid_r
    mkid.image_speed = 0.25
    con = 153
    alarm[4] = 10
}
if (con == 154)
{
    obj_mainchara.hspeed = 0
    mkid.hspeed = 0
    mkid.image_speed = 0
    mkid.image_index = 0
    mkid.sprite_index = spr_mkid_rt
    con = 155
    alarm[4] = 30
}
if (con == 156)
{
    global.msc = 0
    global.typer = 5
    global.facechoice = 0
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_836")
    global.msg[1] = scr_gettext("obj_monsterkidtrigger7_837")
    global.msg[2] = scr_gettext("obj_monsterkidtrigger7_838")
    global.msg[3] = scr_gettext("obj_monsterkidtrigger7_839")
    global.msg[4] = scr_gettext("obj_monsterkidtrigger7_840")
    global.msg[5] = scr_gettext("obj_monsterkidtrigger7_841")
    global.msg[6] = scr_gettext("obj_monsterkidtrigger7_842")
    global.msg[7] = scr_gettext("obj_monsterkidtrigger7_843")
    global.msg[8] = scr_gettext("obj_monsterkidtrigger7_844")
    instance_create(0, 0, obj_dialoguer)
    con = 157
}
if (con == 157 && instance_exists(OBJ_WRITER))
{
    if (OBJ_WRITER.stringno == 1)
        mkid.sprite_index = spr_mkid_lt
    if (OBJ_WRITER.stringno == 4)
        mkid.sprite_index = spr_mkid_rt
}
if (con == 157 && instance_exists(OBJ_WRITER) == 0)
{
    mkid.sprite_index = spr_mkid_rt
    global.battlegroup = 91
    global.seriousbattle = 1
    global.mercy = 1
    instance_create(0, 0, obj_battler)
    con = 158
    alarm[4] = 32
}
if (con == 159)
{
    global.seriousbattle = 0
    global.mercy = 0
    global.interact = 1
    con = 160
    alarm[4] = 30
}
if (con == 160)
{
    obj_mainchara.cutscene = false
    view_object[0] = obj_mainchara
    con = 160.1
    global.interact = 1
    alarm[4] = 30
    if (global.flag[350] == 1)
    {
        with (mkid)
            instance_destroy()
        con = 170
        alarm[4] = -1
    }
    global.interact = 1
}
if (con == 160.1)
    global.interact = 1
if (con == 161.1)
{
    global.interact = 1
    global.typer = 5
    global.facechoice = 0
    global.msc = 0
    global.msg[0] = scr_gettext("obj_monsterkidtrigger7_901")
    global.msg[1] = scr_gettext("obj_monsterkidtrigger7_902")
    instance_create(0, 0, obj_dialoguer)
    con = 162
}
if (con == 162)
    global.interact = 1
if (con == 162 && instance_exists(OBJ_WRITER) == 0)
{
    mkid.hspeed = -4
    mkid.image_speed = 0.5
    mkid.sprite_index = mkid.lsprite
    con = 163
    alarm[4] = 40
}
if (con == 164)
    con = 170
if (con == 170)
{
    global.facing = 0
    global.interact = 0
    if (global.plot < 120)
        global.plot = 120
    con = 171
    instance_destroy()
}
