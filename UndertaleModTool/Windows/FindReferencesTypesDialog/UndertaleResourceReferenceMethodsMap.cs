using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSequence;

namespace UndertaleModTool.Windows
{
    public class HashSetOverride<T> : HashSet<T>
    {
        private readonly bool containsEverything;
        public HashSetOverride(bool containsEverything = false)
        {
            this.containsEverything = containsEverything;
        }
        public new bool Contains(T item) => containsEverything || base.Contains(item);
    }

    public class PredicateForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
        public Func<UndertaleResource, HashSetOverride<Type>, Dictionary<string, object[]>> Predicate { get; set; }
    }

    public static class UndertaleResourceReferenceMethodsMap
    {
        private static UndertaleData data;
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static Dictionary<UndertaleCode, List<UndertaleString>> stringReferences;

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
                                return new() { { "Game objects", gameObjects.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            Dictionary<string, object[]> outDict = new();

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
                                outDict["Room tiles"] = tiles.ToArray();
                            if (sprInstances.Count > 0)
                                outDict["Room sprite instances"] = sprInstances.ToArray();
                            if (bgLayers.Count > 0)
                                outDict["Room background layers"] = bgLayers.ToArray();

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
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
                                return new() { { "Texture groups", textGroups.ToArray() } };
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

                            Dictionary<string, object[]> outDict = new();

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
                                outDict["Room tiles"] = tiles.ToArray();
                            if (tiles.Count > 0)
                                outDict["Room sprite instances"] = tiles.ToArray();

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
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
                                return new() { { "Room tile layers", tileLayers.ToArray() } };
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
                                return new() { { "Texture groups", textGroups.ToArray() } };
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
                                return new() { { "Texture page items", pageItems.ToArray() } };
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
                                return new() { { "Texture groups", textGroups.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleTexturePageItem),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleSprite)))
                            {
                                var sprites = data.Sprites.Where(x => x.Textures.Any(t => t.Texture == obj));
                                if (sprites.Any())
                                    outDict["Sprites"] = sprites.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleBackground)))
                            {
                                var backgrounds = data.Backgrounds.Where(x => x.Texture == obj);
                                if (backgrounds.Any())
                                    outDict[data.IsGameMaker2() ? "Tile sets" : "Backgrounds"] = backgrounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleFont)))
                            {
                                var fonts = data.Fonts.Where(x => x.Texture == obj);
                                if (fonts.Any())
                                    outDict["Fonts"] = fonts.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleEmbeddedImage)))
                                return null;

                            var embImages = data.EmbeddedImages.Where(x => x.TextureEntry == obj);
                            if (embImages.Any())
                                return new() { { "Embedded images", embImages.ToArray() } };
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
                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleBackground)))
                            {
                                var backgrounds = data.Backgrounds.Where(x => x.Name == obj);
                                if (backgrounds.Any())
                                    outDict[data.IsGameMaker2() ? "Tile sets" : "Backgrounds"] = backgrounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleCode)))
                            {
                                var codeEntries = data.Code.Where(x => x.Name == obj);
                                IEnumerable<UndertaleCode> stringRefs;
                                if (stringReferences is not null)
                                    stringRefs = stringReferences.Where(x => x.Value.Any(x => x == obj))
                                                                 .Select(x => x.Key);
                                else
                                    stringRefs = data.Code.Where(x => x.Instructions.Any(
                                                                        i => i.Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> strPtr
                                                                             && strPtr.Resource == obj));

                                codeEntries = codeEntries.Concat(stringRefs);

                                if (codeEntries.Any())
                                    outDict["Code entries (name and contents)"] = codeEntries.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleSound)))
                            {
                                var sounds = data.Sounds.Where(x => x.Name == obj || x.Type == obj || x.File == obj);
                                if (sounds.Any())
                                    outDict["Sounds"] = sounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleAudioGroup)))
                            {
                                var audioGroups = data.AudioGroups.Where(x => x.Name == obj);
                                if (audioGroups.Any())
                                    outDict["Audio groups"] = audioGroups.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleSprite)))
                            {
                                var sprites = data.Sprites.Where(x => x.Name == obj);

                                if (data.IsVersionAtLeast(2, 3, 0))
                                {
                                    sprites = sprites.Concat(data.Sprites.Where(x => x.V2Sequence is not null
                                                                                     && x.V2Sequence.Tracks.Count != 0
                                                                                     && (x.V2Sequence.Tracks[0].Name == obj
                                                                                         || x.V2Sequence.Tracks[0].ModelName == obj)));
                                }

                                if (sprites.Any())
                                    outDict["Sprites"] = sprites.ToArray();
                            }


                            if (types.Contains(typeof(UndertaleExtension)))
                            {
                                var extensions = data.Extensions.Where(x => x.Name == obj || x.ClassName == obj || x.FolderName == obj);
                                if (extensions.Any())
                                    outDict["Extensions"] = extensions.ToArray();
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
                                    outDict["Extension options"] = extnOptions.ToArray();
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
                                outDict["Extension functions"] = extnFunctions.ToArray();
                            if (extnFiles.Count > 0)
                                outDict["Extension files"] = extnFiles.ToArray();

                            if (types.Contains(typeof(UndertaleFont)))
                            {
                                var fonts = data.Fonts.Where(x => x.Name == obj || x.DisplayName == obj);
                                if (fonts.Any())
                                    outDict["Fonts"] = fonts.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleFunction)))
                            {
                                var functions = data.Functions.Where(x => x.Name == obj);
                                if (functions.Any())
                                    outDict["Functions"] = functions.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = data.GameObjects.Where(x => x.Name == obj);
                                if (gameObjects.Any())
                                    outDict["Game objects"] = gameObjects.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleGeneralInfo)))
                            {
                                bool genInfoMatches = data.GeneralInfo.Name == obj || data.GeneralInfo.FileName == obj
                                                      || data.GeneralInfo.Config == obj || data.GeneralInfo.DisplayName == obj;
                                if (genInfoMatches)
                                    outDict["General Info"] = new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) };
                            }

                            if (types.Contains(typeof(UndertaleOptions.Constant)))
                            {
                                bool constantsMatches = data.Options.Constants.Any(x => x.Name == obj || x.Value == obj);
                                if (constantsMatches)
                                    outDict["Game options constants"] = new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) };
                            }

                            if (types.Contains(typeof(UndertaleLanguage)))
                            {
                                bool langsMatches = data.Language.EntryIDs.Any(x => x == obj)
                                                    || data.Language.Languages.Any(x => x.Name == obj || x.Region == obj
                                                                                        || x.Entries.Any(e => e == obj));
                                if (langsMatches)
                                    outDict["Languages"] = new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) };
                            }

                            if (types.Contains(typeof(UndertalePath)))
                            {
                                var paths = data.Paths.Where(x => x.Name == obj);
                                if (paths.Any())
                                    outDict["Paths"] = paths.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom)))
                            {
                                var rooms = data.Rooms.Where(x => x.Name == obj || x.Caption == obj);
                                if (rooms.Any())
                                    outDict["Rooms"] = rooms.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleScript)))
                            {
                                var scripts = data.Scripts.Where(x => x.Name == obj);
                                if (scripts.Any())
                                    outDict["Scripts"] = scripts.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleShader)))
                            {
                                var shaders = data.Shaders.Where(x => x.Name == obj
                                                                      || x.GLSL_ES_Vertex == obj || x.GLSL_Vertex == obj || x.HLSL9_Vertex == obj
                                                                      || x.GLSL_ES_Fragment == obj || x.GLSL_Fragment == obj || x.HLSL9_Fragment == obj
                                                                      || x.VertexShaderAttributes.Any(a => a.Name == obj));
                                if (shaders.Any())
                                    outDict["Shaders"] = shaders.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleTimeline)))
                            {
                                var timelines = data.Timelines.Where(x => x.Name == obj);
                                if (timelines.Any())
                                    outDict["Timelines"] = timelines.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
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
                                return new() { { "Code locals", codeLocals.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleEmbeddedImage)))
                            {
                                var embImages = data.EmbeddedImages.Where(x => x.Name == obj);
                                if (embImages.Any())
                                    outDict["Embedded images"] = embImages.ToArray();
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
                                outDict["Room layers"] = layers.ToArray();
                            if (sprInstances.Count > 0)
                                outDict["Room sprite instances"] = sprInstances.ToArray();

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
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
                                return new() { { "Texture groups", textGroups.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 3, 0),
                        Predicate = (obj, types) =>
                        {
                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleAnimationCurve)))
                            {
                                var animCurves = data.AnimationCurves.Where(x => x.Name == obj);
                                if (animCurves.Any())
                                    outDict["Animation curves"] = animCurves.ToArray();
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
                                    outDict["Animation curve channels"] = animCurveChannels.ToArray();
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
                                                if (seqInst.Sequence.Name == obj)
                                                    seqInstances.Add(new object[] { seqInst, layer, room });
                                        }
                                    }
                                }
                                if (seqInstances.Count > 0)
                                    outDict["Room sequence instances"] = seqInstances.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleSequence)))
                            {
                                var sequences = data.Sequences.Where(x => x.Name == obj
                                                                          || x.FunctionIDs.Values.Any(i => i == obj));
                                if (sequences.Any())
                                    outDict["Sequences"] = sequences.ToArray();
                            }


                            List<object[]> sequenceTracks = new();
                            List<object[]> seqBroadMessages = new();
                            List<object[]> sequenceMoments = new();
                            List<object[]> seqStringKeyframes = new();

                            void ProcessTrack(UndertaleSequence seq, Track track, List<object> trackChain)
                            {
                                trackChain = new(trackChain);
                                trackChain.Insert(0, track);
                                if (types.Contains(typeof(Track)))
                                {
                                    if (track.Name == obj || track.ModelName == obj || track.GMAnimCurveString == obj)
                                        sequenceTracks.Add(trackChain.Append(seq).ToArray());
                                }

                                if (types.Contains(typeof(StringKeyframes)))
                                {
                                    if (track.Keyframes is StringKeyframes strKeyframes)
                                    {
                                        foreach (var data in strKeyframes.List)
                                        {
                                            foreach (var strPair in data.Channels)
                                            {
                                                if (strPair.Value.Value == obj)
                                                    seqStringKeyframes.Add(new object[] { strPair.Key }.Concat(trackChain).Append(seq).ToArray());
                                            }
                                        }
                                    }
                                }

                                foreach (var subTrack in track.Tracks)
                                    ProcessTrack(seq, subTrack, trackChain);
                            };

                            foreach (var seq in data.Sequences)
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    List<object> trackChain = new();
                                    ProcessTrack(seq, track, trackChain);
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
                                outDict["Sequence tracks"] = sequenceTracks.ToArray();
                            if (seqBroadMessages.Count > 0)
                                outDict["Sequence broadcast messages"] = seqBroadMessages.ToArray();
                            if (sequenceMoments.Count > 0)
                                outDict["Sequence moments"] = sequenceMoments.ToArray();
                            if (seqStringKeyframes.Count > 0)
                                outDict["Sequence string keyframes"] = seqStringKeyframes.ToArray();

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
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
                                return new() { { "Filter effects", filterEffects.ToArray() } };
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
                                        if (prop.Name == obj || prop.Value == obj)
                                            effectProps.Add(new object[] { prop, layer, room });
                                }
                            }
                            if (effectProps.Count > 0)
                                return new() { { "Room effect properties", effectProps.ToArray() } };
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

                            void ProcessTrack(UndertaleSequence seq, Track track, List<object> trackChain)
                            {
                                trackChain = new(trackChain);
                                trackChain.Insert(0, track);

                                if (track.Keyframes is TextKeyframes textKeyframes)
                                {
                                    foreach (var keyframe in textKeyframes.List)
                                    {
                                        foreach (var textPair in keyframe.Channels)
                                            if (textPair.Value.Text == obj)
                                                textKeyframesList.Add(new object[] { textPair.Key }.Concat(trackChain).Append(seq).ToArray());
                                    }
                                }

                                foreach (var subTrack in track.Tracks)
                                    ProcessTrack(seq, subTrack, trackChain);
                            };

                            foreach (var seq in data.Sequences)
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    List<object> trackChain = new();
                                    ProcessTrack(seq, track, trackChain);
                                }
                            }
                            if (textKeyframesList.Count > 0)
                                return new() { { "Sequence text keyframes", textKeyframesList.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleGameObject),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleRoom.GameObject)))
                                return null;

                            List<object[]> objInstances = new();
                            if (data.IsGameMaker2())
                            {
                                foreach (var room in data.Rooms)
                                {
                                    foreach (var layer in room.Layers)
                                    {
                                        if (layer.InstancesData is not null)
                                        {
                                            foreach (var inst in layer.InstancesData.Instances)
                                                if (inst.ObjectDefinition == obj)
                                                    objInstances.Add(new object[] { inst, layer, room });
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (var room in data.Rooms)
                                {
                                    foreach (var inst in room.GameObjects)
                                        if (inst.ObjectDefinition == obj)
                                            objInstances.Add(new object[] { inst, room });
                                }
                            }

                            if (objInstances.Count > 0)
                                return new() { { "Room object instance", objInstances.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 3, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(InstanceKeyframes)))
                                return null;

                            List<object[]> instKeyframesList = new();

                            void ProcessTrack(UndertaleSequence seq, Track track, List<object> trackChain)
                            {
                                trackChain = new(trackChain);
                                trackChain.Insert(0, track);

                                if (track.Keyframes is InstanceKeyframes instKeyframes)
                                {
                                    foreach (var keyframe in instKeyframes.List)
                                    {
                                        foreach (var instPair in keyframe.Channels)
                                            if (instPair.Value.Resource.Resource == obj)
                                                instKeyframesList.Add(new object[] { instPair.Key }.Concat(trackChain).Append(seq).ToArray());
                                    }
                                }

                                foreach (var subTrack in track.Tracks)
                                    ProcessTrack(seq, subTrack, trackChain);
                            };

                            foreach (var seq in data.Sequences)
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    List<object> trackChain = new();
                                    ProcessTrack(seq, track, trackChain);
                                }
                            }
                            if (instKeyframesList.Count > 0)
                                return new() { { "Sequence object instance keyframes", instKeyframesList.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleCode),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = data.GameObjects.Where(x => x.Events.Any(
                                                                            e => e.Any(se => se.Actions.Any(
                                                                                a => a.CodeId == obj))));
                                if (gameObjects.Any())
                                    outDict["Game objects"] = gameObjects.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom)))
                            {
                                var rooms = data.Rooms.Where(x => x.CreationCodeId == obj);
                                if (rooms.Any())
                                    outDict["Rooms"] = rooms.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleGlobalInit)))
                            {
                                bool matches = data.GlobalInitScripts?.Any(x => x.Code == obj) == true;
                                if (matches)
                                    outDict["Global init"] = new object[] { new GlobalInitEditor(data.GlobalInitScripts) };

                                matches = data.GameEndScripts?.Any(x => x.Code == obj) == true;
                                if (matches)
                                    outDict["Game end scripts"] = new object[] { new GameEndEditor(data.GameEndScripts) };
                            }

                            if (types.Contains(typeof(UndertaleScript)))
                            {
                                var scripts = data.Scripts.Where(x => x.Code == obj);
                                if (scripts.Any())
                                    outDict["Scripts"] = scripts.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    }
                }
            },
            {
                typeof(UndertaleEmbeddedAudio),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleSound)))
                                return null;

                            var sounds = data.Sounds.Where(x => x.AudioFile == obj);
                            if (sounds.Any())
                                return new() { { "Sounds", sounds.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleAudioGroup),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (obj, types) =>
                        {
                            if (!types.Contains(typeof(UndertaleSound)))
                                return null;

                            var sounds = data.Sounds.Where(x => x.AudioGroup == obj);
                            if (sounds.Any())
                                return new() { { "Sounds", sounds.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            }
        };



        public static Dictionary<string, List<object>> GetReferencesOfObject(UndertaleResource obj, UndertaleData data, HashSetOverride<Type> types)
        {
            if (obj is null)
                return null;

            if (!typeMap.TryGetValue(obj.GetType(), out PredicateForVersion[] predicatesForVer))
                return null;

            UndertaleResourceReferenceMethodsMap.data = data;

            var ver = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
            Dictionary<string, List<object>> outDict = new();
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
                    if (result is null)
                        continue;

                    foreach (var entry in result)
                        outDict.Add(entry.Key, new(entry.Value));
                }  
            }

            if (outDict.Count == 0)
                return null;

            return outDict;
        }

        public static async Task<Dictionary<string, List<object>>> GetUnreferencedObjects(UndertaleData data, Dictionary<Type, string> typesDict)
        {
            UndertaleResourceReferenceMethodsMap.data = data;

            var ver = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);

            Dictionary<string, List<object>> outDict = new();

            List<(IList, string)> assetLists = new();
            foreach (var typePair in typesDict)
            {
                if (data[typePair.Key] is not IList resList)
                    continue;

                assetLists.Add((resList, typePair.Value));
            }
            List<(UndertaleResource, string)> assets = new(assetLists.Select(x => x.Item1.Count).Sum());
            foreach (var list in assetLists)
                assets.AddRange(list.Item1.Cast<UndertaleResource>()
                                          .Select(x => (x, list.Item2)));

            stringReferences = new();
            foreach (var code in data.Code)
            {
                var strings = new List<UndertaleString>();
                foreach (var inst in code.Instructions)
                {
                    if (inst.Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> strPtr)
                        strings.Add(strPtr.Resource);
                }

                if (strings.Count != 0)
                    stringReferences[code] = strings;
            }

            mainWindow.IsEnabled = false;
            try
            {
                mainWindow.InitializeProgressDialog("Searching in progress...", "Please wait...");
                mainWindow.SetProgressBar(null, "Assets", 0, assets.Count);
                mainWindow.StartProgressBarUpdater();

                var assetsPart = Partitioner.Create(0, assets.Count);

                List<Dictionary<string, List<object>>> dicts = new();

                await Task.Run(() =>
                {
                    Parallel.ForEach(assetsPart, (range) =>
                    {
                        var resultDict = new Dictionary<string, List<object>>();

                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            var asset = assets[i];
                            var assetReferences = GetReferencesOfObject(asset.Item1, data, new HashSetOverride<Type>(true));
                            if (assetReferences is null)
                            {
                                if (resultDict.TryGetValue(asset.Item2, out var list))
                                {
                                    list.Add(asset.Item1);
                                }
                                else
                                {
                                    resultDict[asset.Item2] = new() { asset.Item1 };
                                }
                            }

                            mainWindow.IncrementProgressParallel();
                        }

                        dicts.Add(resultDict);
                    });
                });

                Dictionary<string, int> outArrSizes = new();
                foreach (var dict in dicts)
                {
                    foreach (var pair in dict)
                    {
                        outArrSizes.TryGetValue(pair.Key, out int count);
                        outArrSizes[pair.Key] = count + pair.Value.Count;
                    }
                }
                foreach (var dict in dicts)
                {
                    foreach (var pair in dict)
                    {
                        if (outDict.TryGetValue(pair.Key, out var list))
                        {
                            if (pair.Value is not null)
                                list.AddRange(pair.Value);
                        }
                        else
                        {
                            int size = outArrSizes[pair.Key];
                            if (size == 0)
                                continue;

                            list = new(size);
                            outDict[pair.Key] = list;

                            list.AddRange(pair.Value);
                        }
                    }
                }

                await mainWindow.StopProgressBarUpdater();
                mainWindow.HideProgressBar();
            }
            catch
            {
                mainWindow.IsEnabled = true;
                throw;
            }
            mainWindow.IsEnabled = true;

            if (outDict.Count == 0)
                return null;
            return outDict;
        }
    }
}
