using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    public record GameVersion(uint Major, uint Minor, uint Release) : IComparable<GameVersion>
    {
        public static implicit operator GameVersion((uint, uint, uint) verTuple)
        {
            return new(verTuple.Item1, verTuple.Item2, verTuple.Item3);
        }

        public int CompareTo(GameVersion other)
		{
			int cmp = Major.CompareTo(other.Major);
			if (cmp != 0)
                return cmp;

			cmp = Minor.CompareTo(other.Minor);
			if (cmp != 0)
                return cmp;

			return Release.CompareTo(other.Release);
		}
    }

    public class TypesForVersion
    {
        public GameVersion Version { get; set; }
        public GameVersion BeforeVersion { get; set; } = new(uint.MaxValue, uint.MaxValue, uint.MaxValue);
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
                            (typeof(UndertaleRoom.Background), null),
                            (typeof(UndertaleRoom.Tile), null),
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
                            (typeof(UndertaleAudioGroup), "Audio groups"),
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
                        // Bytecode version 15
                        Version = (15, uint.MaxValue, uint.MaxValue),
                        BeforeVersion = (2024, 8, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleCodeLocals), "Code locals")
                        }
                    },
                    new TypesForVersion
                    {
                        // Bytecode version 16
                        Version = (16, uint.MaxValue, uint.MaxValue),
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
                        Version = (16, uint.MaxValue, uint.MaxValue),
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
                        Version = (1, 0, 0),
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
            { typeof(UndertaleAudioGroup), ("Audio groups", (1, 0, 0)) },
            { typeof(UndertaleParticleSystem), ("Particle systems", (2023, 2, 0)) },
            { typeof(UndertaleParticleSystemEmitter), ("Particle system emitters", (2023, 2, 0)) }
        };
        private static Dictionary<Type, string> referenceableTypes;
        private static GameVersion currVersion;
        
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

            GameVersion version;
            if (data.GeneralInfo.Branch == UndertaleGeneralInfo.BranchType.LTS2022_0)
                version = (2022, 0, 0);
            else
                version = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
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

        public static Dictionary<Type, string> GetReferenceableTypes(GameVersion version)
        {
            if (version == currVersion && currVersion != default)
                return referenceableTypes;

            referenceableTypes = referenceableTypesOrig.Where(x => x.Value.Item2.CompareTo(version) <= 0)
                                                       .ToDictionary(x => x.Key, x => x.Value.Item1);
            currVersion = version;

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
    }
}
