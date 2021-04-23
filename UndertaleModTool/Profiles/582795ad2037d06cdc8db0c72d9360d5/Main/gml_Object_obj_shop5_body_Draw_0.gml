if (nowemote != global.faceemotion)
    event_user(0)
nowemote = global.faceemotion
siner += 1
draw_sprite(spr_5_coffeeline, floor((siner / 8)), 178, floor((62 + (sin((siner / 4)) * 1.5))))
draw_sprite(spr_5_tembody, 0, (99 + bodyx), (1 + bodyy))
if (global.faceemotion == 0)
{
    draw_sprite(spr_5_tembrows, 0, floor((138 + offx[0])), floor(((32 + offy[0]) + (facey / 2))))
    draw_sprite(spr_5_eyes1, 0, floor(((139 + offx[1]) + facex)), floor(((40 + offy[1]) + facey)))
    draw_sprite(spr_5_mouth1, 0, floor(((141 + offx[2]) + facex)), floor(((48 + offy[2]) + facey)))
    facetimer += 1
    if (facetimer > 90 && facetimer < 110)
        facex += (sin((facetimer / 10)) * 0.8)
    if (facetimer > 130 && facetimer < 150)
        facex += (sin((facetimer / 10)) * 0.8)
    if (facetimer > 190 && facetimer < 230)
    {
        facex *= 0.9
        if (facex <= 0.5)
            facex = 0
    }
    if (facetimer > 290 && facetimer < 310)
        facey += (sin((facetimer / 10)) * 0.8)
    if (facetimer > 326 && facetimer < 345)
        facey += (sin((facetimer / 10)) * 1.5)
    if (facetimer > 390 && facetimer < 430)
    {
        facey *= 0.9
        if (facey <= 0.5)
            facex = 0
    }
    if (facetimer == 460)
        facetimer = 0
}
if (global.faceemotion == 1)
{
    rr = (random(0.8) - random(0.8))
    rr2 = (random(0.8) - random(0.8))
    draw_sprite(spr_5_tembrows, 0, floor((138 + offx[0])), floor((((32 + offy[0]) + (facey / 2)) - sin((facetimer / 2)))))
    draw_sprite(spr_5_eyes2, 0, floor((((135 + offx[1]) + facex) + rr)), floor((((38 + offy[1]) + facey) + rr2)))
    draw_sprite(spr_5_mouth1, 0, floor(((141 + offx[2]) + facex)), floor(((48 + offy[2]) + facey)))
    facetimer += 1
}
if (global.faceemotion == 2)
{
    draw_sprite(spr_5_tembrows, 0, floor((138 + offx[0])), floor(((32 + offy[0]) + (facey / 2))))
    draw_sprite(spr_5_eyes3, 0, floor(((139 + offx[1]) + facex)), floor(((40 + offy[1]) + facey)))
    draw_sprite(spr_5_mouth2, floor((siner / 3)), floor(((141 + offx[2]) + facex)), floor(((48 + offy[2]) + facey)))
    draw_sprite_ext(spr_5_sweat, 0, 133, (39 + (sin((siner / 4)) * 1.5)), 1, 1, 0, c_white, (1 + sin((siner / 4))))
    if (facetimer > 45 && facetimer < 55)
        facex += (sin((facetimer / 5)) * 0.8)
    if (facetimer > 65 && facetimer < 75)
        facex += (sin((facetimer / 5)) * 0.8)
    if (facetimer > 95 && facetimer < 115)
    {
        facex *= 0.9
        if (facex <= 0.5)
            facex = 0
    }
    if (facetimer == 140)
        facetimer = 0
    facetimer += 1
}
if (global.faceemotion == 3)
{
    facex = 2
    facey = -2
    draw_sprite(spr_5_eyes4, 0, floor(((137 + offx[1]) + facex)), floor(((32 + offy[1]) + facey)))
    draw_sprite(spr_5_mouth3, floor((siner / 3)), floor(((146 + offx[2]) + facex)), floor(((42 + offy[2]) + facey)))
    facetimer += 1
}
if (global.faceemotion == 4)
{
    facey = (sin((facetimer / 5)) * 1.5)
    draw_sprite(spr_5_eyes5, 0, floor(((137 + offx[1]) + facex)), floor(((32 + offy[1]) + facey)))
    draw_sprite(spr_5_mouth3, floor((siner / 3)), floor((((144 + offx[2]) + facex) + (cos((siner / 1.5)) * 1.5))), floor(((43 + offy[2]) + facey)))
    draw_sprite_ext(spr_5_sweat, 0, 133, (39 + (sin((siner / 4)) * 1.5)), 1, 1, 0, c_white, (1 + sin((siner / 4))))
    facetimer += 1
}
if (global.faceemotion == 5)
{
    rr = (random(1) - random(1))
    rr2 = (random(1) - random(1))
    bodyx = rr
    bodyy = rr2
    facey = (sin((facetimer / 3)) * 2)
    draw_sprite(spr_5_eyes6, 0, floor(((137 + offx[1]) + facex)), floor(((31 + offy[1]) + facey)))
    draw_sprite(spr_5_mouth3, floor((siner / 3)), floor((((144 + offx[2]) + facex) + (cos(siner) * 2))), floor(((43 + offy[2]) + facey)))
    draw_sprite_ext(spr_5_sweat, 0, 133, (39 + (sin((siner / 2)) * 2)), 1, 1, 0, c_white, (1 + sin((siner / 2))))
    facetimer += 1
}
if (global.faceemotion == 6)
{
    draw_sprite(spr_5_sellface, floor((siner / 2)), floor(((139 + offx[1]) + facex)), floor(((25 + offy[1]) + facey)))
    facetimer += 1
}
if (global.faceemotion == 7)
{
    draw_sprite(spr_5_sellface_x, floor((siner / 2)), floor(((139 + offx[1]) + facex)), floor(((25 + offy[1]) + facey)))
    facetimer += 1
}
draw_sprite(spr_5_tembox, 0, (80 + boxx), 68)
if (sellmenu == 1)
{
    draw_set_color(c_white)
    scr_setfont(fnt_maintext)
    value[0] = 100
    value[1] = 5
    value[2] = 666
    value[3] = 10
    value[4] = 100
    value[5] = 5
    value[6] = 12
    value[7] = 0
    value[8] = 0
    value[9] = 0
    value[10] = 0
    for (i = 0; i < 8; i += 1)
    {
        s_value[i] = ("    " + string(value[i]))
        if (value[i] >= 10 && value[i] < 100)
            s_value[i] = ("  " + string(value[i]))
        if (value[i] >= 100 && value[i] < 1000)
            s_value[i] = string(value[i])
    }
    odd = -1
    if (sellpos == 0 || sellpos == 2 || sellpos == 4 || sellpos == 6)
    {
        odd = 0
        draw_sprite(spr_heartsmall, 0, 15, (135 + ((sellpos / 2) * 20)))
    }
    if (sellpos == 1 || sellpos == 3 || sellpos == 5 || sellpos == 7)
    {
        odd = 1
        draw_sprite(spr_heartsmall, 0, 155, (135 + (((sellpos - 1) / 2) * 20)))
    }
    if (sellpos == 8)
        draw_sprite(spr_heartsmall, 0, 15, 215)
    if (keyboard_check_pressed(vk_right) && odd == 0)
    {
        if (value[(sellpos + 1)] != 0)
            sellpos += 1
    }
    if (keyboard_check_pressed(vk_left) && odd == 1)
        sellpos -= 1
    if keyboard_check_pressed(vk_down)
    {
        d_fail = 0
        if (value[(sellpos + 2)] == 0)
            d_fail = 1
        if (d_fail == 1 && value[(sellpos + 1)] != 0)
            d_fail = 2
        if (sellpos == 6 || sellpos == 7 || sellpos == 8)
            d_fail = 1
        if (d_fail == 1)
            sellpos = 8
        else if (d_fail == 2)
            sellpos += 1
        else
            sellpos += 2
    }
    if keyboard_check_pressed(vk_up)
    {
        if (sellpos != 0 && sellpos != 1)
        {
            if (sellpos == 8)
            {
                this_i = -1
                i = 7
                while (this_i == -1)
                {
                    if (value[i] != 0)
                        this_i = i
                    i -= 1
                    if (i == -1)
                        this_i = 8
                }
                sellpos = this_i
            }
            else
                sellpos -= 2
        }
    }
    draw_set_color(c_white)
    for (i = 0; i < 4; i += 1)
    {
        if (value[(i * 2)] != 0)
            draw_text(30, (130 + (i * 20)), (s_value[(i * 2)] + "G - Ninechara"))
        if (value[((i * 2) + 1)] != 0)
            draw_text(170, (130 + (i * 20)), (s_value[((i * 2) + 1)] + "G - Ninechara"))
    }
    draw_text(30, 210, scr_gettext("shop_exit_submenu"))
    draw_set_color(c_yellow)
    draw_text(200, 210, "(9999 G)")
    if control_check_pressed(0)
    {
        buffer = 3
        if (sellpos == 8)
            sellmenu = 0
        else
        {
            sellmenu = 2
            sellpos2 = 0
        }
    }
}
if (sellmenu == 2)
{
    buffer -= 1
    draw_set_color(c_white)
    scr_setfont(fnt_maintext)
    draw_text(55, 150, (("Really sell Ninechara for " + string(value[sellpos])) + "G?"))
    draw_text(80, 180, "Yes")
    draw_text(190, 180, "No")
    draw_sprite(spr_heartsmall, 0, (65 + (sellpos2 * 110)), 185)
    draw_set_color(c_yellow)
    draw_text(200, 210, "(9999 G)")
    if (keyboard_check_pressed(vk_left) || keyboard_check_pressed(vk_right))
    {
        if (sellpos2 == 0)
            sellpos2 = 1
        else
            sellpos2 = 0
    }
    if (control_check_pressed(0) && buffer <= 0)
    {
        if (sellpos2 == 1)
            sellmenu = 1
        else
        {
        }
    }
}
if (global.flag[276] == 1)
    draw_sprite(spr_temhat, 0, ((99 + bodyx) + 37), (1 + bodyy))
