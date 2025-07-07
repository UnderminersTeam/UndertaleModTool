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

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = new Underanalyzer.Decompiler.DecompileSettings();

Data.Code.ByName("gml_Object_obj_labdarkness_Create_0").ReplaceGML("", Data);
Data.Code.ByName("gml_Object_obj_labdarkness_Draw_0").ReplaceGML("", Data);
ReplaceTextInGML("gml_Object_obj_labcamera_Create_0", "if (global.osflavor == 2)", "if (global.osflavor == 4)", false, false, globalDecompileContext, decompilerSettings);
