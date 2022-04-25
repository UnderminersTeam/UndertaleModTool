// This script was made by Lil'Alien (Last updated: December 5, 2020)
// Fixing and code improving by VladiStep (April 25, 2022)

using System.IO;
using System;
using System.Drawing;
using System.Windows.Forms;
using UndertaleModLib.Util;
using static UndertaleModLib.Models.UndertaleRoom;

private static DialogResult ShowNamingDialog(ref string input)
{
    Size size = new Size(300, 100);
    Form inputBox = new Form();

    inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
    inputBox.ClientSize = size;
    inputBox.Text = "Name your room";
    inputBox.StartPosition = FormStartPosition.CenterScreen;
    inputBox.MaximizeBox = false;
    inputBox.MinimizeBox = false;
    
    Label labeltxt1 = new Label();
    labeltxt1.Size = new Size(size.Width - 10, 20);
    labeltxt1.Location = new Point(5, 5);
    labeltxt1.Text = "Choose a name for your room:";
    inputBox.Controls.Add(labeltxt1);
    
    Label labeltxt2 = new Label();
    labeltxt2.Size = new Size(size.Width - 10, 20);
    labeltxt2.Location = new Point(5, 25);
    labeltxt2.ForeColor = Color.Gray;
    labeltxt2.Text = "(e.g. room_cc_joker)";
    inputBox.Controls.Add(labeltxt2);
    
    TextBox textBox = new TextBox();
    textBox.Size = new Size(size.Width - 10, 20);
    textBox.Location = new Point(5, 45);
    textBox.Text = input;
    inputBox.Controls.Add(textBox);
    
    // Buttons
    
    Button okButton = new Button();
    okButton.DialogResult = DialogResult.OK;
    okButton.Name = "okButton";
    okButton.Size = new Size(75, 23);
    okButton.Text = "&OK";
    okButton.Location = new Point((size.Width - 160), 70);
    inputBox.Controls.Add(okButton);

    Button cancelButton = new Button();
    cancelButton.DialogResult = DialogResult.Cancel;
    cancelButton.Name = "cancelButton";
    cancelButton.Size = new Size(75, 23);
    cancelButton.Text = "&Cancel";
    cancelButton.Location = new Point(size.Width - 80, 70);
    inputBox.Controls.Add(cancelButton);

    inputBox.AcceptButton = okButton;
    inputBox.CancelButton = cancelButton; 

    DialogResult result = inputBox.ShowDialog();
    input = textBox.Text;
    return result;
}

EnsureDataLoaded();

bool GMS1_mode = Data.GeneralInfo.Major < 2;

string newRoomName = "";
var newRoom = new UndertaleRoom();

var obj_mainchara = Data.GameObjects.ByName("obj_mainchara");
if (obj_mainchara is null)
    throw new ScriptException("This script is for Undertale or Deltarune.");

var obj_overworldcontroller = Data.GameObjects.ByName("obj_overworldcontroller");
var obj_overworldc = Data.GameObjects.ByName("obj_overworldc");

var bg_schooltiles = Data.Sprites.ByName("bg_schooltiles");
var bg_ruinseasynam3 = Data.Backgrounds.ByName("bg_ruinseasynam3");

while (true)
{
    var namingResult = ShowNamingDialog(ref newRoomName);
    
    if (namingResult == DialogResult.Cancel)
        return;

    if (namingResult == DialogResult.OK)
    {
        if (newRoomName == "")
            ScriptError("You must give your room a name.", "No name given");
        else
        {
            newRoomName = newRoomName.Replace(" ", "_");
            int nameLength = newRoomName.Length;

            if (nameLength < 5)
            {
                newRoomName = ("room_" + newRoomName);
            }
            else
            {
                string nameCheck = newRoomName.Substring(0,5);
                
                if (nameCheck != "room_")
                {
                    bool nameWarning = ScriptQuestion("Room name does not contain prefix [ room_ ] and may cause name conflicts.\n\nAdd prefix to its name?");

                    if (nameWarning)
                    {
                        newRoomName = ("room_" + newRoomName);
                    }
                }
            }
            
            var roomCheck = Data.Rooms.ByName(newRoomName);

            if (roomCheck != null)
                ScriptError($"\"{newRoomName}\" already exists.\nChoose a different name.", "Room already exists");
            else
                break;
        }
    }
}

var newRoomNameStr = Data.Strings.MakeString(newRoomName);

uint largest_layerid = 0;

// Find the largest layer id
// See #355
foreach (UndertaleRoom room in Data.Rooms)
{
    foreach (Layer layer in room.Layers)
    {
        if (layer.LayerId > largest_layerid)
            largest_layerid = layer.LayerId;
    }
}
largest_layerid++;

