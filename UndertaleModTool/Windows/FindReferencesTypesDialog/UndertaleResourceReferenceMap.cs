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
            }
        };


        public static (Type, string)[] GetTypeMapForVersion(Type type, (uint, uint, uint) version)
        {
            if (!typeMap.TryGetValue(type, out TypesForVersion[] typesForVer))
                return null;

            IEnumerable<(Type, string)> outTypes = Enumerable.Empty<(Type, string)>();
            foreach (var typeForVer in typesForVer)
            {
                if (typeForVer.Version.CompareTo(version) <= 0)
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
