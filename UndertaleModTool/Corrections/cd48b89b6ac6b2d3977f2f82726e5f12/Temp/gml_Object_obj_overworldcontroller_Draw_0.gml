buffer += 1
if (global.interact == 5)
{
    currentmenu = global.menuno
    if (global.menuno < 6)
        currentspot = global.menucoord[global.menuno]
    xx = view_xview[view_current]
    yy = (view_yview[view_current] + 10)
    moveyy = yy
    if (obj_mainchara.y > (yy + 120))
        moveyy += 135
    if (global.menuno != 4)
    {
        draw_set_color(c_white)
        draw_rectangle((16 + xx), (16 + moveyy), (86 + xx), (70 + moveyy), 0)
        draw_rectangle((16 + xx), (74 + yy), (86 + xx), (147 + yy), 0)
        if (global.menuno == 1 || global.menuno == 5 || global.menuno == 6)
            draw_rectangle((94 + xx), (16 + yy), (266 + xx), (196 + yy), 0)
        if (global.menuno == 2)
            draw_rectangle((94 + xx), (16 + yy), (266 + xx), (224 + yy), 0)
        if (global.menuno == 3)
            draw_rectangle((94 + xx), (16 + yy), (266 + xx), (150 + yy), 0)
        if (global.menuno == 7)
            draw_rectangle((94 + xx), (16 + yy), (266 + xx), (216 + yy), 0)
        draw_set_color(c_black)
        draw_rectangle((19 + xx), (19 + moveyy), (83 + xx), (67 + moveyy), 0)
        draw_rectangle((19 + xx), (77 + yy), (83 + xx), (144 + yy), 0)
        if (global.menuno == 1 || global.menuno == 5 || global.menuno == 6)
            draw_rectangle((97 + xx), (19 + yy), (263 + xx), (193 + yy), 0)
        if (global.menuno == 2)
            draw_rectangle((97 + xx), (19 + yy), (263 + xx), (221 + yy), 0)
        if (global.menuno == 3)
            draw_rectangle((97 + xx), (19 + yy), (263 + xx), (147 + yy), 0)
        if (global.menuno == 7)
            draw_rectangle((97 + xx), (19 + yy), (263 + xx), (213 + yy), 0)
        draw_set_color(c_white)
        draw_set_font(fnt_small)
        draw_text((23 + xx), (49 + moveyy), ((("HP  " + string(global.hp)) + "/") + string(global.maxhp)))
        draw_text((23 + xx), (40 + moveyy), ("LV  " + string(global.lv)))
        draw_text((23 + xx), (58 + moveyy), ("G   " + string(global.gold)))
        draw_set_font(fnt_maintext)
        draw_text((23 + xx), (20 + moveyy), global.charname)
        if (global.menuchoice[0] == 1)
            draw_text((42 + xx), (84 + yy), "ITEM")
        if (global.menuchoice[1] == 1)
            draw_text((42 + xx), (102 + yy), "STAT")
        if (global.menuchoice[2] == 1)
            draw_text((42 + xx), (120 + yy), "CELL")
        if (global.menuno == 1 || global.menuno == 5)
        {
            for (i = 0; i < 8; i += 1)
                draw_text((116 + xx), ((30 + yy) + (i * 16)), global.itemname[i])
            draw_text((116 + xx), (170 + yy), "USE")
            draw_text(((116 + xx) + 48), (170 + yy), "INFO")
            draw_text(((116 + xx) + 105), (170 + yy), "DROP")
        }
    }
    if (global.menuno == 3)
    {
        for (i = 0; i < 7; i += 1)
            draw_text((116 + xx), ((30 + yy) + (i * 16)), global.phonename[i])
    }
    if (global.menuno == 6)
    {
        scr_itemname()
        for (i = 0; i < 8; i += 1)
            draw_text((116 + xx), ((30 + yy) + (i * 16)), global.itemname[i])
    }
    if (global.menuno == 7)
    {
        scr_storagename(300)
        for (i = 0; i < 10; i += 1)
            draw_text((116 + xx), ((30 + yy) + (i * 16)), global.itemname[i])
    }
    if (global.menuno == 2)
    {
        draw_text((108 + xx), (32 + yy), (('"' + global.charname) + '"'))
        draw_text((108 + xx), (62 + yy), ("LV  " + string(global.lv)))
        draw_text((108 + xx), (78 + yy), ((("HP  " + string(global.hp)) + " / ") + string(global.maxhp)))
        draw_text((108 + xx), (110 + yy), (((("AT  " + string((global.at - 10))) + " (") + string(global.wstrength)) + ")"))
        draw_text((108 + xx), (126 + yy), (((("DF  " + string((global.df - 10))) + " (") + string(global.adef)) + ")"))
        weaponname = " "
        armorname = " "
        if (global.weapon == 3)
            weaponname = "Stick"
        if (global.weapon == 13)
            weaponname = "Toy Knife"
        if (global.weapon == 14)
            weaponname = "Tough Glove"
        if (global.weapon == 25)
            weaponname = "Ballet Shoes"
        if (global.weapon == 45)
            weaponname = "Torn Notebook"
        if (global.weapon == 47)
            weaponname = "Burnt Pan"
        if (global.weapon == 49)
            weaponname = "Empty Gun"
        if (global.weapon == 51)
            weaponname = "Worn Dagger"
        if (global.weapon == 52)
            weaponname = "Real Knife"
        if (global.armor == 4)
            armorname = "Bandage"
        if (global.armor == 12)
            armorname = "Faded Ribbon"
        if (global.armor == 15)
            armorname = "Manly Bandanna"
        if (global.armor == 24)
            armorname = "Old Tutu"
        if (global.armor == 44)
            armorname = "Clouded Glasses"
        if (global.armor == 46)
            armorname = "Stained Apron"
        if (global.armor == 48)
            armorname = "Cowboy Hat"
        if (global.armor == 50)
            armorname = "Heart Locket"
        if (global.armor == 53)
            armorname = "The Locket"
        if (global.armor == 64)
            armorname = "Temmie Armor"
        draw_text((108 + xx), (156 + yy), ("WEAPON: " + weaponname))
        draw_text((108 + xx), (172 + yy), ("ARMOR: " + armorname))
        draw_text((108 + xx), (192 + yy), ("GOLD: " + string(global.gold)))
        if (global.kills > 20)
            draw_text((192 + xx), (192 + yy), ("KILLS: " + string(global.kills)))
        if (string_length(global.charname) >= 7)
            draw_text((192 + xx), (32 + yy), "Easy to#change,#huh?")
        draw_text((192 + xx), (110 + yy), ("EXP: " + string(global.xp)))
        if (global.lv == 1)
            nextlevel = (10 - global.xp)
        if (global.lv == 2)
            nextlevel = (30 - global.xp)
        if (global.lv == 3)
            nextlevel = (70 - global.xp)
        if (global.lv == 4)
            nextlevel = (120 - global.xp)
        if (global.lv == 5)
            nextlevel = (200 - global.xp)
        if (global.lv == 6)
            nextlevel = (300 - global.xp)
        if (global.lv == 7)
            nextlevel = (500 - global.xp)
        if (global.lv == 8)
            nextlevel = (800 - global.xp)
        if (global.lv == 9)
            nextlevel = (1200 - global.xp)
        if (global.lv == 10)
            nextlevel = (1700 - global.xp)
        if (global.lv == 11)
            nextlevel = (2500 - global.xp)
        if (global.lv == 12)
            nextlevel = (3500 - global.xp)
        if (global.lv == 13)
            nextlevel = (5000 - global.xp)
        if (global.lv == 14)
            nextlevel = (7000 - global.xp)
        if (global.lv == 15)
            nextlevel = (10000 - global.xp)
        if (global.lv == 16)
            nextlevel = (15000 - global.xp)
        if (global.lv == 17)
            nextlevel = (25000 - global.xp)
        if (global.lv == 18)
            nextlevel = (50000 - global.xp)
        if (global.lv == 19)
            nextlevel = (99999 - global.xp)
        if (global.lv >= 20)
            nextlevel = 0
        draw_text((192 + xx), (126 + yy), ("NEXT: " + string(nextlevel)))
    }
    if (global.menuno == 4)
    {
        iniread = ini_open("undertale.ini")
        name = ini_read_string("General", "Name", "EMPTY")
        love = ini_read_real("General", "Love", 0)
        time = ini_read_real("General", "Time", 1)
        kills = ini_read_real("General", "Kills", 0)
        roome = ini_read_real("General", "Room", 0)
        ini_close()
        draw_set_font(fnt_maintext)
        draw_set_color(c_white)
        draw_rectangle((54 + xx), (49 + yy), (265 + xx), (135 + yy), 0)
        draw_set_color(c_black)
        draw_rectangle((57 + xx), (52 + yy), (262 + xx), (132 + yy), 0)
        draw_set_color(c_white)
        if (global.menucoord[4] == 2)
            draw_set_color(c_yellow)
        minutes = floor((time / 1800))
        seconds = round((((time / 1800) - minutes) * 60))
        if (seconds == 60)
            seconds = 59
        if (seconds < 10)
            seconds = ("0" + string(seconds))
        script_execute(scr_roomname, roome)
        draw_text((70 + xx), (60 + yy), name)
        draw_text((140 + xx), (60 + yy), ("LV " + string(love)))
        draw_text((210 + xx), (60 + yy), ((string(minutes) + ":") + string(seconds)))
        draw_text((70 + xx), (80 + yy), roomname)
        if (global.menucoord[4] == 0)
            draw_sprite(spr_heartsmall, 0, (xx + 71), (yy + 113))
        if (global.menucoord[4] == 1)
            draw_sprite(spr_heartsmall, 0, (xx + 161), (yy + 113))
        if (global.menucoord[4] < 2)
        {
            draw_text((xx + 85), (yy + 110), "Save")
            draw_text((xx + 175), (yy + 110), "Return")
        }
        else
        {
            draw_text((xx + 85), (yy + 110), "File saved.")
            if keyboard_multicheck_pressed(13)
            {
                global.menuno = -1
                global.interact = 0
                global.menucoord[4] = 0
                keyboard_clear(vk_return)
            }
        }
        if (keyboard_check_pressed(vk_left) || keyboard_check_pressed(vk_right))
        {
            if (global.menucoord[4] < 2)
            {
                if (global.menucoord[4] == 1)
                    global.menucoord[4] = 0
                else
                    global.menucoord[4] = 1
                keyboard_clear(vk_left)
                keyboard_clear(vk_right)
            }
        }
        if (keyboard_multicheck_pressed(13) && global.menucoord[4] == 0)
        {
            snd_play(snd_save)
            script_execute(scr_save)
            global.menucoord[4] = 2
            keyboard_clear(vk_return)
        }
        if (keyboard_multicheck_pressed(13) && global.menucoord[4] == 1)
        {
            global.menuno = -1
            global.interact = 0
            global.menucoord[4] = 0
            keyboard_clear(vk_return)
        }
        if keyboard_multicheck_pressed(16)
        {
            global.menuno = -1
            global.interact = 0
            global.menucoord[4] = 0
            keyboard_clear(vk_shift)
        }
    }
    if (global.menuno == 0)
        draw_sprite(spr_heartsmall, 0, (28 + xx), ((88 + yy) + (18 * global.menucoord[0])))
    if (global.menuno == 1)
        draw_sprite(spr_heartsmall, 0, (104 + xx), ((34 + yy) + (16 * global.menucoord[1])))
    if (global.menuno == 3)
        draw_sprite(spr_heartsmall, 0, (104 + xx), ((34 + yy) + (16 * global.menucoord[3])))
    if (global.menuno == 6)
        draw_sprite(spr_heartsmall, 0, (104 + xx), ((34 + yy) + (16 * global.menucoord[6])))
    if (global.menuno == 7)
        draw_sprite(spr_heartsmall, 0, (104 + xx), ((34 + yy) + (16 * global.menucoord[7])))
    if (global.menuno == 5)
    {
        if (global.menucoord[5] == 0)
            draw_sprite(spr_heartsmall, 0, ((104 + xx) + (45 * global.menucoord[5])), (174 + yy))
        if (global.menucoord[5] == 1)
            draw_sprite(spr_heartsmall, 0, ((104 + xx) + ((45 * global.menucoord[5]) + 3)), (174 + yy))
        if (global.menucoord[5] == 2)
            draw_sprite(spr_heartsmall, 0, ((104 + xx) + ((45 * global.menucoord[5]) + 15)), (174 + yy))
    }
    if keyboard_multicheck_pressed(13)
    {
        if (global.menuno == 5)
        {
            if (global.menucoord[5] == 0)
            {
                global.menuno = 9
                script_execute(scr_itemuseb, global.menucoord[1], global.item[global.menucoord[1]])
            }
            if (global.menucoord[5] == 1)
            {
                global.menuno = 9
                script_execute(scr_itemdesc, global.item[global.menucoord[1]])
                script_execute(scr_writetext, 0, "x", 0, 0)
            }
            if (global.menucoord[5] == 2)
            {
                global.menuno = 9
                dontthrow = 0
                if (global.item[global.menucoord[1]] != 23 && global.item[global.menucoord[1]] != 27 && global.item[global.menucoord[1]] != 54 && global.item[global.menucoord[1]] != 56 && global.item[global.menucoord[1]] != 57)
                    script_execute(scr_writetext, 12, "x", 0, 0)
                else
                {
                    if (global.item[global.menucoord[1]] == 23)
                        script_execute(scr_writetext, 23, "x", 0, 0)
                    if (global.item[global.menucoord[1]] == 27)
                    {
                        script_execute(scr_writetext, 0, "* (You put the dog on the&  ground.)/%%", 0, 0)
                        if instance_exists(obj_rarependant)
                        {
                            with (obj_rarependant)
                                con = 1
                        }
                    }
                    if (global.item[global.menucoord[1]] == 54)
                    {
                        script_execute(scr_writetext, 0, "* (You threw the Bad Memory&  away.^1)&* (But it came back.)/%%", 0, 0)
                        dontthrow = 1
                    }
                    if (global.item[global.menucoord[1]] == 56)
                    {
                        if (!instance_exists(obj_undyne_friendc))
                        {
                            script_execute(scr_writetext, 0, "* (Despite what seems like&  common sense^1, you threw&  away the letter.)/%%", 0, 0)
                            global.flag[494] = 1
                        }
                        else
                        {
                            global.faceemotion = 1
                            script_execute(scr_writetext, 0, "* Hey^1! Don't throw that&  away^1! Just deliver it!/%%", 5, 37)
                            dontthrow = 1
                        }
                    }
                    if (global.item[global.menucoord[1]] == 57)
                    {
                        script_execute(scr_writetext, 0, "* (The letter is too powerful to&  throw away.^1)&* (It gets the better of you.)/%%", 0, 0)
                        dontthrow = 1
                    }
                }
                if (dontthrow == 0)
                    script_execute(scr_itemshift, global.menucoord[1], 0)
            }
        }
        if (global.menuno == 3)
        {
            global.menuno = 9
            script_execute(scr_itemuseb, global.menucoord[3], global.phone[global.menucoord[3]])
        }
        if (global.menuno == 6)
        {
            global.menuno = 9
            script_execute(scr_storageget, global.item[global.menucoord[6]], 300)
            if (noroom == 0)
            {
                script_execute(scr_writetext, 16, "x", 0, 0)
                script_execute(scr_itemshift, global.menucoord[6], 0)
            }
            else
                script_execute(scr_writetext, 19, "x", 0, 0)
        }
        if (global.menuno == 7)
        {
            global.menuno = 9
            script_execute(scr_itemget, global.flag[(global.menucoord[7] + 300)])
            if (noroom == 0)
            {
                script_execute(scr_writetext, 17, "x", 0, 0)
                scr_storageshift(global.menucoord[7], 0, 300)
            }
            else
                script_execute(scr_writetext, 18, "x", 0, 0)
        }
        if (global.menuno == 1)
        {
            global.menuno = 5
            global.menucoord[5] = 0
        }
        if (global.menuno == 0)
            global.menuno += (global.menucoord[0] + 1)
        if (global.menuno == 3)
        {
            script_execute(scr_phonename)
            global.menucoord[3] = 0
        }
        if (global.menuno == 1)
        {
            if (global.item[0] != 0)
            {
                global.menucoord[1] = 0
                script_execute(scr_itemname)
            }
            else
                global.menuno = 0
        }
    }
    if keyboard_check_pressed(vk_up)
    {
        if (global.menuno == 0)
        {
            if (global.menucoord[0] != 0)
                global.menucoord[0] -= 1
        }
        if (global.menuno == 1)
        {
            if (global.menucoord[1] != 0)
                global.menucoord[1] -= 1
        }
        if (global.menuno == 3)
        {
            if (global.menucoord[3] != 0)
                global.menucoord[3] -= 1
        }
        if (global.menuno == 6)
        {
            if (global.menucoord[6] != 0)
                global.menucoord[6] -= 1
        }
        if (global.menuno == 7)
        {
            if (global.menucoord[7] != 0)
                global.menucoord[7] -= 1
        }
    }
    if keyboard_check_pressed(vk_down)
    {
        if (global.menuno == 0)
        {
            if (global.menucoord[0] != 2)
            {
                if (global.menuchoice[(global.menucoord[0] + 1)] != 0)
                    global.menucoord[0] += 1
            }
        }
        if (global.menuno == 1)
        {
            if (global.menucoord[1] != 7)
            {
                if (global.item[(global.menucoord[1] + 1)] != 0)
                    global.menucoord[1] += 1
            }
        }
        if (global.menuno == 3)
        {
            if (global.menucoord[3] != 7)
            {
                if (global.phone[(global.menucoord[3] + 1)] != 0)
                    global.menucoord[3] += 1
            }
        }
        if (global.menuno == 6)
        {
            if (global.menucoord[6] != 7)
            {
                if (global.item[(global.menucoord[6] + 1)] != 0)
                    global.menucoord[6] += 1
            }
        }
        if (global.menuno == 7)
        {
            if (global.menucoord[7] != 9)
            {
                if (global.flag[(global.menucoord[7] + 301)] != 0)
                    global.menucoord[7] += 1
            }
        }
    }
    if (keyboard_multicheck_pressed(16) && buffer >= 0)
    {
        if (global.menuno == 0)
        {
            global.menuno = -1
            global.interact = 0
        }
        else if (global.menuno <= 3)
            global.menuno = 0
        if (global.menuno == 5)
            global.menuno = 1
    }
    if keyboard_check_pressed(vk_right)
    {
        if (global.menuno == 5)
        {
            if (global.menucoord[5] != 2)
                global.menucoord[5] += 1
        }
    }
    if keyboard_check_pressed(vk_left)
    {
        if (global.menuno == 5)
        {
            if (global.menucoord[5] != 0)
                global.menucoord[5] -= 1
        }
    }
    if keyboard_multicheck_pressed(17)
    {
        if (global.menuno == 0)
        {
            global.menuno = -1
            global.interact = 0
        }
    }
    if (currentmenu < global.menuno && global.menuno != 9)
        snd_play(snd_select)
    else if (global.menuno >= 0 && global.menuno < 6)
    {
        if (currentspot != global.menucoord[global.menuno])
            snd_play(snd_squeak)
    }
}
if (global.menuno == 9 && instance_exists(obj_dialoguer) == 0)
{
    global.menuno = -1
    global.interact = 0
}
