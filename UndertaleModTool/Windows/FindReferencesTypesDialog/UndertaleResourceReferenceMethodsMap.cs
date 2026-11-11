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
    public class HashSetTypesOverride : HashSet<Type>
    {
        private static bool containsEverything;
        private static HashSet<Type> supportedTypes;

        public static void MakeContainEverything(GameVersion version, bool isYYC)
        {
            containsEverything = true;
            supportedTypes = UndertaleResourceReferenceMap.GetSupportedReferenceTypes(version, isYYC);
        }

        public new bool Contains(Type item)
        {
            if (containsEverything)
                return supportedTypes.Contains(item);

            return base.Contains(item);
        }

        public static void Restore()
        {
            containsEverything = false;
            supportedTypes = null;
        }
    }

    public class PredicateForVersion
    {
        public delegate Dictionary<string, object[]> PredicateDelegate(object objSrc, HashSetTypesOverride types, bool checkOne);

        public GameVersion Version { get; set; }
        public GameVersion BeforeVersion { get; set; } = new((uint.MaxValue, uint.MaxValue, uint.MaxValue), byte.MaxValue);
        public PredicateDelegate Predicate { get; set; }
    }

    public static class UndertaleResourceReferenceMethodsMap
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static readonly Dictionary<string, object[]> emptyResultArr = new();
        private static readonly Dictionary<string, List<object>> emptyResultList = new();

        private static UndertaleData data;
        private static Dictionary<UndertaleCode, HashSet<UndertaleString>> stringReferences;
        private static Dictionary<UndertaleCode, HashSet<UndertaleFunction>> funcReferences;
        private static Dictionary<UndertaleCode, HashSet<UndertaleVariable>> variReferences;
        private static bool ignoreArgumentsVar = false;

        private static IEnumerable<T> NotNullWhere<T>(this IList<T> list, Func<T, bool> predicate)
        {
            return list.Where(x => x is not null && predicate(x));
        }

        private static readonly Dictionary<Type, PredicateForVersion[]> typeMap = new()
        {
            {
                typeof(UndertaleSprite),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleGameObject)))
                                return null;

                            if (objSrc is not UndertaleSprite obj)
                                return null;

                            var gameObjects = data.GameObjects.NotNullWhere(x => x.Sprite == obj);
                            if (gameObjects.Any())
                                return new() { { "Game objects", checkOne ? gameObjects.ToEmptyArray() : gameObjects.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleSprite obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleRoom.Tile)))
                            {
                                IEnumerable<object[]> GetTiles()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var layer in room.Layers)
                                        {
                                            if (layer.AssetsData is not null)
                                            {
                                                foreach (var tile in layer.AssetsData.LegacyTiles)
                                                    if (tile.SpriteDefinition == obj)
                                                        yield return new object[] { tile, layer, room };
                                            }
                                        }
                                    }
                                };

                                var tiles = GetTiles();
                                if (tiles.Any())
                                    outDict["Room tiles"] = checkOne ? tiles.ToEmptyArray() : tiles.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleRoom.SpriteInstance)))
                            {
                                IEnumerable<object[]> GetSprInstances()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var layer in room.Layers)
                                        {
                                            if (layer.AssetsData is not null)
                                            {
                                                foreach (var sprInst in layer.AssetsData.Sprites)
                                                    if (sprInst.Sprite == obj)
                                                        yield return new object[] { sprInst, layer, room };
                                            }
                                        }
                                    }
                                };

                                var sprInstances = GetSprInstances();
                                if (sprInstances.Any())
                                    outDict["Room sprite instances"] = checkOne ? sprInstances.ToEmptyArray() : sprInstances.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleRoom.Layer)))
                            {
                                IEnumerable<object[]> GetBgLayers()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var layer in room.Layers)
                                        {
                                            if (layer.BackgroundData is not null
                                                && layer.BackgroundData.Sprite == obj)
                                                yield return new object[] { layer, room };
                                        }
                                    }
                                };

                                var bgLayers = GetBgLayers();
                                if (bgLayers.Any())
                                    outDict["Room background layers"] = checkOne ? bgLayers.ToEmptyArray() : bgLayers.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            if (objSrc is not UndertaleSprite obj)
                                return null;

                            var textGroups = data.TextureGroupInfo.NotNullWhere(x => x.Sprites.Any(s => s.Resource == obj)
                                                                                     || (x.SpineSprites?.Any(s => s.Resource == obj) == true));
                            if (textGroups.Any())
                                return new() { { "Texture groups", checkOne ? textGroups.ToEmptyArray() : textGroups.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2023, 2, 0), // Not present if it's LTS 2022
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleParticleSystemEmitter)))
                                return null;

                            if (objSrc is not UndertaleSprite obj)
                                return null;

                            var partSysEmitters = data.ParticleSystemEmitters.NotNullWhere(x => x.Sprite == obj);
                            if (partSysEmitters.Any())
                                return new() { { "Particle system emitters", checkOne ? partSysEmitters.ToEmptyArray() : partSysEmitters.ToArray() } };
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (data.IsGameMaker2())
                                return null;

                            if (objSrc is not UndertaleBackground obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleRoom.Background)))
                            {
                                IEnumerable<object[]> GetBackgrounds()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var bg in room.Backgrounds)
                                            if (bg.BackgroundDefinition == obj)
                                                yield return new object[] { bg, room };
                                    }
                                }

                                var backgrounds = GetBackgrounds();
                                if (backgrounds.Any())
                                    outDict["Room backgrounds"] = checkOne ? backgrounds.ToEmptyArray() : backgrounds.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleRoom.Tile)))
                            {
                                IEnumerable<object[]> GetTiles()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var tile in room.Tiles)
                                            if (tile.BackgroundDefinition == obj)
                                                yield return new object[] { tile, room };
                                    }
                                }

                                var tiles = GetTiles();
                                if (tiles.Any())
                                    outDict["Room tiles"] = checkOne ? tiles.ToEmptyArray() : tiles.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleRoom.Layer)))
                                return null;

                            if (objSrc is not UndertaleBackground obj)
                                return null;

                            IEnumerable<object[]> GetTileLayers()
                            {
                                foreach (var room in data.Rooms)
                                {
                                    foreach (var layer in room.Layers)
                                    {
                                        if (layer.TilesData is not null
                                            && layer.TilesData.Background == obj)
                                            yield return new object[] { layer, room };

                                    }
                                }
                            }

                            var tileLayers = GetTileLayers();
                            if (tileLayers.Any())
                                return new() { { "Room tile layers", checkOne ? tileLayers.ToEmptyArray() : tileLayers.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            if (objSrc is not UndertaleBackground obj)
                                return null;

                            var textGroups = data.TextureGroupInfo.NotNullWhere(x => x.Tilesets.Any(s => s.Resource == obj));
                            if (textGroups.Any())
                                return new() { { "Texture groups", checkOne ? textGroups.ToEmptyArray() : textGroups.ToArray() } };
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleEmbeddedTexture obj)
                                return null;

                            var pageItems = data.TexturePageItems.NotNullWhere(x => x.TexturePage == obj);
                            if (pageItems.Any())
                                return new() { { "Texture page items", checkOne ? pageItems.ToEmptyArray() : pageItems.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            if (objSrc is not UndertaleEmbeddedTexture obj)
                                return null;

                            var textGroups = data.TextureGroupInfo.NotNullWhere(x => x.TexturePages.Any(s => s.Resource == obj));
                            if (textGroups.Any())
                                return new() { { "Texture groups", checkOne ? textGroups.ToEmptyArray() : textGroups.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleFont),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            if (objSrc is not UndertaleFont obj)
                                return null;

                            var textGroups = data.TextureGroupInfo.NotNullWhere(x => x.Fonts.Any(s => s.Resource == obj));
                            if (textGroups.Any())
                                return new() { { "Texture groups", checkOne ? textGroups.ToEmptyArray() : textGroups.ToArray() } };
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            Dictionary<string, object[]> outDict = new();

                            if (objSrc is not UndertaleTexturePageItem obj)
                                return null;

                            if (types.Contains(typeof(UndertaleSprite)))
                            {
                                var sprites = data.Sprites.NotNullWhere(x => x.Textures.Any(t => t.Texture == obj));
                                if (sprites.Any())
                                    outDict["Sprites"] = checkOne ? sprites.ToEmptyArray() : sprites.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleBackground)))
                            {
                                var backgrounds = data.Backgrounds.NotNullWhere(x => x.Texture == obj);
                                if (backgrounds.Any())
                                    outDict[data.IsGameMaker2() ? "Tile sets" : "Backgrounds"] = checkOne ? backgrounds.ToEmptyArray() : backgrounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleFont)))
                            {
                                var fonts = data.Fonts.NotNullWhere(x => x.Texture == obj);
                                if (fonts.Any())
                                    outDict["Fonts"] = checkOne ? fonts.ToEmptyArray() : fonts.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleEmbeddedImage)))
                                return null;

                            if (objSrc is not UndertaleTexturePageItem obj)
                                return null;

                            var embImages = data.EmbeddedImages.NotNullWhere(x => x.TextureEntry == obj);
                            if (embImages.Any())
                                return new() { { "Embedded images", checkOne ? embImages.ToEmptyArray() : embImages.ToArray() } };
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleString obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleBackground)))
                            {
                                var backgrounds = data.Backgrounds.NotNullWhere(x => x.Name == obj);
                                if (backgrounds.Any())
                                    outDict[data.IsGameMaker2() ? "Tile sets" : "Backgrounds"] = checkOne ? backgrounds.ToEmptyArray() : backgrounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleCode)))
                            {
                                var codeEntries = data.Code.NotNullWhere(x => x.Name == obj);
                                IEnumerable<UndertaleCode> stringRefs;
                                if (stringReferences is not null)
                                    stringRefs = stringReferences.Where(x => x.Value.Contains(obj))
                                                                 .Select(x => x.Key);
                                else
                                    stringRefs = data.Code.NotNullWhere(x => x.Instructions.Any(i => i.ValueString?.Resource == obj));

                                codeEntries = codeEntries.Concat(stringRefs);

                                if (codeEntries.Any())
                                    outDict["Code entries (name and contents)"] = checkOne ? codeEntries.ToEmptyArray() : codeEntries.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleFunction)))
                            {
                                var functions = data.Functions.NotNullWhere(x => x.Name == obj);
                                if (functions.Any())
                                    outDict["Functions"] = checkOne ? functions.ToEmptyArray() : functions.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleVariable)))
                            {
                                var variables = data.Variables.NotNullWhere(x => x.Name == obj);
                                if (variables.Any())
                                    outDict["Variables"] = checkOne ? variables.ToEmptyArray() : variables.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleSound)))
                            {
                                var sounds = data.Sounds.NotNullWhere(x => x.Name == obj || x.Type == obj || x.File == obj);
                                if (sounds.Any())
                                    outDict["Sounds"] = checkOne ? sounds.ToEmptyArray() : sounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleAudioGroup)))
                            {
                                var audioGroups = data.AudioGroups.NotNullWhere(x => x.Name == obj);
                                if (audioGroups.Any())
                                    outDict["Audio groups"] = checkOne ? audioGroups.ToEmptyArray() : audioGroups.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleSprite)))
                            {
                                var sprites = data.Sprites.NotNullWhere(x => x.Name == obj);

                                if (data.IsVersionAtLeast(2, 3, 0))
                                {
                                    sprites = sprites.Concat(data.Sprites.NotNullWhere(x => x.V2Sequence is not null
                                                                                            && x.V2Sequence.Tracks.Count != 0
                                                                                            && (x.V2Sequence.Tracks[0].Name == obj
                                                                                                || x.V2Sequence.Tracks[0].ModelName == obj)));
                                }

                                if (sprites.Any())
                                    outDict["Sprites"] = checkOne ? sprites.ToEmptyArray() : sprites.ToArray();
                            }


                            if (types.Contains(typeof(UndertaleExtension)))
                            {
                                var extensions = data.Extensions.NotNullWhere(x => x.Name == obj || x.ClassName == obj || x.FolderName == obj);
                                if (extensions.Any())
                                    outDict["Extensions"] = checkOne ? extensions.ToEmptyArray() : extensions.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleExtensionOption)))
                            {
                                IEnumerable<object[]> GetExtnOptions()
                                {
                                    foreach (var extn in data.Extensions.SkipNullItems())
                                    {
                                        foreach (var option in extn.Options)
                                            if (option.Name == obj || option.Value == obj)
                                                yield return new object[] { option, extn };
                                    }
                                }

                                var extnOptions = GetExtnOptions();
                                if (extnOptions.Any())
                                    outDict["Extension options"] = checkOne ? extnOptions.ToEmptyArray() : extnOptions.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleExtensionFile)))
                            {
                                IEnumerable<object[]> GetExtnFiles()
                                {
                                    foreach (var extn in data.Extensions.SkipNullItems())
                                    {
                                        foreach (var file in extn.Files)
                                            if (file.Filename == obj || file.InitScript == obj || file.CleanupScript == obj)
                                                yield return new object[] { file, extn };
                                    }
                                }

                                var extnFiles = GetExtnFiles();
                                if (extnFiles.Any())
                                    outDict["Extension files"] = checkOne ? extnFiles.ToEmptyArray() : extnFiles.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleExtensionFunction)))
                            {
                                IEnumerable<object[]> GetExtnFunctions()
                                {
                                    foreach (var extn in data.Extensions.SkipNullItems())
                                    {
                                        foreach (var file in extn.Files)
                                        {
                                            foreach (var func in file.Functions)
                                                if (func.Name == obj || func.ExtName == obj)
                                                    yield return new object[] { func, file, extn };
                                        }
                                    }
                                }

                                var extnFunctions = GetExtnFunctions();
                                if (extnFunctions.Any())
                                    outDict["Extension functions"] = checkOne ? extnFunctions.ToEmptyArray() : extnFunctions.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleFont)))
                            {
                                var fonts = data.Fonts.NotNullWhere(x => x.Name == obj || x.DisplayName == obj);
                                if (fonts.Any())
                                    outDict["Fonts"] = checkOne ? fonts.ToEmptyArray() : fonts.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = data.GameObjects.NotNullWhere(x => x.Name == obj);
                                if (gameObjects.Any())
                                    outDict["Game objects"] = checkOne ? gameObjects.ToEmptyArray() : gameObjects.ToArray();
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

                            if (types.Contains(typeof(UndertalePath)))
                            {
                                var paths = data.Paths.NotNullWhere(x => x.Name == obj);
                                if (paths.Any())
                                    outDict["Paths"] = checkOne ? paths.ToEmptyArray() : paths.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom)))
                            {
                                var rooms = data.Rooms.NotNullWhere(x => x.Name == obj || x.Caption == obj);
                                if (rooms.Any())
                                    outDict["Rooms"] = checkOne ? rooms.ToEmptyArray() : rooms.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleScript)))
                            {
                                var scripts = data.Scripts.NotNullWhere(x => x.Name == obj);
                                if (scripts.Any())
                                    outDict["Scripts"] = checkOne ? scripts.ToEmptyArray() : scripts.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleShader)))
                            {
                                var shaders = data.Shaders.NotNullWhere(x => x.Name == obj
                                                                             || x.GLSL_ES_Vertex == obj || x.GLSL_Vertex == obj || x.HLSL9_Vertex == obj
                                                                             || x.GLSL_ES_Fragment == obj || x.GLSL_Fragment == obj || x.HLSL9_Fragment == obj
                                                                             || x.VertexShaderAttributes.Any(a => a.Name == obj));
                                if (shaders.Any())
                                    outDict["Shaders"] = checkOne ? shaders.ToEmptyArray() : shaders.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleTimeline)))
                            {
                                var timelines = data.Timelines.NotNullWhere(x => x.Name == obj);
                                if (timelines.Any())
                                    outDict["Timelines"] = checkOne ? timelines.ToEmptyArray() : timelines.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        // Bytecode version 15
                        Version = new(15),
                        BeforeVersion = (2024, 8, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleCodeLocals)))
                                return null;

                            if (objSrc is not UndertaleString obj)
                                return null;

                            var codeLocals = data.CodeLocals.NotNullWhere(x => x.Name == obj || x.Locals.Any(l => l.Name == obj));
                            if (codeLocals.Any())
                                return new() { { "Code locals", checkOne ? codeLocals.ToEmptyArray() : codeLocals.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        // Bytecode version 16
                        Version = new(16),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleLanguage)))
                                return null;

                            if (objSrc is not UndertaleString obj)
                                return null;

                            bool langsMatches = data.Language.EntryIDs.Contains(obj)
                                                || data.Language.Languages.Any(x => x.Name == obj || x.Region == obj
                                                                                    || x.Entries.Contains(obj));

                            if (langsMatches)
                                return new() { { "Languages",  new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) } } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleString obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleEmbeddedImage)))
                            {
                                var embImages = data.EmbeddedImages.NotNullWhere(x => x.Name == obj);
                                if (embImages.Any())
                                    outDict["Embedded images"] = checkOne ? embImages.ToEmptyArray() : embImages.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom.Layer)))
                            {
                                IEnumerable<object[]> GetLayers()
                                {
                                    foreach (var room in data.Rooms)
                                        foreach (var layer in room.Layers)
                                            if (layer.LayerName == obj || layer.EffectType == obj)
                                                yield return new object[] { layer, room };
                                }

                                var layers = GetLayers();
                                if (layers.Any())
                                    outDict["Room layers"] = checkOne ? layers.ToEmptyArray() : layers.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleRoom.SpriteInstance)))
                            {
                                IEnumerable<object[]> GetSprInstances()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var layer in room.Layers)
                                        {
                                            if (layer.AssetsData is not null)
                                            {
                                                foreach (var sprInst in layer.AssetsData.Sprites)
                                                    if (sprInst.Name == obj)
                                                        yield return new object[] { sprInst, layer, room };
                                            }
                                        }
                                    }
                                }

                                var sprInstances = GetSprInstances();
                                if (sprInstances.Any())
                                    outDict["Room sprite instances"] = checkOne ? sprInstances.ToEmptyArray() : sprInstances.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 2, 1),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleTextureGroupInfo)))
                                return null;

                            if (objSrc is not UndertaleString obj)
                                return null;

                            var textGroups = data.TextureGroupInfo.NotNullWhere(x => x.Name == obj);
                            if (textGroups.Any())
                                return new() { { "Texture groups", checkOne ? textGroups.ToEmptyArray() : textGroups.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 3, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleString obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleAnimationCurve)))
                            {
                                var animCurves = data.AnimationCurves.NotNullWhere(x => x.Name == obj);
                                if (animCurves.Any())
                                    outDict["Animation curves"] = checkOne ? animCurves.ToEmptyArray() : animCurves.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleAnimationCurve.Channel)))
                            {
                                IEnumerable<object[]> GetAnimCurveChannels()
                                {
                                    foreach (var curve in data.AnimationCurves.SkipNullItems())
                                    {
                                        foreach (var ch in curve.Channels)
                                            if (ch.Name == obj)
                                                yield return new object[] { ch, curve };
                                    }
                                }

                                var animCurveChannels = GetAnimCurveChannels();
                                if (animCurveChannels.Any())
                                    outDict["Animation curve channels"] = checkOne ? animCurveChannels.ToEmptyArray() : animCurveChannels.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom.SequenceInstance)))
                            {
                                IEnumerable<object[]> GetSeqInstances()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var layer in room.Layers)
                                        {
                                            if (layer.AssetsData is not null)
                                            {
                                                foreach (var seqInst in layer.AssetsData.Sequences)
                                                    if (seqInst.Sequence.Name == obj)
                                                        yield return new object[] { seqInst, layer, room };
                                            }
                                        }
                                    }
                                }

                                var seqInstances = GetSeqInstances();
                                if (seqInstances.Any())
                                    outDict["Room sequence instances"] = checkOne ? seqInstances.ToEmptyArray() : seqInstances.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleSequence)))
                            {
                                var sequences = data.Sequences.NotNullWhere(x => x.Name == obj
                                                                                 || x.FunctionIDs.Any(f => f.FunctionName == obj));
                                if (sequences.Any())
                                    outDict["Sequences"] = checkOne ? sequences.ToEmptyArray() : sequences.ToArray();
                            }

                            // TODO: make these "IEnumerable<object[]>"?
                            List<object[]> sequenceTracks = new();
                            List<object[]> seqStringKeyframes = new();
                            void ProcessTrack(UndertaleSequence seq, Track track, List<object> trackChain)
                            {
                                if (checkOne && (sequenceTracks.Count > 0 || seqStringKeyframes.Count > 0))
                                    return; // Stop recursion, we already found one reference

                                trackChain = new(trackChain);
                                trackChain.Insert(0, track);
                                if (types.Contains(typeof(Track)))
                                {
                                    if (track.Name == obj || track.ModelName == obj)
                                    {
                                        sequenceTracks.Add(trackChain.Append(seq).ToArray());
                                        if (checkOne)
                                            return; // No need for getting all of the references
                                    }
                                }

                                if (types.Contains(typeof(StringKeyframes)))
                                {
                                    if (track.Keyframes is StringKeyframes strKeyframes)
                                    {
                                        foreach (var keyframe in strKeyframes.List)
                                        {
                                            foreach (var strPair in keyframe.Channels)
                                            {
                                                if (strPair.Value.Value == obj)
                                                {
                                                    seqStringKeyframes.Add(new object[] { strPair.Channel }.Concat(trackChain).Append(seq).ToArray());
                                                    if (checkOne)
                                                        return; // No need for getting all of the references
                                                }
                                            }
                                        }
                                    }
                                }

                                foreach (var subTrack in track.Tracks)
                                {
                                    ProcessTrack(seq, subTrack, trackChain);
                                    if (checkOne && (sequenceTracks.Count > 0 || seqStringKeyframes.Count > 0))
                                        return;
                                }
                            };
                            foreach (var seq in data.Sequences.SkipNullItems())
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    List<object> trackChain = new();
                                    ProcessTrack(seq, track, trackChain);
                                    if (checkOne && (sequenceTracks.Count > 0 || seqStringKeyframes.Count > 0))
                                        break;
                                }
                            }
                            if (sequenceTracks.Count > 0)
                                outDict["Sequence tracks"] = checkOne ? sequenceTracks.ToEmptyArray() : sequenceTracks.ToArray();
                            if (seqStringKeyframes.Count > 0)
                                outDict["Sequence string keyframes"] = checkOne ? seqStringKeyframes.ToEmptyArray() : seqStringKeyframes.ToArray();

                            if (types.Contains(typeof(BroadcastMessage)))
                            {
                                IEnumerable<object[]> GetSeqBroadMessages()
                                {
                                    foreach (var seq in data.Sequences.SkipNullItems())
                                    {
                                        foreach (var keyframe in seq.BroadcastMessages)
                                        {
                                            foreach (var msgPair in keyframe.Channels)
                                                if (msgPair.Value.Messages.Contains(obj))
                                                    yield return new object[] { msgPair.Channel, keyframe, seq };
                                        }
                                    }
                                }

                                var seqBroadMessages = GetSeqBroadMessages();
                                if (seqBroadMessages.Any())
                                    outDict["Sequence broadcast messages"] = checkOne ? seqBroadMessages.ToEmptyArray() : seqBroadMessages.ToArray();
                            }
                            if (types.Contains(typeof(Moment)))
                            {
                                IEnumerable<object[]> GetSequenceMoments()
                                {
                                    foreach (var seq in data.Sequences.SkipNullItems())
                                    {
                                        foreach (var keyframe in seq.Moments)
                                        {
                                            foreach (var momentPair in keyframe.Channels)
                                                if (momentPair.Value.Events.Contains(obj))
                                                    yield return new object[] { momentPair.Channel, keyframe, seq };
                                        }
                                    }
                                }

                                var sequenceMoments = GetSequenceMoments();
                                if (sequenceMoments.Any())
                                    outDict["Sequence moments"] = checkOne ? sequenceMoments.ToEmptyArray() : sequenceMoments.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 3, 6),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleFilterEffect)))
                                return null;

                            if (objSrc is not UndertaleString obj)
                                return null;

                            var filterEffects = data.FilterEffects.NotNullWhere(x => x.Name == obj || x.Value == obj);
                            if (filterEffects.Any())
                                return new() { { "Filter effects", checkOne ? filterEffects.ToEmptyArray() : filterEffects.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2022, 1, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleRoom.EffectProperty)))
                                return null;

                            if (objSrc is not UndertaleString obj)
                                return null;

                            IEnumerable<object[]> GetEffectProps()
                            {
                                foreach (var room in data.Rooms)
                                {
                                    foreach (var layer in room.Layers)
                                    {
                                        foreach (var prop in layer.EffectProperties)
                                            if (prop.Name == obj || prop.Value == obj)
                                                yield return new object[] { prop, layer, room };
                                    }
                                }
                            }

                            var effectProps = GetEffectProps();
                            if (effectProps.Any())
                                return new() { { "Room effect properties", checkOne ? effectProps.ToEmptyArray() : effectProps.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2022, 2, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(TextKeyframes)))
                                return null;

                            if (objSrc is not UndertaleString obj)
                                return null;

                            // TODO: make this "IEnumerable<object[]>"?
                            List<object[]> textKeyframesList = new();
                            void ProcessTrack(UndertaleSequence seq, Track track, List<object> trackChain)
                            {
                                if (checkOne && textKeyframesList.Count > 0)
                                    return; // Stop recursion, we already found one reference

                                trackChain = new(trackChain);
                                trackChain.Insert(0, track);

                                if (track.Keyframes is TextKeyframes textKeyframes)
                                {
                                    foreach (var keyframe in textKeyframes.List)
                                    {
                                        foreach (var textPair in keyframe.Channels)
                                        {
                                            if (textPair.Value.Text == obj)
                                            {
                                                textKeyframesList.Add(new object[] { textPair.Channel }.Concat(trackChain).Append(seq).ToArray());
                                                if (checkOne)
                                                    return; // No need for getting all of the references
                                            }
                                        }
                                    }
                                }

                                foreach (var subTrack in track.Tracks)
                                {
                                    ProcessTrack(seq, subTrack, trackChain);
                                    if (checkOne && textKeyframesList.Count > 0)
                                        return;
                                }
                            };

                            foreach (var seq in data.Sequences.SkipNullItems())
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    List<object> trackChain = new();
                                    ProcessTrack(seq, track, trackChain);
                                    if (checkOne && textKeyframesList.Count > 0)
                                        break;
                                }
                            }
                            if (textKeyframesList.Count > 0)
                                return new() { { "Sequence text keyframes", checkOne ? textKeyframesList.ToEmptyArray() : textKeyframesList.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2023, 2, 0), // Not present if it's LTS 2022
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleString obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleParticleSystem)))
                            {
                                var partSystems = data.ParticleSystems.NotNullWhere(x => x.Name == obj);
                                if (partSystems.Any())
                                    outDict["Particle systems"] = checkOne ? partSystems.ToEmptyArray() : partSystems.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleParticleSystemEmitter)))
                            {
                                var partSysEmitters = data.ParticleSystemEmitters.NotNullWhere(x => x.Name == obj);
                                if (partSysEmitters.Any())
                                    outDict["Particle system emitters"] = checkOne ? partSysEmitters.ToEmptyArray() : partSysEmitters.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom.ParticleSystemInstance)))
                            {
                                IEnumerable<object[]> GetPartSysInstances()
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var layer in room.Layers)
                                        {
                                            if (layer.AssetsData is not null)
                                            {
                                                foreach (var partSysInst in layer.AssetsData.ParticleSystems)
                                                    if (partSysInst.Name == obj)
                                                        yield return new object[] { partSysInst, layer, room };
                                            }
                                        }
                                    }
                                }

                                var partSysInstances = GetPartSysInstances();
                                if (partSysInstances.Any())
                                    outDict["Room particle system instances"] = checkOne ? partSysInstances.ToEmptyArray() : partSysInstances.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleGameObject obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = obj.FindChildren(data);
                                if (gameObjects.Any())
                                    outDict["Game objects (children)"] = checkOne ? gameObjects.ToEmptyArray() : gameObjects.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom.GameObject)))
                            {
                                IEnumerable<object[]> GetObjInstances()
                                {
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
                                                            yield return new object[] { inst, layer, room };
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
                                                    yield return new object[] { inst, room };
                                        }
                                    }
                                }

                                var objInstances = GetObjInstances();
                                if (objInstances.Any())
                                    outDict["Room object instances"] = checkOne ? objInstances.ToEmptyArray() : objInstances.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2, 3, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(InstanceKeyframes)))
                                return null;

                            if (objSrc is not UndertaleGameObject obj)
                                return null;

                            // TODO: make this "IEnumerable<object[]>"?
                            List<object[]> instKeyframesList = new();
                            void ProcessTrack(UndertaleSequence seq, Track track, List<object> trackChain)
                            {
                                if (checkOne && instKeyframesList.Count > 0)
                                    return; // Stop recursion, we already found one reference

                                trackChain = new(trackChain);
                                trackChain.Insert(0, track);

                                if (track.Keyframes is InstanceKeyframes instKeyframes)
                                {
                                    foreach (var keyframe in instKeyframes.List)
                                    {
                                        foreach (var instPair in keyframe.Channels)
                                            if (instPair.Value.Resource.Resource == obj)
                                            {
                                                instKeyframesList.Add(new object[] { instPair.Channel }.Concat(trackChain).Append(seq).ToArray());
                                                if (checkOne)
                                                    return; // No need for getting all of the references
                                            }
                                    }
                                }

                                foreach (var subTrack in track.Tracks)
                                {
                                    ProcessTrack(seq, subTrack, trackChain);
                                    if (checkOne && instKeyframesList.Count > 0)
                                        return;
                                }
                            };

                            foreach (var seq in data.Sequences.SkipNullItems())
                            {
                                foreach (var track in seq.Tracks)
                                {
                                    List<object> trackChain = new();
                                    ProcessTrack(seq, track, trackChain);
                                    if (checkOne && instKeyframesList.Count > 0)
                                        break;
                                }
                            }
                            if (instKeyframesList.Count > 0)
                                return new() { { "Sequence object instance keyframes", checkOne ? instKeyframesList.ToEmptyArray() : instKeyframesList.ToArray() } };
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleCode obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = data.GameObjects.NotNullWhere(x => x.Events.Any(
                                                                                       e => e.Any(se => se.Actions.Any(
                                                                                         a => a.CodeId == obj))));
                                if (gameObjects.Any())
                                    outDict["Game objects"] = checkOne ? gameObjects.ToEmptyArray() : gameObjects.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom)))
                            {
                                var rooms = data.Rooms.NotNullWhere(x => x.CreationCodeId == obj);
                                if (rooms.Any())
                                    outDict["Rooms"] = checkOne ? rooms.ToEmptyArray() : rooms.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom.GameObject)))
                            {
                                IEnumerable<object[]> GetObjInstances()
                                {
                                    if (data.IsGameMaker2())
                                    {
                                        foreach (var room in data.Rooms)
                                        {
                                            foreach (var layer in room.Layers)
                                            {
                                                if (layer.InstancesData is not null)
                                                {
                                                    foreach (var inst in layer.InstancesData.Instances)
                                                        if (inst.CreationCode == obj)
                                                            yield return new object[] { inst, layer, room };
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var room in data.Rooms)
                                        {
                                            foreach (var inst in room.GameObjects)
                                                if (inst.CreationCode == obj)
                                                    yield return new object[] { inst, room };
                                        }
                                    }
                                }

                                var objInstances = GetObjInstances();
                                if (objInstances.Any())
                                    outDict["Room object instances"] = checkOne ? objInstances.ToEmptyArray() : objInstances.ToArray();
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
                                var scripts = data.Scripts.NotNullWhere(x => x.Code == obj);
                                if (scripts.Any())
                                    outDict["Scripts"] = checkOne ? scripts.ToEmptyArray() : scripts.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    },
                    new PredicateForVersion()
                    {
                        // Bytecode version 16
                        Version = new(16),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleRoom.GameObject)))
                                return null;

                            if (objSrc is not UndertaleCode obj)
                                return null;

                            IEnumerable<object[]> GetObjInstances()
                            {
                                if (data.IsGameMaker2())
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var layer in room.Layers)
                                        {
                                            if (layer.InstancesData is not null)
                                            {
                                                foreach (var inst in layer.InstancesData.Instances)
                                                    if (inst.PreCreateCode == obj)
                                                        yield return new object[] { inst, layer, room };
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var room in data.Rooms)
                                    {
                                        foreach (var inst in room.GameObjects)
                                            if (inst.PreCreateCode == obj)
                                                yield return new object[] { inst, room };
                                    }
                                }
                            }

                            var objInstances = GetObjInstances();
                            if (objInstances.Any())
                                return new() { { "Room object instances", checkOne ? objInstances.ToEmptyArray() : objInstances.ToArray() } };
                            else
                                return null;
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleSound)))
                                return null;

                            if (objSrc is not UndertaleEmbeddedAudio obj)
                                return null;

                            var sounds = data.Sounds.NotNullWhere(x => x.AudioFile == obj);
                            if (sounds.Any())
                                return new() { { "Sounds", checkOne ? sounds.ToEmptyArray() : sounds.ToArray() } };
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
                        Version = new(14), // Bytecode version 14
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleSound)))
                                return null;

                            if (objSrc is not UndertaleAudioGroup obj)
                                return null;

                            var sounds = data.Sounds.NotNullWhere(x => x.AudioGroup == obj);
                            if (sounds.Any())
                                return new() { { "Sounds", checkOne ? sounds.ToEmptyArray() : sounds.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleFunction),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleCode)))
                                return null;

                            if (objSrc is not UndertaleFunction obj)
                                return null;

                            IEnumerable<UndertaleCode> funcRefs;
                            if (funcReferences is not null)
                                funcRefs = funcReferences.Where(x => x.Value.Contains(obj))
                                                         .Select(x => x.Key);
                            else
                                funcRefs = data.Code.NotNullWhere(x => x.Instructions.Any(i => i.ValueFunction == obj));
                            if (funcRefs.Any())
                                return new() { { "Code", checkOne ? funcRefs.ToEmptyArray() : funcRefs.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleVariable),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (1, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleCode)))
                                return null;

                            if (objSrc is not UndertaleVariable obj)
                                return null;

                            // Exclude the internal "arguments" variable from unreferenced assets.
                            if (ignoreArgumentsVar && obj.InstanceType == UndertaleInstruction.InstanceType.Local
                                && obj.NameStringID == 0 && obj.Name?.Content == "arguments")
                                return emptyResultArr;

                            IEnumerable<UndertaleCode> variRefs;
                            if (variReferences is not null)
                                variRefs = variReferences.Where(x => x.Value.Contains(obj))
                                                         .Select(x => x.Key);
                            else
                                variRefs = data.Code.NotNullWhere(x => x.Instructions.Any(i => i.ValueVariable == obj));
                            if (variRefs.Any())
                                return new() { { "Code", checkOne ? variRefs.ToEmptyArray() : variRefs.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(ValueTuple<UndertaleBackground, UndertaleBackground.TileID>),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (2, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleRoom.Layer)))
                                return null;

                            if (objSrc is not ValueTuple<UndertaleBackground, UndertaleBackground.TileID> obj)
                                return null;

                            IEnumerable<object[]> GetTileLayers()
                            {
                                uint tileId = obj.Item2.ID;

                                foreach (var room in data.Rooms)
                                {
                                    foreach (var layer in room.Layers)
                                    {
                                        if (layer.TilesData is not null)
                                        {
                                            if (layer.TilesData.Background != obj.Item1)
                                                continue;

                                            // Flatten 2-dimensional array and extract the actual tile IDs
                                            var allTileIDs = layer.TilesData.TileData.SelectMany(x => x)
                                                                                     .Select(x => x & 0x0FFFFFFF);
                                            if (allTileIDs.Contains(tileId))
                                                yield return new object[] { layer, room };
                                                    
                                        }
                                    }
                                }
                            }
                            var tileLayers = GetTileLayers();

                            if (tileLayers.Any())
                                return new() { { "Room tile layers", checkOne ? tileLayers.ToEmptyArray() : tileLayers.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleParticleSystem),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (2023, 2, 0), // Not present if it's LTS 2022
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleRoom.ParticleSystemInstance)))
                                return null;

                            if (objSrc is not UndertaleParticleSystem obj)
                                return null;

                            IEnumerable<object[]> GetPartSysInstances()
                            {
                                foreach (var room in data.Rooms)
                                {
                                    foreach (var layer in room.Layers)
                                    {
                                        if (layer.AssetsData is not null)
                                        {
                                            foreach (var partSysInst in layer.AssetsData.ParticleSystems)
                                                if (partSysInst.ParticleSystem == obj)
                                                    yield return new object[] { partSysInst, layer, room };
                                        }
                                    }
                                }
                            }

                            var partSysInstances = GetPartSysInstances();
                            if (partSysInstances.Any())
                                return new() { { "Room particle system instance", checkOne ? partSysInstances.ToEmptyArray() : partSysInstances.ToArray() } };
                            else
                                return null;
                        }
                    }
                }
            },
            {
                typeof(UndertaleParticleSystemEmitter),
                new[]
                {
                    new PredicateForVersion()
                    {
                        Version = (2023, 2, 0), // Not present if it's LTS 2022
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleParticleSystemEmitter obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleParticleSystem)))
                            {
                                var partSystems = data.ParticleSystems.NotNullWhere(x => x.Emitters.Any(e => e.Resource == obj));
                                if (partSystems.Any())
                                    outDict["Particle systems"] = checkOne ? partSystems.ToEmptyArray() : partSystems.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleParticleSystemEmitter)))
                            {
                                var partSysEmitters = data.ParticleSystemEmitters.NotNullWhere(x => x.SpawnOnDeath == obj || x.SpawnOnUpdate == obj);
                                if (partSysEmitters.Any())
                                    outDict["Particle system emitters"] = checkOne ? partSysEmitters.ToEmptyArray() : partSysEmitters.ToArray();
                            }

                            if (outDict.Count == 0)
                                return null;
                            return outDict;
                        }
                    }
                }
            }
        };

        public static void SetCurrentGameData(UndertaleData data)
        {
            UndertaleResourceReferenceMethodsMap.data = data;
        }
        public static void ClearCurrentGameData()
        {
            UndertaleResourceReferenceMethodsMap.data = null;
        }

        // For singular use, sets and clears the current game data.
        public static Dictionary<string, List<object>> GetReferencesOfObject(object obj, UndertaleData data, HashSetTypesOverride types, bool checkOne = false)
        {
            SetCurrentGameData(data);
            var result = GetReferencesOfObject(obj, types, checkOne);
            ClearCurrentGameData();

            return result;
        }
        public static Dictionary<string, List<object>> GetReferencesOfObject(object obj, HashSetTypesOverride types, bool checkOne = false)
        {
            if (obj is null)
                return null;

            if (!typeMap.TryGetValue(obj.GetType(), out PredicateForVersion[] predicatesForVer))
                return null;

            bool onlyEmptyResult = true;

            GameVersion ver = new(data.GeneralInfo);
            Dictionary<string, List<object>> outDict = new();
            foreach (var predicateForVer in predicatesForVer)
            {
                bool isAtLeast = ver.CompareTo(predicateForVer.Version) >= 0;
                bool isAboveMost = ver.CompareTo(predicateForVer.BeforeVersion) >= 0;

                if (!isAtLeast || isAboveMost)
                    continue;

                var result = predicateForVer.Predicate(obj, types, checkOne);
                if (result is null)
                {
                    onlyEmptyResult = false;
                    continue;
                }
                if (onlyEmptyResult && result == emptyResultArr)
                    continue;

                onlyEmptyResult = false;

                foreach (var entry in result)
                    outDict.Add(entry.Key, new(entry.Value));
            }

            if (onlyEmptyResult)
                return emptyResultList;
            if (outDict.Count == 0)
                return null;

            // Sort the output dictionary by the asset type name, for consistent order
            outDict = outDict.OrderBy(x => x.Key)
                             .ToDictionary(x => x.Key, x => x.Value);

            return outDict;
        }

        public static async Task<Dictionary<string, List<object>>> GetUnreferencedObjects(UndertaleData data, Dictionary<Type, string> typesDict)
        {
            SetCurrentGameData(data);

            Dictionary<string, List<object>> outDict = new();

            Dictionary<object, int> assetIndexes = new();
            List<(IList, string)> assetLists = new();
            foreach (var typePair in typesDict)
            {
                if (data[typePair.Key] is not IList resList)
                    continue;

                assetLists.Add((resList, typePair.Value));
            }
            List<(UndertaleResource, string)> assets = new(assetLists.Sum(x => x.Item1.Count));
            foreach (var list in assetLists)
            {
                for (int i = 0; i < list.Item1.Count; i++)
                {
                    var asset = list.Item1[i];
                    assetIndexes[asset] = i;
                    assets.Add(((UndertaleResource)asset, list.Item2));
                }
            }

            // If it's not a YYC game
            if (data.Code is not null)
            {
                stringReferences = new();
                funcReferences = new();
                variReferences = new();
                foreach (var code in data.Code)
                {
                    var strings = new HashSet<UndertaleString>();
                    var functions = new HashSet<UndertaleFunction>();
                    var variables = new HashSet<UndertaleVariable>();
                    foreach (var inst in code.Instructions)
                    {
                        if (inst.ValueString?.Resource is UndertaleString str)
                            strings.Add(str);

                        if (inst.ValueVariable is UndertaleVariable variable)
                            variables.Add(variable);

                        if (inst.ValueFunction is UndertaleFunction function)
                            functions.Add(function);
                    }

                    if (strings.Count != 0)
                        stringReferences[code] = strings;
                    if (functions.Count != 0)
                        funcReferences[code] = functions;
                    if (variables.Count != 0)
                        variReferences[code] = variables;
                }
            }

            mainWindow.IsEnabled = false;
            try
            {
                mainWindow.InitializeProgressDialog("Searching in progress...", "Please wait...");
                mainWindow.SetProgressBar(null, "Assets", 0, assets.Count);
                mainWindow.StartProgressBarUpdater();

                ConcurrentBag<Dictionary<string, List<object>>> dicts = new();

                if (assets.Count > 0) // A Partitioner can't be created on an empty list.
                {
                    ignoreArgumentsVar = true;

                    GameVersion version = new(data.GeneralInfo);
                    bool isYYC = data.Code is null;
                    HashSetTypesOverride.MakeContainEverything(version, isYYC);

                    var assetsPart = Partitioner.Create(0, assets.Count);

                    await Task.Run(() =>
                    {
                        Parallel.ForEach(assetsPart, (range) =>
                        {
                            var resultDict = new Dictionary<string, List<object>>();

                            for (int i = range.Item1; i < range.Item2; i++)
                            {
                                var asset = assets[i];
                                var assetReferences = GetReferencesOfObject(asset.Item1, new HashSetTypesOverride(), true);
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

                    ignoreArgumentsVar = false;
                }

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

                // Sort each asset list by the asset ID (the index in the source asset list)
                foreach (var assetList in outDict.Values)
                {
                    assetList.Sort((left, right) => {
                        return assetIndexes[left].CompareTo(assetIndexes[right]);
                    });
                }

                // Also sort the output dictionary by the asset type name (so the asset group order is also consistent)
                outDict = outDict.OrderBy(x => x.Key)
                                 .ToDictionary(x => x.Key, x => x.Value);
            }
            finally
            {
                await mainWindow.StopProgressBarUpdater();
                mainWindow.HideProgressBar();

                mainWindow.IsEnabled = true;
                stringReferences = null;
                funcReferences = null;
                variReferences = null;
                
                ClearCurrentGameData();
                HashSetTypesOverride.Restore();
            }

            if (outDict.Count == 0)
                return null;
            return outDict;
        }

        private static T[] ToEmptyArray<T>(this IEnumerable<T> _) => Array.Empty<T>();
    }
}
