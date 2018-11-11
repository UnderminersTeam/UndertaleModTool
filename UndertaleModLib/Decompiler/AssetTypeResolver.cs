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
        public static Dictionary<string, AssetIDType[]> builtin_funcs = new Dictionary<string, AssetIDType[]>()
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

            { "background_get_name", new AssetIDType[] { AssetIDType.Background } },
            { "background_get_width", new AssetIDType[] { AssetIDType.Background } },
            { "background_get_height", new AssetIDType[] { AssetIDType.Background } },
            { "background_get_texture", new AssetIDType[] { AssetIDType.Background } },
            { "background_get_uvs", new AssetIDType[] { AssetIDType.Background } },
            { "background_exists", new AssetIDType[] { AssetIDType.Background } },
            { "background_add", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "background_replace", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "background_duplicate", new AssetIDType[] { AssetIDType.Background } },
            { "background_assign", new AssetIDType[] { AssetIDType.Background, AssetIDType.Background } },
            { "background_create_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color } },
            { "background_create_gradient", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
            { "background_create_from_surface", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "background_set_alpha_from_background", new AssetIDType[] { AssetIDType.Background, AssetIDType.Background } },
            { "background_save", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other } },
            { "background_delete", new AssetIDType[] { AssetIDType.Background } },
            { "background_flush", new AssetIDType[] { AssetIDType.Background } },
            { "background_flush_multi", new AssetIDType[] { AssetIDType.Background } }, // array
            { "background_prefetch", new AssetIDType[] { AssetIDType.Background } },
            { "background_prefetch_multi", new AssetIDType[] { AssetIDType.Background } }, // array

            // only a few relevant ones for tiles
            { "tile_add", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "tile_set_background", new AssetIDType[] { AssetIDType.Other, AssetIDType.Background } },
            { "tile_set_blend", new AssetIDType[] { AssetIDType.Other, AssetIDType.Color } },

            { "audio_exists", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_get_name", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_get_type", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_play_sound", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other } },
            { "audio_play_sound_at", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "audio_pause_sound", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_pause_all", new AssetIDType[] { } },
            { "audio_resume_sound", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_resume_all", new AssetIDType[] { } },
            { "audio_stop_sound", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_stop_all", new AssetIDType[] { } },
            { "audio_is_playing", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_is_paused", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_create_streaam", new AssetIDType[] { AssetIDType.Other } },
            { "audio_destroy_streaam", new AssetIDType[] { AssetIDType.Other } },

            { "audio_sound_set_track_position", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other } },
            { "audio_sound_get_track_position", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_sound_length", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_sound_pitch", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other } },
            { "audio_sound_get_pitch", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_falloff_set_model", new AssetIDType[] { AssetIDType.Other } },
            { "audio_sound_gain", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other } },
            { "audio_sound_get_gain", new AssetIDType[] { AssetIDType.Sound } },
            { "audio_master_gain", new AssetIDType[] { AssetIDType.Other } },
            // TODO: There is a bunch more advanced audio functions but I'm tired and Undertale/Deltarune don't need these afaik

            // Legacy sound functions
            { "sound_exists", new AssetIDType[] { AssetIDType.Sound } },
            { "sound_get_name", new AssetIDType[] { AssetIDType.Sound } },
            { "sound_play", new AssetIDType[] { AssetIDType.Sound } },
            { "sound_loop", new AssetIDType[] { AssetIDType.Sound } },
            { "sound_stop", new AssetIDType[] { AssetIDType.Sound } },
            { "sound_stop_all", new AssetIDType[] { } },
            { "sound_isplaying", new AssetIDType[] { AssetIDType.Sound } },
            { "sound_volume", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other } },
            { "sound_fade", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other } },
            { "sound_global_volume", new AssetIDType[] { AssetIDType.Other } },
            // Deprecated legacy functions (wait what)
            { "sound_add", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "sound_replace", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
            { "sound_delete", new AssetIDType[] { AssetIDType.Sound } },
        };

        public static Dictionary<string, AssetIDType> builtin_vars = new Dictionary<string, AssetIDType>()
        {
            // only the relevant ones because I'm sick of writing this
            { "background_index", AssetIDType.Background }, // array
            { "background_colour", AssetIDType.Color }, // array
        };

        internal static bool AnnotateTypesForFunctionCall(string function_name, AssetIDType[] arguments, Dictionary<string, AssetIDType[]> scriptArgs)
        {
            if (builtin_funcs.ContainsKey(function_name))
            {
                var func_types = builtin_funcs[function_name];
                if (arguments.Length > func_types.Length)
                    throw new Exception("Bad call to " + function_name + " with " + arguments.Length + " arguments (instead of " + func_types.Length + ")");
                for (int i = 0; i < arguments.Length; i++)
                    arguments[i] = func_types[i];
                return true;
            }
            if (function_name == "script_execute")
            {
                // This needs a special case
                if (arguments.Length < 1)
                    throw new Exception("Bad call to " + function_name + " with " + arguments.Length + " arguments (instead of at least 1)");
                arguments[0] = AssetIDType.Script;
                if (scriptArgs.ContainsKey(function_name) && scriptArgs[function_name] != null)
                {
                    for (int i = 0; i < arguments.Length && i < scriptArgs[function_name].Length; i++)
                        arguments[1 + i] = scriptArgs[function_name][i];
                }
                return true;
            }
            if (scriptArgs.ContainsKey(function_name) && scriptArgs[function_name] != null)
            {
                for (int i = 0; i < arguments.Length && i < scriptArgs[function_name].Length; i++)
                    arguments[i] = scriptArgs[function_name][i];
                return true;
            }
            return false;
        }

        internal static AssetIDType AnnotateTypeForVariable(string variable_name)
        {
            if (builtin_vars.ContainsKey(variable_name))
                return builtin_vars[variable_name];
            return AssetIDType.Other;
        }
    }
}
