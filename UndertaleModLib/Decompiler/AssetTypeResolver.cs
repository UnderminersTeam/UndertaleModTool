using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Decompiler
{
    public enum AssetIDType
    {
        Other = 0,
        Color,
        Sprite,
        Background,
        Sound,
        Font,
        Path,
        Timeline,
        Room,
        GameObject, // or GameObjectInstance, these are interchangable
        Script
    };

    internal class AssetTypeResolver
    {
        public static Dictionary<string, AssetIDType[]> builtins = new Dictionary<string, AssetIDType[]>()
        {
            { "script_exists", new AssetIDType[] { AssetIDType.Script } },
            { "script_get_name", new AssetIDType[] { AssetIDType.Script } },
            // script_execute handled separately

            { "instance_change", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.GameObject } },
            { "instance_copy", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.GameObject } },
            { "instance_create", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
            { "instance_destroy", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
            { "instance_exists", new AssetIDType[] { AssetIDType.GameObject } },
            { "instance_find", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
            { "instance_furthest", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
            { "instance_nearest", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
            { "instance_number", new AssetIDType[] { AssetIDType.GameObject } },
            { "instance_place", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
            { "instance_position", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },

            { "sprite_get_name", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_number", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_width", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_height", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_xoffset", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_yoffset", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_bbox_bottom", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_bbox_left", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_bbox_right", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_bbox_top", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_get_tpe", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other } },
            { "sprite_get_texture", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other } },
            { "sprite_get_uvs", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other } },

            { "sprite_exists", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_add", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_replace", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_duplicate", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_assign", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Sprite } },
            { "sprite_merge", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Sprite } },
            { "sprite_create_from_surface", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_add_from_surface", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_collision_mask", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_set_offset", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_delete", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_set_alpha_from_sprite", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Sprite } },
            { "sprite_set_cache_size", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other } },
            { "sprite_set_cache_size_ext", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_save", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other } },
            { "sprite_save_strip", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other } },
            { "sprite_flush", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_flush_multi", new AssetIDType[] { AssetIDType.Sprite } }, // sprite ARRAY
            { "sprite_prefetch", new AssetIDType[] { AssetIDType.Sprite } },
            { "sprite_prefetch_multi", new AssetIDType[] { AssetIDType.Sprite } }, // sprite ARRAY

            // TODO: Continue writing this huuuuuuuuuuuge list
        };

        internal static bool AnnotateTypesForFunctionCall(string function_name, AssetIDType[] arguments)
        {
            if (builtins.ContainsKey(function_name))
            {
                var func_types = builtins[function_name];
                if (arguments.Length > func_types.Length)
                    throw new Exception("Bad call to " + function_name + " with " + arguments.Length + " arguments (instead of " + func_types.Length + ")");
                for (int i = 0; i < arguments.Length; i++)
                    arguments[i] = func_types[i];
                return true;
            }
            if (function_name == "script_execute")
            {
                // This needs a special case because it's a vararg
                if (arguments.Length < 1)
                    throw new Exception("Bad call to " + function_name + " with " + arguments.Length + " arguments (instead of at least 1)");
                arguments[0] = AssetIDType.Script;
                // TODO: Handle cross-script type propagation
                return true;
            }
            return false;
        }
    }
}
