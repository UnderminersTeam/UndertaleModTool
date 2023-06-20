using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "undertale")
{
    ReplaceTextInGML("gml_Object_obj_labdarkness_Create_0", "glowamt = 0.5", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Create_0", "if (global.plot > 125)", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Create_0", "if (scr_murderlv() >= 12)", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Create_0", "instance_destroy()", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Draw_0", "x1 = (obj_mainchara.x - 10)", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Draw_0", "x2 = (obj_mainchara.x + 30)", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Draw_0", "y1 = (obj_mainchara.y - 5)", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Draw_0", "y2 = (obj_mainchara.y + 35)", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Draw_0", "draw_sprite_ext(spr_darkhalo_big, 0, x1, y1, 1, 1, 0, c_white, glowamt)", "");
    ReplaceTextInGML("gml_Object_obj_labdarkness_Draw_0", "draw_set_alpha(1)", "");
    ReplaceTextInGML("gml_Object_obj_labcamera_Create_0", "if (global.osflavor == 2)", "if (global.osflavor == 4)");
} else {
    ScriptError("Error 0: Compatible with Undertale only");
    return;
}
