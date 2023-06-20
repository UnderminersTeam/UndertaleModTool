using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();


if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2" || Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2" || Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "undertale")
{
    // nothing
} else {
    ScriptError("Error 0: Only compatible with Undertale & Deltarune");
    return;
}

string dataPath = Path.Combine(Path.GetDirectoryName(ScriptPath), "TouchControls_data");

int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();

UndertaleEmbeddedTexture controls = new UndertaleEmbeddedTexture();
controls.Name = new UndertaleString("Texture " + ++lastTextPage);
controls.TextureData.TextureBlob = File.ReadAllBytes(dataPath+"/controls.png");
Data.EmbeddedTextures.Add(controls);
textures.Add(Path.GetFileName(dataPath+"/controls.png"), controls);

UndertaleEmbeddedTexture black = new UndertaleEmbeddedTexture();
black.Name = new UndertaleString("Texture " + ++lastTextPage);
black.TextureData.TextureBlob = File.ReadAllBytes(dataPath+"/black.png");
Data.EmbeddedTextures.Add(black);
textures.Add(Path.GetFileName(dataPath+"/black.png"), black);

UndertaleTexturePageItem pg_joybase1 = new UndertaleTexturePageItem();
pg_joybase1.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_joybase1.SourceX = 0; pg_joybase1.SourceY = 0; pg_joybase1.SourceWidth = 59; pg_joybase1.SourceHeight = 59;
pg_joybase1.TargetX = 0; pg_joybase1.TargetY = 0; pg_joybase1.TargetWidth = 59; pg_joybase1.TargetHeight = 59;
pg_joybase1.BoundingWidth = 59; pg_joybase1.BoundingHeight = 59;
pg_joybase1.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_joybase1);

UndertaleTexturePageItem pg_joybase2 = new UndertaleTexturePageItem();
pg_joybase2.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_joybase2.SourceX = 0; pg_joybase2.SourceY = 61; pg_joybase2.SourceWidth = 59; pg_joybase2.SourceHeight = 59;
pg_joybase2.TargetX = 0; pg_joybase2.TargetY = 0; pg_joybase2.TargetWidth = 59; pg_joybase2.TargetHeight = 59;
pg_joybase2.BoundingWidth = 59; pg_joybase2.BoundingHeight = 59;
pg_joybase2.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_joybase2);

UndertaleTexturePageItem pg_joystick = new UndertaleTexturePageItem();
pg_joystick.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_joystick.SourceX = 61; pg_joystick.SourceY = 0; pg_joystick.SourceWidth = 41; pg_joystick.SourceHeight = 41;
pg_joystick.TargetX = 0; pg_joystick.TargetY = 0; pg_joystick.TargetWidth = 41; pg_joystick.TargetHeight = 41;
pg_joystick.BoundingWidth = 41; pg_joystick.BoundingHeight = 41;
pg_joystick.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_joystick);

UndertaleTexturePageItem pg_joystick2 = new UndertaleTexturePageItem();
pg_joystick2.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_joystick2.SourceX = 0; pg_joystick2.SourceY = 122; pg_joystick2.SourceWidth = 41; pg_joystick2.SourceHeight = 41;
pg_joystick2.TargetX = 0; pg_joystick2.TargetY = 0; pg_joystick2.TargetWidth = 41; pg_joystick2.TargetHeight = 41;
pg_joystick2.BoundingWidth = 41; pg_joystick2.BoundingHeight = 41;
pg_joystick2.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_joystick2);

UndertaleTexturePageItem pg_settings_n = new UndertaleTexturePageItem();
pg_settings_n.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_settings_n.SourceX = 61; pg_settings_n.SourceY = 43; pg_settings_n.SourceWidth = 19; pg_settings_n.SourceHeight = 24;
pg_settings_n.TargetX = 0; pg_settings_n.TargetY = 0; pg_settings_n.TargetWidth = 19; pg_settings_n.TargetHeight = 24;
pg_settings_n.BoundingWidth = 19; pg_settings_n.BoundingHeight = 24;
pg_settings_n.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_settings_n);

UndertaleTexturePageItem pg_settings_p = new UndertaleTexturePageItem();
pg_settings_p.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_settings_p.SourceX = 83; pg_settings_p.SourceY = 43; pg_settings_p.SourceWidth = 19; pg_settings_p.SourceHeight = 24;
pg_settings_p.TargetX = 0; pg_settings_p.TargetY = 0; pg_settings_p.TargetWidth = 19; pg_settings_p.TargetHeight = 24;
pg_settings_p.BoundingWidth = 19; pg_settings_p.BoundingHeight = 24;
pg_settings_p.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_settings_p);

