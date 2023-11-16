// Made by GitMuslim

using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() != "undertale")
{
    ScriptError("Error 0: Compatible with Undertale only");
    return;
}
ReplaceTextInGML("gml_Object_obj_labdarkness_Create_0", "glowamt = 0.5\nif (global.plot > 125)\n    instance_destroy()\nif (scr_murderlv() >= 12)\n    instance_destroy()", "");
ReplaceTextInGML("gml_Object_obj_labdarkness_Draw_0", "x1 = (obj_mainchara.x - 10)\nx2 = (obj_mainchara.x + 30)\ny1 = (obj_mainchara.y - 5)\ny2 = (obj_mainchara.y + 35)\ndraw_sprite_ext(spr_darkhalo_big, 0, x1, y1, 1, 1, 0, c_white, glowamt)\ndraw_set_alpha(1)", "");
ReplaceTextInGML("gml_Object_obj_labcamera_Create_0", "if (global.osflavor == 2)", "if (global.osflavor == 4)");
