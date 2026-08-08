using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UndertaleModLib.Util;

EnsureDataLoaded();

ScriptMessage(@"Enables borders for UNDERTALE and DELTARUNE
by TerraFrost, built off of the original scripts by
krzys_h, Jockeholm, and Grossley.");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true,
    MainThreadAction = MainThreadAction
};

var LTS = false;
string internalName = Data.GeneralInfo.Name.Content;
string displayName = Data.GeneralInfo.DisplayName.Content;
string bordersPath = Path.Join(Path.GetDirectoryName(ScriptPath), "Borders");
string bordersPathDTShared = Path.Join(Path.GetDirectoryName(ScriptPath), "Borders/Deltarune/Shared");

// Code Replacement
if (internalName == "NXTALE") // UNDERTALE v1.11
{
    // Show the border settings on PC.
    importGroup.QueueFindReplace("gml_Object_obj_settingsmenu_Draw_0", @"if (global.osflavor <= 2)
{
    menu_max = 2;
    if (obj_time.j_ch == 0)
    {
        menu_max = 1;
    }
}", "");
    importGroup.QueueFindReplace("gml_Object_obj_settingsmenu_Draw_0", "if (global.osflavor >= 4)", "if (global.osflavor >= 1)");

    importGroup.QueueFindReplace("gml_Script_scr_draw_background_ps4", "if (os_type == os_ps4 || os_type == os_switch_beta)", "if (os_type == os_ps4 || os_type == os_switch_beta || os_type == os_windows)");

    importGroup.QueueFindReplace("gml_Script_scr_draw_screen_border", "if (os_type == os_ps4 || os_type == os_switch_beta)", "if (os_type == os_ps4 || os_type == os_switch_beta || os_type == os_windows)");

    importGroup.QueueFindReplace("gml_Script_scr_draw_screen_border", "if (os_type == os_switch_beta)", "if (os_type == os_switch_beta || os_type == os_windows)");

    // Enable the dog border unlock
    importGroup.QueueFindReplace("gml_Object_obj_rarependant_Step_1", "if (global.osflavor == 5)", "if (global.osflavor >= 1)");

    // Load borders
    importGroup.QueueFindReplace("gml_Object_obj_time_Step_1", "scr_enable_screen_border(global.osflavor >= 4)", "scr_enable_screen_border(global.osflavor >= 1)");

    // Resize the game window to account for the borders
    //Data.GeneralInfo.DefaultWindowWidth = 1920; // This setup prevents the game from starting??
    //Data.GeneralInfo.DefaultWindowHeight = 1080;
    importGroup.QueueAppend("gml_Script_SCR_GAMESTART", "window_set_size(960, 540);");

    importGroup.QueueFindReplace("gml_Object_obj_time_Draw_77", @"else
{
    global.window_xofs = 0;
    global.window_yofs = 0;
}", "");
    importGroup.QueueFindReplace("gml_Object_obj_time_Draw_77", "if (global.osflavor >= 3)", "if (true)");
    importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "if (global.osflavor >= 3)", "if (global.osflavor >= 1)");
    importGroup.QueueFindReplace("gml_Object_obj_time_Draw_76", "else if (global.osflavor >= 4)", "else if (global.osflavor >= 1)");

    importGroup.Import();

    ScriptMessage("Borders loaded and enabled for UNDERTALE v1.11!");
    return;
}
else if (internalName.StartsWith("UNDERTALE"))
{
    UndertaleModLib.Compiler.CodeImportGroup importGroupUT = new(Data)
    {
        MainThreadAction = MainThreadAction
    };

    // Change os_type == 14 checks in scr_draw_screen_border to always pass
    importGroupUT.QueueFindReplace("gml_Script_scr_draw_screen_border", "os_type == os_psvita", "0");
    importGroupUT.QueueFindReplace("gml_Script_scr_draw_screen_border", "os_type == os_ps4", "1");

    // Same for the code that calls it
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Draw_77", "global.osflavor >= 3", "1");

    // Remove checks from obj_time creation event
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Create_0", "os_type == os_psvita", "0");
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Create_0", "os_type == os_ps4", "1");
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Create_0", "global.osflavor >= 4", "1");
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Create_0", "global.osflavor >= 3", "1");

    // Now patch out the check for the window scale, make it always be true
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Draw_76", "global.osflavor >= 4", "1");
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Draw_76", "os_type == os_switch_beta", "1");
    //Attempt border display fix in gml_Object_obj_time_Draw_76

    // Patch out the OS checks for gml_Script_scr_draw_background_ps4, make PS Vita always false, and PS4 always true, simplifying code.
    importGroupUT.QueueFindReplace("gml_Script_scr_draw_background_ps4", "os_type == os_psvita", "0");
    importGroupUT.QueueFindReplace("gml_Script_scr_draw_background_ps4", "os_type == os_ps4", "1");

    // Now, patch the settings menu!
    importGroupUT.QueueFindReplace("gml_Object_obj_settingsmenu_Draw_0", "obj_time.j_ch > 0", "0");
    importGroupUT.QueueFindReplace("gml_Object_obj_settingsmenu_Draw_0", "global.osflavor <= 2", "0");
    importGroupUT.QueueFindReplace("gml_Object_obj_settingsmenu_Draw_0", "global.osflavor >= 4", "1");

    // Remove code not applicable (PS Vita, Windows, <=2) and make some code always true (global.osflavor >= 4)
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Step_1", "os_type == os_psvita", "0");
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Step_1", "global.osflavor <= 2", "0");
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Step_1", "global.osflavor == 1", "0");
    importGroupUT.QueueFindReplace("gml_Object_obj_time_Step_1", "global.osflavor >= 4", "1");

    // Also resize the window so that the border can be seen without going fullscreen
    Data.Functions.EnsureDefined("window_set_size", Data.Strings);
    importGroupUT.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Create_0"), "window_set_size(960, 540);");

    importGroupUT.Import();

    // Set the location to search for the borders
    bordersPath = Path.Join(Path.GetDirectoryName(ScriptPath), "Borders/Undertale");
}
else if (!Data.IsVersionAtLeast(2, 3) && (displayName == "SURVEY_PROGRAM" || displayName == "DELTARUNE Chapter 1")) // DELTARUNE Chapter 1, before 1&2 demo
{
    ScriptError("The Chapter 1 SURVEY_PROGRAM demo is not supported.");
    return;
}
else if (displayName == "DELTARUNE Chapter 1&2") // DELTARUNE (Chapter 1&2 demo prior to LTS)
{
    // Chapter 1
    // Patch the OS check in gml_Object_obj_time_Draw_77 to check for Windows
    importGroup.QueueFindReplace("gml_Object_obj_time_ch1_Draw_77", "if (os_type == os_switch || os_type == os_ps4)", "if (os_type == os_switch || os_type == os_ps4 || os_type == os_windows)");

    // Patch the check in obj_time creation event to always be true
    importGroup.QueueFindReplace("gml_Object_obj_time_ch1_Create_0", "if (global.is_console)", "");
    importGroup.QueueFindReplace("gml_Object_obj_time_ch1_Create_0", "scr_enable_screen_border_ch1(global.is_console);", "scr_enable_screen_border_ch1(1);");

    // Patch the checks in the main menu to always be true
    importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_ch1_Step_0", "if (global.is_console)", "");
    importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_ch1_Other_15", "if (!global.is_console)", "if (!(global.is_console || !global.is_console))");

    // Patch the check for the window scale to check for Windows
    importGroup.QueueFindReplace("gml_Object_obj_time_ch1_Draw_76", "if (os_type == os_switch || os_type == os_ps4)", "if (os_type == os_switch || os_type == os_ps4 || os_type == os_windows)");

    // Patch gml_Script_scr_draw_background_ps4 to check for Windows
    importGroup.QueueFindReplace("gml_GlobalScript_scr_draw_background_ps4_ch1", "if (os_type == os_ps4 || os_type == os_switch)", "if (os_type == os_ps4 || os_type == os_switch || os_type == os_windows)");

    // Now, patch the settings menu!
    importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_ch1_Step_0", "global.is_console", "global.is_console || !global.is_console");
    importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_ch1_Draw_0", "global.is_console", "global.is_console || !global.is_console");
    importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_ch1_Draw_0", "var _selectXPos = ((global.lang == \"ja\" && global.is_console) || !global.is_console) ? (xx + 385) : (xx + 430);", "var _selectXPos = (global.lang == \"ja\" && global.is_console) ? (xx + 385) : (xx + 430);");

    // Also resize the window so that the border can be seen without going fullscreen
    Data.Functions.EnsureDefined("window_set_size", Data.Strings);
    importGroup.QueueAppend("gml_GlobalScript_scr_gamestart_ch1", "window_set_size(960, 540);");
    importGroup.QueueFindReplace("gml_Object_obj_time_ch1_Draw_77", @"else
{
    global.window_xofs = 0;
    global.window_yofs = 0;
}", "");
    importGroup.QueueFindReplace("gml_Object_obj_time_ch1_Create_0", "if (display_width > (640 * _ww) && display_height > (480 * _ww))", "if (display_width > (960 * _ww) && display_height > (540 * _ww))");
    importGroup.QueueFindReplace("gml_Object_obj_time_ch1_Create_0", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");

    // Chapter 2
    // Patch the OS check in gml_Object_obj_time_Draw_75 to check for Windows
    importGroup.QueueFindReplace("gml_Object_obj_time_Draw_75", "if (global.is_console)", "");

    // Patch the OS check in gml_Object_obj_initializer2_Step_0 to always be true
    importGroup.QueueFindReplace("gml_Object_obj_initializer2_Step_0", "    if (global.is_console)", "");

    // Patch the check in obj_time creation event to always be true
    importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "if (global.is_console)", "");
    importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "scr_enable_screen_border(global.is_console);", "scr_enable_screen_border(1);");

    // Patch the check in gml_Object_obj_time_Step_1 to not check for console
    importGroup.QueueFindReplace("gml_Object_obj_time_Step_1", "if (global.is_console && os_is_paused())", "if (os_is_paused())");

    // Patch the checks in the main menu to always be true
    importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Create_0", "if (global.is_console)", "");
    importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Other_15", "if (!global.is_console)", "if (!(global.is_console || !global.is_console))");
    importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Step_0", @"if (!global.is_console)
                        {
                            ini_close();
                        }
                        else", "");

    // Patch the check for the window scale to check for Windows
    importGroup.QueueFindReplace("gml_Object_obj_border_controller_Draw_76", "if (os_type == os_switch || os_type == os_ps4)", "if (os_type == os_switch || os_type == os_ps4 || os_type == os_windows)");

    // Patch gml_Script_scr_draw_background_ps4 to check for Windows
    importGroup.QueueFindReplace("gml_GlobalScript_scr_draw_background_ps4", "if (os_type == os_ps4 || os_type == os_switch)", "if (os_type == os_ps4 || os_type == os_switch || os_type == os_windows)");

    // Now, patch the settings menu!
    importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Step_0", "global.is_console", "global.is_console || !global.is_console");
    importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Draw_0", "global.is_console", "global.is_console || !global.is_console");
    importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Draw_0", "var _selectXPos = ((global.lang == \"ja\" && global.is_console) || !global.is_console) ? (xx + 385) : (xx + 430);", "var _selectXPos = (global.lang == \"ja\" && global.is_console) ? (xx + 385) : (xx + 430);");

    // Also resize the window so that the border can be seen without going fullscreen
    Data.Functions.EnsureDefined("window_set_size", Data.Strings);
    importGroup.QueueAppend("gml_GlobalScript_scr_gamestart", "window_set_size(960, 540);");
    importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "if (display_width > (640 * _ww) && display_height > (480 * _ww))", "if (display_width > (960 * _ww) && display_height > (540 * _ww))");
    importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");
    importGroup.QueueFindReplace("gml_Object_obj_CHAPTER_SELECT_Create_0", "if (display_width > (640 * _ww) && display_height > (480 * _ww))", "if (display_width > (960 * _ww) && display_height > (540 * _ww))");
    importGroup.QueueFindReplace("gml_Object_obj_CHAPTER_SELECT_Create_0", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");
    importGroup.QueueFindReplace("gml_Object_obj_time_Alarm_1", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");
    importGroup.QueueFindReplace("gml_GlobalScript_scr_attack_override", "window_set_size(640 * __screensize, 480 * __screensize)", "window_set_size(960 * __screensize, 540 * __screensize)");
    importGroup.QueueFindReplace("gml_GlobalScript_scr_attack_override", "window_set_size(640, 480)", "window_set_size(960, 540)");
    importGroup.QueueFindReplace("gml_Object_obj_bullettester_Step_0", "window_set_size(640, 480)", "window_set_size(960, 540)");

    importGroup.Import();

    // Set the location to search for the borders
    bordersPath = Path.Join(Path.GetDirectoryName(ScriptPath), "Borders/Deltarune/Chapter 2");
}
else if (displayName.StartsWith("DELTARUNE Chapter")) // DELTARUNE (LTS demo AND full game)
{
    if (Data.GameObjects.ByName("obj_event_manager") is null)
    {
        // Chapter 1&2 LTS demo toggle
        LTS = true;
    }
    if (displayName == "DELTARUNE Chapter 1") // Chapter 1
    {
        // Patch the OS check in gml_Object_obj_time_Draw_77 to check for Windows
        importGroup.QueueFindReplace("gml_Object_obj_time_Draw_77", $"if ({(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps4 || os_type == os_ps5)", $"if ({(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps4 || os_type == os_ps5 || os_type == os_windows)");

        // Patch the check in obj_time creation event to always be true
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "if (global.is_console)", "");
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "scr_enable_screen_border(global.is_console);", "scr_enable_screen_border(1);");

        // Patch the checks in the main menu to always be true
        importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Step_0", "if (global.is_console)", "");
        importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Other_15", "if (!global.is_console)", "if (!(global.is_console || !global.is_console))");

        // Patch the check for the window scale to check for Windows
        importGroup.QueueFindReplace("gml_Object_obj_time_Draw_76", $"if ({(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps4 || os_type == os_ps5)", $"if ({(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps4 || os_type == os_ps5 || os_type == os_windows)");

        // Patch gml_Script_scr_draw_background_ps4 to check for Windows
        importGroup.QueueFindReplace("gml_GlobalScript_scr_draw_background_ps4", $"if (os_type == os_ps4 || os_type == os_ps5 || {(LTS ? "os_type == os_switch" : "scr_is_switch_os()")})", $"if (os_type == os_ps4 || os_type == os_ps5 || {(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_windows)");

        // Now, patch the settings menu!
        importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Step_0", "global.is_console", "global.is_console || !global.is_console");
        importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Draw_0", "global.is_console", "global.is_console || !global.is_console");
        importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Draw_0", "var _selectXPos = ((global.lang == \"ja\" && global.is_console) || !global.is_console) ? (xx + 385) : (xx + 430);", "var _selectXPos = (global.lang == \"ja\" && global.is_console) ? (xx + 385) : (xx + 430);");

        // Also resize the window so that the border can be seen without going fullscreen
        Data.Functions.EnsureDefined("window_set_size", Data.Strings);
        importGroup.QueueAppend("gml_GlobalScript_scr_gamestart", "window_set_size(960, 540);");
        importGroup.QueueFindReplace("gml_Object_obj_time_Draw_77", @"else
{
    global.window_xofs = 0;
    global.window_yofs = 0;
}", "");
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "if (display_width > (640 * _ww) && display_height > (480 * _ww))", "if (display_width > (960 * _ww) && display_height > (540 * _ww))");
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");
        importGroup.QueueFindReplace("gml_Object_obj_time_Draw_75", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");

        importGroup.Import();
    }
    if (displayName != "DELTARUNE Chapter 1") // Chapters 2-5
    {
        // Patch the OS check in gml_Object_obj_time_Draw_75 to check for Windows
        importGroup.QueueFindReplace("gml_Object_obj_time_Draw_75", "if (global.is_console)", "");

        // Patch the OS check in gml_Object_obj_initializer2_Step_0 to always be true
        importGroup.QueueFindReplace("gml_Object_obj_initializer2_Step_0", "    if (global.is_console)", "");

        // Patch the check in obj_time creation event to always be true
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "if (global.is_console)", "");
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "scr_enable_screen_border(global.is_console);", "scr_enable_screen_border(1);");

        // Patch the check in gml_Object_obj_time_Step_1 to not check for console
        importGroup.QueueFindReplace("gml_Object_obj_time_Step_1", "if (global.is_console && os_is_paused())", "if (os_is_paused())");

        // Patch the checks in the main menu to always be true
        importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Other_15", "if (!global.is_console)", "if (!(global.is_console || !global.is_console))");
        importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Step_0", @"if (!global.is_console)
                        {
                            ini_close();
                        }
                        else", "");

        // Patch the check for the window scale to check for Windows
        importGroup.QueueFindReplace("gml_Object_obj_border_controller_Draw_76", $"if ({(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps4 || os_type == os_ps5)", $"if ({(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps4 || os_type == os_ps5 || os_type == os_windows)");

        // Patch gml_Script_scr_draw_background_ps4 to check for Windows
        importGroup.QueueFindReplace("gml_GlobalScript_scr_draw_background_ps4", $"if (os_type == os_ps4 || {(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps5)", $"if (os_type == os_ps4 || {(LTS ? "os_type == os_switch" : "scr_is_switch_os()")} || os_type == os_ps5 || os_type == os_windows)");

        // Now, patch the settings menu!
        importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Step_0", "global.is_console", "global.is_console || !global.is_console");
        importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Draw_0", "global.is_console", "global.is_console || !global.is_console");
        importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Draw_0", "var _selectXPos = ((global.lang == \"ja\" && global.is_console) || !global.is_console) ? (xx + 385) : (xx + 430);", "var _selectXPos = (global.lang == \"ja\" && global.is_console) ? (xx + 385) : (xx + 430);");

        // Also resize the window so that the border can be seen without going fullscreen
        Data.Functions.EnsureDefined("window_set_size", Data.Strings);
        importGroup.QueueAppend("gml_GlobalScript_scr_gamestart", "window_set_size(960, 540);");
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "if (display_width > (640 * _ww) && display_height > (480 * _ww))", "if (display_width > (960 * _ww) && display_height > (540 * _ww))");
        importGroup.QueueFindReplace("gml_Object_obj_time_Create_0", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");
        importGroup.QueueFindReplace("gml_Object_obj_time_Alarm_1", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");
        importGroup.QueueFindReplace("gml_GlobalScript_scr_attack_override", "window_set_size(640 * __screensize, 480 * __screensize)", "window_set_size(960 * __screensize, 540 * __screensize)");
        importGroup.QueueFindReplace("gml_GlobalScript_scr_attack_override", "window_set_size(640, 480)", "window_set_size(960, 540)");

        // Chapter changes
        if (displayName != "DELTARUNE Chapter 2") // NOT Chapter 2
        {
            // Patch the checks in gml_Object_obj_time_Step_0 to not check for console
            importGroup.QueueFindReplace("gml_Object_obj_time_Step_0", "if (global.is_console && sunkus_kb_check_pressed(vk_pause))", "if (sunkus_kb_check_pressed(vk_pause))");
            importGroup.QueueFindReplace("gml_Object_obj_time_Step_0", "if (global.is_console)", "");

            // Patch the check at the end of the load script to not check for console
            importGroup.QueueFindReplace("gml_GlobalScript_scr_load", "if (global.is_console)\n    {\n        global.tempflag[95] = 1;\n    }", "global.tempflag[95] = 1;");
        }

        if (displayName != "DELTARUNE Chapter 5") // NOT Chapter 5
        {
            // Patch a few more window resize checks
            importGroup.QueueFindReplace("gml_Object_obj_time_Draw_77", "window_set_size(640 * window_size_multiplier, 480 * window_size_multiplier)", "window_set_size(960 * window_size_multiplier, 540 * window_size_multiplier)");
            importGroup.QueueFindReplace("gml_Object_obj_bullettester_Step_0", "window_set_size(640, 480)", "window_set_size(960, 540)");

            if (displayName != "DELTARUNE Chapter 2") // NOT Chapter 2
            {
                // Patch one more check in the main menu to always be true
                importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Step_0", "if (global.is_console)", "");
            }
        }

        if (displayName == "DELTARUNE Chapter 2")
        {
            // Patch one more check in the main menu to always be true
            importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Create_0", "if (global.is_console)", "");

            if (LTS == false) // NOT the LTS demo
            {
                // Patch one more check in the main menu to always be true
                importGroup.QueueFindReplace("gml_Object_DEVICE_MENU_Step_0", "if (global.is_console)\n                        {\n                            global.screen_border_id = ini_read_string(\"BORDER\", \"TYPE\", \"Dynamic\");\n                            var _disable_border = global.screen_border_id == \"None\" || global.screen_border_id == \"なし\";\n                            scr_enable_screen_border(!_disable_border);\n                        }", "if (global.is_console || !global.is_console)\n                        {\n                            global.screen_border_id = ini_read_string(\"BORDER\", \"TYPE\", \"Dynamic\");\n                            var _disable_border = global.screen_border_id == \"None\" || global.screen_border_id == \"なし\";\n                            scr_enable_screen_border(!_disable_border);\n                        }");
            }
        }

        if (displayName == "DELTARUNE Chapter 3")
        {
            // Patch out the OS check for hiding borders when entering room_ch3_gacharoom_unknown
            importGroup.QueueFindReplace("gml_Object_obj_room_ranking_b_Step_0", "if (gacha_con == 121 && global.is_console)", "if (gacha_con == 121)");
        }

        if (displayName == "DELTARUNE Chapter 4")
        {
            // Patch out the OS check for border code in gml_Object_obj_room_castle_area_1_Step_0
            importGroup.QueueFindReplace("gml_Object_obj_room_castle_area_1_Step_0", "if (global.is_console)", "");
        }

        if (displayName == "DELTARUNE Chapter 5")
        {
            // Patch the check in scr_save to always be true
            importGroup.QueueFindReplace("gml_GlobalScript_scr_save", "if (global.is_console)", "");

            // Patch out the OS check for changing the border during the Pink fight
            importGroup.QueueFindReplace("gml_Object_obj_date_controller_Step_0", "if (global.is_console)", "");
        }

        importGroup.Import();

        // Set the location to search for the borders
        bordersPath = displayName switch
        {
            "DELTARUNE Chapter 2" => Path.Join(Path.GetDirectoryName(ScriptPath), "Borders/Deltarune/Chapter 2"),
            "DELTARUNE Chapter 3" => Path.Join(Path.GetDirectoryName(ScriptPath), "Borders/Deltarune/Chapter 3"),
            "DELTARUNE Chapter 4" => Path.Join(Path.GetDirectoryName(ScriptPath), "Borders/Deltarune/Chapter 4"),
            "DELTARUNE Chapter 5" => Path.Join(Path.GetDirectoryName(ScriptPath), "Borders/Deltarune/Chapter 5")
        };
    }
}
else
{
    ScriptError("Unsupported game version.");
    return;
}

Dictionary<string, UndertaleEmbeddedTexture> textures = new();
int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

// Load border textures
Action<string[]> LoadBorders = (params string[] borderPaths) =>
{
    for (int i = 0; i < borderPaths.Length; i++)
    {
        // If failed to find border path, throw error telling what path it searched
        if (!Directory.Exists(borderPaths[i]))
        {
            string displayedPath = borderPaths[i].Replace('\\', '/'); // Uniformly use "/" in the file path
            throw new ScriptException("Border textures not found in \"" + displayedPath + "\" for some reason???");
        }

        foreach (var path in Directory.EnumerateFiles(borderPaths[i]))
        {
            UndertaleEmbeddedTexture newtex = new UndertaleEmbeddedTexture();
            newtex.Name = new UndertaleString($"Texture {++lastTextPage}");
            newtex.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(path)); // Possibly other formats than PNG in the future, but no Undertale versions currently have them
            Data.EmbeddedTextures.Add(newtex);
            textures.Add(Path.GetFileName(path), newtex);
        }
    }
};

// Create texture fragments and assign them to existing (but empty) sprites (or backgrounds if your UNDERTALE)
Action<string, UndertaleEmbeddedTexture, ushort, ushort, ushort, ushort> AssignBorder = (name, tex, x, y, width, height) =>
{
    var spr = Data.Sprites.ByName(name);
    var bg = Data.Backgrounds.ByName(name);
    if (spr is null && bg is null)
    {
        // The anime, dog, and casino borders do not exist on PC yet
        return;
    }

    UndertaleTexturePageItem tpag = new UndertaleTexturePageItem();
    tpag.Name = new UndertaleString($"PageItem {++lastTextPageItem}");
    tpag.SourceX = x; tpag.SourceY = y; tpag.SourceWidth = width; tpag.SourceHeight = height;
    tpag.TargetX = 0; tpag.TargetY = 0; tpag.TargetWidth = width; tpag.TargetHeight = height;
    tpag.BoundingWidth = width; tpag.BoundingHeight = height;
    tpag.TexturePage = tex;
    Data.TexturePageItems.Add(tpag);

    if (spr is not null)
    {
        UndertaleSprite.TextureEntry texentry = new();
        texentry.Texture = tpag;
        spr.Textures[0] = texentry;
    }
    else if (bg is not null)
        bg.Texture = tpag;
};

switch (displayName)
{
    case "UNDERTALE":
        {
            LoadBorders(new[] { bordersPath });
            AssignBorder("bg_border_anime_1080",      textures["bg_border_anime.png"],   0, 0, 1920, 1080);
            AssignBorder("bg_border_castle_1080",     textures["bg_border_castle.png"],  0, 0, 1920, 1080);
            AssignBorder("bg_border_dog_1080",        textures["bg_border_dog.png"],     0, 0, 1920, 1080);
            AssignBorder("bg_border_casino_1080",     textures["bg_border_casino.png"],  0, 0, 1920, 1080);
            AssignBorder("bg_border_fire_1080",       textures["bg_border_fire.png"],    0, 0, 1920, 1080);
            AssignBorder("bg_border_line_1080",       textures["bg_border_line.png"],    0, 0, 1920, 1080);
            AssignBorder("bg_border_rad_1080",        textures["bg_border_rad.png"],     0, 0, 1920, 1080);
            AssignBorder("bg_border_ruins_1080",      textures["bg_border_ruins.png"],   0, 0, 1920, 1080);
            AssignBorder("bg_border_sepia_1080",      textures["bg_border_sepia.png"],   114, 38, 1920, 1080);
            AssignBorder("bg_border_sepia_1080_1a",   textures["bg_border_sepia.png"],   2, 1750, 137, 137);
            AssignBorder("bg_border_sepia_1080_1b",   textures["bg_border_sepia.png"],   2, 1606, 137, 137);
            AssignBorder("bg_border_sepia_1080_2a",   textures["bg_border_sepia.png"],   2, 562, 92, 87);
            AssignBorder("bg_border_sepia_1080_2b",   textures["bg_border_sepia.png"],   2, 470, 92, 87);
            AssignBorder("bg_border_sepia_1080_3a",   textures["bg_border_sepia.png"],   2, 162, 47, 117);
            AssignBorder("bg_border_sepia_1080_3b",   textures["bg_border_sepia.png"],   2, 38, 47, 117);
            AssignBorder("bg_border_sepia_1080_4a",   textures["bg_border_sepia.png"],   2, 1150, 91, 107);
            AssignBorder("bg_border_sepia_1080_4b",   textures["bg_border_sepia.png"],   2, 1038, 91, 107);
            AssignBorder("bg_border_sepia_1080_5a",   textures["bg_border_sepia.png"],   2, 750, 97, 92);
            AssignBorder("bg_border_sepia_1080_5b",   textures["bg_border_sepia.png"],   2, 654, 97, 92);
            AssignBorder("bg_border_sepia_1080_6a",   textures["bg_border_sepia.png"],   2, 942, 107, 91);
            AssignBorder("bg_border_sepia_1080_6b",   textures["bg_border_sepia.png"],   2, 846, 107, 91);
            AssignBorder("bg_border_sepia_1080_7a",   textures["bg_border_sepia.png"],   2, 378, 87, 87);
            AssignBorder("bg_border_sepia_1080_7b",   textures["bg_border_sepia.png"],   2, 286, 87, 87);
            AssignBorder("bg_border_sepia_1080_8a",   textures["bg_border_sepia.png"],   2, 1366, 102, 97);
            AssignBorder("bg_border_sepia_1080_8b",   textures["bg_border_sepia.png"],   2, 1262, 102, 97);
            AssignBorder("bg_border_sepia_1080_9a",   textures["bg_border_sepia.png"],   118, 2, 112, 31);
            AssignBorder("bg_border_sepia_1080_9b",   textures["bg_border_sepia.png"],   2, 2, 112, 31);
            AssignBorder("bg_border_sepia_1080_glow", textures["bg_border_sepia.png"],   2, 1470, 137, 132);
            AssignBorder("bg_border_truelab_1080",    textures["bg_border_truelab.png"], 0, 0, 1920, 1080);
            AssignBorder("bg_border_tundra_1080",     textures["bg_border_tundra.png"],  0, 0, 1920, 1080);
            AssignBorder("bg_border_water1_1080",     textures["bg_border_water1.png"],  0, 0, 1920, 1080);

            ChangeSelection(Data.Backgrounds.ByName("bg_border_water1_1080"));
            break;
        }
    case "DELTARUNE Chapter 1&2": // DELTARUNE (Chapter 1&2 demo)
        {
            LoadBorders(new[] { bordersPath, bordersPathDTShared });
            AssignBorder("border_line_1080",        textures["border_line_1080.png"],     0, 0, 1920, 1080);
            AssignBorder("border_lw_town",          textures["border_lw_town.png"],       0, 0, 1920, 1080);
            AssignBorder("border_dw_castletown",    textures["border_dw_castletown.png"], 0, 0, 1920, 1080);
            AssignBorder("border_dw_cyber",         textures["border_dw_cyber.png"],      0, 0, 1920, 1080);
            AssignBorder("border_dw_mansion",       textures["border_dw_mansion.png"],    0, 0, 1920, 1080);
            AssignBorder("border_dw_city",          textures["border_dw_city.png"],       0, 0, 1920, 1080);
            AssignBorder("bg_border_line_1080_ch1", textures["border_line_1080.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dark_ch1",         textures["border_dw_castletown.png"], 0, 0, 1920, 1080);
            AssignBorder("border_light_ch1",        textures["border_lw_town.png"],       0, 0, 1920, 1080);

            ChangeSelection(Data.Sprites.ByName("border_light_ch1"));
            break;
        }
    case "DELTARUNE Chapter 1":
        {
            LoadBorders(new[] { bordersPathDTShared });
            AssignBorder("bg_border_line_1080", textures["border_line_1080.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dark",         textures["border_dw_castletown.png"], 0, 0, 1920, 1080);
            AssignBorder("border_light",        textures["border_lw_town.png"],       0, 0, 1920, 1080);

            ChangeSelection(Data.Sprites.ByName("border_light"));
            break;
        }
    case "DELTARUNE Chapter 2":
        {
            LoadBorders(new[] { bordersPath, bordersPathDTShared });
            AssignBorder("border_line_1080",     textures["border_line_1080.png"],     0, 0, 1920, 1080);
            AssignBorder("border_lw_town",       textures["border_lw_town.png"],       0, 0, 1920, 1080);
            AssignBorder("border_dw_castletown", textures["border_dw_castletown.png"], 0, 0, 1920, 1080);
            AssignBorder("border_dw_cyber",      textures["border_dw_cyber.png"],      0, 0, 1920, 1080);
            AssignBorder("border_dw_mansion",    textures["border_dw_mansion.png"],    0, 0, 1920, 1080);
            AssignBorder("border_dw_city",       textures["border_dw_city.png"],       0, 0, 1920, 1080);

            ChangeSelection(Data.Sprites.ByName("border_dw_city"));
            break;
        }
    case "DELTARUNE Chapter 3":
        {
            LoadBorders(new[] { bordersPath, bordersPathDTShared });
            AssignBorder("border_dw_tv_meta",        textures["border_dw_tv_meta.png"],        0, 0, 1920, 1080);
            AssignBorder("border_dw_tv_black",       textures["border_dw_tv_black.png"],       0, 0, 1920, 1080);
            AssignBorder("border_dw_green_room",     textures["border_dw_green_room.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_word",           textures["border_dw_word.png"],           0, 0, 1920, 1080);
            AssignBorder("border_dw_teevie",         textures["border_dw_teevie.png"],         0, 0, 1920, 1080);
            AssignBorder("border_dw_red_smiles",     textures["border_dw_red_smiles.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_blue_light",     textures["border_dw_blue_light.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_green_sloppy_z", textures["border_dw_green_sloppy_z.png"], 0, 0, 1920, 1080);
            AssignBorder("border_dw_blue_stars",     textures["border_dw_blue_stars.png"],     0, 0, 1920, 1080);
            AssignBorder("border_lw_town_night",     textures["border_lw_town_night.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_tv_blue",        textures["border_dw_tv_blue.png"],        0, 0, 1920, 1080);
            AssignBorder("border_line_1080",         textures["border_line_1080.png"],         0, 0, 1920, 1080);
            AssignBorder("border_lw_town",           textures["border_lw_town.png"],           0, 0, 1920, 1080);
            AssignBorder("border_dw_castletown",     textures["border_dw_castletown.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_blue",           textures["border_dw_blue.png"],           0, 0, 1920, 1080);
            AssignBorder("border_dw_green_sloppy",   textures["border_dw_green_sloppy.png"],   0, 0, 1920, 1080);

            ChangeSelection(Data.Sprites.ByName("border_dw_green_sloppy"));
            break;
        }
    case "DELTARUNE Chapter 4":
        {
            LoadBorders(new[] { bordersPath, bordersPathDTShared });
            AssignBorder("border_dw_titan_base",     textures["border_dw_titan_base.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_titan_eyes_red", textures["border_dw_titan_eyes_red.png"], 0, 0, 1920, 1080);
            AssignBorder("border_lw_town_night",     textures["border_lw_town_night.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_titan_eyes",     textures["border_dw_titan_eyes.png"],     0, 0, 1920, 1080);
            AssignBorder("border_line_1080",         textures["border_line_1080.png"],         0, 0, 1920, 1080);
            AssignBorder("border_lw_town",           textures["border_lw_town.png"],           0, 0, 1920, 1080);
            AssignBorder("border_dw_castletown",     textures["border_dw_castletown.png"],     0, 0, 1920, 1080);
            AssignBorder("border_dw_church_c",       textures["border_dw_church_c.png"],       0, 0, 1920, 1080);
            AssignBorder("border_dw_church_a",       textures["border_dw_church_a.png"],       0, 0, 1920, 1080);
            AssignBorder("border_dw_church_b",       textures["border_dw_church_b.png"],       0, 0, 1920, 1080);

            ChangeSelection(Data.Sprites.ByName("border_dw_church_b"));
            break;
        }
    case "DELTARUNE Chapter 5":
        {
            LoadBorders(new[] { bordersPath, bordersPathDTShared });
            AssignBorder("border_lw_town_morning",                textures["border_lw_town_morning.png"],              0, 0, 1920, 1080);
            AssignBorder("border_dw_castle_left",                 textures["border_dw_castle_left.png"],               0, 0, 1920, 1080);
            AssignBorder("border_dw_castle_cafe",                 textures["border_dw_castle_cafe.png"],               0, 0, 1920, 1080);
            AssignBorder("border_dw_garden_cliff",                textures["border_dw_garden_cliff.png"],              0, 0, 1920, 1350);
            AssignBorder("border_dw_garden_cliff_bottom",         textures["border_dw_garden_cliff.png"],              0, 1350, 1920, 1350);
            AssignBorder("border_dw_garden",                      textures["border_dw_garden.png"],                    0, 0, 1920, 1080);
            AssignBorder("border_dw_castle_right",                textures["border_dw_castle_right.png"],              0, 0, 1920, 1080);
            AssignBorder("border_dw_garden_cliff_frame",          textures["border_dw_garden_cliff_frame.png"],        0, 0, 1920, 1080);
            AssignBorder("border_lw_town_sunset",                 textures["border_lw_town_sunset.png"],               0, 0, 1920, 1080);
            AssignBorder("border_dw_garden_cliff_lattice",        textures["border_dw_garden_cliff_lattice.png"],      0, 0, 1920, 1350);
            AssignBorder("border_dw_garden_cliff_lattice_bottom", textures["border_dw_garden_cliff_lattice.png"],      0, 1350, 1920, 1350);
            AssignBorder("border_dw_pink_alt",                    textures["border_dw_pink_alt.png"],                  0, 0, 1920, 1080);
            AssignBorder("border_line_1080",                      textures["border_line_1080.png"],                    0, 0, 1920, 1080);
            AssignBorder("border_lw_town",                        textures["border_lw_town.png"],                      0, 0, 1920, 1080);
            AssignBorder("border_dw_castletown",                  textures["border_dw_castletown.png"],                0, 0, 1920, 1080);
            AssignBorder("border_dw_castle_top",                  textures["border_dw_castle_top.png"],                0, 0, 1920, 1080);
            AssignBorder("border_dw_pink",                        textures["border_dw_pink.png"],                      0, 0, 1920, 1080);
            AssignBorder("border_dw_garden_cliff_bottom_frame",   textures["border_dw_garden_cliff_bottom_frame.png"], 0, 0, 1920, 1080);
            AssignBorder("border_dw_castle_right_gold",           textures["border_dw_castle_right_gold.png"],         0, 0, 1920, 1080);
            AssignBorder("border_lw_town_night",                  textures["border_lw_town_night.png"],                0, 0, 1920, 1080);

            ChangeSelection(Data.Sprites.ByName("border_dw_garden_cliff"));
            break;
        }
}

ScriptMessage("Borders loaded and enabled for " + displayName + $"{(LTS ? " LTS" : "")}!");