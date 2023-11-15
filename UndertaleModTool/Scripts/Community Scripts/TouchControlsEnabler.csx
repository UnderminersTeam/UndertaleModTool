using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

string display_name = Data?.GeneralInfo?.DisplayName?.Content.ToLower();

if (display_name != "deltarune chapter 1 & 2" && display_name != "deltarune chapter 1&2" && display_name != "undertale")
{
    ScriptError("Error 0: Only compatible with Undertale & Deltarune");
    return;
}

string dataPath = Path.Combine(Path.GetDirectoryName(ScriptPath), "TouchControls_data");

int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();

UndertaleEmbeddedTexture controlsTexturePage = new UndertaleEmbeddedTexture()
{
    Name = new UndertaleString("Texture " + Data.EmbeddedTextures.Count),
};
controlsTexturePage.TextureData.TextureBlob = File.ReadAllBytes(Path.Combine(dataPath, "controls.png"));
Data.EmbeddedTextures.Add(controlsTexturePage);
textures.Add(Path.GetFileName(Path.Combine(dataPath, "controls.png")), controlsTexturePage);

UndertaleEmbeddedTexture black = new UndertaleEmbeddedTexture()
{
    Name = new UndertaleString("Texture " + Data.EmbeddedTextures.Count),
};
black.TextureData.TextureBlob = File.ReadAllBytes(Path.Combine(dataPath, "black.png"));
Data.EmbeddedTextures.Add(black);
textures.Add(Path.GetFileName(Path.Combine(dataPath, "black.png")), black);

UndertaleTexturePageItem AddNewTexturePageItem(ushort sourceX, ushort sourceY, ushort sourceWidth, ushort sourceHeight, string texturePageName)
{
    var name = new UndertaleString("PageItem " + Data.TexturePageItems.Count);
    ushort targetX = 0;
    ushort targetY = 0;
    ushort targetWidth = sourceWidth;
    ushort targetHeight = sourceHeight;
    ushort boundingWidth = sourceWidth;
    ushort boundingHeight = sourceHeight;
    var texturePage = textures[texturePageName];

    UndertaleTexturePageItem tpItem = new UndertaleTexturePageItem { Name = name, SourceX = sourceX, SourceY = sourceY, SourceWidth = sourceWidth, SourceHeight = sourceHeight, TargetX = targetX, TargetY = targetY, TargetWidth = targetWidth, TargetHeight = targetHeight, BoundingWidth = boundingWidth, BoundingHeight = boundingHeight, TexturePage = texturePage };
    Data.TexturePageItems.Add(tpItem);
    return tpItem;
}

UndertaleTexturePageItem pg_joybase1 = AddNewTexturePageItem(0, 0, 59, 59, "controls.png");
UndertaleTexturePageItem pg_joybase2 = AddNewTexturePageItem(0, 61, 59, 59, "controls.png");
UndertaleTexturePageItem pg_joystick = AddNewTexturePageItem(61, 0, 41, 41, "controls.png");
UndertaleTexturePageItem pg_joystick2 = AddNewTexturePageItem(0, 122, 41, 41, "controls.png");
UndertaleTexturePageItem pg_settings_n = AddNewTexturePageItem(61, 43, 19, 24, "controls.png");
UndertaleTexturePageItem pg_settings_p = AddNewTexturePageItem(83, 43, 19, 24, "controls.png");
UndertaleTexturePageItem pg_zbutton = AddNewTexturePageItem(104, 0, 27, 29, "controls.png");
UndertaleTexturePageItem pg_zbutton_p = AddNewTexturePageItem(104, 31, 27, 29, "controls.png");
UndertaleTexturePageItem pg_xbutton = AddNewTexturePageItem(133, 0, 27, 29, "controls.png");
UndertaleTexturePageItem pg_xbutton_p = AddNewTexturePageItem(133, 31, 27, 29, "controls.png");
UndertaleTexturePageItem pg_cbutton = AddNewTexturePageItem(162, 0, 27, 29, "controls.png");
UndertaleTexturePageItem pg_cbutton_p = AddNewTexturePageItem(162, 31, 27, 29, "controls.png");
UndertaleTexturePageItem pg_controls_config = AddNewTexturePageItem(61, 68, 100, 13, "controls.png");
UndertaleTexturePageItem pg_button_scale = AddNewTexturePageItem(61, 83, 79, 9, "controls.png");
UndertaleTexturePageItem pg_analog_scale = AddNewTexturePageItem(61, 94, 79, 12, "controls.png");
UndertaleTexturePageItem pg_analog_type = AddNewTexturePageItem(61, 108, 72, 12, "controls.png");
UndertaleTexturePageItem pg_reset_config = AddNewTexturePageItem(61, 122, 79, 12, "controls.png");
UndertaleTexturePageItem pg_controls_opacity = AddNewTexturePageItem(61, 136, 107, 13, "controls.png");
UndertaleTexturePageItem pg_arrow_leftright = AddNewTexturePageItem(142, 83, 41, 9, "controls.png");
UndertaleTexturePageItem pg_black = AddNewTexturePageItem(0, 0, 640, 480, "black.png");

