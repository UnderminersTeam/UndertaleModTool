// Uses add a new room script components made by Lil'Alien (Last updated 21/12/21) [Nice date]
// Converted to a dialogue editor script application by Grossley on January 4th, 2022.

using System.IO;
using System;
using System.Drawing;
using System.Windows.Forms;
using UndertaleModLib.Util;

EnsureDataLoaded();

ScriptMessage(@"Undertale Dialog Simulator mod made by Lil'Alien.
Adapted to UMT script format by Grossley.");

string GameName = Data?.GeneralInfo?.DisplayName?.Content.ToLower();
if (GameName == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (GameName == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
if (Data.GeneralInfo.Name.Content == "NXTALE" || Data.GeneralInfo.Name.Content.StartsWith("UNDERTALE")) 
{
	if (!ScriptQuestion("Would you like to apply this mod?"))
	{
		return;
	}
}
else if (Data.GeneralInfo.DisplayName.Content == "SURVEY_PROGRAM" || Data.GeneralInfo.DisplayName.Content == "DELTARUNE Chapter 1")
{
    ScriptError("Error 2: This script is not compatible with Deltarune Chapter 1 (2018) yet.");
    return;
}
else
{
    ScriptError("This script can only be used with\nUndertale.", "Not Undertale");
    return;
}

bool GMS1_mode = !Data.IsGameMaker2();
bool GMS2_3_mode = Data.IsVersionAtLeast(2, 3);
bool isDeltarune = false;

if (Data.GeneralInfo.Name.ToString() == "\"DELTARUNE\"")
    isDeltarune = true;

string newRoomName = "room_dialoguer";    
var roomCheck = Data.Rooms.ByName(newRoomName);
if (roomCheck != null)
{
    ScriptError(newRoomName + " already exists.");
    return;
}
var objCheck = Data.GameObjects.ByName("obj_dialog_sim");
if (objCheck != null)
{
    ScriptError("Object 'obj_dialog_sim' already exists.");
    return;
}
    
SetUpDialogueObject();

//ScriptMessage("Adding new room...");
uint last_layer_id = GetLastLayerID();
HandleAddingNewRoom();
AppendGML("gml_Object_obj_dialoguer_Create_0", @"gs = noone
if (room == room_dialoguer)
{
    if (obj_dialog_sim.menu_x[3] == 1)
    {
        gs = scr_marker(0, 0, spr_pixwht)
        gs.image_blend = c_lime
        gs.image_xscale = room_width
        gs.image_yscale = room_height
    }
}
");

AppendGML("gml_Object_obj_dialoguer_Destroy_0", @"if instance_exists(gs)
{
    with (gs)
        instance_destroy()
}
");

AppendGML("gml_Object_obj_screen_Other_4", @"if (room == room_dialoguer)
    window_set_caption(""UNDERTALE Dialog Simulator"")
");

ReplaceTextInGML("gml_Object_obj_time_Step_1", "room_goto_next()", "room_goto(room_dialoguer)", true, false);
ImportGMLStringEx("gml_Script_scr_msgup", @"
if (room != room_dialoguer)
{
"
+ GetDecompiledText("gml_Script_scr_msgup")
+ @"
}
else
{
    stringno++
    originalstring = scr_replace_buttons_pc(mystring[stringno])
    stringpos = 0
    halt = false
    alarm[0] = textspeed
}
");

ScriptMessage(@"Undertale Dialog Simulator is set up.

Press P to replay dialogue without modifying text.");
// Internal stuff

public void HandleAddingNewRoom()
{
    UndertaleRoom newRoom = new UndertaleRoom();
    newRoom.Name = Data.Strings.MakeString(newRoomName);
    newRoom.Width = 320;
    newRoom.Height = 240;
    newRoom.Top = (uint)0;
    newRoom.Left = (uint)0;
    newRoom.Right = (uint)1024;
    newRoom.Bottom = (uint)768;
    newRoom.Speed = (uint)(GMS1_mode ? 30 : 0);
    newRoom.Flags = (UndertaleRoom.RoomEntryFlags.EnableViews | UndertaleRoom.RoomEntryFlags.ShowColor);
    if (!GMS1_mode)
        newRoom.Flags = (newRoom.Flags | UndertaleRoom.RoomEntryFlags.IsGMS2);
    if (GMS2_3_mode)
        newRoom.Flags = (newRoom.Flags | UndertaleRoom.RoomEntryFlags.IsGMS2_3);

    Data.Rooms.Add(newRoom);

    newRoom.Views[0].ViewWidth = (isDeltarune ? 640 : 320);
    newRoom.Views[0].ViewHeight = (isDeltarune ? 480 : 240);
    newRoom.Views[0].ViewX = 0;
    newRoom.Views[0].ViewY = 0;
    newRoom.Views[0].PortWidth = 640;
    newRoom.Views[0].PortHeight = 480;
    newRoom.Views[0].PortX = 0;
    newRoom.Views[0].PortY = 0;
    newRoom.Views[0].BorderX = (uint)(isDeltarune ? 160 : 32);
    newRoom.Views[0].BorderY = (uint)(isDeltarune ? 240 : 32);
    newRoom.Views[0].Enabled = true;
    newRoom.Width = (uint)(isDeltarune ? 640 : 320);
    newRoom.Height = (uint)(isDeltarune ? 480 : 240);

    if (Data.GameObjects.ByName("obj_mainchara") != null && (!isDeltarune))
        newRoom.Views[0].ObjectId = Data.GameObjects.ByName("obj_mainchara");
    UndertaleRoom.Layer newInstancesLayer = new UndertaleRoom.Layer();
    if (!(GMS1_mode))
    {
        newInstancesLayer.LayerName = Data.Strings.MakeString(newRoomName + "_GameObjects_Layer");
        newInstancesLayer.LayerId = last_layer_id++;
        newInstancesLayer.LayerType = UndertaleRoom.LayerType.Instances;
        newInstancesLayer.IsVisible = true;
        newInstancesLayer.Data = Activator.CreateInstance<UndertaleRoom.Layer.LayerInstancesData>();
        
        newRoom.Layers.Add(newInstancesLayer);
        
        newRoom.SetupRoom();
    }

    if (Data.GameObjects.ByName("obj_mainchara") != null)
    {
        UndertaleRoom.GameObject newPlayerObj = new UndertaleRoom.GameObject();
        newPlayerObj.X = 160;
        newPlayerObj.Y = 120;
        newPlayerObj.ObjectDefinition = Data.GameObjects.ByName("obj_mainchara");
        newPlayerObj.InstanceID = Data.GeneralInfo.LastObj++;
        UndertaleRoom.GameObject obj = newPlayerObj;
        newRoom.GameObjects.Add(obj);
        if (!(GMS1_mode))
        {
            newInstancesLayer.InstancesData.Instances.Add(obj);
            newRoom.SetupRoom();
        }
    }

    if (Data.GameObjects.ByName("obj_overworldcontroller") != null)
    {
        UndertaleRoom.GameObject newControllerObj = new UndertaleRoom.GameObject();
        newControllerObj.X = 0;
        newControllerObj.Y = 0;
        newControllerObj.ObjectDefinition = Data.GameObjects.ByName("obj_overworldcontroller");
        newControllerObj.InstanceID = Data.GeneralInfo.LastObj++;
        UndertaleRoom.GameObject obj = newControllerObj;
        newRoom.GameObjects.Add(obj);
        if (!(GMS1_mode))
        {
            newInstancesLayer.InstancesData.Instances.Add(obj);
            newRoom.SetupRoom();
        }
    }

    if (Data.GameObjects.ByName("obj_dialog_sim") != null)
    {
        UndertaleRoom.GameObject newPlayerObj = new UndertaleRoom.GameObject();
        newPlayerObj.X = 160;
        newPlayerObj.Y = 120;
        newPlayerObj.ObjectDefinition = Data.GameObjects.ByName("obj_dialog_sim");
        newPlayerObj.InstanceID = Data.GeneralInfo.LastObj++;
        UndertaleRoom.GameObject obj = newPlayerObj;
        newRoom.GameObjects.Add(obj);
        if (!(GMS1_mode))
        {
            newInstancesLayer.InstancesData.Instances.Add(obj);
            newRoom.SetupRoom();
        }
    }

    /*
    if (Data.Backgrounds.ByName("bg_ruinseasynam3") != null && GMS1_mode)
    {
        UndertaleRoom.Tile tile = new UndertaleRoom.Tile();
        tile.InstanceID = Data.GeneralInfo.LastTile++;
        tile.BackgroundDefinition = Data.Backgrounds.ByName("bg_ruinseasynam3");
        tile.X = 240;
        tile.Y = 10;
        tile.SourceX = 20;
        tile.SourceY = 240;
        tile.Width = 20;
        tile.Height = 20;
        tile.TileDepth = 1000000;
        newRoom.Tiles.Add(tile);
    }
    */
    if (Data.GameObjects.ByName("obj_darkcontroller") != null && !GMS1_mode)
    {
        UndertaleRoom.GameObject newControllerObj = new UndertaleRoom.GameObject();
        newControllerObj.X = 0;
        newControllerObj.Y = 0;
        newControllerObj.ObjectDefinition = Data.GameObjects.ByName("obj_darkcontroller");
        newControllerObj.InstanceID = Data.GeneralInfo.LastObj++;
        UndertaleRoom.GameObject obj = newControllerObj;
        newRoom.GameObjects.Add(obj);
        newInstancesLayer.InstancesData.Instances.Add(obj);
        newRoom.SetupRoom();
    }

    if (!GMS1_mode && !GMS2_3_mode) // Adding new tile layer crashes it
    {
        var newLayer1 = AddNewTileLayer(newRoomName + "_Tiles_Layer_-10000", -10000, newRoom);
        var newLayer2 = AddNewTileLayer(newRoomName + "_Tiles_Layer_-9990", -9990, newRoom);
        var newLayer3 = AddNewTileLayer(newRoomName + "_Tiles_Layer_999990", 999990, newRoom);
        var newLayer4 = AddNewTileLayer(newRoomName + "_Tiles_Layer_1000000", 1000000, newRoom);
        /*
        if (Data.Sprites.ByName("bg_ruinseasynam3") != null)
        {
            AddNewTile(Data.Sprites.ByName("bg_ruinseasynam3"), Data.ByName("bg_ruinseasynam3"), 40, 10, 40, 0, 20, 20, 1, 1, 1000000, newLayer4);
            AddNewTile(Data.Sprites.ByName("bg_ruinseasynam3"), Data.ByName("bg_ruinseasynam3"), 70, 10, 20, 240, 20, 20, 1, 1, 999990, newLayer3);
            AddNewTile(Data.Sprites.ByName("bg_ruinseasynam3"), Data.ByName("bg_ruinseasynam3"), 100, 10, 120, 260, 20, 20, 1, 1, -9990, newLayer2);
            AddNewTile(Data.Sprites.ByName("bg_ruinseasynam3"), Data.ByName("bg_ruinseasynam3"), 130, 10, 120, 220, 20, 20, 1, 1, -10000, newLayer1);
        }
        if (Data.Sprites.ByName("bg_darktiles1") != null)
        {
            AddNewTile(Data.Sprites.ByName("bg_darktiles1"), Data.ByName("bg_darktiles1"), 20, 20, 0, 40, 40, 40, 1, 1, 1000000, newLayer4);
            AddNewTile(Data.Sprites.ByName("bg_darktiles1"), Data.ByName("bg_darktiles1"), 80, 20, 0, 120, 40, 40, 1, 1, 999990, newLayer3);
            AddNewTile(Data.Sprites.ByName("bg_darktiles1"), Data.ByName("bg_darktiles1"), 140, 20, 80, 360, 40, 40, 1, 1, -9990, newLayer2);
            AddNewTile(Data.Sprites.ByName("bg_darktiles1"), Data.ByName("bg_darktiles1"), 200, 20, 160, 0, 40, 40, 1, 1, -10000, newLayer1);
        }
        */
    }

    if (!GMS1_mode)
    {
        UndertaleRoom.Layer newBackgroundLayer = new UndertaleRoom.Layer();
        newBackgroundLayer.LayerName = Data.Strings.MakeString(newRoomName + "_Background_Layer");
        newBackgroundLayer.LayerId = last_layer_id++;
        newBackgroundLayer.LayerDepth = 2147483600;
        newBackgroundLayer.LayerType = UndertaleRoom.LayerType.Background;
        newBackgroundLayer.IsVisible = true;
        
        var newBGLayerData = Activator.CreateInstance<UndertaleRoom.Layer.LayerBackgroundData>();
        newBGLayerData.Visible = true;
        newBGLayerData.Color = (uint)4278190080;
        newBGLayerData.AnimationSpeed = 15;
        
        newBackgroundLayer.Data = newBGLayerData;
        
        newRoom.Layers.Add(newBackgroundLayer);
        
        newRoom.SetupRoom();
    }

    Data.GeneralInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = newRoom });

    ChangeSelection(newRoom);

    //ScriptMessage(newRoomName + " has been added successfully!");
    
}

private UndertaleRoom.Layer AddNewTileLayer(string layername, int layerdepth, UndertaleRoom layerroom)
{
    UndertaleRoom.Layer newAssetsLayer = new UndertaleRoom.Layer();
    newAssetsLayer.LayerName = Data.Strings.MakeString(layername);
    newAssetsLayer.LayerId = last_layer_id++;
    newAssetsLayer.LayerDepth = layerdepth;
    newAssetsLayer.Data = Activator.CreateInstance<UndertaleRoom.Layer.LayerAssetsData>();
    newAssetsLayer.LayerType = UndertaleRoom.LayerType.Assets;
    newAssetsLayer.IsVisible = true;

    layerroom.Layers.Add(newAssetsLayer);

    layerroom.SetupRoom();
    
    return newAssetsLayer;
}

private void AddNewTile(UndertaleSprite tilesprite, UndertaleNamedResource tileobject, int tilex, int tiley, uint tilesourcex, uint tilesourcey, uint tilewidth, uint tileheight, float tilescalex, float tilescaley, int tiledepth, UndertaleRoom.Layer tilelayer)
{
    if (tilelayer.AssetsData.LegacyTiles == null)
        tilelayer.AssetsData.LegacyTiles = new UndertalePointerList<UndertaleRoom.Tile>();
    
    if (tilelayer.AssetsData.Sprites == null)
        tilelayer.AssetsData.Sprites = new UndertalePointerList<UndertaleRoom.SpriteInstance>();
    
    UndertaleRoom.Tile tile = new UndertaleRoom.Tile();
    tile.spriteMode = true;
    tile.X = tilex;
    tile.Y = tiley;
    tile.SpriteDefinition = tilesprite;
    tile.ObjectDefinition = tileobject;
    tile.SourceX = tilesourcex;
    tile.SourceY = tilesourcey;
    tile.Width = tilewidth;
    tile.Height = tileheight;
    tile.ScaleX = tilescalex;
    tile.ScaleY = tilescaley;
    tile.TileDepth = tiledepth;
    tile.InstanceID = Data.GeneralInfo.LastTile++;

    tilelayer.AssetsData.LegacyTiles.Add(tile);
}

public uint GetLastLayerID()
{
    uint a_last_layer_id = 0;
    foreach (UndertaleRoom Room in Data.Rooms) 
    {
        foreach (UndertaleRoom.Layer Layer in Room.Layers) 
        {
            if (Layer.LayerId > a_last_layer_id) 
                a_last_layer_id = Layer.LayerId;
        }
    }
    return a_last_layer_id;
}

public void ImportGMLStringEx(string codeName, string gmlCode, bool doParse = true, bool CheckDecompiler = false)
{
    ImportGMLString(codeName, gmlCode, doParse, CheckDecompiler);
}

public void SetUpDialogueObject()
{
UndertaleGameObject nativeOBJ = new UndertaleGameObject();
nativeOBJ.Name = Data.Strings.MakeString("obj_dialog_sim");
Data.GameObjects.Add(nativeOBJ);
    
ImportGMLStringEx("gml_Object_obj_dialog_sim_Create_0", @"
getstr = """"
global.interact = 1
global.flag[17] = 1
obj_mainchara.visible = false
obj_mainchara.y = 320
for (i = 0; i <= 90; i++)
    mystr[i] = ""%%""
cflash = 0
cflash_timer = 0
mydialoguer = -4
menu_x[0] = 0
menu_x[1] = 0
menu_x[2] = 0
menu_x[3] = 0
menu_x[4] = 0
menu_y = 0
mytyper = 5
myface = 0
depth = 10
if ossafe_file_exists(""dialog_sim"")
{
    mysave = ossafe_file_text_open_read(""dialog_sim"")
    menu_x[1] = ossafe_file_text_read_real(mysave)
    ossafe_file_text_readln(mysave)
    menu_x[2] = ossafe_file_text_read_real(mysave)
    ossafe_file_text_readln(mysave)
    mytyper = ossafe_file_text_read_real(mysave)
    ossafe_file_text_readln(mysave)
    myface = ossafe_file_text_read_real(mysave)
    ossafe_file_text_readln(mysave)
    for (i = 0; i <= 90; i++)
    {
        mystr[i] = ossafe_file_text_read_string(mysave)
        ossafe_file_text_readln(mysave)
    }
    ossafe_file_text_close(mysave)
}
help = 0
help_yoff = 0
");
ImportGMLStringEx("gml_Object_obj_dialog_sim_Draw_0", @"
var disstr, facetext, changetext, oporig;
draw_set_colour(c_white)
draw_set_alpha(1)
draw_set_font(fnt_maintext)
disstr = mystr[menu_x[0]]
draw_set_colour(c_gray)
draw_text(5, 83, ""Press Backspace to input and preview dialog."")
draw_set_halign(fa_right)
draw_set_valign(fa_bottom)
draw_text(318, 238, ""Press F1 for dialog help!"")
draw_set_halign(fa_left)
draw_set_valign(fa_top)
draw_set_colour(c_white)
draw_text_ext(5, 98, disstr, 12, 310)
draw_set_colour(c_lime)
oporig = 150
if (menu_y != -2)
    draw_text(5, (oporig + (menu_y * 14)), "">"")
if (menu_y == 0)
    draw_set_colour(c_lime)
else
    draw_set_colour(c_green)
draw_text(16, oporig, ""Message:"")
if (menu_y == 0)
{
    draw_set_colour(c_white)
    draw_text(117, oporig, (string(menu_x[0]) + "" < >""))
}
else
{
    draw_set_colour(c_gray)
    draw_text(117, oporig, string(menu_x[0]))
}
if (menu_y == 1)
    draw_set_colour(c_lime)
else
    draw_set_colour(c_green)
draw_text(16, (oporig + 14), ""First Speaker:"")
facetext = ""Bepis""
switch menu_x[1]
{
    case 0:
        facetext = ""Default""
        mytyper = 5
        myface = 0
        break
    case 1:
        facetext = ""Chara""
        mytyper = 106
        myface = 0
        break
    case 2:
        facetext = ""Toriel""
        mytyper = 4
        myface = 1
        break
    case 3:
        facetext = ""Toriel (No face)""
        mytyper = 4
        myface = 0
        break
    case 4:
        facetext = ""Flowey""
        mytyper = 9
        myface = 2
        break
    case 5:
        facetext = ""Flowey (No face)""
        mytyper = 9
        myface = 0
        break
    case 6:
        facetext = ""Flowey (Evil)""
        mytyper = 16
        myface = 2
        break
    case 7:
        facetext = ""Flowey (Evil, No face)""
        mytyper = 16
        myface = 0
        break
    case 8:
        facetext = ""Sans""
        mytyper = 17
        myface = 3
        break
    case 9:
        facetext = ""Sans (No face)""
        mytyper = 17
        myface = 0
        break
    case 10:
        facetext = ""Sans (Serious)""
        mytyper = 112
        myface = 3
        break
    case 11:
        facetext = ""Sans (Toriel voice)""
        mytyper = 48
        myface = 3
        break
    case 12:
        facetext = ""Sans (Toriel voice, No face)""
        mytyper = 112
        myface = 0
        break
    case 13:
        facetext = ""Papyrus""
        mytyper = 19
        myface = 4
        break
    case 14:
        facetext = ""Papyrus (No face)""
        mytyper = 19
        myface = 0
        break
    case 15:
        facetext = ""Undyne""
        mytyper = 37
        myface = 5
        break
    case 16:
        facetext = ""Undyne (No face)""
        mytyper = 37
        myface = 0
        break
    case 17:
        facetext = ""Alphys""
        mytyper = 47
        myface = 6
        break
    case 18:
        facetext = ""Alphys (No face)""
        mytyper = 47
        myface = 0
        break
    case 19:
        facetext = ""Asgore""
        mytyper = 60
        myface = 7
        break
    case 20:
        facetext = ""Asgore (No face)""
        mytyper = 60
        myface = 0
        break
    case 21:
        facetext = ""Mettaton""
        mytyper = 27
        myface = 8
        break
    case 22:
        facetext = ""Mettaton (No face)""
        mytyper = 27
        myface = 0
        break
    case 23:
        facetext = ""Asriel""
        mytyper = 89
        myface = 9
        break
    case 24:
        facetext = ""Asriel (No face)""
        mytyper = 89
        myface = 0
        break
    case 25:
        facetext = ((""[Er"" + chr(irandom_range(32, 126))) + ""or]"")
        mytyper = 666
        myface = 0
        break
    case 26:
        facetext = ((""[Er"" + chr(irandom_range(32, 126))) + ""or2]"")
        mytyper = 34
        myface = 0
        break
}

if (menu_y == 1)
{
    draw_set_colour(c_white)
    if (menu_x[1] == 25 || menu_x[1] == 26)
        draw_set_colour(c_red)
    draw_text(117, (oporig + 14), (facetext + "" < >""))
}
else
{
    draw_set_colour(c_gray)
    draw_text(117, (oporig + 14), facetext)
}
if (menu_y == 2)
    draw_set_colour(c_lime)
else
    draw_set_colour(c_green)
draw_text(16, (oporig + 28), ""First Emotion:"")
if (menu_y == 2)
{
    draw_set_colour(c_white)
    draw_text(117, (oporig + 28), (string(menu_x[2]) + "" < >""))
}
else
{
    draw_set_colour(c_gray)
    draw_text(117, (oporig + 28), string(menu_x[2]))
}
if (menu_y == 3)
    draw_set_colour(c_lime)
else
    draw_set_colour(c_green)
draw_text(16, (oporig + 42), ""Green Screen:"")
if (menu_y == 3)
{
    draw_set_colour(c_white)
    if (menu_x[3] == 0)
        draw_text(117, (oporig + 42), ""Disabled < >"")
    else
        draw_text(117, (oporig + 42), ""Enabled < >"")
}
else
{
    draw_set_colour(c_gray)
    if (menu_x[3] == 0)
        draw_text(117, (oporig + 42), ""Disabled"")
    else
        draw_text(117, (oporig + 42), ""Enabled"")
}
if (menu_y == 4)
    draw_set_colour(c_lime)
else
    draw_set_colour(c_green)
draw_text(16, (oporig + 56), ""Change Speaker:"")
changetext = ""Bepis""
switch menu_x[4]
{
    case 0:
        changetext = ""No change""
        break
    case 1:
        changetext = ""Default""
        break
    case 2:
        changetext = ""Default (Silent)""
        break
    case 3:
        changetext = ""Toriel""
        break
    case 4:
        changetext = ""Flowey (Evil)""
        break
    case 5:
        changetext = ""Sans""
        break
    case 6:
        changetext = ""Sans (Toriel voice)""
        break
    case 7:
        changetext = ""Papyrus""
        break
    case 8:
        changetext = ""Undyne""
        break
    case 9:
        changetext = ""Alphys""
        break
    case 10:
        changetext = ""Asgore""
        break
    case 11:
        changetext = ""Mettaton""
        break
    case 12:
        changetext = ""Asriel""
        break
}

if (menu_y == 4)
{
    draw_set_colour(c_white)
    draw_text(117, (oporig + 56), (changetext + "" [Enter] (Replaces msg)""))
}
else
{
    draw_set_colour(c_gray)
    draw_text(117, (oporig + 56), changetext)
}
if (menu_y == 5)
{
    draw_set_colour(c_lime)
    draw_text(16, (oporig + 70), ""Export as code [Enter]"")
}
else
{
    draw_set_colour(c_green)
    draw_text(16, (oporig + 70), ""Export as code"")
}
if (menu_y == -1)
{
    draw_set_colour(c_lime)
    draw_text(16, (oporig - 14), ""Clear all messages [Enter]"")
}
else
{
    draw_set_colour(c_green)
    draw_text(16, (oporig - 14), ""Clear all messages"")
}
draw_set_colour(c_white)
if (help == 1)
{
    if (mouse_wheel_down() || keyboard_check_pressed(vk_down))
    {
        if (help_yoff < 1008)
            help_yoff += 32
    }
    if (mouse_wheel_up() || keyboard_check_pressed(vk_up))
    {
        if (help_yoff > 0)
            help_yoff -= 32
    }
    draw_set_colour(c_black)
    draw_rectangle(0, 0, room_width, room_height, false)
    draw_set_colour(c_white)
    if (help_yoff > 0)
        draw_text(310, 2, ""^"")
    if (help_yoff < 1008)
        draw_text_transformed(310, 238, ""^"", 1, -1, 0)
    draw_text(4, (4 - help_yoff), ""^1
^2
^3
^4... - Pause (Max is 9)

\R (Red)
\G (Green)
\W (White)
\Y (Yellow)
\X (Black)
\B (Blue)
\O (Orange)
\L (Azure)
\P (Magenta)
\p (Pink) - Colour text

\E0
\E1
\E2
\E3... - Face emotion (Max is 9)

\F0
\F1
\F2
\F3... - Face index (See below)
0 - None
1 - Toriel
2 - Flowey
3 - Sans
4 - Papyrus
5 - Undyne
6 - Alphys
7 - Asgore
8 - Mettaton
9 - Asriel

\C - Text choice

\S- - Disables text sound
\S+ - Enables text sound
\Sp - Plays phone sfx

\T- - Makes text tiny
\T+ - Makes text normal-sized

\T% - Change speaker
where % is some character (e.g. \Ts)
T - Toriel
t - Sans Toriel voice
0 - Default
S - Default no sound
F - Flowey Evil
s - Sans
P - Papyrus
M - Mettaton
U - Undyne
A - Alphys
a - Asgore
R - Asriel

\z4 - Writes infinity sign

\*% - Shows control button
where % is some character (e.g. \*Z)
A - Shows button bound to Left
D - Shows button bound to Right
Z - Shows button bound to Confirm
X - Shows button bound to Cancel
C - Shows button bound to Menu

\>1 - Offsets text horizontally

& - Line break
/ - Stop and wait for input
/% - Stop and wait for input, close textbox
% - Skip to next message
%% - Close textbox immediately
"")
}
");
ImportGMLStringEx("gml_Object_obj_dialog_sim_Other_10", "");
ImportGMLStringEx("gml_Object_obj_dialog_sim_Other_5", @"
mysave = ossafe_file_text_open_write(""dialog_sim"")
ossafe_file_text_write_real(mysave, menu_x[1])
ossafe_file_text_writeln(mysave)
ossafe_file_text_write_real(mysave, menu_x[2])
ossafe_file_text_writeln(mysave)
ossafe_file_text_write_real(mysave, mytyper)
ossafe_file_text_writeln(mysave)
ossafe_file_text_write_real(mysave, myface)
ossafe_file_text_writeln(mysave)
for (i = 0; i <= 90; i++)
{
    ossafe_file_text_write_string(mysave, mystr[i])
    ossafe_file_text_writeln(mysave)
}
ossafe_file_text_close(mysave)
");
ImportGMLStringEx("gml_Object_obj_dialog_sim_Step_0", @"
var mypath, myfile;
if keyboard_check_pressed(vk_f1)
{
    with (obj_base_writer)
        instance_destroy()
    with (obj_dialoguer)
        instance_destroy()
    with (obj_choicer)
        instance_destroy()
    if (help == 0)
        help = 1
    else
        help = 0
}
if (help == 0)
{
    if keyboard_check_pressed(vk_return)
    {
        if (menu_y == 4)
        {
            if (menu_x[4] != 0)
            {
                switch menu_x[4]
                {
                    case 1:
                        mystr[menu_x[0]] = ""\T0 %""
                        break
                    case 2:
                        mystr[menu_x[0]] = ""\TS %""
                        break
                    case 3:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F1 \TT %""
                        break
                    case 4:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F2 \TF %""
                        break
                    case 5:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F3 \Ts %""
                        break
                    case 6:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F3 \Tt %""
                        break
                    case 7:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F4 \TP %""
                        break
                    case 8:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F5 \TU %""
                        break
                    case 9:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F6 \TA %""
                        break
                    case 10:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F7 \Ta %""
                        break
                    case 11:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F8 \TM %""
                        break
                    case 12:
                        mystr[menu_x[0]] = ""\TS \F0 \E0 \F9 \TR %""
                        break
                }

                snd_play(snd_select)
                if (menu_x[0] < 90)
                    menu_x[0]++
            }
        }
        else if (menu_y == 5)
        {
            mypath = get_save_filename(""GML code (.gml)|*.gml|Text file (.txt)|*.txt"", ""Exported_code"")
            if (mypath != """")
            {
                myfile = ossafe_file_text_open_write(mypath)
                ossafe_file_text_write_string(myfile, (""global.typer = "" + string(mytyper)))
                ossafe_file_text_writeln(myfile)
                ossafe_file_text_write_string(myfile, (""global.facechoice = "" + string(myface)))
                ossafe_file_text_writeln(myfile)
                ossafe_file_text_write_string(myfile, (""global.faceemotion = "" + string(menu_x[2])))
                ossafe_file_text_writeln(myfile)
                ossafe_file_text_write_string(myfile, ""global.msc = 0"")
                ossafe_file_text_writeln(myfile)
                i = 0
                while (i <= 90)
                {
                    if (mystr[i] == ""%%"")
                        break
                    else
                    {
                        ossafe_file_text_write_string(myfile, (""global.msg["" + string(i) + ""] = "" + '""' + string_replace_all(string(mystr[i]), '""', "" + '"" + '""' + ""' + "") + '""'))
                        ossafe_file_text_writeln(myfile)
                        i++
                        continue
                    }
                }
                ossafe_file_text_write_string(myfile, ""mydialoguer = instance_create(0, 0, obj_dialoguer)"")
                ossafe_file_text_close(myfile)
                snd_play(snd_select)
            }
        }
        else if (menu_y == -1)
        {
            menu_x[0] = 0
            for (i = 0; i <= 90; i++)
                mystr[i] = ""%%""
            snd_play(snd_damage)
        }
    }
    if keyboard_check_pressed(ord(""P""))
    {
        if (getstr != """")
        {
            mystr[menu_x[0]] = getstr
            with (obj_base_writer)
                instance_destroy()
            with (obj_dialoguer)
                instance_destroy()
            with (obj_choicer)
                instance_destroy()
            global.typer = mytyper
            global.facechoice = myface
            global.faceemotion = menu_x[2]
            global.msc = 0
            for (i = 0; i <= 90; i++)
            {
                if (mystr[i] == """")
                    mystr[i] = ""%%""
                global.msg[i] = mystr[i]
            }
            mydialoguer = instance_create(0, 0, obj_dialoguer)
            menu_y = -2
        }
    }
    if keyboard_check_pressed(vk_backspace)
    {
        getstr = get_string(""Type in your dialog, then press OK."", mystr[menu_x[0]])
        if (getstr != """")
        {
            mystr[menu_x[0]] = getstr
            with (obj_base_writer)
                instance_destroy()
            with (obj_dialoguer)
                instance_destroy()
            with (obj_choicer)
                instance_destroy()
            global.typer = mytyper
            global.facechoice = myface
            global.faceemotion = menu_x[2]
            global.msc = 0
            for (i = 0; i <= 90; i++)
            {
                if (mystr[i] == """")
                    mystr[i] = ""%%""
                global.msg[i] = mystr[i]
            }
            mydialoguer = instance_create(0, 0, obj_dialoguer)
            menu_y = -2
        }
    }
    if keyboard_check_pressed(vk_down)
    {
        if (menu_y == -2)
        {
            with (obj_base_writer)
                instance_destroy()
            with (obj_dialoguer)
                instance_destroy()
            with (obj_choicer)
                instance_destroy()
        }
        if (menu_y < 5)
        {
            menu_y++
            snd_play(snd_squeak)
        }
    }
    else if keyboard_check_pressed(vk_up)
    {
        if (menu_y == -2)
        {
            with (obj_base_writer)
                instance_destroy()
            with (obj_dialoguer)
                instance_destroy()
            with (obj_choicer)
                instance_destroy()
            menu_y = 0
        }
        if (menu_y > -1)
        {
            menu_y--
            snd_play(snd_squeak)
        }
    }
    else if keyboard_check_pressed(vk_left)
    {
        if (menu_y == 0)
        {
            if (menu_x[0] > 0)
            {
                menu_x[0]--
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 1)
        {
            if (menu_x[1] > 0)
            {
                menu_x[1]--
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 2)
        {
            if (menu_x[2] > 0)
            {
                menu_x[2]--
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 3)
        {
            if (menu_x[3] == 1)
            {
                menu_x[3] = 0
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 4)
        {
            if (menu_x[4] > 0)
            {
                menu_x[4]--
                snd_play(snd_squeak)
            }
        }
    }
    else if keyboard_check_pressed(vk_right)
    {
        if (menu_y == 0)
        {
            if (menu_x[0] < 90)
            {
                menu_x[0]++
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 1)
        {
            if (menu_x[1] < 26)
            {
                menu_x[1]++
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 2)
        {
            if (menu_x[2] < 9)
            {
                menu_x[2]++
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 3)
        {
            if (menu_x[3] == 0)
            {
                menu_x[3] = 1
                snd_play(snd_squeak)
            }
        }
        else if (menu_y == 4)
        {
            if (menu_x[4] < 12)
            {
                menu_x[4]++
                snd_play(snd_squeak)
            }
        }
    }
}
");
}

public void AppendGML(string CodeName = "", string CodeContent = "")
{
    Data.Code.ByName(CodeName).AppendGML(CodeContent, Data);
}
