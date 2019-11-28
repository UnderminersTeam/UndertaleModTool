using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public enum AssetIDType
    {
        Other = 0,
        Color,
        KeyboardKey,
        Enum_HAlign,
        Enum_VAlign,
        Enum_OSType,
        Enum_GamepadButton,
        Enum_PathEndAction,
        Enum_BufferKind,
        Enum_BufferType,
        Enum_BufferSeek,
        e__VW, // The constant used in __view_get and __view_set compatibility scripts
        e__BG, // The constant used in __background_get and __background_set compatibility scripts
        Boolean,

        Sprite,
        Background,
        Sound,
        Font,
        Path,
        Timeline,
        Room,
        GameObject, // or GameObjectInstance or InstanceType, these are all interchangable
        Script,
        Shader,

        Layer // GMS2
    };

    public enum HAlign : int
    {
        fa_left = 0,
        fa_center = 1,
        fa_right = 2
    };

    public enum VAlign : int
    {
        fa_top = 0,
        fa_middle = 1,
        fa_bottom = 2
    }

    public enum OSType : int
    {
        os_windows = 0,
        os_uwp = 18,
        os_linux = 6,
        os_macosx = 1,
        os_ios = 3,
        os_android = 4,
        os_ps3 = 16,
        os_ps4 = 14,
        os_psvita = 12,
        os_xboxone = 15,
        os_unknown = -1,
        os_3ds = 11,
        os_bb10 = 13,
        os_psp = 2,
        os_symbian = 5,
        os_tizen = 8,
        os_wiiu = 10,
        os_win8native = 9,
        os_xbox360 = 17,
        os_switch_beta = 20, // this one was used while switch support was in beta and changed later?
        os_switch = 21,
    }

    public enum GamepadButton : int
    {
        gp_face1 = 32769,
        gp_face2 = 32770,
        gp_face3 = 32771,
        gp_face4 = 32772,
        gp_shoulderl = 32773,
        gp_shoulderlb = 32775,
        gp_shoulderr = 32774,
        gp_shoulderrb = 32776,
        gp_select = 32777,
        gp_start = 32778,
        gp_stickl = 32779,
        gp_stickr = 32780,
        gp_padu = 32781,
        gp_padd = 32782,
        gp_padl = 32783,
        gp_padr = 32784,
        gp_axislh = 32785,
        gp_axislv = 32786,
        gp_axisrh = 32787,
        gp_axisrv = 32788,
    }

    public enum PathEndAction : int
    {
        path_action_stop = 0,
        path_action_restart = 1,
        path_action_continue = 2,
        path_action_reverse = 3
    }

    public enum BufferKind : int
    {
        buffer_fixed = 0,
        buffer_grow = 1,
        buffer_wrap = 2,
        buffer_fast = 3,
        buffer_vbuffer = 4,
        buffer_network = 5
    }

    public enum BufferType : int
    {
        buffer_u8 = 1,
        buffer_s8 = 2,
        buffer_u16 = 3,
        buffer_s16 = 4,
        buffer_u32 = 5,
        buffer_s32 = 6,
        buffer_f16 = 7,
        buffer_f32 = 8,
        buffer_f64 = 9,
        buffer_bool = 10,
        buffer_string = 11,
        buffer_u64 = 12,
        buffer_text = 13
    }

    public enum BufferSeek : int
    {
        buffer_seek_start = 0,
        buffer_seek_relative = 1,
        buffer_seek_end = 2
    }

    public enum e__VW : int
    {
        XView = 0,
        YView = 1,
        WView = 2,
        HView = 3,
        Angle = 4,
        HBorder = 5,
        VBorder = 6,
        HSpeed = 7,
        VSpeed = 8,
        Object = 9,
        Visible = 10,
        XPort = 11,
        YPort = 12,
        WPort = 13,
        HPort = 14,
        Camera = 15,
        SurfaceID = 16,
    };
    public enum e__BG : int
    {
        Visible = 0,
        Foreground = 1,
        Index = 2,
        X = 3,
        Y = 4,
        Width = 5,
        Height = 6,
        HTiled = 7,
        VTiled = 8,
        XScale = 9,
        YScale = 10,
        HSpeed = 11,
        VSpeed = 12,
        Blend = 13,
        Alpha = 14
    };

    public enum Boolean : int
    {
        @false = 0,
        @true = 1
    }

    public class AssetTypeResolver
    {
        public static Dictionary<string, AssetIDType[]> builtin_funcs;

        public static Dictionary<string, AssetIDType> builtin_vars;

        internal static bool AnnotateTypesForFunctionCall(string function_name, AssetIDType[] arguments, Dictionary<string, AssetIDType[]> scriptArgs)
        {
            // Scripts overload builtins because in GMS2 some functions are just backwards-compatibility scripts
            if (scriptArgs.ContainsKey(function_name) && scriptArgs[function_name] != null)
            {
                for (int i = 0; i < arguments.Length && i < scriptArgs[function_name].Length; i++)
                    arguments[i] = scriptArgs[function_name][i];
                return true;
            }

            function_name = function_name.Replace("color", "colour"); // Just GameMaker things... both are valid :o

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
            return false;
        }

        internal static AssetIDType AnnotateTypeForVariable(string variable_name)
        {
            if (builtin_vars.ContainsKey(variable_name))
                return builtin_vars[variable_name];
            return AssetIDType.Other;
        }

        public static int? FindConstValue(string const_name)
        {
            if (const_name.Length >= 1 && Char.IsDigit(const_name[0]))
                return null; // that is not a constant
            if (const_name.Length >= 1 && const_name[0] == '-')
                return null; // that is not a constant either

            // By avoiding Enum.TryParse, we avoid exception spam in the console, and there isn't any speed loss.
            if (Enum.IsDefined(typeof(OSType), const_name))
                return (int)Enum.Parse(typeof(OSType), const_name);
            if (Enum.IsDefined(typeof(GamepadButton), const_name))
                return (int)Enum.Parse(typeof(GamepadButton), const_name);
            if (Enum.IsDefined(typeof(HAlign), const_name))
                return (int)Enum.Parse(typeof(HAlign), const_name);
            if (Enum.IsDefined(typeof(VAlign), const_name))
                return (int)Enum.Parse(typeof(VAlign), const_name);
            if (Enum.IsDefined(typeof(e__VW), const_name))
                return (int)Enum.Parse(typeof(e__VW), const_name);
            if (Enum.IsDefined(typeof(e__BG), const_name))
                return (int)Enum.Parse(typeof(e__BG), const_name);
            if (Enum.IsDefined(typeof(EventSubtypeKey), const_name))
                return Convert.ToInt32((uint)Enum.Parse(typeof(EventSubtypeKey), const_name));

            return null;
        }

        // Properly initializes per-project/game
        public static void InitializeTypes(UndertaleData data)
        {
            builtin_funcs = new Dictionary<string, AssetIDType[]>()
            {
                { "action_create_object", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "instance_activate_object", new AssetIDType[] { AssetIDType.GameObject } },
                { "script_exists", new AssetIDType[] { AssetIDType.Script } },
                { "script_get_name", new AssetIDType[] { AssetIDType.Script } },
                // script_execute handled separately

                { "instance_change", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.GameObject } },
                { "instance_copy", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.GameObject } },
                { "instance_create", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_destroy", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Boolean } },
                { "instance_exists", new AssetIDType[] { AssetIDType.GameObject } },
                { "instance_find", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "instance_furthest", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_nearest", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_number", new AssetIDType[] { AssetIDType.GameObject } },
                { "instance_place", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_position", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_deactivate_all", new AssetIDType[] { AssetIDType.Boolean } },

                { "instance_activate_layer", new AssetIDType[] { AssetIDType.Layer } }, // GMS2
                { "instance_deactivate_layer", new AssetIDType[] { AssetIDType.Layer } }, // GMS2
                { "instance_create_depth", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } }, // GMS2
                { "instance_create_layer", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } }, // GMS2

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
                { "sprite_add", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_replace", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_duplicate", new AssetIDType[] { AssetIDType.Sprite } },
                { "sprite_assign", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Sprite } },
                { "sprite_merge", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Sprite } },
                { "sprite_create_from_surface", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_add_from_surface", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean } },
                { "sprite_collision_mask", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
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
                { "audio_play_sound", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Boolean } },
                { "audio_play_sound_at", new AssetIDType[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other } },
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
                { "audio_play_sound_on", new AssetIDType[] { AssetIDType.Other, AssetIDType.Sound, AssetIDType.Boolean, AssetIDType.Other } },
                { "audio_play_in_sync_group", new AssetIDType[] { AssetIDType.Other, AssetIDType.Sound } },
                // TODO? I don't know if the ones with only asset type Other are worth adding here

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

                { "font_get_name", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_fontname", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_first", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_last", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_italic", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_bold", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_size", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_texture", new AssetIDType[] { AssetIDType.Font } },
                { "font_get_uvs", new AssetIDType[] { AssetIDType.Font } },

                { "font_set_cache_size", new AssetIDType[] { AssetIDType.Font, AssetIDType.Other } },
                { "font_exists", new AssetIDType[] { AssetIDType.Font } },
                { "font_add", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_add_sprite", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_add_sprite_ext", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_replace", new AssetIDType[] { AssetIDType.Font, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_replace_sprite", new AssetIDType[] { AssetIDType.Font, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_replace_sprite_ext", new AssetIDType[] { AssetIDType.Font, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_delete", new AssetIDType[] { AssetIDType.Font } },

                { "path_start", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Enum_PathEndAction, AssetIDType.Other } },
                { "path_end", new AssetIDType[] { } },

                { "path_exists", new AssetIDType[] { AssetIDType.Path } },
                { "path_get_closed", new AssetIDType[] { AssetIDType.Path } },
                { "path_get_kind", new AssetIDType[] { AssetIDType.Path } },
                { "path_get_length", new AssetIDType[] { AssetIDType.Path } },
                { "path_get_name", new AssetIDType[] { AssetIDType.Path } },
                { "path_get_number", new AssetIDType[] { AssetIDType.Path } },
                { "path_get_point_speed", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_point_x", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_point_y", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_precision", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_speed", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_x", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_y", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other} },

                { "path_add", new AssetIDType[] { } },
                { "path_add_point", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "path_change_point", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "path_insert_point", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "path_delete_point", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_clear_points", new AssetIDType[] { AssetIDType.Path } },
                { "path_append", new AssetIDType[] { AssetIDType.Path, AssetIDType.Path } },
                { "path_assign", new AssetIDType[] { AssetIDType.Path, AssetIDType.Path } },
                { "path_delete", new AssetIDType[] { AssetIDType.Path } },
                { "path_duplicate", new AssetIDType[] { AssetIDType.Path } },
                { "path_flip", new AssetIDType[] { AssetIDType.Path } },
                { "path_mirror", new AssetIDType[] { AssetIDType.Path } },
                { "path_reverse", new AssetIDType[] { AssetIDType.Path } },
                { "path_rotate", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_scale", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other } },
                { "path_set_closed", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_set_kind", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_set_precision", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_shift", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other } },

                { "timeline_exists", new AssetIDType[] { AssetIDType.Timeline } },
                { "timeline_get_name", new AssetIDType[] { AssetIDType.Timeline } },
                { "timeline_delete", new AssetIDType[] { AssetIDType.Timeline } },
                { "timeline_moment_add_script", new AssetIDType[] { AssetIDType.Timeline, AssetIDType.Other, AssetIDType.Other } },
                { "timeline_moment_clear", new AssetIDType[] { AssetIDType.Timeline, AssetIDType.Other } },
                { "timeline_clear", new AssetIDType[] { AssetIDType.Timeline } },
                { "timeline_size", new AssetIDType[] { AssetIDType.Timeline } },
                { "timeline_max_moment", new AssetIDType[] { AssetIDType.Timeline } },

                { "room_exists", new AssetIDType[] { AssetIDType.Room } },
                { "room_next", new AssetIDType[] { AssetIDType.Room } },
                { "room_previous", new AssetIDType[] { AssetIDType.Room } },
                { "room_get_name", new AssetIDType[] { AssetIDType.Room } },

                { "room_goto", new AssetIDType[] { AssetIDType.Room } },
                { "room_goto_next", new AssetIDType[] { } },
                { "room_goto_previous", new AssetIDType[] { } },
                { "room_restart", new AssetIDType[] { } },

                { "room_add", new AssetIDType[] { } },
                { "room_duplicate", new AssetIDType[] { AssetIDType.Room } },
                { "room_assign", new AssetIDType[] { AssetIDType.Room, AssetIDType.Room } },
                { "room_instance_add", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "room_instance_clear", new AssetIDType[] { AssetIDType.Room } },
                { "room_tile_add", new AssetIDType[] { AssetIDType.Room, AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "room_tile_add_ext", new AssetIDType[] { AssetIDType.Room, AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "room_tile_clear", new AssetIDType[] { AssetIDType.Room } },
                { "room_set_background", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "room_set_background_colour", new AssetIDType[] { AssetIDType.Room, AssetIDType.Color, AssetIDType.Other } },
                { "room_set_height", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other } },
                { "room_set_width", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other } },
                { "room_set_persistent", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other } },
                { "room_set_view_enabled", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other } },

                { "room_set_viewport", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } }, // GMS2 only
                { "room_get_viewport", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other } }, // GMS2 only
                { "room_get_camera", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other } }, // GMS2 only
                { "room_set_camera", new AssetIDType[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Other } }, // GMS2 only

                { "object_exists", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_depth", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_mask", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_name", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_parent", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_persistent", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_solid", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_sprite", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_visible", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_get_physics", new AssetIDType[] { AssetIDType.GameObject } },
                { "object_is_ancestor", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.GameObject } },
                { "object_set_depth", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_mask", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_persistent", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_solid", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_sprite", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_visible", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other } },

                // only relevant event func
                { "event_perform_object", new AssetIDType[] { AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },

                { "merge_colour", new AssetIDType[] { AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },

                // only relevant functions listed
                { "draw_clear", new AssetIDType[] { AssetIDType.Color } },
                { "draw_clear_alpha", new AssetIDType[] { AssetIDType.Color, AssetIDType.Other } },
                { "draw_set_colour", new AssetIDType[] { AssetIDType.Color } },

                { "draw_circle_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_ellipse_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_line_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color } },
                { "draw_line_width_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color } },
                { "draw_point_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color } },
                { "draw_rectangle_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_roundrect_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_roundrect_colour_ext", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_healthbar", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },

                { "draw_sprite", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_ext", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_general", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_part", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_part_ext", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_stretched", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_stretched_ext", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_pos", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_tiled", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_tiled_ext", new AssetIDType[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },

                { "draw_background", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_ext", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_part", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_part_ext", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_stretched", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_stretched_ext", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_tiled", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_tiled_ext", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_general", new AssetIDType[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },

                { "draw_set_font", new AssetIDType[] { AssetIDType.Font } },
                { "draw_set_halign", new AssetIDType[] { AssetIDType.Enum_HAlign } },
                { "draw_set_valign", new AssetIDType[] { AssetIDType.Enum_VAlign } },
                { "draw_text_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_text_ext_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_text_transformed_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_text_transformed_ext_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },

                { "draw_vertex_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_vertex_texture_colour", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },

                { "distance_to_object", new AssetIDType[] { AssetIDType.GameObject } },

                { "place_meeting", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "position_meeting", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "position_change", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other } },
                { "collision_circle", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "collision_ellipse", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "collision_line", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "collision_point", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "collision_rectangle", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },

                { "mp_linear_step", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_linear_step_object", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "mp_linear_path", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_linear_path_object", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "mp_potential_settings", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_potential_step", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_potential_step_object", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "mp_potential_path", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_potential_path_object", new AssetIDType[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                // mp_grid only relevant ones because I'm lazy
                { "mp_grid_path", new AssetIDType[] { AssetIDType.Other, AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean } },
                { "mp_grid_add_instances", new AssetIDType[] { AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other } },

                // TODO: 3D drawing, didn't bother

                // TODO: surface drawing

                { "shader_is_compiled", new AssetIDType[] { AssetIDType.Shader } },
                { "shader_set", new AssetIDType[] { AssetIDType.Shader } },
                // { "shader_current", new AssetIDType[] { } }, returns shader.

                // TODO: GMS2 tilemaps
                // TODO: GMS2 layers
            
                { "io_clear", new AssetIDType[] { } },
                { "keyboard_multicheck", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_multicheck_pressed", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_check", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_check_pressed", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_check_released", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_check_direct", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_clear", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_key_press", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_key_release", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_set_map", new AssetIDType[] { AssetIDType.KeyboardKey, AssetIDType.KeyboardKey } },
                { "keyboard_get_map", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_unset_map", new AssetIDType[] { AssetIDType.KeyboardKey } },
                { "keyboard_set_numlock", new AssetIDType[] { AssetIDType.Other } },
                { "keyboard_get_numlock", new AssetIDType[] { } },

                { "gamepad_button_value", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_button_check", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_button_check_pressed", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_button_check_released", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },

                { "buffer_create", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_BufferKind, AssetIDType.Other } },
                { "buffer_create_from_vertex_buffer", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_BufferKind, AssetIDType.Other } },
                { "buffer_create_from_vertex_buffer_ext", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_BufferKind, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "buffer_read", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_BufferType } },
                { "buffer_write", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_BufferType, AssetIDType.Other } },
                { "buffer_peek", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Enum_BufferType } },
                { "buffer_poke", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Enum_BufferType, AssetIDType.Other } },
                { "buffer_fill", new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Enum_BufferType, AssetIDType.Other, AssetIDType.Other } },
                { "buffer_sizeof", new AssetIDType[] { AssetIDType.Enum_BufferType } },
                { "buffer_seek", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_BufferSeek, AssetIDType.Other } },

                // Also big TODO: Implement Boolean type for all these functions
            };

            builtin_vars = new Dictionary<string, AssetIDType>()
            {
                // only the relevant ones because I'm sick of writing this
                { "background_index", AssetIDType.Background }, // array
                { "background_colour", AssetIDType.Color }, // array
                { "view_object", AssetIDType.GameObject }, // array
                { "path_index", AssetIDType.Path },
                { "room_first", AssetIDType.Room },
                { "room_last", AssetIDType.Room },
                { "room", AssetIDType.Room },
                { "object_index", AssetIDType.GameObject },
                { "sprite_index", AssetIDType.Sprite },
                { "mask_index", AssetIDType.Sprite },
                { "image_blend", AssetIDType.Color },
                { "event_object", AssetIDType.GameObject },
                { "keyboard_key", AssetIDType.KeyboardKey },
                { "keyboard_lastkey", AssetIDType.KeyboardKey },
                { "os_type", AssetIDType.Enum_OSType },
                { "timeline_index", AssetIDType.Timeline },
                { "path_endaction", AssetIDType.Enum_PathEndAction },
                { "view_enabled", AssetIDType.Boolean },
                { "view_visible", AssetIDType.Boolean },

            };

            // TODO: make proper file/manifest for all games to use, not just UT/DR, and also not these specific names
            string lowerName = data?.GeneralInfo?.DisplayName?.Content.ToLower();
            //Just Undertale
            if (lowerName != null && (lowerName == "undertale"))
            {
                //Sometimes used as a bool, should not matter though and be an improvement overall.
                builtin_vars.Add("king", AssetIDType.GameObject);
                builtin_funcs["SCR_TEXTSETUP"] = new AssetIDType[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                //I should confirm adding this causes no adverse effects later. 
                builtin_vars.Add("myroom", AssetIDType.Room);
                //gml_Object_obj_dummytrigger_Collision_1576
                builtin_vars.Add("dummy", AssetIDType.GameObject);
                //This should do something to fix the piano room
                builtin_vars.Add("sprite_id", AssetIDType.Sprite);
                builtin_vars.Add("myfont", AssetIDType.Font);
                builtin_funcs["scr_getsprite"] = new AssetIDType[] { AssetIDType.Sprite };
                //gml_Object_obj_barabody_Create_0
                builtin_vars.Add("hand1pic", AssetIDType.Sprite);
                builtin_vars.Add("hand2pic", AssetIDType.Sprite);
                builtin_vars.Add("headpic", AssetIDType.Sprite);
                builtin_vars.Add("bodypic", AssetIDType.Sprite);
            }
            //Just deltarune
            if (lowerName != null && (lowerName == "survey_program" || lowerName.StartsWith("deltarune"))) {
                builtin_vars.Add("idlesprite", AssetIDType.Sprite);
                builtin_vars.Add("actreadysprite", AssetIDType.Sprite);
                builtin_vars.Add("actsprite", AssetIDType.Sprite);
                builtin_vars.Add("defendsprite", AssetIDType.Sprite);
                builtin_vars.Add("attackreadysprite", AssetIDType.Sprite);
                builtin_vars.Add("attacksprite", AssetIDType.Sprite);
                builtin_vars.Add("itemsprite", AssetIDType.Sprite);
                builtin_vars.Add("itemreadysprite", AssetIDType.Sprite);
                builtin_vars.Add("spellreadysprite", AssetIDType.Sprite);
                builtin_vars.Add("spellsprite", AssetIDType.Sprite);
                builtin_vars.Add("defeatsprite", AssetIDType.Sprite);
                builtin_vars.Add("victorysprite", AssetIDType.Sprite);
                builtin_vars.Add("dsprite_blush", AssetIDType.Sprite);
                builtin_vars.Add("usprite_blush", AssetIDType.Sprite);
                builtin_vars.Add("lsprite_blush", AssetIDType.Sprite);
                builtin_vars.Add("rsprite_blush", AssetIDType.Sprite);
                builtin_vars.Add("heartsprite", AssetIDType.Sprite);
                builtin_vars.Add("msprite", AssetIDType.Sprite);
                builtin_vars.Add("particlesprite", AssetIDType.Sprite);
                builtin_vars.Add("s_sprite", AssetIDType.Sprite);
                builtin_vars.Add("shopkeepsprite", AssetIDType.Sprite);
                builtin_vars.Add("slidesprite", AssetIDType.Sprite);
                builtin_vars.Add("smsprite", AssetIDType.Sprite);
                builtin_vars.Add("sparedsprite", AssetIDType.Sprite);
                builtin_vars.Add("sussprite", AssetIDType.Sprite);
                //"targetsprite" seems to be unused but just in case
                builtin_vars.Add("targetsprite", AssetIDType.Sprite);
                builtin_vars.Add("thissprite", AssetIDType.Sprite);
                builtin_vars.Add("touchsprite", AssetIDType.Sprite);
                builtin_vars.Add("sprite_type", AssetIDType.Sprite);
                builtin_vars.Add("darkzone", AssetIDType.Boolean);
                builtin_vars.Add("darkmode", AssetIDType.Boolean);
                builtin_vars.Add("darkify", AssetIDType.Boolean);
                builtin_vars.Add("noroom", AssetIDType.Boolean);
                builtin_vars.Add("room_persistent", AssetIDType.Boolean);
                builtin_vars.Add("loop", AssetIDType.Boolean);
                builtin_vars.Add("__loadedroom", AssetIDType.Room);
                builtin_vars.Add("roomchoice", AssetIDType.Room);
                builtin_vars.Add("writersnd", AssetIDType.Sound);
                builtin_vars.Add("sndchange", AssetIDType.Boolean);
                builtin_vars.Add("muschange", AssetIDType.Boolean);
                builtin_vars.Add("audchange", AssetIDType.Boolean);
                builtin_vars.Add("sndplay", AssetIDType.Boolean);
                builtin_vars.Add("sound_played", AssetIDType.Boolean);
                builtin_vars.Add("chalksound", AssetIDType.Boolean);
                builtin_vars.Add("grabsounded", AssetIDType.Boolean);
                builtin_vars.Add("hatsounded", AssetIDType.Boolean);
                builtin_vars.Add("soundplayed", AssetIDType.Boolean);
                builtin_vars.Add("windsound", AssetIDType.Boolean);
                builtin_vars.Add("playtextsound", AssetIDType.Boolean);
                builtin_vars.Add("textsound", AssetIDType.Sound);
                builtin_vars.Add("selectnoise", AssetIDType.Boolean);
                builtin_vars.Add("movenoise", AssetIDType.Boolean);
                builtin_vars.Add("grazenoise", AssetIDType.Boolean);
                builtin_vars.Add("selnoise", AssetIDType.Boolean);
                builtin_vars.Add("damagenoise", AssetIDType.Boolean);
                builtin_vars.Add("laznoise", AssetIDType.Boolean);
                builtin_vars.Add("stepnoise", AssetIDType.Boolean);
                builtin_vars.Add("bumpnoise", AssetIDType.Boolean);
                builtin_vars.Add("burstnoise", AssetIDType.Boolean);
                builtin_vars.Add("BACKNOISE", AssetIDType.Boolean);
                builtin_vars.Add("DEATHNOISE", AssetIDType.Boolean);
                builtin_vars.Add("gnoise", AssetIDType.Boolean);
                builtin_vars.Add("firstnoise", AssetIDType.Boolean);
                builtin_vars.Add("dmgnoise", AssetIDType.Boolean);
                builtin_vars.Add("usable", AssetIDType.Boolean);
                builtin_vars.Add("tempkeyitemusable", AssetIDType.Boolean);
                builtin_vars.Add("spellusable", AssetIDType.Boolean);
                builtin_vars.Add("NAMEFADE_COMPLETE", AssetIDType.Boolean);
                builtin_vars.Add("dancekris", AssetIDType.GameObject);
                builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                builtin_vars.Add("attacked", AssetIDType.Boolean);
                builtin_vars.Add("attack_qual", AssetIDType.Boolean);
                builtin_vars.Add("attacking", AssetIDType.Boolean);
                builtin_vars.Add("attackedkris", AssetIDType.Boolean);
                builtin_vars.Add("attacks", AssetIDType.Boolean);
                builtin_vars.Add("battleend", AssetIDType.Boolean);
                builtin_vars.Add("battlemode", AssetIDType.Boolean);
                builtin_vars.Add("battlemoder", AssetIDType.Boolean);
                builtin_vars.Add("becamebattle", AssetIDType.Boolean);
                builtin_vars.Add("seriousbattle", AssetIDType.Boolean);
                builtin_vars.Add("cango", AssetIDType.Boolean);
                builtin_vars.Add("canact", AssetIDType.Boolean);
                builtin_vars.Add("CANCEL", AssetIDType.Boolean);
                builtin_vars.Add("cancelwalk", AssetIDType.Boolean);
                builtin_vars.Add("cancelattack", AssetIDType.Boolean);
                builtin_vars.Add("canchoose", AssetIDType.Boolean);
                builtin_vars.Add("canclick", AssetIDType.Boolean);
                builtin_vars.Add("cancollide", AssetIDType.Boolean);
                builtin_vars.Add("candodge", AssetIDType.Boolean);
                builtin_vars.Add("candraw", AssetIDType.Boolean);
                builtin_vars.Add("canequip", AssetIDType.Boolean);
                builtin_vars.Add("canpress", AssetIDType.Boolean);
                builtin_vars.Add("cant", AssetIDType.Boolean);
                builtin_vars.Add("depthcancel", AssetIDType.Boolean);
                builtin_vars.Add("defend_command", AssetIDType.Boolean);
                builtin_vars.Add("automiss", AssetIDType.Boolean);
                builtin_vars.Add("awoke", AssetIDType.Boolean);
                builtin_vars.Add("act_command", AssetIDType.Boolean);
                builtin_vars.Add("acted", AssetIDType.Boolean);
                builtin_vars.Add("activated", AssetIDType.Boolean);
                builtin_vars.Add("activatethrow", AssetIDType.Boolean);
                builtin_vars.Add("addflag", AssetIDType.Boolean);
                builtin_vars.Add("addup", AssetIDType.Boolean);
                builtin_vars.Add("afford", AssetIDType.Boolean);
                builtin_vars.Add("aftercon", AssetIDType.Boolean);
                builtin_vars.Add("ALREADY", AssetIDType.Boolean);
                builtin_vars.Add("ambushed", AssetIDType.Boolean);
                builtin_vars.Add("permashake", AssetIDType.Boolean);
                builtin_vars.Add("aster", AssetIDType.Boolean);
                builtin_vars.Add("autoaster", AssetIDType.Boolean);
                builtin_vars.Add("autoed", AssetIDType.Boolean);
                builtin_vars.Add("betray", AssetIDType.Boolean);
                builtin_vars.Add("abovemaxhp", AssetIDType.Boolean);
                builtin_vars.Add("abletotarget", AssetIDType.Boolean);
                builtin_vars.Add("accept", AssetIDType.Boolean);
                builtin_vars.Add("actual", AssetIDType.Boolean);
                builtin_vars.Add("currentsong", AssetIDType.Sound);
                builtin_vars.Add("batmusic", AssetIDType.Sound);
                builtin_vars.Add("beanie", AssetIDType.Boolean);
                builtin_vars.Add("beaten", AssetIDType.Boolean);
                builtin_vars.Add("becomeflash", AssetIDType.Boolean);
                builtin_vars.Add("becomesleep", AssetIDType.Boolean);
                builtin_vars.Add("sleeping", AssetIDType.Boolean);
                builtin_vars.Add("bellcon", AssetIDType.Boolean);
                builtin_vars.Add("belowzero", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //builtin_vars.Add("noiseskip", AssetIDType.Boolean);
                //Colors weave into a spire of flame
                builtin_vars.Add("mycolor", AssetIDType.Color);
                builtin_vars.Add("colorchange", AssetIDType.Boolean);
                builtin_vars.Add("xcolor", AssetIDType.Color);
                builtin_vars.Add("skippable", AssetIDType.Boolean);
                builtin_vars.Add("charcolor", AssetIDType.Color);
                builtin_vars.Add("hpcolor", AssetIDType.Color);
                builtin_vars.Add("bcolor", AssetIDType.Color);
                builtin_vars.Add("flashcolor", AssetIDType.Color);
                builtin_vars.Add("smcolor", AssetIDType.Color);
                builtin_vars.Add("dcolor", AssetIDType.Color);
                builtin_vars.Add("basecolor", AssetIDType.Color);
                builtin_vars.Add("_abilitycolor", AssetIDType.Color);
                builtin_vars.Add("mnamecolor1", AssetIDType.Color);
                builtin_vars.Add("mnamecolor2", AssetIDType.Color);
                builtin_vars.Add("scolor", AssetIDType.Color);
                builtin_vars.Add("arrowcolor", AssetIDType.Color);
                builtin_vars.Add("particlecolor", AssetIDType.Color);
                builtin_vars.Add("linecolor", AssetIDType.Color);
                builtin_vars.Add("fadecolor", AssetIDType.Color);
                builtin_vars.Add("color", AssetIDType.Color);
            }
            //Both UT and DR
            if (lowerName != null && (lowerName == "undertale" || lowerName == "survey_program" || lowerName.StartsWith("deltarune")))
            {
                //Hope this script works!
                builtin_funcs["scr_bouncer"] = new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_vars.Add("currentroom", AssetIDType.Room);
                builtin_vars.Add("dsprite", AssetIDType.Sprite);
                builtin_vars.Add("usprite", AssetIDType.Sprite);
                builtin_vars.Add("lsprite", AssetIDType.Sprite);
                builtin_vars.Add("rsprite", AssetIDType.Sprite);
                builtin_vars.Add("dtsprite", AssetIDType.Sprite);
                builtin_vars.Add("utsprite", AssetIDType.Sprite);
                builtin_vars.Add("ltsprite", AssetIDType.Sprite);
                builtin_vars.Add("rtsprite", AssetIDType.Sprite);
                builtin_vars.Add("normalsprite", AssetIDType.Sprite);
                builtin_vars.Add("hurtsprite", AssetIDType.Sprite);
                builtin_vars.Add("hurtsound", AssetIDType.Sound);
                //New built in vars found by Grossley
                builtin_vars.Add("interact", AssetIDType.Other);
                //Test me!
                builtin_vars.Add("sound0", AssetIDType.Sound);
                //From v1.11 Undertale comparison, not tested unlike v1.001!
                builtin_vars.Add("asprite", AssetIDType.Sprite);
                builtin_vars.Add("bsprite", AssetIDType.Sprite);
                builtin_vars.Add("tailobj", AssetIDType.GameObject);
                builtin_vars.Add("heart", AssetIDType.GameObject);
                builtin_vars.Add("draedmode", AssetIDType.Boolean);
                builtin_vars.Add("background_color", AssetIDType.Color);
                builtin_vars.Add("object0", AssetIDType.GameObject);
                builtin_vars.Add("part1", AssetIDType.GameObject);
                builtin_vars.Add("pap", AssetIDType.GameObject);
                builtin_vars.Add("fileerased", AssetIDType.Sprite);
                builtin_vars.Add("catty", AssetIDType.GameObject);
                builtin_vars.Add("bratty", AssetIDType.GameObject);
                builtin_vars.Add("creator", AssetIDType.GameObject);
                //It's not 100% accurate to resolve this way but it seems like this variable only gets directly assigned values and is used as a bool, it should be fine.
                builtin_vars.Add("parent", AssetIDType.GameObject);
                //These are not so consistent... ;-;
                //op is used in Muffet's stuff but is critical in Omega flowey positioning... worse to resolve than to not.
                //builtin_vars.Add("op", AssetIDType.GameObject);
                //Toby messed up in "gml_Object_obj_wizardorb_chaser_Alarm_0" (should be "scr_monstersum()"), "pop" is never a script.
                //From v1.001 Undertale via comparison
                //A TIER quality
                builtin_vars.Add("onionsprite", AssetIDType.Sprite);
                builtin_vars.Add("headsprite", AssetIDType.Sprite);
                builtin_vars.Add("breaksprite", AssetIDType.Sprite);
                builtin_vars.Add("foodimg", AssetIDType.Sprite);
                builtin_vars.Add("facespr", AssetIDType.Sprite);
                builtin_vars.Add("bombsprite", AssetIDType.Sprite);
                builtin_vars.Add("mysprite", AssetIDType.Sprite);
                builtin_vars.Add("arms", AssetIDType.Sprite);
                builtin_vars.Add("levelpic", AssetIDType.Sprite);
                builtin_vars.Add("image", AssetIDType.Sprite);
                builtin_vars.Add("song_index", AssetIDType.Sound);
                builtin_vars.Add("thischara", AssetIDType.GameObject);
                //B TIER quality
                builtin_vars.Add("tspr5", AssetIDType.Sprite);
                builtin_vars.Add("tspr3", AssetIDType.Sprite);
                builtin_vars.Add("tspr2", AssetIDType.Sprite);
                builtin_vars.Add("tspr1", AssetIDType.Sprite);
                builtin_vars.Add("tspr4", AssetIDType.Sprite);
                builtin_vars.Add("snapper", AssetIDType.GameObject);
                builtin_vars.Add("subject", AssetIDType.GameObject);
                builtin_vars.Add("clip", AssetIDType.GameObject);
                //C TIER quality
                builtin_vars.Add("sound1", AssetIDType.Sound);
                builtin_vars.Add("sound2", AssetIDType.Sound);
            }
        }
    }
}
