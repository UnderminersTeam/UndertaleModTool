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
                typeof(UndertaleString),
                new[]
                {
                    new TypesForVersion
                    {
                        Version = (1, 0, 0),
                        Types = new[]
                        {
                            (typeof(UndertaleBackground), "Backgrounds"),
                            (typeof(UndertaleCode), "Code entries"),
                            (typeof(UndertaleSound), "Sounds"),
                            (typeof(UndertaleAudioGroup), "Audio groups"),
                            (typeof(UndertaleSprite), "Sprites"),
                            (typeof(UndertaleExtension), "Extensions"),
                            (typeof(UndertaleExtensionFile), "Extension files"),
                            (typeof(UndertaleExtensionOption), "Extension options"),
                            (typeof(UndertaleExtensionFunction), "Extension functions"),
                            (typeof(UndertaleFont), "Fonts"),
                            (typeof(UndertaleFunction), "Functions"),
                            (typeof(UndertaleGameObject), "Game objects"),
                            (typeof(UndertaleGeneralInfo), "General info"),
                            (typeof(UndertaleOptions.Constant), "Game options constants"),
                            (typeof(UndertaleLanguage), "Languages"),
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
                    }
                }
            }
        };


        public static (Type, string)[] GetTypeMapForVersion(Type type, (uint, uint, uint) version, byte bytecodeVersion)
        {
            if (!typeMap.TryGetValue(type, out TypesForVersion[] typesForVer))
                return null;

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

            return outTypes.Where(x => x.Item2 is not null).ToArray();
        }

        public static bool IsTypeReferenceable(Type type)
        {
            if (type is null)
                return false;

            return typeMap.ContainsKey(type);
        }
    }
}
