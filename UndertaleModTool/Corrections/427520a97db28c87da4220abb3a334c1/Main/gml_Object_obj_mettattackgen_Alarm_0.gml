gil = global.idealborder[0]
gir = global.idealborder[1]
fff = 25
type = global.attacktype
if (type == 100)
    global.turntimer = 90
if (type == 1)
{
    i = 0
    repeat (7)
    {
        bl = instance_create((240 + (i * 24)), 120, obj_blackbox_pl)
        bl.vspeed = 4
        i += 1
    }
    i = 0
    repeat (7)
    {
        bl = instance_create((240 + (i * 24)), 80, obj_blackbox_pl)
        bl.vspeed = 4
        i += 1
    }
    i = 0
    repeat (7)
    {
        bl = instance_create((240 + (i * 24)), 40, obj_blackbox_pl)
        bl.vspeed = 4
        i += 1
    }
}
if (type == 2)
{
    i = 0
    repeat (9)
    {
        bl = instance_create((220 + (i * 24)), 120, obj_blackbox_pl)
        bl.vspeed = 4
        bl.sf = 20
        i += 1
    }
    i = 0
    repeat (9)
    {
        bl = instance_create((220 + (i * 24)), 60, obj_blackbox_pl)
        bl.vspeed = 4
        bl.sf = -20
        i += 1
    }
    i = 0
    repeat (9)
    {
        bl = instance_create((220 + (i * 24)), 0, obj_blackbox_pl)
        bl.sf = 20
        bl.vspeed = 4
        i += 1
    }
}
if (type == 3)
{
    i = 0
    repeat (2)
    {
        ll = instance_create(216, (200 - (i * 80)), obj_metthand_l)
        with (ll)
        {
            sf = 25
            sp = 15
            vspeed = 2
            yseg = 60
        }
        rr = instance_create(360, (160 - (i * 80)), obj_metthand_r)
        with (rr)
        {
            sf = 20
            sp = 12
            vspeed = 2
            yseg = 40
        }
        i += 1
    }
}
if (type == 4)
{
    i = 0
    repeat (4)
    {
        ll = instance_create((96 + (24 * i)), (180 - (i * 40)), obj_mettleg_l)
        with (ll)
        {
            sf = 60
            sp = 6
            vspeed = 3
        }
        rr = instance_create((312 + (24 * i)), (180 - (i * 40)), obj_mettleg_r)
        with (rr)
        {
            sf = 60
            sp = 6
            vspeed = 3
        }
        i += 1
    }
    i = 0
    repeat (4)
    {
        ll = instance_create((144 - (24 * i)), (20 - (i * 50)), obj_mettleg_l)
        with (ll)
        {
            sf = 60
            sp = 6
            vspeed = 3
        }
        rr = instance_create((360 - (24 * i)), (20 - (i * 50)), obj_mettleg_r)
        with (rr)
        {
            sf = 60
            sp = 6
            vspeed = 3
        }
        i += 1
    }
    obj_mettleg_r.on = 0
    obj_mettleg_l.on = 0
}
if (type == 5)
{
    i = 0
    ll = instance_create(202, 140, obj_mettleg_l)
    with (ll)
    {
        sf = 30
        sp = 6
        vspeed = 3
    }
    rr = instance_create(212, 40, obj_mettleg_r)
    with (rr)
    {
        sf = 30
        sp = 6
        vspeed = 3
    }
    rr = instance_create(212, 10, obj_mettleg_r)
    with (rr)
    {
        sf = 30
        sp = 6
        vspeed = 3
        c = 2
    }
    ll = instance_create(202, -100, obj_mettleg_l)
    with (ll)
    {
        sf = 30
        sp = 6
        vspeed = 3
    }
    ll = instance_create(202, -130, obj_mettleg_l)
    with (ll)
    {
        c = 2
        sf = 30
        sp = 6
        vspeed = 3
    }
    i = 0
    obj_mettleg_r.on = 1
    obj_mettleg_l.on = 1
}
if (type == 6)
{
    instance_create(240, 20, obj_blackbox_pl)
    instance_create(240, 120, obj_blackbox_pl)
    instance_create(264, 40, obj_blackbox_pl)
    instance_create(264, 100, obj_blackbox_pl)
    instance_create(264, 200, obj_blackbox_pl)
    instance_create(288, 0, obj_blackbox_pl)
    instance_create(288, 80, obj_blackbox_pl)
    instance_create(288, 140, obj_blackbox_pl)
    instance_create(312, 20, obj_blackbox_pl)
    instance_create(312, 60, obj_blackbox_pl)
    instance_create(312, 180, obj_blackbox_pl)
    instance_create(336, 40, obj_blackbox_pl)
    instance_create(336, 120, obj_blackbox_pl)
    instance_create(336, 220, obj_blackbox_pl)
    instance_create(360, 60, obj_blackbox_pl)
    instance_create(360, 160, obj_blackbox_pl)
    instance_create(384, 0, obj_blackbox_pl)
    instance_create(384, 80, obj_blackbox_pl)
    instance_create(384, 200, obj_blackbox_pl)
    instance_create(240, 60, obj_plusbomb)
    instance_create(240, 180, obj_plusbomb)
    instance_create(264, 0, obj_plusbomb)
    instance_create(264, 160, obj_plusbomb)
    instance_create(288, 40, obj_plusbomb)
    instance_create(312, 100, obj_plusbomb)
    instance_create(336, 60, obj_plusbomb)
    instance_create(336, 160, obj_plusbomb)
    instance_create(360, 0, obj_plusbomb)
    instance_create(360, 100, obj_plusbomb)
    instance_create(384, 40, obj_plusbomb)
    instance_create(384, 140, obj_plusbomb)
    with (obj_blackbox_pl)
    {
        vspeed = 2
        y -= 200
    }
    with (obj_plusbomb)
    {
        vspeed = 2
        y -= 200
    }
}
if (type == 7)
{
    j = 0
    repeat (16)
    {
        g[0] = choose(0, 1)
        g[1] = (g[0] + choose(1, 2))
        g[2] = (g[1] + choose(1, 2))
        g[3] = choose(5, 6)
        b = choose(0, 1, 2, 3)
        b2 = choose(0, 1, 2, 3)
        b3 = choose(0, 1, 2, 3)
        bm = 0
        for (i = 0; i < 4; i += 1)
        {
            if (b == i || b2 == i || (b3 == i && bm < 3))
            {
                bm += 1
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 45)) + random(20)), obj_plusbomb)
            }
            else
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 45)) + random(20)), obj_blackbox_pl)
        }
        j += 1
    }
    with (obj_plusbomb)
        vspeed += (random(0.5) - 0.2)
    with (obj_blackbox_pl)
        vspeed += (random(0.5) - 0.2)
}
if (type == 8)
{
    j = 0
    repeat (5)
    {
        instance_create(obj_heart.x, (-100 - (j * 240)), obj_plusbomb)
        instance_create((obj_heart.x + 20), ((-100 - (j * 240)) + 120), obj_plusbomb)
        j += 1
    }
    with (obj_plusbomb)
        vspeed += 2
}
if (type == 9)
{
    j = 0
    repeat (10)
    {
        instance_create((obj_heart.x + choose(0, 20)), (-100 - (j * 150)), obj_plusbomb)
        j += 1
    }
    with (obj_plusbomb)
        vspeed += 3
}
if (type == 10)
{
    j = 0
    repeat (8)
    {
        bm1 = choose(0, 1, 2, 3, 4)
        bm2 = choose(0, 1, 2, 3, 4)
        if (bm2 == bm1)
            bm2 += 1
        for (i = 0; i < 4; i += 1)
        {
            if (bm1 == i || bm2 == i || j == i)
                instance_create((global.idealborder[0] + (i * 25)), (0 - (j * 260)), obj_plusbomb)
            else
                instance_create((global.idealborder[0] + (i * 25)), (0 - (j * 260)), obj_blackbox_pl)
        }
        j += 1
    }
    obj_plusbomb.vspeed += 8
    obj_blackbox_pl.vspeed += 8
    instance_create(0, 0, obj_blackbox_rewinder)
}
if (type == 11)
{
    j = 0
    repeat (8)
    {
        for (i = 0; i < 3; i += 1)
            instance_create((global.idealborder[0] + (25 * i)), (0 - (j * 150)), obj_upbox_pl)
        j += 1
    }
    obj_upbox_pl.vspeed = 4
}
if (type == 12)
{
    j = 0
    repeat (3)
    {
        for (i = 0; i < 5; i += 1)
            instance_create((global.idealborder[0] + (25 * i)), (0 - (j * 200)), obj_upbox_new_pl)
        j += 1
    }
    j = 0
    repeat (8)
    {
        ch = choose(0, 1, 2, 3, 4)
        instance_create(((global.idealborder[0] + 10) + (ch * 25)), (0 - (j * 50)), obj_mettlightning_pl)
        ch2 = choose(0, 1, 2, 3, 4)
        instance_create(((global.idealborder[0] + 10) + (ch2 * 25)), (0 - (j * 75)), obj_mettlightning_pl)
        j += 1
    }
    obj_upbox_new_pl.vspeed = 4
}
if (type == 13)
{
    j = 0
    repeat (7)
    {
        for (i = 0; i < 3; i += 1)
            instance_create((global.idealborder[0] + random(150)), ((0 - (j * 120)) - (i * 40)), obj_mettfodder)
        j += 1
    }
}
if (type == 14)
{
    j = 0
    repeat (5)
    {
        for (i = 0; i < 3; i += 1)
            instance_create((global.idealborder[0] + random(150)), ((0 - (j * 150)) - (i * 50)), obj_mettfodder)
        j += 1
    }
}
if (type == 15)
{
    for (i = 0; i < 3; i += 1)
        instance_create((global.idealborder[0] + (25 * i)), 0, obj_dancemett)
    for (i = 0; i < 3; i += 1)
        instance_create((global.idealborder[1] - (25 * (i + 1))), -125, obj_dancemett)
    for (i = 0; i < 3; i += 1)
        instance_create((global.idealborder[0] + (25 * i)), -250, obj_dancemett)
}
if (type == 16)
{
    for (i = 0; i < 4; i += 1)
        instance_create((global.idealborder[0] + (25 * i)), 0, obj_dancemett)
    for (i = 0; i < 4; i += 1)
        instance_create((global.idealborder[1] - (25 * (i + 1))), -125, obj_dancemett)
    for (i = 0; i < 4; i += 1)
        instance_create((global.idealborder[0] + (25 * i)), -250, obj_dancemett)
}
if (type == 26)
{
    global.turntimer = 140
    j = 0
    repeat (4)
    {
        g[0] = choose(0, 1)
        g[1] = (g[0] + choose(1, 2))
        g[2] = (g[1] + choose(1, 2))
        g[3] = choose(5, 6)
        for (i = 0; i < 4; i += 1)
            instance_create((global.idealborder[0] + (g[i] * 25)), (((0 - (j * 95)) + random(60)) - 100), obj_blackbox_pl)
        j += 1
    }
    with (obj_blackbox_pl)
    {
        s = random(8)
        sf = 3
        sp = 4
        vspeed += 2
    }
}
if (type == 27)
{
    global.turntimer = 165
    i = 0
    repeat (2)
    {
        bb = instance_create((global.idealborder[0] + (i * 25)), 30, obj_blackbox_pl)
        bb.vspeed = 4
        i += 1
    }
    ll = instance_create((gil + 50), 30, obj_metthand_l)
    with (ll)
    {
        sf = 1
        sp = 1
        vspeed = 4
        yseg = 999
        ysegi = 999
    }
    i = 0
    repeat (2)
    {
        bb = instance_create(((gir - 25) - (i * 25)), -120, obj_blackbox_pl)
        bb.vspeed = 4
        i += 1
    }
    ll = instance_create((gir - 75), -120, obj_metthand_r)
    with (ll)
    {
        sf = 1
        sp = 1
        vspeed = 4
        yseg = 999
        ysegi = 999
    }
    ll = instance_create(gil, -250, obj_metthand_l)
    with (ll)
    {
        sf = 1
        sp = 1
        vspeed = 4
        ysegi = 70
    }
}
if (type == 28)
{
    global.turntimer = 160
    ll = instance_create(gil, -25, obj_metthand_l)
    with (ll)
    {
        sf = 4
        sp = 3
        vspeed = 4
        yseg = 999
        ysegi = 40
    }
    ll = instance_create(gir, -125, obj_metthand_r)
    with (ll)
    {
        sf = 4
        sp = 3
        vspeed = 4
        yseg = 999
        ysegi = 40
    }
    i = 0
    repeat (3)
    {
        bb = instance_create((gil + (i * 25)), -230, obj_blackbox_pl)
        bb.vspeed = 4
        i += 1
    }
    pl = instance_create((gil + 75), -230, obj_plusbomb)
    pl.vspeed = 4
    i += 1
    repeat (3)
    {
        bb = instance_create((gil + (i * 25)), -230, obj_blackbox_pl)
        bb.vspeed = 4
        i += 1
    }
}
if (type == 29)
{
    global.turntimer = 150
    j = 0
    repeat (5)
    {
        g[0] = choose(0, 1)
        g[1] = (g[0] + choose(1, 2))
        g[2] = (g[1] + choose(1, 2))
        g[3] = choose(5, 6)
        b = choose(0, 1, 2, 3)
        b2 = choose(0, 1, 2, 3)
        b3 = choose(0, 1, 2, 3)
        bm = 0
        for (i = 0; i < 4; i += 1)
        {
            if (b == i || (b2 == i && bm < 3))
            {
                bm += 1
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 85)) + random(50)), obj_plusbomb)
            }
            else if (b3 == i)
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 85)) + random(50)), obj_mettlightning_pl)
            else
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 85)) + random(50)), obj_blackbox_pl)
        }
        j += 1
    }
    with (obj_plusbomb)
        vspeed += (random(0.6) + 1)
    with (obj_blackbox_pl)
        vspeed += (random(0.6) + 1)
    with (obj_mettlightning_pl)
        vspeed += (random(0.6) + 1)
}
if (type == 99)
{
    ll = instance_create(gil, -25, obj_metthand_l)
    with (ll)
    {
        sf = 4
        sp = 3
        vspeed = 4
        yseg = 999
        ysegi = 40
    }
    ll = instance_create(gir, -150, obj_metthand_r)
    with (ll)
    {
        sf = 4
        sp = 3
        vspeed = 4
        yseg = 999
        ysegi = 40
    }
    j = 0
    repeat (5)
    {
        i = 0
        repeat (5)
        {
            lt = instance_create((global.idealborder[0] + ((gir - gil) / 2)), (0 - (j * 50)), obj_mettlightning_pl)
            lt.hspeed = (((-0.5 + (0.25 * i)) + random(0.2)) - 0.1)
            lt.vspeed = ((1.5 + random(0.2)) - 0.1)
            lt.friction = -0.05
            i += 1
        }
        j += 1
    }
}
if (type == 30)
{
    global.turntimer = 200
    i = 0
    ll = instance_create((gil - 45), 60, obj_mettleg_l)
    ll.on = 0
    ll.sf = 80
    ll = instance_create(gil, -80, obj_mettleg_r)
    ll.on = 0
    ll.sf = 80
    ll = instance_create((gil - 70), -240, obj_mettleg_l)
    ll.on = 1
    ll.sf = 30
    ll = instance_create((gil + 90), -240, obj_mettleg_r)
    ll.on = 1
    ll.sf = 30
    ll = instance_create((gil - 140), -380, obj_mettleg_l)
    ll.on = 1
    ll.sf = 30
    ll = instance_create((gil + 30), -380, obj_mettleg_r)
    ll.on = 1
    ll.sf = 30
    obj_mettleg_l.vspeed = 4
    obj_mettleg_r.vspeed = 4
}
if (type == 31)
{
    global.turntimer = 190
    i = 0
    repeat (4)
    {
        mf = instance_create(gil, (-50 - (i * 70)), obj_mettfodder)
        mf = instance_create((gir - 25), (-50 - (i * 70)), obj_mettfodder)
        obj_mettfodder.vspeed = 5
        obj_mettfodder.type = 2
        i += 1
    }
    i = 0
    repeat (5)
    {
        bl = instance_create(((gil + 25) + random(((gir - gil) - 75))), (-50 * i), obj_plusbomb)
        i += 1
    }
}
if (type == 32)
{
    global.turntimer = 210
    i = 0
    ll = instance_create((gil - 45), 60, obj_mettleg_l)
    ll.on = 0
    ll.sf = 60
    ll = instance_create((gil - 45), 20, obj_mettleg_l)
    ll.on = 0
    ll.sf = 60
    ll = instance_create((gil - 45), -20, obj_mettleg_l)
    ll.on = 0
    ll.sf = 60
    ll = instance_create(gil, -200, obj_mettleg_r)
    ll.on = 0
    ll.sf = 60
    ll = instance_create(gil, -240, obj_mettleg_r)
    ll.on = 0
    ll.sf = 60
    ll = instance_create(gil, -280, obj_mettleg_r)
    ll.on = 0
    ll.sf = 60
    obj_mettleg_l.vspeed = 3.3
    obj_mettleg_r.vspeed = 3.3
    ks = instance_create((gil + 75), -175, obj_blackbox_pl)
    ks.vspeed = 3.3
    ks.sf = 20
    ks.sp = 10
    ks = instance_create((gil + 75), -50, obj_blackbox_pl)
    ks.vspeed = 3.3
    ks.sf = 20
    ks.sp = 10
    ks = instance_create((gil + 75), -150, obj_blackbox_pl)
    ks.vspeed = 3.3
    ks.sf = 20
    ks.sp = 10
    ks = instance_create((gil + 75), -75, obj_blackbox_pl)
    ks.vspeed = 3.3
    ks.sf = 20
    ks.sp = 10
}
if (type == 33)
{
    global.turntimer = 190
    ll = instance_create(gil, -25, obj_metthand_l)
    with (ll)
    {
        sf = 20
        sp = 7
        vspeed = 3.5
        yseg = 999
        ysegi = 40
    }
    ll = instance_create(gir, -235, obj_metthand_r)
    with (ll)
    {
        sf = 20
        sp = 7
        vspeed = 3.5
        yseg = 999
        ysegi = 40
    }
    j = 0
    repeat (1)
    {
        i = 0
        repeat (5)
        {
            lt = instance_create(((gil + 25) + (i * 25)), -80, obj_mettfodder)
            lt.vspeed = 3.5
            lt.type = 2
            lt.early = 80
            i += 1
        }
        j += 1
    }
}
if (type == 34)
{
    global.turntimer = 90
    instance_create(0, 0, obj_essaystuff)
}
if (type == 35)
{
    global.turntimer = 600
    bossheart = instance_create(320, 162, obj_mettheart_1)
}
if (type == 36)
{
    global.turntimer = 250
    specialtimer = 1
    j = 0
    repeat (3)
    {
        for (i = 0; i < 3; i += 1)
        {
            km = instance_create((global.idealborder[0] + random(150)), ((0 - (j * 90)) - (i * 30)), obj_mettfodder)
        }
        j += 1
    }
    obj_mettfodder.vspeed += 1
}
if (type == 37)
{
    global.turntimer = 270
    ds = instance_create(308, (global.idealborder[2] - 10), obj_discoball_pl)
    obj_heart.x += 7
    obj_heart.y += 10
    ds.diff = 0
}
if (type == 38)
{
    global.turntimer = 193
    ds = instance_create(308, (global.idealborder[2] - 10), obj_discoball_pl)
    obj_heart.x += 7
    obj_heart.y += 10
    ds.diff = 1
    if (global.specialdam[0] > 1)
    {
        ds.diff = 0
        global.turntimer = 270
    }
}
if (type == 39)
{
    global.turntimer = 210
    specialtimer = 2
    j = 0
    oo = (global.idealborder[0] + 5)
    repeat (2)
    {
        bb = instance_create(oo, (-200 - (j * 260)), obj_plusbomb)
        bb.side = 1
        bb = instance_create((oo + 20), ((-200 - (j * 260)) + 120), obj_plusbomb)
        bb.side = 2
        j += 1
    }
    with (obj_plusbomb)
    {
    }
}
if (type == 40)
{
    global.turntimer = 160
    specialtimer = 2
    j = 0
    oo = (global.idealborder[0] + 5)
    repeat (3)
    {
        bb = instance_create(oo, (-200 - (j * 260)), obj_plusbomb)
        bb.side = 1
        bb = instance_create((oo + 20), ((-200 - (j * 260)) + 120), obj_plusbomb)
        bb.side = 2
        j += 1
    }
    with (obj_plusbomb)
    {
        if (global.specialdam[1] < 2)
        {
            vspeed += 2
            vspeed += 1
        }
    }
}
if (type == 41)
{
    global.turntimer = 110
    instance_create((global.idealborder[0] + 30), (global.idealborder[2] + 10), obj_happybreaktime)
}
if (type == 42)
{
    global.turntimer = 600
    bossheart = instance_create(320, 162, obj_mettheart_2)
}
if (type == 99)
{
    global.turntimer = 180
    i = 0
    ll = instance_create(202, 140, obj_mettleg_l)
    with (ll)
    {
        sf = 30
        sp = 6
        vspeed = 3
    }
    rr = instance_create(212, 40, obj_mettleg_r)
    with (rr)
    {
        sf = 30
        sp = 6
        vspeed = 3
    }
    rr = instance_create(212, 10, obj_mettleg_r)
    with (rr)
    {
        sf = 30
        sp = 6
        vspeed = 3
        c = 2
    }
    ll = instance_create(202, -100, obj_mettleg_l)
    with (ll)
    {
        sf = 30
        sp = 6
        vspeed = 3
    }
    ll = instance_create(202, -130, obj_mettleg_l)
    with (ll)
    {
        c = 2
        sf = 30
        sp = 6
        vspeed = 3
    }
    i = 0
    obj_mettleg_r.on = 1
    obj_mettleg_l.on = 1
}
if (type == 43)
{
    global.turntimer = 200
    j = 0
    repeat (8)
    {
        bm1 = choose(0, 1, 2, 3, 4)
        bm2 = choose(0, 1, 2, 3, 4)
        if (bm2 == bm1)
            bm2 += 1
        for (i = 0; i < 4; i += 1)
        {
            if (bm1 == i || bm2 == i || j == i)
                instance_create((global.idealborder[0] + (i * 25)), (0 - (j * 180)), obj_plusbomb)
            else
                instance_create((global.idealborder[0] + (i * 25)), (0 - (j * 180)), obj_blackbox_pl)
        }
        j += 1
    }
    obj_plusbomb.vspeed += 3
    obj_blackbox_pl.vspeed += 3
    rw = instance_create(0, 0, obj_blackbox_rewinder)
    rw.maxrw = 10
}
if (type == 44)
{
    global.turntimer = 220
    j = 0
    repeat (8)
    {
        bm1 = choose(0, 1, 2, 3, 4)
        bm2 = choose(0, 1, 2, 3, 4)
        if (bm2 == bm1)
            bm2 += 1
        for (i = 0; i < 4; i += 1)
        {
            if (bm1 == i || bm2 == i || j == i)
                instance_create((global.idealborder[0] + (i * 25)), (-60 - (j * 250)), obj_plusbomb)
            else
                instance_create((global.idealborder[0] + (i * 25)), (-60 - (j * 250)), obj_blackbox_pl)
        }
        j += 1
    }
    obj_plusbomb.vspeed += 6
    obj_blackbox_pl.vspeed += 6
    rw = instance_create(0, 0, obj_blackbox_rewinder)
    if (global.specialdam[2] > 2)
    {
        rw.maxrw = 10
        obj_plusbomb.vspeed -= 1
        obj_blackbox_pl.vspeed -= 1
    }
}
if (type == 45)
{
    global.turntimer = 165
    j = 0
    repeat (10)
    {
        g[0] = choose(0, 1)
        g[1] = (g[0] + choose(1, 2))
        g[2] = (g[1] + choose(1, 2))
        g[3] = choose(5, 6)
        b = choose(0, 1, 2, 3)
        b2 = choose(0, 1, 2, 3)
        b3 = choose(0, 1, 2, 3)
        bm = 0
        for (i = 0; i < 4; i += 1)
        {
            if (b == i || b2 == i || (b3 == i && bm < 3))
            {
                bm += 1
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 45)) + random(20)), obj_plusbomb)
            }
            else
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 45)) + random(20)), obj_blackbox_pl)
        }
        j += 1
    }
    with (obj_plusbomb)
        vspeed += ((1 + random(0.5)) - 0.2)
    with (obj_blackbox_pl)
        vspeed += ((1 + random(0.5)) - 0.2)
}
if (type == 46)
{
    global.turntimer = 140
    j = 0
    repeat (10)
    {
        g[0] = choose(0, 1)
        g[1] = (g[0] + choose(1, 2))
        g[2] = (g[1] + choose(1, 2))
        g[3] = choose(5, 6)
        b = choose(0, 1, 2, 3)
        b2 = choose(0, 1, 2, 3)
        b3 = choose(0, 1, 2, 3)
        bm = 0
        for (i = 0; i < 4; i += 1)
        {
            if (b == i || b2 == i || (b3 == i && bm < 3))
            {
                bm += 1
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 54)) + random(25)), obj_plusbomb)
            }
            else
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 54)) + random(25)), obj_blackbox_pl)
        }
        j += 1
    }
    with (obj_plusbomb)
        vspeed += ((2.5 + random(0.5)) - 0.2)
    with (obj_blackbox_pl)
        vspeed += ((2.5 + random(0.5)) - 0.2)
}
if (type == 47)
{
    global.turntimer = 200
    specialtimer = 2
    j = 0
    obj_heart.x = global.idealborder[0]
    repeat (6)
    {
        ch = choose(5, 25)
        bb = instance_create((obj_heart.x + ch), (-100 - (j * 180)), obj_plusbomb)
        if (ch == 5)
            bb.side = 1
        if (ch == 25)
            bb.side = 2
        j += 1
    }
    with (obj_plusbomb)
    {
        vspeed += 3
        if (global.specialdam[1] > 3)
            vspeed -= 1
    }
}
if (type == 48)
{
    global.turntimer = 700
    bossheart = instance_create(320, 162, obj_mettheart_3)
}
if (type == 49)
{
    global.turntimer = 800
    bossheart = instance_create(320, 232, obj_mettheart_4)
}
if (type == 50)
{
    global.turntimer = 170
    ll = instance_create(gil, -25, obj_metthand_l)
    with (ll)
    {
        s = random(300)
        sf = 50
        sp = 10
        vspeed = 4.5
        yseg = 999
        ysegi = 80
    }
    ll = instance_create(gir, -185, obj_metthand_r)
    with (ll)
    {
        s = random(300)
        sf = 50
        sp = 10
        vspeed = 4.5
        yseg = 999
        ysegi = 80
    }
    ll = instance_create(gil, -345, obj_metthand_l)
    with (ll)
    {
        s = random(300)
        sf = 50
        sp = 10
        vspeed = 4.5
        yseg = 999
        ysegi = 80
    }
    j = 0
}
if (type == 51)
{
    global.turntimer = 160
    ds = instance_create(308, global.idealborder[2], obj_discoball_pl)
    ds.diff = 2
}
if (type == 52)
{
    global.turntimer = 150
    j = 0
    repeat (9)
    {
        g[0] = choose(0, 1)
        g[1] = (g[0] + choose(1, 2))
        g[2] = (g[1] + choose(1, 2))
        g[3] = choose(5, 6)
        b = choose(0, 1, 2, 3)
        b2 = choose(0, 1, 2, 3)
        b3 = choose(0, 1, 2, 3)
        bm = 0
        for (i = 0; i < 4; i += 1)
        {
            if (b == i || b2 == i || (b3 == i && bm < 3))
            {
                bm += 1
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 45)) + random(25)), obj_plusbomb)
            }
            else
                instance_create((global.idealborder[0] + (g[i] * 25)), ((0 - (j * 45)) + random(25)), obj_blackbox_pl)
        }
        j += 1
    }
    with (obj_plusbomb)
        vspeed += ((1 + random(0.5)) - 0.2)
    with (obj_blackbox_pl)
        vspeed += ((1 + random(0.5)) - 0.2)
}
if (type == 53)
{
    global.turntimer = 200
    specialtimer = 2
    j = 0
    global.idealborder[1] = (global.idealborder[0] + 50)
    obj_heart.x = global.idealborder[0]
    oo = (global.idealborder[0] + 5)
    repeat (7)
    {
        ch = choose(0, 20)
        bb = instance_create((oo + ch), (-100 - (j * 170)), obj_plusbomb)
        if (ch == 0)
            bb.side = 1
        if (ch == 20)
            bb.side = 2
        j += 1
    }
    with (obj_plusbomb)
        vspeed += 3.5
}
if (type == 54)
{
    global.turntimer = 250
    specialtimer = 1
    j = 0
    repeat (4)
    {
        for (i = 0; i < 3; i += 1)
        {
            f = instance_create((global.idealborder[0] + random(150)), ((0 - (j * 120)) - (i * 40)), obj_mettfodder)
            f.vspeed += 0.5
            t = instance_create((global.idealborder[0] + random(150)), (((0 - (j * 120)) - (i * 40)) - 20), obj_mettlightning_pl)
            t.direction += (random(20) - 10)
            t.vspeed += 2
            t.friction = -0.02
        }
        j += 1
    }
}
if (type == 55)
{
    global.turntimer = 170
    i = 0
    repeat (4)
    {
        ll = instance_create((96 + (24 * i)), (180 - (i * 40)), obj_mettleg_l)
        with (ll)
        {
            sf = 60
            sp = 6
            vspeed = 2.8
        }
        rr = instance_create((312 + (24 * i)), (180 - (i * 40)), obj_mettleg_r)
        with (rr)
        {
            sf = 60
            sp = 6
            vspeed = 2.8
        }
        i += 1
    }
    i = 0
    repeat (3)
    {
        ll = instance_create((144 - (24 * i)), (20 - (i * 50)), obj_mettleg_l)
        with (ll)
        {
            sf = 60
            sp = 6
            vspeed = 2.8
        }
        rr = instance_create((360 - (24 * i)), (20 - (i * 50)), obj_mettleg_r)
        with (rr)
        {
            sf = 60
            sp = 6
            vspeed = 2.8
        }
        i += 1
    }
    obj_mettleg_r.on = 0
    obj_mettleg_l.on = 0
}
if (type == 56)
{
    global.turntimer = 260
    j = 0
    repeat (8)
    {
        bm1 = choose(0, 1, 2, 3, 4)
        bm2 = choose(0, 1, 2, 3, 4)
        if (bm2 == bm1)
            bm2 += 1
        for (i = 0; i < 4; i += 1)
        {
            if (bm1 == i || bm2 == i || j == i)
                instance_create((global.idealborder[0] + (i * 25)), (-100 - (j * 240)), obj_plusbomb)
            else
                instance_create((global.idealborder[0] + (i * 25)), (-100 - (j * 240)), obj_blackbox_pl)
        }
        j += 1
    }
    obj_plusbomb.vspeed += 5
    obj_blackbox_pl.vspeed += 5
    rw = instance_create(0, 0, obj_blackbox_rewinder)
    rw.rew = -40
}
