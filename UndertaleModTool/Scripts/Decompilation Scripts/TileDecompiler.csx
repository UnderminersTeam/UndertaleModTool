// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Newtonsoft.Json;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Collections.Generic;

void SaveBlankPNG(string path, int width, int height)
{
    using var bmp = new Bitmap(width, height);
    using var g = Graphics.FromImage(bmp);
    g.Clear(Color.Transparent);
    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
}

EnsureDataLoaded();

string spriteFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "sprites" + Path.DirectorySeparatorChar;
string tilesetFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "tilesets" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
if (Directory.Exists(tilesetFolder))
{
    Directory.Delete(tilesetFolder, true);
}

string tilesprPrefix = "tilespr_";

public class TileSet
{
    public string resourceType { get; set; } = "GMTileSet";
    public string resourceVersion { get; set; } = "1.0";
    public string name { get; set; }
    public AssetReference textureGroupId { get; set; }
    public AssetReference spriteId { get; set; } = new AssetReference();
    public int tileWidth { get; set; }
    public int tileHeight { get; set; }
    public int tilexoff { get; set; }
    public int tileyoff { get; set; }
    public int tilehsep { get; set; }
    public int tilevsep { get; set; }
    public int out_tilehborder { get; set; }
    public int out_tilevborder { get; set; }
    public bool spriteNoExport { get; set; }
    public int out_columns { get; set; }
    public int tile_count { get; set; }
    public List<object> autoTileSets { get; set; } = new List<object>();
    public List<object> tileAnimationFrames { get; set; } = new List<object>();
    public double tileAnimationSpeed { get; set; }
    public TileAnimation tileAnimation { get; set; } = new TileAnimation();
    public MacroPageTiles macroPageTiles { get; set; } = new MacroPageTiles();
    public AssetReference parent { get; set; } = new AssetReference();
}

public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }
}

public class TileAnimation
{
    public List<uint> FrameData { get; set; } = new List<uint>();
    public uint SerialiseFrameCount { get; set; } = 1;
}

public class MacroPageTiles
{
    public int SerialiseWidth { get; set; } = 0;
    public int SerialiseHeight { get; set; } = 0;
    public List<object> TileSerialiseData { get; set; } = new List<object>();
}

var defaultTexGroup = new AssetReference
{
    name = "Default",
    path = "texturegroups/Default"
};

var texGroups = new Dictionary<UndertaleBackground, AssetReference>();
foreach (UndertaleTextureGroupInfo group in Data.TextureGroupInfo)
{
    var reference = new AssetReference
    {
        name = group.Name.Content,
        path = "texturegroups/" + group.Name.Content
    };
    foreach (UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> bg in group.Tilesets)
    {
        texGroups.TryAdd(bg.Resource, reference);
    }
}

