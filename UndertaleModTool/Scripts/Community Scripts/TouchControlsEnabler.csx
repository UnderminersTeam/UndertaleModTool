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

UndertaleEmbeddedTexture controls = new UndertaleEmbeddedTexture()
{
    Name = new UndertaleString("Texture " + ++lastTextPage),
};
controls.TextureData.TextureBlob = File.ReadAllBytes(dataPath+"/controls.png");
Data.EmbeddedTextures.Add(controls);
textures.Add(Path.GetFileName(dataPath+"/controls.png"), controls);

UndertaleEmbeddedTexture black = new UndertaleEmbeddedTexture()
{
    Name = new UndertaleString("Texture " + ++lastTextPage),
};
black.TextureData.TextureBlob = File.ReadAllBytes(dataPath+"/black.png");
Data.EmbeddedTextures.Add(black);
textures.Add(Path.GetFileName(dataPath+"/black.png"), black);

UndertaleTexturePageItem pg_joybase1 = new UndertaleTexturePageItem()
{
  Name = new UndertaleString("PageItem " + ++lastTextPageItem),
  SourceX = 0, SourceY = 0, SourceWidth = 59, SourceHeight = 59,
  TargetX = 0, TargetY = 0, TargetWidth = 59, TargetHeight = 59,
  BoundingWidth = 59, BoundingHeight = 59,
  TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_joybase1);

UndertaleTexturePageItem pg_joybase2 = new UndertaleTexturePageItem()
{
  Name = new UndertaleString("PageItem " + ++lastTextPageItem),
  SourceX = 0, SourceY = 61, SourceWidth = 59, SourceHeight = 59,
  TargetX = 0, TargetY = 0, TargetWidth = 59, TargetHeight = 59,
  BoundingWidth = 59, BoundingHeight = 59,
  TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_joybase2);

UndertaleTexturePageItem pg_joystick = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 0, SourceWidth = 41, SourceHeight = 41,
    TargetX = 0, TargetY = 0, TargetWidth = 41, TargetHeight = 41,
    BoundingWidth = 41, BoundingHeight = 41,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_joystick);

UndertaleTexturePageItem pg_joystick2 = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 0, SourceY = 122, SourceWidth = 41, SourceHeight = 41,
    TargetX = 0, TargetY = 0, TargetWidth = 41, TargetHeight = 41,
    BoundingWidth = 41, BoundingHeight = 41,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_joystick2);

UndertaleTexturePageItem pg_settings_n = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 43, SourceWidth = 19, SourceHeight = 24,
    TargetX = 0, TargetY = 0, TargetWidth = 19, TargetHeight = 24,
    BoundingWidth = 19, BoundingHeight = 24,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_settings_n);

UndertaleTexturePageItem pg_settings_p = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 83, SourceY = 43, SourceWidth = 19, SourceHeight = 24,
    TargetX = 0, TargetY = 0, TargetWidth = 19, TargetHeight = 24,
    BoundingWidth = 19, BoundingHeight = 24,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_settings_p);

UndertaleTexturePageItem pg_zbutton = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 104, SourceY = 0, SourceWidth = 27, SourceHeight = 29,
    TargetX = 0, TargetY = 0, TargetWidth = 27, TargetHeight = 29,
    BoundingWidth = 27, BoundingHeight = 29,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_zbutton);

UndertaleTexturePageItem pg_zbutton_p = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 104, SourceY = 31, SourceWidth = 27, SourceHeight = 29,
    TargetX = 0, TargetY = 0, TargetWidth = 27, TargetHeight = 29,
    BoundingWidth = 27, BoundingHeight = 29,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_zbutton_p);

UndertaleTexturePageItem pg_xbutton = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 133, SourceY = 0, SourceWidth = 27, SourceHeight = 29,
    TargetX = 0, TargetY = 0, TargetWidth = 27, TargetHeight = 29,
    BoundingWidth = 27, BoundingHeight = 29,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_xbutton);

UndertaleTexturePageItem pg_xbutton_p = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 133, SourceY = 31, SourceWidth = 27, SourceHeight = 29,
    TargetX = 0, TargetY = 0, TargetWidth = 27, TargetHeight = 29,
    BoundingWidth = 27, BoundingHeight = 29,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_xbutton_p);

UndertaleTexturePageItem pg_cbutton = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 162, SourceY = 0, SourceWidth = 27, SourceHeight = 29,
    TargetX = 0, TargetY = 0, TargetWidth = 27, TargetHeight = 29,
    BoundingWidth = 27, BoundingHeight = 29,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_cbutton);

UndertaleTexturePageItem pg_cbutton_p = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 162, SourceY = 31, SourceWidth = 27, SourceHeight = 29,
    TargetX = 0, TargetY = 0, TargetWidth = 27, TargetHeight = 29,
    BoundingWidth = 27, BoundingHeight = 29,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_cbutton_p);

UndertaleTexturePageItem pg_controls_config = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 68, SourceWidth = 100, SourceHeight = 13,
    TargetX = 0, TargetY = 0, TargetWidth = 100, TargetHeight = 13,
    BoundingWidth = 100, BoundingHeight = 13,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_controls_config);

UndertaleTexturePageItem pg_button_scale = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 83, SourceWidth = 79, SourceHeight = 9,
    TargetX = 0, TargetY = 0, TargetWidth = 79, TargetHeight = 9,
    BoundingWidth = 79, BoundingHeight = 9,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_button_scale);

UndertaleTexturePageItem pg_analog_scale = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 94, SourceWidth = 79, SourceHeight = 12,
    TargetX = 0, TargetY = 0, TargetWidth = 79, TargetHeight = 12,
    BoundingWidth = 79, BoundingHeight = 12,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_analog_scale);

UndertaleTexturePageItem pg_analog_type = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 108, SourceWidth = 72, SourceHeight = 12,
    TargetX = 0, TargetY = 0, TargetWidth = 72, TargetHeight = 12,
    BoundingWidth = 72, BoundingHeight = 12,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_analog_type);

UndertaleTexturePageItem pg_reset_config = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 122, SourceWidth = 79, SourceHeight = 12,
    TargetX = 0, TargetY = 0, TargetWidth = 79, TargetHeight = 12,
    BoundingWidth = 79, BoundingHeight = 12,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_reset_config);

UndertaleTexturePageItem pg_controls_opacity = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 61, SourceY = 136, SourceWidth = 107, SourceHeight = 13,
    TargetX = 0, TargetY = 0, TargetWidth = 107, TargetHeight = 13,
    BoundingWidth = 107, BoundingHeight = 13,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_controls_opacity);

UndertaleTexturePageItem pg_arrow_leftright = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 142, SourceY = 83, SourceWidth = 41, SourceHeight = 9,
    TargetX = 0, TargetY = 0, TargetWidth = 41, TargetHeight = 9,
    BoundingWidth = 41, BoundingHeight = 9,
    TexturePage = textures["controls.png"]
};
Data.TexturePageItems.Add(pg_arrow_leftright);

UndertaleTexturePageItem pg_black = new UndertaleTexturePageItem()
{
    Name = new UndertaleString("PageItem " + ++lastTextPageItem),
    SourceX = 0, SourceY = 0, SourceWidth = 640, SourceHeight = 480,
    TargetX = 0, TargetY = 0, TargetWidth = 640, TargetHeight = 480,
    BoundingWidth = 640, BoundingHeight = 480,
    TexturePage = textures["black.png"]
};
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
