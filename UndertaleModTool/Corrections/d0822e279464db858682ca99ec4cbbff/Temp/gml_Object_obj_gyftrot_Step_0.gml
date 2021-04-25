if (global.mnfight == 3)
    attacked = 0
if (alarm[5] > 0)
{
    if (global.monster[0] == true)
    {
        if (global.monsterinstance[0].alarm[5] > alarm[5])
            alarm[5] = global.monsterinstance[0].alarm[5]
    }
    if (global.monster[1] == true)
    {
        if (global.monsterinstance[1].alarm[5] > alarm[5])
            alarm[5] = global.monsterinstance[1].alarm[5]
    }
    if (global.monster[2] == true)
    {
        if (global.monsterinstance[2].alarm[5] > alarm[5])
            alarm[5] = global.monsterinstance[2].alarm[5]
    }
}
if (global.mnfight == 1)
{
    if (talked == 0)
    {
        alarm[5] = 110
        alarm[6] = 1
        talked = 1
        global.heard = 0
    }
}
if keyboard_multicheck_pressed(13)
{
    if (alarm[5] > 5 && obj_lborder.x == global.idealborder[0] && alarm[6] < 0)
        alarm[5] = 2
}
if (global.hurtanim[myself] == 1)
{
    shudder = 16
    alarm[3] = global.damagetimer
    global.hurtanim[myself] = 3
}
if (global.hurtanim[myself] == 2)
{
    global.monsterhp[myself] -= takedamage
    with (dmgwriter)
        alarm[2] = 15
    if (global.monsterhp[myself] >= 1)
    {
        mypart1 = instance_create(x, y, part1)
        mypart2 = instance_create(x, y, part2)
        global.hurtanim[myself] = 0
        image_index = 0
        global.myfight = 0
        global.mnfight = 1
    }
    else
    {
        global.myfight = 0
        global.mnfight = 1
        killed = 1
        instance_destroy()
    }
}
if (global.hurtanim[myself] == 5)
{
    global.damage = 0
    instance_create(((x + (sprite_width / 2)) - 48), (y - 24), obj_dmgwriter)
    with (obj_dmgwriter)
        alarm[2] = 30
    global.myfight = 0
    global.mnfight = 1
    global.hurtanim[myself] = 0
}
if (global.mnfight == 2)
{
    if (attacked == 0)
    {
        pop = scr_monstersum()
        if instance_exists(obj_jerry)
        {
            if (obj_jerry.ditch == 0)
                pop -= 1
        }
        global.turntimer = 120
        if (mercymod > 90)
            global.turntimer = -2
        global.firingrate = 5
        if (global.hardmode == 1)
            global.firingrate = 3
        if (pop == 3)
            global.firingrate = global.firingrate * 2.4
        if (pop == 2)
            global.firingrate = global.firingrate * 1.7
        if (mycommand >= 0 && mycommand <= 60)
        {
            gen = instance_create(x, y, obj_gyftgen)
            gen.bullettype = 1
        }
        else
        {
            gen = instance_create(x, y, obj_giftgen)
            gen.bullettype = 0
        }
        gen.myself = myself
        if (mycommand >= 0)
            global.msg[0] = "* Gyftrot laments its lack of&  hands."
        if (mycommand >= 25)
            global.msg[0] = "* Gyftrot eyes you with&  suspicion."
        if (mycommand >= 40)
            global.msg[0] = "* Gyftrot distrusts your&  youthful demeanor."
        if (mycommand >= 60)
            global.msg[0] = "* Ah^1, the scent of fresh&  pine needles."
        if (mycommand >= 80)
            global.msg[0] = "* Gyftrot tries vainly to&  remove its decorations."
        if (giftgiven == 1)
            global.msg[0] = "* Gyftrot pretends to refuse&  your gift."
        if (giftgiven == 2)
            global.msg[0] = "* Gyftrot politely accepts&  your gift."
        if (googly == 1)
            global.msg[0] = "* Gyftrot stumbles blindly."
        if (itemgone == 1 || itemgone == 2)
            global.msg[0] = "* Gyftrot is slightly less&  irritated."
        if (itemgone == 3)
            global.msg[0] = "* Gyftrot's problems have&  been taken away."
        if (betray == 1)
            global.msg[0] = "* Gyftrot looks disappointed."
        if (global.monsterhp[myself] < 30)
            global.msg[0] = "* Gyftrot's antlers tremble."
        attacked = 1
    }
}
if (global.myfight == 2)
{
    if (whatiheard != -1)
    {
        if (global.heard == 0)
        {
            if (whatiheard == 0)
            {
                global.msc = 0
                global.msg[0] = (((("* GYFTROT " + string(global.monsteratk[myself])) + " ATK ") + string(global.monsterdef[myself])) + ' DEF&* Some teens "decorated" it as&  a prank./^')
                OBJ_WRITER.halt = 3
                iii = instance_create(global.idealborder[0], global.idealborder[2], OBJ_WRITER)
                with (iii)
                    halt = 0
            }
            if (whatiheard == 1)
            {
                global.msc = 0
                if (itemgone < 3)
                {
                    if (gift[itemgone] == 0)
                        global.msg[0] = "* You remove the striped cane&  that says " + chr(34) + "I use this tiny&  cane to walk" + chr(34) + " on it./^"
                    if (gift[itemgone] == 1)
                        global.msg[0] = "* You remove the box of&  non-dog-related raisins./^"
                    if (gift[itemgone] == 2)
                        global.msg[0] = "* You remove the lenticular&  bookmark of a smug teen&  winking./^"
                    if (gift[itemgone] == 3)
                        global.msg[0] = "* You remove the barbed wire&  made of pipe cleaners./^"
                    if (gift[itemgone] == 4)
                        global.msg[0] = "* You remove a childhood&  photograph of Snowdrake and&  his parent./^"
                    if (gift[itemgone] == 5)
                        global.msg[0] = "* You remove a small^1, confused&  dog./^"
                    if (gift[itemgone] == 6)
                        global.msg[0] = "* You remove a stocking filled&  with chicken nuggets./^"
                    if (gift[itemgone] == 7)
                        global.msg[0] = "* You remove the shirt that says& 'I'm with stupid' and points&  inward./^"
                }
                if (itemgone == 3)
                {
                    global.msg[0] = "* You try to undecorate...?/^"
                    mercymod = 180
                }
                if (googly == 1)
                    global.msg[0] = "* You remove the googly eyes./^"
                OBJ_WRITER.halt = 3
                iii = instance_create(global.idealborder[0], global.idealborder[2], OBJ_WRITER)
                with (iii)
                    halt = 0
                if (googly == 1)
                {
                    googly = 0
                    ung = 1
                    with (mypart5)
                        instance_destroy()
                }
                else if (itemgone < 3)
                {
                    if (itemgone == 2)
                    {
                        with (mypart8)
                            instance_destroy()
                        itemgone = 3
                    }
                    if (itemgone == 1)
                    {
                        with (mypart4)
                            instance_destroy()
                        itemgone = 2
                    }
                    if (itemgone == 0)
                    {
                        with (mypart3)
                            instance_destroy()
                        itemgone = 1
                    }
                }
                if (mercymod < 150)
                {
                    if (itemgone > 0 && mercymod < 100)
                        mercymod = 10
                    if (itemgone > 2)
                    {
                        mercymod = 160
                        global.flag[138] = 1
                    }
                }
            }
            if (whatiheard == 3)
            {
                global.msc = 0
                if (googly == 0)
                {
                    global.msg[0] = "* You add some googly eyes&  you found on the ground./^"
                    googly = 1
                    mypart5 = instance_create(x, y, part3)
                    mypart5.gift = 8
                }
                else
                    global.msg[0] = "* You can't improve on&  perfection./^"
                OBJ_WRITER.halt = 3
                iii = instance_create(global.idealborder[0], global.idealborder[2], OBJ_WRITER)
                with (iii)
                    halt = 0
            }
            if (whatiheard == 4)
            {
                global.msc = 0
                if (giftgiven == 0 && googly == 0 && itemgone > 0 && betray == 0)
                {
                    mypart6 = instance_create(x, y, part3)
                    mypart6.gift = 9
                    if (global.gold == 0)
                    {
                        global.msg[0] = "* You give the cheapest gift&  of all..^1.&* Friendship./^"
                        giftgiven = 2
                        mercymod = 140
                        global.goldreward[myself] += 50
                    }
                    if (global.gold > 0)
                    {
                        if (global.gold >= 35)
                        {
                            global.msg[0] = "* You give 35 G because&  you can't think of an&  appropriate gift./^"
                            global.gold -= 35
                        }
                        else
                        {
                            global.gold = 0
                            global.msg[0] = "* You give your remaining&  money because you can't&  think of a better gift./^"
                        }
                        giftgiven = 1
                        mercymod = 140
                    }
                }
                else
                {
                    if (googly == 1 || betray == 1 || itemgone == 0)
                        global.msg[0] = "* Gyftrot refuses your gift./^"
                    if (giftgiven == 1)
                        global.msg[0] = "* Hey now^1.&* You aren't made of money./^"
                    if (giftgiven == 2)
                        global.msg[0] = "* Hey now^1.&* You aren't made of friendship./^"
                }
                OBJ_WRITER.halt = 3
                iii = instance_create(global.idealborder[0], global.idealborder[2], OBJ_WRITER)
                with (iii)
                    halt = 0
            }
        }
        global.heard = 1
    }
}
if (global.myfight == 4)
{
    if (global.mercyuse == 0)
    {
        script_execute(scr_mercystandard)
        if (mercy < 0)
            instance_destroy()
    }
}
if (mercymod == 222 && instance_exists(OBJ_WRITER) == 0)
{
    script_execute(scr_mercystandard)
    if (mercy < 0)
        instance_destroy()
}
