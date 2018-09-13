// Change os_type == 14 checks in scr_draw_screen_border to always pass
// This:
// 00028: pushvar.v os_type
// 00030: pushi.e 14
// 00031: cmp.i.v EQ
// 00032: bf $+10
// Changes to:
// 00028: pushvar.v os_type
// 00030: pushi.e 14
// 00031: popz.i
// 00032: popz.v
// which is effectively a no-op
var scr_draw_screen_border = Data.Scripts.ByName("scr_draw_screen_border").Code;
for(int i = 0; i < scr_draw_screen_border.Instructions.Count; i++)
	if (scr_draw_screen_border.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		scr_draw_screen_border.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && scr_draw_screen_border.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.EQ &&
		scr_draw_screen_border.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)scr_draw_screen_border.Instructions[i-2].Value == 14 &&
		scr_draw_screen_border.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushVar && (scr_draw_screen_border.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "os_type")
	{
		scr_draw_screen_border.Instructions[i-1].Kind = UndertaleInstruction.Opcode.Popz; scr_draw_screen_border.Instructions[i-1].Type1 = UndertaleInstruction.DataType.Int32;
		scr_draw_screen_border.Instructions[i  ].Kind = UndertaleInstruction.Opcode.Popz; scr_draw_screen_border.Instructions[i  ].Type1 = UndertaleInstruction.DataType.Variable;
	}

