using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSequence;

namespace UndertaleModTool.Windows
{
    public class PredicateForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
        public Func<UndertaleResource, HashSet<Type>, (string, object[])[]> Predicate { get; set; }
    }

    public static class UndertaleResourceReferenceMethodsMap
    {
        private static UndertaleData data;

        private static readonly Dictionary<Type, PredicateForVersion[]> typeMap = new()
        {
            {
                typeof(UndertaleSprite),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleGameObject)))
                                return null;

                            var gameObjects = data.GameObjects.Where(x => x.Sprite == obj);
                            if (gameObjects.Any())
                                return new(string, object[])[] {("Game objects", gameObjects.ToArray()) };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            List<object[]> tiles = new();
                            List<object[]> sprInstances = new();
                            List<object[]> bgLayers = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var layer in room.Layers)
                                {
                                    if (layer.AssetsData is not null)
                                    {
                                        if (types.Contains(typeof(UndertaleRoom.Tile)))
                                        {
                                            foreach (var tile in layer.AssetsData.LegacyTiles)
                                                if (tile.SpriteDefinition == obj)
                                                    tiles.Add(new object[] { tile, layer, room });
                                        }

                                        if (types.Contains(typeof(UndertaleRoom.SpriteInstance)))
                                        {
                                            foreach (var sprInst in layer.AssetsData.Sprites)
                                                if (sprInst.Sprite == obj)
                                                    sprInstances.Add(new object[] { sprInst, layer, room });
                                        }
                                    }

                                    if (types.Contains(typeof(UndertaleRoom.Layer)))
                                    {
                                        if (layer.BackgroundData is not null
                                            && layer.BackgroundData.Sprite == obj)
                                            bgLayers.Add(new object[] { layer, room });
                                    }
                                }
                            }
                            if (tiles.Count > 0)
                                outList = outList.Append(("Room tiles", tiles.ToArray()));
                            if (sprInstances.Count > 0)
                                outList = outList.Append(("Room sprite instances", sprInstances.ToArray()));
                            if (bgLayers.Count > 0)
                                outList = outList.Append(("Room background layers", bgLayers.ToArray()));

                            if (outList == Enumerable.Empty<(string, object[])>())
                                return null;
                            return outList.ToArray();
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            var textGroups = data.TextureGroupInfo.Where(x => x.Sprites.Any(s => s.Resource == obj)
                                                                              || (x.SpineSprites?.Any(s => s.Resource == obj) == true));
                            if (textGroups.Any())
                                return new (string, object[])[] { ("Texture groups", textGroups.ToArray()) };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleBackground),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            if (data.IsGameMaker2())
                                return null;

                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            List<object[]> backgrounds = new();
                            List<object[]> tiles = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var bg in room.Backgrounds)
                                    if (bg.BackgroundDefinition == obj)
                                        backgrounds.Add(new object[] { bg, room });

                                foreach (var tile in room.Tiles)
                                    if (tile.BackgroundDefinition == obj)
                                        tiles.Add(new object[] { tile, room });
                            }
                            if (backgrounds.Count > 0)
                                outList = outList.Append(("Room tiles", tiles.ToArray()));
                            if (tiles.Count > 0)
                                outList = outList.Append(("Room sprite instances", tiles.ToArray()));

                            if (outList == Enumerable.Empty<(string, object[])>())
                                return null;
                            return outList.ToArray();
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            List<object[]> tileLayers = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var layer in room.Layers)
                                {
                                    if (layer.TilesData is not null
                                        && layer.TilesData.Background == obj)
                                        tileLayers.Add(new object[] { layer, room });

                                }
                            }
                            if (tileLayers.Count > 0)
                                return new (string, object[])[] { ("Room tile layers", tileLayers.ToArray()) };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            var textGroups = data.TextureGroupInfo.Where(x => x.Tilesets.Any(s => s.Resource == obj));
                            if (textGroups.Any())
                                return new (string, object[])[] { ("Texture groups", textGroups.ToArray()) };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleEmbeddedTexture),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            var pageItems = data.TexturePageItems.Where(x => x.TexturePage == obj);
                            if (pageItems.Any())
                                return new (string, object[])[] { ("Texture page items", pageItems.ToArray()) };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            var textGroups = data.TextureGroupInfo.Where(x => x.TexturePages.Any(s => s.Resource == obj));
                            if (textGroups.Any())
                                return new (string, object[])[] { ("Texture groups", textGroups.ToArray()) };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleString),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            if (types.Contains(typeof(UndertaleBackground)))
                            {
                                var backgrounds = data.Backgrounds.Where(x => x.Name == obj);
                                if (backgrounds.Any())
                                    outList = outList.Append((data.IsGameMaker2() ? "Tile sets" : "Backgrounds", backgrounds.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleCode)))
                            {
                                var codeEntries = data.Code.Where(x => x.Name == obj);
                                if (codeEntries.Any())
                                    outList = outList.Append(("Code entries", codeEntries.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleSound)))
                            {
                                var sounds = data.Sounds.Where(x => x.Name == obj || x.Type == obj || x.File == obj);
                                if (sounds.Any())
                                    outList = outList.Append(("Sounds", sounds.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleAudioGroup)))
                            {
                                var audioGroups = data.AudioGroups.Where(x => x.Name == obj);
                                if (audioGroups.Any())
                                    outList = outList.Append(("Audio groups", audioGroups.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleSprite)))
                            {
                                var sprites = data.Sprites.Where(x => x.Name == obj);
                                if (sprites.Any())
                                    outList = outList.Append(("Sprites", sprites.ToArray()));
                            }
                            

                            if (types.Contains(typeof(UndertaleExtension)))
                            {
                                var extensions = data.Extensions.Where(x => x.Name == obj || x.ClassName == obj || x.FolderName == obj);
                                if (extensions.Any())
                                    outList = outList.Append(("Extensions", extensions.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleExtensionOption)))
                            {
                                List<object[]> extnOptions = new();
                                foreach (var extn in data.Extensions)
                                {
                                    foreach (var option in extn.Options)
                                        if (option.Name == obj || option.Value == obj)
                                            extnOptions.Add(new object[] { option, extn });
                                }
                                if (extnOptions.Count > 0)
                                    outList = outList.Append(("Extension options", extnOptions.ToArray()));
                            }

                            List<object[]> extnFunctions = new();
                            List<object[]> extnFiles = new();
                            foreach (var extn in data.Extensions)
                            {
                                foreach (var file in extn.Files)
                                {
                                    if (types.Contains(typeof(UndertaleExtensionFile)))
                                    {
                                        if (file.Filename == obj || file.InitScript == obj || file.CleanupScript == obj)
                                            extnFiles.Add(new object[] { file, extn });
                                    }

                                    if (types.Contains(typeof(UndertaleExtensionFunction)))
                                    {
                                        foreach (var func in file.Functions)
                                            if (func.Name == obj || func.ExtName == obj)
                                                extnFunctions.Add(new object[] { func, file, extn });
                                    }
                                }
                            }
                            if (extnFunctions.Count > 0)
                                outList = outList.Append(("Extension functions", extnFunctions.ToArray()));
                            if (extnFiles.Count > 0)
                                outList = outList.Append(("Extension files", extnFiles.ToArray()));

                            if (types.Contains(typeof(UndertaleFont)))
                            {
                                var fonts = data.Fonts.Where(x => x.Name == obj || x.DisplayName == obj);
                                if (fonts.Any())
                                    outList = outList.Append(("Fonts", fonts.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleFunction)))
                            {
                                var functions = data.Functions.Where(x => x.Name == obj);
                                if (functions.Any())
                                    outList = outList.Append(("Functions", functions.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = data.GameObjects.Where(x => x.Name == obj);
                                if (gameObjects.Any())
                                    outList = outList.Append(("Game objects", gameObjects.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleGeneralInfo)))
                            {
                                bool genInfoMatches = data.GeneralInfo.Name == obj || data.GeneralInfo.FileName == obj
                                                      || data.GeneralInfo.Config == obj || data.GeneralInfo.DisplayName == obj;
                                if (genInfoMatches)
                                    outList = outList.Append(("General Info", new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) }));
                            }

                            if (types.Contains(typeof(UndertaleOptions.Constant)))
                            {
                                bool constantsMatches = data.Options.Constants.Any(x => x.Name == obj || x.Value == obj);
                                if (constantsMatches)
                                    outList = outList.Append(("Game options constants", new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) }));
                            }

                            if (types.Contains(typeof(UndertaleLanguage)))
                            {
                                bool langsMatches = data.Language.EntryIDs.Any(x => x == obj)
                                                    || data.Language.Languages.Any(x => x.Name == obj || x.Region == obj
                                                                                        || x.Entries.Any(e => e == obj));
                                if (langsMatches)
                                    outList = outList.Append(("Languages", new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) }));
                            }

                            if (types.Contains(typeof(UndertalePath)))
                            {
                                var paths = data.Paths.Where(x => x.Name == obj);
                                if (paths.Any())
                                    outList = outList.Append(("Paths", paths.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleRoom)))
                            {
                                var rooms = data.Rooms.Where(x => x.Name == obj || x.Caption == obj);
                                if (rooms.Any())
                                    outList = outList.Append(("Rooms", rooms.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleScript)))
                            {
                                var scripts = data.Scripts.Where(x => x.Name == obj);
                                if (scripts.Any())
                                    outList = outList.Append(("Scripts", scripts.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleShader)))
                            {
                                var shaders = data.Shaders.Where(x => x.Name == obj
                                                                      || x.GLSL_ES_Vertex == obj || x.GLSL_Vertex == obj || x.HLSL9_Vertex == obj
                                                                      || x.GLSL_ES_Fragment == obj || x.GLSL_Fragment == obj || x.HLSL9_Fragment == obj
                                                                      || x.VertexShaderAttributes.Any(a => a.Name == obj));
                                if (shaders.Any())
                                    outList = outList.Append(("Shaders", shaders.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleTimeline)))
                            {
                                var timelines = data.Timelines.Where(x => x.Name == obj);
                                if (timelines.Any())
                                    outList = outList.Append(("Timelines", timelines.ToArray()));
                            }

                            if (outList == Enumerable.Empty<(string, object[])>())
                                return null;
                            return outList.ToArray();
                        }
                    },
                    new PredicateForVersion()
                    {
                        // Bytecode version 15
                        Version = (15, uint.MaxValue, uint.MaxValue),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleCodeLocals)))
                                return null;

                            var codeLocals = data.CodeLocals.Where(x => x.Name == obj || x.Locals.Any(l => l.Name == obj));
                            if (codeLocals.Any())
                                return new (string, object[])[] { ("Code locals", codeLocals.ToArray()) };
                            else
                                return null;
                        }
                    },
                    /*new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            if (types.Contains(typeof(Undertale)))
                            {
                                var  = data..Where(x => x.Name == obj);
                                if (.Any())
                                    outList = outList.Append(("", .ToArray()));
                            }

                            if (outList == Enumerable.Empty<(string, object[])>())
                                return null;
                            return outList.ToArray();
                        }
                    },*/
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            if (types.Contains(typeof(UndertaleEmbeddedImage)))
                            {
                                var embImages = data.EmbeddedImages.Where(x => x.Name == obj);
                                if (embImages.Any())
                                    outList = outList.Append(("Embedded images", embImages.ToArray()));
                            }

                            List<object[]> layers = new();
                            List<object[]> sprInstances = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var layer in room.Layers)
                                {
                                    if (types.Contains(typeof(UndertaleRoom.Layer)))
                                    {
                                        if (layer.LayerName == obj || layer.EffectType == obj)
                                            layers.Add(new object[] { layer, room });
                                    }

                                    if (layer.AssetsData is not null)
                                    {
                                        if (types.Contains(typeof(UndertaleRoom.SpriteInstance)))
                                        {
                                            foreach (var sprInst in layer.AssetsData.Sprites)
                                                if (sprInst.Name == obj)
                                                    sprInstances.Add(new object[] { sprInst, layer, room });
                                        }
                                    }
                                }

                                foreach (var tile in room.Tiles)
                                    if (tile.BackgroundDefinition == obj)
                                        sprInstances.Add(new object[] { tile, room });
                            }
                            if (layers.Count > 0)
                                outList = outList.Append(("Room layers", layers.ToArray()));
                            if (sprInstances.Count > 0)
                                outList = outList.Append(("Room sprite instances", sprInstances.ToArray()));

                            if (outList == Enumerable.Empty<(string, object[])>())
                                return null;
                            return outList.ToArray();
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            var textGroups = data.TextureGroupInfo.Where(x => x.Name == obj);
                            if (textGroups.Any())
                                return new (string, object[])[] { ("Texture groups", textGroups.ToArray()) };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 3, 0),
                        Predicate = (obj, types) =>
                        {
                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            if (types.Contains(typeof(UndertaleAnimationCurve)))
                            {
                                var animCurves = data.AnimationCurves.Where(x => x.Name == obj);
                                if (animCurves.Any())
                                    outList = outList.Append(("Animation curves", animCurves.ToArray()));
                            }
                            if (types.Contains(typeof(UndertaleAnimationCurve.Channel)))
                            {
                                List<object[]> animCurveChannels = new();
                                foreach (var curve in data.AnimationCurves)
                                {
                                    foreach (var ch in curve.Channels)
                                        if (ch.Name == obj)
                                            animCurveChannels.Add(new object[] { ch, curve });
                                }

                                if (animCurveChannels.Count > 0)
                                    outList = outList.Append(("Animation curve channels", animCurveChannels.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleRoom.SequenceInstance)))
                            {
                                List<object[]> seqInstances = new();
                                foreach (var room in data.Rooms)
                                {
                                    foreach (var layer in room.Layers)
                                    {
                                        if (layer.AssetsData is not null)
                                        {
                                            foreach (var seqInst in layer.AssetsData.Sequences)
                                                if (seqInst.Sequence == obj)
                                                    seqInstances.Add(new object[] { seqInst, layer, room });
                                        }
                                    }
                                }
                                if (seqInstances.Count > 0)
                                    outList = outList.Append(("Room sequence instances", seqInstances.ToArray()));
                            }

                            if (types.Contains(typeof(UndertaleSequence)))
                            {
                                var sequences = data.Sequences.Where(x => x.Name == obj
                                                                          || x.FunctionIDs.Values.Any(i => i == obj));
                                if (sequences.Any())
                                    outList = outList.Append(("Sequences", sequences.ToArray()));
                            }

                            List<object[]> sequenceTracks = new();
                            List<object[]> seqBroadMessages = new();
                            List<object[]> sequenceMoments = new();
                            List<object[]> seqStringKeyframes = new();
                            foreach (var seq in data.Sequences)
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    if (types.Contains(typeof(Track)))
                                    {
                                        if (track.Name == obj || track.ModelName == obj || track.GMAnimCurveString == obj)
                                            sequenceTracks.Add(new object[] { track, seq });
                                    }

                                    if (types.Contains(typeof(StringKeyframes)))
                                    {
                                        if (track.Keyframes is StringKeyframes strKeyframes)
                                        {
                                            foreach (var data in strKeyframes.List)
                                            {
                                                foreach (var strPair in data.Channels)
                                                    if (strPair.Value.Value == obj)
                                                        seqStringKeyframes.Add(new object[] { strPair.Key, data, seq });
                                            }
                                        }
                                    }
                                }

                                if (types.Contains(typeof(BroadcastMessage)))
                                {
                                    foreach (var keyframe in seq.BroadcastMessages)
                                    {
                                        foreach (var msgPair in keyframe.Channels)
                                            if (msgPair.Value.Messages.Any(x => x == obj))
                                                seqBroadMessages.Add(new object[] { msgPair.Key, keyframe, seq });
                                    }
                                }

                                if (types.Contains(typeof(Moment)))
                                {
                                    foreach (var keyframe in seq.Moments)
                                    {
                                        foreach (var momentPair in keyframe.Channels)
                                            if (momentPair.Value.Event == obj)
                                                sequenceMoments.Add(new object[] { momentPair.Key, keyframe, seq });
                                    }
                                }
                            }
                            if (sequenceTracks.Count > 0)
                                outList = outList.Append(("Sequence tracks", sequenceTracks.ToArray()));
                            if (seqBroadMessages.Count > 0)
                                outList = outList.Append(("Sequence broadcast messages", seqBroadMessages.ToArray()));
                            if (sequenceMoments.Count > 0)
                                outList = outList.Append(("Sequence moments", sequenceMoments.ToArray()));
                            if (seqStringKeyframes.Count > 0)
                                outList = outList.Append(("Sequence string keyframes", seqStringKeyframes.ToArray()));

                            if (outList == Enumerable.Empty<(string, object[])>())
                                return null;
                            return outList.ToArray();
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 3, 6),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleFilterEffect)))
                                return null;

                            var filterEffects = data.FilterEffects.Where(x => x.Name == obj || x.Value == obj);
                            if (filterEffects.Any())
                                return new (string, object[])[] { ("Filter effects", filterEffects.ToArray()) };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2022, 1, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleRoom.EffectProperty)))
                                return null;

                            List<object[]> effectProps = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var layer in room.Layers)
                                {
                                    foreach (var prop in layer.EffectProperties)
                                        if (prop.Name == obj)
                                            effectProps.Add(new object[] { prop, layer, room });
                                }
                            }
                            if (effectProps.Count > 0)
                                return new (string, object[])[] { ("Room effect properties", effectProps.ToArray()) };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2022, 2, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(TextKeyframes)))
                                return null;

                            List<object[]> textKeyframesList = new();
                            foreach (var seq in data.Sequences)
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    if (track.Keyframes is TextKeyframes textKeyframes)
                                    {
                                        foreach (var data in textKeyframes.List)
                                        {
                                            foreach (var textPair in data.Channels)
                                                if (textPair.Value.Text == obj)
                                                    textKeyframesList.Add(new object[] { textPair.Key, data, seq });
                                        }
                                    }
                                }
                            }
                            if (textKeyframesList.Count > 0)
                                return new (string, object[])[] { ("Sequence text keyframes", textKeyframesList.ToArray()) };
                            else
                                return null;
                        }
                    }
                }
            }
        };


        public static (string, object[])[] GetReferencesOfObject(UndertaleResource obj, UndertaleData data, HashSet<Type> types)
        {
            if (obj is null)
                return null;

            if (!typeMap.TryGetValue(obj.GetType(), out PredicateForVersion[] predicatesForVer))
                return null;

            UndertaleResourceReferenceMethodsMap.data = data;

            var ver = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();
            foreach (var predicateForVer in predicatesForVer)
            {
                bool isAtLeast = false;
                if (predicateForVer.Version.Minor == uint.MaxValue)
                    isAtLeast = predicateForVer.Version.Major <= data.GeneralInfo.BytecodeVersion;
                else
                    isAtLeast = predicateForVer.Version.CompareTo(ver) <= 0;

                if (isAtLeast)
                {
                    var result = predicateForVer.Predicate(obj, types);
                    if (result is not null)
                        outList = outList.Concat(result);
                }  
            }

            if (outList == Enumerable.Empty<(string, object[])>())
                return null;

            return outList.ToArray();
        }
    }
}
