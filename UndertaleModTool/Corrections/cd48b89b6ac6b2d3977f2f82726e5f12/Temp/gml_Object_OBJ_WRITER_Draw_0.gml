myx = writingx
myy = writingy
for (n = 1; n < (stringpos + 1); n = n + 1)
{
    nskip = 0
    if (string_char_at(originalstring, n) == "&")
    {
        script_execute(SCR_NEWLINE)
        n += 1
    }
    if (string_char_at(originalstring, n) == "^")
    {
        if (string_char_at(originalstring, (n + 1)) == "0")
            nskip = 1
        else
            n += 2
    }
    if (string_char_at(originalstring, n) == "\")
    {
        if (string_char_at(originalstring, (n + 1)) == "R")
            mycolor = c_red
        if (string_char_at(originalstring, (n + 1)) == "G")
            mycolor = c_lime
        if (string_char_at(originalstring, (n + 1)) == "W")
            mycolor = c_white
        if (string_char_at(originalstring, (n + 1)) == "Y")
            mycolor = c_yellow
        if (string_char_at(originalstring, (n + 1)) == "X")
            mycolor = c_black
        if (string_char_at(originalstring, (n + 1)) == "B")
            mycolor = c_blue
        if (string_char_at(originalstring, (n + 1)) == "O")
            mycolor = c_orange
        if (string_char_at(originalstring, (n + 1)) == "L")
            mycolor = $FFA914
        if (string_char_at(originalstring, (n + 1)) == "P")
            mycolor = c_fuchsia
        if (string_char_at(originalstring, (n + 1)) == "p")
            mycolor = $D4BBFF
        if (string_char_at(originalstring, (n + 1)) == "C")
        {
            if (global.inbattle == 0)
            {
                if (instance_exists(obj_choicer) == 0)
                    choicer = instance_create(0, 0, obj_choicer)
                choicer.creator = id
                halt = 5
            }
        }
        if (string_char_at(originalstring, (n + 1)) == "M")
        {
            global.flag[20] = real(string_char_at(originalstring, (n + 2)))
            n += 1
        }
        if (string_char_at(originalstring, (n + 1)) == "E")
        {
            global.faceemotion = real(string_char_at(originalstring, (n + 2)))
            n += 1
        }
        if (string_char_at(originalstring, (n + 1)) == "F")
        {
            global.facechoice = real(string_char_at(originalstring, (n + 2)))
            global.facechange = 1
            n += 1
        }
        if (string_char_at(originalstring, (n + 1)) == "T")
        {
            newtyper = string_char_at(originalstring, (n + 2))
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
            script_execute(SCR_TEXTTYPE, global.typer)
            global.facechange = 1
            n += 1
        }
        if (string_char_at(originalstring, (n + 1)) == "z")
        {
            sym = real(string_char_at(originalstring, (n + 2)))
            sym_s = spr_infinitysign
            if (sym == 4)
                sym_s = spr_infinitysign
            if (sym == 4)
                draw_sprite_ext(sym_s, 0, (myx + (random(shake) - (shake / 2))), ((myy + 10) + (random(shake) - (shake / 2))), 2, 2, 0, c_white, 1)
            n += 1
        }
        n += 2
    }
    if (string_char_at(originalstring, n) == "/")
    {
        halt = 1
        if (string_char_at(originalstring, (n + 1)) == "%")
            halt = 2
        if (string_char_at(originalstring, (n + 1)) == "^" && string_char_at(originalstring, (n + 2)) != "0")
            halt = 4
        if (string_char_at(originalstring, (n + 1)) == "*")
            halt = 6
        break
    }
    if (string_char_at(originalstring, n) == "%")
    {
        if (string_char_at(originalstring, (n + 1)) == "%")
        {
            instance_destroy()
            break
        }
        stringpos = 1
        stringno += 1
        originalstring = mystring[stringno]
        myx = writingx
        myy = writingy
        lineno = 0
        alarm[0] = textspeed
        myletter = " "
        break
    }
    if (myx > writingxend)
        script_execute(SCR_NEWLINE)
    myletter = string_char_at(originalstring, n)
    if (global.typer == 18)
    {
        if (myletter == "l" || myletter == "i")
            myx += 2
        if (myletter == "I")
            myx += 2
        if (myletter == "!")
            myx += 2
        if (myletter == ".")
            myx += 2
        if (myletter == "S")
            myx += 1
        if (myletter == "?")
            myx += 2
        if (myletter == "D")
            myx += 1
        if (myletter == "A")
            myx += 1
        if (myletter == "'")
            myx += 1
    }
    draw_set_font(myfont)
    draw_set_color(mycolor)
    if (shake > 38)
    {
        if (shake == 39)
        {
            direction += 10
            draw_text((myx + hspeed), (myy + vspeed), myletter)
        }
        if (shake == 40)
            draw_text((myx + hspeed), (myy + vspeed), myletter)
        if (shake == 41)
        {
            direction += (10 * n)
            draw_text((myx + hspeed), (myy + vspeed), myletter)
            direction -= (10 * n)
        }
        if (shake == 42)
        {
            direction += (20 * n)
            draw_text((myx + hspeed), (myy + vspeed), myletter)
            direction -= (20 * n)
        }
        if (shake == 43)
        {
            direction += (30 * n)
            draw_text(((myx + (hspeed * 0.7)) + 10), (myy + (vspeed * 0.7)), myletter)
            direction -= (30 * n)
        }
    }
    else
        draw_text((myx + (random(shake) - (shake / 2))), (myy + (random(shake) - (shake / 2))), myletter)
    myx += spacing
    if (myfont == fnt_comicsans)
    {
        if (myletter == "w")
            myx += 2
        if (myletter == "m")
            myx += 2
        if (myletter == "i")
            myx -= 2
        if (myletter == "l")
            myx -= 2
        if (myletter == "s")
            myx -= 1
        if (myletter == "j")
            myx -= 1
    }
    if (myfont == fnt_papyrus)
    {
        if (myletter == "D")
            myx += 1
        if (myletter == "Q")
            myx += 3
        if (myletter == "M")
            myx += 1
        if (myletter == "L")
            myx -= 1
        if (myletter == "K")
            myx -= 1
        if (myletter == "C")
            myx += 1
        if (myletter == ".")
            myx -= 3
        if (myletter == "!")
            myx -= 3
        if (myletter == "O" || myletter == "W")
            myx += 2
        if (myletter == "I")
            myx -= 6
        if (myletter == "T")
            myx -= 1
        if (myletter == "P")
            myx -= 2
        if (myletter == "R")
            myx -= 2
        if (myletter == "A")
            myx += 1
        if (myletter == "H")
            myx += 1
        if (myletter == "B")
            myx += 1
        if (myletter == "G")
            myx += 1
        if (myletter == "F")
            myx -= 1
        if (myletter == "?")
            myx -= 3
        if (myletter == "'")
            myx -= 6
        if (myletter == "J")
            myx -= 1
    }
    n += nskip
}
