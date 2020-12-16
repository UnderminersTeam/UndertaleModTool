// Imports and unlocks border images into PC version of the game

EnsureDataLoaded();

ScriptMessage("Border enabler (1080p edition)\nby krzys_h");

// Change os_type == 14 checks in scr_draw_screen_border to always pass
// This:
// 00028: pushbltn.v os_type
// 00030: pushi.e 14
// 00031: cmp.i.v EQ
// 00032: bf $+10
// Changes to:
// 00028: pushbltn.v os_type
// 00030: pushi.e 14
// 00031: popz.i
// 00032: popz.v
// which is effectively a no-op
var scr_draw_screen_border = Data.Scripts.ByName("scr_draw_screen_border").Code;
for(int i = 0; i < scr_draw_screen_border.Instructions.Count; i++)
	if (scr_draw_screen_border.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		scr_draw_screen_border.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && scr_draw_screen_border.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.EQ &&
		scr_draw_screen_border.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)scr_draw_screen_border.Instructions[i-2].Value == 14 &&
		scr_draw_screen_border.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushBltn && (scr_draw_screen_border.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "os_type")
	{
        scr_draw_screen_border.Instructions[i-1] = Assembler.AssembleOne("popz.i", Data);
        scr_draw_screen_border.Instructions[i  ] = Assembler.AssembleOne("popz.v", Data);
	}