UndertaleTexturePageItem pg_zbutton = new UndertaleTexturePageItem();
pg_zbutton.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_zbutton.SourceX = 104; pg_zbutton.SourceY = 0; pg_zbutton.SourceWidth = 27; pg_zbutton.SourceHeight = 29;
pg_zbutton.TargetX = 0; pg_zbutton.TargetY = 0; pg_zbutton.TargetWidth = 27; pg_zbutton.TargetHeight = 29;
pg_zbutton.BoundingWidth = 27; pg_zbutton.BoundingHeight = 29;
pg_zbutton.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_zbutton);

UndertaleTexturePageItem pg_zbutton_p = new UndertaleTexturePageItem();
pg_zbutton_p.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_zbutton_p.SourceX = 104; pg_zbutton_p.SourceY = 31; pg_zbutton_p.SourceWidth = 27; pg_zbutton_p.SourceHeight = 29;
pg_zbutton_p.TargetX = 0; pg_zbutton_p.TargetY = 0; pg_zbutton_p.TargetWidth = 27; pg_zbutton_p.TargetHeight = 29;
pg_zbutton_p.BoundingWidth = 27; pg_zbutton_p.BoundingHeight = 29;
pg_zbutton_p.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_zbutton_p);

UndertaleTexturePageItem pg_xbutton = new UndertaleTexturePageItem();
pg_xbutton.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_xbutton.SourceX = 133; pg_xbutton.SourceY = 0; pg_xbutton.SourceWidth = 27; pg_xbutton.SourceHeight = 29;
pg_xbutton.TargetX = 0; pg_xbutton.TargetY = 0; pg_xbutton.TargetWidth = 27; pg_xbutton.TargetHeight = 29;
pg_xbutton.BoundingWidth = 27; pg_xbutton.BoundingHeight = 29;
pg_xbutton.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_xbutton);

UndertaleTexturePageItem pg_xbutton_p = new UndertaleTexturePageItem();
pg_xbutton_p.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_xbutton_p.SourceX = 133; pg_xbutton_p.SourceY = 31; pg_xbutton_p.SourceWidth = 27; pg_xbutton_p.SourceHeight = 29;
pg_xbutton_p.TargetX = 0; pg_xbutton_p.TargetY = 0; pg_xbutton_p.TargetWidth = 27; pg_xbutton_p.TargetHeight = 29;
pg_xbutton_p.BoundingWidth = 27; pg_xbutton_p.BoundingHeight = 29;
pg_xbutton_p.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_xbutton_p);

UndertaleTexturePageItem pg_cbutton = new UndertaleTexturePageItem();
pg_cbutton.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_cbutton.SourceX = 162; pg_cbutton.SourceY = 0; pg_cbutton.SourceWidth = 27; pg_cbutton.SourceHeight = 29;
pg_cbutton.TargetX = 0; pg_cbutton.TargetY = 0; pg_cbutton.TargetWidth = 27; pg_cbutton.TargetHeight = 29;
pg_cbutton.BoundingWidth = 27; pg_cbutton.BoundingHeight = 29;
pg_cbutton.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_cbutton);

UndertaleTexturePageItem pg_cbutton_p = new UndertaleTexturePageItem();
pg_cbutton_p.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_cbutton_p.SourceX = 162; pg_cbutton_p.SourceY = 31; pg_cbutton_p.SourceWidth = 27; pg_cbutton_p.SourceHeight = 29;
pg_cbutton_p.TargetX = 0; pg_cbutton_p.TargetY = 0; pg_cbutton_p.TargetWidth = 27; pg_cbutton_p.TargetHeight = 29;
pg_cbutton_p.BoundingWidth = 27; pg_cbutton_p.BoundingHeight = 29;
pg_cbutton_p.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_cbutton_p);

UndertaleTexturePageItem pg_controls_config = new UndertaleTexturePageItem();
pg_controls_config.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_controls_config.SourceX = 61; pg_controls_config.SourceY = 68; pg_controls_config.SourceWidth = 100; pg_controls_config.SourceHeight = 13;
pg_controls_config.TargetX = 0; pg_controls_config.TargetY = 0; pg_controls_config.TargetWidth = 100; pg_controls_config.TargetHeight = 13;
pg_controls_config.BoundingWidth = 100; pg_controls_config.BoundingHeight = 13;
pg_controls_config.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_controls_config);

UndertaleTexturePageItem pg_button_scale = new UndertaleTexturePageItem();
pg_button_scale.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_button_scale.SourceX = 61; pg_button_scale.SourceY = 83; pg_button_scale.SourceWidth = 79; pg_button_scale.SourceHeight = 9;
pg_button_scale.TargetX = 0; pg_button_scale.TargetY = 0; pg_button_scale.TargetWidth = 79; pg_button_scale.TargetHeight = 9;
pg_button_scale.BoundingWidth = 79; pg_button_scale.BoundingHeight = 9;
pg_button_scale.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_button_scale);

