if (EVENT == 1)
{
    snd_free_all()
    global.currentsong[0] = snd_init("AUDIO_DRONE.ogg")
    global.currentsong[1] = mus_loop(global.currentsong[0])
    global.typer = 667
    global.fc = 0
    global.msg[0] = scr_84_get_lang_string("DEVICE_FAILURE_slash_Step_0_gml_10_0")
    EVENT = 2
    W = instance_create(70, 80, obj_writer)
    if (global.tempflag[3] >= 1)
    {
        with (obj_writer)
            instance_destroy()
    }
}
if (EVENT == 0)
    EVENT = 1
if (EVENT == 2 && (!instance_exists(obj_writer)))
{
    JA_XOFF = 0
    if (global.lang == "ja")
        JA_XOFF = 44
    global.msg[0] = scr_84_get_lang_string("DEVICE_FAILURE_slash_Step_0_gml_28_0")
    if (global.tempflag[3] >= 1)
    {
        JA_XOFF = 0
        global.msg[0] = scr_84_get_lang_string("DEVICE_FAILURE_slash_Step_0_gml_32_0")
    }
    EVENT = 3
    alarm[4] = 30
    if (global.tempflag[3] >= 1)
        alarm[4] = 15
    W = instance_create((40 + JA_XOFF), 80, obj_writer)
}
if (EVENT == 4)
{
    choice = instance_create(100, 120, DEVICE_CHOICE)
    EVENT = 5
}
if (EVENT == 5)
{
    if (global.choice == 0)
    {
        with (obj_writer)
            instance_destroy()
        EVENT = 6
    }
    if (global.choice == 1)
    {
        with (obj_writer)
            instance_destroy()
        EVENT = 26
    }
}
if (EVENT == 6)
{
    snd_free_all()
    global.flag[6] = 1
    global.msg[0] = scr_84_get_lang_string("DEVICE_FAILURE_slash_Step_0_gml_68_0")
    W = instance_create(50, 80, obj_writer)
    EVENT = 7
    alarm[4] = 30
    if (global.tempflag[3] >= 1)
    {
        with (obj_writer)
            instance_destroy()
        alarm[4] = 1
    }
}
if (EVENT == 8)
{
    WHITEFADE = 1
    FADEUP = 0.01
    EVENT = 9
    alarm[4] = 120
    if (global.tempflag[3] >= 1)
    {
        FADEUP = 0.03
        alarm[4] = 45
    }
    global.tempflag[3] += 1
}
if (EVENT == 10)
{
    scr_windowcaption(scr_84_get_lang_string("DEVICE_FAILURE_slash_Step_0_gml_95_0"))
    scr_tempload()
    EVENT = 11
}
if (EVENT == 26)
{
    snd_free_all()
    global.msg[0] = scr_84_get_lang_string("DEVICE_FAILURE_slash_Step_0_gml_103_0")
    EVENT = 27
    W = instance_create(60, 80, obj_writer)
}
if (EVENT == 27 && (!instance_exists(obj_writer)))
{
    global.currentsong[0] = snd_init("AUDIO_DARKNESS.ogg")
    global.currentsong[1] = mus_play(global.currentsong[0])
    EVENT = 28
    DARK_WAIT = 0
}
if (EVENT == 28)
{
    DARK_WAIT += 1
    if (DARK_WAIT >= 2040)
        ossafe_game_end()
    if (!snd_is_playing(global.currentsong[1]))
        ossafe_game_end()
    if (os_type == os_ps4 || os_type == os_switch)
    {
        if (DARK_WAIT >= 90 && (!restart))
        {
            for (var i = 0; i < array_length_1d(gamepad_controls); i++)
            {
                if gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gamepad_controls[i])
                {
                    if (gamepad_controls[i] == global.button0 || gamepad_controls[i] == global.button1 || gamepad_controls[i] == global.button2 || gamepad_controls[i] == 32775)
                    {
                        mus_volume(global.currentsong[1], 0, 80)
                        restart = 1
                        break
                    }
                }
            }
        }
        if restart
        {
            restart_timer++
            if (restart_timer >= 100)
                ossafe_game_end()
        }
    }
}
if (EVENT >= 0 && EVENT <= 4)
{
    if button2_h()
    {
        with (obj_writer)
        {
            if (pos < (length - 3))
                pos += 2
            if (specfade <= 0.9)
                specfade -= 0.1
            if (rate <= 1)
                rate = 1
        }
    }
}
