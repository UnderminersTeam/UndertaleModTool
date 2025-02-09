// Imports and unlocks border images into PC version of the game

using UndertaleModLib.Util;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}


//ScriptError("Script under reconstruction to use profile system.");
//return;

ScriptMessage("Border enabler (1080p edition)\nby krzys_h");

// Change os_type == 14 checks in scr_draw_screen_border to always pass
ReplaceTextInGML("gml_Script_scr_draw_screen_border", @"os_type == os_psvita", "0", true);
ReplaceTextInGML("gml_Script_scr_draw_screen_border", @"os_type == os_ps4", "1", true);

// Same for the code that calls it
ReplaceTextInGML("gml_Object_obj_time_Draw_77", @"global.osflavor >= 3", "1", true);

//Remove checks from obj_time creation event
ReplaceTextInGML("gml_Object_obj_time_Create_0", @"os_type == os_psvita", "0", true);
ReplaceTextInGML("gml_Object_obj_time_Create_0", @"os_type == os_ps4", "1", true);
ReplaceTextInGML("gml_Object_obj_time_Create_0", @"global.osflavor >= 4", "1", true);
ReplaceTextInGML("gml_Object_obj_time_Create_0", @"global.osflavor >= 3", "1", true);

//Now patch out the check for the window scale, make it always be true
ReplaceTextInGML("gml_Object_obj_time_Draw_76", @"global.osflavor >= 4", "1", true);
ReplaceTextInGML("gml_Object_obj_time_Draw_76", @"os_type == os_switch_beta", "1", true);
//Attempt border display fix in gml_Object_obj_time_Draw_76

//Patch out the OS checks for gml_Script_scr_draw_background_ps4, make PS Vita always false, and PS4 always true, simplifying code.
ReplaceTextInGML("gml_Script_scr_draw_background_ps4", @"os_type == os_psvita", "0", true);
ReplaceTextInGML("gml_Script_scr_draw_background_ps4", @"os_type == os_ps4", "1", true);

// Now, patch the settings menu!
ReplaceTextInGML("gml_Object_obj_settingsmenu_Draw_0", @"obj_time.j_ch > 0", "0", true);
ReplaceTextInGML("gml_Object_obj_settingsmenu_Draw_0", @"global.osflavor <= 2", "0", true);
ReplaceTextInGML("gml_Object_obj_settingsmenu_Draw_0", @"global.osflavor >= 4", "1", true);

//Remove code not applicable (PS Vita, Windows, <=2) and make some code always true (global.osflavor >= 4)
ReplaceTextInGML("gml_Object_obj_time_Step_1", @"os_type == os_psvita", "0", true);
ReplaceTextInGML("gml_Object_obj_time_Step_1", @"global.osflavor <= 2", "0", true);
ReplaceTextInGML("gml_Object_obj_time_Step_1", @"global.osflavor == 1", "0", true);
ReplaceTextInGML("gml_Object_obj_time_Step_1", @"global.osflavor >= 4", "1", true);

// Also resize the window so that the border can be seen without going fullscreen
Data.Functions.EnsureDefined("window_set_size", Data.Strings);
Data.Code.ByName("gml_Object_obj_time_Create_0").AppendGML("window_set_size(960, 540);", Data);

// Load border textures
string bordersPath = Path.Combine(Path.GetDirectoryName(ScriptPath), "Borders");

Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();
if (!Directory.Exists(bordersPath))
{
    throw new ScriptException("Border textures not found??");
}

// throw new ScriptException(bordersPath);
int lastTextPage = Data.EmbeddedTextures.Count - 1;
int lastTextPageItem = Data.TexturePageItems.Count - 1;

foreach (var path in Directory.EnumerateFiles(bordersPath))
{
    UndertaleEmbeddedTexture newtex = new UndertaleEmbeddedTexture();
    newtex.Name = new UndertaleString($"Texture {++lastTextPage}");
    newtex.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(path)); // Possibly other formats than PNG in the future, but no Undertale versions currently have them
    Data.EmbeddedTextures.Add(newtex);
    textures.Add(Path.GetFileName(path), newtex);
}

// Create texture fragments and assign them to existing (but empty) backgrounds
Action<string, UndertaleEmbeddedTexture, ushort, ushort, ushort, ushort> AssignBorderBackground = (name, tex, x, y, width, height) => 
{
    var bg = Data.Backgrounds.ByName(name);
    if (bg is null) 
    {
        // The anime border does not exist on PC yet ;)
        return;
    }
    UndertaleTexturePageItem tpag = new UndertaleTexturePageItem();
    tpag.Name = new UndertaleString($"PageItem {++lastTextPageItem}");
    tpag.SourceX = x; tpag.SourceY = y; tpag.SourceWidth = width; tpag.SourceHeight = height;
    tpag.TargetX = 0; tpag.TargetY = 0; tpag.TargetWidth = width; tpag.TargetHeight = height;
    tpag.BoundingWidth = width; tpag.BoundingHeight = height;
    tpag.TexturePage = tex;
    Data.TexturePageItems.Add(tpag);
    bg.Texture = tpag;
};

