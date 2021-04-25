var menu_max, vspacing, button_x, i, itext, xx, button, b, command, sprite, border_label, border_sel;
buffer -= 1
if (fun == true && harp == abc_123_a)
{
    harp = caster_load("music/harpnoise.ogg")
    if (weather == 1)
        weathermusic = caster_load("music/options_winter.ogg")
    if (weather == 2 || weather == 4)
        weathermusic = caster_load("music/options_fall.ogg")
    if (weather == 3)
        weathermusic = caster_load("music/options_summer.ogg")
}
if (weather == 1)
{
    c = instance_create(0, 0, obj_ct_fallobj)
    c.sprite_index = spr_christmasflake
    siner += 1
    draw_sprite_ext(spr_tobdog_winter, 0, 250, 218, 1, 1, 0, c_white, 1)
    draw_set_color(c_gray)
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_fun_winter"), 1, 1, -20)
}
if (weather == 2)
{
    c = instance_create(0, 0, obj_ct_fallobj)
    c.sprite_index = spr_fallleaf
    c.image_blend = choose(merge_color(c_red, c_white, 0.5))
    siner += 1
    draw_sprite_ext(spr_tobdog_spring, floor((siner / 15)), 250, 218, 1, 1, 0, c_white, 1)
    draw_set_color(c_gray)
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_fun_spring"), 1, 1, -20)
}
if (weather == 3)
{
    extreme2 += 1
    if (extreme2 >= 240)
    {
        extreme += 1
        if (extreme >= 1100 && abs(sin((siner / 15))) < 0.1)
        {
            extreme = 0
            extreme2 = 0
        }
    }
    siner += 1
    draw_sprite_ext(spr_tobdog_summer, floor((siner / 15)), 250, 225, (2 + (sin((siner / 15)) * (0.2 + (extreme / 900)))), (2 - (sin((siner / 15)) * (0.2 + (extreme / 900)))), 0, c_white, 1)
    draw_set_color(c_yellow)
    draw_circle((258 + (cos((siner / 18)) * 6)), (40 + (sin((siner / 18)) * 6)), (28 + (sin((siner / 6)) * 4)), 0)
    draw_set_color(c_gray)
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_fun_summer"), 1, 1, -20)
}
if (weather == 4)
{
    c = instance_create(0, 0, obj_ct_fallobj)
    c.sprite_index = spr_fallleaf
    c.image_blend = choose(65535, 4235519, 255)
    siner += 1
    draw_sprite_ext(spr_tobdog_autumn, 0, 250, 218, 1, 1, 0, c_white, 1)
    draw_set_color(c_gray)
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_fun_fall"), 1, 1, -20)
}
menu_max = 6
if (global.osflavor <= 2)
{
    menu_max = 2
    if (obj_time.j_ch == 0)
        menu_max = 1
}
if (menu_engage == 0)
{
    if keyboard_check_pressed(vk_down)
        menu += 1
    if keyboard_check_pressed(vk_up)
        menu -= 1
    if (menu <= 0)
        menu = 0
    if (menu >= menu_max)
        menu = menu_max
    if (buffer < 0 && control_check_pressed(0))
    {
        if (!(global.osflavor >= 4 && menu >= 2 && menu <= 4))
        {
            menu_engage = 1
            js_buffer = 1
            buffer = 4
        }
    }
    if (menu == 1)
    {
        if ((global.osflavor <= 2 && menu_engage == 1) || keyboard_check_pressed(vk_left) || keyboard_check_pressed(vk_right))
        {
            if (global.language == "en")
                global.language = "ja"
            else
                global.language = "en"
        }
        menu_engage = 0
    }
    if (menu == 6)
    {
        if ((global.osflavor <= 2 && menu_engage == 1) || keyboard_check_pressed(vk_right))
        {
            do {
                global.screen_border_id++
                if (global.screen_border_id > num_borders)
                    global.screen_border_id = 0
            } until (global.screen_border_id < 0 || border_enabled[global.screen_border_id]);
            
            global.screen_border_state = 0
            global.screen_border_dynamic_fade_id = 0
            global.screen_border_dynamic_fade_level = 0
            scr_enable_screen_border(1)
        }
        else if keyboard_check_pressed(vk_left)
        {
            do {
                global.screen_border_id--
                if (global.screen_border_id < 0)
                    global.screen_border_id = num_borders
            } until (global.screen_border_id < 0 || border_enabled[global.screen_border_id]);
            
            global.screen_border_state = 0
            global.screen_border_dynamic_fade_id = 0
            global.screen_border_dynamic_fade_level = 0
            scr_enable_screen_border(1)
        }
        menu_engage = 0
    }
}
if (menu == 0 && menu_engage == 1)
    finish = 1
draw_set_color(c_white)
scr_setfont(fnt_maintext)
scr_drawtext_centered_scaled(160, 10, scr_gettext("settings_title"), 2, 2)
if (menu != 0)
    draw_set_color(c_white)
else
    draw_set_color(c_yellow)
draw_text(20, 40, scr_gettext("joyconfig_exit"))
if (menu != 1)
    draw_set_color(c_white)
