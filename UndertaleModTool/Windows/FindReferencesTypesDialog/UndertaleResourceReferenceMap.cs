using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    public class TypesForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
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
                            (typeof(UndertaleGlobalInit), "Global initialization and game end scripts"),
                            (typeof(UndertaleScript), "Scripts")
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

        private static readonly Dictionary<Type, string> referenceableTypes = new()
        {
            { typeof(UndertaleSprite), "Sprites" },
            { typeof(UndertaleBackground), "Backgrounds" },
            { typeof(UndertaleEmbeddedTexture), "Embedded textures" },
            { typeof(UndertaleTexturePageItem), "Texture page items" },
            { typeof(UndertaleString), "Strings" },
            { typeof(UndertaleGameObject), "Game objects" },
            { typeof(UndertaleCode), "Code entries" },
            { typeof(UndertaleFunction), "Functions" },
            { typeof(UndertaleVariable), "Variables" },
            { typeof(UndertaleEmbeddedAudio), "Embedded audio" },
            { typeof(UndertaleAudioGroup), "Audio groups" },
            { typeof(UndertaleParticleSystem), "Particle systems" },
            { typeof(UndertaleParticleSystemEmitter), "Particle system emitters" }
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

                if (isAtLeast)
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
