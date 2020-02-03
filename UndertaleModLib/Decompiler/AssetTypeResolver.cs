using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
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
        os_windows = 0, // legacy constant os_win32 is equal to os_windows
        os_macosx = 1,
        os_psp = 2,
        os_ios = 3,
        os_android = 4,
        os_symbian = 5,
        os_linux = 6,
        os_winphone = 7,
        os_tizen = 8,
        os_win8native = 9,
        os_wiiu = 10,
        os_3ds = 11,
        os_psvita = 12,
        os_bb10 = 13,
        os_ps4 = 14,
        os_xboxone = 15,
        os_ps3 = 16,
        os_xbox360 = 17,
        os_uwp = 18,
        os_amazon = 19, // the same as android but... different?
        os_switch_beta = 20, // this one was used while switch support was in beta and changed later? In newer runtimes 20 is now os_tvos...
        os_switch = 21,
        os_unknown = -1
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
        public static Dictionary<string, AssetIDType[]> custom_funcs;

        public static Dictionary<string, AssetIDType> builtin_vars;
        public static Dictionary<string, AssetIDType> custom_vars;

        public static string programDir = AppDomain.CurrentDomain.BaseDirectory;

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

        public static void ATRLog(string line)
        {
            try
            {
                File.AppendAllLines(programDir + "ATR.log", line);
            }
            catch
            {
                // Do nothing.
            }
        }

        public static AssetIDType StringToAssetIDRef(string xmltype)
        {
            // Returns an AssetIDType, used only in XML stuff.
            xmltype = xmltype.ToLower(); // case-insensitive.
            switch (xmltype)
            {
                case "sprite": return AssetIDType.Sprite;
                case "sound": return AssetIDType.Sound;
                case "background": return AssetIDType.Background;
                case "font": return AssetIDType.Font;
                case "path": return AssetIDType.Path;
                case "timeline": return AssetIDType.Timeline;
                case "gameobject":
                case "object":
                    return AssetIDType.GameObject; // i allow both.
                case "boolean": return AssetIDType.Boolean;
                case "gamepadbutton": return AssetIDType.Enum_GamepadButton;
                case "room": return AssetIDType.Room;
                case "shader": return AssetIDType.Shader;
                case "other": return AssetIDType.Other;
                case "layer": return AssetIDType.Layer;
                case "color": return AssetIDType.Color;
                case "keyboardkey":
                case "keyboard":
                case "key":
                    return AssetIDType.KeyboardKey;
                case "ostype": return AssetIDType.Enum_OSType;
                case "script": return AssetIDType.Script;

                default:
                {
                    ATRLog("Could not evaluate AssetType! Your type was: " + xmltype); // ??? Oh no.
                    return AssetIDType.Other;
                }
            }
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
                { "gamepad_axis_value", new AssetIDType[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },

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

            custom_funcs = new Dictionary<string, AssetIDType[]>();
            custom_vars = new Dictionary<string, AssetIDType>();

            int length = 3;

            Dictionary<string, AssetIDType>[] cv_arr = new Dictionary<string, AssetIDType>[length];
            Dictionary<string, AssetIDType[]>[] cf_arr = new Dictionary<string, AssetIDType[]>[length];
            string[] cv_conditions = new string[length];

            bool errorOccured = false;
            if (lowerName != null && File.Exists(programDir + "AssetTypeResolverProfile.xml"))
            {
                try { LoadAssetDataFromXML(lowerName); }
                catch { errorOccured = true; }

                if (!errorOccured)
                {
                    foreach (KeyValuePair<string, AssetIDType> custom_var in custom_vars)
                        builtin_vars.Add(custom_var.Key, custom_var.Value);

                    foreach (KeyValuePair<string, AssetIDType[]> custom_func in custom_funcs)
                        builtin_funcs.Add(custom_func.Key, custom_func.Value);
                }
            } else errorOccured = true;
            if (lowerName != null || errorOccured) // The file doesn't exist, load internal data.
            {
                //Just Undertale
                //Sometimes used as a bool, should not matter though and be an improvement overall.
                cv_arr[0] = new Dictionary<string, AssetIDType>();
                cf_arr[0] = new Dictionary<string, AssetIDType[]>();
                cv_arr[0].Add("king", AssetIDType.GameObject);
                cf_arr[0]["SCR_TEXTSETUP"] = new AssetIDType[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                //I should confirm adding this causes no adverse effects later. 
                cv_arr[0].Add("myroom", AssetIDType.Room);
                //gml_Object_obj_dummytrigger_Collision_1576
                cv_arr[0].Add("dummy", AssetIDType.GameObject);
                //gml_Object_obj_asriel_swordarm_Create_0
                cv_arr[0].Add("sm", AssetIDType.GameObject);
                //This should do something to fix the piano room
                cv_arr[0].Add("sprite_id", AssetIDType.Sprite);
                cf_arr[0]["scr_getsprite"] = new AssetIDType[] { AssetIDType.Sprite };
                //gml_Object_obj_barabody_Create_0
                cv_arr[0].Add("hand1pic", AssetIDType.Sprite);
                cv_arr[0].Add("hand2pic", AssetIDType.Sprite);
                cv_arr[0].Add("headpic", AssetIDType.Sprite);
                //gml_Object_obj_asgoreb_body_Create_0
                cv_arr[0].Add("bodypic", AssetIDType.Sprite);
                //gml_Object_obj_castroll_Draw_0
                cv_arr[0].Add("do_room_goto", AssetIDType.Boolean);
                cv_arr[0].Add("do_room_goto_target", AssetIDType.Room);

                cv_conditions[0] = "undertale";


                //Just deltarune
                cv_arr[1] = new Dictionary<string, AssetIDType>();
                cf_arr[1] = new Dictionary<string, AssetIDType[]>();
                cv_arr[1].Add("idlesprite", AssetIDType.Sprite);
                cv_arr[1].Add("actreadysprite", AssetIDType.Sprite);
                cv_arr[1].Add("actsprite", AssetIDType.Sprite);
                cv_arr[1].Add("defendsprite", AssetIDType.Sprite);
                cv_arr[1].Add("attackreadysprite", AssetIDType.Sprite);
                cv_arr[1].Add("attacksprite", AssetIDType.Sprite);
                cv_arr[1].Add("itemsprite", AssetIDType.Sprite);
                cv_arr[1].Add("itemreadysprite", AssetIDType.Sprite);
                cv_arr[1].Add("spellreadysprite", AssetIDType.Sprite);
                cv_arr[1].Add("spellsprite", AssetIDType.Sprite);
                cv_arr[1].Add("defeatsprite", AssetIDType.Sprite);
                cv_arr[1].Add("victorysprite", AssetIDType.Sprite);
                cv_arr[1].Add("dsprite_blush", AssetIDType.Sprite);
                cv_arr[1].Add("usprite_blush", AssetIDType.Sprite);
                cv_arr[1].Add("lsprite_blush", AssetIDType.Sprite);
                cv_arr[1].Add("rsprite_blush", AssetIDType.Sprite);
                cv_arr[1].Add("heartsprite", AssetIDType.Sprite);
                cv_arr[1].Add("msprite", AssetIDType.Sprite);
                cv_arr[1].Add("particlesprite", AssetIDType.Sprite);
                cv_arr[1].Add("s_sprite", AssetIDType.Sprite);
                cv_arr[1].Add("shopkeepsprite", AssetIDType.Sprite);
                cv_arr[1].Add("slidesprite", AssetIDType.Sprite);
                cv_arr[1].Add("smsprite", AssetIDType.Sprite);
                cv_arr[1].Add("sparedsprite", AssetIDType.Sprite);
                cv_arr[1].Add("sussprite", AssetIDType.Sprite);
                //"targetsprite" seems to be unused but just in case
                cv_arr[1].Add("targetsprite", AssetIDType.Sprite);
                cv_arr[1].Add("thissprite", AssetIDType.Sprite);
                cv_arr[1].Add("touchsprite", AssetIDType.Sprite);
                cv_arr[1].Add("sprite_type", AssetIDType.Sprite);
                cv_arr[1].Add("darkzone", AssetIDType.Boolean);
                cv_arr[1].Add("darkmode", AssetIDType.Boolean);
                cv_arr[1].Add("darkify", AssetIDType.Boolean);
                cv_arr[1].Add("noroom", AssetIDType.Boolean);
                cv_arr[1].Add("room_persistent", AssetIDType.Boolean);
                cv_arr[1].Add("loop", AssetIDType.Boolean);
                cv_arr[1].Add("__loadedroom", AssetIDType.Room);
                cv_arr[1].Add("roomchoice", AssetIDType.Room);
                cv_arr[1].Add("writersnd", AssetIDType.Sound);
                cv_arr[1].Add("sndchange", AssetIDType.Boolean);
                cv_arr[1].Add("muschange", AssetIDType.Boolean);
                cv_arr[1].Add("audchange", AssetIDType.Boolean);
                cv_arr[1].Add("sndplay", AssetIDType.Boolean);
                cv_arr[1].Add("sound_played", AssetIDType.Boolean);
                cv_arr[1].Add("chalksound", AssetIDType.Boolean);
                cv_arr[1].Add("grabsounded", AssetIDType.Boolean);
                cv_arr[1].Add("hatsounded", AssetIDType.Boolean);
                cv_arr[1].Add("soundplayed", AssetIDType.Boolean);
                cv_arr[1].Add("windsound", AssetIDType.Boolean);
                cv_arr[1].Add("playtextsound", AssetIDType.Boolean);
                cv_arr[1].Add("textsound", AssetIDType.Sound);
                cv_arr[1].Add("selectnoise", AssetIDType.Boolean);
                cv_arr[1].Add("movenoise", AssetIDType.Boolean);
                cv_arr[1].Add("grazenoise", AssetIDType.Boolean);
                cv_arr[1].Add("selnoise", AssetIDType.Boolean);
                cv_arr[1].Add("damagenoise", AssetIDType.Boolean);
                cv_arr[1].Add("laznoise", AssetIDType.Boolean);
                cv_arr[1].Add("stepnoise", AssetIDType.Boolean);
                cv_arr[1].Add("bumpnoise", AssetIDType.Boolean);
                cv_arr[1].Add("burstnoise", AssetIDType.Boolean);
                cv_arr[1].Add("BACKNOISE", AssetIDType.Boolean);
                cv_arr[1].Add("DEATHNOISE", AssetIDType.Boolean);
                cv_arr[1].Add("gnoise", AssetIDType.Boolean);
                cv_arr[1].Add("firstnoise", AssetIDType.Boolean);
                cv_arr[1].Add("dmgnoise", AssetIDType.Boolean);
                cv_arr[1].Add("usable", AssetIDType.Boolean);
                cv_arr[1].Add("tempkeyitemusable", AssetIDType.Boolean);
                cv_arr[1].Add("spellusable", AssetIDType.Boolean);
                cv_arr[1].Add("NAMEFADE_COMPLETE", AssetIDType.Boolean);
                cv_arr[1].Add("dancekris", AssetIDType.GameObject);
                cv_arr[1].Add("noiseskip", AssetIDType.Boolean);
                cv_arr[1].Add("attacked", AssetIDType.Boolean);
                cv_arr[1].Add("attack_qual", AssetIDType.Boolean);
                cv_arr[1].Add("attacking", AssetIDType.Boolean);
                cv_arr[1].Add("attackedkris", AssetIDType.Boolean);
                cv_arr[1].Add("attacks", AssetIDType.Boolean);
                cv_arr[1].Add("battleend", AssetIDType.Boolean);
                cv_arr[1].Add("battlemoder", AssetIDType.Boolean);
                cv_arr[1].Add("becamebattle", AssetIDType.Boolean);
                cv_arr[1].Add("seriousbattle", AssetIDType.Boolean);
                //A little bit wrong, but probably fine.
                cv_arr[1].Add("cango", AssetIDType.Boolean);
                cv_arr[1].Add("canact", AssetIDType.Boolean);
                cv_arr[1].Add("CANCEL", AssetIDType.Boolean);
                cv_arr[1].Add("cancelwalk", AssetIDType.Boolean);
                cv_arr[1].Add("cancelattack", AssetIDType.Boolean);
                cv_arr[1].Add("canchoose", AssetIDType.Boolean);
                cv_arr[1].Add("canclick", AssetIDType.Boolean);
                cv_arr[1].Add("cancollide", AssetIDType.Boolean);
                cv_arr[1].Add("candodge", AssetIDType.Boolean);
                cv_arr[1].Add("candraw", AssetIDType.Boolean);
                cv_arr[1].Add("canequip", AssetIDType.Boolean);
                cv_arr[1].Add("canpress", AssetIDType.Boolean);
                cv_arr[1].Add("cant", AssetIDType.Boolean);
                cv_arr[1].Add("depthcancel", AssetIDType.Boolean);
                cv_arr[1].Add("defend_command", AssetIDType.Boolean);
                cv_arr[1].Add("automiss", AssetIDType.Boolean);
                cv_arr[1].Add("awoke", AssetIDType.Boolean);
                cv_arr[1].Add("act_command", AssetIDType.Boolean);
                cv_arr[1].Add("acted", AssetIDType.Boolean);
                cv_arr[1].Add("activated", AssetIDType.Boolean);
                cv_arr[1].Add("activatethrow", AssetIDType.Boolean);
                cv_arr[1].Add("addflag", AssetIDType.Boolean);
                cv_arr[1].Add("addup", AssetIDType.Boolean);
                cv_arr[1].Add("afford", AssetIDType.Boolean);
                cv_arr[1].Add("aftercon", AssetIDType.Boolean);
                cv_arr[1].Add("ALREADY", AssetIDType.Boolean);
                cv_arr[1].Add("ambushed", AssetIDType.Boolean);
                cv_arr[1].Add("permashake", AssetIDType.Boolean);
                cv_arr[1].Add("aster", AssetIDType.Boolean);
                cv_arr[1].Add("autoaster", AssetIDType.Boolean);
                cv_arr[1].Add("autoed", AssetIDType.Boolean);
                cv_arr[1].Add("betray", AssetIDType.Boolean);
                cv_arr[1].Add("abovemaxhp", AssetIDType.Boolean);
                cv_arr[1].Add("abletotarget", AssetIDType.Boolean);
                cv_arr[1].Add("accept", AssetIDType.Boolean);
                cv_arr[1].Add("actual", AssetIDType.Boolean);
                cv_arr[1].Add("currentsong", AssetIDType.Sound);
                cv_arr[1].Add("batmusic", AssetIDType.Sound);
                cv_arr[1].Add("beanie", AssetIDType.Boolean);
                cv_arr[1].Add("beaten", AssetIDType.Boolean);
                cv_arr[1].Add("becomeflash", AssetIDType.Boolean);
                cv_arr[1].Add("becomesleep", AssetIDType.Boolean);
                cv_arr[1].Add("sleeping", AssetIDType.Boolean);
                cv_arr[1].Add("bellcon", AssetIDType.Boolean);
                cv_arr[1].Add("belowzero", AssetIDType.Boolean);
                //cv_arr[1].Add("noiseskip", AssetIDType.Boolean);
                //Colors weave into a spire of flame
                cv_arr[1].Add("mycolor", AssetIDType.Color);
                cv_arr[1].Add("colorchange", AssetIDType.Boolean);
                cv_arr[1].Add("xcolor", AssetIDType.Color);
                cv_arr[1].Add("skippable", AssetIDType.Boolean);
                cv_arr[1].Add("charcolor", AssetIDType.Color);
                cv_arr[1].Add("hpcolor", AssetIDType.Color);
                cv_arr[1].Add("bcolor", AssetIDType.Color);
                cv_arr[1].Add("flashcolor", AssetIDType.Color);
                cv_arr[1].Add("smcolor", AssetIDType.Color);
                cv_arr[1].Add("dcolor", AssetIDType.Color);
                cv_arr[1].Add("basecolor", AssetIDType.Color);
                cv_arr[1].Add("_abilitycolor", AssetIDType.Color);
                cv_arr[1].Add("mnamecolor1", AssetIDType.Color);
                cv_arr[1].Add("mnamecolor2", AssetIDType.Color);
                cv_arr[1].Add("scolor", AssetIDType.Color);
                cv_arr[1].Add("arrowcolor", AssetIDType.Color);
                cv_arr[1].Add("particlecolor", AssetIDType.Color);
                cv_arr[1].Add("linecolor", AssetIDType.Color);
                cv_arr[1].Add("fadecolor", AssetIDType.Color);
                cv_arr[1].Add("color", AssetIDType.Color);
                //Scripts
                cf_arr[1]["SCR_TEXTSETUP"] = new AssetIDType[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };

                cv_conditions[1] = "survey_program|&deltarune";


                //Both UT and DR
                cv_arr[2] = new Dictionary<string, AssetIDType>();
                cf_arr[2] = new Dictionary<string, AssetIDType[]>();
                //gml_Object_obj_vulkinbody_UNDERTALE_Create_0
                //Seems to be used a lot as a regular value between the values of around 0-20. 
                cv_arr[2].Add("face", AssetIDType.Sprite);
                cv_arr[2].Add("myfont", AssetIDType.Font);
                //Hope this script works!
                cf_arr[2]["scr_bouncer"] = new AssetIDType[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                cv_arr[2].Add("currentroom", AssetIDType.Room);
                cv_arr[2].Add("dsprite", AssetIDType.Sprite);
                cv_arr[2].Add("usprite", AssetIDType.Sprite);
                cv_arr[2].Add("lsprite", AssetIDType.Sprite);
                cv_arr[2].Add("rsprite", AssetIDType.Sprite);
                cv_arr[2].Add("dtsprite", AssetIDType.Sprite);
                cv_arr[2].Add("utsprite", AssetIDType.Sprite);
                cv_arr[2].Add("ltsprite", AssetIDType.Sprite);
                cv_arr[2].Add("rtsprite", AssetIDType.Sprite);
                cv_arr[2].Add("normalsprite", AssetIDType.Sprite);
                cv_arr[2].Add("hurtsprite", AssetIDType.Sprite);
                cv_arr[2].Add("hurtsound", AssetIDType.Sound);
                //New built in vars found by Grossley
                cv_arr[2].Add("interact", AssetIDType.Other);
                //Test me!
                cv_arr[2].Add("sound0", AssetIDType.Sound);
                //From v1.11 Undertale comparison, not tested unlike v1.001!
                cv_arr[2].Add("asprite", AssetIDType.Sprite);
                cv_arr[2].Add("bsprite", AssetIDType.Sprite);
                cv_arr[2].Add("tailobj", AssetIDType.GameObject);
                cv_arr[2].Add("heart", AssetIDType.GameObject);
                cv_arr[2].Add("draedmode", AssetIDType.Boolean);
                //Deltarune
                cv_arr[2].Add("haveauto", AssetIDType.Boolean);
                cv_arr[2].Add("goahead", AssetIDType.Boolean);
                cv_arr[2].Add("is_auto_susie", AssetIDType.Boolean);
                cv_arr[2].Add("techwon", AssetIDType.Boolean);
                cv_arr[2].Add("itemed", AssetIDType.Boolean);
                cv_arr[2].Add("critical", AssetIDType.Boolean);
                cv_arr[2].Add("tile_fade", AssetIDType.Boolean);
                cv_arr[2].Add("boss", AssetIDType.Boolean);
                cv_arr[2].Add("skipvictory", AssetIDType.Boolean);
                cv_arr[2].Add("victory", AssetIDType.Boolean);
                cv_arr[2].Add("fighting", AssetIDType.Boolean);
                cv_arr[2].Add("charmove", AssetIDType.Boolean);
                cv_arr[2].Add("charcantarget", AssetIDType.Boolean);
                cv_arr[2].Add("chardead", AssetIDType.Boolean);
                cv_arr[2].Add("targeted", AssetIDType.Boolean);
                cv_arr[2].Add("havechar", AssetIDType.Boolean);
                cv_arr[2].Add("noreturn", AssetIDType.Boolean);
                cv_arr[2].Add("timeron", AssetIDType.Boolean);
                cv_arr[2].Add("flash", AssetIDType.Boolean);
                cv_arr[2].Add("mercydraw", AssetIDType.Boolean);
                cv_arr[2].Add("tireddraw", AssetIDType.Boolean);
                cv_arr[2].Add("pacify_glow", AssetIDType.Boolean);
                cv_arr[2].Add("drawsus", AssetIDType.Boolean);
                cv_arr[2].Add("drawral", AssetIDType.Boolean);
                cv_arr[2].Add("susblend", AssetIDType.Color);
                cv_arr[2].Add("ralblend", AssetIDType.Color);
                cv_arr[2].Add("hurt", AssetIDType.Boolean);
                cv_arr[2].Add("skipme", AssetIDType.Boolean);
                cv_arr[2].Add("darken", AssetIDType.Boolean);
                cv_arr[2].Add("combatdarken", AssetIDType.Boolean);
                cv_arr[2].Add("stepped", AssetIDType.Boolean);
                //warned being a bool is probably mostly correct.
                cv_arr[2].Add("warned", AssetIDType.Boolean);
                cv_arr[2].Add("tired", AssetIDType.Boolean);
                cv_arr[2].Add("fixed", AssetIDType.Boolean);
                cv_arr[2].Add("nexttry", AssetIDType.Boolean);
                cv_arr[2].Add("floating", AssetIDType.Boolean);
                cv_arr[2].Add("bodyfade", AssetIDType.Boolean);
                cv_arr[2].Add("selected", AssetIDType.Boolean);
                cv_arr[2].Add("hurk", AssetIDType.Boolean);
                cv_arr[2].Add("persistent", AssetIDType.Boolean);
                cv_arr[2].Add("dhaver", AssetIDType.Boolean);
                cv_arr[2].Add("walk", AssetIDType.Boolean);
                cv_arr[2].Add("fun", AssetIDType.Boolean);
                cv_arr[2].Add("runmove", AssetIDType.Boolean);
                cv_arr[2].Add("frozen", AssetIDType.Boolean);
                cv_arr[2].Add("hadfrozen", AssetIDType.Boolean);
                cv_arr[2].Add("offscreen_frozen", AssetIDType.Boolean);
                cv_arr[2].Add("ignoresolid", AssetIDType.Boolean);
                cv_arr[2].Add("eraser", AssetIDType.Boolean);
                cv_arr[2].Add("visible", AssetIDType.Boolean);
                cv_arr[2].Add("bikeflip", AssetIDType.Boolean);
                cv_arr[2].Add("checked", AssetIDType.Boolean);
                cv_arr[2].Add("secondtime", AssetIDType.Boolean);
                cv_arr[2].Add("ralsei_lecture", AssetIDType.Boolean);
                cv_arr[2].Add("choiced", AssetIDType.Boolean);
                cv_arr[2].Add("FINISH", AssetIDType.Boolean);
                cv_arr[2].Add("LOCK", AssetIDType.Boolean);
                cv_arr[2].Add("locked", AssetIDType.Boolean);
                cv_arr[2].Add("ERASE", AssetIDType.Boolean);
                cv_arr[2].Add("fastmode", AssetIDType.Boolean);
                cv_arr[2].Add("fadeplease", AssetIDType.Boolean);
                cv_arr[2].Add("active", AssetIDType.Boolean);
                cv_arr[2].Add("alpha_changed", AssetIDType.Boolean);
                cv_arr[2].Add("charinstance", AssetIDType.GameObject);
                cv_arr[2].Add("reset", AssetIDType.Boolean);
                //globals pertaining to monsters in Deltarune 
                cv_arr[2].Add("monsterstatus", AssetIDType.Boolean);
                cv_arr[2].Add("monster", AssetIDType.Boolean);
                //Cutscene
                cv_arr[2].Add("cutscene", AssetIDType.Boolean);
                cv_arr[2].Add("black", AssetIDType.Boolean);
                cv_arr[2].Add("monsterinstancetype", AssetIDType.GameObject);
                //cv_arr[2].Add("itemed", AssetIDType.Boolean);
                //cv_arr[2].Add("itemed", AssetIDType.Boolean);
                //cv_arr[2].Add("itemed", AssetIDType.Boolean);
                //cv_arr[2].Add("itemed", AssetIDType.Boolean);
                //Undertale
                cv_arr[2].Add("background_color", AssetIDType.Color);
                cv_arr[2].Add("myblend", AssetIDType.Color);
                cv_arr[2].Add("object0", AssetIDType.GameObject);
                cv_arr[2].Add("part1", AssetIDType.GameObject);
                cv_arr[2].Add("pap", AssetIDType.GameObject);
                cv_arr[2].Add("fileerased", AssetIDType.Sprite);
                cv_arr[2].Add("catty", AssetIDType.GameObject);
                cv_arr[2].Add("bratty", AssetIDType.GameObject);
                cv_arr[2].Add("creator", AssetIDType.GameObject);
                //It's not 100% accurate to resolve this way but it seems like this variable only gets directly assigned values and is used as a bool, it should be fine.
                cv_arr[2].Add("parent", AssetIDType.GameObject);
                //These are not so consistent... ;-;
                //op is used in Muffet's stuff but is critical in Omega flowey positioning... worse to resolve than to not.
                //cv_arr[2].Add("op", AssetIDType.GameObject);
                //Toby messed up in "gml_Object_obj_wizardorb_chaser_Alarm_0" (should be "scr_monstersum()"), "pop" is never a script.
                //From v1.001 Undertale via comparison
                //A TIER quality
                cv_arr[2].Add("onionsprite", AssetIDType.Sprite);
                cv_arr[2].Add("headsprite", AssetIDType.Sprite);
                cv_arr[2].Add("breaksprite", AssetIDType.Sprite);
                cv_arr[2].Add("foodimg", AssetIDType.Sprite);
                cv_arr[2].Add("facespr", AssetIDType.Sprite);
                cv_arr[2].Add("bombsprite", AssetIDType.Sprite);
                cv_arr[2].Add("mysprite", AssetIDType.Sprite);
                cv_arr[2].Add("arms", AssetIDType.Sprite);
                cv_arr[2].Add("levelpic", AssetIDType.Sprite);
                cv_arr[2].Add("image", AssetIDType.Sprite);
                cv_arr[2].Add("song_index", AssetIDType.Sound);
                cv_arr[2].Add("thischara", AssetIDType.GameObject);
                //B TIER quality
                cv_arr[2].Add("tspr5", AssetIDType.Sprite);
                cv_arr[2].Add("tspr3", AssetIDType.Sprite);
                cv_arr[2].Add("tspr2", AssetIDType.Sprite);
                cv_arr[2].Add("tspr1", AssetIDType.Sprite);
                cv_arr[2].Add("tspr4", AssetIDType.Sprite);
                cv_arr[2].Add("snapper", AssetIDType.GameObject);
                cv_arr[2].Add("subject", AssetIDType.GameObject);
                cv_arr[2].Add("clip", AssetIDType.GameObject);
                //C TIER quality
                cv_arr[2].Add("sound1", AssetIDType.Sound);
                cv_arr[2].Add("sound2", AssetIDType.Sound);

                cv_conditions[2] = "undertale|survey_program|&deltarune";


                // Parse condition
                for (int i = 0; i < cv_conditions.Length; i++)
                {
                    string[] conditions = cv_conditions[i].Split("|".ToCharArray());
                    for (int j = 0; j < conditions.Length; j++)
                    {
                        bool checktype = false;
                        string condition = conditions[i];
                        if (condition.Contains("&"))
                        {
                            condition = condition.Remove(0, 1);
                            checktype = true;
                        }

                        if (((checktype) && lowerName.Contains(condition)) || ((!checktype) && (lowerName == condition)))
                        {
                            foreach (KeyValuePair<string, AssetIDType> custom_var in cv_arr[i])
                                builtin_vars.Add(custom_var.Key, custom_var.Value);

                            foreach (KeyValuePair<string, AssetIDType[]> custom_func in cf_arr[i])
                                builtin_funcs.Add(custom_func.Key, custom_func.Value);

                            break;
                        }
                    }
                }

                // Try to generate an XML file from custom_ dictionaries...
                GenerateAssetData(cv_arr, cf_arr, lowerName, cv_conditions);

                // Log that we couldn't write or load external XML...
                ATRLog("WARNING: Internal data is used because XML wasn't written or your XML had an error, check lines above.");
            }
        }

        public static void GenerateAssetData(Dictionary<string, AssetIDType>[] vars, Dictionary<string, AssetIDType[]>[] funcs, string gname, string[] conds)
        {
            XDocument AssetXML = new XDocument(new XComment(" This file was autogenerated by GenerateAssetData() "), new XElement("Games"));

            XElement pointerToGames = AssetXML.Element("Games");

            for (int i = 0; i < conds.Length; i++)
            {
                string cur_cond = conds[i];
                XElement _game = new XElement("Game");
                if (!cur_cond.Contains("|")) // only one game
                {
                    _game.Add(new XAttribute("gname", cur_cond));
                }
                else //multiple games
                {
                    string[] parsedcont = conds[i].Split("|".ToCharArray());
                    for (int c = 0; c < parsedcont.Length; c++)
                    {
                        _game.Add(new XAttribute("gname" + (c + 1).ToString(), parsedcont[c]));
                    }
                }

                foreach (var cur_var in vars[i])
                {
                    _game.Add(new XElement("Resolve", new XAttribute("type", "variable"), new XAttribute("name", cur_var.Key), cur_var.Value.ToString("g").Replace("Enum_", "")));
                }

                foreach (var cur_func in funcs[i])
                {
                    string temp = "";
                    for (int f = 0; f < cur_func.Value.Length; f++)
                    {
                        temp += cur_func.Value[f].ToString("g").Replace("Enum_", "") + ((f != cur_func.Value.Length - 1) ? ", " : "");
                    }
                    _game.Add(new XElement("Resolve", new XAttribute("type", "function"), new XAttribute("name", cur_func.Key), temp));
                }
                pointerToGames.Add(_game);

            }

            string AXString = AssetXML.ToString();

            try
            {
                File.WriteAllText(programDir + "AssetTypeResolverProfile.xml", AXString);
            }
            catch { } // silently fail if can't write XML, data is loaded into custom_ dictionaries anyway...
        }

        public static void LoadAssetDataFromXML(string lowerName)
        {
            // XML loading
            XmlDocument xml = new XmlDocument();
            try { xml.Load(programDir + "AssetTypeResolverProfile.xml"); }
            catch { throw new ArgumentException("Invalid XML!"); } // if XML is not valid, stop.

            XmlNodeList xnList = xml.SelectNodes("/Games/Game");
            bool ourgame = false; // if lowerName matches the name in our Node.
            foreach (XmlNode xn in xnList)
            {
                bool checktype = false; // false - direct compare, true - string.StartsWith()

                // "if this node has stuff for our game" stuff...
                string gname = xn.Attributes["gname"] != null ? xn.Attributes["gname"].FirstChild.InnerText : "";
                checktype = gname.StartsWith("&");
                if (checktype) gname = gname.Remove(0, 1);
                if (gname == "") // gname doesn't exist, read gname1,gname2
                {
                    for (int i = 1; i < 10; i++) // loop from game1 to game9
                    {
                        string _n = (xn.Attributes["gname" + i.ToString()] != null) ? xn.Attributes["gname" + i.ToString()].FirstChild.InnerText : "";
                        if (i == 1 && _n == "") throw new ArgumentException("Could not find gname attribute in Game node!"); // gname doesn't exist (it's OK), but gname1 also doesn't exist (that's bad).
                        checktype = _n.StartsWith("&");
                        if (checktype) _n = _n.Remove(0, 1);
                        if (_n == "") break;
                        else if ((!checktype && _n == lowerName) || (checktype && lowerName.StartsWith(_n)))
                        {
                            ourgame = true;
                            break;
                        }
                    }
                }
                else if ((!checktype && (gname == lowerName)) || (checktype && lowerName.StartsWith(gname))) ourgame = true;

                // apply stuff from this node if ourgame is True.
                if (ourgame)
                {
                    foreach (var el in xn)
                    {
                        // Cannot convert XML Comment into an XML Element fix >:(
                        XmlElement gel;
                        if (!el.GetType().Equals(typeof(XmlElement))) continue; // a comment (or something else?), skip the rest...
                        else gel = (XmlElement)el;

                        string type = gel.Attributes["type"].FirstChild.InnerText;
                        string name = gel.Attributes["name"].FirstChild.InnerText;
                        string[] assettypes = gel.InnerText.Replace(" ", "").Split(",".ToCharArray());
                        if ((type == "function") || (type == "script")) // i'm kind, i allow both
                        {
                            AssetIDType[] scrtypes = new AssetIDType[assettypes.Length];
                            for (int i = 0; i < assettypes.Length; i++)
                            {
                                scrtypes[i] = StringToAssetIDRef(assettypes[i]);
                            }
                            custom_funcs[name] = scrtypes; // custom_funcs["scr_bork"] = AssetIDType[] { scrtypes[i] /* parsed by StringToAssetIDRef */ };
                        }
                        else if (type == "variable") // variable is simpler, only one Asset type.
                        {
                            AssetIDType parsedtype = StringToAssetIDRef(assettypes[0]);
                            custom_vars.Add(name, parsedtype);
                        }
                        else
                        {
                            ATRLog("Could not parse type attribute! Your item's name is: " + name + " and it's type is: " + type); // type="alasbdassda" ??
                        }
                    }
                }
            }
        }
    }
}
