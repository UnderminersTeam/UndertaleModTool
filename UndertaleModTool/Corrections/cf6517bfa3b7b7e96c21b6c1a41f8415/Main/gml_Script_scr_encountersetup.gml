xx = __view_get(0, 0)
yy = __view_get(1, 0)
for (i = 0; i < 3; i += 1)
{
    global.heromakex[i] = (xx + 80)
    global.heromakey[i] = ((yy + 50) + (80 * i))
    global.monsterinstancetype[i] = obj_lancerboss
    global.monstertype[i] = 1
    global.monstermakex[i] = (xx + 540)
    global.monstermakey[i] = ((yy + 160) + (80 * i))
}
if (global.char[0] != 0 && global.char[1] == 0 && global.char[2] == 0)
    global.heromakey[0] = (yy + 140)
if (global.char[0] != 0 && global.char[1] != 0 && global.char[2] == 0)
{
    global.heromakey[0] = (yy + 100)
    global.heromakey[1] = (yy + 180)
}
switch argument0
{
    case 0:
        break
    case 1:
        global.monsterinstancetype[0] = obj_placeholderenemy
        global.monstertype[0] = 1
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 110)
        global.monsterinstancetype[1] = obj_placeholderenemy
        global.monstertype[1] = 1
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 200)
        global.monstertype[2] = 0
        break
    case 2:
        global.monsterinstancetype[0] = obj_lancerboss
        global.monstertype[0] = 2
        global.monstermakex[0] = (xx + 540)
        global.monstermakey[0] = (yy + 200)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        break
    case 3:
        global.monsterinstancetype[0] = obj_dummyenemy
        global.monstertype[0] = 3
        global.monstermakex[0] = (xx + 500)
        global.monstermakey[0] = (yy + 160)
        if instance_exists(obj_npc_room)
        {
            global.monstermakex[0] = obj_npc_room.xstart
            global.monstermakey[0] = obj_npc_room.ystart
        }
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        break
    case 4:
        global.monsterinstancetype[0] = obj_diamondenemy
        global.monstertype[0] = 5
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 140)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_82_0")
        if (global.flag[500] >= 1)
            global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_83_0")
        if (global.flag[500] == 2)
            global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_84_0")
        break
    case 5:
        global.monsterinstancetype[0] = obj_diamondenemy
        global.monstertype[0] = 5
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 110)
        global.monsterinstancetype[1] = obj_diamondenemy
        global.monstertype[1] = 5
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 200)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_100_0")
        break
    case 6:
        global.monsterinstancetype[0] = obj_diamondenemy
        global.monstertype[0] = 5
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 110)
        global.monsterinstancetype[1] = obj_heartenemy
        global.monstertype[1] = 6
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 200)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_116_0")
        break
    case 7:
        global.monsterinstancetype[0] = obj_smallcheckers_enemy
        global.monstertype[0] = 9
        global.monstermakex[0] = (xx + 440)
        global.monstermakey[0] = (yy + 150)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_128_0")
        break
    case 8:
        global.monsterinstancetype[0] = obj_clubsenemy
        global.monstertype[0] = 16
        global.monstermakex[0] = (xx + 400)
        global.monstermakey[0] = (yy + 120)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_139_0")
        break
    case 9:
        global.monsterinstancetype[0] = obj_heartenemy
        global.monstertype[0] = 6
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 20)
        global.monsterinstancetype[1] = obj_heartenemy
        global.monstertype[1] = 6
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 120)
        global.monsterinstancetype[2] = obj_heartenemy
        global.monstertype[2] = 6
        global.monstermakex[2] = (xx + 460)
        global.monstermakey[2] = (yy + 220)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_158_0")
        break
    case 12:
        global.monsterinstancetype[0] = obj_checkers_enemy
        global.monstertype[0] = 10
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 120)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_172_0")
        break
    case 13:
        global.monsterinstancetype[0] = obj_ponman_enemy
        global.monstertype[0] = 11
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 110)
        global.monsterinstancetype[1] = obj_ponman_enemy
        global.monstertype[1] = 11
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 200)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_185_0")
        global.monstertype[2] = 0
        break
    case 14:
        global.monsterinstancetype[0] = obj_ponman_enemy
        global.monstertype[0] = 11
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 20)
        global.monsterinstancetype[1] = obj_ponman_enemy
        global.monstertype[1] = 11
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 120)
        global.monsterinstancetype[2] = obj_ponman_enemy
        global.monstertype[2] = 11
        global.monstermakex[2] = (xx + 460)
        global.monstermakey[2] = (yy + 220)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_206_0")
        break
    case 15:
        global.monsterinstancetype[0] = obj_clubsenemy
        global.monstertype[0] = 7
        global.monstermakex[0] = (xx + 400)
        global.monstermakey[0] = (yy + 30)
        global.monsterinstancetype[1] = obj_heartenemy
        global.monstertype[1] = 6
        global.monstermakex[1] = (xx + 420)
        global.monstermakey[1] = (yy + 200)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_223_0")
        break
    case 16:
        global.monsterinstancetype[0] = obj_rabbick_enemy
        global.monstertype[0] = 13
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 140)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_234_0")
        break
    case 17:
        global.monsterinstancetype[0] = obj_rabbick_enemy
        global.monstertype[0] = 13
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 60)
        global.monsterinstancetype[1] = obj_rabbick_enemy
        global.monstertype[1] = 13
        global.monstermakex[1] = (xx + 460)
        global.monstermakey[1] = (yy + 180)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_250_0")
        break
    case 18:
        global.monsterinstancetype[0] = obj_bloxer_enemy
        global.monstertype[0] = 14
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 140)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_261_0")
        break
    case 19:
        global.monsterinstancetype[0] = obj_bloxer_enemy
        global.monstertype[0] = 14
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 60)
        global.monsterinstancetype[1] = obj_bloxer_enemy
        global.monstertype[1] = 14
        global.monstermakex[1] = (xx + 460)
        global.monstermakey[1] = (yy + 180)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_277_0")
        break
    case 20:
        global.monsterinstancetype[0] = obj_lancerboss2
        global.monstertype[0] = 12
        global.heromakex[0] = (xx + 120)
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 160)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_292_0")
        break
    case 21:
        global.monsterinstancetype[0] = obj_jigsawryenemy
        global.monstertype[0] = 15
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 140)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_303_0")
        if (global.flag[500] >= 1)
            global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_304_0")
        if (global.flag[500] == 2)
            global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_305_0")
        break
    case 22:
        global.monsterinstancetype[0] = obj_jigsawryenemy
        global.monstertype[0] = 15
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 20)
        global.monsterinstancetype[1] = obj_jigsawryenemy
        global.monstertype[1] = 15
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 120)
        global.monsterinstancetype[2] = obj_jigsawryenemy
        global.monstertype[2] = 15
        global.monstermakex[2] = (xx + 460)
        global.monstermakey[2] = (yy + 220)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_324_0")
        break
    case 23:
        global.monsterinstancetype[0] = obj_jigsawryenemy
        global.monstertype[0] = 15
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 20)
        global.monsterinstancetype[1] = obj_diamondenemy
        global.monstertype[1] = 5
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 120)
        global.monsterinstancetype[2] = obj_heartenemy
        global.monstertype[2] = 6
        global.monstermakex[2] = (xx + 460)
        global.monstermakey[2] = (yy + 220)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_343_0")
        break
    case 24:
        global.monsterinstancetype[0] = obj_rabbick_enemy
        global.monstertype[0] = 13
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 60)
        global.monsterinstancetype[1] = obj_diamondenemy
        global.monstertype[1] = 5
        global.monstermakex[1] = (xx + 460)
        global.monstermakey[1] = (yy + 180)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_358_0")
        break
    case 25:
        global.heromakex[0] = (xx + 80)
        global.heromakey[0] = (yy + 100)
        global.heromakex[1] = (xx + 90)
        global.heromakey[1] = (yy + 150)
        global.heromakex[2] = (xx + 100)
        global.heromakey[2] = (yy + 210)
        global.monsterinstancetype[0] = obj_joker
        global.monstertype[0] = 20
        global.monstermakex[0] = (xx + 500)
        global.monstermakey[0] = (yy + 160)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_384_0")
        break
    case 27:
        global.monsterinstancetype[0] = obj_checkers_enemy
        global.monstertype[0] = 21
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 120)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_397_0")
        global.heromakey[0] = (yy + 65)
        break
    case 28:
        global.monsterinstancetype[0] = obj_rudinnranger
        global.monstertype[0] = 22
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 110)
        global.monsterinstancetype[1] = obj_rudinnranger
        global.monstertype[1] = 22
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 200)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_414_0")
        break
    case 29:
        global.monsterinstancetype[0] = obj_headhathy
        global.monstertype[0] = 23
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 110)
        global.monsterinstancetype[1] = obj_headhathy
        global.monstertype[1] = 23
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 200)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_430_0")
        break
    case 30:
        global.monsterinstancetype[0] = obj_headhathy
        global.monstertype[0] = 23
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 20)
        global.monsterinstancetype[1] = obj_headhathy
        global.monstertype[1] = 23
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 120)
        global.monsterinstancetype[2] = obj_headhathy
        global.monstertype[2] = 23
        global.monstermakex[2] = (xx + 460)
        global.monstermakey[2] = (yy + 220)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_449_0")
        break
    case 31:
        global.monsterinstancetype[0] = obj_susieenemy
        global.monstertype[0] = 19
        global.monstermakex[0] = (xx + 520)
        global.monstermakey[0] = (yy + 80)
        global.monsterinstancetype[1] = obj_lancerboss3
        global.monstertype[1] = 18
        global.monstermakex[1] = (xx + 540)
        global.monstermakey[1] = (yy + 240)
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_463_0")
        break
    case 32:
        global.monsterinstancetype[0] = obj_rabbick_enemy
        global.monstertype[0] = 13
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 20)
        global.monsterinstancetype[1] = obj_rabbick_enemy
        global.monstertype[1] = 13
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 120)
        global.monsterinstancetype[2] = obj_rabbick_enemy
        global.monstertype[2] = 13
        global.monstermakex[2] = (xx + 460)
        global.monstermakey[2] = (yy + 220)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_484_0")
        break
    case 33:
        global.monsterinstancetype[0] = obj_diamondenemy
        global.monstertype[0] = 5
        global.monstermakex[0] = (xx + 480)
        global.monstermakey[0] = (yy + 20)
        global.monsterinstancetype[1] = obj_heartenemy
        global.monstertype[1] = 6
        global.monstermakex[1] = (xx + 500)
        global.monstermakey[1] = (yy + 120)
        global.monsterinstancetype[2] = obj_diamondenemy
        global.monstertype[2] = 5
        global.monstermakex[2] = (xx + 460)
        global.monstermakey[2] = (yy + 220)
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_505_0")
        break
    case 40:
        global.monsterinstancetype[0] = obj_king_boss
        global.monstertype[0] = 25
        global.monstermakex[0] = (xx + 460)
        global.monstermakey[0] = (yy + 70)
        global.monstertype[1] = 0
        global.monstertype[2] = 0
        global.battlemsg[0] = scr_84_get_lang_string("scr_encountersetup_slash_scr_encountersetup_gml_517_0")
        break
}

