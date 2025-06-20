zx = 545;
zy = 375;
xx = 595;
xy = 315;
cx = 645;
cy = 255;
f1x = 612;
f1y = 35;
analog_posx = 63;
analog_posy = 337.5;
button_scale = 0.75;
analog_scale = 0.75;
joystick_type = 1;
controls_opacity = 0.5;
analog_center_x = analog_posx;
analog_center_y = analog_posy;
arrowkeys_area_size = 19.675;
arrowkeys_back_area_size = 45;
settingsx = 30;
settingsy = 35;
show = 1;
edit = 0;
timer = 0;
text_fade = 0;
image_alpha = 1;
version = 2;

if (file_exists("touchconfig.ini"))
{
    ini_open("touchconfig.ini");
    
    if (ini_read_real("CONFIG", "version", 0) == version)
    {
        zx = ini_read_real("CONFIG", "zx", zx);
        zy = ini_read_real("CONFIG", "zy", zy);
        xx = ini_read_real("CONFIG", "xx", xx);
        xy = ini_read_real("CONFIG", "xy", xy);
        cx = ini_read_real("CONFIG", "cx", cx);
        cy = ini_read_real("CONFIG", "cy", cy);
        f1x = ini_read_real("CONFIG", "f1x", f1x);
        f1y = ini_read_real("CONFIG", "f1y", f1y);
        analog_posx = ini_read_real("CONFIG", "analog_posx", analog_posx);
        analog_posy = ini_read_real("CONFIG", "analog_posy", analog_posy);
        button_scale = ini_read_real("CONFIG", "button_scale", button_scale);
        analog_scale = ini_read_real("CONFIG", "analog_scale", analog_scale);
        joystick_type = ini_read_real("CONFIG", "joystick_type", joystick_type);
        controls_opacity = ini_read_real("CONFIG", "controls_opacity", controls_opacity);
        ini_close();
    }
    else
    {
        ini_close();
        file_delete("touchconfig.ini");
    }
}

virtual_key_zp = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_xp = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_cp = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_f1p = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_analogp = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_settings = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_z = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_x = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_c = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_f1 = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_up = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_right = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_left = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_down = virtual_key_add(0, 0, 0, 0, 0);
virtual_key_analog = virtual_key_add(0, 0, 0, 0, 0);
