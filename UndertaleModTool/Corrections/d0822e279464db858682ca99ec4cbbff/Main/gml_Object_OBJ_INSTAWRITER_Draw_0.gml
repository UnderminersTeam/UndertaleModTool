myx = writingx
myy = writingy
for (n = 1; n < (stringpos + 1); n = n + 1)
{
    if (string_char_at(originalstring, n) == "&")
    {
        script_execute(SCR_NEWLINE)
        n += 1
    }
    if (string_char_at(originalstring, n) == "^")
        n += 2
    if (string_char_at(originalstring, n) == "\")
    {
        if (string_char_at(originalstring, (n + 1)) == "R")
            mycolor = c_red
        if (string_char_at(originalstring, (n + 1)) == "W")
            mycolor = c_white
        if (string_char_at(originalstring, (n + 1)) == "X")
            mycolor = c_black
        if (string_char_at(originalstring, (n + 1)) == "Y")
            mycolor = c_yellow
        if (string_char_at(originalstring, (n + 1)) == "G")
            mycolor = c_lime
        if (string_char_at(originalstring, (n + 1)) == "B")
            mycolor = c_blue
        if (string_char_at(originalstring, (n + 1)) == "p")
            mycolor = $D4BBFF
        if (string_char_at(originalstring, (n + 1)) == "P")
            script_execute(SCR_TEXTSETUP, 4, 255, x, y, (x + 150), 43, 4, 94, 10)
        if (string_char_at(originalstring, (n + 1)) == "C")
        {
            if (global.inbattle == 0)
            {
                if (instance_exists(obj_choicer) == 0)
                    choicer = instance_create(0, 0, obj_choicer)
                choicer.creator = id
            }
            if (global.inbattle == 1)
                halt = 5
        }
        n += 2
    }
    if (string_char_at(originalstring, n) == "/")
    {
        halt = 1
        if (string_char_at(originalstring, (n + 1)) == "%")
            halt = 2
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
}
