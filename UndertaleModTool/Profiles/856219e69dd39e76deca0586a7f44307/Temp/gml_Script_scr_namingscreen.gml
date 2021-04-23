var xx, kana_count, i, Won, CP, CH, confirm, row, yy, col, menu_text0, menu_text1, menu_text2, charset_text0, charset_text1, charset_text2, old_col, best, bestdiff, diff, roomname, lvtext, timetext, namesize, lvsize, timesize, x_center, lvpos, namepos, timepos, continue_text, reset_text, action, lines, num_lines, label_x, key, label, yy2;
draw_set_color(c_white)
scr_setfont(fnt_maintext)
if (naming == 4)
{
    global.charname = charname
    instance_create(0, 0, obj_whitefader)
    caster_free(-3)
    alerm = 0
    naming = 5
    cy = caster_load("music/cymbal.ogg")
    caster_play(cy, 0.8, 0.95)
}
if (naming == 5)
{
    alerm += 1
    if (q < 120)
        q += 1
    xx = (name_x - (q / 3))
    if (global.language == "ja")
    {
        kana_count = 0
        for (i = 0; i < strlen(charname); i++)
        {
            if (ord(string_char_at(charname, i)) >= 12288)
                kana_count++
        }
        if (kana_count > 1)
            xx -= ((kana_count - 1) * (q / 10))
    }
    draw_text_transformed((xx + random((r * 2))), (((q / 2) + name_y) + random((r * 2))), charname, (1 + (q / 50)), (1 + (q / 50)), random_ranger((((-r) * q) / 60), ((r * q) / 60)))
    if (alerm > 179)
    {
        instance_create(0, 0, obj_persistentfader)
        if (truereset > 0)
        {
            ossafe_ini_open("undertale.ini")
            sk = ini_read_real("reset", "s_key", 0)
            Won = ini_read_real("General", "Won", 0)
            CP = ini_read_real("General", "CP", 0)
            CH = ini_read_real("General", "CH", 0)
            ossafe_ini_close()
            if ossafe_file_exists("undertale.ini")
                ossafe_file_delete("undertale.ini")
            ossafe_ini_open("undertale.ini")
            ini_write_real("reset", "reset", 1)
            if (sk != 0)
                ini_write_real("reset", "s_key", sk)
            if (Won != 0)
                ini_write_real("General", "BW", Won)
            if (CP != 0)
                ini_write_real("General", "BP", CP)
            if (CH != 0)
                ini_write_real("General", "BH", CH)
            ossafe_ini_close()
        }
        caster_free(cy)
        global.flag[5] = (floor(random(100)) + 1)
        ossafe_ini_open("undertale.ini")
        ini_write_real("General", "fun", global.flag[5])
        ossafe_ini_close()
        ossafe_savedata_save()
        with (obj_time)
            time = 0
        if scr_hardmodename(charname)
            global.flag[6] = 1
        room_goto_next()
    }
}
if (naming == 2)
{
    if (charname == "")
    {
        spec_m = scr_gettext("name_entry_missing")
        allow = 0
    }
    else if scr_hardmodename(charname)
    {
        spec_m = scr_gettext("name_entry_hardmode")
        allow = 1
    }
    else if (hasname == 1 && truereset == 0 && (!scr_hardmodename(global.charname)))
    {
        spec_m = scr_gettext("name_entry_already")
        allow = 1
    }
    else
        scr_namingscreen_check(charname)
    confirm = (control_check_pressed(0) && selected2 >= 0)
    if confirm
    {
        if allow
        {
            if (selected2 == 1 && string_length(charname) > 0)
                naming = 4
        }
        if (selected2 == 0)
        {
            if (hasname == 1 && truereset == 0)
                naming = 3
            else
                naming = 1
        }
        exit
    }
    draw_set_color(c_white)
    if (q < 120)
        q += 1
    xx = (name_x - (q / 3))
    if (global.language == "ja")
    {
        kana_count = 0
        for (i = 0; i < strlen(charname); i++)
        {
            if (ord(string_char_at(charname, i)) >= 12288)
                kana_count++
        }
        if (kana_count > 1)
            xx -= ((kana_count - 1) * (q / 10))
    }
    draw_text_transformed((xx + random((r * 2))), (((q / 2) + name_y) + random((r * 2))), charname, (1 + (q / 50)), (1 + (q / 50)), random_ranger((((-r) * q) / 60), ((r * q) / 60)))
    draw_text(90, 30, spec_m)
    draw_set_color(c_white)
    if allow
    {
        if (selected2 == 0)
            draw_set_color(c_yellow)
        scr_drawtext_centered(80, 200, scr_gettext("no"))
        draw_set_color(c_white)
        if (selected2 == 1)
            draw_set_color(c_yellow)
        scr_drawtext_centered(240, 200, scr_gettext("yes"))
    }
    else
    {
        draw_set_color(c_yellow)
        scr_drawtext_centered(80, 200, scr_gettext("name_entry_goback"))
        draw_set_color(c_white)
    }
    if allow
    {
        if (keyboard_check_pressed(vk_right) || keyboard_check_pressed(vk_left))
        {
            if (selected2 == 1)
                selected2 = 0
            else
                selected2 = 1
        }
    }
}
if (naming == 1)
{
    q = 0
    r = 0.5
    for (row = 0; row < rows; row++)
    {
        yy = ymap[row]
        for (col = 0; col < cols; col++)
        {
            xx = xmap[col]
            if (selected_row == row && selected_col == col)
                draw_set_color(c_yellow)
            else
                draw_set_color(c_white)
            draw_text((xx + random(r)), (yy + random(r)), charmap[row, col])
        }
    }
    draw_set_color(c_white)
    if (selected_row == -1 && selected_col == 0)
        draw_set_color(c_yellow)
    menu_text0 = scr_gettext("name_entry_quit")
    draw_text(menu_x0, menu_y, menu_text0)
    draw_set_color(c_white)
    if (selected_row == -1 && selected_col == 1)
        draw_set_color(c_yellow)
    menu_text1 = scr_gettext("name_entry_backspace")
    draw_text(menu_x1, menu_y, menu_text1)
    draw_set_color(c_white)
    if (selected_row == -1 && selected_col == 2)
        draw_set_color(c_yellow)
    menu_text2 = scr_gettext("name_entry_done")
    draw_text(menu_x2, menu_y, menu_text2)
    if (global.language == "ja")
    {
        draw_set_color(c_white)
        if (selected_row == -2 && selected_col == 0)
            draw_set_color(c_yellow)
        charset_text0 = "ひらがな"
        draw_text(charset_x0, charset_y, charset_text0)
        draw_set_color(c_white)
        if (selected_row == -2 && selected_col == 1)
            draw_set_color(c_yellow)
        charset_text1 = "カタカナ"
        draw_text(charset_x1, charset_y, charset_text1)
        draw_set_color(c_white)
        if (selected_row == -2 && selected_col == 2)
            draw_set_color(c_yellow)
        charset_text2 = "アルファベット"
        draw_text(charset_x2, charset_y, charset_text2)
    }
    old_col = selected_col
    do
    {
        if keyboard_check_pressed(vk_right)
        {
            selected_col++
            if (selected_row == -1)
            {
                if (selected_col > 2)
                    selected_col = 0
            }
            else if (selected_col >= cols)
            {
                if (selected_row == (rows - 1))
				{
                    selected_col = old_col
					break
				}
                else
                {
                    selected_col = 0
                    selected_row++
                }
            }
        }
        if keyboard_check_pressed(vk_left)
        {
            selected_col--
            if (selected_col < 0)
            {
                if (selected_row == 0)
                    selected_col = 0
                else if (selected_row > 0)
                {
                    selected_col = (cols - 1)
                    selected_row--
                }
                else
                    selected_col = 2
            }
        }
        if keyboard_check_pressed(vk_down)
        {
            if (selected_row == -1)
            {
                selected_row = 0
                xx = menu_x0
                if (selected_col == 1)
                    xx = menu_x1
                if (selected_col == 2)
                    xx = menu_x2
                best = 0
                bestdiff = abs((xmap[0] - xx))
                for (i = 1; i < cols; i++)
                {
                    diff = abs((xmap[i] - xx))
                    if (diff < bestdiff)
                    {
                        best = i
                        bestdiff = diff
                    }
                }
                selected_col = best
            }
            else
            {
                selected_row++
                if (selected_row >= rows)
                {
                    if (global.language == "ja")
                    {
                        selected_row = -2
                        xx = xmap[selected_col]
                        if (xx >= (charset_x2 - 10))
                            selected_col = 2
                        else if (xx >= (charset_x1 - 10))
                            selected_col = 1
                        else
                            selected_col = 0
                    }
                    else
                    {
                        selected_row = -1
                        xx = xmap[selected_col]
                        if (xx >= (menu_x2 - 10))
                            selected_col = 2
                        else if (xx >= (menu_x1 - 10))
                            selected_col = 1
                        else
                            selected_col = 0
                    }
                }
            }
        }
        if keyboard_check_pressed(vk_up)
        {
            if (selected_row == -2)
            {
                selected_row = (rows - 1)
                if (selected_col > 0)
                {
                    xx = charset_x1
                    if (selected_col == 2)
                        xx = charset_x2
                    best = 0
                    bestdiff = abs((xmap[0] - xx))
                    for (i = 1; i < cols; i++)
                    {
                        diff = abs((xmap[i] - xx))
                        if (diff < bestdiff)
                        {
                            best = i
                            bestdiff = diff
                        }
                    }
                    selected_col = best
                }
            }
            else if (global.language != "ja" && selected_row == -1)
            {
                selected_row = (rows - 1)
                if (selected_col > 0)
                {
                    xx = menu_x1
                    if (selected_col == 2)
                        xx = menu_x2
                    best = 0
                    bestdiff = abs((xmap[0] - xx))
                    for (i = 1; i < cols; i++)
                    {
                        diff = abs((xmap[i] - xx))
                        if (diff < bestdiff)
                        {
                            best = i
                            bestdiff = diff
                        }
                    }
                    selected_col = best
                }
            }
            else
            {
                selected_row--
                if (selected_row == -1)
                {
                    xx = xmap[selected_col]
                    if (xx >= (menu_x2 - 10))
                        selected_col = 2
                    else if (xx >= (menu_x1 - 10))
                        selected_col = 1
                    else
                        selected_col = 0
                }
            }
        }
    }
    until (selected_col < 0 || selected_row < 0 || string_length(charmap[selected_row, selected_col]) > 0);
    bks_f = 0
    confirm = control_check_pressed(0)
    if confirm
    {
        if (selected_row == -1)
        {
            if (selected_col == 0)
                naming = 3
            if (selected_col == 1)
                bks_f = 1
            if (selected_col == 2)
            {
                if (string_length(charname) > 0)
                {
                    naming = 2
                    selected2 = 0
                }
            }
            control_clear(0)
        }
        else if (selected_row == -2)
        {
            selected_charmap = (1 + selected_col)
            if (selected_charmap == 1)
            {
                rows = hiragana_rows
                cols = hiragana_cols
                xmap = hiragana_x
                ymap = hiragana_y
                charmap = hiragana_charmap
            }
            else if (selected_charmap == 2)
            {
                rows = katakana_rows
                cols = katakana_cols
                xmap = katakana_x
                ymap = katakana_y
                charmap = katakana_charmap
            }
            else
            {
                rows = ja_ascii_rows
                cols = ja_ascii_cols
                xmap = ja_ascii_x
                ymap = ja_ascii_y
                charmap = ja_ascii_charmap
            }
        }
        else
        {
            if (string_length(charname) == 6)
                charname = string_delete(charname, 6, 1)
            charname += charmap[selected_row, selected_col]
        }
    }
    if (control_check_pressed(1) || bks_f == 1)
    {
        s = string_length(charname)
        if (s > 0)
            charname = string_delete(charname, s, 1)
        control_clear(1)
    }
    draw_set_color(c_white)
    draw_text(name_x, name_y, charname)
    scr_drawtext_centered(160, title_y, scr_gettext("name_entry_title"))
}
if (naming == 3)
{
    iniread = ossafe_ini_open("undertale.ini")
    if (ini_section_exists("General") && hasname == 1)
    {
        minutes = floor((time / 1800))
        seconds = round((((time / 1800) - minutes) * 60))
        if (seconds == 60)
            seconds = 0
        if (seconds < 10)
            seconds = ("0" + string(seconds))
        roomname = scr_roomname(roome)
        lvtext = scr_gettext("save_menu_lv", string(love))
        timetext = scr_gettext("save_menu_time", string(minutes), string(seconds))
        namesize = string_width(substr(name, 1, 6))
        lvsize = string_width(lvtext)
        timesize = string_width(timetext)
        x_center = 160
        lvpos = round((((x_center + (namesize / 2)) - (timesize / 2)) - (lvsize / 2)))
        namepos = 70
        timepos = 250
        if (global.language == "ja")
        {
            namepos -= 6
            timepos += 6
        }
        draw_text(namepos, 62, name)
        draw_text(lvpos, 62, lvtext)
        draw_text((timepos - timesize), 62, timetext)
        if (global.language == "ja")
            scr_drawtext_centered(x_center, 80, roomname)
        else
            draw_text(namepos, 80, roomname)
        if (selected3 == 0)
            draw_set_color(c_yellow)
        continue_text = scr_gettext("load_menu_continue")
        draw_text(continue_x, 105, continue_text)
        draw_set_color(c_white)
        draw_set_color(c_white)
        if (selected3 == 2)
            draw_set_color(c_yellow)
        scr_drawtext_centered(160, 125, scr_gettext("settings_name"))
        draw_set_color(c_white)
        if (selected3 == 1)
            draw_set_color(c_yellow)
        if (truereset == 0)
            reset_text = scr_gettext("load_menu_reset")
        else
            reset_text = scr_gettext("load_menu_truereset")
        draw_text(reset_x, 105, reset_text)
        if (keyboard_check_pressed(vk_right) || keyboard_check_pressed(vk_left))
        {
            if (selected3 == 0)
                selected3 = 1
            else if (selected3 == 1)
                selected3 = 0
            keyboard_clear(vk_left)
            keyboard_clear(vk_right)
        }
        if keyboard_check_pressed(vk_down)
        {
            if (selected3 == 0 || selected3 == 1)
                selected3 = 2
            keyboard_clear(vk_down)
        }
        if keyboard_check_pressed(vk_up)
        {
            if (selected3 == 2)
                selected3 = 0
            keyboard_clear(vk_down)
        }
        action = -1
        if control_check_pressed(0)
            action = selected3
        if (action == 0)
        {
            caster_free(-3)
            if (ossafe_file_exists("file0") == 0)
                room_goto_next()
            else
                script_execute(scr_load)
        }
        if (action == 1)
        {
            if (hasname == 0 || scr_hardmodename(global.charname) || truereset > 0)
                naming = 1
            else
            {
                charname = global.charname
                naming = 2
                alerm = 0
                r = 0.5
                q = 0
            }
            control_clear(0)
        }
        if (action == 2)
        {
            caster_free(-3)
            room_goto(room_settings)
        }
    }
    else
    {
        draw_set_color(c_silver)
        draw_text(85, 20, scr_gettext("instructions_title"))
        if (global.osflavor >= 4)
        {
            scr_drawtext_icons(85, 50, "\*Z")
            draw_text(115, 50, scr_gettext("instructions_confirm_label"))
            scr_drawtext_icons(85, 70, "\*X")
            draw_text(115, 70, scr_gettext("instructions_cancel_label"))
            scr_drawtext_icons(85, 90, "\*C")
            draw_text(115, 90, scr_gettext("instructions_menu_label"))
            draw_text(86, 130, scr_gettext("instructions_hp0"))
        }
        else
        {
            lines[0] = "confirm"
            lines[1] = "cancel"
            lines[2] = "menu"
            lines[3] = "fullscreen"
            lines[4] = "quit"
            num_lines = 5
            if (global.language == "ja")
            {
                label_x = 0
                for (i = 0; i < num_lines; i++)
                {
                    key = scr_gettext((("instructions_" + lines[i]) + "_key"))
                    draw_text(50, (45 + (i * 18)), key)
                    xx = ((50 + string_width(key)) + 20)
                    if (xx > label_x)
                        label_x = xx
                }
                for (i = 0; i < num_lines; i++)
                {
                    label = scr_gettext((("instructions_" + lines[i]) + "_label"))
                    draw_text(label_x, (45 + (i * 18)), label)
                }
                draw_text(50, 145, scr_gettext("instructions_hp0"))
            }
            else
            {
                for (i = 0; i < num_lines; i++)
                {
                    key = scr_gettext((("instructions_" + lines[i]) + "_key"))
                    label = scr_gettext((("instructions_" + lines[i]) + "_label"))
                    draw_text(85, (50 + (i * 18)), ((key + " - ") + label))
                }
                draw_text(85, 140, scr_gettext("instructions_hp0"))
            }
        }
        xx = 85
        if (global.language == "ja")
            xx = 84
        yy = 160
        if (global.osflavor <= 2)
            yy += 12
        draw_set_color(c_white)
        if (selected3 == 0)
            draw_set_color(c_yellow)
        draw_text(xx, yy, scr_gettext("instructions_begin"))
        if keyboard_check_pressed(vk_down)
        {
            if (selected3 == 0)
                selected3 = 1
        }
        if keyboard_check_pressed(vk_up)
        {
            if (selected3 == 1)
                selected3 = 0
        }
        yy2 = (yy + 20)
        draw_set_color(c_white)
        if (selected3 == 1)
            draw_set_color(c_yellow)
        draw_text(xx, yy2, scr_gettext("settings_name"))
        action = -1
        if control_check_pressed(0)
            action = selected3
        if (action == 0)
        {
            naming = 1
            control_clear(1)
        }
        if (action == 1)
        {
            caster_free(-3)
            room_goto(room_settings)
        }
    }
}
