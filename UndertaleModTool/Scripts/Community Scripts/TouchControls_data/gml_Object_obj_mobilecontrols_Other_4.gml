var coof = display_get_gui_width() / 640
var coofH = display_get_gui_height() / 480
virtual_key_zp = virtual_key_add((zx * coof), (zy * coofH), (27 * button_scale * coof), (29 * button_scale * coofH), 125)
virtual_key_xp = virtual_key_add((xx * coof), (xy * coofH), (27 * button_scale * coof), (29 * button_scale * coofH), 124)
virtual_key_cp = virtual_key_add((cx * coof), (cy * coofH), (27 * button_scale * coof), (29 * button_scale * coofH), 94)
virtual_key_analogp = virtual_key_add((analog_posx * coof), (analog_posy * coofH), (59 * analog_scale * coof), (59 * analog_scale * coofH), 93)
virtual_key_settings = virtual_key_add((settingsx * coof), (settingsy * coofH), (19 * button_scale * coof), (25 * button_scale * coofH), 92)
if (edit != 0)
    return;
virtual_key_z = virtual_key_add((zx * coof), (zy * coofH), (27 * button_scale * coof), (29 * button_scale * coofH), 90)
virtual_key_x = virtual_key_add((xx * coof), (xy * coofH), (27 * button_scale * coof), (29 * button_scale * coofH), 88)
virtual_key_c = virtual_key_add((cx * coof), (cy * coofH), (27 * button_scale * coof), (29 * button_scale * coofH), 67)
virtual_key_up = virtual_key_add((analog_posx - arrowkeys_back_area_size * analog_scale * coof), (analog_posy - arrowkeys_back_area_size * analog_scale * coofH), (arrowkeys_back_area_size * analog_scale * coof + (59 * analog_scale * coof + arrowkeys_back_area_size * analog_scale * coof)), (arrowkeys_area_size * analog_scale * coofH + arrowkeys_back_area_size * analog_scale * coofH), 38)
virtual_key_right = virtual_key_add((analog_posx + 59 * analog_scale * coof - arrowkeys_area_size * analog_scale * coof), (analog_posy - arrowkeys_back_area_size * analog_scale * coofH), (arrowkeys_area_size * analog_scale * coof + arrowkeys_back_area_size * analog_scale * coof), (arrowkeys_back_area_size * analog_scale * coofH + 59 * analog_scale * coofH + arrowkeys_back_area_size * analog_scale * coofH), 39)
virtual_key_left = virtual_key_add((analog_posx - arrowkeys_back_area_size * analog_scale * coof), (analog_posy - arrowkeys_back_area_size * analog_scale * coofH), (arrowkeys_area_size * analog_scale * coof + arrowkeys_back_area_size * analog_scale * coof), (arrowkeys_back_area_size * analog_scale * coofH + (59 * analog_scale * coofH + arrowkeys_back_area_size * analog_scale * coofH)), 37)
virtual_key_down = virtual_key_add((analog_posx - arrowkeys_back_area_size * analog_scale * coof), (analog_posy + 59 * analog_scale * coofH - arrowkeys_area_size * analog_scale * coofH), (arrowkeys_back_area_size * analog_scale * coof + 59 * analog_scale * coof + arrowkeys_back_area_size * analog_scale * coof), (arrowkeys_area_size * analog_scale * coofH + arrowkeys_back_area_size * analog_scale * coofH), 40)
virtual_key_analog = virtual_key_add((analog_posx - arrowkeys_back_area_size * analog_scale * coof), (analog_posy - arrowkeys_back_area_size * analog_scale * coofH), ((59 + arrowkeys_back_area_size) * analog_scale * coof + arrowkeys_back_area_size * analog_scale * coof), ((59 + arrowkeys_back_area_size) * analog_scale * coofH + arrowkeys_back_area_size * analog_scale * coofH), 126)
