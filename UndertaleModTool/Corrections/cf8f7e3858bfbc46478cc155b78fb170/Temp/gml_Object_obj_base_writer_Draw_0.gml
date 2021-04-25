if vtext
    myx = (writingxend - vspacing)
else
    myx = writingx
myy = writingy
var halfsize = 0
for (var n = 1; n <= stringpos; n++)
{
    var ch = string_char_at(originalstring, n)
    var myletter = ""
    if (ch == "^" && string_char_at(originalstring, (n + 1)) != "0")
    {
        n++
        continue
    }
    if (ch == "\\")
    {
        n++
        ch = string_char_at(originalstring, n)
        if (ch == "R")
            mycolor = c_red
        else if (ch == "G")
            mycolor = c_lime
        else if (ch == "W")
            mycolor = c_white
        else if (ch == "Y")
            mycolor = c_yellow
        else if (ch == "X")
            mycolor = c_black
        else if (ch == "B")
            mycolor = c_blue
        else if (ch == "O")
            mycolor = c_orange
        else if (ch == "L")
            mycolor = 0xFDC00E
        else if (ch == "P")
            mycolor = c_fuchsia
        else if (ch == "p")
            mycolor = 0xD4BBFF
        else if (ch == "C")
            event_user(1)
        else if (ch == "M")
        {
            n++
            ch = string_char_at(originalstring, n)
            global.flag[20] = real(ch)
        }
        else if (ch == "E")
        {
            n++
            ch = string_char_at(originalstring, n)
            global.faceemotion = real(ch)
        }
        else if (ch == "F")
        {
            n++
            ch = string_char_at(originalstring, n)
            global.facechoice = real(ch)
            global.facechange = 1
        }
        else if (ch == "S")
            n++
        else if (ch == "T")
        {
            n++
            var newtyper = string_char_at(originalstring, n)
            if (newtyper == "-")
                halfsize = 1
            else if (newtyper == "+")
                halfsize = 0
            else
            {
                if (newtyper == "T")
                    global.typer = 4
                if (newtyper == "t")
                    global.typer = 48
                if (newtyper == "0")
                    global.typer = 5
                if (newtyper == "S")
                    global.typer = 10
                if (newtyper == "F")
                    global.typer = 16
                if (newtyper == "s")
                    global.typer = 17
                if (newtyper == "P")
                    global.typer = 18
                if (newtyper == "M")
                    global.typer = 27
                if (newtyper == "U")
                    global.typer = 37
                if (newtyper == "A")
                    global.typer = 47
                if (newtyper == "a")
                    global.typer = 60
                if (newtyper == "R")
                    global.typer = 76
                SCR_TEXTTYPE(global.typer)
                global.facechange = 1
            }
        }
        else if (ch == "z")
        {
            n++
            var sym = real(string_char_at(originalstring, n))
            if (sym == 4)
            {
                var sym_s = spr_infinitysign
                draw_sprite_ext(sym_s, 0, (myx + (random(shake) - (shake / 2))), ((myy + 10) + (random(shake) - (shake / 2))), 2, 2, 0, c_white, 1)
            }
        }
        else if (ch == "*")
        {
            n++
            ch = string_char_at(originalstring, n)
            var icontype = 0
            if (myfont == fnt_papyrus || myfont == fnt_ja_papyrus_btl)
                icontype = 1
            var sprite = scr_getbuttonsprite(ch, icontype)
            if (sprite != -4)
            {
                var spritex = myx
                var spritey = myy
                if (shake > 38)
                {
                    if (shake == 39)
                    {
                        direction += 10
                        spritex += hspeed
                        spritey += vspeed
                    }
                    else if (shake == 40)
                    {
                        spritex += hspeed
                        spritey += vspeed
                    }
                    else if (shake == 41)
                    {
                        direction += (10 * n)
                        spritex += hspeed
                        spritey += vspeed
                        direction -= (10 * n)
                    }
                    else if (shake == 42)
                    {
                        direction += (20 * n)
                        spritex += hspeed
                        spritey += vspeed
                        direction -= (20 * n)
                    }
                    else if (shake == 43)
                    {
                        direction += (30 * n)
                        spritex += ((hspeed * 0.7) + 10)
                        spritey += (vspeed * 0.7)
                        direction -= (30 * n)
                    }
                }
                else if (!instance_exists(obj_papdate))
                {
                    spritex += (random(shake) - (shake / 2))
                    spritey += (random(shake) - (shake / 2))
                }
                var icon_scale = 1
                if (myfont == fnt_main || myfont == fnt_ja_main)
                    icon_scale = 2
                if (myfont == fnt_main || myfont == fnt_maintext)
                    spritey += (1 * icon_scale)
                if (myfont == fnt_ja_papyrus_btl)
                    spritex -= 1
                if (myfont == fnt_papyrus && icontype == 1)
                    spritey += floor(((16 - sprite_get_height(sprite)) / 2))
                if vtext
                {
                    draw_sprite_ext(sprite, 0, (spritex - sprite_get_width(sprite)), spritey, icon_scale, icon_scale, 0, c_white, 1)
                    myy += ((sprite_get_height(sprite) + 1) * icon_scale)
                }
                else
                {
                    draw_sprite_ext(sprite, 0, spritex, spritey, icon_scale, icon_scale, 0, c_white, 1)
                    myx += ((sprite_get_width(sprite) + 1) * icon_scale)
                }
            }
        }
        else if (ch == ">")
        {
            n++
            var choiceindex = real(string_char_at(originalstring, n))
            if (choiceindex == 1)
                myx = 196
            else
            {
                myx = 100
                if (myfont == fnt_ja_comicsans_big)
                    myx += 11
            }
            if (__view_get(2, view_current) == 640)
                myx *= 2
            myx += __view_get(0, view_current)
        }
        continue
    }
    if (ch == "&")
    {
        script_execute(SCR_NEWLINE)
        continue
    }
    if (ch == "/")
    {
        halt = 1
        var nextch = string_char_at(originalstring, (n + 1))
        if (nextch == "%")
            halt = 2
        else if (nextch == "^" && string_char_at(originalstring, (n + 2)) != "0")
            halt = 4
        else if (nextch == "*")
            halt = 6
        break
    }
    else if (ch == "%")
    {
        if (string_char_at(originalstring, (n + 1)) == "%")
        {
            instance_destroy()
            break
        }
        stringno++
        originalstring = scr_replace_buttons_pc(mystring[stringno])
        stringpos = 0
        myx = writingx
        myy = writingy
        alarm[0] = textspeed
        break
    }
    else
    {
        myletter = string_char_at(originalstring, n)
        if (myletter == "^")
            n++
        if ((!vtext) && myx > writingxend)
            script_execute(SCR_NEWLINE)
        var letterx = myx
        var offsetx = 0
        var offsety = 0
        var halfscale = 1
        if halfsize
        {
            halfscale = 0.5
            if vtext
                offsetx += (vspacing * 0.33)
            else
                offsety += (vspacing * 0.33)
        }
        if (global.language == "en")
        {
            if (global.typer == 18)
            {
                if (myletter == "l" || myletter == "i")
                    letterx += 2
                if (myletter == "I")
                    letterx += 2
                if (myletter == "!")
                    letterx += 2
                if (myletter == ".")
                    letterx += 2
                if (myletter == "S")
                    letterx += 1
                if (myletter == "?")
                    letterx += 2
                if (myletter == "D")
                    letterx += 1
                if (myletter == "A")
                    letterx += 1
                if (myletter == "'")
                    letterx += 1
            }
        }
        else if (global.language == "ja")
        {
            if (vtext && (myfont == fnt_ja_papyrus || myfont == fnt_ja_papyrus_btl))
            {
                if (myy == writingy && (myletter == "「" || myletter == "『"))
                    myy -= round((((string_width(string_hash_to_newline(myletter)) / 2) * htextscale) * halfscale))
            }
            else if (myfont == fnt_ja_maintext || myfont == fnt_ja_main)
            {
                var unit = (htextscale * halfscale)
                if (myfont == fnt_ja_main)
                    unit *= 2
                if (ord(myletter) < 1024 || ord(myletter) == 8211)
                {
                    if (n > 1)
                    {
                        var lastch = ord(string_char_at(originalstring, (n - 1)))
                        if (lastch >= 1024 && lastch < 65281 && lastch != 8211 && lastch != 12288)
                            letterx += unit
                    }
                }
            }
        }
        scr_setfont(myfont)
        draw_set_color(mycolor)
        if vtext
            var angle = -90
        else
            angle = 0
        if (shake > 38)
        {
            if (shake == 39)
            {
                direction += 10
                offsetx += hspeed
                offsety += vspeed
            }
            else if (shake == 40)
            {
                offsetx += hspeed
                offsety += vspeed
            }
            else if (shake == 41)
            {
                direction += (10 * n)
                offsetx += hspeed
                offsety += vspeed
                direction -= (10 * n)
            }
            else if (shake == 42)
            {
                direction += (20 * n)
                offsetx += hspeed
                offsety += vspeed
                direction -= (20 * n)
            }
            else if (shake == 43)
            {
                direction += (30 * n)
                offsetx += ((hspeed * 0.7) + 10)
                offsety += (vspeed * 0.7)
                direction -= (30 * n)
            }
        }
        else
        {
            offsetx += (random(shake) - (shake / 2))
            offsety += (random(shake) - (shake / 2))
        }
        var display_scale = (surface_get_width(application_surface) / __view_get(2, view_current))
        var finalx = (round(((letterx + offsetx) * display_scale)) / display_scale)
        var finaly = (round(((myy + offsety) * display_scale)) / display_scale)
        draw_text_transformed(finalx, finaly, string_hash_to_newline(myletter), (htextscale * halfscale), (vtextscale * halfscale), angle)
        letterx += spacing
        if (global.language == "en")
        {
            if (myfont == fnt_comicsans)
            {
                if (myletter == "w")
                    letterx += 2
                if (myletter == "m")
                    letterx += 2
                if (myletter == "i")
                    letterx -= 2
                if (myletter == "l")
                    letterx -= 2
                if (myletter == "s")
                    letterx -= 1
                if (myletter == "j")
                    letterx -= 1
            }
            else if (myfont == fnt_papyrus)
            {
                if (myletter == "D")
                    letterx += 1
                if (myletter == "Q")
                    letterx += 3
                if (myletter == "M")
                    letterx += 1
                if (myletter == "L")
                    letterx -= 1
                if (myletter == "K")
                    letterx -= 1
                if (myletter == "C")
                    letterx += 1
                if (myletter == ".")
                    letterx -= 3
                if (myletter == "!")
                    letterx -= 3
                if (myletter == "O" || myletter == "W")
                    letterx += 2
                if (myletter == "I")
                    letterx -= 6
                if (myletter == "T")
                    letterx -= 1
                if (myletter == "P")
                    letterx -= 2
                if (myletter == "R")
                    letterx -= 2
                if (myletter == "A")
                    letterx += 1
                if (myletter == "H")
                    letterx += 1
                if (myletter == "B")
                    letterx += 1
                if (myletter == "G")
                    letterx += 1
                if (myletter == "F")
                    letterx -= 1
                if (myletter == "?")
                    letterx -= 3
                if (myletter == "'")
                    letterx -= 6
                if (myletter == "J")
                    letterx -= 1
            }
        }
        else if (global.language == "ja")
        {
            if vtext
                myy += round(((string_width(string_hash_to_newline(myletter)) * htextscale) * halfscale))
            else if (myletter == " " || ord(myletter) >= 65377)
                letterx -= floor((spacing / 2))
            else if (ord(myletter) < 1024 || ord(myletter) == 8211)
            {
                if (myfont == fnt_ja_comicsans || myfont == fnt_ja_comicsans_big)
                    letterx -= floor((spacing * 0.3))
                else
                    letterx -= floor((spacing * 0.4))
            }
        }
        if (!vtext)
        {
            if halfsize
                myx = round((myx + ((letterx - myx) / 2)))
            else
                myx = letterx
        }
    }
}
