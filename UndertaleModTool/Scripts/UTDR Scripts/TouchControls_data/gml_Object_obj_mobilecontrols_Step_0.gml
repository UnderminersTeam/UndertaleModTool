var _analog_scale = lerp(1, 4, analog_scale);

if (keyboard_check(124))
{
    analog_center_x = clamp(device_mouse_x_to_gui(0), analog_posx - (30 * _analog_scale), analog_posx + (30 * _analog_scale));
    analog_center_y = clamp(device_mouse_y_to_gui(0), analog_posy - (30 * _analog_scale), analog_posy + (30 * _analog_scale));
}
else
{
    analog_center_x = analog_posx;
    analog_center_y = analog_posy;
}

if (keyboard_check(125))
    timer = min(timer + 1, 30);

if (keyboard_check_released(125))
{
    if (edit == 0)
    {
        if (timer < 30)
        {
            show ^= 1;
            scr_add_keys();
        }
        else
        {
            show = 1;
            edit = 1;
        }
    }
    else if (timer < 30)
    {
        ini_open("touchconfig.ini");
        ini_write_real("CONFIG", "version", version);
        ini_write_real("CONFIG", "zx", zx);
        ini_write_real("CONFIG", "zy", zy);
        ini_write_real("CONFIG", "xx", xx);
        ini_write_real("CONFIG", "xy", xy);
        ini_write_real("CONFIG", "cx", cx);
        ini_write_real("CONFIG", "cy", cy);
        ini_write_real("CONFIG", "f1x", f1x);
        ini_write_real("CONFIG", "f1y", f1y);
        ini_write_real("CONFIG", "analog_posx", analog_posx);
        ini_write_real("CONFIG", "analog_posy", analog_posy);
        ini_write_real("CONFIG", "button_scale", button_scale);
        ini_write_real("CONFIG", "analog_scale", analog_scale);
        ini_write_real("CONFIG", "joystick_type", joystick_type);
        ini_write_real("CONFIG", "controls_opacity", controls_opacity);
        ini_close();
        edit = 2;
        scr_add_keys();
        edit = 0;
    }
    else
    {
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
    }
    
    timer = 0;
}

image_alpha = (show == 1) ? min(image_alpha + 0.1, 1) : max(image_alpha - 0.1, 0);
text_fade = (edit == 0) ? max(text_fade - 0.1, 0) : min(text_fade + 0.1, 1);

if (edit == 0)
    exit;

scr_add_keys();

if (keyboard_check(ord("1")))
{
    zx = device_mouse_x_to_gui(0);
    zy = device_mouse_y_to_gui(0);
    exit;
}

if (keyboard_check(ord("2")))
{
    xx = device_mouse_x_to_gui(0);
    xy = device_mouse_y_to_gui(0);
    exit;
}

if (keyboard_check(ord("3")))
{
    cx = device_mouse_x_to_gui(0);
    cy = device_mouse_y_to_gui(0);
    exit;
}

if (keyboard_check(ord("4")))
{
    f1x = device_mouse_x_to_gui(0);
    f1y = device_mouse_y_to_gui(0);
    exit;
}

if (keyboard_check(ord("0")))
{
    analog_posx = device_mouse_x_to_gui(0);
    analog_posy = device_mouse_y_to_gui(0);
    analog_center_x = analog_posx;
    analog_center_y = analog_posy;
    exit;
}

if (device_mouse_x_to_gui(0) >= 459.5 && device_mouse_y_to_gui(0) >= 75 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 93 && mouse_check_button_pressed(mb_left))
    button_scale -= 0.125;

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 75 && device_mouse_x_to_gui(0) <= 541.5 && device_mouse_y_to_gui(0) <= 93 && mouse_check_button_pressed(mb_left))
    button_scale += 0.125;

if (device_mouse_x_to_gui(0) >= 459.5 && device_mouse_y_to_gui(0) >= 121 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 139 && mouse_check_button_pressed(mb_left))
    analog_scale -= 0.125;

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 121 && device_mouse_x_to_gui(0) <= 541.5 && device_mouse_y_to_gui(0) <= 139 && mouse_check_button_pressed(mb_left))
    analog_scale += 0.125;

if (device_mouse_x_to_gui(0) >= 459.5 && device_mouse_y_to_gui(0) >= 167 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 185 && mouse_check_button_pressed(mb_left))
    joystick_type ^= 1;

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 167 && device_mouse_x_to_gui(0) <= 541.5 && device_mouse_y_to_gui(0) <= 185 && mouse_check_button_pressed(mb_left))
    joystick_type ^= 1;

if (device_mouse_x_to_gui(0) >= 459.5 && device_mouse_y_to_gui(0) >= 213 && device_mouse_x_to_gui(0) <= 469.5 && device_mouse_y_to_gui(0) <= 231 && mouse_check_button_pressed(mb_left))
    controls_opacity -= 0.125;

if (device_mouse_x_to_gui(0) >= 531.5 && device_mouse_y_to_gui(0) >= 213 && device_mouse_x_to_gui(0) <= 541.5 && device_mouse_y_to_gui(0) <= 231 && mouse_check_button_pressed(mb_left))
    controls_opacity += 0.125;

button_scale = clamp(button_scale, 0, 1);
analog_scale = clamp(analog_scale, 0, 1);
controls_opacity = clamp(controls_opacity, 0, 1);
