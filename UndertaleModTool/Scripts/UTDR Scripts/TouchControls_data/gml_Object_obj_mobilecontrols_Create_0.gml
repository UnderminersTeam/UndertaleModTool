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
virtual_key_zp = virtual_key_add(0, 0, 0, 0, 1)
virtual_key_xp = virtual_key_add(0, 0, 0, 0, 2)
virtual_key_cp = virtual_key_add(0, 0, 0, 0, 3)
virtual_key_analogp = virtual_key_add(0, 0, 0, 0, 4)
virtual_key_settings = virtual_key_add(0, 0, 0, 0, 5)
virtual_key_z = virtual_key_add(0, 0, 0, 0, 6)
virtual_key_x = virtual_key_add(0, 0, 0, 0, 7)
virtual_key_c = virtual_key_add(0, 0, 0, 0, 8)
virtual_key_up = virtual_key_add(0, 0, 0, 0, 9)
virtual_key_right = virtual_key_add(0, 0, 0, 0, 10)
virtual_key_left = virtual_key_add(0, 0, 0, 0, 11)
virtual_key_down = virtual_key_add(0, 0, 0, 0, 12)
virtual_key_analog = virtual_key_add(0, 0, 0, 0, 13)
latestGuiH = display_get_gui_height()
latestGuiW = display_get_gui_width()