SetProgressBar(null, "Tile Sets", 0, Data.Backgrounds.Count);
StartProgressBarUpdater();
await Task.Run(() => Parallel.ForEach(Data.Backgrounds, (UndertaleBackground tileSet) =>
{
    string guid1 = Guid.NewGuid().ToString();
    string guid2 = Guid.NewGuid().ToString();
    string tilesetPath = tilesetFolder + tileSet.Name.Content + Path.DirectorySeparatorChar;
    string tilesetSprPath = spriteFolder + tilesprPrefix + tileSet.Name.Content + Path.DirectorySeparatorChar;
    string layersFolder = tilesetSprPath + "layers" + Path.DirectorySeparatorChar;
    Directory.CreateDirectory(layersFolder);
    string layersTargetFolder = layersFolder + guid1 + Path.DirectorySeparatorChar;
    Directory.CreateDirectory(layersTargetFolder);
    Directory.CreateDirectory(tilesetPath);
    Directory.CreateDirectory(tilesetSprPath);

    int outputWidth = (int)(tileSet.GMS2TileWidth * tileSet.GMS2TileColumns);
    int outputHeight = (int)Math.Ceiling(tileSet.GMS2TileCount / (double)tileSet.GMS2TileColumns) * (int)tileSet.GMS2TileHeight;

    if (tileSet.Texture != null)
    {
        worker.ExportAsPNG(tileSet.Texture, tilesetSprPath + guid1 + ".png", null, true);
        worker.ExportAsPNG(tileSet.Texture, layersTargetFolder + guid2 + ".png", null, true);
        worker.ExportAsPNG(tileSet.Texture, tilesetPath + "output_tileset.png", null, true);
    }
    else
    {
        SaveBlankPNG(tilesetSprPath + guid1 + ".png", outputWidth, outputHeight);
        SaveBlankPNG(layersTargetFolder + guid2 + ".png", outputWidth, outputHeight);
        SaveBlankPNG(tilesetPath + "output_tileset.png", outputWidth, outputHeight);
    }

    TileSet tilesetData = new TileSet()
    {
        name = tileSet.Name.Content,
        spriteId = new AssetReference()
        {
            name = tilesprPrefix + tileSet.Name.Content,
            path = $"sprites/{tilesprPrefix}{tileSet.Name.Content}/{tilesprPrefix}{tileSet.Name.Content}.yy"
        },
        tileWidth = (int)tileSet.GMS2TileWidth,
        tileHeight = (int)tileSet.GMS2TileHeight,
        tileAnimation = new TileAnimation(),
        tilexoff = (int)tileSet.GMS2OutputBorderX,
        tileyoff = (int)tileSet.GMS2OutputBorderX,
        tilehsep = (int)tileSet.GMS2OutputBorderX * 2,
        tilevsep = (int)tileSet.GMS2OutputBorderY * 2,
        out_tilehborder = (int)tileSet.GMS2OutputBorderX,
        out_tilevborder = (int)tileSet.GMS2OutputBorderY,
        spriteNoExport = true,
        textureGroupId = texGroups.GetValueOrDefault(tileSet, defaultTexGroup),
        out_columns = (int)tileSet.GMS2TileColumns,
        tile_count = (int)tileSet.GMS2TileCount,
        parent = new AssetReference() { name = "Tile Sets", path = "folders/Tile Sets.yy" },
    };

    foreach (UndertaleBackground.TileID tileId in tileSet.GMS2TileIds)
    {
        tilesetData.tileAnimation.FrameData.Add(tileId.ID);
    }

    tilesetData.tileAnimation.SerialiseFrameCount = tileSet.GMS2ItemsPerTileCount;
    tilesetData.tileAnimationSpeed = 1 / ((double)tileSet.GMS2FrameLength / 1000000);

    string sprJson = @$"
        {{
          ""resourceType"": ""GMSprite"",
          ""resourceVersion"": ""1.0"",
          ""name"": ""{tilesprPrefix}{tileSet.Name.Content}"",
          ""bboxMode"": 0,
          ""collisionKind"": 1,
          ""type"": 0,
          ""origin"": 0,
          ""preMultiplyAlpha"": false,
          ""edgeFiltering"": false,
          ""collisionTolerance"": 0,
          ""swfPrecision"": 2.525,
          ""HTile"": false,
          ""VTile"": false,
          ""For3D"": false,
          ""DynamicTexturePage"": false,
          ""width"": {outputWidth},
          ""height"": {outputHeight},
          ""textureGroupId"": {{
            ""name"": ""Default"",
            ""path"": ""texturegroups/Default"",
          }},
          ""swatchColours"": null,
          ""gridX"": 0,
          ""gridY"": 0,
          ""frames"": [
            {{
              ""resourceType"": ""GMSpriteFrame"",
              ""resourceVersion"": ""1.1"",
              ""name"": ""{guid1}"",
            }},
          ],
          ""sequence"": {{
            ""resourceType"": ""GMSequence"",
            ""resourceVersion"": ""1.4"",
            ""name"": ""{tilesprPrefix}{tileSet.Name.Content}"",
            ""timeUnits"": 1,
            ""playback"": 1,
            ""playbackSpeed"": 1.0,
            ""playbackSpeedType"": 1,
            ""autoRecord"": true,
            ""volume"": 1.0,
            ""length"": 1.0,
            ""events"": {{
              ""Keyframes"": [],
              ""resourceVersion"": ""1.0"",
              ""resourceType"": ""KeyframeStore<MessageEventKeyframe>"",
            }},
            ""moments"": {{
              ""Keyframes"": [],
              ""resourceVersion"": ""1.0"",
              ""resourceType"": ""KeyframeStore<MomentsEventKeyframe>"",
            }},
            ""tracks"": [
              {{
                ""resourceType"": ""GMSpriteFramesTrack"",
                ""resourceVersion"": ""1.0"",
                ""name"": ""frames"",
                ""spriteId"": null,
                ""keyframes"": {{
                  ""Keyframes"": [
                    {{
                      ""id"": ""{Guid.NewGuid()}"",
                      ""Key"": 0.0,
                      ""Length"": 1.0,
                      ""Stretch"": false,
                      ""Disabled"": false,
                      ""IsCreationKey"": false,
                      ""Channels"": {{
                        ""0"": {{
                          ""Id"": {{
                            ""name"": ""{guid1}"",
                            ""path"": ""sprites/{tilesprPrefix}{tileSet.Name.Content}/{tilesprPrefix}{tileSet.Name.Content}.yy"",
                          }},
                          ""resourceVersion"": ""1.0"",
                          ""resourceType"": ""SpriteFrameKeyframe"",
                        }},
                      }},
                      ""resourceVersion"": ""1.0"",
                      ""resourceType"": ""Keyframe<SpriteFrameKeyframe>"",
                    }},
                  ],
                  ""resourceVersion"": ""1.0"",
                  ""resourceType"": ""KeyframeStore<SpriteFrameKeyframe>"",
                }},
                ""trackColour"": 0,
                ""inheritsTrackColour"": true,
                ""builtinName"": 0,
                ""traits"": 0,
                ""interpolation"": 1,
                ""tracks"": [],
                ""events"": [],
                ""isCreationTrack"": false,
                ""modifiers"": [],
              }},
            ],
            ""visibleRange"": {{
              ""x"": 0.0,
              ""y"": 0.0,
            }},
            ""lockOrigin"": false,
            ""xorigin"": 0,
            ""yorigin"": 0,
            ""eventToFunction"": {{}},
            ""eventStubScript"": null,
          }},
          ""layers"": [
            {{
              ""resourceType"": ""GMImageLayer"",
              ""resourceVersion"": ""1.0"",
              ""name"": ""{guid2}"",
              ""visible"": true,
              ""isLocked"": false,
              ""blendMode"": 0,
              ""opacity"": 100.0,
              ""displayName"": ""default"",
            }},
          ],
          ""nineSlice"": null,
          ""parent"": {{
            ""name"": ""Sprites"",
            ""path"": ""folders/Sprites.yy"",
          }},
        }}
    ";

    File.WriteAllText(tilesetSprPath + tilesprPrefix + tileSet.Name.Content + ".yy", sprJson);
    string json = JsonConvert.SerializeObject(tilesetData, Formatting.Indented);
    File.WriteAllText(tilesetPath + tileSet.Name.Content + ".yy", json);
    IncrementProgressParallel();
}));
await StopProgressBarUpdater();
HideProgressBar();

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}