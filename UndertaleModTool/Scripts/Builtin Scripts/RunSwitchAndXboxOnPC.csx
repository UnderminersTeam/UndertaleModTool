// Sets up NXTALE to run well on PC.
using System.Linq;
using UndertaleModLib.Models;

EnsureDataLoaded();

if (Data.GeneralInfo.Name.Content != "NXTALE")
{
    ScriptError("""
        This script can only be used with the Nintendo Switch
        and Xbox One version of Undertale.
        """, "Not NXTALE");
    return;
}

bool isXbox = Data.Rooms.ByName("room_xbox_engagement") is not null;

if (isXbox)
{
    // Fix crucial initialization code not running.
    ReplaceTextInGML("gml_Object_obj_time_Create_0", """
    if (os_type == os_xboxone)
    {
        script_execute(SCR_GAMESTART, 0, 0, 0, 0, 0)
    """, """
    if (true)
    {
        script_execute(SCR_GAMESTART, 0, 0, 0, 0, 0)
    """);

    var obj_time = Data.GameObjects.ByName("obj_time");
    var otherEvents = obj_time.Events[(int)EventType.Other];
    var gameStartEvent = otherEvents.FirstOrDefault(x => x.EventSubtype == (int)EventSubtypeOther.GameStart);
    if (gameStartEvent is not null)
        otherEvents.Remove(gameStartEvent);
}

// Disables interpolation. Only necessary for the NX version.
Data.GeneralInfo.Info &= ~UndertaleGeneralInfo.InfoFlags.Interpolate;

// Use console gamepad code paths to use the new gamepad functions,
// because the compatibility ones are completely broken.

// Use the NX code path for gamepad detection.
ReplaceTextInGML("gml_Object_obj_time_Create_0", "if (global.osflavor >= 4)", "if (true)");
// Make the game not pretend a controller is there initially on PC?
// Why did they do this?
ReplaceTextInGML("gml_Object_obj_time_Create_0", """
    else
        j_ch = 1
""", """
    else if (global.osflavor >= 4)
        j_ch = 1
""");
// The os_switch constant changed after the NX version.
string os_switch = "os_switch" + (!isXbox ? "_beta" : "");
ReplaceTextInGML("gml_Object_obj_time_Step_1", "global.osflavor <= 2", "false");
ReplaceTextInGML("gml_Object_obj_time_Step_1", $"os_type == {os_switch}", "true");
ReplaceTextInGML("gml_Object_obj_time_Step_1", """
    if (j_ch > 0)
        missing_controller_timeout = 0
""", $"""
    if (os_type == {os_switch})
        if (j_ch > 0)
            missing_controller_timeout = 0
""");

// Use the Xbox default buttons on PC.
// We have to map the joystick_* button constants to the gamepad_*
// button constants anyway.
ReplaceTextInGML("gml_Object_obj_time_Create_0", """
global.button0 = 2
global.button1 = 1
global.button2 = 4
""", """
global.button0 = gp_face1
global.button1 = gp_face2
global.button2 = gp_face4
""");
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", """
    global.button0 = 2
    global.button1 = 1
    global.button2 = 4
""", """
    global.button0 = gp_face1
    global.button1 = gp_face2
    global.button2 = gp_face4
""");

// Use the console path for the axis check.
ReplaceTextInGML("gml_Object_obj_time_Step_1", """
    if (global.osflavor >= 4)
    {
        if (gamepad_button_check
""", """
    if (true)
    {
        if (gamepad_button_check
""");

// Use the console path for the button checks.
ReplaceTextInGML("gml_Script_control_update", "else if (obj_time.j_ch > 0)", "else if (false)");
ReplaceTextInGML("gml_Script_control_update", "global.osflavor >= 4", "obj_time.j_ch > 0");

// Make the Joystick Menu use the new gamepad functions.
ReplaceTextInGML("gml_Object_obj_joypadmenu_Create_0", "joystick_has_pov(obj_time.j_ch)", "true");
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", "joystick_has_pov(obj_time.j_ch)", "true");
// TODO: gamepad_button_count(obj_time.j_ch - 1) might work better?
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", "joystick_buttons(obj_time.j_ch)", "11 + 1");
ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", "joystick_check_button(obj_time.j_ch, i)", "gamepad_button_check(obj_time.j_ch - 1, gp_face1 + i)");
for (var i = 0; i < 3; i++)
{
    ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", $"global.button{i} = i", $"global.button{i} = gp_face1 + i");
    ReplaceTextInGML("gml_Object_obj_joypadmenu_Draw_0", $"string_hash_to_newline(global.button{i})", $"string(global.button{i} - gp_face1)");
}

// Make the Mad Mew Mew heart halves accept both kinds of input.
foreach (char half in new[] {'l', 'r'})
{
    foreach (char side in new[] {'u', 'd', 'l', 'r'})
    {
        ReplaceTextInGML($"gml_Script_scr_heart{half}_hold{side}", "global.osflavor <= 2 && ", "");
        ReplaceTextInGML($"gml_Script_scr_heart{half}_hold{side}", "global.osflavor >= 4 && ", "");
    }
}

if (ScriptQuestion("Enable the Dog Shrine?"))
{
    if (isXbox)
    {
        // This enables the entrance to the Dog Shrine.
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Create_0", "global.osflavor == 4 || global.osflavor == 5 || global.osflavor == 6", "true");
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Alarm_2", "(global.osflavor == 4 || global.osflavor == 5 || global.osflavor == 6) && ", "");
        // This patch is unnecessary in the NX version, as the door will get you into the ruined Dog Shrine.
        ReplaceTextInGML("gml_Object_obj_doorXmusicfade_Alarm_2", "if (global.osflavor == 6)", "else");

        // Enable the donation box trash in Waterfall.
        ReplaceTextInGML("gml_Object_obj_npc_room_Create_0", "global.osflavor != 4 && global.osflavor != 6", "false");
    }
    else
    {
        // This enables the entrance to the Dog Shrine.
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Create_0", "global.osflavor == 4 || global.osflavor == 5", "true");
        ReplaceTextInGML("gml_Object_obj_kitchenchecker_Alarm_2", "(global.osflavor == 4 || global.osflavor == 5) && ", "");

        // Enable the donation box trash in Waterfall.
        ReplaceTextInGML("gml_Object_obj_npc_room_Create_0", "(global.osflavor != 4 && global.osflavor != 5) || ", "");
    }
}

string requiredFiles = !isXbox ? """
Copy "mus_mewmew.ogg", "mus_sfx_dogseal.ogg",
and "DELTARUNE.exe" (from the 2018 Ch1 demo)
to the folder you will save this data file to.
Then use the Deltarune runner to run the game.
""" : """
Copy "mus_mewmew.ogg", "mus_sfx_dogseal.ogg",
"mus_dogshrine_xbox.ogg", and a GMS2 2.2.2-2.2.5
runner to the folder you will save this data file to.
Then use the new runner to run the game.
""";
ScriptMessage($"""
NXTALE Enabler by Kneesnap
Xbox and gamepad fixes by Dobby233Liu

NOTE: You're not done yet!

{requiredFiles}

Due to gamepad code changes, you might want to reset the
gamepad settings.
""");