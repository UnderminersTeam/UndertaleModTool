var _button_scale = lerp(1, 3, button_scale);
var _analog_scale = lerp(1, 4, analog_scale);
var _analog_posx = analog_posx - (30 * _analog_scale);
var _analog_posy = analog_posy - (30 * _analog_scale);
virtual_key_delete(virtual_key_up);
virtual_key_delete(virtual_key_down);
virtual_key_delete(virtual_key_left);
virtual_key_delete(virtual_key_right);
virtual_key_delete(virtual_key_z);
virtual_key_delete(virtual_key_x);
virtual_key_delete(virtual_key_c);
virtual_key_delete(virtual_key_f1);
virtual_key_delete(virtual_key_zp);
virtual_key_delete(virtual_key_xp);
virtual_key_delete(virtual_key_cp);
virtual_key_delete(virtual_key_f1p);
virtual_key_delete(virtual_key_analog);
virtual_key_delete(virtual_key_analogp);
virtual_key_zp = virtual_key_add(zx - (14 * _button_scale), zy - (14 * _button_scale), 27 * _button_scale, 29 * _button_scale, 49);
virtual_key_xp = virtual_key_add(xx - (14 * _button_scale), xy - (14 * _button_scale), 27 * _button_scale, 29 * _button_scale, 50);
virtual_key_cp = virtual_key_add(cx - (14 * _button_scale), cy - (14 * _button_scale), 27 * _button_scale, 29 * _button_scale, 51);
virtual_key_f1p = virtual_key_add(f1x - (10 * _button_scale), f1y - (12 * _button_scale), 19 * _button_scale, 25 * _button_scale, 52);
virtual_key_analogp = virtual_key_add(_analog_posx, _analog_posy, 59 * _analog_scale, 59 * _analog_scale, 48);

if (edit == 0)
    virtual_key_settings = virtual_key_add(settingsx - (10 * _button_scale), settingsy - (12 * _button_scale), 19 * _button_scale, 25 * _button_scale, 125);

if (edit == 1 || show == 0)
    exit;

virtual_key_z = virtual_key_add(zx - (14 * _button_scale), zy - (14 * _button_scale), 27 * _button_scale, 29 * _button_scale, 90);
virtual_key_x = virtual_key_add(xx - (14 * _button_scale), xy - (14 * _button_scale), 27 * _button_scale, 29 * _button_scale, 88);
virtual_key_c = virtual_key_add(cx - (14 * _button_scale), cy - (14 * _button_scale), 27 * _button_scale, 29 * _button_scale, 67);
virtual_key_f1 = virtual_key_add(f1x - (10 * _button_scale), f1y - (12 * _button_scale), 19 * _button_scale, 25 * _button_scale, 112);
virtual_key_up = virtual_key_add(_analog_posx - (arrowkeys_back_area_size * _analog_scale), _analog_posy - (arrowkeys_back_area_size * _analog_scale), (arrowkeys_back_area_size * _analog_scale) + ((59 * _analog_scale) + (arrowkeys_back_area_size * _analog_scale)), (arrowkeys_area_size * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), 38);
virtual_key_right = virtual_key_add((_analog_posx + (59 * _analog_scale)) - (arrowkeys_area_size * _analog_scale), _analog_posy - (arrowkeys_back_area_size * _analog_scale), (arrowkeys_area_size * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), (arrowkeys_back_area_size * _analog_scale) + (59 * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), 39);
virtual_key_left = virtual_key_add(_analog_posx - (arrowkeys_back_area_size * _analog_scale), _analog_posy - (arrowkeys_back_area_size * _analog_scale), (arrowkeys_area_size * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), (arrowkeys_back_area_size * _analog_scale) + ((59 * _analog_scale) + (arrowkeys_back_area_size * _analog_scale)), 37);
virtual_key_down = virtual_key_add(_analog_posx - (arrowkeys_back_area_size * _analog_scale), (_analog_posy + (59 * _analog_scale)) - (arrowkeys_area_size * _analog_scale), (arrowkeys_back_area_size * _analog_scale) + (59 * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), (arrowkeys_area_size * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), 40);
virtual_key_analog = virtual_key_add(_analog_posx - (arrowkeys_back_area_size * _analog_scale), _analog_posy - (arrowkeys_back_area_size * _analog_scale), ((59 + arrowkeys_back_area_size) * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), ((59 + arrowkeys_back_area_size) * _analog_scale) + (arrowkeys_back_area_size * _analog_scale), 124);
