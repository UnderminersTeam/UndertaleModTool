// Written by SolventMercury

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

ScriptMessage("Select the Room to import");
string roomInputPath = PromptLoadFile("Import which file", "Json Files|*.json");
if (roomInputPath == null) throw new ScriptException("The room's path was not set.");

UndertaleRoom newRoom = new UndertaleRoom();

ReadRoom(roomInputPath);

void ReadRoom(string filePath)
{
	FileStream stream = File.OpenRead(filePath);
	byte[] jsonUtf8Bytes = new byte[stream.Length];

	stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
	stream.Close();

	JsonReaderOptions options = new JsonReaderOptions
	{
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	Utf8JsonReader reader = new Utf8JsonReader(jsonUtf8Bytes, options);

	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);

	ReadName(ref reader);
	ReadMainValues(ref reader);

	ClearRoomLists();

	ReadBackgrounds(ref reader);
	ReadViews(ref reader);
	ReadGameObjects(ref reader);
	ReadTiles(ref reader);
	ReadLayers(ref reader);

	// Adds room to data file, if it doesn't exist.
	if (Data.Rooms.ByName(newRoom.Name.Content) == null)
		Data.Rooms.Add(newRoom);

}

void ReadMainValues(ref Utf8JsonReader reader)
{
	string caption = ReadString(ref reader);

	newRoom.Width = (uint) ReadNum(ref reader);
	newRoom.Height = (uint) ReadNum(ref reader);
	newRoom.Speed = (uint) ReadNum(ref reader);
	newRoom.Persistent = ReadBool(ref reader);
	newRoom.BackgroundColor = (uint) ReadNum(ref reader);
	newRoom.DrawBackgroundColor = ReadBool(ref reader);

	string ccIdName = ReadString(ref reader);

	newRoom.Flags = (UndertaleRoom.RoomEntryFlags) ReadNum(ref reader);
	newRoom.World = ReadBool(ref reader);
	newRoom.Top = (uint) ReadNum(ref reader);
	newRoom.Left = (uint) ReadNum(ref reader);
	newRoom.Right = (uint) ReadNum(ref reader);
	newRoom.Bottom = (uint) ReadNum(ref reader);
	newRoom.GravityX = ReadFloat(ref reader);
	newRoom.GravityY = ReadFloat(ref reader);
	newRoom.MetersPerPixel = ReadFloat(ref reader);

	newRoom.Caption = (caption == null) ? null : new UndertaleString(caption);

	if ((newRoom.Caption != null) && !Data.Strings.Any(s => s == newRoom.Caption))
		Data.Strings.Add(newRoom.Caption);

	newRoom.CreationCodeId = (ccIdName == null) ? null : Data.Code.ByName(ccIdName);
}

void ReadName(ref Utf8JsonReader reader)
{
	string name = ReadString(ref reader);
	if (name == null)
		throw new ScriptException("ERROR: Object name was null - object name must be defined!");

	if (Data.Rooms.ByName(name) != null)
	{
		newRoom = Data.Rooms.ByName(name);
	}
	else
	{
		newRoom = new UndertaleRoom();
		newRoom.Name = new UndertaleString(name);
		Data.Strings.Add(newRoom.Name);
	}
}

void ClearRoomLists()
{
	newRoom.Backgrounds.Clear();
	newRoom.Views.Clear();
	newRoom.GameObjects.Clear();
	newRoom.Tiles.Clear();
	newRoom.Layers.Clear();
}

void ReadBackgrounds(ref Utf8JsonReader reader)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.Background newBg = new UndertaleRoom.Background();

			newBg.ParentRoom = newRoom;

			newBg.CalcScaleX = ReadFloat(ref reader);
			newBg.CalcScaleY = ReadFloat(ref reader);
			newBg.Enabled = ReadBool(ref reader);
			newBg.Foreground = ReadBool(ref reader);
			string bgDefName = ReadString(ref reader);
			newBg.X = (int) ReadNum(ref reader);
			newBg.Y = (int) ReadNum(ref reader);
			newBg.TileX = (int) ReadNum(ref reader);
			newBg.TileY = (int) ReadNum(ref reader);
			newBg.SpeedX = (int) ReadNum(ref reader);
			newBg.SpeedY = (int) ReadNum(ref reader);
			newBg.Stretch = ReadBool(ref reader);

			newBg.BackgroundDefinition = (bgDefName == null) ? null : Data.Backgrounds.ByName(bgDefName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newRoom.Backgrounds.Add(newBg);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
	}
}

