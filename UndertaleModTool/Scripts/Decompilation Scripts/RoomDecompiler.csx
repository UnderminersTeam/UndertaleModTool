// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Collections.Generic;

EnsureDataLoaded();

string roomFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "rooms" + Path.DirectorySeparatorChar;

if (Directory.Exists(roomFolder))
{
    Directory.Delete(roomFolder, true);
}

// 180 lines of classes... pain
public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }

	public static AssetReference Create(UndertaleNamedResource obj, UndertaleNamedResource pathObj, string folderName) {
		if (obj == null) return null;
		return new AssetReference {
			name = obj.Name.Content,
			path = $"{folderName}/{pathObj.Name.Content}/{pathObj.Name.Content}.yy"
		};
	}
}

public class GMRView {
	public bool inherit = false;
	public bool visible = true;
	public int xview = 0;
	public int yview = 0;
	public int wview = 1920;
	public int hview = 1080;
	public int xport = 0;
	public int yport = 0;
	public int wport = 1920;
	public int hport = 1080;
	public AssetReference objectId = null;
	public uint hborder = 32;
	public uint vborder = 32;
	public int hspeed = -1;
	public int vspeed = -1;
}

public class GMRLayer {
	public string resourceType;
	public string resourceVersion;
	public string name = "";
	public bool visible = true;
	public float depth = 0;
	public bool userdefinedDepth = false;
	public bool inheritLayerDepth = false;
	public bool inheritLayerSettings = false;
	public double gridX = 32;
	public double gridY = 32;
	public List<GMRLayer> layers = new List<GMRLayer>();

	public bool hierarchyFrozen = false;
	public bool effectEnabled = true;
	public string effectType = null;
	public List<GMREffectProperty> properties = new List<GMREffectProperty>();
}

public class GMRAssetLayer : GMRLayer {
	public string resourceType = "GMRAssetLayer";
	public string resourceVersion = "1.0";
	public List<GMRAsset> assets = new List<GMRAsset>();
}

public class GMRAsset {
	public string resourceType;
	public string resourceVersion;
	// probably???
	public AssetReference inheritedItemId = null;
	public bool frozen = false;
	public bool ignore = false;
	public bool inheritItemSettings = false;
}

public class GMRSpriteGraphic : GMRAsset {
	public string resourceType = "GMRSpriteGraphic";
	public string resourceVersion = "1.0";
	public string name = "";
	public AssetReference spriteId = null;
	public float headPosition = 0f;
	public float rotation = 0f;
	public float scaleX = 1f;
	public float scaleY = 1f;
	public float animationSpeed = 1f;
	public uint colour = 0xffffffff;
	public float x = 0f;
	public float y = 0f;
}

// legacy tiles
public class GMRGraphic : GMRAsset {
	public string resourceType = "GMRGraphic";
	public string resourceVersion = "1.0";
	public string name = "";
	public AssetReference spriteId = null;
	public int x = 0;
	public int y = 0;
	public float w = 0;
	public float h = 0;
	public float u0 = 0;
	public float v0 = 0;
	public float u1 = 0;
	// ultrakill refere
	public float v1 = 0;
	public uint colour = 0xffffffff;
	public List<string> tags = new List<string>();
}

public class GMRPathLayer : GMRLayer {
	public string resourceType = "GMRPathLayer";
	public string resourceVersion = "1.0";
	public AssetReference pathId = null;
	public uint colour = 0xffffffff;
}

public class GMRTileLayer : GMRLayer {
	public string resourceType = "GMRTileLayer";
	public string resourceVersion = "1.1";
	public AssetReference tilesetId = null;
	public float x = 0f;
	public float y = 0f;
	public GMRTileData tiles = null;
}

public class GMRTileData {
	public uint SerialiseWidth = 0;
	public uint SerialiseHeight = 0;
	public List<int> TileSerialiseData = new List<int>();
}

public class GMREffectProperty {
	public uint type = 0;
	public string name;
	public string value;
}

public class GMRInstanceLayer : GMRLayer {
	public string resourceType = "GMRInstanceLayer";
	public string resourceVersion = "1.0";
	public List<GMRInstance> instances = new List<GMRInstance>();
}

