if (con == 1)
{
    global.msc = 0
    global.typer = 104
    global.msg[0] = "Greetings./"
    global.msg[1] = (("I^2 am " + global.charname) + "./%%")
    instance_create(220, 320, OBJ_WRITER)
    con = 2
}
if (con == 2 && instance_exists(OBJ_WRITER) == 0)
{
    caster_loop(ch_sfx1, 1, 1)
    con = 3
    alarm[4] = 60
}
if (con == 4)
{
    global.msc = 0
    global.typer = 104
    global.msg[0] = "Thank you./"
    global.msg[1] = "Your power awakened&me from death./"
    global.msg[2] = 'My "human soul."/'
    global.msg[3] = 'My "determination."/'
    global.msg[4] = "They were not mine^1,&but YOURS./"
    global.msg[5] = "At first^1, I&was so confused./"
    global.msg[6] = "Our plan had failed^1,&hadn't it?/"
    global.msg[7] = "Why was I brought&back to life?/"
    global.msg[8] = ".../"
    global.msg[9] = "You./"
    global.msg[10] = "With your guidance./"
    global.msg[11] = "I realized the purpose&of my reincarnation./"
    global.msg[12] = "Power./"
    global.msg[13] = "Together^1, we eradicated&the enemy and became&strong./"
    global.msg[14] = "HP. ATK. DEF.&GOLD. EXP. LV./"
    global.msg[15] = "Every time a number&increases^1, that&feeling.../"
    global.msg[16] = "That's me./"
    global.msg[17] = (('"' + global.charname) + '."/')
    global.msg[18] = "Now./"
    global.msg[19] = "Now, we have reached&the absolute./"
    global.msg[20] = "There is nothing&left for us here./"
    global.msg[21] = "Let us erase this&pointless world^1, and&move on to the next./%%"
    if file_exists("system_information_963")
    {
        global.msg[0] = (('"' + global.charname) + '."/')
        global.msg[1] = "The demon that comes&when people call&its name./"
        global.msg[2] = "It doesn't matter when./"
        global.msg[3] = "It doesn't matter where./"
        global.msg[4] = "Time after time,&I will appear./"
        global.msg[5] = "And, with your help./"
        global.msg[6] = "We will eradicate the&enemy and become&strong./"
        global.msg[7] = "HP. ATK. DEF.&GOLD. EXP. LV./"
        global.msg[8] = "Every time a number&increases^1, that&feeling.../"
        global.msg[9] = "That's me./"
        global.msg[10] = (('"' + global.charname) + '."/')
        global.msg[11] = ".../"
        global.msg[12] = "But./"
        global.msg[13] = "You and I are not&the same^1, are we?/"
        global.msg[14] = "This SOUL resonates&with a strange&feeling./"
        global.msg[15] = "There is a reason&you continue to&recreate this world./"
        global.msg[16] = "There is a reason&you continue to&destroy it./"
        global.msg[17] = "You./"
        global.msg[18] = "You are wracked with&a perverted&sentimentality./"
        global.msg[19] = "Hmm./"
        global.msg[20] = "I cannot understand&these feelings&any more./"
        global.msg[21] = "Despite this./"
        global.msg[22] = "I feel obligated to&suggest./"
        global.msg[23] = "Should you choose to&create this world&once more./"
        global.msg[24] = "Another path would&be better suited./"
        global.msg[25] = "Now, partner./"
        global.msg[26] = "Let us send this&world back into the&abyss./%%"
    }
    instance_create(150, 320, OBJ_WRITER)
    con = 5
}
if (con == 5 && instance_exists(OBJ_WRITER) == 0)
{
    con = 6
    alarm[4] = 30
}
if (con == 7)
{
    con = 8
    choicer = 1
}
if (con == 20)
{
    global.msc = 0
    global.typer = 104
    global.msg[0] = "Right^1. You are a&great partner./"
    global.msg[1] = "We'll be together&forever^1, won't we?/%%"
    con = 22
    instance_create(150, 320, OBJ_WRITER)
}
if (con == 22 && instance_exists(OBJ_WRITER))
{
    if (OBJ_WRITER.stringno == 1)
        sprite_index = spr_truechara_weird
}
if (con == 22 && instance_exists(OBJ_WRITER) == 0)
    con = 60
if (con == 30)
{
    global.msc = 0
    global.typer = 104
    global.msg[0] = "No...?/"
    global.msg[1] = "Hmm.../"
    global.msg[2] = "How curious./"
    global.msg[3] = "You must have&misunderstood./"
    global.msg[4] = "SINCE WHEN WERE YOU&THE ONE IN CONTROL?/%%"
    if file_exists("system_information_963")
    {
        global.msg[0] = "No...?/"
        global.msg[1] = "Hmm...&This feeling you have./"
        global.msg[2] = "This is what I&spoke of./"
        global.msg[3] = "Unfortunately,&regarding this.../"
        global.msg[4] = "YOU MADE YOUR CHOICE&LONG AGO./%%"
    }
    con = 31
    instance_create(150, 320, OBJ_WRITER)
}
if (con == 31 && instance_exists(OBJ_WRITER))
{
    if (OBJ_WRITER.stringno == 4)
        sprite_index = spr_truechara_weird
}
if (con == 31 && instance_exists(OBJ_WRITER) == 0)
    con = 40
if (con == 40)
{
    caster_stop(-3)
    caster_play(ch_sfx2, 1, 0.95)
    sprite_index = spr_truechara_laugh
    image_speed = 0.5
    con = 41
    flashred = 1
    alarm[4] = 120
    wx = 0
    wy = 0
    if (global.osflavor == 1)
    {
        window_set_fullscreen(false)
        window_set_caption(" ")
        window_center()
        wx = window_get_x()
        wy = window_get_y()
    }
}
if (con == 41)
{
    x = (((room_width / 2) + random(4)) - random(4))
    y = (((room_height / 2) + random(4)) - random(4))
    image_xscale += 0.08
    image_yscale += 0.08
    if (global.osflavor == 1)
        window_set_position(((wx + random(((redsiner / 4) + 4))) - random(((redsiner / 4) + 4))), ((wy + random(((redsiner / 4) + 4))) - random(((redsiner / 4) + 4))))
}
if (con == 42)
{
    con = 60
    if (global.osflavor == 1)
        window_center()
    flashred = 0
}
if (con == 60)
{
    caster_free(-3)
    snd_play(snd_laz)
    image_speed = 0
    image_index = 0
    sprite_index = spr_strike
    image_xscale = 5
    image_yscale = 5
    y = ((room_height / 2) - (sprite_height / 2))
    x = ((room_width / 2) - (sprite_width / 2))
    image_speed = 0.1
    con = 61
}
if (con == 61)
{
    if (image_index >= 5.5)
    {
        visible = false
        con = 62
        alarm[4] = 40
    }
}
if (con == 63)
{
    snd_play(snd_damage)
    instance_create(0, 0, obj_gameshake)
    con = 64
}
