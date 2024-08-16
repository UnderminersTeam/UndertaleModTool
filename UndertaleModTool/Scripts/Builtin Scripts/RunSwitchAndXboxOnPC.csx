// Sets up NXTALE to run well on PC.
using System.Linq;
using UndertaleModLib.Models;

EnsureDataLoaded();

if (Data.GeneralInfo.Name.Content != "NXTALE")
{
    ScriptError("This script can only be used with\nThe Nintendo Switch and Xbox One version of Undertale.", "Not NXTALE");
    return;
}

bool isXbox = Data.Rooms.ByName("room_xbox_engagement") is not null;

if (isXbox)
{
    // Fix initialization code not running
    ReplaceTextInGML("gml_Object_obj_time_Create_0", @"
if (os_type == os_xboxone)
{
    script_execute(SCR_GAMESTART, 0, 0, 0, 0, 0)", @"
if (true)
{
    script_execute(SCR_GAMESTART, 0, 0, 0, 0, 0)");

    var obj_time = Data.GameObjects.ByName("obj_time");
    var otherEvents = obj_time.Events[(int)EventType.Other];
    var gameStartEvent = otherEvents.FirstOrDefault(x => x.EventSubtype == (int)EventSubtypeOther.GameStart);
    if (gameStartEvent is not null)
        otherEvents.Remove(gameStartEvent);
}

// Disables interpolation. Only necessary for NX version.
Data.GeneralInfo.Info &= ~(UndertaleGeneralInfo.InfoFlags.Interpolate);

// Use new gamepad functions, because the compatibility ones are completely broken

// use NX routine for joypad detection
ReplaceTextInGML("gml_Object_obj_time_Create_0", @"
if (global.osflavor >= 4)
{
    i = 0
    while (i < gamepad_get_device_count())", @"
if (true)
{
    i = 0
    while (i < gamepad_get_device_count())");
ReplaceTextInGML("gml_Object_obj_time_Create_0", @"
    else
        j_ch = 1", @"
    else if (global.osflavor >= 4)
        j_ch = 1");
string os_switch = "os_switch" + (!isXbox ? "_beta" : "");
ReplaceTextInGML("gml_Object_obj_time_Step_1", @"
if (global.osflavor <= 2)
{
    if (jt == 0)
    {
        if (j_ch != 2)
        {
            if joystick_exists(1)
                j_ch = 1
            else if (j_ch == 1)
                j_ch = 0
        }
    }
    if (jt == 4)
    {
        if (j_ch != 1)
        {
            if joystick_exists(2)
                j_ch = 2
            else if (j_ch == 2)
                j_ch = 0
        }
    }
    jt += 1
    if (jt >= 8)
        jt = 0
}
else if (os_type == " + os_switch + ")", "");
ReplaceTextInGML("gml_Object_obj_time_Step_1", @"
    if (j_ch > 0)
        missing_controller_timeout = 0
    else if (missing_controller_timeout == 0)
        missing_controller_timeout = current_time + 2000
    else if (current_time >= missing_controller_timeout)
    {
        if (switch_controller_support_show() == 0)
        {
            j_ch = switch_controller_support_get_selected_id() + 1
            missing_controller_timeout = 0
        }
    }", @"
    if (os_type == " + os_switch + @") {
        if (j_ch > 0)
            missing_controller_timeout = 0
        else if (missing_controller_timeout == 0)
            missing_controller_timeout = current_time + 2000
        else if (current_time >= missing_controller_timeout)
        {
            if (switch_controller_support_show() == 0)
            {
                j_ch = switch_controller_support_get_selected_id() + 1
                missing_controller_timeout = 0
            }
        }
    }");

// Use Xbox default buttons
ReplaceTextInGML("gml_Object_obj_time_Create_0", @"
global.button0 = 2
global.button1 = 1
global.button2 = 4", @"
global.button0 = gp_face1
global.button1 = gp_face2
global.button2 = gp_face4");
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", @"
    global.button0 = 2
    global.button1 = 1
    global.button2 = 4", @"
    global.button0 = gp_face1
    global.button1 = gp_face2
    global.button2 = gp_face4");

// axis check
ReplaceTextInGML("gml_Object_obj_time_Step_1", @"
    if (global.osflavor >= 4)
    {
        if (gamepad_button_check", @"
    if (true)
    {
        if (gamepad_button_check");

// button check
ReplaceTextInGML("gml_Script_control_update", "else if (obj_time.j_ch > 0)", "else if (false)");
ReplaceTextInGML("gml_Script_control_update", "global.osflavor >= 4", "obj_time.j_ch > 0");

/*
// show button config
ReplaceTextInGML("gml_Object_obj_settingsmenu_Draw_0", "global.osflavor <= 2", "false");
ReplaceTextInGML("gml_Object_obj_settingsmenu_Draw_0", "global.osflavor >= 4", "true");
*/

// Fix Joystick Menu
/*ReplaceTextInGML("gml_Script___joystick_2_gamepad", @"if (argument0 == 2)
    return global.__jstick_pad2;
else
    return global.__jstick_pad1;", @"show_debug_message(""Debug : __joystick_2_gamepad was called"")
if (argument0 != obj_time.j_ch)
    show_debug_message(""Debug : passed in value other than j_ch while calling joystick functions"")
return obj_time.j_ch - 1;");*/
ReplaceTextInGML("gml_Object_obj_joypadmenu_Create_0", "joystick_has_pov(obj_time.j_ch)", "true");
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", "joystick_has_pov(obj_time.j_ch)", "true");
// gamepad_button_count(obj_time.j_ch - 1) might work better but I'm not sure
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", "joystick_buttons(obj_time.j_ch)", "11 + 1");
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", "joystick_check_button(obj_time.j_ch, i)", "gamepad_button_check(obj_time.j_ch - 1, 32769 + i)");
for (var i = 0; i < 3; i++)
{
    ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", $"global.button{i} = i", $"global.button{i} = 32769 + i");
    ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", $"string_hash_to_newline(global.button{i})", $"string(global.button{i} - 32769)");
}

// Use Xbox or PS4 button sprites
if (isXbox)
    ReplaceTextInGML("gml_Script_scr_getbuttonsprite", "os_type == os_xboxone", "true");
else
    ReplaceTextInGML("gml_Script_scr_getbuttonsprite", "os_type == os_ps4", "true");

// Allow gamepad input for left/right heart halfs
foreach (string half in new List<string>{"l", "r"})
{
    foreach (string side in new List<string>{"u", "d", "l", "r"})
    {
        ReplaceTextInGML($"gml_Script_scr_heart{half}_hold{side}", "global.osflavor <= 2 && ", "");
        ReplaceTextInGML($"gml_Script_scr_heart{half}_hold{side}", "global.osflavor >= 4 && ", "");
    }
}

if (ScriptQuestion("Enable the Dog Shrine?"))
{
    // This enables the Dog Shrine's entrance.
    if (isXbox)
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Create_0", "global.osflavor == 4 || global.osflavor == 5 || global.osflavor == 6", "true");
    else
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Create_0", "global.osflavor == 4 || global.osflavor == 5", "true");

    if (isXbox)
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Alarm_2", "(global.osflavor == 4 || global.osflavor == 5 || global.osflavor == 6) && ", "");
    else
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Alarm_2", "(global.osflavor == 4 || global.osflavor == 5) && ", "");

    if (isXbox)
        ReplaceTextInGML("gml_Object_obj_doorXmusicfade_Alarm_2", "if (global.osflavor == 6)", "else");
    // if in NX version, the door will get you into the ruined dog shrine

    // Enable donation box trash
    if (isXbox)
        ReplaceTextInGML("gml_Object_obj_npc_room_Create_0", "global.osflavor != 4 && global.osflavor != 6", "false");
    else
        ReplaceTextInGML("gml_Object_obj_npc_room_Create_0", "(global.osflavor != 4 && global.osflavor != 5) || ", "");
}

// Done.
ScriptMessage(@"NXTALE Enabler by Kneesnap
Xbox and gamepad fixes by Dobby233Liu

NOTE: You're not done yet!

For Switch version:
Copy 'mus_mewmew.ogg', 'mus_sfx_dogseal.ogg', and 'DELTARUNE.exe'
into the folder you will save this game archive.
Use the DELTARUNE runner to run Undertale.

For Xbox version:
Copy 'mus_mewmew.ogg', 'mus_sfx_dogseal.ogg' and 'mus_dogshrine_xbox.ogg'
into the folder you will save this game archive.
Use a 2.2.2-2.2.5 runner to run Undertale.

You might want to reset joystick settings.");