// Same for the code that calls it
var gml_Object_obj_time_Draw_77 = Data.Code.ByName("gml_Object_obj_time_Draw_77");
for(int i = 0; i < gml_Object_obj_time_Draw_77.Instructions.Count; i++)
	if (gml_Object_obj_time_Draw_77.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_time_Draw_77.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_time_Draw_77.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_time_Draw_77.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_time_Draw_77.Instructions[i-2].Value == 3 &&
		gml_Object_obj_time_Draw_77.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_time_Draw_77.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
    {
        gml_Object_obj_time_Draw_77.Instructions[i-1] = Assembler.AssembleOne("popz.i", Data);
        gml_Object_obj_time_Draw_77.Instructions[i  ] = Assembler.AssembleOne("popz.v", Data);
    }

var gml_Object_obj_time_Create_0 = Data.Code.ByName("gml_Object_obj_time_Create_0");
for(int i = 0; i < gml_Object_obj_time_Create_0.Instructions.Count; i++)
	if (gml_Object_obj_time_Create_0.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_time_Create_0.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_time_Create_0.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_time_Create_0.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_time_Create_0.Instructions[i-2].Value == 3 &&
		gml_Object_obj_time_Create_0.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_time_Create_0.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
    {
        gml_Object_obj_time_Create_0.Instructions[i-1] = Assembler.AssembleOne("popz.i", Data);
        gml_Object_obj_time_Create_0.Instructions[i  ] = Assembler.AssembleOne("popz.v", Data);
    }

var gml_Object_obj_time_Draw_76 = Data.Code.ByName("gml_Object_obj_time_Draw_76");
for(int i = 0; i < gml_Object_obj_time_Draw_76.Instructions.Count; i++)
	if (gml_Object_obj_time_Draw_76.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_time_Draw_76.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_time_Draw_76.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_time_Draw_76.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_time_Draw_76.Instructions[i-2].Value == 4 &&
		gml_Object_obj_time_Draw_76.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_time_Draw_76.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
    {
        gml_Object_obj_time_Draw_76.Instructions[i-1] = Assembler.AssembleOne("popz.i", Data);
        gml_Object_obj_time_Draw_76.Instructions[i  ] = Assembler.AssembleOne("popz.v", Data);
    }

var scr_draw_background_ps4 = Data.Scripts.ByName("scr_draw_background_ps4").Code;
for(int i = 0; i < scr_draw_background_ps4.Instructions.Count; i++)
	if (scr_draw_background_ps4.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		scr_draw_background_ps4.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && scr_draw_background_ps4.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.EQ &&
		scr_draw_background_ps4.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)scr_draw_background_ps4.Instructions[i-2].Value == 14 &&
		scr_draw_background_ps4.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushBltn && (scr_draw_background_ps4.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "os_type")
    {
        scr_draw_background_ps4.Instructions[i-1] = Assembler.AssembleOne("popz.i", Data);
        scr_draw_background_ps4.Instructions[i  ] = Assembler.AssembleOne("popz.v", Data);
    }

// Now, patch the settings menu!
var gml_Object_obj_settingsmenu_Draw_0 = Data.Code.ByName("gml_Object_obj_settingsmenu_Draw_0");
for(int i = 0; i < gml_Object_obj_settingsmenu_Draw_0.Instructions.Count; i++)
{
	if (gml_Object_obj_settingsmenu_Draw_0.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_settingsmenu_Draw_0.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_settingsmenu_Draw_0.Instructions[i-2].Value == 4 &&
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_settingsmenu_Draw_0.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
    {
        gml_Object_obj_settingsmenu_Draw_0.Instructions[i-1] = Assembler.AssembleOne("popz.i", Data);
        gml_Object_obj_settingsmenu_Draw_0.Instructions[i  ] = Assembler.AssembleOne("popz.v", Data);
    }
	
	//00568: pushglb.v osflavor
	//00570: pushi.e 2
	//00571: cmp.i.v LTE
	//00572: bf $+12
	if (gml_Object_obj_settingsmenu_Draw_0.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_settingsmenu_Draw_0.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.LTE &&
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_settingsmenu_Draw_0.Instructions[i-2].Value == 2 &&
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_settingsmenu_Draw_0.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
	{
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i  ].Kind = UndertaleInstruction.Opcode.B; // TODO: yeah, I'm leaving junk on the stack, I don't want to waste time figuring out how to avoid that in an odd number of instructions :P
	}
}

// 00188: pushglb.v osflavor
// 00190: pushi.e 4
// 00191: cmp.i.v GTE
// 00192: conv.b.v 
// 00193: call.i scr_enable_screen_border(argc=1)
var gml_Object_obj_time_Step_1 = Data.Code.ByName("gml_Object_obj_time_Step_1");
for(int i = 0; i < gml_Object_obj_time_Step_1.Instructions.Count; i++)
	if (gml_Object_obj_time_Step_1.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Call    && gml_Object_obj_time_Step_1.Instructions[i  ].Function.Target.Name.Content == "scr_enable_screen_border" &&
		gml_Object_obj_time_Step_1.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Conv    &&
		gml_Object_obj_time_Step_1.Instructions[i-2].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_time_Step_1.Instructions[i-2].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_time_Step_1.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_time_Step_1.Instructions[i-3].Value == 4 &&
		gml_Object_obj_time_Step_1.Instructions[i-4].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_time_Step_1.Instructions[i-4].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
    {
      gml_Object_obj_time_Step_1.Instructions[i-2] = Assembler.AssembleOne("pushi.e 1", Data);
		// TODO: junk on stack again
	}

// Also resize the window so that the border can be seen without going fullscreen
Data.Functions.EnsureDefined("window_set_size", Data.Strings);
gml_Object_obj_time_Create_0.AppendGML("window_set_size(960, 540);", Data);

// Load border textures
string bordersPath = new Uri(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + "\\SampleScripts\\Borders\\").LocalPath;

Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();
if (!Directory.Exists(bordersPath))
{
	throw new Exception("Border textures not found??");
}

// throw new Exception(bordersPath);

foreach(var path in Directory.EnumerateFiles(bordersPath))
{
	UndertaleEmbeddedTexture newtex = new UndertaleEmbeddedTexture();
	newtex.TextureData.TextureBlob = File.ReadAllBytes(path);
	Data.EmbeddedTextures.Add(newtex);
	textures.Add(Path.GetFileName(path), newtex);
}

// Create texture fragments and assign them to existing (but empty) backgrounds
Action<string, UndertaleEmbeddedTexture, ushort, ushort, ushort, ushort> AssignBorderBackground = (name, tex, x, y, width, height) => {
	var bg = Data.Backgrounds.ByName(name);
	if (bg == null) {
		// The anime border does not exist on PC yet ;)
		return;
	}
	UndertaleTexturePageItem tpag = new UndertaleTexturePageItem();
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