AssignBorderBackground("bg_border_anime_1080",      textures["bg_border_anime.png"],   0, 0, 1920, 1080);
AssignBorderBackground("bg_border_castle_1080",     textures["bg_border_castle.png"],  0, 0, 1920, 1080);
AssignBorderBackground("bg_border_dog_1080",        textures["bg_border_dog.png"],     0, 0, 1920, 1080);
AssignBorderBackground("bg_border_fire_1080",       textures["bg_border_fire.png"],    0, 0, 1920, 1080);
AssignBorderBackground("bg_border_line_1080",       textures["bg_border_line.png"],    0, 0, 1920, 1080);
AssignBorderBackground("bg_border_rad_1080",        textures["bg_border_rad.png"],     0, 0, 1920, 1080);
AssignBorderBackground("bg_border_ruins_1080",      textures["bg_border_ruins.png"],   0, 0, 1920, 1080);
AssignBorderBackground("bg_border_sepia_1080",      textures["bg_border_sepia.png"],   114, 38, 1920, 1080);
AssignBorderBackground("bg_border_sepia_1080_1a",   textures["bg_border_sepia.png"],   2, 1750, 137, 137);
AssignBorderBackground("bg_border_sepia_1080_1b",   textures["bg_border_sepia.png"],   2, 1606, 137, 137);
AssignBorderBackground("bg_border_sepia_1080_2a",   textures["bg_border_sepia.png"],   2, 562, 92, 87);
AssignBorderBackground("bg_border_sepia_1080_2b",   textures["bg_border_sepia.png"],   2, 470, 92, 87);
AssignBorderBackground("bg_border_sepia_1080_3a",   textures["bg_border_sepia.png"],   2, 162, 47, 117);
AssignBorderBackground("bg_border_sepia_1080_3b",   textures["bg_border_sepia.png"],   2, 38, 47, 117);
AssignBorderBackground("bg_border_sepia_1080_4a",   textures["bg_border_sepia.png"],   2, 1150, 91, 107);
AssignBorderBackground("bg_border_sepia_1080_4b",   textures["bg_border_sepia.png"],   2, 1038, 91, 107);
AssignBorderBackground("bg_border_sepia_1080_5a",   textures["bg_border_sepia.png"],   2, 750, 97, 92);
AssignBorderBackground("bg_border_sepia_1080_5b",   textures["bg_border_sepia.png"],   2, 654, 97, 92);
AssignBorderBackground("bg_border_sepia_1080_6a",   textures["bg_border_sepia.png"],   2, 942, 107, 91);
AssignBorderBackground("bg_border_sepia_1080_6b",   textures["bg_border_sepia.png"],   2, 846, 107, 91);
AssignBorderBackground("bg_border_sepia_1080_7a",   textures["bg_border_sepia.png"],   2, 378, 87, 87);
AssignBorderBackground("bg_border_sepia_1080_7b",   textures["bg_border_sepia.png"],   2, 286, 87, 87);
AssignBorderBackground("bg_border_sepia_1080_8a",   textures["bg_border_sepia.png"],   2, 1366, 102, 97);
AssignBorderBackground("bg_border_sepia_1080_8b",   textures["bg_border_sepia.png"],   2, 1262, 102, 97);
AssignBorderBackground("bg_border_sepia_1080_9a",   textures["bg_border_sepia.png"],   118, 2, 112, 31);
AssignBorderBackground("bg_border_sepia_1080_9b",   textures["bg_border_sepia.png"],   2, 2, 112, 31);
AssignBorderBackground("bg_border_sepia_1080_glow", textures["bg_border_sepia.png"],   2, 1470, 137, 132);
AssignBorderBackground("bg_border_truelab_1080",    textures["bg_border_truelab.png"], 0, 0, 1920, 1080);
AssignBorderBackground("bg_border_tundra_1080",     textures["bg_border_tundra.png"],  0, 0, 1920, 1080);
AssignBorderBackground("bg_border_water1_1080",     textures["bg_border_water1.png"],  0, 0, 1920, 1080);

ChangeSelection(Data.Backgrounds.ByName("bg_border_water1_1080"));
ScriptMessage("Borders loaded and enabled!");