global.msg[0] = scr_gettext(("item_use_" + string(argument1)))
switch argument1
{
    case 0:
        break
    case 1:
        if (global.seriousbattle == 0)
        {
            randomtext = round(random(15))
            if (randomtext <= 2)
                global.msg[0] += scr_gettext("item_use_1a")
            if (randomtext == 15)
                global.msg[0] += scr_gettext("item_use_1b")
        }
        script_execute(scr_recoitem, 10)
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 2:
        if (global.seriousbattle == 1)
            global.msg[0] = scr_gettext("item_use_2_serious")
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 15)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 3:
        if instance_exists(obj_dogeparent)
        {
            if instance_exists(obj_lesserdoge)
            {
                if (obj_lesserdoge.mercymod < 60)
                    obj_lesserdoge.mercymod = 102
            }
            else
                obj_dogeparent.mercymod = 102
            if (instance_exists(obj_mandog) || instance_exists(obj_womandog))
            {
                if (scr_monstersum() == 1)
                {
                    script_execute(scr_writetext, 0, scr_gettext("item_use_3_doge1"), 0, 0)
                    obj_dogeparent.mercymod = -9999
                }
                else
                    script_execute(scr_writetext, 0, scr_gettext("item_use_3_doge2"), 0, 0)
            }
            else
            {
                if instance_exists(obj_greatdog)
                    obj_dogeparent.mercymod = 250
                script_execute(scr_writetext, 0, scr_gettext("item_use_3_greatdog"), 0, 0)
            }
            if instance_exists(obj_endogeny)
                obj_endogeny.mercymod = 999999
        }
        else if (instance_exists(obj_papyrusboss) || instance_exists(obj_wizard) || instance_exists(obj_mettatonex))
        {
            if instance_exists(obj_papyrusboss)
                script_execute(scr_writetext, 0, scr_gettext("item_use_3_papyrus"), 0, 0)
            if instance_exists(obj_wizard)
            {
                script_execute(scr_writetext, 0, scr_gettext("item_use_3_madjick"), 0, 0)
                obj_wizard.mercymod = 300
            }
            if instance_exists(obj_mettatonex)
            {
                with (obj_ratingsmaster)
                {
                    curtype = 7
                    event_user(0)
                }
                script_execute(scr_writetext, 0, scr_gettext("item_use_3_mettaton"), 0, 0)
            }
        }
        else
            script_execute(scr_writetext, 0, scr_gettext("item_use_3"), 0, 0)
        break
    case 4:
        snd_play(snd_power)
        if (global.seriousbattle == 0)
            global.msg[0] += scr_gettext("item_use_4a")
        script_execute(scr_recoitem, 10)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 5:
        script_execute(scr_recoitem, 1)
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 6:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 8)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 7:
        if (global.seriousbattle == 0)
        {
            if (global.inbattle == 1)
            {
                randomtext = ceil(random(10))
                if (randomtext > 9)
                    global.msg[0] = scr_gettext("item_use_7a")
            }
        }
        if instance_exists(obj_spiderb)
        {
            with (obj_spiderb)
                event_user(6)
        }
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 12)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 8:
        if (global.seriousbattle == 0)
        {
            randomtext = round(random(10))
            if (randomtext > 8)
                global.msg[0] += scr_gettext("item_use_8a")
        }
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 5)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 9:
        foodsounder = instance_create(0, 0, obj_foodsound)
        if (global.seriousbattle == 0)
        {
            with (foodsounder)
                soundtype = 2
        }
        script_execute(scr_recoitem, 16)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 10:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 24)
        script_execute(scr_itemshift, argument0, 0)
        if instance_exists(obj_spiderb)
        {
            with (obj_spiderb)
                event_user(6)
        }
        break
    case 11:
        instance_create(0, 0, obj_foodsound)
        spec_p = 0
        if (global.hp < global.maxhp)
            global.hp = global.maxhp
        if instance_exists(obj_asgoreb)
        {
            spec_p = 1
            global.msg[1] = scr_gettext("item_use_11_asgore1")
            global.msg[2] = scr_gettext("item_use_11_asgore2")
            with (obj_asgoreb)
            {
                global.monsteratk[myself] -= 1
                global.monsterdef[myself] -= 5
            }
        }
        if instance_exists(obj_ripoff_toriel)
        {
            spec_p = 1
            global.msg[1] = scr_gettext("item_use_11_toriel")
            with (obj_monsterparent)
                totalmercy += 3
        }
        if (spec_p == 0)
            script_execute(scr_writetext, 0, (scr_gettext("item_use_11") + "%"), 0, 0)
        if (spec_p == 1)
            script_execute(scr_writetext, 0, scr_gettext("item_use_11"), 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 12:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 13:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 14:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 15:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 16:
        if (room == room_tundra6A)
            global.flag[56] = 4
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 45)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 17:
        randomtext = floor(random(8))
        if (randomtext == 0)
            global.msg[0] = scr_gettext("item_use_17a")
        if (randomtext == 1)
            global.msg[0] = scr_gettext("item_use_17b")
        if (randomtext == 2)
            global.msg[0] = scr_gettext("item_use_17c")
        if (randomtext == 3)
            global.msg[0] = scr_gettext("item_use_17d")
        if (randomtext == 4)
            global.msg[0] = scr_gettext("item_use_17e")
        if (randomtext == 5)
            global.msg[0] = scr_gettext("item_use_17f")
        if (randomtext == 6)
            global.msg[0] = scr_gettext("item_use_17g")
        if (randomtext == 7)
            global.msg[0] = scr_gettext("item_use_17h")
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 15)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 18:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 28)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 19:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 11)
        global.item[argument0] = 20
        break
    case 20:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 11)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 21:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 22)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 22:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 2)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 23:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 34)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 24:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 25:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 26:
        if (global.inbattle == 0)
        {
            d = instance_create(x, y, obj_imageview)
            d.sprite_index = scr_getsprite(864)
            global.interact = 1
        }
        else
        {
            if (global.weapon == 14)
            {
                snd_play(snd_tearcard)
                add = 6
                if (global.at > 18)
                    add = 5
                if (global.at > 23)
                    add = 4
                if (global.at > 26)
                    add = 3
                if (global.at > 28)
                    add = 2
                global.at += add
                global.msg[1] = scr_gettext("item_use_26a", string(add))
                global.flag[78] = 1
            }
            else
                global.msg[1] = scr_gettext("item_use_26b")
            script_execute(scr_writetext, 0, "x", 0, 0)
            script_execute(scr_itemshift, argument0, 0)
        }
        break
    case 27:
        script_execute(scr_writetext, 0, "x", 0, 0)
        if instance_exists(obj_rarependant)
        {
            with (obj_rarependant)
                con = 1
        }
        script_execute(scr_itemshift, argument0, 0)
        break
    case 28:
        healamt = 1
        dogsad = floor(random(4))
        if (dogsad == 0)
        {
            global.msg[0] += scr_gettext("item_use_28a")
            healamt = 30
        }
        if (dogsad == 1)
        {
            global.msg[0] += scr_gettext("item_use_28b")
            healamt = 10
        }
        if (dogsad == 2)
        {
            global.msg[0] += scr_gettext("item_use_28c")
            healamt = 2
        }
        if (dogsad == 3)
        {
            global.msg[0] += scr_gettext("item_use_28d")
            healamt = 999
        }
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_swallow
        if (global.seriousbattle == 0)
            grt.sound2 = snd_dogresidue
        if (global.seriousbattle == 1)
            grt.sound2 = snd_heal_c
        grt.alarm[1] = 10
        script_execute(scr_recoitem, healamt)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 29:
        if (global.item[7] == 0)
            global.msg[1] = scr_gettext("item_use_29a")
        else
            global.msg[1] = scr_gettext("item_use_29b")
        global.msg[2] = scr_gettext("item_use_29c")
        global.msg[3] = scr_gettext("item_use_29d")
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_item
        grt.sound2 = snd_dogresidue
        script_execute(scr_writetext, 0, "x", 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        for (i = 0; i < 8; i += 1)
        {
            rr = (floor(random(7)) + 28)
            if (global.item[i] == 0)
                global.item[i] = rr
        }
        break
    case 30:
        if (global.item[7] == 0)
            global.msg[1] = scr_gettext("item_use_29a")
        else
            global.msg[1] = scr_gettext("item_use_29b")
        global.msg[2] = scr_gettext("item_use_29c")
        global.msg[3] = scr_gettext("item_use_29d")
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_item
        grt.sound2 = snd_dogresidue
        script_execute(scr_writetext, 0, "x", 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        for (i = 0; i < 8; i += 1)
        {
            rr = (floor(random(7)) + 28)
            if (global.item[i] == 0)
                global.item[i] = rr
        }
        break
    case 31:
        if (global.item[7] == 0)
            global.msg[1] = scr_gettext("item_use_29a")
        else
            global.msg[1] = scr_gettext("item_use_29b")
        global.msg[2] = scr_gettext("item_use_29c")
        global.msg[3] = scr_gettext("item_use_29d")
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_item
        grt.sound2 = snd_dogresidue
        script_execute(scr_writetext, 0, "x", 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        for (i = 0; i < 8; i += 1)
        {
            rr = (floor(random(7)) + 28)
            if (global.item[i] == 0)
                global.item[i] = rr
        }
        break
    case 32:
        if (global.item[7] == 0)
            global.msg[1] = scr_gettext("item_use_29a")
        else
            global.msg[1] = scr_gettext("item_use_29b")
        global.msg[2] = scr_gettext("item_use_29c")
        global.msg[3] = scr_gettext("item_use_29d")
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_item
        grt.sound2 = snd_dogresidue
        script_execute(scr_writetext, 0, "x", 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        for (i = 0; i < 8; i += 1)
        {
            rr = (floor(random(7)) + 28)
            if (global.item[i] == 0)
                global.item[i] = rr
        }
        break
    case 33:
        if (global.item[7] == 0)
            global.msg[1] = scr_gettext("item_use_29a")
        else
            global.msg[1] = scr_gettext("item_use_29b")
        global.msg[2] = scr_gettext("item_use_29c")
        global.msg[3] = scr_gettext("item_use_29d")
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_item
        grt.sound2 = snd_dogresidue
        script_execute(scr_writetext, 0, "x", 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        for (i = 0; i < 8; i += 1)
        {
            rr = (floor(random(7)) + 28)
            if (global.item[i] == 0)
                global.item[i] = rr
        }
        break
    case 34:
        if (global.item[7] == 0)
            global.msg[1] = scr_gettext("item_use_29a")
        else
            global.msg[1] = scr_gettext("item_use_29b")
        global.msg[2] = scr_gettext("item_use_29c")
        global.msg[3] = scr_gettext("item_use_29d")
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_item
        grt.sound2 = snd_dogresidue
        script_execute(scr_writetext, 0, "x", 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        for (i = 0; i < 8; i += 1)
        {
            rr = (floor(random(7)) + 28)
            if (global.item[i] == 0)
                global.item[i] = rr
        }
        break
    case 35:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 21)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 36:
        if (global.inbattle == 0)
        {
            instance_create(0, 0, obj_foodsound)
            script_execute(scr_recoitem, 15)
        }
        if (global.inbattle == 1)
        {
            if (global.seriousbattle == 1)
            {
                global.msg[0] = scr_gettext("item_use_36a")
                instance_create(0, 0, obj_foodsound)
                script_execute(scr_recoitem, 90)
            }
            else
                nood = instance_create(0, 0, obj_instantnoodleitem)
        }
        script_execute(scr_itemshift, argument0, 0)
        break
    case 37:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 18)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 38:
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_swallow
        if (global.seriousbattle == 0)
            grt.sound2 = snd_dogsalad
        if (global.seriousbattle == 1)
            grt.sound2 = snd_heal_c
        grt.alarm[1] = 10
        script_execute(scr_recoitem, 20)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 39:
        if (global.seriousbattle == 0)
        {
            grt = instance_create(0, 0, obj_soundcombo)
            grt.sound1 = snd_swallow
            if (global.seriousbattle == 0)
                grt.sound2 = snd_catsalad
            if (global.seriousbattle == 1)
                grt.sound2 = snd_heal_c
            grt.alarm[1] = 10
        }
        else
            grt = instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 21)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 40:
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_swallow
        grt.sound2 = snd_sparkle1
        grt.alarm[1] = 10
        if instance_exists(obj_ratingsmaster)
        {
            global.msg[0] = scr_gettext("item_use_40_mettaton")
            with (obj_ratingsmaster)
            {
                curtype = 10
                event_user(0)
            }
        }
        script_execute(scr_recoitem, 27)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 41:
        if (global.inbattle == 1)
        {
            if (global.sp < 8)
            {
                global.sp += 1
                if instance_exists(obj_spiderb)
                    global.msg[0] += scr_gettext("item_use_41_nospeed")
                else
                    global.msg[0] += scr_gettext("item_use_41_speed")
            }
        }
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_swallow
        grt.sound2 = snd_speedup
        grt.alarm[1] = 10
        script_execute(scr_recoitem, 10)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 42:
        if instance_exists(obj_ratingsmaster)
        {
            global.msg[0] = scr_gettext("item_use_42_mettaton")
            with (obj_ratingsmaster)
            {
                curtype = 9
                event_user(0)
            }
        }
        grt = instance_create(0, 0, obj_foodsound)
        grt.alarm[1] = 10
        script_execute(scr_recoitem, 14)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 43:
        if (global.seriousbattle == 0)
        {
            grt = instance_create(0, 0, obj_soundcombo)
            grt.sound1 = snd_swallow
            grt.sound2 = snd_hero
            grt.alarm[1] = 10
        }
        else
            grt = instance_create(0, 0, obj_foodsound)
        if (global.inbattle == 1)
        {
            if (global.at < 150)
                global.at += 4
            global.msg[0] += scr_gettext("item_use_43_attack")
        }
        if instance_exists(obj_ratingsmaster)
        {
            with (obj_ratingsmaster)
            {
                curtype = 9
                event_user(0)
            }
        }
        script_execute(scr_recoitem, 40)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 44:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 45:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 46:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 47:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 48:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 49:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 50:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 51:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 52:
        scr_weaponeq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 53:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 54:
        global.hp -= 1
        grt = instance_create(0, 0, obj_soundcombo)
        grt.sound1 = snd_swallow
        grt.sound2 = snd_hurt1
        grt.alarm[1] = 10
        if (global.hp <= 2)
        {
            grt.sound2 = snd_power
            global.hp = global.maxhp
            global.msg[0] = scr_gettext("item_use_54_heal")
        }
        script_execute(scr_writetext, 0, "x", 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 55:
        if (global.flag[503] == 1)
            global.msg[0] = scr_gettext("item_use_55_short")
        global.flag[503] = 1
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 17)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 56:
        if (!instance_exists(obj_undyne_friendc))
        {
            global.msg[0] = scr_gettext("item_use_56")
            global.msg[1] = scr_gettext("item_use_56a")
            if (room == room_fire_prelab)
                global.msg[0] = scr_gettext("item_use_56_prelab")
            script_execute(scr_writetext, 0, "x", 0, 0)
        }
        else
        {
            global.faceemotion = 1
            global.msg[0] = scr_gettext("item_use_56_undyne")
            script_execute(scr_writetext, 0, "x", 5, 37)
        }
        break
    case 57:
        if (!instance_exists(obj_undyne_friendc))
        {
            global.msg[0] = scr_gettext("item_use_56")
            global.msg[1] = scr_gettext("item_use_56a")
            if (room == room_fire_prelab)
                global.msg[0] = scr_gettext("item_use_56_prelab")
            script_execute(scr_writetext, 0, "x", 0, 0)
        }
        else
        {
            global.faceemotion = 1
            global.msg[0] = scr_gettext("item_use_56_undyne")
            script_execute(scr_writetext, 0, "x", 5, 37)
        }
        break
    case 58:
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 13)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 59:
        if instance_exists(obj_ratingsmaster)
        {
            global.msg[0] = scr_gettext("item_use_59_mettaton")
            with (obj_ratingsmaster)
            {
                curtype = 8
                event_user(0)
            }
        }
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 17)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 60:
        if (room == room_icecave1)
        {
            global.msg[0] = scr_gettext("item_use_60_fail1")
            global.msg[1] = scr_gettext("item_use_60_fail2")
            global.msg[2] = scr_gettext("item_use_60_fail3")
        }
        if instance_exists(obj_mettatonex)
            global.msg[0] = scr_gettext("item_use_60_mettaton")
        if instance_exists(obj_blookhouses)
        {
            global.msg[0] = scr_gettext("item_use_60_open1")
            global.msg[1] = scr_gettext("item_use_60_open2")
            global.flag[433] = 1
            snd_play(snd_item)
            with (obj_blookhouses)
                event_user(1)
            script_execute(scr_itemshift, argument0, 0)
        }
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 61:
        if instance_exists(obj_ratingsmaster)
        {
            global.msg[0] = scr_gettext("item_use_61_mettaton")
            with (obj_ratingsmaster)
            {
                curtype = 13
                event_user(0)
            }
        }
        grt = instance_create(0, 0, obj_foodsound)
        grt.alarm[1] = 10
        script_execute(scr_recoitem, 60)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 62:
        if (global.seriousbattle == 0)
            global.msg[0] = scr_gettext("item_use_62a")
        if instance_exists(obj_endogeny)
            obj_endogeny.mercymod = 999999
        instance_create(0, 0, obj_foodsound)
        script_execute(scr_recoitem, 65)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 63:
        instance_create(0, 0, obj_foodsound)
        spec_p = 0
        if (global.hp < (global.maxhp - 1))
            global.hp = (global.maxhp - 1)
        if instance_exists(obj_asgoreb)
        {
            spec_p = 1
            global.msg[1] = scr_gettext("item_use_11_asgore1")
            global.msg[2] = scr_gettext("item_use_11_asgore2")
            with (obj_asgoreb)
            {
                global.monsteratk[myself] -= 1
                global.monsterdef[myself] -= 5
            }
        }
        if instance_exists(obj_ripoff_toriel)
        {
            spec_p = 1
            global.msg[1] = scr_gettext("item_use_11_toriel")
            with (obj_monsterparent)
                totalmercy += 3
        }
        if (spec_p == 0)
            script_execute(scr_writetext, 0, (scr_gettext("item_use_63") + "%"), 0, 0)
        if (spec_p == 1)
            script_execute(scr_writetext, 0, scr_gettext("item_use_63"), 0, 0)
        script_execute(scr_itemshift, argument0, 0)
        break
    case 64:
        scr_armoreq(argument0, argument1)
        snd_play(snd_item)
        script_execute(scr_writetext, 0, "x", 0, 0)
        break
    case 201:
        if (global.flag[37] == 1)
            scr_writetext(1508, "x", 0, 0)
        else if (global.plot < 19.9)
        {
            if (global.plot > 18 && room == room_torhouse2)
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1507, "x", 0, 0)
            }
            else
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1501, "x", 0, 0)
            }
        }
        else
        {
            snd_play(snd_phone)
            script_execute(scr_writetext, 1506, "x", 0, 0)
        }
        break
    case 202:
        if (global.flag[37] == 1)
            scr_writetext(1508, "x", 0, 0)
        else if (global.plot < 19.9)
        {
            if (global.plot > 18 && room == room_torhouse2)
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1507, "x", 0, 0)
            }
            else
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1502, "x", 0, 0)
                script_execute(scr_phoneshift, argument0, 0)
            }
        }
        else
        {
            snd_play(snd_phone)
            script_execute(scr_writetext, 1506, "x", 0, 0)
        }
        break
    case 203:
        if (global.flag[37] == 1)
            scr_writetext(1508, "x", 0, 0)
        else if (global.plot < 19.9)
        {
            if (global.plot > 18 && room == room_torhouse2)
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1507, "x", 0, 0)
            }
            else
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1503, "x", 0, 0)
                script_execute(scr_phoneshift, argument0, 0)
            }
        }
        else
        {
            snd_play(snd_phone)
            script_execute(scr_writetext, 1506, "x", 0, 0)
        }
        break
    case 204:
        if (global.flag[37] == 1)
            scr_writetext(1508, "x", 0, 0)
        else if (global.plot < 19.9)
        {
            if (global.plot > 18 && room == room_torhouse2)
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1507, "x", 0, 0)
            }
            else
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1504, "x", 0, 0)
                script_execute(scr_phoneshift, argument0, 0)
            }
        }
        else
        {
            snd_play(snd_phone)
            script_execute(scr_writetext, 1506, "x", 0, 0)
        }
        break
    case 205:
        if (global.flag[37] == 1)
            scr_writetext(1508, "x", 0, 0)
        else if (global.plot < 19.9)
        {
            if (global.plot > 18 && room == room_torhouse2)
            {
                snd_play(snd_phone)
                script_execute(scr_writetext, 1507, "x", 0, 0)
            }
            else
            {
                snd_play(snd_phone)
                if (global.flag[41] == 1)
                    script_execute(scr_phoneshift, argument0, 0)
                script_execute(scr_writetext, 1505, "x", 0, 0)
            }
        }
        else
        {
            snd_play(snd_phone)
            script_execute(scr_writetext, 1506, "x", 0, 0)
        }
        break
    case 206:
        scr_phone_moveup(argument0, argument1)
        snd_play(snd_phone)
        if (global.flag[7] == 0)
            script_execute(scr_writetext, 1506, "x", 0, 0)
        if (global.flag[7] == 1)
            script_execute(scr_writetext, 1515, "x", 0, 0)
        break
    case 210:
        scr_phone_moveup(argument0, argument1)
        script_execute(scr_writetext, 1510, "x", 0, 0)
        break
    case 220:
        if (room != room_water_dogroom)
        {
            snd_play(snd_dimbox)
            scr_phone_moveup(argument0, argument1)
            ii = instance_create(0, 0, obj_itemswapper)
            ii.spec = 1
            ii.boxtype = 0
        }
        else
            script_execute(scr_writetext, 1520, "x", 0, 0)
        break
    case 221:
        if (room != room_water_dogroom)
        {
            snd_play(snd_dimbox)
            scr_phone_moveup(argument0, argument1)
            ii = instance_create(0, 0, obj_itemswapper)
            ii.spec = 1
            ii.boxtype = 1
        }
        else
            script_execute(scr_writetext, 1520, "x", 0, 0)
        break
}

