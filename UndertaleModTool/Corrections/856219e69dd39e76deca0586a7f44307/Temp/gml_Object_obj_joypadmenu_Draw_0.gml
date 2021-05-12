var vspacing, button_x, xx, stext, analog_text_width, sensebar_x;
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
if (menu_engage == 0)
{
    if keyboard_check_pressed(vk_down)
        menu += 1
    if (menu == 4)
        menu = 5
    if (fun == false)
    {
        if (menu == 6)
            menu = 7
    }
    if keyboard_check_pressed(vk_up)
        menu -= 1
    if (menu == 4)
        menu = 3
    if (fun == false)
    {
        if (menu == 6)
            menu = 5
    }
    if (menu <= 0)
        menu = 0
    if (menu >= 9)
        menu = 9
    if (buffer < 0)
    {
        if control_check_pressed(0)
        {
            menu_engage = 1
            js_buffer = 1
            buffer = 4
        }
    }
}
if (menu == 0 && menu_engage == 1)
{
    ossafe_ini_open("config.ini")
    ini_write_real("joypad1", "b0", global.button0)
    ini_write_real("joypad1", "b1", global.button1)
    ini_write_real("joypad1", "b2", global.button2)
    ini_write_real("joypad1", "as", global.analog_sense)
    ini_write_real("joypad1", "jd", global.joy_dir)
    ossafe_ini_close()
    sm = instance_create(0, 0, obj_settingsmenu)
    sm.menu_engage = 0
    sm.buffer = buffer
    sm.intro = intro
    sm.rectile = rectile
    sm.buffer = buffer
    sm.weather = weather
    sm.extreme = extreme
    sm.extreme2 = extreme2
    sm.harp = harp
    sm.weathermusic = weathermusic
    instance_destroy()
}
draw_set_color(c_white)
scr_setfont(fnt_maintext)
if (weather != 3)
{
    draw_set_halign(fa_right)
    draw_text_transformed(300, 10, scr_gettext("joyconfig_title"), 2, 2, 0)
    draw_set_halign(fa_left)
}
else
    draw_text_transformed(10, 10, scr_gettext("joyconfig_title"), 2, 2, 0)
if (menu != 0)
    draw_set_color(c_white)
else
    draw_set_color(c_yellow)
draw_text(20, 40, scr_gettext("joyconfig_exit"))
vspacing = 15
if (global.language == "ja")
    vspacing = 16