public class GMRInstance {
	public string resourceType = "GMRInstance";
	public string resourceVersion = "1.0";
	public string name;
	// overridden variables
	// this isn't in utmt's gui, so it's probably
	// compiled into the creation code
	public List<object> properties = new List<object>();
	public bool isDnd = false;
	public AssetReference objectId = null;
	public bool inheritCode = false;
	public bool hasCreationCode = false;
	public uint colour = 0xffffffff;
	public float rotation = 0f;
	public float scaleX = 1f;
	public float scaleY = 1f;
	public int imageIndex = 0;
	public float imageSpeed = 0f;
	// probably???
	public AssetReference inheritedItemId = null;
	public bool frozen = false;
	public bool ignore = false;
	public bool inheritItemSettings = false;
	public int x = 0;
	public int y = 0;
}

public class GMRBackgroundLayer : GMRLayer {
	public string resourceType = "GMRBackgroundLayer";
	public string resourceVersion = "1.0";
	public AssetReference spriteId = null;
	public uint colour = 0xffffffff;
	public float x = 0;
	public float y = 0;
	public bool htiled = false;
	public bool vtiled = false;
	public float hspeed = 0f;
	public float vspeed = 0f;
	public bool stretch = false;
	public float animationFPS = 15f;
	public uint animationSpeedType = 0;
	public bool userdefinedAnimFPS = false;
}

public class GMREffectLayer : GMRLayer {
	public string resourceType = "GMREffectLayer";
	public string resourceVersion = "1.0";
}

public class GMRoom {
	public string resourceType = "GMRoom";
	public string resourceVersion = "1.0";
	public string name;
	public bool isDnd = false;
	public float volume = 1f;
	// probably???
	public AssetReference parentRoom = null;
	public List<GMRView> views = new List<GMRView>();
	public List<GMRLayer> layers = new List<GMRLayer>();
	public bool inheritLayers = false;
	public string creationCodeFile = "";
	public bool inheritCode = false;
	public List<AssetReference> instanceCreationOrder = new List<AssetReference>();
	public bool inheritCreationOrder = false;
	public AssetReference sequenceId = null;
	public GMRoomSettings roomSettings = new GMRoomSettings();
	public GMViewSettings viewSettings = new GMViewSettings();
	public GMPhysicsSettings physicsSettings = new GMPhysicsSettings();
	public AssetReference parent = null; 
}

public class GMRoomSettings {
	public bool inheritRoomSettings = false;
	public uint Width = 1920;
	public uint Height = 1080;
	public bool persistent = false;
}
public class GMViewSettings {
	public bool inheritViewSettings = false;
	public bool enableViews = false;
	public bool clearViewBackground = false;
	public bool clearDisplayBuffer = false;
}
public class GMPhysicsSettings {
	public bool inheritPhysicsSettings = false;
	public bool PhysicsWorld = false;
	public float PhysicsWorldGravityX = 0f;
	public float PhysicsWorldGravityY = 10f;
	public float PhysicsWorldPixToMetres = 0.1f;
}

