// Made by GitMuslim

using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

if (Data.GeneralInfo?.DisplayName?.Content.ToLower() != "undertale")
{
    if (!ScriptQuestion("This game isn't Undertale, thus the script may not work properly. Continue anyway?"))
    {
        return;
    }
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
importGroup.QueueReplace("gml_Object_obj_labdarkness_Create_0", "");
importGroup.QueueReplace("gml_Object_obj_labdarkness_Draw_0", "");
importGroup.QueueFindReplace("gml_Object_obj_labcamera_Create_0", "if (global.osflavor == 2)", "if (global.osflavor == 4)");
importGroup.Import();