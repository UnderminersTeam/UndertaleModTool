// Made by GitMuslim, Some fixes by NC-devC
// Rework by GFOXSH

using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

string displayName = Data.GeneralInfo?.DisplayName?.Content.ToLower();

if (displayName != "deltarune chapter 1 & 2" && displayName != "deltarune chapter 1&2" && displayName != "undertale")
{
    if (!ScriptQuestion("This game is neither Undertale or Deltarune, thus the script may not work properly. Continue anyway?"))
    {
        return;
    }
}

string dataPath = Path.Combine(Path.GetDirectoryName(ScriptPath), "TouchControls_data");

Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();

UndertaleEmbeddedTexture controlsTexturePage = new UndertaleEmbeddedTexture();
controlsTexturePage.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(Path.Combine(dataPath, "controls.png"))); // TODO: generate other formats
Data.EmbeddedTextures.Add(controlsTexturePage);
textures.Add(Path.GetFileName(Path.Combine(dataPath, "controls.png")), controlsTexturePage);

UndertaleTexturePageItem AddNewTexturePageItem(ushort sourceX, ushort sourceY, ushort sourceWidth, ushort sourceHeight)
{
    ushort targetX = 0;
    ushort targetY = 0;
    ushort targetWidth = sourceWidth;
    ushort targetHeight = sourceHeight;
    ushort boundingWidth = sourceWidth;
    ushort boundingHeight = sourceHeight;
    var texturePage = textures["controls.png"];

    UndertaleTexturePageItem tpItem = new() 
    { 
        SourceX = sourceX, 
        SourceY = sourceY, 
        SourceWidth = sourceWidth, 
        SourceHeight = sourceHeight, 
        TargetX = targetX, 
        TargetY = targetY, 
        TargetWidth = targetWidth, 
        TargetHeight = targetHeight, 
        BoundingWidth = boundingWidth, 
        BoundingHeight = boundingHeight, 
        TexturePage = texturePage,
        Name = new UndertaleString($"PageItem {Data.TexturePageItems.Count}")
    };
    Data.TexturePageItems.Add(tpItem);
    return tpItem;
}