void ApplyCommonLayerData(GMRLayer layerData, UndertaleRoom.Layer layer, UndertaleRoom room) {
	layerData.name = layer.LayerName.Content;
	layerData.visible = layer.IsVisible;
	layerData.depth = layer.LayerDepth;
	layerData.effectEnabled = layer.EffectEnabled;
	layerData.effectType = layer.EffectType?.Content;
	foreach (UndertaleRoom.EffectProperty property in layer.EffectProperties) {
		layerData.properties.Add(new GMREffectProperty{
			type = (uint)property.Kind,
			name = property.Name.Content,
			value = property.Value.Content
		});
	}
	layerData.userdefinedDepth = true;
	layerData.gridX = room.GridWidth;
	layerData.gridY = room.GridHeight;
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

SetProgressBar(null, "Rooms", 0, Data.Rooms.Count);
StartProgressBarUpdater();
await Task.Run(() => Parallel.ForEach(Data.Rooms, (UndertaleRoom room) => {
    string roomDir = roomFolder + room.Name.Content + Path.DirectorySeparatorChar;
    Directory.CreateDirectory(roomDir);

	List<GMRView> viewsData = new List<GMRView>();
	foreach (UndertaleRoom.View view in room.Views) {
		viewsData.Add(new GMRView {
			visible = view.Enabled,
			xview = view.ViewX,
			yview = view.ViewY,
			wview = view.ViewWidth,
			hview = view.ViewHeight,
			xport = view.PortX,
			yport = view.PortY,
			wport = view.PortWidth,
			hport = view.PortHeight,
			objectId = AssetReference.Create(view.ObjectId, view.ObjectId, "objects"),
			hborder = view.BorderX,
			vborder = view.BorderY,
			hspeed = view.SpeedX,
			vspeed = view.SpeedY
		});
	}
	List<AssetReference> orderData = new List<AssetReference>();
	List<GMRLayer> layersData = new List<GMRLayer>();
	foreach (UndertaleRoom.Layer layer in room.Layers) {
		switch (layer.LayerType) {
			case UndertaleRoom.LayerType.Path:
				// no data apparently
				continue;
			case UndertaleRoom.LayerType.Background: {
				GMRBackgroundLayer layerData = new GMRBackgroundLayer();
				layerData.x = layer.XOffset;
				layerData.y = layer.YOffset;
				layerData.hspeed = layer.HSpeed;
				layerData.vspeed = layer.VSpeed;

				layerData.spriteId = AssetReference.Create(
					layer.BackgroundData.Sprite, layer.BackgroundData.Sprite,
					"sprites"
				);
				layerData.htiled = layer.BackgroundData.TiledHorizontally;
				layerData.vtiled = layer.BackgroundData.TiledVertically;
				layerData.stretch = layer.BackgroundData.Stretch;
				// bri'ish vs. americ'n
				layerData.colour = layer.BackgroundData.Color;

				layerData.animationFPS = layer.BackgroundData.AnimationSpeed;
				layerData.animationSpeedType = (uint)layer.BackgroundData.AnimationSpeedType;
				layerData.userdefinedAnimFPS = true;
				ApplyCommonLayerData(layerData, layer, room);
				layersData.Add(layerData);
				} break;
			case UndertaleRoom.LayerType.Instances: {
				GMRInstanceLayer layerData = new GMRInstanceLayer();

				// not UndertaleGameObject, GameObject. confusing, right?
				foreach (UndertaleRoom.GameObject obj in layer.InstancesData.Instances) {
					string instName = $"inst_{obj.InstanceID}";
					if (obj.CreationCode != null) {
						var code = obj.CreationCode;
						var gmlPath = $"{roomDir}InstanceCreationCode_{instName}.gml";
						try
						{
							File.WriteAllText(gmlPath, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
						}
						catch (Exception e)
						{
							File.WriteAllText(gmlPath, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
        				}
					}
					layerData.instances.Add(new GMRInstance{
						objectId = AssetReference.Create(
							obj.ObjectDefinition, obj.ObjectDefinition,
							"objects"
						),
						name = instName,
						x = obj.X,
						y = obj.Y,
						scaleX = obj.ScaleX,
						scaleY = obj.ScaleY,
						colour = obj.Color,
						rotation = obj.Rotation,
						hasCreationCode = obj.CreationCode != null,
						imageSpeed = obj.ImageSpeed,
						imageIndex = obj.ImageIndex
					});
					orderData.Add(new AssetReference{
						name = instName,
						path = $"rooms/{room.Name.Content}/{room.Name.Content}.yy"
					});
				}

				ApplyCommonLayerData(layerData, layer, room);
				layersData.Add(layerData);
				} break;
			case UndertaleRoom.LayerType.Assets: {
				GMRAssetLayer layerData = new GMRAssetLayer();
				foreach (UndertaleRoom.Tile asset in layer.AssetsData.LegacyTiles) {
					layerData.assets.Add(new GMRGraphic{
						name = $"inst_{asset.InstanceID}",
						spriteId = AssetReference.Create(
							asset.ObjectDefinition, asset.ObjectDefinition,
							"sprites"
						),
						x = asset.X,
						y = asset.Y,
						w = asset.Width * asset.ScaleX,
						h = asset.Height * asset.ScaleY,
						u0 = asset.SourceX,
						v0 = asset.SourceY,
						u1 = asset.SourceX + Convert.ToUInt32(asset.Width),
						v1 = asset.SourceY + Convert.ToUInt32(asset.Height),
					});
				}
				foreach (UndertaleRoom.SpriteInstance asset in layer.AssetsData.Sprites) {
					layerData.assets.Add(new GMRSpriteGraphic{
						name = asset.Name.Content,
						spriteId = AssetReference.Create(
							asset.Sprite, asset.Sprite,
							"sprites"
						),
						x = asset.X,
						y = asset.Y,
						scaleX = asset.ScaleX,
						scaleY = asset.ScaleY,
						colour = asset.Color,
						rotation = asset.Rotation,
						headPosition = asset.FrameIndex,
						animationSpeed = asset.AnimationSpeed
					});
				}
				ApplyCommonLayerData(layerData, layer, room);
				layersData.Add(layerData);
				} break;
			case UndertaleRoom.LayerType.Tiles: {
				GMRTileLayer layerData = new GMRTileLayer();
				layerData.x = layer.XOffset;
				layerData.y = layer.YOffset;

				layerData.tilesetId = AssetReference.Create(
					layer.TilesData.Background, layer.TilesData.Background,
					"tilesets"
				);

				layerData.tiles = new GMRTileData();
				layerData.tiles.SerialiseWidth = layer.TilesData.TilesX;
				layerData.tiles.SerialiseHeight = layer.TilesData.TilesY;
				foreach (uint[] tileRow in layer.TilesData.TileData) {
					foreach (uint tileId in tileRow) {
						int _tileId = (int)tileId;
						layerData.tiles.TileSerialiseData.Add(_tileId);
					}
				}

				ApplyCommonLayerData(layerData, layer, room);
				layersData.Add(layerData);
				} break;
			case UndertaleRoom.LayerType.Effect: {
				GMREffectLayer layerData = new GMREffectLayer();
				// no other data
				ApplyCommonLayerData(layerData, layer, room);
				layersData.Add(layerData);
				} break;
			default:
				throw new Exception($"Unknown layer type: {layer.LayerType}");
		}
	}

	if (room.CreationCodeId != null) {
		var code = room.CreationCodeId;
        var gmlPath = $"{roomDir}RoomCreationCode.gml";
        try
        {
            File.WriteAllText(gmlPath, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
        }
        catch (Exception e)
        {
            File.WriteAllText(gmlPath, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
        }
	}
	GMRoom roomData = new GMRoom {
		name = room.Name.Content,
		creationCodeFile = room.CreationCodeId == null ? "" : $"${{project_dir}}\\rooms\\{room.Name.Content}\\RoomCreationCode.gml",
		views = viewsData,
		layers = layersData,
		instanceCreationOrder = orderData,
		roomSettings = new GMRoomSettings {
			Width = room.Width,
			Height = room.Height,
			persistent = room.Persistent
		},
		viewSettings = new GMViewSettings {
			enableViews = room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.EnableViews),
			clearViewBackground = room.DrawBackgroundColor,
			clearDisplayBuffer = room.DrawBackgroundColor
		},
		physicsSettings = new GMPhysicsSettings {
			PhysicsWorld = room.World,
			PhysicsWorldGravityX = room.GravityX,
			PhysicsWorldGravityY = room.GravityY,
			PhysicsWorldPixToMetres = room.MetersPerPixel
		},
        parent = new AssetReference()
        {
            name = "Rooms",
            path = "folders/Rooms.yy"
        }
	};

    string json = JsonConvert.SerializeObject(roomData, Formatting.Indented);
    File.WriteAllText(roomDir + room.Name.Content + ".yy", json);

	IncrementProgressParallel();
}));
await StopProgressBarUpdater();
HideProgressBar();