button_x = 0
for (i = 1; i < 4; i += 1)
{
    if (menu != i)
        draw_set_color(c_white)
    else
        draw_set_color(c_yellow)
    if (i == 1)
        itext = scr_gettext("joyconfig_button_confirm")
    if (i == 2)
        itext = scr_gettext("joyconfig_button_cancel")
    if (i == 3)
        itext = scr_gettext("joyconfig_button_menu")
    draw_text(20, (75 + ((i - 1) * vspacing)), itext)
    xx = ((20 + string_width(itext)) + 10)
    if (xx > button_x)
        button_x = xx
    draw_set_color(c_white)
}
stext = scr_gettext("settings_button_separator")
for (i = 1; i < 4; i += 1)
{
    if (menu != i)
        draw_set_color(c_white)
    else
        draw_set_color(c_yellow)
    draw_text(button_x, (75 + ((i - 1) * vspacing)), stext)
    draw_set_color(c_white)
}
button_x += string_width(stext)
if (menu == 1 && menu_engage == 1)
{
    draw_set_color(c_blue)
    ossafe_fill_rectangle(button_x, 75, (button_x + string_width(scr_gettext("joyconfig_prompt_button"))), 90)
    draw_set_color(c_white)
    o_o += 1
    if (o_o >= 16)
        o_o = 0
    if (o_o <= 8)
        draw_text(button_x, 75, scr_gettext("joyconfig_prompt_button"))
    if (obj_time.j_ch > 0 && js_buffer == 0)
    {
        for (i = 0; i < joystick_buttons(obj_time.j_ch); i += 1)
        {
            if joystick_check_button(obj_time.j_ch, i)
            {
                global.button0 = i
                menu_engage = 0
                buffer = 4
                break
            }
        }
    }
    if (js_buffer == 1)
    {
        bt = 0
        for (i = 0; i < joystick_buttons(obj_time.j_ch); i += 1)
        {
            if (!joystick_check_button(obj_time.j_ch, i))
                bt += 1
        }
        if (bt >= joystick_buttons(obj_time.j_ch))
            js_buffer = 0
    }
    if (buffer < 0)
    {
        if (control_check_pressed(0) || control_check_pressed(1))
            menu_engage = 0
    }
}
else
{
    draw_set_color(c_aqua)
    draw_text((button_x + 10), 75, global.button0)
}
if (menu == 2 && menu_engage == 1)
{
    draw_set_color(c_blue)
    ossafe_fill_rectangle(button_x, (75 + (1 * vspacing)), (button_x + string_width(scr_gettext("joyconfig_prompt_button"))), (90 + (1 * vspacing)))
    draw_set_color(c_white)
    o_o += 1
    if (o_o >= 16)
        o_o = 0
    if (o_o <= 8)
        draw_text(button_x, (75 + (1 * vspacing)), scr_gettext("joyconfig_prompt_button"))
    if (js_buffer == 0)
    {
        if (obj_time.j_ch > 0)
        {
            for (i = 0; i < joystick_buttons(obj_time.j_ch); i += 1)
            {
                if joystick_check_button(obj_time.j_ch, i)
                {
                    global.button1 = i
                    menu_engage = 0
                    break
                }
            }
        }
    }
    if (js_buffer == 1)
    {
        bt = 0
        for (i = 0; i < joystick_buttons(obj_time.j_ch); i += 1)
        {
            if (!joystick_check_button(obj_time.j_ch, i))
                bt += 1
        }
        if (bt >= joystick_buttons(obj_time.j_ch))
            js_buffer = 0
    }
    if (buffer < 0)
    {
        if (control_check_pressed(0) || control_check_pressed(1))
            menu_engage = 0
    }
}
else
{
    draw_set_color(c_aqua)
    draw_text((button_x + 10), (75 + (1 * vspacing)), global.button1)
}
if (menu == 3 && menu_engage == 1)
{
    draw_set_color(c_blue)
    ossafe_fill_rectangle(button_x, (75 + (2 * vspacing)), (button_x + string_width(scr_gettext("joyconfig_prompt_button"))), (90 + (2 * vspacing)))
    draw_set_color(c_white)
    o_o += 1
    if (o_o >= 16)
        o_o = 0
    if (o_o <= 8)
        draw_text(button_x, (75 + (2 * vspacing)), scr_gettext("joyconfig_prompt_button"))
    if (obj_time.j_ch > 0 && js_buffer == 0)
    {
        for (i = 0; i < joystick_buttons(obj_time.j_ch); i += 1)
        {
            if joystick_check_button(obj_time.j_ch, i)
            {
                global.button2 = i
                menu_engage = 0
                break
            }
        }
    }
    if (js_buffer == 1)
    {
        bt = 0
        for (i = 0; i < joystick_buttons(obj_time.j_ch); i += 1)
        {
            if (!joystick_check_button(obj_time.j_ch, i))
                bt += 1
        }
        if (bt >= joystick_buttons(obj_time.j_ch))
            js_buffer = 0
    }
    if (buffer < 0)
    {
        if (control_check_pressed(0) || control_check_pressed(1))
            menu_engage = 0
    }
}
else
{
    draw_set_color(c_aqua)
    draw_text((button_x + 10), (75 + (2 * vspacing)), global.button2)
}
if (menu == 5)
    draw_set_color(c_yellow)
else
    draw_set_color(c_white)
draw_text(20, 135, scr_gettext("joyconfig_analog_sens"))
analog_text_width = string_width(scr_gettext("joyconfig_analog_sens"))
sensebar_x = ((20 + analog_text_width) + 10)
if (menu == 5 && menu_engage == 1)
{
    draw_sprite_ext(spr_sensing_slider, 0, sensebar_x, 135, 1, 1, 0, c_red, 1)
    draw_sprite_ext(spr_sensebar, 0, ((sensebar_x + 40) - (global.analog_sense * 100)), 138, 1, 1, 0, c_yellow, 1)
    if (buffer < 0)
    {
        if obj_time.right
            global.analog_sense -= global.analog_sense_sense
        if obj_time.left
            global.analog_sense += global.analog_sense_sense
        if (global.analog_sense >= 0.4)
            global.analog_sense = 0.4
        if (global.analog_sense <= 0.02)
            global.analog_sense = 0.02
        if (control_check_pressed(0) || control_check_pressed(1))
            menu_engage = 0
    }
}
else
{
    draw_sprite_ext(spr_sensing_slider, 0, sensebar_x, 135, 1, 1, 0, c_red, 0.6)
    draw_sprite_ext(spr_sensebar, 0, ((sensebar_x + 40) - (global.analog_sense * 100)), 138, 1, 1, 0, c_yellow, 0.6)
}
if (fun == true)
{
    if (menu == 6)
        draw_set_color(c_yellow)
    else
        draw_set_color(c_white)
    scale = (analog_text_width / string_width(scr_gettext("joyconfig_analog_sens_sq")))
    draw_text_transformed(20, (135 + vspacing), scr_gettext("joyconfig_analog_sens_sq"), scale, 1, 0)
    if (menu == 6 && menu_engage == 1)
    {
        draw_sprite_ext(spr_sensing_slider, 0, sensebar_x, (135 + vspacing), 1, 1, 0, c_green, 1)
        draw_sprite_ext(spr_sensebar, 0, ((sensebar_x + 40) - (global.analog_sense_sense * 200)), (138 + vspacing), 1, 1, 0, c_yellow, 1)
        if (buffer < 0)
        {
            if obj_time.right
                global.analog_sense_sense -= 0.01
            if obj_time.left
                global.analog_sense_sense += 0.01
            if (global.analog_sense_sense >= 0.2)
                global.analog_sense_sense = 0.2
            if (global.analog_sense_sense <= 0.01)
                global.analog_sense_sense = 0.01
            if (control_check_pressed(0) || control_check_pressed(1))
                menu_engage = 0
        }
    }
    else
    {
        draw_sprite_ext(spr_sensing_slider, 0, sensebar_x, (135 + vspacing), 1, 1, 0, c_green, 0.6)
        draw_sprite_ext(spr_sensebar, 0, ((sensebar_x + 40) - (global.analog_sense_sense * 200)), (138 + vspacing), 1, 1, 0, c_yellow, 0.6)
    }
}
if (menu == 7)
    draw_set_color(c_yellow)