/*
private void UpdateRandomUID()
{
    Random random = new Random((int)(Data.GeneralInfo.Timestamp & 4294967295L));
    long firstRandom = (long)random.Next() << 32 | (long)random.Next();
    long infoNumber = (long)(Data.GeneralInfo.Timestamp - 1000);
    ulong temp = (ulong)infoNumber;
    temp = ((temp << 56 & 18374686479671623680UL) | (temp >> 8 & 71776119061217280UL) |
            (temp << 32 & 280375465082880UL) | (temp >> 16 & 1095216660480UL) | (temp << 8 & 4278190080UL) |
            (temp >> 24 & 16711680UL) | (temp >> 16 & 65280UL) | (temp >> 32 & 255UL));
    infoNumber = (long)temp;
    infoNumber ^= firstRandom;
    infoNumber = ~infoNumber;
    infoNumber ^= ((long)Data.GeneralInfo.GameID << 32 | (long)Data.GeneralInfo.GameID);
    infoNumber ^= ((long)(Data.GeneralInfo.DefaultWindowWidth + (int)Data.GeneralInfo.Info) << 48 |
                   (long)(Data.GeneralInfo.DefaultWindowHeight + (int)Data.GeneralInfo.Info) << 32 |
                   (long)(Data.GeneralInfo.DefaultWindowHeight + (int)Data.GeneralInfo.Info) << 16 |
                   (long)(Data.GeneralInfo.DefaultWindowWidth + (int)Data.GeneralInfo.Info));
    infoNumber ^= Data.GeneralInfo.BytecodeVersion;
    int infoLocation = (int)(Math.Abs((int)(Data.GeneralInfo.Timestamp & 65535L) / 7 + (Data.GeneralInfo.GameID - Data.GeneralInfo.DefaultWindowWidth) + Data.GeneralInfo.RoomOrder.Count) % 4);
    Data.GeneralInfo.GMS2RandomUID.Clear();
    Data.GeneralInfo.GMS2RandomUID.Add(firstRandom);
    for (int i = 0; i < 4; i++)
    {
        if (i == infoLocation)
            Data.GeneralInfo.GMS2RandomUID.Add(infoNumber);
        else
        {
            int first = random.Next();
            int second = random.Next();
            Data.GeneralInfo.GMS2RandomUID.Add(((long)first << 32) | (long)second);
        }
    }
}
*/

private Layer AddNewTileLayer(UndertaleRoom room, string layerName, int layerDepth)
{
    Layer newAssetsLayer = new Layer();
    newAssetsLayer.LayerName = Data.Strings.MakeString(layerName);
    newAssetsLayer.LayerId = largest_layerid++;
    newAssetsLayer.LayerDepth = layerDepth;
    newAssetsLayer.Data = new Layer.LayerAssetsData();
    newAssetsLayer.LayerType = LayerType.Assets;
    newAssetsLayer.IsVisible = true;

    // "??=" - assign if null
    newAssetsLayer.AssetsData.LegacyTiles ??= new UndertalePointerList<Tile>();
    newAssetsLayer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();
    newAssetsLayer.AssetsData.Sequences ??= new UndertalePointerList<SequenceInstance>();

    room.Layers.Add(newAssetsLayer);
    
    return newAssetsLayer;
}

private void AddNewTile(UndertaleRoom room, Layer tileLayer, UndertaleNamedResource tileObject, int tileX, int tileY,
                        uint tileSourceX, uint tileSourceY, uint tileWidth, uint tileHeight,
                        float tileScaleX, float tileScaleY, int tileDepth)
{
    Tile tile = new Tile();
    tile._SpriteMode = tileObject is UndertaleSprite;
    tile.X = tileX;
    tile.Y = tileY;
    tile.ObjectDefinition = tileObject;
    tile.SourceX = tileSourceX;
    tile.SourceY = tileSourceY;
    tile.Width = tileWidth;
    tile.Height = tileHeight;
    tile.ScaleX = tileScaleX;
    tile.ScaleY = tileScaleY;
    tile.TileDepth = tileDepth;
    tile.InstanceID = Data.GeneralInfo.LastTile++;

    room?.Tiles.Add(tile);
    tileLayer?.AssetsData.LegacyTiles.Add(tile);
}

private void AddNewGameObject(UndertaleRoom room, Layer layer, UndertaleGameObject obj, int x, int y)
{
    if (obj is null)
        return;

    GameObject newObj = new GameObject()
    {
        X = x,
        Y = y,
        ObjectDefinition = obj,
        InstanceID = Data.GeneralInfo.LastObj++
    };

    room.GameObjects.Add(newObj);
    layer?.InstancesData.Instances.Add(newObj);
}

