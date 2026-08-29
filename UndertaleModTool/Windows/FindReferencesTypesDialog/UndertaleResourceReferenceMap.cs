using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    public readonly record struct GameVersion((uint Major, uint Minor, uint Release)? GameMakerVersion = null,
                                              byte? BytecodeVersion = null)
        : IComparable<GameVersion>
    {
        public GameVersion() : this(null, null)
        {
            throw new InvalidOperationException("At least one version (GameMaker or bytecode) must be specified.");
        }
        public GameVersion(byte bytecodeVersion) : this(null, bytecodeVersion)
        {
        }
        public GameVersion(UndertaleGeneralInfo gameInfo)
            : this((gameInfo.Major, gameInfo.Minor, gameInfo.Release), gameInfo.BytecodeVersion)
        {
            // The asset types that were introduced in GM 2023+ are not available in LTS 2022.
            // It seems obvious, but the general info says that it's a GM 2023+ for a LTS 2022 game.
            // For example, particle systems - they are missing in LTS 2022, even if the general info says it's GM 2023.6.
            // So we treat that version as GM 2022.9, as it should be
            if (gameInfo.Branch == UndertaleGeneralInfo.BranchType.LTS2022_0)
                GameMakerVersion = (2022, 9, 0);
        }

        public bool HasGMVersion => GameMakerVersion.HasValue;
        public bool HasBytecodeVersion => BytecodeVersion.HasValue;

        public static implicit operator GameVersion((uint, uint, uint) verTuple)
        {
            return new(verTuple);
        }

        public int CompareTo(GameVersion other)
        {
            bool compareGMVer = HasGMVersion && other.HasGMVersion;
            bool compareBytecodeVer = HasBytecodeVersion && other.HasBytecodeVersion;

            if (!compareGMVer && !compareBytecodeVer)
                throw new InvalidOperationException("No common version to compare (GameMaker or bytecode)");

            if (compareGMVer)
            {
                int gmCompare = GameMakerVersion.Value.CompareTo(other.GameMakerVersion.Value);
                if (gmCompare != 0 || !compareBytecodeVer)
                    return gmCompare;
            }

            return BytecodeVersion.Value.CompareTo(other.BytecodeVersion.Value);
        }
    }

    public class TypesForVersion
    {
        public GameVersion Version { get; set; }
        public GameVersion BeforeVersion { get; set; } = new((uint.MaxValue, uint.MaxValue, uint.MaxValue), byte.MaxValue);
        public (Type, string)[] Types { get; set; }
    }

    public static class UndertaleResourceReferenceMap
    {
        // Don't forget to update `referenceableTypesOrig` as well
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
                            (typeof(UndertaleGameObject), "Game objects")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.Tile), "Room tiles"),
                            (typeof(UndertaleRoom.SpriteInstance), "Room sprite instances"),
                            (typeof(UndertaleRoom.Layer.LayerBackgroundData), "Room background layers")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), "Texture groups")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2023, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleParticleSystemEmitter), "Particle system emitters")
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
                        BeforeVersion = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.Background), "Room backgrounds"),
                            (typeof(UndertaleRoom.Tile), "Room tiles")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.Layer), "Room tile layers")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), "Texture groups")
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
                            (typeof(UndertaleTexturePageItem), "Texture page items")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), "Texture groups")
                        }
                    },
                }
            },
            {
                typeof(UndertaleFont),
                new[]
                {
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), "Texture groups")
                        }
                    }
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
                            (typeof(UndertaleSprite), "Sprites"),
                            (typeof(UndertaleBackground), "Backgrounds"),
                            (typeof(UndertaleFont), "Fonts")
                        }
                    },
                    new TypesForVersion()
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleBackground), "Tile sets"),
                            (typeof(UndertaleEmbeddedImage), "Embedded images")
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
                            (typeof(UndertaleBackground), "Backgrounds"),
                            (typeof(UndertaleCode), "Code entries (name and contents)"),
                            (typeof(UndertaleVariable), "Variables"),
                            (typeof(UndertaleFunction), "Functions"),
                            (typeof(UndertaleSound), "Sounds"),
                            (typeof(UndertaleSprite), "Sprites"),
                            (typeof(UndertaleExtension), "Extensions"),
                            (typeof(UndertaleExtensionFile), "Extension files"),
                            (typeof(UndertaleExtensionOption), "Extension options"),
                            (typeof(UndertaleExtensionFunction), "Extension functions"),
                            (typeof(UndertaleFont), "Fonts"),
                            (typeof(UndertaleGameObject), "Game objects"),
                            (typeof(UndertaleGeneralInfo), "General info"),
                            (typeof(UndertaleOptions.Constant), "Game options constants"),
                            (typeof(UndertalePath), "Paths"),
                            (typeof(UndertaleRoom), "Rooms"),
                            (typeof(UndertaleScript), "Scripts"),
                            (typeof(UndertaleShader), "Shaders"),
                            (typeof(UndertaleTimeline), "Timelines")
                        }
                    },
                    new TypesForVersion
                    {
                        // Bytecode version 14
                        Version = new(14),
                        Types = new[]
                        {
                            (typeof(UndertaleAudioGroup), "Audio groups")
                        }
                    },
                    new TypesForVersion
                    {
                        // Bytecode version 15
                        Version = new(15),
                        BeforeVersion = (2024, 8, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleCodeLocals), "Code locals")
                        }
                    },
                    new TypesForVersion
                    {
                        // Bytecode version 16
                        Version = new(16),
                        Types = new[]
                        {
                            (typeof(UndertaleLanguage), "Languages"),
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleBackground), "Tile sets"),
                            (typeof(UndertaleEmbeddedImage), "Embedded images"),
                            (typeof(UndertaleRoom.Layer), "Room layers"),
                            (typeof(UndertaleRoom.SpriteInstance), "Room sprite instances")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 2, 1),
                        Types = new[]
                        {
                            (typeof(UndertaleTextureGroupInfo), "Texture groups")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 3, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleAnimationCurve), "Animation curves"),
                            (typeof(UndertaleAnimationCurve.Channel), "Animation curve channels"),
                            (typeof(UndertaleRoom.SequenceInstance), "Room sequence instances"),
                            (typeof(UndertaleSequence), "Sequences"),
                            (typeof(UndertaleSequence.Track), "Sequence tracks"),
                            (typeof(UndertaleSequence.BroadcastMessage), "Sequence broadcast messages"),
                            (typeof(UndertaleSequence.Moment), "Sequence moments"),
                            (typeof(UndertaleSequence.StringKeyframes), "Sequence string keyframes")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 3, 6),
                        Types = new[]
                        {
                            (typeof(UndertaleFilterEffect), "Filter effects")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2022, 1, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.EffectProperty), "Room effect properties")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2022, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleSequence.TextKeyframes), "Sequence track text keyframes")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2023, 2, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleParticleSystem), "Particle systems"),
                            (typeof(UndertaleParticleSystemEmitter), "Particle system emitters"),
                            (typeof(UndertaleRoom.ParticleSystemInstance), "Room particle system instances")
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
                            (typeof(UndertaleGameObject), "Game objects (children)"),
                            (typeof(UndertaleRoom.GameObject), "Room object instance")
                        }
                    },
                    new TypesForVersion
                    {
                        Version = (2, 3, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleSequence.InstanceKeyframes), "Sequence instance keyframes")
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
                            (typeof(UndertaleGameObject), "Game objects"),
                            (typeof(UndertaleRoom), "Rooms"),
                            (typeof(UndertaleRoom.GameObject), "Room object instances (creation code)"),
                            (typeof(UndertaleGlobalInit), "Global initialization and game end scripts"),
                            (typeof(UndertaleScript), "Scripts")
                        }
                    },
                    new TypesForVersion()
                    {
                        // Bytecode version 16
                        Version = new(16),
                        Types = new[]
                        {
                            (typeof(UndertaleRoom.GameObject), "Room object instances (creation or pre create code)")
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
                            (typeof(UndertaleSound), "Sounds")
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
                        // Bytecode version 14
                        Version = new(14),
                        Types = new[]
                        {
                            (typeof(UndertaleSound), "Sounds")
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
                            (typeof(UndertaleCode), "Code")
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
                            (typeof(UndertaleCode), "Code")
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
                            (typeof(UndertaleRoom.Layer), "Room tile layer")
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
                            (typeof(UndertaleRoom.ParticleSystemInstance), "Room particle system instances")
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
                            (typeof(UndertaleParticleSystem), "Particle systems"),
                            (typeof(UndertaleParticleSystemEmitter), "Particle system emitters")
                        }
                    }
                }
            }
        };

        // From `typeMap`
        private static readonly Dictionary<Type, (string, GameVersion)> referenceableTypesOrig = new()
        {
            { typeof(UndertaleSprite), ("Sprites", (1, 0, 0)) },
            { typeof(UndertaleBackground), ("Backgrounds", (1, 0, 0)) },
            { typeof(UndertaleEmbeddedTexture), ("Embedded textures", (1, 0, 0)) },
            { typeof(UndertaleFont), ("Fonts", (1, 0, 0)) },
            { typeof(UndertaleTexturePageItem), ("Texture page items", (1, 0, 0)) },
            { typeof(UndertaleString), ("Strings", (1, 0, 0)) },
            { typeof(UndertaleGameObject), ("Game objects", (1, 0, 0)) },
            { typeof(UndertaleCode), ("Code entries", (1, 0, 0)) },
            { typeof(UndertaleFunction), ("Functions", (1, 0, 0)) },
            { typeof(UndertaleVariable), ("Variables", (1, 0, 0)) },
            { typeof(UndertaleEmbeddedAudio), ("Embedded audio", (1, 0, 0)) },
            { typeof(UndertaleAudioGroup), ("Audio groups", new(14)) }, // Bytecode version 14
            { typeof(UndertaleParticleSystem), ("Particle systems", (2023, 2, 0)) },
            { typeof(UndertaleParticleSystemEmitter), ("Particle system emitters", (2023, 2, 0)) }
        };
        private static Dictionary<Type, string> referenceableTypes;
        private static (GameVersion Version, bool IsYYC) currVerState;
        
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

            GameVersion version = new(data.GeneralInfo);

            IEnumerable<(Type, string)> outTypes = Enumerable.Empty<(Type, string)>();
            foreach (var typeForVer in typesForVer)
            {
                bool isAtLeast = version.CompareTo(typeForVer.Version) >= 0;
                bool isAboveMost = version.CompareTo(typeForVer.BeforeVersion) >= 0;

                if (isAtLeast && !isAboveMost)
                    outTypes = typeForVer.Types.UnionBy(outTypes, x => x.Item1);
            }

            if (outTypes == Enumerable.Empty<(Type, string)>())
                return null;

            return outTypes.Where(x => x.Item2 is not null
                                       && !(data.Code is null && CodeTypes.Contains(x.Item1)))
                           .ToArray();
        }

        public static Dictionary<Type, string> GetReferenceableTypes(GameVersion version, bool isYYC)
        {
            if (version == currVerState.Version && currVerState != default
                && isYYC == currVerState.IsYYC)
                return referenceableTypes;

            // Filter out code-related types, because YYC game = no code in "data.win"
            IEnumerable<KeyValuePair<Type, (string, GameVersion)>> typesOrigSrc;
            if (isYYC)
                typesOrigSrc = referenceableTypesOrig.ExceptBy(CodeTypes, x => x.Key);
            else
                typesOrigSrc = referenceableTypesOrig;

            referenceableTypes = typesOrigSrc.Where(x => x.Value.Item2.CompareTo(version) <= 0)
                                             .ToDictionary(x => x.Key, x => x.Value.Item1);
            currVerState = (version, isYYC);

            if (referenceableTypes.Count == 0)
                return referenceableTypes;
            
            referenceableTypes[typeof(UndertaleBackground)] = version.CompareTo((2, 0, 0)) >= 0
                                                              ? "Tile sets" : "Backgrounds";

            return referenceableTypes;
        }

        public static bool IsTypeReferenceable(Type type)
        {
            if (type is null)
                return false;

            return typeMap.ContainsKey(type);
        }

        public static HashSet<Type> GetSupportedReferenceTypes(GameVersion version, bool isYYC)
        {
            HashSet<Type> supportedTypes = new();

            // Filter out code-related input types, because YYC game = no code in "data.win"
            IEnumerable<TypesForVersion[]> typeMapSrc;
            if (isYYC)
                typeMapSrc = typeMap.ExceptBy(CodeTypes, x => x.Key).Select(x => x.Value);
            else
                typeMapSrc = typeMap.Values;

            foreach (var versions in typeMapSrc)
            {
                foreach (var typesForVersion in versions)
                {
                    bool isAtLeast = version.CompareTo(typesForVersion.Version) >= 0;
                    bool isAboveMost = version.CompareTo(typesForVersion.BeforeVersion) >= 0;

                    if (!isAtLeast || isAboveMost)
                        continue;

                    foreach ((Type type, string displayName) in typesForVersion.Types)
                    {
                        if (isYYC && CodeTypes.Contains(type))
                            continue;

                        supportedTypes.Add(type);
                    }
                }
            }

            return supportedTypes;
        }
    }
}
