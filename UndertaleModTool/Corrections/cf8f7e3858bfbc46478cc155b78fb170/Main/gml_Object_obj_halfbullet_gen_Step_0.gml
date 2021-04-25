if (active == true && defined == 1)
{
    while (bullettime[curbullet] == bultimer)
    {
        bul = instance_create(0, 0, obj_halfbullet)
        if instance_exists(bul)
        {
            if (bulletpos[curbullet] == 0 || bulletpos[curbullet] == 1 || bulletpos[curbullet] == 2)
            {
                bul.x = xpos[bulletpos[curbullet]]
                bul.y = -60
                bul.hspeed = 0
                bul.vspeed = bulletspeed[curbullet]
            }
            if (bulletpos[curbullet] == 3 || bulletpos[curbullet] == 4 || bulletpos[curbullet] == 5)
            {
                bul.x = -48
                bul.y = ypos[bulletpos[curbullet]]
                bul.vspeed = 0
                bul.hspeed = bulletspeed[curbullet]
            }
            if (bulletpos[curbullet] == 6 || bulletpos[curbullet] == 7 || bulletpos[curbullet] == 8)
            {
                bul.x = 700
                bul.y = ypos[bulletpos[curbullet]]
                bul.vspeed = 0
                bul.hspeed = (-bulletspeed[curbullet])
            }
            bul.type = bullettype[curbullet]
            if (bul.type == 1)
                bul.image_blend = global.joycon_color[0]
            if (bul.type == 2)
                bul.image_blend = global.joycon_color[1]
        }
        curbullet += 1
        if (curbullet >= maxbullet)
        {
            break
            active = false
        }
    }
}
bultimer += 1
if (bultimer >= (maxtimer - 10))
    darkness = 0
if (bultimer >= maxtimer)
{
    with (obj_halfbullet)
        instance_destroy()
    instance_destroy()
}
if (shift_timer_max >= -1)
{
    shift_timer += 1
    if (shift_timer == shift_timer_max)
    {
        with (obj_hearthalf)
            vertical_ok = 1
    }
}