UndertaleTexturePageItem pg_analog_scale = new UndertaleTexturePageItem();
pg_analog_scale.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_analog_scale.SourceX = 61; pg_analog_scale.SourceY = 94; pg_analog_scale.SourceWidth = 79; pg_analog_scale.SourceHeight = 12;
pg_analog_scale.TargetX = 0; pg_analog_scale.TargetY = 0; pg_analog_scale.TargetWidth = 79; pg_analog_scale.TargetHeight = 12;
pg_analog_scale.BoundingWidth = 79; pg_analog_scale.BoundingHeight = 12;
pg_analog_scale.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_analog_scale);

UndertaleTexturePageItem pg_analog_type = new UndertaleTexturePageItem();
pg_analog_type.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_analog_type.SourceX = 61; pg_analog_type.SourceY = 108; pg_analog_type.SourceWidth = 72; pg_analog_type.SourceHeight = 12;
pg_analog_type.TargetX = 0; pg_analog_type.TargetY = 0; pg_analog_type.TargetWidth = 72; pg_analog_type.TargetHeight = 12;
pg_analog_type.BoundingWidth = 72; pg_analog_type.BoundingHeight = 12;
pg_analog_type.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_analog_type);

UndertaleTexturePageItem pg_reset_config = new UndertaleTexturePageItem();
pg_reset_config.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_reset_config.SourceX = 61; pg_reset_config.SourceY = 122; pg_reset_config.SourceWidth = 79; pg_reset_config.SourceHeight = 12;
pg_reset_config.TargetX = 0; pg_reset_config.TargetY = 0; pg_reset_config.TargetWidth = 79; pg_reset_config.TargetHeight = 12;
pg_reset_config.BoundingWidth = 79; pg_reset_config.BoundingHeight = 12;
pg_reset_config.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_reset_config);

UndertaleTexturePageItem pg_controls_opacity = new UndertaleTexturePageItem();
pg_controls_opacity.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_controls_opacity.SourceX = 61; pg_controls_opacity.SourceY = 136; pg_controls_opacity.SourceWidth = 107; pg_controls_opacity.SourceHeight = 13;
pg_controls_opacity.TargetX = 0; pg_controls_opacity.TargetY = 0; pg_controls_opacity.TargetWidth = 107; pg_controls_opacity.TargetHeight = 13;
pg_controls_opacity.BoundingWidth = 107; pg_controls_opacity.BoundingHeight = 13;
pg_controls_opacity.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_controls_opacity);

UndertaleTexturePageItem pg_arrow_leftright = new UndertaleTexturePageItem();
pg_arrow_leftright.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_arrow_leftright.SourceX = 142; pg_arrow_leftright.SourceY = 83; pg_arrow_leftright.SourceWidth = 41; pg_arrow_leftright.SourceHeight = 9;
pg_arrow_leftright.TargetX = 0; pg_arrow_leftright.TargetY = 0; pg_arrow_leftright.TargetWidth = 41; pg_arrow_leftright.TargetHeight = 9;
pg_arrow_leftright.BoundingWidth = 41; pg_arrow_leftright.BoundingHeight = 9;
pg_arrow_leftright.TexturePage = textures["controls.png"];
Data.TexturePageItems.Add(pg_arrow_leftright);

UndertaleTexturePageItem pg_black = new UndertaleTexturePageItem();
pg_black.Name = new UndertaleString("PageItem " + ++lastTextPageItem);
pg_black.SourceX = 0; pg_black.SourceY = 0; pg_black.SourceWidth = 640; pg_black.SourceHeight = 480;
pg_black.TargetX = 0; pg_black.TargetY = 0; pg_black.TargetWidth = 640; pg_black.TargetHeight = 480;
pg_black.BoundingWidth = 640; pg_black.BoundingHeight = 480;
pg_black.TexturePage = textures["black.png"];
Data.TexturePageItems.Add(pg_black);

var spr_joybase = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_joybase"),
    Width = 59,
    Height = 59,
    MarginLeft = 0,
    MarginRight = 58,
    MarginTop = 0,
    MarginBottom = 58,
};

spr_joybase.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joybase1 });
spr_joybase.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joybase2 });

var spr_joystick = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_joystick"),
    Width = 42,
    Height = 42,
    MarginLeft = 0,
    MarginRight = 41,
    MarginTop = 0,
    MarginBottom = 41,
};

spr_joystick.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joystick });
spr_joystick.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_joystick2 });

var spr_z_button = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_z_button"),
    Width = 27,
    Height = 29,
    MarginLeft = 0,
    MarginRight = 26,
    MarginTop = 0,
    MarginBottom = 28,
};