if (GMS1_mode == false)
{
    newRoom = new UndertaleRoom()
    {
        Name = newRoomNameStr,
        Width = 320,
        Height = 240,
        Top = (uint)0,
        Left = (uint)0,
        Right = (uint)0,
        Bottom = (uint)0,
        Speed = (uint)0,
        Flags = RoomEntryFlags.EnableViews | RoomEntryFlags.ShowColor | RoomEntryFlags.IsGMS2
    };
    
    Data.Rooms.Add(newRoom);
    
    newRoom.Views[0].ViewWidth = 320;
    newRoom.Views[0].ViewHeight = 240;
    newRoom.Views[0].ViewX = 0;
    newRoom.Views[0].ViewY = 0;
    newRoom.Views[0].PortWidth = 640;
    newRoom.Views[0].PortHeight = 480;
    newRoom.Views[0].PortX = 0;
    newRoom.Views[0].PortY = 0;
    newRoom.Views[0].BorderX = 160;
    newRoom.Views[0].BorderY = 240;
    newRoom.Views[0].Enabled = true;
    newRoom.Views[0].ObjectId = obj_mainchara;
    
    Layer newInstancesLayer = new Layer();
    newInstancesLayer.LayerName = Data.Strings.MakeString(newRoomName + "_GameObjects_Layer");
    newInstancesLayer.LayerId = largest_layerid++;
    newInstancesLayer.LayerType = LayerType.Instances;
    newInstancesLayer.IsVisible = true;
    newInstancesLayer.Data = new Layer.LayerInstancesData();
    
    newRoom.Layers.Add(newInstancesLayer);

    AddNewGameObject(newRoom, newInstancesLayer, obj_mainchara, 160, 120);
    AddNewGameObject(newRoom, newInstancesLayer, obj_overworldc, 0, 0);
    
    var newLayer1 = AddNewTileLayer(newRoom, newRoomName + "_Tiles_Layer_-10000", -10000);
    var newLayer2 = AddNewTileLayer(newRoom, newRoomName + "_Tiles_Layer_-9990", -9990);
    
    var newLayer3 = AddNewTileLayer(newRoom, newRoomName + "_Tiles_Layer_999990", 999990);
    var newLayer4 = AddNewTileLayer(newRoom, newRoomName + "_Tiles_Layer_1000000", 1000000);
    
    if (bg_schooltiles is not null)
    {
        AddNewTile(null, newLayer4, bg_schooltiles, 20, 20, 0, 0, 20, 20, 1, 1, 1000000);
        AddNewTile(null, newLayer3, bg_schooltiles, 40, 20, 0, 40, 20, 20, 1, 1, 999990);
        AddNewTile(null, newLayer2, bg_schooltiles, 60, 20, 0, 160, 20, 20, 1, 1, -9990);
        AddNewTile(null, newLayer1, bg_schooltiles, 80, 20, 40, 160, 20, 20, 1, 1, -10000);
    }
    
    Layer newBackgroundLayer = new Layer();
    newBackgroundLayer.LayerName = Data.Strings.MakeString(newRoomName + "_Background_Layer");
    newBackgroundLayer.LayerId = largest_layerid++;
    newBackgroundLayer.LayerDepth = 2147483600;
    newBackgroundLayer.LayerType = LayerType.Background;
    newBackgroundLayer.IsVisible = true;
    
    var newBGLayerData = new Layer.LayerBackgroundData();
    newBGLayerData.Visible = true;
    newBGLayerData.Color = (uint)4278190080;
    newBGLayerData.AnimationSpeed = 15;
    
    newBackgroundLayer.Data = newBGLayerData;
    
    newRoom.Layers.Add(newBackgroundLayer);
    
    Data.GeneralInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = newRoom });
    
    //UpdateRandomUID();
}
else
{
    newRoom = new UndertaleRoom()
    {
        Name = newRoomNameStr,
        Width = 320,
        Height = 240,
        Top = (uint)0,
        Left = (uint)0,
        Right = (uint)1024,
        Bottom = (uint)768,
        Speed = (uint)30,
        Flags = RoomEntryFlags.EnableViews | RoomEntryFlags.ShowColor
    };
    
    Data.Rooms.Add(newRoom);
    
    newRoom.Views[0].ViewWidth = 320;
    newRoom.Views[0].ViewHeight = 240;
    newRoom.Views[0].ViewX = 0;
    newRoom.Views[0].ViewY = 0;
    newRoom.Views[0].PortWidth = 640;
    newRoom.Views[0].PortHeight = 480;
    newRoom.Views[0].PortX = 0;
    newRoom.Views[0].PortY = 0;
    newRoom.Views[0].BorderX = 32;
    newRoom.Views[0].BorderY = 32;
    newRoom.Views[0].Enabled = true;
    newRoom.Views[0].ObjectId = obj_mainchara;

    AddNewGameObject(newRoom, null, obj_mainchara, 160, 120);
    AddNewGameObject(newRoom, null, obj_overworldcontroller, 0, 0);

    AddNewTile(newRoom, null, bg_ruinseasynam3, 20, 20, 20, 240, 20, 20, 1, 1, 1000000);
    
    Data.GeneralInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = newRoom });
}

ChangeSelection(newRoom);

ScriptMessage(newRoomName + " has been added successfully!");