UndertaleSprite AddNewUndertaleSprite(string spriteName, ushort width, ushort height)
{
    var name = Data.Strings.MakeString(spriteName);
    ushort marginLeft = 0;
    int marginRight = width - 1;
    ushort marginTop = 0;
    int marginBottom = height - 1;

    var sItem = new UndertaleSprite { Name = name, Width = width, Height = height, MarginLeft = marginLeft, MarginRight = marginRight, MarginTop = marginTop, MarginBottom = marginBottom };
    return sItem;
}

var spr_joybase = AddNewUndertaleSprite("spr_joybase", 59, 59);
var spr_joystick = AddNewUndertaleSprite("spr_joystick", 42, 42);
var spr_z_button = AddNewUndertaleSprite("spr_z_button", 27, 29);
var spr_x_button = AddNewUndertaleSprite("spr_x_button", 27, 29);
var spr_c_button = AddNewUndertaleSprite("spr_c_button", 27, 29);
var spr_settings = AddNewUndertaleSprite("spr_settings", 19, 24);
var spr_controls_config = AddNewUndertaleSprite("spr_controls_config", 100, 13);
var spr_button_scale = AddNewUndertaleSprite("spr_button_scale", 79, 9);
var spr_analog_scale = AddNewUndertaleSprite("spr_analog_scale", 79, 12);
var spr_analog_type = AddNewUndertaleSprite("spr_analog_type", 72, 12);
var spr_reset_config = AddNewUndertaleSprite("spr_reset_config", 79, 12);
var spr_controls_opacity = AddNewUndertaleSprite("spr_controls_opacity", 107, 13);
var spr_arrow_leftright = AddNewUndertaleSprite("spr_arrow_leftright", 41, 9);
var spr_black = AddNewUndertaleSprite("spr_black", 640, 480);

spr_joybase.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joybase1 });
spr_joybase.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joybase2 });
spr_joystick.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joystick });
spr_joystick.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joystick2 });
spr_z_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_zbutton });
spr_z_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_zbutton_p });
spr_x_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_xbutton });
spr_x_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_xbutton_p });
spr_c_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_cbutton });
spr_c_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_cbutton_p });
spr_settings.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_settings_n });
spr_settings.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_settings_p });
spr_controls_config.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_controls_config });
spr_button_scale.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_button_scale });
spr_analog_scale.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_analog_scale });
spr_analog_type.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_analog_type });
spr_reset_config.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_reset_config });
spr_controls_opacity.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_controls_opacity });
spr_arrow_leftright.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_arrow_leftright });
spr_black.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_black });

Data.Sprites.Add(spr_joybase);
Data.Sprites.Add(spr_joystick);
Data.Sprites.Add(spr_z_button);
Data.Sprites.Add(spr_x_button);
Data.Sprites.Add(spr_c_button);
Data.Sprites.Add(spr_settings);
Data.Sprites.Add(spr_controls_config);
Data.Sprites.Add(spr_button_scale);
Data.Sprites.Add(spr_analog_scale);
Data.Sprites.Add(spr_analog_type);
Data.Sprites.Add(spr_reset_config);
Data.Sprites.Add(spr_controls_opacity);
Data.Sprites.Add(spr_arrow_leftright);
Data.Sprites.Add(spr_black);

ImportGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Create_0.gml"), true, false, true);
ImportGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Draw_64.gml"), true, false, true);
ImportGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Other_4.gml"), true, false, true);
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("scr_add_keys"), Code = Data.Code.ByName("gml_Object_obj_mobilecontrols_Other_4") });
ImportGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Step_0.gml"), true, false, true);

var mobileControls = Data.GameObjects.ByName("obj_mobilecontrols");
mobileControls.Persistent = true;

if (display_name == "deltarune chapter 1 & 2" || display_name == "deltarune chapter 1&2")
{
    Data.Code.ByName("gml_Object_obj_gamecontroller_Create_0").AppendGML("instance_create(0, 0, obj_mobilecontrols);", Data);
}
else if (display_name == "undertale") 
{
    Data.Code.ByName("gml_Object_obj_time_Create_0").AppendGML("instance_create(0, 0, obj_mobilecontrols);", Data);
}