UndertaleTexturePageItem pg_black_0 = AddNewTexturePageItem(0, 0, 640, 480);
UndertaleTexturePageItem pg_joybase_0 = AddNewTexturePageItem(642, 0, 59, 59);
UndertaleTexturePageItem pg_joybase_1 = AddNewTexturePageItem(703, 0, 59, 59);
UndertaleTexturePageItem pg_joystick_0 = AddNewTexturePageItem(642, 61, 41, 41);
UndertaleTexturePageItem pg_joystick_1 = AddNewTexturePageItem(685, 61, 41, 41);
UndertaleTexturePageItem pg_zbutton_0 = AddNewTexturePageItem(642, 104, 27, 29);
UndertaleTexturePageItem pg_zbutton_1 = AddNewTexturePageItem(671, 104, 27, 29);
UndertaleTexturePageItem pg_xbutton_0 = AddNewTexturePageItem(642, 135, 27, 29);
UndertaleTexturePageItem pg_xbutton_1 = AddNewTexturePageItem(671, 135, 27, 29);
UndertaleTexturePageItem pg_cbutton_0 = AddNewTexturePageItem(642, 166, 27, 29);
UndertaleTexturePageItem pg_cbutton_1 = AddNewTexturePageItem(671, 166, 27, 29);
UndertaleTexturePageItem pg_f1button_0 = AddNewTexturePageItem(642, 197, 19, 23);
UndertaleTexturePageItem pg_f1button_1 = AddNewTexturePageItem(663, 197, 19, 23);
UndertaleTexturePageItem pg_settings_0 = AddNewTexturePageItem(642, 222, 19, 23);
UndertaleTexturePageItem pg_settings_1 = AddNewTexturePageItem(663, 222, 19, 23);
UndertaleTexturePageItem pg_controls_config_0 = AddNewTexturePageItem(642, 247, 100, 13);
UndertaleTexturePageItem pg_button_scale_0 = AddNewTexturePageItem(642, 262, 79, 9);
UndertaleTexturePageItem pg_analog_scale_0 = AddNewTexturePageItem(642, 273, 79, 12);
UndertaleTexturePageItem pg_analog_type_0 = AddNewTexturePageItem(642, 287, 72, 12);
UndertaleTexturePageItem pg_controls_opacity_0 = AddNewTexturePageItem(642, 301, 107, 13);
UndertaleTexturePageItem pg_arrow_slider_0 = AddNewTexturePageItem(642, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_1 = AddNewTexturePageItem(685, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_2 = AddNewTexturePageItem(728, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_3 = AddNewTexturePageItem(771, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_4 = AddNewTexturePageItem(814, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_5 = AddNewTexturePageItem(857, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_6 = AddNewTexturePageItem(900, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_7 = AddNewTexturePageItem(943, 316, 41, 9);
UndertaleTexturePageItem pg_arrow_slider_8 = AddNewTexturePageItem(642, 327, 41, 9);
UndertaleTexturePageItem pg_arrow_switch_0 = AddNewTexturePageItem(642, 338, 41, 9);
UndertaleTexturePageItem pg_arrow_switch_1 = AddNewTexturePageItem(685, 338, 41, 9);

void AddNewUndertaleSprite(string spriteName, ushort width, ushort height, UndertaleTexturePageItem[] spriteTextures)
{
    var name = Data.Strings.MakeString(spriteName);
    ushort marginLeft = 0;
    int marginRight = width - 1;
    ushort marginTop = 0;
    int marginBottom = height - 1;

    var sItem = new UndertaleSprite { Name = name, Width = width, Height = height, MarginLeft = marginLeft, MarginRight = marginRight, MarginTop = marginTop, MarginBottom = marginBottom };
    foreach (var spriteTexture in spriteTextures) 
    {
        sItem.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = spriteTexture });
    }
	
	if (spriteName == "spr_joybase" || spriteName == "spr_joystick" || spriteName == "spr_z_button" || spriteName == "spr_x_button" || spriteName == "spr_c_button" || spriteName == "spr_f1_button" || spriteName == "spr_settings") {
		sItem.OriginXWrapper = (ushort)Math.Round(width / 2.0);
		sItem.OriginYWrapper = (ushort)Math.Round(height / 2.0);
	}
	
	Data.Sprites.Add(sItem);
}

AddNewUndertaleSprite("spr_black", 640, 480, new UndertaleTexturePageItem[] {pg_black_0});
AddNewUndertaleSprite("spr_joybase", 59, 59, new UndertaleTexturePageItem[] {pg_joybase_0, pg_joybase_1});
AddNewUndertaleSprite("spr_joystick", 42, 42, new UndertaleTexturePageItem[] {pg_joystick_0, pg_joystick_1});
AddNewUndertaleSprite("spr_z_button", 27, 29, new UndertaleTexturePageItem[] {pg_zbutton_0, pg_zbutton_1});
AddNewUndertaleSprite("spr_x_button", 27, 29, new UndertaleTexturePageItem[] {pg_xbutton_0, pg_xbutton_1});
AddNewUndertaleSprite("spr_c_button", 27, 29, new UndertaleTexturePageItem[] {pg_cbutton_0, pg_cbutton_1});
AddNewUndertaleSprite("spr_f1_button", 19, 23, new UndertaleTexturePageItem[] {pg_f1button_0, pg_f1button_1});
AddNewUndertaleSprite("spr_settings", 19, 23, new UndertaleTexturePageItem[] {pg_settings_0, pg_settings_1});
AddNewUndertaleSprite("spr_controls_config", 100, 13, new UndertaleTexturePageItem[] {pg_controls_config_0});
AddNewUndertaleSprite("spr_button_scale", 79, 9, new UndertaleTexturePageItem[] {pg_button_scale_0});
AddNewUndertaleSprite("spr_analog_scale", 79, 12, new UndertaleTexturePageItem[] {pg_analog_scale_0});
AddNewUndertaleSprite("spr_analog_type", 72, 12, new UndertaleTexturePageItem[] {pg_analog_type_0});
AddNewUndertaleSprite("spr_controls_opacity", 107, 13, new UndertaleTexturePageItem[] {pg_controls_opacity_0});
AddNewUndertaleSprite("spr_arrow_slider", 41, 9, new UndertaleTexturePageItem[] {pg_arrow_slider_0, pg_arrow_slider_1, pg_arrow_slider_2, pg_arrow_slider_3, pg_arrow_slider_4, pg_arrow_slider_5, pg_arrow_slider_6, pg_arrow_slider_7, pg_arrow_slider_8});
AddNewUndertaleSprite("spr_arrow_switch", 41, 9, new UndertaleTexturePageItem[] {pg_arrow_switch_0, pg_arrow_switch_1});

string currentFont = "";
var fntMain = Data.Fonts.Any(o => o.Name.Content.ToLower() == "fnt_main");

if (fntMain)
{
    currentFont = "fnt_main";
}
else
{
    var fntMainBig = Data.Fonts.Any(o => o.Name.Content.ToLower() == "fnt_mainbig");
    if (fntMainBig)
    {
        currentFont = "fnt_mainbig";
    }
    else
    {
        currentFont = Data.Fonts.First().Name.Content;
    }
}

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

QueueGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Create_0.gml"));
QueueGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Draw_64.gml"));
QueueGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Other_4.gml"));
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("scr_add_keys"), Code = Data.Code.ByName("gml_Object_obj_mobilecontrols_Other_4") });
QueueGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Step_0.gml"));

var mobileControls = Data.GameObjects.ByName("obj_mobilecontrols");
mobileControls.Persistent = true;

var obj_gamecontroller = Data.GameObjects.ByName("obj_gamecontroller");
if (obj_gamecontroller is not null)
{
    importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_gamecontroller_Create_0"), 
                            "instance_create(0, 0, obj_mobilecontrols);");
    importGroup.Import();
    return;
}

var obj_time = Data.GameObjects.ByName("obj_time");
if (obj_time is not null)
{
    importGroup.QueueAppend(Data.Code.ByName("gml_Object_obj_time_Create_0"),
                            "instance_create(0, 0, obj_mobilecontrols);");
    importGroup.Import();
    return;
}

importGroup.Import();

var firstRoom = Data.Rooms[0];
var shouldAdd = !(firstRoom.GameObjects.Any(o => o.ObjectDefinition == mobileControls));

if (shouldAdd)
{
    firstRoom.GameObjects.Add(new UndertaleRoom.GameObject()
    {
        InstanceID = Data.GeneralInfo.LastObj++,
        ObjectDefinition = mobileControls,
        X = 0, Y = 0
    });
}

void QueueGMLFile(string path)
{
    importGroup.QueueReplace(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
}
