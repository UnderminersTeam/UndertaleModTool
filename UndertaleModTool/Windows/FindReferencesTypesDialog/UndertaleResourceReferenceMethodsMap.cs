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

                            List<(UndertaleRoom, UndertaleRoom.Tile)> tiles = new();
                            List<(UndertaleRoom, UndertaleRoom.SpriteInstance)> sprInstances = new();
                            List<UndertaleRoom.Layer> bgLayers = new();
                            foreach (var room in data.Rooms)
                            {
                                foreach (var layer in room.Layers)
                                {
                                    if (layer.AssetsData is not null)
                                    {
                                        foreach (var tile in layer.AssetsData.LegacyTiles)
                                            if (tile.SpriteDefinition == obj)
                                                tiles.Add((room, tile));

                                        foreach (var sprInst in layer.AssetsData.Sprites)
                                            if (sprInst.Sprite == obj)
                                                sprInstances.Add((room, sprInst));
                                    }

                                    if (layer.BackgroundData is not null
                                        && layer.BackgroundData.Sprite == obj)
                                        bgLayers.Add(layer);

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
