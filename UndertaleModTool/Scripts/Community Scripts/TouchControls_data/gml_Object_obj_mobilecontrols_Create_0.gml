persistent = true
control_fixture = physics_fixture_create()
physics_fixture_set_awake(control_fixture, 1)
if (string_lower(game_display_name) == "undertale")
{
    settings_font = fnt_main
    settings_num_x = 477
}
else
{
    settings_font = fnt_mainbig
    settings_num_x = 502
}
zx = 510
zy = 340
xx = 560
xy = 280
cx = 610
cy = 220
button_scale = 2.5
analog_scale = 3.3
analog_posx = -42
analog_posy = 232.5
analog_edit_selected = 0
analog_center_x = (analog_posx + (((59 * analog_scale) / 2) - ((41 * analog_scale) / 2)))
analog_center_y = (analog_posy + (((59 * analog_scale) / 2) - ((41 * analog_scale) / 2)))
arrowkeys_area_size = 19.675
arrowkeys_back_area_size = 45
joystick_type = 0
settingsx = 5
settingsy = 5
edit = 0
black_fade = 0
text_black_fade = 0
controls_opacity = 0.5
if file_exists("touchconfig.ini")
{
    ini_open("touchconfig.ini")
    zx = ini_read_real("CONFIG", "zx", 0)
    zy = ini_read_real("CONFIG", "zy", 0)
    xx = ini_read_real("CONFIG", "xx", 0)
    xy = ini_read_real("CONFIG", "xy", 0)
    cx = ini_read_real("CONFIG", "cx", 0)
    cy = ini_read_real("CONFIG", "cy", 0)
    analog_posx = ini_read_real("CONFIG", "analog_posx", 0)
    analog_posy = ini_read_real("CONFIG", "analog_posy", 0)
    button_scale = ini_read_real("CONFIG", "button_scale", 0)
    analog_scale = ini_read_real("CONFIG", "analog_scale", 0)
    joystick_type = ini_read_real("CONFIG", "joystick_type", 0)
    controls_opacity = ini_read_real("CONFIG", "controls_opacity", 0)
    ini_close()
}

