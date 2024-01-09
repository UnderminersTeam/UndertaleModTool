using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSequence;
using static UndertaleModLib.Models.UndertaleInstruction;

namespace UndertaleModTool.Windows
{
    public class HashSetTypesOverride : HashSet<Type>
    {
        private readonly bool containsEverything, isYYC;
        public HashSetTypesOverride(bool containsEverything = false, bool isYYC = false)
        {
            this.containsEverything = containsEverything;
            this.isYYC = isYYC;
        }
        public new bool Contains(Type item)
        {
            if (!containsEverything)
                return base.Contains(item);

            return !isYYC || !UndertaleResourceReferenceMap.CodeTypes.Contains(item);
        } 
    }

    public class PredicateForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
        public Func<object, HashSetTypesOverride, bool, Dictionary<string, object[]>> Predicate { get; set; }
    }

    public static class UndertaleResourceReferenceMethodsMap
    {
        private static UndertaleData data;
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static Dictionary<UndertaleCode, HashSet<UndertaleString>> stringCodeReferences;
        private static Dictionary<UndertaleCode, HashSet<UndertaleFunction>> funcCodeReferences;
        private static Dictionary<UndertaleCode, HashSet<UndertaleVariable>> variCodeReferences;

        private static Dictionary<string, short> fontFunctions, gameObjFunctions, spriteFunctions, bgFunctions;
        private static Dictionary<UndertaleCode, HashSet<UndertaleFont>> fontCodeReferences;
        private static Dictionary<UndertaleCode, HashSet<UndertaleGameObject>> gameObjCodeReferences;
        private static Dictionary<UndertaleCode, HashSet<UndertaleSprite>> spriteCodeReferences;
        private static Dictionary<UndertaleCode, HashSet<UndertaleBackground>> bgCodeReferences;

        #region Call instructions processing
        private static bool isGMS2, isGMS2_3, isGM2023_8;
        private static int dummyInt;
        private static bool ConsumeCallArgument(bool isLastArg, ref int i, UndertaleCode code, ref int val, bool dontParse = false)
        {
            if (isLastArg && !dontParse)
            {
                // If it's an asset argument and we don't need to consume
                // all instructions, then we only check one (GM 2023.8+) or two instructions.
                // Also, it's possible that it will be `conv` + `pushi` in GM 2023.8+, but
                // it would mean that the reference is not recognized, so
                // we should also skip that.
                var instr = code.Instructions[i];

                if (isGM2023_8 && instr.Kind == Opcode.Break
                    && instr.Value is short v && v == -11)
                {
                    int assetIndex = Decompiler.ExpressionAssetRef.DecodeResourceIndex(instr.IntArgument);
                    if (assetIndex < 0)
                        val = -1;
                    else
                        val = assetIndex;
                }
                else if (instr.Kind == Opcode.Conv && i - 1 >= 0)
                {
                    instr = code.Instructions[--i];
                    if (instr.Kind != Opcode.PushI || instr.Value is not short v1)
                        return false;

                    if (v1 < 0)
                        val = -1;
                    else
                        val = v1;
                }
                else
                    return false;

                return true;
            }

            short instrRemaining = 1;

            while (instrRemaining > 0)
            {
                if (i - 1 < 0)
                    return false;

                var instr = code.Instructions[i--];

                switch (instr.Kind)
                {
                    case Opcode.Conv:
                        instrRemaining++;

                        break;

                    case Opcode.PushI:
                    case Opcode.PushBltn or Opcode.PushGlb or Opcode.PushEnv or Opcode.PushLoc:
                        break;
                    case Opcode.Break when isGMS2_3:
                        if (instr.Value is short v)
                        {
                            if (v == -2 || v == -4) // `pushaf`, `pushac`
                                instrRemaining += 2;
                        }
                        break;

                    case Opcode.Push:
                        if (instr.Value is Reference<UndertaleVariable> varRef)
                        {
                            if (varRef.Type == VariableType.Array
                                || varRef.Type == VariableType.ArrayPushAF)
                            {
                                instrRemaining += 2;
                            }
                            else if (varRef.Type == VariableType.StackTop)
                            {
                                instrRemaining++;
                            }
                        }
                        break;

                    case Opcode.Add or Opcode.Sub or Opcode.Mul
                         or Opcode.Div or Opcode.And or Opcode.Rem
                         or Opcode.Shl or Opcode.Shr or Opcode.Xor or Opcode.Or:
                        instrRemaining += 2;

                        break;

                    case Opcode.Neg or Opcode.Not or Opcode.Ret or Opcode.Dup:
                        instrRemaining++;

                        break;

                    case Opcode.Call:
                        i++;
                        ConsumeCallInstructions(instr.ArgumentsCount - 1, ref i, code);
                        break;

                    case Opcode.CallV:
                        i++;
                        ConsumeCallInstructions(instr.Extra - 1, ref i, code);
                        break;
                }

                if (instrRemaining > 0)
                    instrRemaining--;
            }

            return true;
        }
        private static int ProcessCallInstructions(int argIndex, ref int i, UndertaleCode code)
        {
            int assetIndex = -1;
            int consumedCount = 0;
            i--;
            while (i >= 0 && consumedCount <= argIndex)
            {
                bool isLastArg = consumedCount == argIndex;
                if (!ConsumeCallArgument(isLastArg, ref i, code, ref assetIndex))
                    break;

                consumedCount++;
            }

            return assetIndex;
        }
        private static void ConsumeCallInstructions(int argIndex, ref int i, UndertaleCode code)
        {
            int consumedCount = 0;
            i--;
            while (i >= 0 && consumedCount <= argIndex)
            {
                bool isLastArg = consumedCount == argIndex;
                // `dummyInt` won't be changed because of `dontParse = true`
                if (!ConsumeCallArgument(isLastArg, ref i, code, ref dummyInt, true))
                    break;

                consumedCount++;
            }
        }
        #endregion

        private delegate int GetAssetIndexDeleg(int argIndex, int argCount, int i, UndertaleCode code, int assetListLen);
        private static readonly GetAssetIndexDeleg getAssetIndex = (int argIndex, int argCount, int i, UndertaleCode code, int assetListLen) =>
        {
            if (argCount == 1)
            {
                if (i - 2 < 0)
                    return -1;

                int assetInstrIndex = i - 1;
                var assetInstr = code.Instructions[assetInstrIndex];
                if (assetInstr.Kind != Opcode.Conv)
                    return -1;

                assetInstrIndex--;
                assetInstr = code.Instructions[assetInstrIndex];
                if (assetInstr.Kind != Opcode.PushI
                    || assetInstr.Type1 != DataType.Int16
                    || assetInstr.Value is not short val
                    || val < 0 || val > assetListLen - 1)
                    return -1;

                return val;
            }
            else
            {
                int assetIndex = ProcessCallInstructions(argIndex, ref i, code);
                if (assetIndex > assetListLen)
                    return -1;

                return assetIndex;
            }
        };
        private static readonly GetAssetIndexDeleg getAssetIndexGM2023_8 = (int argIndex, int argCount, int i, UndertaleCode code, int assetListLen) =>
        {
            if (argCount == 1)
            {
                int assetInstrIndex = i - 1;
                if (assetInstrIndex < 0)
                    return -1;

                var assetInstr = code.Instructions[assetInstrIndex];

                // If not `pushref`
                if (assetInstr.Kind != Opcode.Break
                    || assetInstr.Value is not short val
                    || val != -11)
                    return -1;

                int assetIndex = Decompiler.ExpressionAssetRef.DecodeResourceIndex(assetInstr.IntArgument);
                if (assetIndex < 0 || assetIndex > assetListLen - 1)
                    return -1;

                return assetIndex;
            }
            else
            {
                int assetIndex = ProcessCallInstructions(argIndex, ref i, code);
                if (assetIndex > assetListLen)
                    return -1;

                return assetIndex;
            }
        };
        private static GetAssetIndexDeleg getAssetIndexCurr;
        private static IEnumerable<UndertaleCode> GetCodeEntriesWithAsset<T>(Dictionary<string, short> assetFunctions, IList<T> assetList, T obj)
                                                  where T : class, UndertaleResource
        {
            foreach (var code in data.Code)
            {
                UndertaleCode assetReference = null;

                for (int i = 0; i < code.Instructions.Count; i++)
                {
                    var instr = code.Instructions[i];

                    string funcName = instr.Function?.Target?.Name?.Content;
                    if (funcName is null)
                        continue;
                    if (instr.ArgumentsCount == 0)
                        continue;
                    if (!assetFunctions.TryGetValue(funcName, out var argIndex))
                        continue;
                    if (argIndex > instr.ArgumentsCount - 1)
                        continue;

                    int assetIndex = getAssetIndexCurr(argIndex, instr.ArgumentsCount, i, code, assetList.Count);
                    if (assetIndex == -1)
                        continue;

                    if (assetList[assetIndex] == obj)
                    {
                        assetReference = code;
                        break;
                    }
                }

                if (assetReference is not null)
                    yield return assetReference;
            }
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
                            if (objSrc is not UndertaleSprite obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = data.GameObjects.Where(x => x.Sprite == obj);
                                if (gameObjects.Any())
                                    outDict["Game objects"] = checkOne ? gameObjects.ToEmptyArray() : gameObjects.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleCode)))
                            {
                                IEnumerable<UndertaleCode> spriteRefs;
                                if (spriteCodeReferences is not null)
                                {
                                    spriteRefs = spriteCodeReferences.Where(x => x.Value.Contains(obj))
                                                                     .Select(x => x.Key);
                                }
                                else
                                {
                                    spriteRefs = GetCodeEntriesWithAsset(spriteFunctions, data.Sprites, obj);
                                }
                                if (spriteRefs.Any())
                                    outDict["Code"] = checkOne ? spriteRefs.ToEmptyArray() : spriteRefs.ToArray();
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

                            var textGroups = data.TextureGroupInfo.Where(x => x.Sprites.Any(s => s.Resource == obj)
                                                                              || (x.SpineSprites?.Any(s => s.Resource == obj) == true));
                            if (textGroups.Any())
                                return new() { { "Texture groups", checkOne ? textGroups.ToEmptyArray() : textGroups.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2023, 2, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleParticleSystemEmitter)))
                                return null;

                            if (objSrc is not UndertaleSprite obj)
                                return null;

                            var partSysEmitters = data.ParticleSystemEmitters.Where(x => x.Sprite == obj);
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
                            if (types.Contains(typeof(UndertaleCode)))
                            {
                                IEnumerable<UndertaleCode> bgRefs;
                                if (bgCodeReferences is not null)
                                {
                                    bgRefs = bgCodeReferences.Where(x => x.Value.Contains(obj))
                                                             .Select(x => x.Key);
                                }
                                else
                                {
                                    bgRefs = GetCodeEntriesWithAsset(bgFunctions, data.Backgrounds, obj);
                                }
                                if (bgRefs.Any())
                                    outDict["Code"] = checkOne ? bgRefs.ToEmptyArray() : bgRefs.ToArray();
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

                            var textGroups = data.TextureGroupInfo.Where(x => x.Tilesets.Any(s => s.Resource == obj));
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
                            if (!types.Contains(typeof(UndertaleTexturePageItem)))
                                return null;

                            if (objSrc is not UndertaleEmbeddedTexture obj)
                                return null;

                            var pageItems = data.TexturePageItems.Where(x => x.TexturePage == obj);
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

                            var textGroups = data.TextureGroupInfo.Where(x => x.TexturePages.Any(s => s.Resource == obj));
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
                        Version = (1, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleCode)))
                                return null;

                            if (objSrc is not UndertaleFont obj)
                                return null;

                            IEnumerable<UndertaleCode> fontRefs;
                            if (fontCodeReferences is not null)
                            {
                                fontRefs = fontCodeReferences.Where(x => x.Value.Contains(obj))
                                                             .Select(x => x.Key);
                            }
                            else
                            {
                                fontRefs = GetCodeEntriesWithAsset(fontFunctions, data.Fonts, obj);
                            }
                            if (fontRefs.Any())
                                return new() { { "Code", checkOne ? fontRefs.ToEmptyArray() : fontRefs.ToArray() } };
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

                            if (objSrc is not UndertaleFont obj)
                                return null;

                            var textGroups = data.TextureGroupInfo.Where(x => x.Fonts.Any(s => s.Resource == obj));
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
                            if (objSrc is not UndertaleTexturePageItem obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleSprite)))
                            {
                                var sprites = data.Sprites.Where(x => x.Textures.Any(t => t.Texture == obj));
                                if (sprites.Any())
                                    outDict["Sprites"] = checkOne ? sprites.ToEmptyArray() : sprites.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleBackground)))
                            {
                                var backgrounds = data.Backgrounds.Where(x => x.Texture == obj);
                                if (backgrounds.Any())
                                    outDict[data.IsGameMaker2() ? "Tile sets" : "Backgrounds"] = checkOne ? backgrounds.ToEmptyArray() : backgrounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleFont)))
                            {
                                var fonts = data.Fonts.Where(x => x.Texture == obj);
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

                            var embImages = data.EmbeddedImages.Where(x => x.TextureEntry == obj);
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
                                var backgrounds = data.Backgrounds.Where(x => x.Name == obj);
                                if (backgrounds.Any())
                                    outDict[data.IsGameMaker2() ? "Tile sets" : "Backgrounds"] = checkOne ? backgrounds.ToEmptyArray() : backgrounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleCode)))
                            {
                                var codeEntries = data.Code.Where(x => x.Name == obj);
                                IEnumerable<UndertaleCode> stringRefs;
                                if (stringCodeReferences is not null)
                                    stringRefs = stringCodeReferences.Where(x => x.Value.Contains(obj))
                                                                     .Select(x => x.Key);
                                else
                                    stringRefs = data.Code.Where(x => x.Instructions.Any(
                                                                        i => i.Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> strPtr
                                                                             && strPtr.Resource == obj));

                                codeEntries = codeEntries.Concat(stringRefs);

                                if (codeEntries.Any())
                                    outDict["Code entries (name and contents)"] = checkOne ? codeEntries.ToEmptyArray() : codeEntries.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleFunction)))
                            {
                                var functions = data.Functions.Where(x => x.Name == obj);
                                if (functions.Any())
                                    outDict["Functions"] = checkOne ? functions.ToEmptyArray() : functions.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleVariable)))
                            {
                                var variables = data.Variables.Where(x => x.Name == obj);
                                if (variables.Any())
                                    outDict["Variables"] = checkOne ? variables.ToEmptyArray() : variables.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleSound)))
                            {
                                var sounds = data.Sounds.Where(x => x.Name == obj || x.Type == obj || x.File == obj);
                                if (sounds.Any())
                                    outDict["Sounds"] = checkOne ? sounds.ToEmptyArray() : sounds.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleAudioGroup)))
                            {
                                var audioGroups = data.AudioGroups.Where(x => x.Name == obj);
                                if (audioGroups.Any())
                                    outDict["Audio groups"] = checkOne ? audioGroups.ToEmptyArray() : audioGroups.ToArray();
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
                                    outDict["Sprites"] = checkOne ? sprites.ToEmptyArray() : sprites.ToArray();
                            }


                            if (types.Contains(typeof(UndertaleExtension)))
                            {
                                var extensions = data.Extensions.Where(x => x.Name == obj || x.ClassName == obj || x.FolderName == obj);
                                if (extensions.Any())
                                    outDict["Extensions"] = checkOne ? extensions.ToEmptyArray() : extensions.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleExtensionOption)))
                            {
                                IEnumerable<object[]> GetExtnOptions()
                                {
                                    foreach (var extn in data.Extensions)
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
                                    foreach (var extn in data.Extensions)
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
                                    foreach (var extn in data.Extensions)
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
                                var fonts = data.Fonts.Where(x => x.Name == obj || x.DisplayName == obj);
                                if (fonts.Any())
                                    outDict["Fonts"] = checkOne ? fonts.ToEmptyArray() : fonts.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleGameObject)))
                            {
                                var gameObjects = data.GameObjects.Where(x => x.Name == obj);
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

                            if (types.Contains(typeof(UndertaleLanguage)))
                            {
                                bool langsMatches = data.Language.EntryIDs.Contains(obj)
                                                    || data.Language.Languages.Any(x => x.Name == obj || x.Region == obj
                                                                                        || x.Entries.Contains(obj));
                                if (langsMatches)
                                    outDict["Languages"] = new object[] { new GeneralInfoEditor(data.GeneralInfo, data.Options, data.Language) };
                            }

                            if (types.Contains(typeof(UndertalePath)))
                            {
                                var paths = data.Paths.Where(x => x.Name == obj);
                                if (paths.Any())
                                    outDict["Paths"] = checkOne ? paths.ToEmptyArray() : paths.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom)))
                            {
                                var rooms = data.Rooms.Where(x => x.Name == obj || x.Caption == obj);
                                if (rooms.Any())
                                    outDict["Rooms"] = checkOne ? rooms.ToEmptyArray() : rooms.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleScript)))
                            {
                                var scripts = data.Scripts.Where(x => x.Name == obj);
                                if (scripts.Any())
                                    outDict["Scripts"] = checkOne ? scripts.ToEmptyArray() : scripts.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleShader)))
                            {
                                var shaders = data.Shaders.Where(x => x.Name == obj
                                                                      || x.GLSL_ES_Vertex == obj || x.GLSL_Vertex == obj || x.HLSL9_Vertex == obj
                                                                      || x.GLSL_ES_Fragment == obj || x.GLSL_Fragment == obj || x.HLSL9_Fragment == obj
                                                                      || x.VertexShaderAttributes.Any(a => a.Name == obj));
                                if (shaders.Any())
                                    outDict["Shaders"] = checkOne ? shaders.ToEmptyArray() : shaders.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleTimeline)))
                            {
                                var timelines = data.Timelines.Where(x => x.Name == obj);
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
                        Version = (15, uint.MaxValue, uint.MaxValue),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleCodeLocals)))
                                return null;

                            if (objSrc is not UndertaleString obj)
                                return null;

                            var codeLocals = data.CodeLocals.Where(x => x.Name == obj || x.Locals.Any(l => l.Name == obj));
                            if (codeLocals.Any())
                                return new() { { "Code locals", checkOne ? codeLocals.ToEmptyArray() : codeLocals.ToArray() } };
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
                                var embImages = data.EmbeddedImages.Where(x => x.Name == obj);
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

                            var textGroups = data.TextureGroupInfo.Where(x => x.Name == obj);
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
                                var animCurves = data.AnimationCurves.Where(x => x.Name == obj);
                                if (animCurves.Any())
                                    outDict["Animation curves"] = checkOne ? animCurves.ToEmptyArray() : animCurves.ToArray();
                            }
                            if (types.Contains(typeof(UndertaleAnimationCurve.Channel)))
                            {
                                IEnumerable<object[]> GetAnimCurveChannels()
                                {
                                    foreach (var curve in data.AnimationCurves)
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
                                var sequences = data.Sequences.Where(x => x.Name == obj
                                                                          || x.FunctionIDs.ContainsValue(obj));
                                if (sequences.Any())
                                    outDict["Sequences"] = checkOne ? sequences.ToEmptyArray() : sequences.ToArray();
                            }

                            // TODO: make these "IEnumerable<object[]>"
                            List<object[]> sequenceTracks = new();
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
                            }
                            if (sequenceTracks.Count > 0)
                                outDict["Sequence tracks"] = checkOne ? sequenceTracks.ToEmptyArray() : sequenceTracks.ToArray();
                            if (seqStringKeyframes.Count > 0)
                                outDict["Sequence string keyframes"] = checkOne ? seqStringKeyframes.ToEmptyArray() : seqStringKeyframes.ToArray();

                            if (types.Contains(typeof(BroadcastMessage)))
                            {
                                IEnumerable<object[]> GetSeqBroadMessages()
                                {
                                    foreach (var seq in data.Sequences)
                                    {
                                        foreach (var keyframe in seq.BroadcastMessages)
                                        {
                                            foreach (var msgPair in keyframe.Channels)
                                                if (msgPair.Value.Messages.Contains(obj))
                                                    yield return new object[] { msgPair.Key, keyframe, seq };
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
                                    foreach (var seq in data.Sequences)
                                    {
                                        foreach (var keyframe in seq.Moments)
                                        {
                                            foreach (var momentPair in keyframe.Channels)
                                                if (momentPair.Value.Event == obj)
                                                    yield return new object[] { momentPair.Key, keyframe, seq };
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

                            var filterEffects = data.FilterEffects.Where(x => x.Name == obj || x.Value == obj);
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

                            // TODO: make this "IEnumerable<object[]>"
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
                                return new() { { "Sequence text keyframes", checkOne ? textKeyframesList.ToEmptyArray() : textKeyframesList.ToArray() } };
                            else
                                return null;
                        }
                    },
                    new PredicateForVersion()
                    {
                        Version = (2023, 2, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleString obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleParticleSystem)))
                            {
                                var partSystems = data.ParticleSystems.Where(x => x.Name == obj);
                                if (partSystems.Any())
                                    outDict["Particle systems"] = checkOne ? partSystems.ToEmptyArray() : partSystems.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleParticleSystemEmitter)))
                            {
                                var partSysEmitters = data.ParticleSystemEmitters.Where(x => x.Name == obj);
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
                                var gameObjects = data.GameObjects.Where(x => x.ParentId == obj);
                                if (gameObjects.Any())
                                    outDict["Game objects (parent entry)"] = checkOne ? gameObjects.ToEmptyArray() : gameObjects.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleCode)))
                            {
                                IEnumerable<UndertaleCode> gameObjRefs;
                                if (gameObjCodeReferences is not null)
                                {
                                    gameObjRefs = gameObjCodeReferences.Where(x => x.Value.Contains(obj))
                                                                       .Select(x => x.Key);
                                }
                                else
                                {
                                    gameObjRefs = GetCodeEntriesWithAsset(gameObjFunctions, data.GameObjects, obj);
                                }
                                if (gameObjRefs.Any())
                                    outDict["Code"] = checkOne ? gameObjRefs.ToEmptyArray() : gameObjRefs.ToArray();
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
                                    outDict["Room object instance"] = checkOne ? objInstances.ToEmptyArray() : objInstances.ToArray();
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

                            // TODO: make this "IEnumerable<object[]>"
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
                                var gameObjects = data.GameObjects.Where(x => x.Events.Any(
                                                                            e => e.Any(se => se.Actions.Any(
                                                                                a => a.CodeId == obj))));
                                if (gameObjects.Any())
                                    outDict["Game objects"] = checkOne ? gameObjects.ToEmptyArray() : gameObjects.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleRoom)))
                            {
                                var rooms = data.Rooms.Where(x => x.CreationCodeId == obj);
                                if (rooms.Any())
                                    outDict["Rooms"] = checkOne ? rooms.ToEmptyArray() : rooms.ToArray();
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
                                    outDict["Scripts"] = checkOne ? scripts.ToEmptyArray() : scripts.ToArray();
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
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleSound)))
                                return null;

                            if (objSrc is not UndertaleEmbeddedAudio obj)
                                return null;

                            var sounds = data.Sounds.Where(x => x.AudioFile == obj);
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
                        Version = (1, 0, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (!types.Contains(typeof(UndertaleSound)))
                                return null;

                            if (objSrc is not UndertaleAudioGroup obj)
                                return null;

                            var sounds = data.Sounds.Where(x => x.AudioGroup == obj);
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
                            if (funcCodeReferences is not null)
                                funcRefs = funcCodeReferences.Where(x => x.Value.Contains(obj))
                                                             .Select(x => x.Key);
                            else
                                funcRefs = data.Code.Where(x => x.Instructions.Any(
                                                                  i => i.Function?.Target == obj
                                                                       || i.Value is Reference<UndertaleFunction> funcRef
                                                                          && funcRef.Target == obj));
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

                            IEnumerable<UndertaleCode> variRefs;
                            if (variCodeReferences is not null)
                                variRefs = variCodeReferences.Where(x => x.Value.Contains(obj))
                                                             .Select(x => x.Key);
                            else
                                variRefs = data.Code.Where(x => x.Instructions.Any(
                                                                  i => i.Destination?.Target == obj
                                                                       || i.Value is Reference<UndertaleVariable> varRef
                                                                          && varRef.Target == obj));
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
                        Version = (2023, 2, 0),
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
                        Version = (2023, 2, 0),
                        Predicate = (objSrc, types, checkOne) =>
                        {
                            if (objSrc is not UndertaleParticleSystemEmitter obj)
                                return null;

                            Dictionary<string, object[]> outDict = new();

                            if (types.Contains(typeof(UndertaleParticleSystem)))
                            {
                                var partSystems = data.ParticleSystems.Where(x => x.Emitters.Any(e => e.Resource == obj));
                                if (partSystems.Any())
                                    outDict["Particle systems"] = checkOne ? partSystems.ToEmptyArray() : partSystems.ToArray();
                            }

                            if (types.Contains(typeof(UndertaleParticleSystemEmitter)))
                            {
                                var partSysEmitters = data.ParticleSystemEmitters.Where(x => x.SpawnOnDeath == obj || x.SpawnOnUpdate == obj);
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


        private static void InitializeAssetFunctions()
        {
            if (fontFunctions is null)
            {
                var kvpList = AssetTypeResolver.builtin_funcs.Select(x =>
                {
                    return new KeyValuePair<string, short>(x.Key, (short)Array.IndexOf(x.Value, AssetIDType.Font));
                }).Where(x => x.Value != -1);
                fontFunctions = new(kvpList);
            }
            if (gameObjFunctions is null)
            {
                var kvpList = AssetTypeResolver.builtin_funcs.Select(x =>
                {
                    return new KeyValuePair<string, short>(x.Key, (short)Array.IndexOf(x.Value, AssetIDType.GameObject));
                }).Where(x => x.Value != -1);
                gameObjFunctions = new(kvpList);
            }
            if (spriteFunctions is null)
            {
                var kvpList = AssetTypeResolver.builtin_funcs.Select(x =>
                {
                    return new KeyValuePair<string, short>(x.Key, (short)Array.IndexOf(x.Value, AssetIDType.Sprite));
                }).Where(x => x.Value != -1);
                spriteFunctions = new(kvpList);
            }
            if (bgFunctions is null && !data.IsGameMaker2())
            {
                var kvpList = AssetTypeResolver.builtin_funcs.Select(x =>
                {
                    return new KeyValuePair<string, short>(x.Key, (short)Array.IndexOf(x.Value, AssetIDType.Background));
                }).Where(x => x.Value != -1);
                bgFunctions = new(kvpList);
            }
        }

        public static Dictionary<string, List<object>> GetReferencesOfObject(object obj, UndertaleData data, HashSetTypesOverride types, bool checkOne = false)
        {
            if (obj is null)
                return null;

            if (!typeMap.TryGetValue(obj.GetType(), out PredicateForVersion[] predicatesForVer))
                return null;

            UndertaleResourceReferenceMethodsMap.data = data;

            if (!checkOne)
            {
                InitializeAssetFunctions();

                isGMS2 = data.IsGameMaker2();
                isGMS2_3 = data.IsVersionAtLeast(2, 3);
                isGM2023_8 = data.IsVersionAtLeast(2023, 8);
                getAssetIndexCurr = isGM2023_8 ? getAssetIndexGM2023_8 : getAssetIndex;
            }

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
                    var result = predicateForVer.Predicate(obj, types, checkOne);
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

            InitializeAssetFunctions();

            isGMS2 = data.IsGameMaker2();
            isGMS2_3 = data.IsVersionAtLeast(2, 3);
            isGM2023_8 = data.IsVersionAtLeast(2023, 8);
            getAssetIndexCurr = isGM2023_8 ? getAssetIndexGM2023_8 : getAssetIndex;

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

            #region Instruction processing
            void ProcessInstruction(int i, UndertaleCode code, UndertaleInstruction instr,
                                    ref HashSet<UndertaleFont> fonts, ref HashSet<UndertaleGameObject> gameObjects,
                                    ref HashSet<UndertaleSprite> sprites, ref HashSet<UndertaleBackground> backgrounds)
            {
                string funcName = instr.Function.Target.Name?.Content;
                if (funcName is null)
                    return;
                if (instr.ArgumentsCount == 0)
                    return;

                ProcessInstructionForAsset(i, code, funcName, in fonts, data.Fonts, fontFunctions);
                ProcessInstructionForAsset(i, code, funcName, in gameObjects, data.GameObjects, gameObjFunctions);
                ProcessInstructionForAsset(i, code, funcName, in sprites, data.Sprites, spriteFunctions);
                if (!isGMS2)
                    ProcessInstructionForAsset(i, code, funcName, in backgrounds, data.Backgrounds, bgFunctions);
            }
            void ProcessInstructionForAsset<T>(int i, UndertaleCode code, string funcName, in HashSet<T> assetSet,
                                               IList<T> assetList, Dictionary<string, short> assetFunctions)
                 where T : class, UndertaleResource
            {
                if (!assetFunctions.TryGetValue(funcName, out var argIndex))
                    return;

                int argCount = code.Instructions[i].ArgumentsCount;
                if (argIndex > argCount - 1)
                    return;

                int assetIndex = getAssetIndexCurr(argIndex, argCount, i, code, assetList.Count);
                if (assetIndex == -1)
                    return;

                assetSet.Add(assetList[assetIndex]);
            }
            #endregion

            stringCodeReferences = new();
            funcCodeReferences = new();
            variCodeReferences = new();
            fontCodeReferences = new();
            gameObjCodeReferences = new();
            spriteCodeReferences = new();
            bgCodeReferences = new();
            HashSet<UndertaleBackground> backgrounds = null;
            foreach (var code in data.Code)
            {
                var strings = new HashSet<UndertaleString>();
                var functions = new HashSet<UndertaleFunction>();
                var variables = new HashSet<UndertaleVariable>();
                var fonts = new HashSet<UndertaleFont>();
                var gameObjects = new HashSet<UndertaleGameObject>();
                var sprites = new HashSet<UndertaleSprite>();
                if (!isGMS2)
                    backgrounds = new();

                for (int i = 0; i < code.Instructions.Count; i++)
                {
                    var instr = code.Instructions[i];

                    if (instr.Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> strPtr)
                        strings.Add(strPtr.Resource);

                    if (instr.Destination?.Target is not null)
                        variables.Add(instr.Destination.Target);
                    if (instr.Value is Reference<UndertaleVariable> varRef && varRef.Target is not null)
                        variables.Add(varRef.Target);

                    if (instr.Function?.Target is not null)
                    {
                        functions.Add(instr.Function.Target);

                        ProcessInstruction(i, code, instr, ref fonts, ref gameObjects, ref sprites, ref backgrounds);
                    }

                    if (instr.Value is Reference<UndertaleFunction> funcRef && funcRef.Target is not null)
                        functions.Add(funcRef.Target);
                }

                if (strings.Count != 0)
                    stringCodeReferences[code] = strings;
                if (functions.Count != 0)
                    funcCodeReferences[code] = functions;
                if (variables.Count != 0)
                    variCodeReferences[code] = variables;
                if (fonts.Count != 0)
                    fontCodeReferences[code] = fonts;
                if (gameObjects.Count != 0)
                    gameObjCodeReferences[code] = gameObjects;
                if (sprites.Count != 0)
                    spriteCodeReferences[code] = sprites;
                if (!isGMS2 && backgrounds.Count != 0)
                    bgCodeReferences[code] = backgrounds;
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
                            var assetReferences = GetReferencesOfObject(asset.Item1, data,
                                                                        new HashSetTypesOverride(true, data.Code is null), true);
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
                stringCodeReferences = null;
                funcCodeReferences = null;
                variCodeReferences = null;
                fontCodeReferences = null;
                gameObjCodeReferences = null;
                spriteCodeReferences = null;
                bgCodeReferences = null;

                throw;
            }
            mainWindow.IsEnabled = true;
            stringCodeReferences = null;
            funcCodeReferences = null;
            variCodeReferences = null;
            fontCodeReferences = null;
            gameObjCodeReferences = null;
            spriteCodeReferences = null;
            bgCodeReferences = null;

            if (outDict.Count == 0)
                return null;
            return outDict;
        }

        public static void ClearFunctionLists()
        {
            fontFunctions = null;
            gameObjFunctions = null;
            spriteFunctions = null;
            bgFunctions = null;
        }

        private static T[] ToEmptyArray<T>(this IEnumerable<T> _) => Array.Empty<T>();
    }
}
