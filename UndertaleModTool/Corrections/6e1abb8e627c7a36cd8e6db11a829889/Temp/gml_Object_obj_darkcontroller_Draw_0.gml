var ch_y;
xx = __view_get(0, 0)
yy = __view_get(1, 0)
tpoff = ((tp - tpy) + yy)
bpoff = (((-bp) + bpy) + yy)
if (drawchar == 1)
{
    draw_set_color(c_black)
    draw_rectangle((xx - 10), ((yy + 480) - bp), (xx + 650), (yy + 500), 0)
    draw_set_color(c_black)
    draw_rectangle((xx - 10), (yy + tp), (xx + 650), (yy - 10), 0)
    scr_charbox()
    if (global.menuno == 0)
        deschaver = 0
    if (deschaver == 0)
    {
        draw_sprite_ext(scr_84_get_sprite("spr_darkmenudesc"), global.menucoord[0], (xx + 20), ((yy + tp) - 56), 2, 2, 0, c_white, 1)
        msprite[0] = spr_darkitembt
        msprite[1] = spr_darkequipbt
        msprite[2] = spr_darktalkbt
        msprite[3] = spr_darktechbt
        msprite[4] = spr_darkconfigbt
        for (var i = 0; i < 5; i += 1)
        {
            off = 1
            if (global.menucoord[0] == i)
                off = 0
            if ((global.menuno - 1) == i)
                off = 2
            spritemx = 0
            if (i >= 2)
                spritemx = -100
            if (i != 2)
                draw_sprite_ext(msprite[i], off, (((xx + 120) + (i * 100)) + spritemx), ((yy + tp) - 60), 2, 2, 0, c_white, 1)
        }
        draw_set_color(c_white)
        scr_84_set_draw_font("mainbig")
        draw_text((xx + 520), ((yy + tp) - 60), string_hash_to_newline((scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_47_0") + string(global.gold))))
    }
}
if (global.menuno == 5)
{
    draw_set_color(c_black)
    if (global.lang == "ja")
    {
        draw_rectangle((xx + 60), (yy + 85), (xx + 580), (yy + 412), 0)
        scr_darkbox((xx + 50), (yy + 75), (xx + 590), (yy + 422))
    }
    else
    {
        draw_rectangle((xx + 60), (yy + 90), (xx + 580), (yy + 410), 0)
        scr_darkbox((xx + 50), (yy + 80), (xx + 590), (yy + 420))
    }
    if ((global.submenu >= 30 && global.submenu <= 33) || global.submenu == 36)
    {
        var _xPos = (global.lang == "en" ? (xx + 170) : (xx + 150))
        var _heartXPos = (global.lang == "en" ? (xx + 145) : (xx + 125))
        var _selectXPos = (global.lang == "en" ? (xx + 430) : (xx + 385))
        draw_set_color(c_white)
        draw_text((xx + 270), (yy + 100), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_74_0")))
        audvol = (string(round((global.flag[17] * 100))) + "%")
        musvol = (string(abs((global.flag[16] * 100))) + "%")
        runoff = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_79_0")
        if (global.flag[11] == 1)
            runoff = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_79_1")
        flashoff = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_80_0")
        if (global.flag[8] == 1)
            flashoff = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_80_1")
        shakeoff = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_81_0")
        if (global.flag[12] == 1)
            shakeoff = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_81_1")
        draw_sprite(spr_heart, 0, _heartXPos, ((yy + 160) + (global.submenucoord[30] * 35)))
        if (global.submenu == 33)
            draw_set_color(c_yellow)
        draw_text(_xPos, (yy + 150), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_86_0")))
        draw_text(_selectXPos, (yy + 150), string_hash_to_newline(audvol))
        draw_set_color(c_white)
        draw_text(_xPos, (yy + 185), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_91_0")))
        draw_text(_xPos, (yy + 220), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_92_0")))
        draw_text(_selectXPos, (yy + 220), string_hash_to_newline(flashoff))
        draw_text(_xPos, (yy + 255), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_94_0")))
        draw_text(_selectXPos, (yy + 255), string_hash_to_newline(runoff))
        if (global.submenu == 36)
            draw_set_color(c_yellow)
        else if global.disable_border
            draw_set_color(c_gray)
        draw_text(_xPos, (yy + 290), border_text)
        draw_text(_selectXPos, (yy + 290), border_options[selected_border])
        draw_set_color(c_white)
        draw_text(_xPos, (yy + 325), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_96_0")))
    }
    if (global.submenu == 34)
    {
        draw_set_color(c_white)
        draw_text((xx + 200), (yy + 150), string_hash_to_newline(scr_84_get_subst_string(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_101_0"), global.asc_def[global.input_k[5]])))
    }
    if (global.submenu == 35)
    {
        var _yOffset = (global.lang == "en" ? 0 : -4)
        draw_set_color(c_white)
        draw_text((xx + 105), ((yy + 100) + _yOffset), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_113_0")))
        if obj_gamecontroller.gamepad_active
            draw_text((xx + 435), ((yy + 100) + _yOffset), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_115_0")))
        function[0] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_117_0")
        function[1] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_118_0")
        function[2] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_119_0")
        function[3] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_120_0")
        function[4] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_121_0")
        function[5] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_122_0")
        function[6] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_123_0")
        function[7] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_124_0")
        function[8] = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_125_0")
        draw_sprite(spr_heart, 0, (xx + 80), (((yy + 150) + (global.submenucoord[35] * 28)) + _yOffset))
        var line_padding = (global.lang == "ja" ? 1 : 0)
        for (i = 0; i <= 8; i += 1)
        {
            draw_set_color(c_white)
            if (global.submenucoord[35] == i)
                draw_set_color(c_aqua)
            if (global.submenucoord[35] == i && control_select_con == 1)
                draw_set_color(c_red)
            if (i == 7 && control_flash_timer > 0)
                draw_set_color(merge_color(c_aqua, c_yellow, ((control_flash_timer / 10) - 0.1)))
            draw_text((xx + 105), (((yy + 140) + (i * (28 + line_padding))) + _yOffset), string_hash_to_newline(function[i]))
            if (i < 7)
            {
                if obj_gamecontroller.gamepad_active
                {
                    var _sprite = scr_getbuttonsprite(global.input_g[i], 0)
                    _xPos = (global.lang == "en" ? (xx + 475) : (xx + 465))
                    draw_sprite_ext(_sprite, 0, _xPos, (((yy + 140) + (i * (28 + line_padding))) + _yOffset), 2, 2, 0, c_white, 1)
                }
            }
        }
    }
}
if (global.menuno == 4)
{
    draw_set_color(c_black)
    if (global.lang == "ja")
    {
        draw_rectangle((xx + 60), (yy + 90), (xx + 612), (yy + 410), 0)
        scr_darkbox((xx + 50), (yy + 80), (xx + 622), (yy + 420))
    }
    else
    {
        draw_rectangle((xx + 60), (yy + 90), (xx + 580), (yy + 410), 0)
        scr_darkbox((xx + 50), (yy + 80), (xx + 590), (yy + 420))
    }
    draw_set_color(c_white)
    draw_rectangle((xx + 60), (yy + 216), ((xx + 60) + 520), ((yy + 216) + 5), 0)
    draw_rectangle((xx + 294), (yy + 220), ((xx + 294) + 5), ((yy + 220) + 190), 0)
    if (global.lang == "ja")
        draw_rectangle((xx + 60), (yy + 216), ((xx + 60) + 552), ((yy + 216) + 5), 0)
    draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 0, (xx + 124), (yy + 84), 2, 2, 0, c_white, 1)
    draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 4, (xx + 124), (yy + 210), 2, 2, 0, c_white, 1)
    draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 5, (xx + 380), (yy + 210), 2, 2, 0, c_white, 1)
    draw_sprite_ext(spr_dmenu_captions, 6, (xx + 340), (yy + 225), 1, 1, 0, c_white, 1)
    coord = global.submenucoord[20]
    charcoord = global.char[coord]
    draw_set_color(c_white)
    scr_84_set_draw_font("mainbig")
    draw_text((xx + 130), (yy + 105), string_hash_to_newline(global.charname[charcoord]))
    for (i = 0; i < 3; i += 1)
    {
        chosen = 0.4
        if (i == coord)
            chosen = 1
        draw_sprite_ext(spr_equipchar, global.char[i], ((90 + xx) + (i * 50)), (160 + yy), 2, 2, 0, c_white, chosen)
    }
    if (global.submenu == 20)
        draw_sprite(spr_heart_harrows, (menusiner / 20), ((100 + xx) + (coord * 50)), (141 + yy))
    var ch_yoff = (yy + 230)
    var ch_vspace = 25
    if (global.lang == "ja")
        ch_vspace = 28
    for (var ch_i = 0; ch_i < 6; ch_i++)
        ch_y[ch_i] = (ch_yoff + (ch_i * ch_vspace))
    draw_set_color(c_white)
    draw_text((xx + 100), ch_y[0], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_207_0")))
    draw_item_icon((xx + 74), (ch_y[0] + 6), 1)
    draw_text((xx + 100), ch_y[1], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_208_0")))
    draw_item_icon((xx + 74), (ch_y[1] + 6), 4)
    draw_text((xx + 100), ch_y[2], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_209_0")))
    draw_item_icon((xx + 74), (ch_y[2] + 6), 5)
    draw_text((xx + 100), ch_y[5], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_212_0")))
    draw_item_icon((xx + 74), (ch_y[5] + 6), 9)
    char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_214_0")
    guts_amount = 0
    fluff_amount = 0
    if (charcoord == 1)
    {
        guts_amount = 1
        draw_set_color(c_dkgray)
        draw_text((xx + 100), ch_y[4], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_226_0")))
        if (dograndom >= 99)
        {
            draw_set_color(c_white)
            draw_text((xx + 100), ch_y[3], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_231_0")))
            draw_sprite_ext(spr_dog_sleep, ((-threebuffer) / 30), (xx + 220), (ch_y[3] + 5), 2, 2, 0, c_white, 1)
        }
        else
            draw_text((xx + 100), ch_y[3], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_236_0")))
        draw_set_color(c_white)
        char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_239_0")
        if (global.plot >= 30)
            char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_240_0")
        if (global.flag[252] == 1)
            char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_241_0")
    }
    if (charcoord == 2)
    {
        guts_amount = 2
        rude_amount = 100
        if (global.plot >= 154)
            rude_amount = 99
        crude_amount = 100
        if (global.flag[214] == 1)
            crude_amount = 101
        draw_text((xx + 100), ch_y[3], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_251_0")))
        draw_item_icon((xx + 74), (ch_y[3] + 6), 13)
        if (global.lang == "ja")
        {
            draw_text_transformed((xx + 100), ch_y[4], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_252_0")), 1, 1, 0)
            draw_item_icon((xx + 74), (ch_y[4] + 6), 13)
        }
        else
        {
            draw_text_transformed((xx + 100), ch_y[4], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_252_0")), 0.8, 1, 0)
            draw_item_icon((xx + 74), (ch_y[4] + 6), 13)
        }
        draw_text((xx + 230), ch_y[3], string_hash_to_newline(rude_amount))
        draw_text((xx + 230), ch_y[4], string_hash_to_newline(crude_amount))
        char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_255_0")
        if (global.plot >= 154)
            char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_256_0")
    }
    if (charcoord == 3)
    {
        guts_amount = 0
        fluff_amount = 1
        kindness_amount = 100
        kind_icon = 10
        kind_text = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_265_0")
        if (dograndom >= 99)
        {
            kind_icon = 11
            kind_text = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_269_0")
            kindness_amount = 1
        }
        char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_272_0")
        if (global.charweapon[3] == 9)
            char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_275_0")
        if (global.charweapon[3] == 10)
        {
            char_desc = scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_279_0")
            fluff_amount += 1
        }
        draw_text((xx + 100), ch_y[3], string_hash_to_newline(kind_text))
        draw_item_icon((xx + 74), (ch_y[3] + 6), kind_icon)
        draw_text((xx + 230), ch_y[3], string_hash_to_newline(kindness_amount))
        fluff_xscale = 0.8
        if (global.lang == "ja")
            fluff_xscale = 1
        draw_text_transformed((xx + 100), ch_y[4], string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_286_0")), fluff_xscale, 1, 0)
        draw_item_icon((xx + 74), (ch_y[4] + 6), 12)
        for (i = 0; i < fluff_amount; i += 1)
            draw_item_icon(((xx + 230) + (i * 20)), (ch_y[4] + 6), 12)
    }
    draw_text((xx + 320), (yy + 105), string_hash_to_newline(char_desc))
    guts_xoff = 0
    if (global.lang == "ja")
        guts_xoff = 16
    for (i = 0; i < guts_amount; i += 1)
        draw_item_icon((((xx + 190) + (i * 20)) + guts_xoff), (ch_y[5] + 6), 9)
    atsum = (((global.at[global.char[coord]] + global.itemat[global.char[coord], 0]) + global.itemat[global.char[coord], 1]) + global.itemat[global.char[coord], 2])
    dfsum = (((global.df[global.char[coord]] + global.itemdf[global.char[coord], 0]) + global.itemdf[global.char[coord], 1]) + global.itemdf[global.char[coord], 2])
    magsum = (((global.mag[global.char[coord]] + global.itemmag[global.char[coord], 0]) + global.itemmag[global.char[coord], 1]) + global.itemmag[global.char[coord], 2])
    grazesum = global.grazeamt
    sizesum = global.grazesize
    for (i = 0; i < 3; i += 1)
    {
        for (j = 0; j < 3; j += 1)
        {
            if (global.char[i] != 0)
            {
                grazesum += global.itemgrazeamt[global.char[i], j]
                sizesum += global.itemgrazesize[global.char[i], j]
            }
        }
    }
    draw_text((xx + 230), ch_y[0], string_hash_to_newline(atsum))
    draw_text((xx + 230), ch_y[1], string_hash_to_newline(dfsum))
    draw_text((xx + 230), ch_y[2], string_hash_to_newline(magsum))
    spell_xoff = 0
    if (global.lang == "ja")
        spell_xoff = -10
    for (i = 0; i < 6; i += 1)
    {
        if (global.spell[charcoord, i] > 0)
        {
            g = 0
            if (global.spellusable[charcoord, i] == false)
                g = 1
            if (global.spellcost[charcoord, i] > global.tension)
                g = 1
            if (g == 0)
                draw_set_color(c_white)
            if (g == 1)
                draw_set_color(c_gray)
            draw_text((xx + 340), ch_y[i], string_hash_to_newline((string(round(((global.spellcost[charcoord, i] / global.maxtension) * 100))) + "%")))
            draw_text(((xx + 410) + spell_xoff), ch_y[i], string_hash_to_newline(global.spellname[charcoord, i]))
        }
    }
    if (global.submenu == 21)
        draw_sprite(spr_heart, 0, (xx + 320), ((yy + 240) + (global.submenucoord[21] * ch_vspace)))
    if (deschaver == 1)
    {
        draw_set_color(c_white)
        draw_text((xx + 20), (yy + 10), string_hash_to_newline(global.spelldesc[charcoord, global.submenucoord[21]]))
    }
}
if (global.menuno == 2)
{
    draw_set_color(c_black)
    ch_vspace = 27
    if (global.lang == "ja")
    {
        ch_vspace = 28
        draw_rectangle((xx + 60), (yy + 90), (xx + 626), (yy + 410), 0)
        scr_darkbox((xx + 50), (yy + 80), (xx + 636), (yy + 420))
    }
    else
    {
        draw_rectangle((xx + 60), (yy + 90), (xx + 580), (yy + 410), 0)
        scr_darkbox((xx + 50), (yy + 80), (xx + 590), (yy + 420))
    }
    draw_set_color(c_white)
    draw_rectangle((xx + 270), (yy + 91), (xx + 275), ((yy + 91) + 135), 0)
    draw_rectangle((xx + 59), (yy + 221), ((xx + 59) + 523), ((yy + 221) + 5), 0)
    if (global.lang == "ja")
        draw_rectangle((xx + 59), (yy + 221), ((xx + 63) + 565), ((yy + 221) + 5), 0)
    draw_rectangle((xx + 318), (yy + 221), ((xx + 318) + 5), ((yy + 221) + 190), 0)
    draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 0, (xx + 118), (yy + 86), 2, 2, 0, c_white, 1)
    draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 1, (xx + 376), (yy + 86), 2, 2, 0, c_white, 1)
    if (global.submenucoord[11] == 1 || global.submenucoord[11] == 2)
        draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 2, (xx + 372), (yy + 216), 2, 2, 0, c_white, 1)
    else
        draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 3, (xx + 372), (yy + 216), 2, 2, 0, c_white, 1)
    draw_sprite_ext(scr_84_get_sprite("spr_dmenu_captions"), 4, (xx + 116), (yy + 216), 2, 2, 0, c_white, 1)
    coord = global.submenucoord[10]
    charcoord = global.char[coord]
    menusiner += 1
    draw_set_color(c_white)
    scr_84_set_draw_font("mainbig")
    charname_xoff = 0
    if (global.lang == "ja")
        charname_xoff = -25
    draw_text(((xx + 135) + charname_xoff), (yy + 107), string_hash_to_newline(global.charname[charcoord]))
    for (i = 0; i < 3; i += 1)
    {
        chosen = 0.4
        if (i == coord)
            chosen = 1
        draw_sprite_ext(spr_equipchar, global.char[i], ((90 + xx) + (i * 50)), (160 + yy), 2, 2, 0, c_white, chosen)
    }
    if (global.submenu == 10)
        draw_sprite(spr_heart_harrows, (menusiner / 20), ((100 + xx) + (coord * 50)), (142 + yy))
    if (global.submenu != 11)
    {
        draw_sprite_ext(spr_dmenu_equip, (charcoord - 1), (xx + 302), (yy + 108), 2, 2, 0, c_white, 1)
        draw_sprite_ext(spr_dmenu_equip, 3, (xx + 302), (yy + 142), 2, 2, 0, c_white, 1)
        draw_sprite_ext(spr_dmenu_equip, 4, (xx + 302), (yy + 172), 2, 2, 0, c_white, 1)
    }
    if (global.submenu == 11)
    {
        if (global.submenucoord[11] == 0)
            draw_sprite(spr_heart, 0, (xx + 308), (yy + 122))
        else
            draw_sprite_ext(spr_dmenu_equip, (charcoord - 1), (xx + 302), (yy + 108), 2, 2, 0, c_white, 1)
        if (global.submenucoord[11] == 1)
            draw_sprite(spr_heart, 0, (xx + 308), (yy + 152))
        else
            draw_sprite_ext(spr_dmenu_equip, 3, (xx + 302), (yy + 142), 2, 2, 0, c_white, 1)
        if (global.submenucoord[11] == 2)
            draw_sprite(spr_heart, 0, (xx + 308), (yy + 182))
        else
            draw_sprite_ext(spr_dmenu_equip, 4, (xx + 302), (yy + 172), 2, 2, 0, c_white, 1)
    }
    if ((!charweaponname[charcoord]) == " ")
    {
        draw_text((xx + 365), (yy + 112), string_hash_to_newline(charweaponname[charcoord]))
        draw_item_icon((xx + 343), (yy + 118), charweaponicon[charcoord])
    }
    else
    {
        draw_set_color(c_dkgray)
        draw_text((xx + 365), (yy + 112), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_423_0")))
        draw_set_color(c_white)
    }
    if (global.chararmor1[charcoord] != 0)
    {
        draw_text((xx + 365), (yy + 142), string_hash_to_newline(chararmor1name[charcoord]))
        draw_item_icon((xx + 343), (yy + 148), chararmor1icon[charcoord])
    }
    else
    {
        draw_set_color(c_dkgray)
        draw_text((xx + 365), (yy + 142), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_435_0")))
        draw_set_color(c_white)
    }
    if (global.chararmor2[charcoord] != 0)
    {
        draw_text((xx + 365), (yy + 172), string_hash_to_newline(chararmor2name[charcoord]))
        draw_item_icon((xx + 343), (yy + 178), chararmor2icon[charcoord])
    }
    else
    {
        draw_set_color(c_dkgray)
        draw_text((xx + 365), (yy + 172), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_447_0")))
        draw_set_color(c_white)
    }
    eq_xoff = 0
    if (global.lang == "ja")
        eq_xoff = -6
    if (global.submenucoord[11] == 0)
    {
        j = 0
        for (i = pagemax[0]; i < (6 + pagemax[0]); i += 1)
        {
            g = 0
            if (charcoord == 1 && weaponchar1[i] == 0)
                g = 1
            if (charcoord == 2 && weaponchar2[i] == 0)
                g = 1
            if (charcoord == 3 && weaponchar3[i] == 0)
                g = 1
            if (g == 0)
                draw_set_color(c_white)
            if (g == 1)
                draw_set_color(c_gray)
            draw_item_icon(((xx + 364) + eq_xoff), ((yy + 236) + (j * ch_vspace)), weaponicon[i])
            if (global.weapon[i] != 0)
                draw_text(((xx + 384) + eq_xoff), ((yy + 230) + (j * ch_vspace)), string_hash_to_newline(weaponname[i]))
            else
            {
                draw_set_color(c_dkgray)
                draw_text(((xx + 384) + eq_xoff), ((yy + 230) + (j * ch_vspace)), string_hash_to_newline("---------"))
            }
            j += 1
        }
    }
    if (global.submenucoord[11] == 1 || global.submenucoord[11] == 2)
    {
        j = 0
        for (i = pagemax[1]; i < (6 + pagemax[1]); i += 1)
        {
            g = 0
            if (charcoord == 1 && armorchar1[i] == 0)
                g = 1
            if (charcoord == 2 && armorchar2[i] == 0)
                g = 1
            if (charcoord == 3 && armorchar3[i] == 0)
                g = 1
            if (g == 0)
                draw_set_color(c_white)
            if (g == 1)
                draw_set_color(c_gray)
            draw_item_icon(((xx + 364) + eq_xoff), ((yy + 236) + (j * ch_vspace)), armoricon[i])
            if (global.armor[i] != 0)
                draw_text(((xx + 384) + eq_xoff), ((yy + 230) + (j * ch_vspace)), string_hash_to_newline(armorname[i]))
            else
            {
                draw_set_color(c_dkgray)
                draw_text(((xx + 384) + eq_xoff), ((yy + 230) + (j * ch_vspace)), string_hash_to_newline("---------"))
            }
            j += 1
        }
    }
    if (global.submenu == 12 || global.submenu == 13 || global.submenu == 14)
    {
        scroll_xoff = 0
        if (global.lang == "ja")
            scroll_xoff = 54
        if (global.submenu == 12)
            pm = 0
        else
            pm = 1
        draw_sprite(spr_heart, 0, ((xx + 344) + eq_xoff), ((yy + 240) + ((global.submenucoord[global.submenu] - pagemax[pm]) * ch_vspace)))
        draw_set_color(c_white)
        for (i = 0; i < 12; i += 1)
        {
            buff = 0
            if (global.submenucoord[global.submenu] == i)
                buff += 3
            if (pm == 0)
            {
                if (global.weapon[i] == 0)
                    buff -= 1
            }
            if (pm == 1)
            {
                if (global.armor[i] == 0)
                    buff -= 1
            }
            draw_rectangle((((xx + 555) - buff) + scroll_xoff), (((yy + 260) + (i * 10)) - buff), (((xx + 558) + buff) + scroll_xoff), (((yy + 263) + (i * 10)) + buff), 0)
        }
        if (pagemax[pm] > 0)
            draw_sprite_ext(spr_morearrow, 0, ((xx + 551) + scroll_xoff), ((yy + 250) - (sin((cur_jewel / 12)) * 3)), 1, -1, 0, c_white, 1)
        if ((5 + pagemax[pm]) < 11)
            draw_sprite_ext(spr_morearrow, 0, ((xx + 551) + scroll_xoff), ((yy + 385) + (sin((cur_jewel / 12)) * 3)), 1, 1, 0, c_white, 1)
    }
    draw_set_color(c_white)
    if (global.submenu == 11)
    {
        if (global.submenucoord[11] == 0)
            draw_text((xx + 20), (yy + 10), string_hash_to_newline(charweapondesc[charcoord]))
        if (global.submenucoord[11] == 1)
            draw_text((xx + 20), (yy + 10), string_hash_to_newline(chararmor1desc[charcoord]))
        if (global.submenucoord[11] == 2)
            draw_text((xx + 20), (yy + 10), string_hash_to_newline(chararmor2desc[charcoord]))
    }
    if (global.submenu == 12)
        draw_text((xx + 20), (yy + 10), string_hash_to_newline(weapondesc[global.submenucoord[12]]))
    if (global.submenu == 13 || global.submenu == 14)
        draw_text((xx + 20), (yy + 10), string_hash_to_newline(armordesc[global.submenucoord[global.submenu]]))
    draw_set_color(c_white)
    draw_text((xx + 100), ((yy + 230) + (ch_vspace * 0)), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_556_0")))
    draw_item_icon((xx + 74), ((yy + 236) + (ch_vspace * 0)), 1)
    draw_text((xx + 100), ((yy + 230) + (ch_vspace * 1)), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_557_0")))
    draw_item_icon((xx + 74), ((yy + 236) + (ch_vspace * 1)), 4)
    draw_text((xx + 100), ((yy + 230) + (ch_vspace * 2)), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_558_0")))
    draw_item_icon((xx + 74), ((yy + 236) + (ch_vspace * 2)), 5)
    atsum = (((global.at[global.char[coord]] + global.itemat[global.char[coord], 0]) + global.itemat[global.char[coord], 1]) + global.itemat[global.char[coord], 2])
    dfsum = (((global.df[global.char[coord]] + global.itemdf[global.char[coord], 0]) + global.itemdf[global.char[coord], 1]) + global.itemdf[global.char[coord], 2])
    magsum = (((global.mag[global.char[coord]] + global.itemmag[global.char[coord], 0]) + global.itemmag[global.char[coord], 1]) + global.itemmag[global.char[coord], 2])
    grazesum = global.grazeamt
    sizesum = global.grazesize
    for (i = 0; i < 3; i += 1)
    {
        for (j = 0; j < 3; j += 1)
        {
            if (global.char[i] != 0)
            {
                grazesum += global.itemgrazeamt[global.char[i], j]
                sizesum += global.itemgrazesize[global.char[i], j]
            }
        }
    }
    if (global.submenu == 12 || global.submenu == 13 || global.submenu == 14)
    {
        cur = global.submenucoord[global.submenu]
        g = 0
        if (global.submenu == 12)
        {
            if (charcoord == 1 && weaponchar1[cur] == 0)
                g = 1
            if (charcoord == 2 && weaponchar2[cur] == 0)
                g = 1
            if (charcoord == 3 && weaponchar3[cur] == 0)
                g = 1
        }
        if (global.submenu == 13 || global.submenu == 14)
        {
            if (charcoord == 1 && armorchar1[cur] == 0)
                g = 1
            if (charcoord == 2 && armorchar2[cur] == 0)
                g = 1
            if (charcoord == 3 && armorchar3[cur] == 0)
                g = 1
        }
        _abilitytext[0] = charweaponability[charcoord]
        _abilitytext[1] = chararmor1ability[charcoord]
        _abilitytext[2] = chararmor2ability[charcoord]
        _abilitycolor[0] = c_dkgray
        _abilitycolor[1] = c_dkgray
        _abilitycolor[2] = c_dkgray
        _abilityicon[0] = charweaponabilityicon[charcoord]
        _abilityicon[1] = chararmor1abilityicon[charcoord]
        _abilityicon[2] = chararmor2abilityicon[charcoord]
        if (g == 0 && global.submenu == 12)
        {
            atup = (weaponat[cur] - global.itemat[charcoord, 0])
            dfup = (weapondf[cur] - global.itemdf[charcoord, 0])
            magup = (weaponmag[cur] - global.itemmag[charcoord, 0])
            styleup = weaponstyle[cur]
            grazeup = (weapongrazeamt[cur] - global.itemgrazeamt[charcoord, 0])
            sizeup = (weapongrazesize[cur] - global.itemgrazesize[charcoord, 0])
            _abilitycolor[0] = c_white
            _abilityicon[0] = weaponabilityicon[cur]
            if (weaponability[cur] != _abilitytext[0])
            {
                _abilitycolor[0] = c_yellow
                if (weaponability[cur] == " ")
                    _abilitycolor[0] = c_red
            }
            _abilitytext[0] = weaponability[cur]
            draw_set_color(c_white)
            if (atup > 0)
                draw_set_color(c_yellow)
            if (atup < 0)
                draw_set_color(c_red)
            if (global.lang == "ja")
                draw_text((xx + 230), ((yy + 230) + (ch_vspace * 0)), string_hash_to_newline(string((atsum + atup))))
            else
                draw_text((xx + 230), ((yy + 230) + (ch_vspace * 0)), string_hash_to_newline((((string((atsum + atup)) + scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_643_0")) + string(atup)) + ")")))
            draw_set_color(c_white)
            if (dfup > 0)
                draw_set_color(c_yellow)
            if (dfup < 0)
                draw_set_color(c_red)
            draw_text((xx + 230), ((yy + 230) + (ch_vspace * 1)), string_hash_to_newline((dfsum + dfup)))
            draw_set_color(c_white)
            if (magup > 0)
                draw_set_color(c_yellow)
            if (magup < 0)
                draw_set_color(c_red)
            draw_text((xx + 230), ((yy + 230) + (ch_vspace * 2)), string_hash_to_newline((magsum + magup)))
        }
        if (g == 0)
        {
            if (global.submenu == 13 || global.submenu == 14)
            {
                arno = (global.submenu - 12)
                atup = (armorat[cur] - global.itemat[charcoord, arno])
                dfup = (armordf[cur] - global.itemdf[charcoord, arno])
                magup = (armormag[cur] - global.itemmag[charcoord, arno])
                grazeup = (armorgrazeamt[cur] - global.itemgrazeamt[charcoord, arno])
                sizeup = (armorgrazesize[cur] - global.itemgrazesize[charcoord, arno])
                _abilitycolor[arno] = c_white
                _abilityicon[arno] = armorabilityicon[cur]
                if (armorability[cur] != _abilitytext[arno])
                {
                    _abilitycolor[arno] = c_yellow
                    if (armorability[cur] == " ")
                        _abilitycolor[arno] = c_red
                }
                _abilitytext[arno] = armorability[cur]
                draw_set_color(c_white)
                if (atup > 0)
                    draw_set_color(c_yellow)
                if (atup < 0)
                    draw_set_color(c_red)
                draw_text((xx + 230), ((yy + 230) + (ch_vspace * 0)), string_hash_to_newline((atsum + atup)))
                draw_set_color(c_white)
                if (dfup > 0)
                    draw_set_color(c_yellow)
                if (dfup < 0)
                    draw_set_color(c_red)
                draw_text((xx + 230), ((yy + 230) + (ch_vspace * 1)), string_hash_to_newline((dfsum + dfup)))
                draw_set_color(c_white)
                if (magup > 0)
                    draw_set_color(c_yellow)
                if (magup < 0)
                    draw_set_color(c_red)
                draw_text((xx + 230), ((yy + 230) + (ch_vspace * 2)), string_hash_to_newline((magsum + magup)))
            }
        }
        if (g == 1)
        {
            draw_text((xx + 230), ((yy + 230) + (ch_vspace * 0)), string_hash_to_newline(atsum))
            draw_text((xx + 230), ((yy + 230) + (ch_vspace * 1)), string_hash_to_newline(dfsum))
            draw_text((xx + 230), ((yy + 230) + (ch_vspace * 2)), string_hash_to_newline(magsum))
        }
        for (i = 0; i < 3; i += 1)
        {
            if (_abilitytext[i] == " ")
            {
                draw_set_color(_abilitycolor[i])
                draw_text((xx + 100), ((yy + 230) + (ch_vspace * (i + 3))), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_736_0")))
            }
            else
            {
                draw_set_color(_abilitycolor[i])
                draw_text((xx + 100), ((yy + 230) + (ch_vspace * (i + 3))), string_hash_to_newline(_abilitytext[i]))
                draw_set_color(c_orange)
                draw_item_icon((xx + 74), (((yy + 230) + (ch_vspace * (i + 3))) + 8), _abilityicon[i])
            }
        }
    }
    else
    {
        draw_text((xx + 230), ((yy + 230) + (ch_vspace * 0)), string_hash_to_newline(atsum))
        draw_text((xx + 230), ((yy + 230) + (ch_vspace * 1)), string_hash_to_newline(dfsum))
        draw_text((xx + 230), ((yy + 230) + (ch_vspace * 2)), string_hash_to_newline(magsum))
        _abilitytext[0] = charweaponability[charcoord]
        _abilitytext[1] = chararmor1ability[charcoord]
        _abilitytext[2] = chararmor2ability[charcoord]
        _abilitycolor[0] = c_white
        _abilitycolor[1] = c_white
        _abilitycolor[2] = c_white
        _abilityicon[0] = charweaponabilityicon[charcoord]
        _abilityicon[1] = chararmor1abilityicon[charcoord]
        _abilityicon[2] = chararmor2abilityicon[charcoord]
        for (i = 0; i < 3; i += 1)
        {
            if (_abilitytext[i] == " ")
            {
                draw_set_color(c_dkgray)
                draw_text((xx + 100), ((yy + 230) + (ch_vspace * (i + 3))), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_773_0")))
            }
            else
            {
                draw_set_color(_abilitycolor[i])
                draw_text((xx + 100), ((yy + 230) + (ch_vspace * (i + 3))), string_hash_to_newline(_abilitytext[i]))
                draw_set_color(c_orange)
                draw_item_icon((xx + 74), (((yy + 230) + (ch_vspace * (i + 3))) + 8), _abilityicon[i])
            }
        }
    }
}
if (global.menuno == 1)
{
    draw_set_color(c_black)
    if (global.lang == "ja")
    {
        draw_rectangle((xx + 46), (yy + 90), (xx + 594), (yy + 360), 0)
        scr_darkbox((xx + 36), (yy + 80), (xx + 604), (yy + 370))
    }
    else
    {
        draw_rectangle((xx + 70), (yy + 90), (xx + 570), (yy + 360), 0)
        scr_darkbox((xx + 60), (yy + 80), (xx + 580), (yy + 370))
    }
    scr_84_set_draw_font("mainbig")
    scr_itemname()
    if (global.lang == "ja")
    {
        if (global.submenu == 1)
            draw_sprite(spr_heart, 0, ((xx + 134) + (120 * global.submenucoord[1])), (yy + 120))
        draw_set_color(c_white)
        if (global.submenu > 1)
        {
            if (global.submenucoord[1] == 0)
                draw_set_color(c_orange)
            else
                draw_set_color(c_gray)
        }
        draw_text((xx + 158), (yy + 110), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_807_0")))
        if (global.submenu > 1)
        {
            if (global.submenucoord[1] == 1)
                draw_set_color(c_orange)
            else
                draw_set_color(c_gray)
        }
        draw_text((xx + 278), (yy + 110), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_809_0")))
        if (global.submenu > 1)
        {
            if (global.submenucoord[1] == 2)
                draw_set_color(c_orange)
            else
                draw_set_color(c_gray)
        }
        draw_text((xx + 398), (yy + 110), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_811_0")))
    }
    else
    {
        if (global.submenu == 1)
            draw_sprite(spr_heart, 0, ((xx + 155) + (120 * global.submenucoord[1])), (yy + 120))
        draw_set_color(c_white)
        if (global.submenu > 1)
        {
            if (global.submenucoord[1] == 0)
                draw_set_color(c_orange)
            else
                draw_set_color(c_gray)
        }
        draw_text((xx + 180), (yy + 110), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_807_0")))
        if (global.submenu > 1)
        {
            if (global.submenucoord[1] == 1)
                draw_set_color(c_orange)
            else
                draw_set_color(c_gray)
        }
        draw_text((xx + 300), (yy + 110), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_809_0")))
        if (global.submenu > 1)
        {
            if (global.submenucoord[1] == 2)
                draw_set_color(c_orange)
            else
                draw_set_color(c_gray)
        }
        draw_text((xx + 420), (yy + 110), string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_811_0")))
    }
    if (global.submenu >= 2 && global.submenu <= 6 && global.submenu != 4)
    {
        sm = global.submenucoord[2]
        yheart = (((floor((sm / 2)) * 30) + 162) + yy)
        if (global.lang == "ja")
        {
            xheart = (72 + xx)
            if (sm == 1 || sm == 3 || sm == 5 || sm == 7 || sm == 9 || sm == 11)
                xheart = (334 + xx)
        }
        else
        {
            xheart = (120 + xx)
            if (sm == 1 || sm == 3 || sm == 5 || sm == 7 || sm == 9 || sm == 11)
                xheart = (330 + xx)
        }
        if (global.submenu == 2 || global.submenu == 3)
            draw_sprite(spr_heart, 0, xheart, yheart)
        draw_set_color(c_white)
        draw_text((xx + 20), (yy + 10), string_hash_to_newline(itemdesc[global.submenucoord[2]]))
    }
    if (global.submenu == 7)
    {
        draw_set_color(c_white)
        if (global.lang == "ja")
            draw_text((xx + 20), (yy + 10), (global.itemname[global.submenucoord[2]] + string_hash_to_newline(scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_829_0"))))
        else
            draw_text((xx + 20), (yy + 10), string_hash_to_newline(((scr_84_get_lang_string("obj_darkcontroller_slash_Draw_0_gml_829_0") + global.itemname[global.submenucoord[2]]) + "?")))
    }
    if (global.submenucoord[1] != 2)
    {
        if (global.lang == "ja")
        {
            draw_set_color(bcolor)
            for (i = 0; i < 6; i += 1)
            {
                draw_text((xx + 94), ((yy + 154) + (30 * i)), string_hash_to_newline(global.itemname[(i * 2)]))
                draw_text((xx + 358), ((yy + 154) + (30 * i)), string_hash_to_newline(global.itemname[((i * 2) + 1)]))
            }
            draw_set_color(c_white)
            if (global.submenu == 1)
                draw_set_color(c_gray)
            for (i = 0; i < 6; i += 1)
            {
                draw_text((xx + 92), ((yy + 152) + (30 * i)), string_hash_to_newline(global.itemname[(i * 2)]))
                draw_text((xx + 356), ((yy + 152) + (30 * i)), string_hash_to_newline(global.itemname[((i * 2) + 1)]))
            }
        }
        else
        {
            draw_set_color(bcolor)
            for (i = 0; i < 6; i += 1)
            {
                draw_text((xx + 148), ((yy + 154) + (30 * i)), string_hash_to_newline(global.itemname[(i * 2)]))
                draw_text((xx + 358), ((yy + 154) + (30 * i)), string_hash_to_newline(global.itemname[((i * 2) + 1)]))
            }
            draw_set_color(c_white)
            if (global.submenu == 1)
                draw_set_color(c_gray)
            for (i = 0; i < 6; i += 1)
            {
                draw_text((xx + 146), ((yy + 152) + (30 * i)), string_hash_to_newline(global.itemname[(i * 2)]))
                draw_text((xx + 356), ((yy + 152) + (30 * i)), string_hash_to_newline(global.itemname[((i * 2) + 1)]))
            }
        }
    }
    if (global.submenucoord[1] == 2)
    {
        scr_keyiteminfo_all()
        if (global.lang == "ja")
        {
            draw_set_color(bcolor)
            for (i = 0; i < 6; i += 1)
            {
                draw_text((xx + 94), ((yy + 154) + (30 * i)), string_hash_to_newline(keyitemname[(i * 2)]))
                draw_text((xx + 358), ((yy + 154) + (30 * i)), string_hash_to_newline(keyitemname[((i * 2) + 1)]))
            }
            draw_set_color(c_white)
            if (global.submenu == 1)
                draw_set_color(c_gray)
            for (i = 0; i < 6; i += 1)
            {
                if (global.submenu == 4)
                {
                    if (keyitemusable[(i * 2)] == 1)
                        draw_set_color(c_white)
                    else
                        draw_set_color(c_silver)
                }
                draw_text((xx + 92), ((yy + 152) + (30 * i)), string_hash_to_newline(keyitemname[(i * 2)]))
                if (global.submenu == 4)
                {
                    if (keyitemusable[((i * 2) + 1)] == 1)
                        draw_set_color(c_white)
                    else
                        draw_set_color(c_silver)
                }
                draw_text((xx + 356), ((yy + 152) + (30 * i)), string_hash_to_newline(keyitemname[((i * 2) + 1)]))
            }
        }
        else
        {
            draw_set_color(bcolor)
            for (i = 0; i < 6; i += 1)
            {
                draw_text((xx + 148), ((yy + 154) + (30 * i)), string_hash_to_newline(keyitemname[(i * 2)]))
                draw_text((xx + 358), ((yy + 154) + (30 * i)), string_hash_to_newline(keyitemname[((i * 2) + 1)]))
            }
            draw_set_color(c_white)
            if (global.submenu == 1)
                draw_set_color(c_gray)
            for (i = 0; i < 6; i += 1)
            {
                if (global.submenu == 4)
                {
                    if (keyitemusable[(i * 2)] == 1)
                        draw_set_color(c_white)
                    else
                        draw_set_color(c_silver)
                }
                draw_text((xx + 146), ((yy + 152) + (30 * i)), string_hash_to_newline(keyitemname[(i * 2)]))
                if (global.submenu == 4)
                {
                    if (keyitemusable[((i * 2) + 1)] == 1)
                        draw_set_color(c_white)
                    else
                        draw_set_color(c_silver)
                }
                draw_text((xx + 356), ((yy + 152) + (30 * i)), string_hash_to_newline(keyitemname[((i * 2) + 1)]))
            }
        }
        if (global.submenu == 4)
        {
            sm = global.submenucoord[4]
            yheart = (((floor((sm / 2)) * 30) + 162) + yy)
            if (global.lang == "ja")
            {
                xheart = (72 + xx)
                if (sm == 1 || sm == 3 || sm == 5 || sm == 7 || sm == 9 || sm == 11)
                    xheart = (334 + xx)
            }
            else
            {
                xheart = (120 + xx)
                if (sm == 1 || sm == 3 || sm == 5 || sm == 7 || sm == 9 || sm == 11)
                    xheart = (330 + xx)
            }
            draw_sprite(spr_heart, 0, xheart, yheart)
            draw_set_color(c_white)
            draw_text((xx + 20), (yy + 10), string_hash_to_newline(keyitemdesc[global.submenucoord[4]]))
        }
    }
}
