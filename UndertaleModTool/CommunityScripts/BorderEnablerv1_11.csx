using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}


ScriptMessage(@"Enables border for v1.11
by Jockeholm, based off krzys_h's original script.
Converted to be more efficient by Grossley.");

// Show the border settings on PC.
ReplaceTextInGML(("gml_Object_obj_settingsmenu_Draw_0"), @"if (global.osflavor <= 2)
{
    menu_max = 2
    if (obj_time.j_ch == 0)
        menu_max = 1
}", "", true);
ReplaceTextInGML(("gml_Object_obj_settingsmenu_Draw_0"), @"if (global.osflavor >= 4)", "if (global.osflavor >= 1)", true);

ReplaceTextInGML(("gml_Script_scr_draw_background_ps4"), @"if (os_type == os_ps4 || os_type == os_switch_beta)", "if (os_type == os_ps4 || os_type == os_switch_beta || os_type == os_windows)", true);

ReplaceTextInGML(("gml_Script_scr_draw_screen_border"), @"if (os_type == os_ps4 || os_type == os_switch_beta)", "if (os_type == os_ps4 || os_type == os_switch_beta || os_type == os_windows)", true);

ReplaceTextInGML(("gml_Script_scr_draw_screen_border"), @"if (os_type == os_switch_beta)", "if (os_type == os_switch_beta || os_type == os_windows)", true);

// Enable the dog border unlock
ReplaceTextInGML(("gml_Object_obj_rarependant_Step_1"), @"if (global.osflavor == 5)", "if (global.osflavor >= 1)", true);

// Load borders
ReplaceTextInGML(("gml_Object_obj_time_Step_1"), @"scr_enable_screen_border(global.osflavor >= 4)", "scr_enable_screen_border(global.osflavor >= 1)", true);

// Resize the game window to account for the borders
//Data.GeneralInfo.DefaultWindowWidth = 1920; // This setup prevents the game from starting??
//Data.GeneralInfo.DefaultWindowHeight = 1080;
Data.Code.ByName("gml_Script_SCR_GAMESTART").AppendGML(@"
window_set_size(960, 540)
", Data);

ReplaceTextInGML(("gml_Object_obj_time_Draw_77"), @"else
{
    global.window_xofs = 0
    global.window_yofs = 0
}", "", true);
ReplaceTextInGML(("gml_Object_obj_time_Draw_77"), @"if (global.osflavor >= 3)", "if (true)", true);
ReplaceTextInGML(("gml_Object_obj_time_Create_0"), @"if (global.osflavor >= 3)", "if (global.osflavor >= 1)", true);
ReplaceTextInGML(("gml_Object_obj_time_Draw_76"), @"else if (global.osflavor >= 4)", "else if (global.osflavor >= 1)", true);
ScriptMessage("Finished.");
