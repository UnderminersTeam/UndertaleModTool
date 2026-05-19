using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModTool.Localization;

namespace UndertaleModTool.Windows
{
    public class TypesForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
        public (uint Major, uint Minor, uint Release) BeforeVersion { get; set; } = (uint.MaxValue, uint.MaxValue, uint.MaxValue);
        public (Type, string)[] Types { get; set; }
    }

    public static class UndertaleResourceReferenceMap
    {
        private static readonly Dictionary<Type, TypesForVersion[]> typeMap = new()
        {
            {
                typeof(UndertaleSprite),
                new[]
                {
                    new TypesForVersion
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleGameObject), LocalizationSource.GetString("RefType_GameObjects"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.Tile), LocalizationSource.GetString("RefType_RoomTiles")),
                            (typeof(UndertaleRoom.SpriteInstance), LocalizationSource.GetString("RefType_RoomSpriteInstances")),
                            (typeof(UndertaleRoom.Layer.LayerBackgroundData), LocalizationSource.GetString("RefType_RoomBackgroundLayers"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), LocalizationSource.GetString("RefType_TextureGroups"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2023, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleParticleSystemEmitter), LocalizationSource.GetString("RefType_ParticleSystemEmitters"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleBackground),
                new[]
                {
                    new TypesForVersion
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.Background), LocalizationSource.GetString("RefType_RoomBackgrounds")),
                            (typeof(UndertaleRoom.Tile), LocalizationSource.GetString("RefType_RoomTiles"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.Background), null),
                            (typeof(UndertaleRoom.Tile), null),
                            (typeof(UndertaleRoom.Layer), LocalizationSource.GetString("RefType_RoomTileLayers"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), LocalizationSource.GetString("RefType_TextureGroups"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleEmbeddedTexture),
                new[]
                {
                    new TypesForVersion
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleTexturePageItem), LocalizationSource.GetString("RefType_TexturePageItems"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), LocalizationSource.GetString("RefType_TextureGroups"))
                        }
                    },
                }
            },
            {
                typeof(UndertaleTexturePageItem),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleSprite), LocalizationSource.GetString("RefType_Sprites")),
                            (typeof(UndertaleBackground), LocalizationSource.GetString("RefType_Backgrounds")),
                            (typeof(UndertaleFont), LocalizationSource.GetString("RefType_Fonts"))
                        }
                    },
                    new TypesForVersion()
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleBackground), LocalizationSource.GetString("RefType_TileSets")),
                            (typeof(UndertaleEmbeddedImage), LocalizationSource.GetString("RefType_EmbeddedImages"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleString),
                new[]
                {
                    new TypesForVersion
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleBackground), LocalizationSource.GetString("RefType_Backgrounds")),
                            (typeof(UndertaleCode), LocalizationSource.GetString("RefType_CodeEntriesNameAndContents")),
                            (typeof(UndertaleVariable), LocalizationSource.GetString("RefType_Variables")),
                            (typeof(UndertaleFunction), LocalizationSource.GetString("RefType_Functions")),
                            (typeof(UndertaleSound), LocalizationSource.GetString("RefType_Sounds")),
                            (typeof(UndertaleAudioGroup), LocalizationSource.GetString("RefType_AudioGroups")),
                            (typeof(UndertaleSprite), LocalizationSource.GetString("RefType_Sprites")),
                            (typeof(UndertaleExtension), LocalizationSource.GetString("RefType_Extensions")),
                            (typeof(UndertaleExtensionFile), LocalizationSource.GetString("RefType_ExtensionFiles")),
                            (typeof(UndertaleExtensionOption), LocalizationSource.GetString("RefType_ExtensionOptions")),
                            (typeof(UndertaleExtensionFunction), LocalizationSource.GetString("RefType_ExtensionFunctions")),
                            (typeof(UndertaleFont), LocalizationSource.GetString("RefType_Fonts")),
                            (typeof(UndertaleGameObject), LocalizationSource.GetString("RefType_GameObjects")),
                            (typeof(UndertaleGeneralInfo), LocalizationSource.GetString("RefType_GeneralInfo")),
                            (typeof(UndertaleOptions.Constant), LocalizationSource.GetString("RefType_GameOptionsConstants")),
                            (typeof(UndertalePath), LocalizationSource.GetString("RefType_Paths")),
                            (typeof(UndertaleRoom), LocalizationSource.GetString("RefType_Rooms")),
                            (typeof(UndertaleScript), LocalizationSource.GetString("RefType_Scripts")),
                            (typeof(UndertaleShader), LocalizationSource.GetString("RefType_Shaders")),
                            (typeof(UndertaleTimeline), LocalizationSource.GetString("RefType_Timelines"))
                        }
                    },
                    new TypesForVersion
                    {
                        // Bytecode version 15
                        Version = (15, uint.MaxValue, uint.MaxValue),
                        BeforeVersion = (2024, 8, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleCodeLocals), LocalizationSource.GetString("RefType_CodeLocals"))
                        }
                    },
                    new TypesForVersion
                    {
                        // Bytecode version 16
                        Version = (16, uint.MaxValue, uint.MaxValue),
                        Types = new[]
                        {
                            (typeof(UndertaleLanguage), LocalizationSource.GetString("RefType_Languages")),
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleBackground), LocalizationSource.GetString("RefType_TileSets")),
                            (typeof(UndertaleEmbeddedImage), LocalizationSource.GetString("RefType_EmbeddedImages")),
                            (typeof(UndertaleRoom.Layer), LocalizationSource.GetString("RefType_RoomLayers")),
                            (typeof(UndertaleRoom.SpriteInstance), LocalizationSource.GetString("RefType_RoomSpriteInstances"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), LocalizationSource.GetString("RefType_TextureGroups"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 3, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleAnimationCurve), LocalizationSource.GetString("RefType_AnimationCurves")),
                            (typeof(UndertaleAnimationCurve.Channel), LocalizationSource.GetString("RefType_AnimationCurveChannels")),
                            (typeof(UndertaleRoom.SequenceInstance), LocalizationSource.GetString("RefType_RoomSequenceInstances")),
                            (typeof(UndertaleSequence), LocalizationSource.GetString("RefType_Sequences")),
                            (typeof(UndertaleSequence.Track), LocalizationSource.GetString("RefType_SequenceTracks")),
                            (typeof(UndertaleSequence.BroadcastMessage), LocalizationSource.GetString("RefType_SequenceBroadcastMessages")),
                            (typeof(UndertaleSequence.Moment), LocalizationSource.GetString("RefType_SequenceMoments")),
                            (typeof(UndertaleSequence.StringKeyframes), LocalizationSource.GetString("RefType_SequenceStringKeyframes"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 3, 6),
                        Types = new[]
                        {
                            (typeof(UndertaleFilterEffect), LocalizationSource.GetString("RefType_FilterEffects"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2022, 1, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.EffectProperty), LocalizationSource.GetString("RefType_RoomEffectProperties"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2022, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleSequence.TextKeyframes), LocalizationSource.GetString("RefType_SequenceTextKeyframes"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2023, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleParticleSystem), LocalizationSource.GetString("RefType_ParticleSystems")),
                            (typeof(UndertaleParticleSystemEmitter), LocalizationSource.GetString("RefType_ParticleSystemEmitters")),
                            (typeof(UndertaleRoom.ParticleSystemInstance), LocalizationSource.GetString("RefType_RoomParticleSystemInstances"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleGameObject),
                new[]
                {
                    new TypesForVersion
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.GameObject), LocalizationSource.GetString("RefType_RoomObjectInstance"))
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 3, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleSequence.InstanceKeyframes), LocalizationSource.GetString("RefType_SequenceInstanceKeyframes"))
                        }
                    },
                }
            },
            {
                typeof(UndertaleCode),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleGameObject), LocalizationSource.GetString("RefType_GameObjects")),
                            (typeof(UndertaleRoom), LocalizationSource.GetString("RefType_Rooms")),
                            (typeof(UndertaleGlobalInit), LocalizationSource.GetString("RefType_GlobalInitAndGameEndScripts")),
                            (typeof(UndertaleScript), LocalizationSource.GetString("RefType_Scripts"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleEmbeddedAudio),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleSound), LocalizationSource.GetString("RefType_Sounds"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleAudioGroup),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleSound), LocalizationSource.GetString("RefType_Sounds"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleFunction),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleCode), LocalizationSource.GetString("RefType_Code"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleVariable),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleCode), LocalizationSource.GetString("RefType_Code"))
                        }
                    }
                }
            },
            {
                typeof(ValueTuple<UndertaleBackground, UndertaleBackground.TileID>),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.Layer), LocalizationSource.GetString("RefType_RoomTileLayer"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleParticleSystem),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (2023, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.ParticleSystemInstance), LocalizationSource.GetString("RefType_RoomParticleSystemInstances"))
                        }
                    }
                }
            },
            {
                typeof(UndertaleParticleSystemEmitter),
                new[]
                {
                    new TypesForVersion()
                    {
                        Version = (2023, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleParticleSystem), LocalizationSource.GetString("RefType_ParticleSystems")),
                            (typeof(UndertaleParticleSystemEmitter), LocalizationSource.GetString("RefType_ParticleSystemEmitters"))
                        }
                    }
                }
            }
        };

        private static readonly Dictionary<Type, string> referenceableTypes = new()
        {
            { typeof(UndertaleSprite), LocalizationSource.GetString("RefType_Sprites") },
            { typeof(UndertaleBackground), LocalizationSource.GetString("RefType_Backgrounds") },
            { typeof(UndertaleEmbeddedTexture), LocalizationSource.GetString("RefType_EmbeddedTextures") },
            { typeof(UndertaleTexturePageItem), LocalizationSource.GetString("RefType_TexturePageItems") },
            { typeof(UndertaleString), LocalizationSource.GetString("RefType_Strings") },
            { typeof(UndertaleGameObject), LocalizationSource.GetString("RefType_GameObjects") },
            { typeof(UndertaleCode), LocalizationSource.GetString("RefType_CodeEntries") },
            { typeof(UndertaleFunction), LocalizationSource.GetString("RefType_Functions") },
            { typeof(UndertaleVariable), LocalizationSource.GetString("RefType_Variables") },
            { typeof(UndertaleEmbeddedAudio), LocalizationSource.GetString("RefType_EmbeddedAudio") },
            { typeof(UndertaleAudioGroup), LocalizationSource.GetString("RefType_AudioGroups") },
            { typeof(UndertaleParticleSystem), LocalizationSource.GetString("RefType_ParticleSystems") },
            { typeof(UndertaleParticleSystemEmitter), LocalizationSource.GetString("RefType_ParticleSystemEmitters") }
        };
        
        public static readonly HashSet<Type> CodeTypes = new()
        {
            typeof(UndertaleCode),
            typeof(UndertaleScript),
            typeof(UndertaleCodeLocals),
            typeof(UndertaleVariable),
            typeof(UndertaleFunction)
        };

        public static (Type, string)[] GetTypeMapForVersion(Type type, UndertaleData data)
        {
            if (!typeMap.TryGetValue(type, out TypesForVersion[] typesForVer))
                return null;

            var version = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
            byte bytecodeVersion = data.GeneralInfo.BytecodeVersion;

            IEnumerable<(Type, string)> outTypes = Enumerable.Empty<(Type, string)>();
            foreach (var typeForVer in typesForVer)
            {
                bool isAtLeast = false;
                if (typeForVer.Version.Minor == uint.MaxValue)
                    isAtLeast = typeForVer.Version.Major <= bytecodeVersion;
                else
                    isAtLeast = typeForVer.Version.CompareTo(version) <= 0;

                bool isAboveMost = false;
                if (typeForVer.BeforeVersion.Minor == uint.MaxValue)
                    isAboveMost = typeForVer.BeforeVersion.Major <= bytecodeVersion;
                else
                    isAboveMost = typeForVer.BeforeVersion.CompareTo(version) <= 0;

                if (isAtLeast && !isAboveMost)
                    outTypes = typeForVer.Types.UnionBy(outTypes, x => x.Item1);
            }

            if (outTypes == Enumerable.Empty<(Type, string)>())
                return null;

            return outTypes.Where(x => x.Item2 is not null
                                       && !(data.Code is null && CodeTypes.Contains(x.Item1)))
                           .ToArray();
        }

        public static Dictionary<Type, string> GetReferenceableTypes((uint, uint, uint) version)
        {
            referenceableTypes[typeof(UndertaleBackground)] = version.CompareTo((2, 0, 0)) >= 0
                                                              ? LocalizationSource.GetString("RefType_TileSets") : LocalizationSource.GetString("RefType_Backgrounds");

            return referenceableTypes;
        }

        public static bool IsTypeReferenceable(Type type)
        {
            if (type is null)
                return false;

            return typeMap.ContainsKey(type);
        }
    }
}