// Same for the code that calls it
var gml_Object_obj_time_Draw_77 = Data.Code.ByName("gml_Object_obj_time_Draw_77");
for(int i = 0; i < gml_Object_obj_time_Draw_77.Instructions.Count; i++)
	if (gml_Object_obj_time_Draw_77.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_time_Draw_77.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_time_Draw_77.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_time_Draw_77.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_time_Draw_77.Instructions[i-2].Value == 3 &&
		gml_Object_obj_time_Draw_77.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_time_Draw_77.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
	{
		gml_Object_obj_time_Draw_77.Instructions[i-1].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_time_Draw_77.Instructions[i-1].Type1 = UndertaleInstruction.DataType.Int32;
		gml_Object_obj_time_Draw_77.Instructions[i  ].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_time_Draw_77.Instructions[i  ].Type1 = UndertaleInstruction.DataType.Variable;
	}

var gml_Object_obj_time_Create_0 = Data.Code.ByName("gml_Object_obj_time_Create_0");
for(int i = 0; i < gml_Object_obj_time_Create_0.Instructions.Count; i++)
	if (gml_Object_obj_time_Create_0.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_time_Create_0.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_time_Create_0.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_time_Create_0.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_time_Create_0.Instructions[i-2].Value == 3 &&
		gml_Object_obj_time_Create_0.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_time_Create_0.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
	{
		gml_Object_obj_time_Create_0.Instructions[i-1].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_time_Create_0.Instructions[i-1].Type1 = UndertaleInstruction.DataType.Int32;
		gml_Object_obj_time_Create_0.Instructions[i  ].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_time_Create_0.Instructions[i  ].Type1 = UndertaleInstruction.DataType.Variable;
	}

var gml_Object_obj_time_Draw_76 = Data.Code.ByName("gml_Object_obj_time_Draw_76");
for(int i = 0; i < gml_Object_obj_time_Draw_76.Instructions.Count; i++)
	if (gml_Object_obj_time_Draw_76.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		gml_Object_obj_time_Draw_76.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && gml_Object_obj_time_Draw_76.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.GTE &&
		gml_Object_obj_time_Draw_76.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)gml_Object_obj_time_Draw_76.Instructions[i-2].Value == 4 &&
		gml_Object_obj_time_Draw_76.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushGlb && (gml_Object_obj_time_Draw_76.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "osflavor")
	{
		gml_Object_obj_time_Draw_76.Instructions[i-1].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_time_Draw_76.Instructions[i-1].Type1 = UndertaleInstruction.DataType.Int32;
		gml_Object_obj_time_Draw_76.Instructions[i  ].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_time_Draw_76.Instructions[i  ].Type1 = UndertaleInstruction.DataType.Variable;
	}

var scr_draw_background_ps4 = Data.Scripts.ByName("scr_draw_background_ps4").Code;
for(int i = 0; i < scr_draw_background_ps4.Instructions.Count; i++)
	if (scr_draw_background_ps4.Instructions[i  ].Kind == UndertaleInstruction.Opcode.Bf      &&
		scr_draw_background_ps4.Instructions[i-1].Kind == UndertaleInstruction.Opcode.Cmp     && scr_draw_background_ps4.Instructions[i-1].ComparisonKind == UndertaleInstruction.ComparisonType.EQ &&
		scr_draw_background_ps4.Instructions[i-2].Kind == UndertaleInstruction.Opcode.PushI   && (short)scr_draw_background_ps4.Instructions[i-2].Value == 14 &&
		scr_draw_background_ps4.Instructions[i-3].Kind == UndertaleInstruction.Opcode.PushVar && (scr_draw_background_ps4.Instructions[i-3].Value as UndertaleInstruction.Reference<UndertaleVariable>).Target.Name.Content == "os_type")
	{
		scr_draw_background_ps4.Instructions[i-1].Kind = UndertaleInstruction.Opcode.Popz; scr_draw_background_ps4.Instructions[i-1].Type1 = UndertaleInstruction.DataType.Int32;
		scr_draw_background_ps4.Instructions[i  ].Kind = UndertaleInstruction.Opcode.Popz; scr_draw_background_ps4.Instructions[i  ].Type1 = UndertaleInstruction.DataType.Variable;
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
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i-1].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_settingsmenu_Draw_0.Instructions[i-1].Type1 = UndertaleInstruction.DataType.Int32;
		gml_Object_obj_settingsmenu_Draw_0.Instructions[i  ].Kind = UndertaleInstruction.Opcode.Popz; gml_Object_obj_settingsmenu_Draw_0.Instructions[i  ].Type1 = UndertaleInstruction.DataType.Variable;
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

// Load border textures
// https://www.reddit.com/r/Underminers/comments/99bxxz/after_days_of_searching_i_finally_managed_to_find/e4nnx6s/
Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();
foreach(var path in Directory.EnumerateFiles(@"C:\Users\krzys\Documents\Visual Studio 2017\Projects\UndertaleModTool\Test\bin\Debug\Borders"))
{
	UndertaleEmbeddedTexture newtex = new UndertaleEmbeddedTexture();
	newtex.TextureData.TextureBlob = File.ReadAllBytes(path);
	Data.EmbeddedTextures.Add(newtex);
	textures.Add(Path.GetFileName(path), newtex);
}

// Create texture fragments and assign them to existing (but empty) backgrounds
Action<string, UndertaleEmbeddedTexture> AssignBorderBackground = (name, tex) => {
	UndertaleTexturePageItem tpag = new UndertaleTexturePageItem();
	tpag.SourceX = 4; tpag.SourceY = 4; tpag.SourceWidth = 960; tpag.SourceHeight = 544;
	tpag.TargetX = 0; tpag.TargetY = 0; tpag.TargetWidth = 960; tpag.TargetHeight = 544;
	tpag.BoundingWidth = 960; tpag.BoundingHeight = 544;
	tpag.TexturePage = tex;
	Data.TexturePageItems.Add(tpag);
	Data.Backgrounds.ByName(name + "_544").Texture = tpag;
	
	UndertaleTexturePageItem tpag2 = new UndertaleTexturePageItem();
	tpag2.SourceX = 4; tpag2.SourceY = 4; tpag2.SourceWidth = 960; tpag2.SourceHeight = 544;
	tpag2.TargetX = 0; tpag2.TargetY = 0; tpag2.TargetWidth = 1920; tpag2.TargetHeight = 1080;
	tpag2.BoundingWidth = 1920; tpag2.BoundingHeight = 1080;
	tpag2.TexturePage = tex;
	Data.TexturePageItems.Add(tpag2);
	Data.Backgrounds.ByName(name + "_1080").Texture = tpag2;
};

AssignBorderBackground("bg_border_castle", textures["12.png"]);
AssignBorderBackground("bg_border_fire", textures["13.png"]);
AssignBorderBackground("bg_border_line", textures["14.png"]);
AssignBorderBackground("bg_border_rad", textures["15.png"]);
AssignBorderBackground("bg_border_ruins", textures["16.png"]);
AssignBorderBackground("bg_border_sepia", textures["17.png"]); // TODO: the small thingies
AssignBorderBackground("bg_border_truelab", textures["18.png"]);
AssignBorderBackground("bg_border_tundra", textures["19.png"]);
AssignBorderBackground("bg_border_water1", textures["20.png"]);
AssignBorderBackground("bg_border_water2", textures["20.png"]); // TODO: are we missing one...?