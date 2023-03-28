using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    public class PredicateForVersion
    {
        public (uint Major, uint Minor, uint Release) Version { get; set; }
        public Func<UndertaleResource, (string, object[])[]> Predicate { get; set; }
    }

    public class ChildInstance
    {
        public UndertaleResource Parent { get; set; }
        public object Child { get; set; }

        public ChildInstance(UndertaleResource parent, object child)
        {
            Parent = parent;
            Child = child;
        }
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
                        Predicate = (obj) =>
                        {
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
                        Predicate = (obj) =>
                        {
                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            List<ChildInstance> tiles = new();
                            List<ChildInstance> sprInstances = new();
                            List<ChildInstance> bgLayers = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var layer in room.Layers)
                                {
                                    if (layer.AssetsData is not null)
                                    {
                                        foreach (var tile in layer.AssetsData.LegacyTiles)
                                            if (tile.SpriteDefinition == obj)
                                                tiles.Add(new(room, tile));

                                        foreach (var sprInst in layer.AssetsData.Sprites)
                                            if (sprInst.Sprite == obj)
                                                sprInstances.Add(new(room, sprInst));
                                    }

                                    if (layer.BackgroundData is not null
                                        && layer.BackgroundData.Sprite == obj)
                                        bgLayers.Add(new(room, layer));

                                }
                            }
                            if (tiles.Count > 0)
                                outList = outList.Append(("Room tiles", tiles.Cast<object>().ToArray()));
                            if (sprInstances.Count > 0)
                                outList = outList.Append(("Room sprite instances", sprInstances.Cast<object>().ToArray()));
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
                        Predicate = (obj) =>
                        {
                            var textGroups = data.TextureGroupInfo.Where(x => x.Sprites.Any(s => s.Resource == obj)
                                                                              || x.SpineSprites.Any(s => s.Resource == obj));
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
                        Predicate = (obj) =>
                        {
                            if (data.IsGameMaker2())
                                return null;

                            IEnumerable<(string, object[])> outList = Enumerable.Empty<(string, object[])>();

                            List<ChildInstance> backgrounds = new();
                            List<ChildInstance> tiles = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var bg in room.Backgrounds)
                                    if (bg.BackgroundDefinition == obj)
                                        backgrounds.Add(new(room, bg));

                                foreach (var tile in room.Tiles)
                                    if (tile.BackgroundDefinition == obj)
                                        tiles.Add(new(room, tile));
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
                        Predicate = (obj) =>
                        {
                            List<ChildInstance> tileLayers = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var layer in room.Layers)
                                {
                                    if (layer.TilesData is not null
                                        && layer.TilesData.Background == obj)
                                        tileLayers.Add(new(room, layer));

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
                        Predicate = (obj) =>
                        {
                            var textGroups = data.TextureGroupInfo.Where(x => x.Sprites.Any(s => s.Resource == obj)
                                                                              || x.SpineSprites.Any(s => s.Resource == obj));
                            if (textGroups.Any())
                                return new (string, object[])[] { ("Texture groups", textGroups.ToArray()) };
                            else
                                return null;
                        }
                    }
                }
            }
        };

        public static (string, object[])[] GetReferencesOfObject(UndertaleResource obj, UndertaleData data)
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
                if (predicateForVer.Version.CompareTo(ver) <= 0)
                {
                    var result = predicateForVer.Predicate(obj);
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