else
    draw_set_color(c_yellow)
draw_text(20, 70, scr_gettext("settings_language"))
draw_text(92, 70, scr_gettext(("settings_language_" + global.language)))
if (global.osflavor >= 4)
{
    draw_set_color(c_white)
    draw_text(20, 100, scr_gettext("settings_button"))
    vspacing = 18
    button_x = 0
    for (i = 0; i < 3; i++)
    {
        if (menu == (i + 2))
            draw_set_color(c_yellow)
        else
            draw_set_color(c_white)
        if (i == 0)
            itext = scr_gettext("settings_button_confirm")
        else if (i == 1)
            itext = scr_gettext("settings_button_cancel")
        else
            itext = scr_gettext("settings_button_menu")
        draw_text(40, (100 + ((i + 1) * vspacing)), itext)
        draw_set_color(c_white)
        xx = (40 + string_width(itext))
        if (xx > button_x)
            button_x = xx
    }
    button_x += 15
    for (i = 0; i < 3; i++)
    {
        if (menu == (i + 2) && buffer < 0)
        {
            button = -4
            for (b = 0; button_list[b] != -4; b++)
            {
                if gamepad_button_check(0, button_list[b])
                {
                    button = button_list[b]
                    break
                }
            }
            if (button != -4)
            {
                if (menu == 2)
                {
                    if (global.button1 == button)
                        global.button1 = global.button0
                    if (global.button2 == button)
                        global.button2 = global.button0
                    global.button0 = button
                }
                else if (menu == 3)
                {
                    if (global.button0 == button)
                        global.button0 = global.button1
                    if (global.button2 == button)
                        global.button2 = global.button1
                    global.button1 = button
                }
                else
                {
                    if (global.button0 == button)
                        global.button0 = global.button2
                    if (global.button1 == button)
                        global.button1 = global.button2
                    global.button2 = button
                }
                buffer = 4
            }
        }
        command = string_char_at("ZXC", (i + 1))
        sprite = scr_getbuttonsprite(command, 0)
        if (sprite != -4)
            draw_sprite(sprite, 0, button_x, (100 + ((i + 1) * vspacing)))
    }
    if (r_buffer > 0)
    {
        r_buffer -= 1
        draw_set_color(c_red)
        draw_text_transformed_color(20, ((100 + (3 * vspacing)) + 25), r_line, 1, 1, 0, c_red, c_red, c_red, c_red, (1 - ((10 - r_buffer) / 10)))
    }
    else
    {
        if (menu != 5)
            draw_set_color(c_white)
        else
            draw_set_color(c_yellow)
        draw_text(20, ((100 + (3 * vspacing)) + 25), scr_gettext("joyconfig_reset"))
    }
    if (menu == 5 && menu_engage == 1)
    {
        r_buffer = 15
        rrr = floor(random(50))
        if (fun == true)
        {
            if (rrr == 1)
                r_line = scr_gettext("joyconfig_spaghetted")
            else
                r_line = scr_gettext("joyconfig_resetted")
        }
        global.button0 = global.default_button0
        global.button1 = global.default_button1
        global.button2 = global.default_button2
        global.analog_sense = global.default_analog_sense
        global.analog_sense_sense = global.default_analog_sense_sense
        global.joy_dir = global.default_joy_dir
        menu_engage = 0
    }
    if (menu != 6)
        draw_set_color(c_white)
    else
        draw_set_color(c_yellow)
    border_label = scr_gettext("settings_border")
    draw_text(20, ((100 + (3 * vspacing)) + 50), border_label)
    if (global.screen_border_id == 0)
        border_sel = scr_gettext("settings_border_none")
    else if (global.screen_border_id == -1)
        border_sel = "@!%&"
    else
        border_sel = scr_gettext(("settings_border_" + string(global.screen_border_id)))
    draw_text(((20 + string_width(border_label)) + 20), ((100 + (3 * vspacing)) + 50), border_sel)
}
else if (obj_time.j_ch > 0)
{
    if (menu != 2)
        draw_set_color(c_white)
    else
        draw_set_color(c_yellow)
    draw_text(20, 100, scr_gettext("settings_joyconfig"))
}
if (intro == 1)
{
    if (rectile == 16)
        caster_play(harp, 1, 1)
    rectile += 4
    draw_set_color(c_black)
    ossafe_fill_rectangle((168 - rectile), -10, -1, 250)
    draw_set_color(c_black)
    ossafe_fill_rectangle((152 + rectile), -10, 330, 250)
    if (rectile >= 170)
    {
        caster_loop(weathermusic, 0.8, 1)
        menu_engage = 0
        buffer = 5
        intro = -1
    }
}
if (global.osflavor <= 2 && menu == 2 && menu_engage == 1)
{
    jc = instance_create(0, 0, obj_joypadmenu)
    jc.menu_engage = 0
    jc.buffer = buffer
    jc.intro = intro
    jc.rectile = rectile
    jc.buffer = buffer
    jc.weather = weather
    jc.extreme = extreme
    jc.extreme2 = extreme2
    jc.harp = harp
    jc.weathermusic = weathermusic
    instance_destroy()
}
