draw_set_color(c_green)
yoff = 0
siner += 1
if (fade == 1)
    alpha -= 0.01
if (alpha <= 0)
    instance_destroy()
repeat (2)
{
    for (i = 0; i < 6; i += 1)
    {
        draw_set_alpha(alpha)
        ysin = (sin((((i * 3) + (siner / 2)) / 8)) * 20)
        draw_rectangle((20 + (i * 100)), ((16 + yoff) + ysin), (20 + ((i + 1) * 100)), ((136 + yoff) + ysin), true)
    }
    yoff = 120
}
draw_set_alpha(1)