else
    draw_set_color(c_white)
draw_text(20, 170, scr_gettext("joyconfig_dir_choice"))
if (global.joy_dir == 0)
    draw_text(100, 170, scr_gettext("joyconfig_dir_normal"))
if (global.joy_dir == 1)
    draw_text(100, 170, scr_gettext("joyconfig_dir_analog"))
if (global.joy_dir == 2)
    draw_text(100, 170, scr_gettext("joyconfig_dir_pov"))
if (menu == 7 && menu_engage == 1)
{
    global.joy_dir += 1
    if (global.joy_dir >= 3)
        global.joy_dir = 0
    menu_engage = 0
}
if (r_buffer > 0)
{
    r_buffer -= 1
    draw_set_color(c_red)
    draw_text_transformed_color(20, (170 + vspacing), r_line, 1, 1, 0, c_red, c_red, c_red, c_red, (1 - ((10 - r_buffer) / 10)))
}
else
{
    if (menu == 8)
        draw_set_color(c_yellow)
    else
        draw_set_color(c_white)
    draw_text(20, (170 + vspacing), scr_gettext("joyconfig_reset"))
}
if (menu == 8 && menu_engage == 1)
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
    global.button0 = 2
    global.button1 = 1
    global.button2 = 4
    global.analog_sense = 0.2
    global.analog_sense_sense = 0.01
    global.joy_dir = 0
    if (obj_time.j_ch > 0)
    {
        if (!joystick_has_pov(obj_time.j_ch))
            global.joypad_dir = 1
    }
    menu_engage = 0
}
if (menu == 9)
    draw_set_color(c_yellow)
else
    draw_set_color(c_white)
draw_text(20, (170 + (2 * vspacing)), scr_gettext("joyconfig_test"))
if (menu == 9 && menu_engage == 1)
{
    caster_free(-3)
    room_goto(room_controltest)
}
if (weather == 1)
{
    c = instance_create(0, 0, obj_ct_fallobj)
    c.sprite_index = spr_christmasflake
    siner += 1
    draw_sprite_ext(spr_tobdog_winter, 0, 250, 218, 1, 1, 0, c_white, 1)
    draw_set_color(c_gray)
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_test_winter"), 1, 1, -20)
}
if (weather == 2)
{
    c = instance_create(0, 0, obj_ct_fallobj)
    c.sprite_index = spr_fallleaf
    c.image_blend = choose(merge_color(c_red, c_white, 0.5))
    siner += 1
    draw_sprite_ext(spr_tobdog_spring, floor((siner / 15)), 250, 218, 1, 1, 0, c_white, 1)
    draw_set_color(c_gray)
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_test_spring"), 1, 1, -20)
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
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_test_summer"), 1, 1, -20)
}
if (weather == 4)
{
    c = instance_create(0, 0, obj_ct_fallobj)
    c.sprite_index = spr_fallleaf
    c.image_blend = choose(65535, 4235519, 255)
    siner += 1
    draw_sprite_ext(spr_tobdog_autumn, 0, 250, 218, 1, 1, 0, c_white, 1)
    draw_set_color(c_gray)
    draw_text_transformed((220 + sin((siner / 12))), (120 + cos((siner / 12))), scr_gettext("joyconfig_test_fall"), 1, 1, -20)
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