void ReadViews(ref Utf8JsonReader reader)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.View newView = new UndertaleRoom.View();

			newView.Enabled = ReadBool(ref reader);
			newView.ViewX = (int) ReadNum(ref reader);
			newView.ViewY = (int) ReadNum(ref reader);
			newView.ViewWidth = (int) ReadNum(ref reader);
			newView.ViewHeight = (int) ReadNum(ref reader);
			newView.PortX = (int) ReadNum(ref reader);
			newView.PortY = (int) ReadNum(ref reader);
			newView.PortWidth = (int) ReadNum(ref reader);
			newView.PortHeight = (int) ReadNum(ref reader);
			newView.BorderX = (uint) ReadNum(ref reader);
			newView.BorderY = (uint) ReadNum(ref reader);
			newView.SpeedX = (int) ReadNum(ref reader);
			newView.SpeedY = (int) ReadNum(ref reader);
			string objIdName = ReadString(ref reader);

			newView.ObjectId = (objIdName == null) ? null : Data.GameObjects.ByName(objIdName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newRoom.Views.Add(newView);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
	}
}

void ReadGameObjects(ref Utf8JsonReader reader)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.GameObject newObj = new UndertaleRoom.GameObject();

			newObj.X = (int) ReadNum(ref reader);
			newObj.Y = (int) ReadNum(ref reader);

			string objDefName = ReadString(ref reader);

			newObj.InstanceID = (uint) ReadNum(ref reader);

			string ccIdName = ReadString(ref reader);

			newObj.ScaleX = ReadFloat(ref reader);
			newObj.ScaleY = ReadFloat(ref reader);
			newObj.Color = (uint) ReadNum(ref reader);
			newObj.Rotation = ReadFloat(ref reader);

			string preCcIdName = ReadString(ref reader);

			newObj.ImageSpeed = ReadFloat(ref reader);
			newObj.ImageIndex = (int) ReadNum(ref reader);

			newObj.ObjectDefinition = (objDefName == null) ? null : Data.GameObjects.ByName(objDefName);
			newObj.CreationCode = (ccIdName == null) ? null : Data.Code.ByName(ccIdName);
			newObj.PreCreateCode = (preCcIdName == null) ? null : Data.Code.ByName(preCcIdName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newRoom.GameObjects.Add(newObj);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
	}
}

void ReadTiles(ref Utf8JsonReader reader)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.Tile newTile = new UndertaleRoom.Tile();

			newTile._SpriteMode = ReadBool(ref reader);
			newTile.X = (int) ReadNum(ref reader);
			newTile.Y = (int) ReadNum(ref reader);

			string bgDefName = ReadString(ref reader);
			string sprDefName = ReadString(ref reader);

			newTile.SourceX = (uint) ReadNum(ref reader);
			newTile.SourceY = (uint) ReadNum(ref reader);
			newTile.Width = (uint) ReadNum(ref reader);
			newTile.Height = (uint) ReadNum(ref reader);
			newTile.TileDepth = (int) ReadNum(ref reader);
			newTile.InstanceID = (uint) ReadNum(ref reader);
			newTile.ScaleX = ReadFloat(ref reader);
			newTile.ScaleY = ReadFloat(ref reader);
			newTile.Color = (uint) ReadNum(ref reader);

			newTile.BackgroundDefinition = (bgDefName == null) ? null : Data.Backgrounds.ByName(bgDefName);
			newTile.SpriteDefinition = (sprDefName == null) ? null : Data.Sprites.ByName(sprDefName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newRoom.Tiles.Add(newTile);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
	}
}

