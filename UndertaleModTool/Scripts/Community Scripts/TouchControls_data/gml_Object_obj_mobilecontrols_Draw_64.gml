var ratio = display_get_gui_width() / 640
var ratioVertical = display_get_gui_height() / 480
draw_sprite_ext(spr_black, 0, 0, 0, (1 * ratio), (1 * ratioVertical), 0, c_white, black_fade)
draw_set_font(settings_font)
draw_sprite_ext(spr_controls_config, 0, (220 * ratio), (22.5 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_button_scale, 0, (120.5 * ratio), (75 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_arrow_leftright, 0, (459.5 * ratio), (75 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_analog_scale, 0, (120.5 * ratio), (121 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_arrow_leftright, 0, (459.5 * ratio), (121 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_analog_type, 0, (124 * ratio), (167 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_arrow_leftright, 0, (459.5 * ratio), (167 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_controls_opacity, 0, (106.5 * ratio), (213 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_arrow_leftright, 0, (459.5 * ratio), (213 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_sprite_ext(spr_reset_config, 0, (241 * ratio), (412.25 * ratioVertical), (2 * ratio), (2 * ratioVertical), 0, c_white, text_black_fade)
draw_text_transformed_colour((settings_num_x * ratio), (67 * ratioVertical), button_scale, (1 * ratio), (1 * ratioVertical), 0, c_white, c_white, c_white, c_white, text_black_fade)
draw_text_transformed_colour((settings_num_x * ratio), (113 * ratioVertical), analog_scale, (1 * ratio), (1 * ratioVertical), 0, c_white, c_white, c_white, c_white, text_black_fade)
draw_text_transformed_colour((settings_num_x * ratio), (159 * ratioVertical), joystick_type, (1 * ratio), (1 * ratioVertical), 0, c_white, c_white, c_white, c_white, text_black_fade)
draw_text_transformed_colour((settings_num_x * ratio), (205 * ratioVertical), controls_opacity, (1 * ratio), (1 * ratioVertical), 0, c_white, c_white, c_white, c_white, text_black_fade)
draw_sprite_ext(spr_z_button, keyboard_check(ord("Z")), (zx * ratio), (zy * ratioVertical), (button_scale * ratio), (button_scale * ratioVertical), 0, c_white, controls_opacity)
draw_sprite_ext(spr_x_button, keyboard_check(ord("X")), (xx * ratio), (xy * ratioVertical), (button_scale * ratio), (button_scale * ratioVertical), 0, c_white, controls_opacity)
draw_sprite_ext(spr_c_button, keyboard_check(ord("C")), (cx * ratio), (cy * ratioVertical), (button_scale * ratio), (button_scale * ratioVertical), 0, c_white, controls_opacity)
draw_sprite_ext(spr_joybase, joystick_type, (analog_posx * ratio), (analog_posy * ratioVertical), (analog_scale * ratio), (analog_scale * ratioVertical), 0, c_white, controls_opacity)
draw_sprite_ext(spr_joystick, joystick_type, (analog_center_x * ratio), (analog_center_y * ratioVertical), (analog_scale * ratio), (analog_scale * ratioVertical), 0, c_white, controls_opacity)
draw_sprite_ext(spr_settings, keyboard_check(ord("\")), (settingsx * ratio), (settingsy * ratioVertical), (button_scale * ratio), (button_scale * ratioVertical), 0, c_white, controls_opacity)
