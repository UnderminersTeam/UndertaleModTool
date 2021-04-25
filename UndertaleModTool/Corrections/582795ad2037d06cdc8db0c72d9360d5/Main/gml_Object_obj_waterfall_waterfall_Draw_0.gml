siner += 1
if (image_xscale == 1)
    draw_sprite(spr_waterfall_singletop, (siner / 5), x, y)
if (image_xscale > 1)
{
    draw_sprite(spr_waterfall_topleft, (siner / 5), x, y)
    for (i = 1; i < (image_xscale + 1); i += 1)
    {
        if (i < image_xscale)
        {
            draw_sprite(spr_waterfall_midtop, (siner / 5), (x + (i * 20)), y)
        }
        else
        {
            draw_sprite(spr_waterfall_topright, (siner / 5), ((x + (i * 20)) - 20), y)
            break
        }
    }
}
if (image_yscale > 1 && image_xscale == 1)
{
    for (i = 1; i <= image_yscale; i += 1)
        draw_sprite(spr_waterfall_midmid, (siner / 5), x, (y + (i * 20)))
}
if (image_yscale > 1 && image_xscale > 1)
{
    for (j = 1; j <= image_yscale; j += 1)
    {
        if (j < image_yscale)
            draw_sprite(spr_waterfall_midleft, (siner / 5), x, (y + (j * 20)))
        if (j == image_yscale)
            draw_sprite(spr_waterfall_bottomleft, (siner / 5), x, ((y + (j * 20)) - 20))
        for (i = 1; i <= image_xscale; i += 1)
        {
            if (j < image_yscale)
            {
                if (i == image_xscale)
                    draw_sprite(spr_waterfall_midright, (siner / 5), ((x + (i * 20)) - 20), (y + (j * 20)))
                else
                    draw_sprite(spr_waterfall_midmid, (siner / 5), (x + (i * 20)), (y + (j * 20)))
            }
            if (j == image_yscale)
            {
                if (i == image_xscale)
                    draw_sprite(spr_waterfall_bottomright, (siner / 5), ((x + (i * 20)) - 20), ((y + (j * 20)) - 20))
                else
                    draw_sprite(spr_waterfall_bottommid, (siner / 5), (x + (i * 20)), ((y + (j * 20)) - 20))
            }
        }
    }
}