void ReadLayers(ref Utf8JsonReader reader)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.Layer newLayer = new UndertaleRoom.Layer();

			string layerName = ReadString(ref reader);

			newLayer.LayerId = (uint) ReadNum(ref reader);
			newLayer.LayerType = (UndertaleRoom.LayerType) ReadNum(ref reader);
			newLayer.LayerDepth = (int) ReadNum(ref reader);
			newLayer.XOffset = ReadFloat(ref reader);
			newLayer.YOffset = ReadFloat(ref reader);
			newLayer.HSpeed = ReadFloat(ref reader);
			newLayer.VSpeed = ReadFloat(ref reader);
			newLayer.IsVisible = ReadBool(ref reader);


			newLayer.LayerName = (layerName == null) ? null : new UndertaleString(layerName);

			if ((layerName != null) && !Data.Strings.Any(s => s == newLayer.LayerName))
				Data.Strings.Add(newLayer.LayerName);

			switch (newLayer.LayerType)
			{
				case UndertaleRoom.LayerType.Background:
					ReadBackgroundLayer(ref reader, newLayer);
					break;
				case UndertaleRoom.LayerType.Instances:
					ReadInstancesLayer(ref reader, newLayer);
					break;
				case UndertaleRoom.LayerType.Assets:
					ReadAssetsLayer(ref reader, newLayer);
					break;
				case UndertaleRoom.LayerType.Tiles:
					ReadTilesLayer(ref reader, newLayer);
					break;
				default:
					throw new ScriptException("ERROR: Invalid value for layer data type.");
			}

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newRoom.Layers.Add(newLayer);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
	}
}

void ReadBackgroundLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);

	UndertaleRoom.Layer.LayerBackgroundData newLayerData = new UndertaleRoom.Layer.LayerBackgroundData();

	newLayerData.CalcScaleX = ReadFloat(ref reader);
	newLayerData.CalcScaleY = ReadFloat(ref reader);
	newLayerData.Visible = ReadBool(ref reader);
	newLayerData.Foreground = ReadBool(ref reader);

	string spriteName = ReadString(ref reader);

	newLayerData.TiledHorizontally = ReadBool(ref reader);
	newLayerData.TiledVertically = ReadBool(ref reader);
	newLayerData.Stretch = ReadBool(ref reader);
	newLayerData.Color = (uint) ReadNum(ref reader);
	newLayerData.FirstFrame = ReadFloat(ref reader);
	newLayerData.AnimationSpeed = ReadFloat(ref reader);
	newLayerData.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);

	newLayerData.Sprite = (spriteName == null) ? null : Data.Sprites.ByName(spriteName);

	newLayerData.ParentLayer = newLayer;

	ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

	newLayer.Data = newLayerData;
}

void ReadInstancesLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.PropertyName)
			continue;

		if (reader.TokenType != JsonTokenType.StartArray)
			throw new ScriptException("ERROR: Did not correctly stop reading instances layer");

		UndertaleRoom.Layer.LayerInstancesData newLayerData = new UndertaleRoom.Layer.LayerInstancesData();

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.PropertyName)
				continue;

			if (reader.TokenType == JsonTokenType.StartObject)
			{
				UndertaleRoom.GameObject newObj = new UndertaleRoom.GameObject();

				newObj.X = (int) ReadNum(ref reader);
				newObj.Y = (int) ReadNum(ref reader);

				string objDefName = ReadString(ref reader);

				newObj.InstanceID = (uint) ReadNum(ref reader);

				string ccIdName = ReadString(ref reader);

				newObj.ScaleX = ReadFloat(ref reader);
				newObj.ScaleY = ReadFloat(ref reader);
				newObj.Color = (uint) ReadNum(ref reader);
				newObj.Rotation = ReadFloat(ref reader);

				string preCcIdName = ReadString(ref reader);

				newObj.ImageSpeed = ReadFloat(ref reader);
				newObj.ImageIndex = (int) ReadNum(ref reader);

				newObj.ObjectDefinition = (objDefName == null) ? null : Data.GameObjects.ByName(objDefName);

				newObj.CreationCode = (ccIdName == null) ? null : Data.Code.ByName(ccIdName);

				newObj.PreCreateCode = (preCcIdName == null) ? null : Data.Code.ByName(preCcIdName);

				ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

				newLayerData.Instances.Add(newObj);
				continue;
			}

			if (reader.TokenType == JsonTokenType.EndArray)
				break;

			throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
		}

		ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

		newLayer.Data = newLayerData;

		return;

	}
}

void ReadAssetsLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	UndertaleRoom.Layer.LayerAssetsData newLayerData = new UndertaleRoom.Layer.LayerAssetsData();

	newLayerData.LegacyTiles = new UndertalePointerList<UndertaleRoom.Tile>();
	newLayerData.Sprites = new UndertalePointerList<UndertaleRoom.SpriteInstance>();
	newLayerData.Sequences = new UndertalePointerList<UndertaleRoom.SequenceInstance>();
	newLayerData.NineSlices = new UndertalePointerList<UndertaleRoom.SpriteInstance>();

	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.Tile newTile = new UndertaleRoom.Tile();

			newTile._SpriteMode = ReadBool(ref reader);
			newTile.X = (int) ReadNum(ref reader);
			newTile.Y = (int) ReadNum(ref reader);

			string bgDefName = ReadString(ref reader);
			string sprDefName = ReadString(ref reader);

			newTile.SourceX = (uint) ReadNum(ref reader);
			newTile.SourceY = (uint) ReadNum(ref reader);
			newTile.Width = (uint) ReadNum(ref reader);
			newTile.Height = (uint) ReadNum(ref reader);
			newTile.TileDepth = (int) ReadNum(ref reader);
			newTile.InstanceID = (uint) ReadNum(ref reader);
			newTile.ScaleX = ReadFloat(ref reader);
			newTile.ScaleY = ReadFloat(ref reader);
			newTile.Color = (uint) ReadNum(ref reader);

			newTile.BackgroundDefinition = (bgDefName == null) ? null : Data.Backgrounds.ByName(bgDefName);

			newTile.SpriteDefinition = (sprDefName == null) ? null : Data.Sprites.ByName(sprDefName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newLayerData.LegacyTiles.Add(newTile);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
	}

	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.PropertyName)
			continue;

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.SpriteInstance newSpr = new UndertaleRoom.SpriteInstance();

			string name = ReadString(ref reader);
			string spriteName = ReadString(ref reader);

			newSpr.X = (int) ReadNum(ref reader);
			newSpr.Y = (int) ReadNum(ref reader);
			newSpr.ScaleX = ReadFloat(ref reader);
			newSpr.ScaleY = ReadFloat(ref reader);
			newSpr.Color = (uint) ReadNum(ref reader);
			newSpr.AnimationSpeed = ReadFloat(ref reader);
			newSpr.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);
			newSpr.FrameIndex = ReadFloat(ref reader);
			newSpr.Rotation = ReadFloat(ref reader);

			newSpr.Name = (name == null) ? null : new UndertaleString(name);

			if ((name != null) && !Data.Strings.Any(s => s == newSpr.Name))
				Data.Strings.Add(newSpr.Name);

			newSpr.Sprite = (spriteName == null) ? null : Data.Sprites.ByName(spriteName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newLayerData.Sprites.Add(newSpr);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
	}

	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.PropertyName)
			continue;

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.SequenceInstance newSeq = new UndertaleRoom.SequenceInstance();

			string name = ReadString(ref reader);
			string sequenceName = ReadString(ref reader);

			newSeq.X = (int) ReadNum(ref reader);
			newSeq.Y = (int) ReadNum(ref reader);
			newSeq.ScaleX = ReadFloat(ref reader);
			newSeq.ScaleY = ReadFloat(ref reader);
			newSeq.Color = (uint) ReadNum(ref reader);
			newSeq.AnimationSpeed = ReadFloat(ref reader);
			newSeq.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);
			newSeq.FrameIndex = ReadFloat(ref reader);
			newSeq.Rotation = ReadFloat(ref reader);


			newSeq.Name = (name == null) ? null : new UndertaleString(name);

			if ((name != null) && !Data.Strings.Any(s => s == newSeq.Name))
				Data.Strings.Add(newSeq.Name);

			newSeq.Sequence = (sequenceName == null) ? null : Data.Sequences.ByName(sequenceName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newLayerData.Sequences.Add(newSeq);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
	}

	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.PropertyName)
			continue;

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			UndertaleRoom.SpriteInstance newSpr = new UndertaleRoom.SpriteInstance();

			string name = ReadString(ref reader);
			string spriteName = ReadString(ref reader);

			newSpr.X = (int) ReadNum(ref reader);
			newSpr.Y = (int) ReadNum(ref reader);
			newSpr.ScaleX = ReadFloat(ref reader);
			newSpr.ScaleY = ReadFloat(ref reader);
			newSpr.Color = (uint) ReadNum(ref reader);
			newSpr.AnimationSpeed = ReadFloat(ref reader);
			newSpr.AnimationSpeedType = (AnimationSpeedType) ReadNum(ref reader);
			newSpr.FrameIndex = ReadFloat(ref reader);
			newSpr.Rotation = ReadFloat(ref reader);

			newSpr.Name = (name == null) ? null : new UndertaleString(name);

			if ((name != null) && !Data.Strings.Any(s => s == newSpr.Name))
				Data.Strings.Add(newSpr.Name);

			newSpr.Sprite = spriteName == null ? null : Data.Sprites.ByName(spriteName);

			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

			newLayerData.NineSlices.Add(newSpr);
			continue;
		}

		if (reader.TokenType == JsonTokenType.EndArray)
			break;

		throw new ScriptException("ERROR: Did not correctly stop reading instances in instance layer");
	}

	newLayer.Data = newLayerData;
	ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
}

