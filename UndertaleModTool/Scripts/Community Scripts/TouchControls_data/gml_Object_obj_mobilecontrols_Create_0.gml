if (!((os_type == os_android || os_type == os_ios)))
    instance_destroy()
settings_font = {_font}
settings_num_x = {_settingsnumx}
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
    zx = ini_read_real("CONFIG", "zx", zx)
    zy = ini_read_real("CONFIG", "zy", zy)
    xx = ini_read_real("CONFIG", "xx", xx)
    xy = ini_read_real("CONFIG", "xy", xy)
    cx = ini_read_real("CONFIG", "cx", cx)
    cy = ini_read_real("CONFIG", "cy", cy)
    analog_posx = ini_read_real("CONFIG", "analog_posx", analog_posx)
    analog_posy = ini_read_real("CONFIG", "analog_posy", analog_posy)
    button_scale = ini_read_real("CONFIG", "button_scale", button_scale)
    analog_scale = ini_read_real("CONFIG", "analog_scale", analog_scale)
    joystick_type = ini_read_real("CONFIG", "joystick_type", joystick_type)
    controls_opacity = ini_read_real("CONFIG", "controls_opacity", controls_opacity)
    ini_close()
}

