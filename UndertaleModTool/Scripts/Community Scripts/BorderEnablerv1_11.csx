using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}


ScriptMessage(@"Enables border for v1.11
by Jockeholm, based off krzys_h's original script.
Converted to be more efficient by Grossley.");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true
};

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

ScriptMessage("Finished.");
