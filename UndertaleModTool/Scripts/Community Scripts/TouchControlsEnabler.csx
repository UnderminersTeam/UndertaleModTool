// Made by GitMuslim, Some fixes by NC-devC

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

    UndertaleTexturePageItem tpItem = new UndertaleTexturePageItem { SourceX = sourceX, SourceY = sourceY, SourceWidth = sourceWidth, SourceHeight = sourceHeight, TargetX = targetX, TargetY = targetY, TargetWidth = targetWidth, TargetHeight = targetHeight, BoundingWidth = boundingWidth, BoundingHeight = boundingHeight, TexturePage = texturePage };
    tpItem.Name = new UndertaleString($"PageItem {Data.TexturePageItems.Count}");
    Data.TexturePageItems.Add(tpItem);
    return tpItem;
}

UndertaleTexturePageItem pg_joybase1 = AddNewTexturePageItem(0, 0, 59, 59);
UndertaleTexturePageItem pg_joybase2 = AddNewTexturePageItem(0, 61, 59, 59);
UndertaleTexturePageItem pg_joystick = AddNewTexturePageItem(61, 0, 41, 41);
UndertaleTexturePageItem pg_joystick2 = AddNewTexturePageItem(0, 122, 41, 41);
UndertaleTexturePageItem pg_settings_n = AddNewTexturePageItem(61, 43, 19, 24);
UndertaleTexturePageItem pg_settings_p = AddNewTexturePageItem(83, 43, 19, 24);
UndertaleTexturePageItem pg_zbutton = AddNewTexturePageItem(104, 0, 27, 29);
UndertaleTexturePageItem pg_zbutton_p = AddNewTexturePageItem(104, 31, 27, 29);
UndertaleTexturePageItem pg_xbutton = AddNewTexturePageItem(133, 0, 27, 29);
UndertaleTexturePageItem pg_xbutton_p = AddNewTexturePageItem(133, 31, 27, 29);
UndertaleTexturePageItem pg_cbutton = AddNewTexturePageItem(162, 0, 27, 29);
UndertaleTexturePageItem pg_cbutton_p = AddNewTexturePageItem(162, 31, 27, 29);
UndertaleTexturePageItem pg_controls_config = AddNewTexturePageItem(61, 68, 100, 13);
UndertaleTexturePageItem pg_button_scale = AddNewTexturePageItem(61, 83, 79, 9);
UndertaleTexturePageItem pg_analog_scale = AddNewTexturePageItem(61, 94, 79, 12);
UndertaleTexturePageItem pg_analog_type = AddNewTexturePageItem(61, 108, 72, 12);
UndertaleTexturePageItem pg_reset_config = AddNewTexturePageItem(61, 122, 79, 12);
UndertaleTexturePageItem pg_controls_opacity = AddNewTexturePageItem(61, 136, 107, 13);
UndertaleTexturePageItem pg_arrow_leftright = AddNewTexturePageItem(142, 83, 41, 9);
UndertaleTexturePageItem pg_black = AddNewTexturePageItem(189, 0, 640, 480);

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
    Data.Sprites.Add(sItem);
}

AddNewUndertaleSprite("spr_joybase", 59, 59, new UndertaleTexturePageItem[] {pg_joybase1, pg_joybase2});
AddNewUndertaleSprite("spr_joystick", 42, 42, new UndertaleTexturePageItem[] {pg_joystick, pg_joystick2});
AddNewUndertaleSprite("spr_z_button", 27, 29, new UndertaleTexturePageItem[] {pg_zbutton, pg_zbutton_p});
AddNewUndertaleSprite("spr_x_button", 27, 29, new UndertaleTexturePageItem[] {pg_xbutton, pg_xbutton_p});
AddNewUndertaleSprite("spr_c_button", 27, 29, new UndertaleTexturePageItem[] {pg_cbutton, pg_cbutton_p});
AddNewUndertaleSprite("spr_settings", 19, 24, new UndertaleTexturePageItem[] {pg_settings_n, pg_settings_p});
AddNewUndertaleSprite("spr_controls_config", 100, 13, new UndertaleTexturePageItem[] {pg_controls_config});
AddNewUndertaleSprite("spr_button_scale", 79, 9, new UndertaleTexturePageItem[] {pg_button_scale});
AddNewUndertaleSprite("spr_analog_scale", 79, 12, new UndertaleTexturePageItem[] {pg_analog_scale});
AddNewUndertaleSprite("spr_analog_type", 72, 12, new UndertaleTexturePageItem[] {pg_analog_type});
AddNewUndertaleSprite("spr_reset_config", 79, 12, new UndertaleTexturePageItem[] {pg_reset_config});
AddNewUndertaleSprite("spr_controls_opacity", 107, 13, new UndertaleTexturePageItem[] {pg_controls_opacity});
AddNewUndertaleSprite("spr_arrow_leftright", 41, 9, new UndertaleTexturePageItem[] {pg_arrow_leftright});
AddNewUndertaleSprite("spr_black", 640, 480, new UndertaleTexturePageItem[] {pg_black});

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

int settingsnumx = 0;
if(currentFont == "fnt_main") {settingsnumx = 477; }
else if(currentFont == "fnt_mainbig") { settingsnumx = 502; }

string mobileControlsCreate = File.ReadAllText(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Create_0.gml"));
StringBuilder builder = new StringBuilder(mobileControlsCreate);
builder.Replace("{_font}", currentFont);
builder.Replace("{_settingsnumx}", Convert.ToString(settingsnumx));
mobileControlsCreate = builder.ToString();

ImportGMLString("gml_Object_obj_mobilecontrols_Create_0", mobileControlsCreate);
ImportGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Draw_64.gml"), true, false, true);
ImportGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Other_4.gml"), true, false, true);
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("scr_add_keys"), Code = Data.Code.ByName("gml_Object_obj_mobilecontrols_Other_4") });
ImportGMLFile(Path.Combine(dataPath, "gml_Object_obj_mobilecontrols_Step_0.gml"), true, false, true);

var mobileControls = Data.GameObjects.ByName("obj_mobilecontrols");
mobileControls.Persistent = true;

var obj_gamecontroller = Data.GameObjects.ByName("obj_gamecontroller");
if (obj_gamecontroller is not null)
{
    Data.Code.ByName("gml_Object_obj_gamecontroller_Create_0").AppendGML("instance_create(0, 0, obj_mobilecontrols);", Data);
    return;
}

var obj_time = Data.GameObjects.ByName("obj_time");
if (obj_time is not null)
{
    Data.Code.ByName("gml_Object_obj_time_Create_0").AppendGML("instance_create(0, 0, obj_mobilecontrols);", Data);
    return;
}

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
return;