spr_z_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_zbutton });
spr_z_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_zbutton_p });

var spr_x_button = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_x_button"),
    Width = 27,
    Height = 29,
    MarginLeft = 0,
    MarginRight = 26,
    MarginTop = 0,
    MarginBottom = 28,
};

spr_x_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_xbutton });
spr_x_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_xbutton_p });

var spr_c_button = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_c_button"),
    Width = 27,
    Height = 29,
    MarginLeft = 0,
    MarginRight = 26,
    MarginTop = 0,
    MarginBottom = 28,
};

spr_c_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_cbutton });
spr_c_button.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_cbutton_p });

var spr_settings = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_settings"),
    Width = 19,
    Height = 24,
    MarginLeft = 0,
    MarginRight = 18,
    MarginTop = 0,
    MarginBottom = 23,
};

spr_settings.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_settings_n });
spr_settings.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_settings_p });

Data.Sprites.Add(spr_joybase);
Data.Sprites.Add(spr_joystick);
Data.Sprites.Add(spr_z_button);
Data.Sprites.Add(spr_x_button);
Data.Sprites.Add(spr_c_button);
Data.Sprites.Add(spr_settings);

var spr_controls_config = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_controls_config"),
    Width = 100,
    Height = 13,
    MarginLeft = 0,
    MarginRight = 99,
    MarginTop = 0,
    MarginBottom = 12,
};
spr_controls_config.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_controls_config });
Data.Sprites.Add(spr_controls_config);

var spr_button_scale = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_button_scale"),
    Width = 79,
    Height = 9,
    MarginLeft = 0,
    MarginRight = 78,
    MarginTop = 0,
    MarginBottom = 8,
};
spr_button_scale.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_button_scale });
Data.Sprites.Add(spr_button_scale);

var spr_analog_scale = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_analog_scale"),
    Width = 79,
    Height = 12,
    MarginLeft = 0,
    MarginRight = 78,
    MarginTop = 0,
    MarginBottom = 11,
};
spr_analog_scale.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_analog_scale });
Data.Sprites.Add(spr_analog_scale);

var spr_analog_type = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_analog_type"),
    Width = 72,
    Height = 12,
    MarginLeft = 0,
    MarginRight = 71,
    MarginTop = 0,
    MarginBottom = 11,
};
spr_analog_type.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_analog_type });
Data.Sprites.Add(spr_analog_type);

var spr_reset_config = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_reset_config"),
    Width = 79,
    Height = 12,
    MarginLeft = 0,
    MarginRight = 78,
    MarginTop = 0,
    MarginBottom = 11,
};
spr_reset_config.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_reset_config });
Data.Sprites.Add(spr_reset_config);

var spr_controls_opacity = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_controls_opacity"),
    Width = 107,
    Height = 13,
    MarginLeft = 0,
    MarginRight = 106,
    MarginTop = 0,
    MarginBottom = 12,
};
spr_controls_opacity.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_controls_opacity });
Data.Sprites.Add(spr_controls_opacity);

var spr_arrow_leftright = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_arrow_leftright"),
    Width = 41,
    Height = 9,
    MarginLeft = 0,
    MarginRight = 40,
    MarginTop = 0,
    MarginBottom = 8,
};
spr_arrow_leftright.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_arrow_leftright });
Data.Sprites.Add(spr_arrow_leftright);

var spr_black = new UndertaleSprite()
{
    Name = Data.Strings.MakeString("spr_black"),
    Width = 640,
    Height = 480,
    MarginLeft = 0,
    MarginRight = 639,
    MarginTop = 0,
    MarginBottom = 479,
};
spr_black.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_black });
Data.Sprites.Add(spr_black);

ImportGMLFile(dataPath+"/gml_Object_obj_mobilecontrols_Create_0.gml", true, false, true);
ImportGMLFile(dataPath+"/gml_Object_obj_mobilecontrols_Draw_64.gml", true, false, true);
ImportGMLFile(dataPath+"/gml_Object_obj_mobilecontrols_Other_4.gml", true, false, true);
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("scr_add_keys"), Code = Data.Code.ByName("gml_Object_obj_mobilecontrols_Other_4") });
ImportGMLFile(dataPath+"/gml_Object_obj_mobilecontrols_Step_0.gml", true, false, true);

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    Data.Code.ByName("gml_Object_obj_gamecontroller_Create_0").AppendGML("instance_create(0, 0, obj_mobilecontrols);", Data);
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    Data.Code.ByName("gml_Object_obj_gamecontroller_Create_0").AppendGML("instance_create(0, 0, obj_mobilecontrols);", Data);
} 
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "undertale") 
{
    Data.Code.ByName("gml_Object_obj_time_Create_0").AppendGML("instance_create(0, 0, obj_mobilecontrols);", Data);
}


