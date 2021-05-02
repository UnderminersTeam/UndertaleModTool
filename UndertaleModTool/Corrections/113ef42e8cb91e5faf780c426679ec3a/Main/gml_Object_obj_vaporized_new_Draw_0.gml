dn = 1
ht_a = (sprite_height - (line * 2))
ht_b = (y + (line * 2))
ht_c = (line * 2)
if (dn == 1)
    draw_sprite_part_ext(sprite_index, image_index, 0, ht_c, wd, ht_a, x, ht_b, image_xscale, image_yscale, c_white, 1)
if (finishedreading == 0)
{
    repeat (4)
    {
        ww = 0
        mychar = "0"
        while (mychar != "}" && mychar != "~")
        {
            mychar = string_char_at(mydata, (myread + 1))
            draw_set_color(c_black)
            if (ord(mychar) >= 84 && ord(mychar) <= 121)
            {
                for (i = 0; i < (ord(mychar) - 85); i += 1)
                    ww += 2
            }
            draw_set_color(c_white)
            if (ord(mychar) >= 39 && ord(mychar) <= 82)
            {
                if (wd > 120 && spec == 0)
                {
                    blk = instance_create((x + ww), (y + (line * 2)), obj_whtpxlgrav)
                    blk.image_xscale = (ord(mychar) - 40)
                    with (blk)
                        event_user(0)
                    ww += ((ord(mychar) - 40) * 2)
                }
                else
                {
                    for (i = 0; i < (ord(mychar) - 40); i += 1)
                    {
                        instance_create((x + ww), ((y + (line * 2)) + 2), obj_whtpxlgrav)
                        ww += 2
                    }
                }
            }
            myread += 1
        }
        ww = 0
        line += 1
        if (mychar == "~")
        {
            finishedreading = 1
            instance_destroy()
            return;
        }
        else
            alarm[0] = (1 + myvapor)
    }
}