void ReadTilesLayer(ref Utf8JsonReader reader, UndertaleRoom.Layer newLayer)
{
	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
	UndertaleRoom.Layer.LayerTilesData newLayerData = new UndertaleRoom.Layer.LayerTilesData();
	newLayerData.TilesX = (uint) ReadNum(ref reader);
	newLayerData.TilesY = (uint) ReadNum(ref reader);
	uint[][] tileIds = new uint[newLayerData.TilesY][];
	for (int i = 0; i < newLayerData.TilesY; i++)
	{
		tileIds[i] = new uint[newLayerData.TilesX];
	}

	ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
	for (int y = 0; y < newLayerData.TilesY; y++)
	{
		ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
		for (int x = 0; x < newLayerData.TilesX; x++)
		{
			ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
			(tileIds[y])[x] = (uint) ReadNum(ref reader);
			ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
		}

		ReadAnticipateJSONObject(ref reader, JsonTokenType.EndArray);
	}

	newLayerData.TileData = tileIds;
	ReadAnticipateJSONObject(ref reader, JsonTokenType.EndArray);
	ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);

	newLayer.Data = newLayerData;
}

// Read tokens of specified type

bool ReadBool(ref Utf8JsonReader reader)
{
	while (reader.Read())
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.PropertyName: continue;
			case JsonTokenType.True: return true;
			case JsonTokenType.False: return false;
			default: throw new ScriptException($"ERROR: Unexpected token type. Expected Boolean - found {reader.TokenType}");
		}
	}

	throw new ScriptException("ERROR: Did not find value of expected type. Expected Boolean.");
}

long ReadNum(ref Utf8JsonReader reader)
{
	while (reader.Read())
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.PropertyName: continue;
			case JsonTokenType.Number: return reader.GetInt64();
			default: throw new ScriptException($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
		}
	}

	throw new ScriptException("ERROR: Did not find value of expected type. Expected Integer.");
}

float ReadFloat(ref Utf8JsonReader reader)
{
	while (reader.Read())
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.PropertyName: continue;
			case JsonTokenType.Number: return reader.GetSingle();
			default: throw new ScriptException($"ERROR: Unexpected token type. Expected Decimal - found {reader.TokenType}");
		}
	}

	throw new ScriptException("ERROR: Did not find value of expected type. Expected Decimal.");
}

string ReadString(ref Utf8JsonReader reader)
{
	while (reader.Read())
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.PropertyName: continue;
			case JsonTokenType.String: return reader.GetString();
			case JsonTokenType.Null: return null;
			default: throw new ScriptException($"ERROR: Unexpected token type. Expected String - found {reader.TokenType}");
		}
	}

	throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
}

// Watch for certain meta-tokens

void ReadAnticipateJSONObject(ref Utf8JsonReader reader, JsonTokenType allowedTokenType)
{
	while (reader.Read())
	{
		if (reader.TokenType == JsonTokenType.PropertyName)
			continue;
		if (reader.TokenType == allowedTokenType)
			return;
		throw new ScriptException($"ERROR: Unexpected token type. Expected {allowedTokenType} - found {reader.TokenType}");
	}

	throw new ScriptException("ERROR: Did not find value of expected type. Expected String.");
}