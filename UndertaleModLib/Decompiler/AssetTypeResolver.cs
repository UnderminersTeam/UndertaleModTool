using System;
using System.Collections.Generic;
using System.Globalization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public enum AssetIDType
    {
        Other = 0,
        Color,
        KeyboardKey,
        MouseButton,
        Enum_MouseCursor,
        Enum_HAlign,
        Enum_VAlign,
        Enum_GameSpeed, // GMS2 only
        Enum_OSType,
        Enum_GamepadButton,
        Enum_PathEndAction,
        Enum_BufferKind,
        Enum_BufferType,
        Enum_BufferSeek,
        Enum_Steam_UGC_FileType,
        Enum_Steam_UGC_List,
        Enum_Steam_UGC_QueryType,
        Enum_Steam_UGC_MatchType,
        Enum_Steam_UGC_SortOrder,
        Enum_Steam_LeaderBoard_Sort,
        Enum_Steam_LeaderBoard_Display,
        Enum_Steam_Overlay,
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
        AnimCurve,
        Sequence,
        ParticleSystem,

        EventType, // For event_perform

        Macro, // Bytecode <= 14 has macros in the Constants field of OPTN, only useful for Undertale 1.00
        ContextDependent, // Can be anything, depends on the function and/or other arguments

        TileSet, // Identical to AssetIDType.Background, used internally for GMS2 to prevent tileset functions from resolving incorrectly.
        Layer // GMS2
    }

    public enum HAlign
    {
        fa_left = 0,
        fa_center = 1,
        fa_right = 2
    }

    public enum VAlign
    {
        fa_top = 0,
        fa_middle = 1,
        fa_bottom = 2
    }

    public enum GameSpeed
    {
        gamespeed_fps,
        gamespeed_microseconds
    }

    public enum MouseCursor
    {
        cr_size_all = -22,
        cr_handpoint = -21,
        cr_appstart = -19, // I have no idea why they aren't aligned.
        cr_drag = -12,
        cr_hourglass = -11,
        cr_uparrow = -10,
        cr_size_we = -9,
        cr_size_nwse = -8,
        cr_size_ns = -7,
        cr_size_nesw = -6,
        cr_beam = -4,
        cr_cross = -3,
        cr_arrow = -2,
        cr_none = -1,
        cr_default = 0
    }

    public enum OSType
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

    public enum GamepadButton
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

    public enum PathEndAction
    {
        path_action_stop = 0,
        path_action_restart = 1,
        path_action_continue = 2,
        path_action_reverse = 3
    }

    public enum BufferKind
    {
        buffer_fixed = 0,
        buffer_grow = 1,
        buffer_wrap = 2,
        buffer_fast = 3,
        buffer_vbuffer = 4,
        buffer_network = 5
    }

    public enum BufferType
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

    public enum BufferSeek
    {
        buffer_seek_start = 0,
        buffer_seek_relative = 1,
        buffer_seek_end = 2
    }

    public enum MouseButton
    {
        mb_any = -1,
        mb_none,
        mb_left,
        mb_right,
        mb_middle
    }

    public enum e__VW
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
    }
    public enum e__BG
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
    }

    public enum Boolean
    {
        @false = 0,
        @true = 1
    }

    public enum Steam_UGC_FileType
    {
        ugc_filetype_community,
        ugc_filetype_microtrans
    }

    public enum Steam_UGC_MatchType
    {
        ugc_match_Items,
        ugc_match_Items_Mtx,
        ugc_match_Items_ReadyToUse,
        ugc_match_Collections,
        ugc_match_Artwork,
        ugc_match_Videos,
        ugc_match_Screenshots,
        ugc_match_AllGuides,
        ugc_match_WebGuides,
        ugc_match_IntegratedGuides,
        ugc_match_UsableInGame,
        ugc_match_ControllerBindings,
    }

    public enum Steam_UGC_QueryType
    {
        ugc_query_RankedByVote,
        ugc_query_RankedByPublicationDate,
        ugc_query_AcceptedForGameRankedByAcceptanceDate,
        ugc_query_RankedByTrend,
        ugc_query_FavoritedByFriendsRankedByPublicationDate,
        ugc_query_CreatedByFriendsRankedByPublicationDate,
        ugc_query_RankedByNumTimesReported,
        ugc_query_CreatedByFollowedUsersRankedByPublicationDate,
        ugc_query_NotYetRated,
        ugc_query_RankedByTotalVotesAsc,
        ugc_query_RankedByVotesUp,
        ugc_query_RankedByTextSearch,
    }

    public enum Steam_UGC_List
    {
        ugc_list_Published,
        ugc_list_VotedOn,
        ugc_list_VotedUp,
        ugc_list_VotedDown,
        ugc_list_WillVoteLater,
        ugc_list_Favorited,
        ugc_list_Subscribed,
        ugc_list_UsedOrPlayed,
        ugc_list_Followed
    }

    public enum Steam_UGC_SortOrder
    {
        ugc_sortorder_CreationOrderDesc,
        ugc_sortorder_CreationOrderAsc,
        ugc_sortorder_TitleAsc,
        ugc_sortorder_LastUpdatedDesc,
        ugc_sortorder_SubscriptionDateDesc,
        ugc_sortorder_VoteScoreDesc,
        ugc_sortorder_ForModeration
    }

    public enum Steam_LeaderBoard_Sort
    {
        lb_sort_none,
        lb_sort_ascending,
        lb_sort_descending,
    }

    public enum Steam_LeaderBoard_Display
    {
        lb_disp_none,
        lb_disp_numeric,
        lb_disp_time_sec,
        lb_disp_time_ms,
    }

    public enum Steam_Overlay
    {
        ov_friends,
        ov_community,
        ov_players,
        ov_settings,
        ov_gamegroup,
        ov_achievements,
    }

    // Subtypes are pulled from the builtin list case frankly I
    // don't care enough to type them all out manually.
    // There's like a hundred subtypes.
    public enum Enum_EventType
    {
        ev_create,
        ev_destroy,
        ev_alarm,
        ev_step,
        ev_collision,
        ev_keyboard,
        ev_mouse,
        ev_other,
        ev_draw,
        ev_keypress,
        ev_keyrelease,
        ev_trigger,

        // GMS2
        ev_cleanup,
        ev_gesture,
        ev_pre_create,
    }

    public class AssetTypeResolver
    {
        public static Dictionary<string, AssetIDType[]> builtin_funcs; // keys are function names

        public static Dictionary<string, Dictionary<string, AssetIDType>> builtin_var_overrides; // keys are code block names or object names. In the resulting dictionary keys are variable names.
        public static Dictionary<string, AssetIDType> builtin_vars; // keys are variable names

        public static Dictionary<string, AssetIDType> return_types; // keys are script names (< GMS2.3) or member function variable names (>= GMS2.3)

        internal static bool AnnotateTypesForFunctionCall(string function_name, AssetIDType[] arguments, DecompileContext context)
        {
            return AnnotateTypesForFunctionCall(function_name, arguments, context, null);
        }

        internal static bool AnnotateTypesForFunctionCall(string function_name, AssetIDType[] arguments, DecompileContext context, Decompiler.FunctionCall function)
        {
            Dictionary<string, AssetIDType[]> scriptArgs = context.GlobalContext.ScriptArgsCache;

            bool overloaded = false;
            // Scripts overload builtins because in GMS2 some functions are just backwards-compatibility scripts
            if (scriptArgs.ContainsKey(function_name) && scriptArgs[function_name] != null)
            {
                overloaded = true;
                for (int i = 0; i < arguments.Length && i < scriptArgs[function_name].Length; i++)
                    arguments[i] = scriptArgs[function_name][i];
            }

            function_name = function_name.Replace("color", "colour", StringComparison.InvariantCulture); // Just GameMaker things... both are valid :o

            if(context.GlobalContext.Data?.IsGameMaker2() ?? false)
            {
                // Backgrounds don't exist in GMS2
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i] == AssetIDType.Background)
                        arguments[i] = AssetIDType.Sprite;
                }
            }

            if (builtin_funcs.ContainsKey(function_name))
            {
                AssetIDType[] func_types = builtin_funcs[function_name];

                if (context.GlobalContext.Data?.IsGameMaker2() ?? false)
                {
                    // Backgrounds don't exist in GMS2
                    for (int i = 0; i < func_types.Length; i++)
                    {
                        if (func_types[i] == AssetIDType.Background)
                            func_types[i] = AssetIDType.Sprite;
                    }
                }

                if (overloaded)
                {
                    // Copy the array to make sure we don't overwrite existing known types
                    func_types = (AssetIDType[]) func_types.Clone();
                    AssetIDType scriptArgType;

                    for (int i = 0; i < arguments.Length && i < func_types.Length && i < scriptArgs[function_name].Length; i++)
                    {
                        scriptArgType = scriptArgs[function_name][i];

                        // Merge types together
                        if (func_types[i] == AssetIDType.Other && scriptArgType != AssetIDType.Other)
                            func_types[i] = scriptArgType;
                        // Conflicting types - do not resolve
                        else if (func_types[i] != AssetIDType.Other && scriptArgType != AssetIDType.Other && func_types[i] != scriptArgType)
                            func_types[i] = AssetIDType.Other;
                        // func_types[i] is correct, do not replace
                    }
                }
                for (int i = 0; i < arguments.Length && i < func_types.Length; i++)
                    arguments[i] = func_types[i];
                return true;
            }
            if (function_name == "script_execute")
            {
                // This needs a special case
                if (arguments.Length < 1)
                    throw new Exception("Bad call to " + function_name + " with " + arguments.Length + " arguments (instead of at least 1)");
                arguments[0] = AssetIDType.Script;

                // Attempt to resolve the arguments of the script being called.
                // This is done by reading the literal values passed to the function and resolving
                // the first argument as a function, then recursively calling the asset resolver on it.
                // There's probably a better way to do this.

                if (function != null)
                {
                    if (function.Arguments[0] is Decompiler.ExpressionCast)
                    {
                        var firstArg = (function.Arguments[0] as Decompiler.ExpressionCast).Argument;
                        if ((firstArg is Decompiler.ExpressionConstant) && firstArg.Type == UndertaleInstruction.DataType.Int16)
                        {
                            short script_id = (short) (firstArg as Decompiler.ExpressionConstant).Value;
                            if (script_id >= 0 && script_id < context.GlobalContext.Data.Scripts.Count)
                            {
                                var script = context.GlobalContext.Data.Scripts[script_id];
                                AssetIDType[] args = new AssetIDType[arguments.Length-1];
                                AnnotateTypesForFunctionCall(script.Name.Content, args, context);
                                Array.Copy(args, 0, arguments, 1, args.Length);
                                return true;
                            }
                        }
                    }
                }
                if (scriptArgs.ContainsKey(function_name) && scriptArgs[function_name] != null)
                {
                    for (int i = 0; i < arguments.Length && i < scriptArgs[function_name].Length; i++)
                        arguments[1 + i] = scriptArgs[function_name][i];
                }
                return true;
            }
            return overloaded;
        }

        internal static AssetIDType AnnotateTypeForVariable(DecompileContext context, string variable_name)
        {
            var overrides = GetTypeOverridesFor(context.TargetCode.Name.Content);
            if (overrides.ContainsKey(variable_name))
                return overrides[variable_name];

            if (context.Object != null)
            {
                overrides = GetTypeOverridesFor(context.Object.Name.Content);
                if (overrides.ContainsKey(variable_name))
                    return overrides[variable_name];
            }


            if (builtin_vars.ContainsKey(variable_name))
                return builtin_vars[variable_name];
            return AssetIDType.Other;
        }

        internal static AssetIDType AnnotateTypeForScript(string script_name)
        {
            if (return_types.ContainsKey(script_name))
                return return_types[script_name];
            return AssetIDType.Other;
        }

        internal static Dictionary<string, AssetIDType> GetTypeOverridesFor(DecompileContext context)
        {
            return GetTypeOverridesFor(context.TargetCode.Name.Content);
        }

        internal static Dictionary<string, AssetIDType> GetTypeOverridesFor(string code_entry_name)
        {
            lock(builtin_var_overrides)
            {
                if (!builtin_var_overrides.ContainsKey(code_entry_name))
                    builtin_var_overrides.Add(code_entry_name, new Dictionary<string, AssetIDType>());

                return builtin_var_overrides[code_entry_name];
            }
        }

        internal static void AddOverrideFor(string code_entry_name, string variable_name, AssetIDType type)
        {
            var overrides = GetTypeOverridesFor(code_entry_name);
            overrides[variable_name] = type;
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
            if (Enum.IsDefined(typeof(MouseButton), const_name))
                return (int)Enum.Parse(typeof(MouseButton), const_name);
            if (Enum.IsDefined(typeof(MouseCursor), const_name))
                return (int)Enum.Parse(typeof(MouseCursor), const_name);
            if (Enum.IsDefined(typeof(HAlign), const_name))
                return (int)Enum.Parse(typeof(HAlign), const_name);
            if (Enum.IsDefined(typeof(VAlign), const_name))
                return (int)Enum.Parse(typeof(VAlign), const_name);
            if (Enum.IsDefined(typeof(GameSpeed), const_name))
                return (int)Enum.Parse(typeof(GameSpeed), const_name);
            if (Enum.IsDefined(typeof(e__VW), const_name))
                return (int)Enum.Parse(typeof(e__VW), const_name);
            if (Enum.IsDefined(typeof(e__BG), const_name))
                return (int)Enum.Parse(typeof(e__BG), const_name);
            if (Enum.IsDefined(typeof(EventSubtypeKey), const_name))
                return Convert.ToInt32((uint)Enum.Parse(typeof(EventSubtypeKey), const_name));
            if (Enum.IsDefined(typeof(Enum_EventType), const_name))
                return (int)Enum.Parse(typeof(Enum_EventType), const_name);

            return null;
        }

        // Properly initializes per-project/game
        public static void InitializeTypes(UndertaleData data)
        {

            ContextualAssetResolver.Initialize(data);

            return_types = new Dictionary<string, AssetIDType>();

            builtin_funcs = new Dictionary<string, AssetIDType[]>
            {
                { "action_create_object", new[] { AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "instance_activate_object", new[] { AssetIDType.GameObject } },
                { "script_exists", new[] { AssetIDType.Script } },
                { "script_get_name", new[] { AssetIDType.Script } },
                // script_execute handled separately

                { "instance_change", new[] { AssetIDType.GameObject, AssetIDType.Boolean } },
                { "instance_copy", new[] { AssetIDType.Boolean } },
                { "instance_create", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_destroy", new[] { AssetIDType.GameObject, AssetIDType.Boolean } },
                { "instance_exists", new[] { AssetIDType.GameObject } },
                { "instance_find", new[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "instance_furthest", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_nearest", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_number", new[] { AssetIDType.GameObject } },
                { "instance_place", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_position", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "instance_deactivate_all", new[] { AssetIDType.Boolean } },
                { "application_surface_enable", new[] { AssetIDType.Boolean } },
                { "application_surface_draw_enable", new[] { AssetIDType.Boolean } },
                { "instance_deactivate_object", new[] { AssetIDType.GameObject } },
                { "instance_activate_region", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean } },
                { "instance_deactivate_region", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean , AssetIDType.Boolean } },

                { "instance_activate_layer", new[] { AssetIDType.Layer } }, // GMS2
                { "instance_deactivate_layer", new[] { AssetIDType.Layer } }, // GMS2
                { "instance_create_depth", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } }, // GMS2
                { "instance_create_layer", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } }, // GMS2

                { "sprite_get_name", new[] { AssetIDType.Sprite } },
                { "sprite_get_number", new[] { AssetIDType.Sprite } },
                { "sprite_get_width", new[] { AssetIDType.Sprite } },
                { "sprite_get_height", new[] { AssetIDType.Sprite } },
                { "sprite_get_xoffset", new[] { AssetIDType.Sprite } },
                { "sprite_get_yoffset", new[] { AssetIDType.Sprite } },
                { "sprite_get_bbox_bottom", new[] { AssetIDType.Sprite } },
                { "sprite_get_bbox_left", new[] { AssetIDType.Sprite } },
                { "sprite_get_bbox_right", new[] { AssetIDType.Sprite } },
                { "sprite_get_bbox_top", new[] { AssetIDType.Sprite } },
                { "sprite_get_tpe", new[] { AssetIDType.Sprite, AssetIDType.Other } },
                { "sprite_get_texture", new[] { AssetIDType.Sprite, AssetIDType.Other } },
                { "sprite_get_uvs", new[] { AssetIDType.Sprite, AssetIDType.Other } },

                { "sprite_exists", new[] { AssetIDType.Sprite } },
                { "sprite_add", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_replace", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_duplicate", new[] { AssetIDType.Sprite } },
                { "sprite_assign", new[] { AssetIDType.Sprite, AssetIDType.Sprite } },
                { "sprite_merge", new[] { AssetIDType.Sprite, AssetIDType.Sprite } },
                { "sprite_create_from_surface", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_add_from_surface", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean } },
                { "sprite_collision_mask", new[] { AssetIDType.Sprite, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_set_offset", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_delete", new[] { AssetIDType.Sprite } },
                { "sprite_set_alpha_from_sprite", new[] { AssetIDType.Sprite, AssetIDType.Sprite } },
                { "sprite_set_cache_size", new[] { AssetIDType.Sprite, AssetIDType.Other } },
                { "sprite_set_cache_size_ext", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_save", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other } },
                { "sprite_save_strip", new[] { AssetIDType.Sprite, AssetIDType.Other } },
                { "sprite_flush", new[] { AssetIDType.Sprite } },
                { "sprite_flush_multi", new[] { AssetIDType.Sprite } }, // sprite ARRAY
                { "sprite_prefetch", new[] { AssetIDType.Sprite } },
                { "sprite_prefetch_multi", new[] { AssetIDType.Sprite } }, // sprite ARRAY

                { "background_get_name", new[] { AssetIDType.Background } },
                { "background_get_width", new[] { AssetIDType.Background } },
                { "background_get_height", new[] { AssetIDType.Background } },
                { "background_get_texture", new[] { AssetIDType.Background } },
                { "background_get_uvs", new[] { AssetIDType.Background } },
                { "background_exists", new[] { AssetIDType.Background } },
                { "background_add", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "background_replace", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "background_duplicate", new[] { AssetIDType.Background } },
                { "background_assign", new[] { AssetIDType.Background, AssetIDType.Background } },
                { "background_create_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color } },
                { "background_create_gradient", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "background_create_from_surface", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "background_set_alpha_from_background", new[] { AssetIDType.Background, AssetIDType.Background } },
                { "background_save", new[] { AssetIDType.Background, AssetIDType.Other } },
                { "background_delete", new[] { AssetIDType.Background } },
                { "background_flush", new[] { AssetIDType.Background } },
                { "background_flush_multi", new[] { AssetIDType.Background } }, // array
                { "background_prefetch", new[] { AssetIDType.Background } },
                { "background_prefetch_multi", new[] { AssetIDType.Background } }, // array

                // only a few relevant ones for tiles
                { "tile_add", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "tile_set_background", new[] { AssetIDType.Other, AssetIDType.Background } },
                { "tile_set_blend", new[] { AssetIDType.Other, AssetIDType.Color } },

                { "audio_exists", new[] { AssetIDType.Sound } },
                { "audio_get_name", new[] { AssetIDType.Sound } },
                { "audio_get_type", new[] { AssetIDType.Sound } },
                { "audio_play_sound", new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Boolean } },
                { "audio_play_sound_at", new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other } },
                { "audio_pause_sound", new[] { AssetIDType.Sound } },
                { "audio_pause_all", Array.Empty<AssetIDType>() },
                { "audio_resume_sound", new[] { AssetIDType.Sound } },
                { "audio_resume_all", Array.Empty<AssetIDType>() },
                { "audio_stop_sound", new[] { AssetIDType.Sound } },
                { "audio_stop_all", Array.Empty<AssetIDType>() },
                { "audio_is_playing", new[] { AssetIDType.Sound } },
                { "audio_is_paused", new[] { AssetIDType.Sound } },
                { "audio_create_streaam", new[] { AssetIDType.Other } },
                { "audio_destroy_streaam", new[] { AssetIDType.Other } },

                { "audio_sound_set_track_position", new[] { AssetIDType.Sound, AssetIDType.Other } },
                { "audio_sound_get_track_position", new[] { AssetIDType.Sound } },
                { "audio_sound_length", new[] { AssetIDType.Sound } },
                { "audio_sound_pitch", new[] { AssetIDType.Sound, AssetIDType.Other } },
                { "audio_sound_get_pitch", new[] { AssetIDType.Sound } },
                { "audio_falloff_set_model", new[] { AssetIDType.Other } },
                { "audio_sound_gain", new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other } },
                { "audio_sound_get_gain", new[] { AssetIDType.Sound } },
                { "audio_master_gain", new[] { AssetIDType.Other } },
                { "audio_play_sound_on", new[] { AssetIDType.Other, AssetIDType.Sound, AssetIDType.Boolean, AssetIDType.Other } },
                { "audio_play_in_sync_group", new[] { AssetIDType.Other, AssetIDType.Sound } },
                // TODO? I don't know if the ones with only asset type Other are worth adding here

                // Legacy sound functions
                { "sound_exists", new[] { AssetIDType.Sound } },
                { "sound_get_name", new[] { AssetIDType.Sound } },
                { "sound_play", new[] { AssetIDType.Sound } },
                { "sound_loop", new[] { AssetIDType.Sound } },
                { "sound_stop", new[] { AssetIDType.Sound } },
                { "sound_stop_all", Array.Empty<AssetIDType>() },
                { "sound_isplaying", new[] { AssetIDType.Sound } },
                { "sound_volume", new[] { AssetIDType.Sound, AssetIDType.Other } },
                { "sound_fade", new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other } },
                { "sound_global_volume", new[] { AssetIDType.Other } },
                // Deprecated legacy functions (wait what)
                { "sound_add", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "sound_replace", new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "sound_delete", new[] { AssetIDType.Sound } },

                { "font_get_name", new[] { AssetIDType.Font } },
                { "font_get_fontname", new[] { AssetIDType.Font } },
                { "font_get_first", new[] { AssetIDType.Font } },
                { "font_get_last", new[] { AssetIDType.Font } },
                { "font_get_italic", new[] { AssetIDType.Font } },
                { "font_get_bold", new[] { AssetIDType.Font } },
                { "font_get_size", new[] { AssetIDType.Font } },
                { "font_get_texture", new[] { AssetIDType.Font } },
                { "font_get_uvs", new[] { AssetIDType.Font } },

                { "font_set_cache_size", new[] { AssetIDType.Font, AssetIDType.Other } },
                { "font_exists", new[] { AssetIDType.Font } },
                { "font_add", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_add_sprite", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_add_sprite_ext", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_replace", new[] { AssetIDType.Font, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_replace_sprite", new[] { AssetIDType.Font, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_replace_sprite_ext", new[] { AssetIDType.Font, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "font_delete", new[] { AssetIDType.Font } },

                { "path_start", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Enum_PathEndAction, AssetIDType.Other } },
                { "path_end", Array.Empty<AssetIDType>() },

                { "path_exists", new[] { AssetIDType.Path } },
                { "path_get_closed", new[] { AssetIDType.Path } },
                { "path_get_kind", new[] { AssetIDType.Path } },
                { "path_get_length", new[] { AssetIDType.Path } },
                { "path_get_name", new[] { AssetIDType.Path } },
                { "path_get_number", new[] { AssetIDType.Path } },
                { "path_get_point_speed", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_point_x", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_point_y", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_precision", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_speed", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_x", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_get_y", new[] { AssetIDType.Path, AssetIDType.Other} },

                { "path_add", Array.Empty<AssetIDType>() },
                { "path_add_point", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "path_change_point", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "path_insert_point", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "path_delete_point", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_clear_points", new[] { AssetIDType.Path } },
                { "path_append", new[] { AssetIDType.Path, AssetIDType.Path } },
                { "path_assign", new[] { AssetIDType.Path, AssetIDType.Path } },
                { "path_delete", new[] { AssetIDType.Path } },
                { "path_duplicate", new[] { AssetIDType.Path } },
                { "path_flip", new[] { AssetIDType.Path } },
                { "path_mirror", new[] { AssetIDType.Path } },
                { "path_reverse", new[] { AssetIDType.Path } },
                { "path_rotate", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_scale", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other } },
                { "path_set_closed", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_set_kind", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_set_precision", new[] { AssetIDType.Path, AssetIDType.Other } },
                { "path_shift", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other } },

                { "timeline_exists", new[] { AssetIDType.Timeline } },
                { "timeline_get_name", new[] { AssetIDType.Timeline } },
                { "timeline_delete", new[] { AssetIDType.Timeline } },
                { "timeline_moment_add_script", new[] { AssetIDType.Timeline, AssetIDType.Other, AssetIDType.Other } },
                { "timeline_moment_clear", new[] { AssetIDType.Timeline, AssetIDType.Other } },
                { "timeline_clear", new[] { AssetIDType.Timeline } },
                { "timeline_size", new[] { AssetIDType.Timeline } },
                { "timeline_max_moment", new[] { AssetIDType.Timeline } },

                { "room_exists", new[] { AssetIDType.Room } },
                { "room_next", new[] { AssetIDType.Room } },
                { "room_previous", new[] { AssetIDType.Room } },
                { "room_get_name", new[] { AssetIDType.Room } },

                { "room_goto", new[] { AssetIDType.Room } },
                { "room_goto_next", Array.Empty<AssetIDType>() },
                { "room_goto_previous", Array.Empty<AssetIDType>() },
                { "room_restart", Array.Empty<AssetIDType>() },

                { "room_add", Array.Empty<AssetIDType>() },
                { "room_duplicate", new[] { AssetIDType.Room } },
                { "room_assign", new[] { AssetIDType.Room, AssetIDType.Room } },
                { "room_instance_add", new[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "room_instance_clear", new[] { AssetIDType.Room } },
                { "room_tile_add", new[] { AssetIDType.Room, AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "room_tile_add_ext", new[] { AssetIDType.Room, AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "room_tile_clear", new[] { AssetIDType.Room } },
                { "room_set_background", new[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "room_set_background_colour", new[] { AssetIDType.Room, AssetIDType.Color, AssetIDType.Other } },
                { "room_set_height", new[] { AssetIDType.Room, AssetIDType.Other } },
                { "room_set_width", new[] { AssetIDType.Room, AssetIDType.Other } },
                { "room_set_persistent", new[] { AssetIDType.Room, AssetIDType.Other } },
                { "room_set_view_enabled", new[] { AssetIDType.Room, AssetIDType.Other } },

                { "room_set_viewport", new[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } }, // GMS2 only
                { "room_get_viewport", new[] { AssetIDType.Room, AssetIDType.Other } }, // GMS2 only
                { "room_get_camera", new[] { AssetIDType.Room, AssetIDType.Other } }, // GMS2 only
                { "room_set_camera", new[] { AssetIDType.Room, AssetIDType.Other, AssetIDType.Other } }, // GMS2 only

                // GMS2 viewport compatibility scripts
                { "__view_get", new[] { AssetIDType.Other, AssetIDType.Other } }, // Don't ask why this is here I was going somewhere with this
                { "__view_set", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.ContextDependent } },

                { "object_exists", new[] { AssetIDType.GameObject } },
                { "object_get_depth", new[] { AssetIDType.GameObject } },
                { "object_get_mask", new[] { AssetIDType.GameObject } },
                { "object_get_name", new[] { AssetIDType.GameObject } },
                { "object_get_parent", new[] { AssetIDType.GameObject } },
                { "object_get_persistent", new[] { AssetIDType.GameObject } },
                { "object_get_solid", new[] { AssetIDType.GameObject } },
                { "object_get_sprite", new[] { AssetIDType.GameObject } },
                { "object_get_visible", new[] { AssetIDType.GameObject } },
                { "object_get_physics", new[] { AssetIDType.GameObject } },
                { "object_is_ancestor", new[] { AssetIDType.GameObject, AssetIDType.GameObject } },
                { "object_set_depth", new[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_mask", new[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_persistent", new[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_solid", new[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_sprite", new[] { AssetIDType.GameObject, AssetIDType.Other } },
                { "object_set_visible", new[] { AssetIDType.GameObject, AssetIDType.Other } },

                // Event functions
                { "event_perform_object", new[] { AssetIDType.GameObject, AssetIDType.EventType, AssetIDType.ContextDependent } },
                { "event_perform", new[] { AssetIDType.EventType, AssetIDType.ContextDependent } },

                { "merge_colour", new[] { AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },

                // incomplete
                { "draw_clear", new[] { AssetIDType.Color } },
                { "draw_clear_alpha", new[] { AssetIDType.Color, AssetIDType.Other } },
                { "draw_set_colour", new[] { AssetIDType.Color } },

                { "draw_circle_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_ellipse_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_line_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color } },
                { "draw_line_width_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color } },
                { "draw_point_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color } },
                { "draw_rectangle", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean } },
                { "draw_rectangle_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_roundrect_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_roundrect_colour_ext", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_healthbar", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_set_alpha", new[] { AssetIDType.Other } },

                { "draw_set_blend_mode", new[] { AssetIDType.ContextDependent } },
                { "draw_set_blend_mode_ext", new[] { AssetIDType.ContextDependent, AssetIDType.ContextDependent } },
                { "gpu_set_blendmode", new[] { AssetIDType.ContextDependent } },
                { "gpu_set_blendmode_ext", new[] { AssetIDType.ContextDependent, AssetIDType.ContextDependent } },

                { "d3d_set_fog", new[] { AssetIDType.Boolean, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other } },
                { "gpu_set_fog", new[] { AssetIDType.Boolean, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other } },

                { "layer_script_begin", new[] { AssetIDType.Other, AssetIDType.Script } },
                { "layer_background_create", new[] { AssetIDType.Other, AssetIDType.Sprite } },
                { "layer_background_blend", new[] { AssetIDType.Other, AssetIDType.Color } },
                { "layer_background_visible", new[] { AssetIDType.Other, AssetIDType.Boolean } },
                { "layer_sprite_change", new[] { AssetIDType.Other, AssetIDType.Sprite } },
                { "gpu_set_blendenable", new[] { AssetIDType.Boolean } },
                { "layer_script_end", new[] { AssetIDType.Other, AssetIDType.Script } },
                { "draw_sprite", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_ext", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_general", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_part", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_part_ext", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_stretched", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_stretched_ext", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_sprite_pos", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_tiled", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_sprite_tiled_ext", new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },

                { "draw_background", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_ext", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_part", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_part_ext", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_stretched", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_stretched_ext", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_tiled", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other } },
                { "draw_background_tiled_ext", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_background_general", new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },

                { "draw_set_font", new[] { AssetIDType.Font } },
                { "draw_set_halign", new[] { AssetIDType.Enum_HAlign } },
                { "draw_set_valign", new[] { AssetIDType.Enum_VAlign } },
                { "draw_text_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_text_ext_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_text_transformed_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },
                { "draw_text_transformed_ext_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Color, AssetIDType.Other } },

                { "draw_vertex_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },
                { "draw_vertex_texture_colour", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },

                { "distance_to_object", new[] { AssetIDType.GameObject } },

                { "place_meeting", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "position_meeting", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "position_change", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other } },
                { "collision_circle", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "collision_ellipse", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "collision_line", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Boolean, AssetIDType.Boolean } },
                { "collision_point", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },
                { "collision_rectangle", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other } },

                { "mp_linear_step", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_linear_step_object", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "mp_linear_path", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_linear_path_object", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "mp_potential_settings", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_potential_step", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_potential_step_object", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                { "mp_potential_path", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "mp_potential_path_object", new[] { AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject } },
                // mp_grid only relevant ones because I'm lazy
                { "mp_grid_path", new[] { AssetIDType.Other, AssetIDType.Path, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean } },
                { "mp_grid_add_instances", new[] { AssetIDType.Other, AssetIDType.GameObject, AssetIDType.Other } },

                // TODO: 3D drawing, didn't bother

                // TODO: surface drawing
                { "draw_surface_ext", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other } },

                { "shader_is_compiled", new[] { AssetIDType.Shader } },
                { "shader_set", new[] { AssetIDType.Shader } },
                { "shader_get_uniform", new[] { AssetIDType.Shader, AssetIDType.Other } },
                { "shader_get_sampler_index", new[] { AssetIDType.Shader, AssetIDType.Other } },
                { "shader_enable_corner_id", new[] { AssetIDType.Boolean } },

                // { "shader_current", new AssetIDType[] { } }, returns shader.

                // Interpolation
                { "texture_set_interpolation", new[] { AssetIDType.Boolean } },
                { "texture_set_tiled", new[] { AssetIDType.Boolean } },
                { "gpu_set_texfilter", new[] { AssetIDType.Boolean } }, // GMS2 equivalent of texture_set_interpolation.

                // TODO: GMS2 tilemaps
                { "tileset_get_texture", new[] { AssetIDType.TileSet } },
                { "tileset_get_uvs", new[] { AssetIDType.TileSet } },

                // TODO: GMS2 layers

                // GMS2 only equivalents of room_speed.
                { "game_get_speed", new[] { AssetIDType.Enum_GameSpeed } },
                { "game_set_speed", new[] { AssetIDType.Other, AssetIDType.Enum_GameSpeed } },

                // window_ functions
                { "window_set_cursor", new[] { AssetIDType.Enum_MouseCursor } },
                { "window_set_fullscreen", new[] { AssetIDType.Boolean } },
                { "window_set_color", new[] { AssetIDType.Color } },

                { "io_clear", Array.Empty<AssetIDType>() },
                { "keyboard_check", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_check_pressed", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_check_released", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_check_direct", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_clear", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_key_press", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_key_release", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_set_map", new[] { AssetIDType.KeyboardKey, AssetIDType.KeyboardKey } },
                { "keyboard_get_map", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_unset_map", new[] { AssetIDType.KeyboardKey } },
                { "keyboard_set_numlock", new[] { AssetIDType.Boolean } },
                { "keyboard_get_numlock", Array.Empty<AssetIDType>() },

                // Mouse functions
                { "mouse_check_button", new[] { AssetIDType.MouseButton } },
                { "mouse_check_button_pressed", new[] { AssetIDType.MouseButton } },
                { "mouse_check_button_released", new[] { AssetIDType.MouseButton } },
                { "mouse_clear", new[] { AssetIDType.MouseButton } },

                // Device Mouse functions
                { "device_mouse_check_button", new[] { AssetIDType.Other, AssetIDType.MouseButton } },
                { "device_mouse_check_button_pressed", new[] { AssetIDType.Other, AssetIDType.MouseButton } },
                { "device_mouse_check_button_released", new[] { AssetIDType.Other, AssetIDType.MouseButton } },
                { "device_mouse_dbclick_enable", new[] { AssetIDType.Boolean } },

                { "gamepad_button_value", new[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_button_check", new[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_button_check_pressed", new[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_button_check_released", new[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_axis_value", new[] { AssetIDType.Other, AssetIDType.Enum_GamepadButton } },
                { "gamepad_set_color", new[] { AssetIDType.Other, AssetIDType.Color } }, // PS4 only, DualShock pads have an LED panel.

                { "buffer_create", new[] { AssetIDType.Other, AssetIDType.Enum_BufferKind, AssetIDType.Other } },
                { "buffer_create_from_vertex_buffer", new[] { AssetIDType.Other, AssetIDType.Enum_BufferKind, AssetIDType.Other } },
                { "buffer_create_from_vertex_buffer_ext", new[] { AssetIDType.Other, AssetIDType.Enum_BufferKind, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "buffer_read", new[] { AssetIDType.Other, AssetIDType.Enum_BufferType } },
                { "buffer_write", new[] { AssetIDType.Other, AssetIDType.Enum_BufferType, AssetIDType.Other } },
                { "buffer_peek", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Enum_BufferType } },
                { "buffer_poke", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Enum_BufferType, AssetIDType.Other } },
                { "buffer_fill", new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Enum_BufferType, AssetIDType.Other, AssetIDType.Other } },
                { "buffer_sizeof", new[] { AssetIDType.Enum_BufferType } },
                { "buffer_seek", new[] { AssetIDType.Other, AssetIDType.Enum_BufferSeek, AssetIDType.Other } },

                // Steam functions
                { "steam_ugc_create_item", new[] { AssetIDType.Other, AssetIDType.Enum_Steam_UGC_FileType } },
                { "steam_ugc_create_query_user", new[] { AssetIDType.Enum_Steam_UGC_List, AssetIDType.Enum_Steam_UGC_MatchType, AssetIDType.Enum_Steam_UGC_SortOrder, AssetIDType.Other } },
                { "steam_ugc_create_query_user_ex", new[] { AssetIDType.Enum_Steam_UGC_List, AssetIDType.Enum_Steam_UGC_MatchType, AssetIDType.Enum_Steam_UGC_SortOrder, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },
                { "steam_ugc_create_query_all", new[] { AssetIDType.Enum_Steam_UGC_QueryType, AssetIDType.Enum_Steam_UGC_MatchType, AssetIDType.Other } },
                { "steam_ugc_create_query_all_ex", new[] { AssetIDType.Enum_Steam_UGC_QueryType, AssetIDType.Enum_Steam_UGC_MatchType, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other } },

                { "steam_ugc_query_set_cloud_filename_filter", new[] { AssetIDType.Other, AssetIDType.Boolean } },
                { "steam_ugc_query_set_match_any_tag", new[] { AssetIDType.Other, AssetIDType.Boolean } },
                { "steam_ugc_query_set_return_long_description", new[] { AssetIDType.Other, AssetIDType.Boolean } },
                { "steam_ugc_query_set_return_total_only", new[] { AssetIDType.Other, AssetIDType.Boolean } },
                { "steam_ugc_query_set_allow_cached_response", new[] { AssetIDType.Other, AssetIDType.Boolean } },

                { "steam_create_leaderboard", new[] { AssetIDType.Other, AssetIDType.Enum_Steam_LeaderBoard_Sort, AssetIDType.Enum_Steam_LeaderBoard_Display } },

                { "steam_activate_overlay", new[] { AssetIDType.Enum_Steam_Overlay } },

                // Also big TODO: Implement Boolean type for all these functions

                // Special internal functions
                { "@@GetInstance@@", new[] { AssetIDType.GameObject } },
            };

            builtin_var_overrides = new Dictionary<string, Dictionary<string, AssetIDType>>();

            if (data?.Code != null)
            {
                foreach (var code in data.Code)
                    builtin_var_overrides[code.Name.Content] = new Dictionary<string, AssetIDType>();
            }

            builtin_vars = new Dictionary<string, AssetIDType>
            {
                // only the relevant ones because I'm sick of writing this
                { "background_index", AssetIDType.Background }, // array
                { "background_colour", AssetIDType.Color }, // array
                { "view_object", AssetIDType.GameObject }, // array
                { "path_index", AssetIDType.Path },
                { "room_persistent", AssetIDType.Boolean },
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
                { "mouse_button", AssetIDType.MouseButton },
                { "mouse_last_button", AssetIDType.MouseButton },
                { "os_type", AssetIDType.Enum_OSType },
                { "timeline_index", AssetIDType.Timeline },
                { "path_endaction", AssetIDType.Enum_PathEndAction },
                { "view_enabled", AssetIDType.Boolean },
                { "view_visible", AssetIDType.Boolean },
                { "visible", AssetIDType.Boolean }

            };

            // TODO: make proper file/manifest for all games to use, not just UT/DR, and also not these specific names
            string lowerName = data?.GeneralInfo?.DisplayName?.Content.ToLower(CultureInfo.InvariantCulture);

            // Just Undertale
            if (lowerName != null && lowerName.StartsWith("undertale", StringComparison.InvariantCulture))
            {

                //AddOverrideFor("gml_Object_obj_wizardorb_chaser_Alarm_0", "pop", AssetIDType.Script);

                AddOverrideFor("gml_Object_obj_fakeborderdraw_Draw_0", "op", AssetIDType.GameObject);
                AddOverrideFor("gml_Object_obj_vertcroissant_Step_0", "op", AssetIDType.GameObject);

                return_types["scr_getmusindex"] = AssetIDType.Sound;
                return_types["scr_getsprite"] = AssetIDType.Sprite;
                return_types["caster_load"] = AssetIDType.Sound;

                // Sometimes used as a bool, should not matter though and be an improvement overall.
                builtin_vars.Add("king", AssetIDType.GameObject);
                builtin_funcs["SCR_TEXTSETUP"] = new[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                // I should confirm adding this causes no adverse effects later.
                builtin_vars.Add("myroom", AssetIDType.Room);
                // gml_Object_obj_dummytrigger_Collision_1576
                builtin_vars.Add("dummy", AssetIDType.GameObject);
                // gml_Object_obj_asriel_swordarm_Create_0
                builtin_vars.Add("sm", AssetIDType.GameObject);
                // This should do something to fix the piano room
                builtin_vars.Add("sprite_id", AssetIDType.Sprite);
                builtin_funcs["scr_getsprite"] = new[] { AssetIDType.Sprite };
                // gml_Object_obj_barabody_Create_0
                builtin_vars.Add("hand1pic", AssetIDType.Sprite);
                builtin_vars.Add("hand2pic", AssetIDType.Sprite);
                builtin_vars.Add("headpic", AssetIDType.Sprite);
                // gml_Object_obj_asgoreb_body_Create_0
                builtin_vars.Add("bodypic", AssetIDType.Sprite);
                // gml_Object_obj_castroll_Draw_0
                builtin_vars.Add("do_room_goto", AssetIDType.Boolean);
                builtin_vars.Add("do_room_goto_target", AssetIDType.Room);
            }

            // Just Deltarune
            if (lowerName != null && (lowerName == "survey_program" || lowerName.StartsWith("deltarune", StringComparison.InvariantCulture) || lowerName == "deltarune chapter 1 & 2"))
            {
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
                // "targetsprite" seems to be unused but just in case
                builtin_vars.Add("targetsprite", AssetIDType.Sprite);
                builtin_vars.Add("thissprite", AssetIDType.Sprite);
                builtin_vars.Add("touchsprite", AssetIDType.Sprite);
                builtin_vars.Add("sprite_type", AssetIDType.Sprite);
                builtin_vars.Add("darkzone", AssetIDType.Boolean);
                builtin_vars.Add("darkmode", AssetIDType.Boolean);
                builtin_vars.Add("darkify", AssetIDType.Boolean);
                builtin_vars.Add("noroom", AssetIDType.Boolean);
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
                builtin_vars.Add("battlemoder", AssetIDType.Boolean);
                builtin_vars.Add("becamebattle", AssetIDType.Boolean);
                builtin_vars.Add("seriousbattle", AssetIDType.Boolean);
                // Deltarune console versions.
                builtin_vars.Add("_border_image", AssetIDType.Sprite);
                // A little bit wrong, but probably fine.
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
                // Colors weave into a spire of flame
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
                // Scripts
                builtin_funcs["SCR_TEXTSETUP"] = new[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
            }

            // Both UT and DR
            if (lowerName != null && (lowerName.StartsWith("undertale", StringComparison.InvariantCulture) || lowerName == "survey_program" || lowerName.StartsWith("deltarune", StringComparison.InvariantCulture)))
            {
                AddOverrideFor("gml_Script_scr_getbuttonsprite", "control", AssetIDType.Enum_GamepadButton);
                AddOverrideFor("gml_Script_scr_getbuttonsprite", "button", AssetIDType.Enum_GamepadButton);

                // Don't use this. It will not recompile.
                // AddOverrideFor("gml_Object_obj_shop3_Draw_0", "mycolor", AssetIDType.Macro);

                return_types["scr_getbuttonsprite"] = AssetIDType.Sprite;

                // gml_Object_obj_vulkinbody_UNDERTALE_Create_0
                // Seems to be used a lot as a regular value between the values of around 0-20.
                AddOverrideFor("obj_vulkinbody", "face", AssetIDType.Sprite);

                AddOverrideFor("gml_Script_scr_setfont", "newfont", AssetIDType.Font);

                //builtin_vars.Add("face", AssetIDType.Sprite);

                builtin_vars.Add("myfont", AssetIDType.Font);

                // Hope this script works!
                builtin_funcs["scr_bouncer"] = new[] { AssetIDType.GameObject, AssetIDType.Other, AssetIDType.Other };

                // Deltarune Chapter 2 asset resolutions:
                // Seems to be x, y, measure of distance (maybe)

                builtin_funcs["gml_Script_c_soundplay_wait"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_pitch_time"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_sprite_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["c_soundplay_wait"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_pitch_time"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["draw_sprite_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };

                builtin_funcs["gml_Script__background_set"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["gml_Script_c_addxy"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color };
                builtin_funcs["gml_Script_c_autodepth"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_autofacing"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_autowalk"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_fadeout"] = new[] { AssetIDType.Other };
                builtin_funcs["gml_Script_c_fadeout_color"] = new[] { AssetIDType.Other, AssetIDType.Color };
                builtin_funcs["gml_Script_c_instance"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["gml_Script_c_msgzurasu"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_pan"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_c_pannable"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_panobj"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["gml_Script_c_panspeed"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_c_script_instance"] = new[] { AssetIDType.GameObject, AssetIDType.Script, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_c_script_instance_stop"] = new[] { AssetIDType.GameObject, AssetIDType.Script };
                builtin_funcs["gml_Script_c_setxy"] = new[] { AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_c_shakestep_x"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_soundplay"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_c_soundplay_x"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_c_sprite"] = new[] { AssetIDType.Sprite };
                builtin_funcs["gml_Script_c_stickto"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["gml_Script_c_visible"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_wait"] = new[] { AssetIDType.Other };
                builtin_funcs["gml_Script_c_walkdirect"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_c_walkdirect_wait"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_d3d_set_fog_ch1"] = new[] { AssetIDType.Boolean, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_background_ext_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_background_part_ext_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_background_tiled_ext_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_enable_alphablend_ch1"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_draw_enable_alphablend"] = new[] { AssetIDType.Boolean };
                builtin_funcs["draw_enable_alphablend"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_draw_monster_body_part"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_monster_body_part_ext"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_sprite_ext_centerscale"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_sprite_ext_flash"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_sprite_skew_ext_cute"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_text_outline"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_i_ex"] = new[] { AssetIDType.GameObject };
                builtin_funcs["gml_Script_instance_create_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["gml_Script_msgsetloc"] = new[] { AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_mus_loop"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_mus_loop_ext"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_safe_delete"] = new[] { AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_84_debug"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_scr_act_charsprite"] = new[] { AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Boolean };
                builtin_funcs["gml_Script_scr_anim"] = new[] { AssetIDType.Sprite, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_anim_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_battle"] = new[] { AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_battle_marker"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_bullet_create"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_bulletspawner"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_caterpillar_facing_ch1"] = new[] { AssetIDType.Other };
                builtin_funcs["gml_Script_scr_custom_afterimage"] = new[] { AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_custom_afterimage_ext"] = new[] { AssetIDType.GameObject, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_dark_marker"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_dark_marker_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_dark_marker_depth"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_debug_keycheck"] = new[] { AssetIDType.KeyboardKey };
                builtin_funcs["gml_Script_scr_draw_background_ps4_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_draw_outline_ext"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_draw_sprite_crop_ext"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_ds_list_write"] = new[] { AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_enemyblcon"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_following_afterimage"] = new[] { AssetIDType.GameObject, AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_forcefield"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean };
                builtin_funcs["gml_Script_scr_fx_housesquare"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color };
                builtin_funcs["gml_Script_scr_guardpeek"] = new[] { AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_marker"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_marker_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_mercyadd"] = new[] { AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_monster_add"] = new[] { AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_monster_change"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_move_to_point_over_time"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_pan_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_pan_to_obj"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_pan_to_obj_ch1"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_script_delayed"] = new[] { AssetIDType.Script, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_textsetup"] = new[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_textsetup_ch1"] = new[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_snd_is_playing"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_loop"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_loop_ch1"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_pitch"] = new[] { AssetIDType.Sound, AssetIDType.Other };
                builtin_funcs["gml_Script_snd_play"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_play_ch1"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_play_pitch"] = new[] { AssetIDType.Sound, AssetIDType.Other };
                builtin_funcs["gml_Script_snd_play_x"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_snd_stop"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_stop_ch1"] = new[] { AssetIDType.Sound };
                builtin_funcs["gml_Script_snd_volume"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_act_charsprite"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean };
                builtin_funcs["gml_Script_draw_sprite_part_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_draw_sprite_part_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["draw_sprite_part_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_draw_sprite_part_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_draw_sprite_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["draw_sprite_ext_glow"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_draw_sprite_tiled_area"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Boolean };
                builtin_funcs["gml_Script_c_actorsetsprites"] = new[] { AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Sprite, AssetIDType.Sprite, AssetIDType.Sprite };
                builtin_funcs["gml_Script_c_actortoobject"] = new[] { AssetIDType.GameObject };
                builtin_funcs["gml_Script_scr_marker_animated"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Other };
                builtin_funcs["scr_marker_animated"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Other };
                builtin_funcs["gml_Script_c_jump_sprite"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Sprite };
                builtin_funcs["gml_Script_scr_dark_marker_animated"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Boolean };
                builtin_funcs["scr_act_charsprite"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean };
                builtin_funcs["scr_draw_sprite_tiled_area"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Boolean };
                builtin_funcs["c_actorsetsprites"] = new[] { AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Sprite, AssetIDType.Sprite, AssetIDType.Sprite };
                builtin_funcs["c_actortoobject"] = new[] { AssetIDType.GameObject };
                builtin_funcs["c_jump_sprite"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Sprite };
                builtin_funcs["scr_dark_marker_animated"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Boolean };
                builtin_funcs["_background_set"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["c_addxy"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color };
                builtin_funcs["c_autodepth"] = new[] { AssetIDType.Boolean };
                builtin_funcs["c_autofacing"] = new[] { AssetIDType.Boolean };
                builtin_funcs["c_autowalk"] = new[] { AssetIDType.Boolean };
                builtin_funcs["c_fadeout"] = new[] { AssetIDType.Other };
                builtin_funcs["c_fadeout_color"] = new[] { AssetIDType.Other, AssetIDType.Color };
                builtin_funcs["c_instance"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["c_msgzurasu"] = new[] { AssetIDType.Boolean };
                builtin_funcs["c_pan"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["c_pannable"] = new[] { AssetIDType.Boolean };
                builtin_funcs["c_panobj"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["c_panspeed"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["c_script_instance"] = new[] { AssetIDType.GameObject, AssetIDType.Script, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["c_script_instance_stop"] = new[] { AssetIDType.GameObject, AssetIDType.Script };
                builtin_funcs["c_setxy"] = new[] { AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["c_shakestep_x"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean };
                builtin_funcs["c_soundplay"] = new[] { AssetIDType.Sound };
                builtin_funcs["c_soundplay_x"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["c_sprite"] = new[] { AssetIDType.Sprite };
                builtin_funcs["c_stickto"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["c_visible"] = new[] { AssetIDType.Boolean };
                builtin_funcs["c_wait"] = new[] { AssetIDType.Other };
                builtin_funcs["c_walkdirect"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["c_walkdirect_wait"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["d3d_set_fog_ch1"] = new[] { AssetIDType.Boolean, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["draw_background_ext_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["draw_background_part_ext_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["draw_background_tiled_ext_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["draw_enable_alphablend_ch1"] = new[] { AssetIDType.Boolean };
                builtin_funcs["draw_monster_body_part"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["draw_monster_body_part_ext"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["draw_sprite_ext_centerscale"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["draw_sprite_ext_flash"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other };
                builtin_funcs["draw_sprite_skew_ext_cute"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["draw_text_outline"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["i_ex"] = new[] { AssetIDType.GameObject };
                builtin_funcs["instance_create_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["msgsetloc"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["mus_loop"] = new[] { AssetIDType.Sound };
                builtin_funcs["mus_loop_ext"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["gml_Script_scr_bullet_inherit_ch1"] = new[] { AssetIDType.GameObject };
                builtin_funcs["scr_bullet_inherit_ch1"] = new[] { AssetIDType.GameObject };
                builtin_funcs["safe_delete"] = new[] { AssetIDType.GameObject };
                builtin_funcs["scr_84_debug"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_texture_set_interpolation"] = new[] { AssetIDType.Boolean };
                builtin_funcs["texture_set_interpolation"] = new[] { AssetIDType.Boolean };
                builtin_funcs["gml_Script_texture_set_interpolation_ch1"] = new[] { AssetIDType.Boolean };
                builtin_funcs["texture_set_interpolation_ch1"] = new[] { AssetIDType.Boolean };
                builtin_funcs["scr_act_charsprite"] = new[] { AssetIDType.Other, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Boolean };
                builtin_funcs["scr_anim"] = new[] { AssetIDType.Sprite, AssetIDType.Other };
                builtin_funcs["scr_anim_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other };
                builtin_funcs["scr_battle"] = new[] { AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_battle_marker"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["scr_bullet_create"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["scr_bulletspawner"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["scr_caterpillar_facing_ch1"] = new[] { AssetIDType.Other };
                builtin_funcs["scr_custom_afterimage"] = new[] { AssetIDType.Sprite };
                builtin_funcs["scr_custom_afterimage_ext"] = new[] { AssetIDType.GameObject, AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_dark_marker"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["scr_dark_marker_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["scr_dark_marker_depth"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["scr_debug_keycheck"] = new[] { AssetIDType.KeyboardKey };
                builtin_funcs["scr_draw_background_ps4_ch1"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_draw_outline_ext"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_draw_sprite_crop_ext"] = new[] { AssetIDType.Sprite, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_ds_list_write"] = new[] { AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_enemyblcon"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_following_afterimage"] = new[] { AssetIDType.GameObject, AssetIDType.GameObject };
                builtin_funcs["scr_forcefield"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Boolean, AssetIDType.Boolean };
                builtin_funcs["scr_fx_housesquare"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Color };
                builtin_funcs["scr_guardpeek"] = new[] { AssetIDType.GameObject };
                builtin_funcs["scr_marker"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["scr_marker_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Sprite };
                builtin_funcs["scr_mercyadd"] = new[] { AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_monster_add"] = new[] { AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["scr_monster_change"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.GameObject };
                builtin_funcs["scr_move_to_point_over_time"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_pan_ch1"] = new[] { AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_pan_to_obj"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["scr_pan_to_obj_ch1"] = new[] { AssetIDType.GameObject, AssetIDType.Other };
                builtin_funcs["scr_script_delayed"] = new[] { AssetIDType.Script, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_textsetup"] = new[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_textsetup_ch1"] = new[] { AssetIDType.Font, AssetIDType.Color, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other, AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["snd_is_playing"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_loop"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_loop_ch1"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_pitch"] = new[] { AssetIDType.Sound, AssetIDType.Other };
                builtin_funcs["snd_play"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_play_ch1"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_play_pitch"] = new[] { AssetIDType.Sound, AssetIDType.Other };
                builtin_funcs["snd_play_x"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["snd_stop"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_stop_ch1"] = new[] { AssetIDType.Sound };
                builtin_funcs["snd_volume"] = new[] { AssetIDType.Sound, AssetIDType.Other, AssetIDType.Other };
                builtin_funcs["scr_valid_room"] = new[] { AssetIDType.Room, AssetIDType.Other };

                builtin_vars.Add("_instruments", AssetIDType.Sound);
                builtin_vars.Add("_instrumentsB", AssetIDType.Sound);
                builtin_vars.Add("_instrumentsAlt", AssetIDType.Sound);
                builtin_vars.Add("doorSound", AssetIDType.Sound);
                builtin_vars.Add("doorSound2", AssetIDType.Sound);
                builtin_vars.Add("pushSound", AssetIDType.Sound);
                builtin_vars.Add("firstsound", AssetIDType.Sound);
                builtin_vars.Add("lastsound", AssetIDType.Sound);
                builtin_vars.Add("voiceclips", AssetIDType.Sound);

                builtin_vars.Add("roome", AssetIDType.Room);
                builtin_vars.Add("room_index", AssetIDType.Room);
                builtin_vars.Add("room_offset", AssetIDType.Room);
                builtin_vars.Add("door_destination", AssetIDType.Room);

                builtin_vars.Add("source", AssetIDType.GameObject);
                builtin_vars.Add("sourceobject", AssetIDType.GameObject);
                //builtin_vars.Add("target", AssetIDType.GameObject);
                builtin_vars.Add("writergod", AssetIDType.GameObject);
                AddOverrideFor("obj_carcutscene", "k", AssetIDType.GameObject);
                AddOverrideFor("obj_carcutscene_ch1", "k", AssetIDType.GameObject);
                builtin_vars.Add("childBullet", AssetIDType.GameObject);
                builtin_vars.Add("body_obj", AssetIDType.GameObject);
                builtin_vars.Add("sneo", AssetIDType.GameObject);
                builtin_vars.Add("swatchbc", AssetIDType.GameObject);
                builtin_vars.Add("animator", AssetIDType.GameObject);

                builtin_vars.Add("new_color", AssetIDType.Color);
                builtin_vars.Add("base_colors", AssetIDType.Color);
                builtin_vars.Add("COL_A", AssetIDType.Color);
                builtin_vars.Add("COL_B", AssetIDType.Color);
                builtin_vars.Add("COL_PLUS", AssetIDType.Color);
                builtin_vars.Add("stats_amount", AssetIDType.Color);
                builtin_vars.Add("housecolor", AssetIDType.Color);
                builtin_vars.Add("partblend", AssetIDType.Color);
                builtin_vars.Add("drawcolor", AssetIDType.Color);
                builtin_vars.Add("shockwave_color", AssetIDType.Color);
                builtin_vars.Add("bird_color", AssetIDType.Color);
                builtin_vars.Add("startcolor", AssetIDType.Color);
                builtin_vars.Add("targetColor", AssetIDType.Color);
                builtin_vars.Add("override_color", AssetIDType.Color);

                builtin_vars.Add("control", AssetIDType.Enum_GamepadButton);
                builtin_vars.Add("button0", AssetIDType.Enum_GamepadButton);
                builtin_vars.Add("button1", AssetIDType.Enum_GamepadButton);
                builtin_vars.Add("button2", AssetIDType.Enum_GamepadButton);
                builtin_vars.Add("input_g", AssetIDType.Enum_GamepadButton);
                builtin_vars.Add("gamepad_controls", AssetIDType.Enum_GamepadButton);
                builtin_vars.Add("new_gamepad_key", AssetIDType.Enum_GamepadButton);
                builtin_vars.Add("_control", AssetIDType.Enum_GamepadButton);

                builtin_vars.Add("input_k", AssetIDType.KeyboardKey);

/*
                builtin_vars.Add("_pacified", AssetIDType.Boolean);
                builtin_vars.Add("_spared", AssetIDType.Boolean);
                builtin_vars.Add("_violenced", AssetIDType.Boolean);
                builtin_vars.Add("_frozened", AssetIDType.Boolean);
*/
                builtin_vars.Add("nitro", AssetIDType.Boolean);
                builtin_vars.Add("confirm", AssetIDType.Boolean);
                builtin_vars.Add("attack_succeeded", AssetIDType.Boolean);
                builtin_vars.Add("phase3_hit_check", AssetIDType.Boolean);
                builtin_vars.Add("beat_phase2_no_damage_taken_check", AssetIDType.Boolean);
                builtin_vars.Add("beat_phase1_no_damage_taken_check", AssetIDType.Boolean);
                builtin_vars.Add("has_tutorial_kick_hit_player", AssetIDType.Boolean);
                builtin_vars.Add("freezable", AssetIDType.Boolean);
                builtin_vars.Add("charcan", AssetIDType.Boolean);
                builtin_vars.Add("kktalked", AssetIDType.Boolean);
                builtin_vars.Add("capntalked", AssetIDType.Boolean);
                builtin_vars.Add("moveswapped", AssetIDType.Boolean);
                builtin_vars.Add("cause_explosion", AssetIDType.Boolean);
                builtin_vars.Add("exploded", AssetIDType.Boolean);
                builtin_vars.Add("make_simple_bullet", AssetIDType.Boolean);
                builtin_vars.Add("dance_active", AssetIDType.Boolean);
                builtin_vars.Add("spawnVirus", AssetIDType.Boolean);
                builtin_vars.Add("spawning", AssetIDType.Boolean);
                builtin_vars.Add("caralert", AssetIDType.Boolean);
                builtin_vars.Add("shotready", AssetIDType.Boolean);
                builtin_vars.Add("deleteCars", AssetIDType.Boolean);
                builtin_vars.Add("clear_traffic", AssetIDType.Boolean);
                builtin_vars.Add("onroad", AssetIDType.Boolean);
                builtin_vars.Add("quizloop", AssetIDType.Boolean);
                builtin_vars.Add("quizmode", AssetIDType.Boolean);
                builtin_vars.Add("gif_recording", AssetIDType.Boolean);
                builtin_vars.Add("quicksaved", AssetIDType.Boolean);
                builtin_vars.Add("paused", AssetIDType.Boolean);
                builtin_vars.Add("dbselect", AssetIDType.Boolean);
                builtin_vars.Add("drawfeet", AssetIDType.Boolean);
                builtin_vars.Add("moving", AssetIDType.Boolean);
                builtin_vars.Add("duckmode", AssetIDType.Boolean);
                builtin_vars.Add("dancing", AssetIDType.Boolean);
                builtin_vars.Add("hasCandy", AssetIDType.Boolean);
                builtin_vars.Add("allowAll", AssetIDType.Boolean);
                builtin_vars.Add("partvisible", AssetIDType.Boolean);
                builtin_vars.Add("breaking", AssetIDType.Boolean);
                builtin_vars.Add("destroyable", AssetIDType.Boolean);
                builtin_vars.Add("skipintro", AssetIDType.Boolean);
                builtin_vars.Add("peeking", AssetIDType.Boolean);
                builtin_vars.Add("destroyonhit", AssetIDType.Boolean);
                builtin_vars.Add("spawned", AssetIDType.Boolean);
                builtin_vars.Add("turnSignal", AssetIDType.Boolean);
                builtin_vars.Add("dothis", AssetIDType.Boolean);
                builtin_vars.Add("stuck", AssetIDType.Boolean);
                builtin_vars.Add("save_loaded", AssetIDType.Boolean);
                builtin_vars.Add("save_ready", AssetIDType.Boolean);
                builtin_vars.Add("ignoredepth", AssetIDType.Boolean);
                builtin_vars.Add("drawshadow", AssetIDType.Boolean);
                builtin_vars.Add("tracking", AssetIDType.Boolean);
                builtin_vars.Add("haventusedspell", AssetIDType.Boolean);
                builtin_vars.Add("back", AssetIDType.Boolean);
                builtin_vars.Add("spare_used", AssetIDType.Boolean);
                builtin_vars.Add("touched", AssetIDType.Boolean);
                builtin_vars.Add("interactshower", AssetIDType.Boolean);
                builtin_vars.Add("windowswitcher", AssetIDType.Boolean);
                builtin_vars.Add("cutsceneshow", AssetIDType.Boolean);
                builtin_vars.Add("showdebug", AssetIDType.Boolean);
                builtin_vars.Add("writedisplay", AssetIDType.Boolean);
                builtin_vars.Add("displaymode", AssetIDType.Boolean);
                builtin_vars.Add("oldway", AssetIDType.Boolean);
                builtin_vars.Add("special", AssetIDType.Boolean);
                builtin_vars.Add("sameattacker", AssetIDType.Boolean);
                builtin_vars.Add("sameattack", AssetIDType.Boolean);
                builtin_vars.Add("bosscheck", AssetIDType.Boolean);
                builtin_vars.Add("dheld", AssetIDType.Boolean);
                builtin_vars.Add("uheld", AssetIDType.Boolean);
                builtin_vars.Add("rheld", AssetIDType.Boolean);
                builtin_vars.Add("lheld", AssetIDType.Boolean);
                builtin_vars.Add("edgedebug", AssetIDType.Boolean);
                builtin_vars.Add("solve", AssetIDType.Boolean);
                builtin_vars.Add("noface", AssetIDType.Boolean);
                builtin_vars.Add("drawshine", AssetIDType.Boolean);
                builtin_vars.Add("weird", AssetIDType.Boolean);
                builtin_vars.Add("freeze", AssetIDType.Boolean);
                builtin_vars.Add("fightmode", AssetIDType.Boolean);
                builtin_vars.Add("show_door_open", AssetIDType.Boolean);
                builtin_vars.Add("disable_face", AssetIDType.Boolean);
                builtin_vars.Add("enable_face", AssetIDType.Boolean);
                builtin_vars.Add("end_game", AssetIDType.Boolean);
                builtin_vars.Add("float", AssetIDType.Boolean);
                builtin_vars.Add("checkPress", AssetIDType.Boolean);
                builtin_vars.Add("pressable", AssetIDType.Boolean);
                builtin_vars.Add("init_forcefields", AssetIDType.Boolean);
                builtin_vars.Add("auto_continue", AssetIDType.Boolean);
                builtin_vars.Add("no_silhouette", AssetIDType.Boolean);
                builtin_vars.Add("allow_move", AssetIDType.Boolean);
                builtin_vars.Add("grazed", AssetIDType.Boolean);
                builtin_vars.Add("pressed", AssetIDType.Boolean);
                builtin_vars.Add("border_fade_in", AssetIDType.Boolean);
                builtin_vars.Add("border_fade_out", AssetIDType.Boolean);
                builtin_vars.Add("rideactgo", AssetIDType.Boolean);
                builtin_vars.Add("forgiveLoop", AssetIDType.Boolean);
                builtin_vars.Add("readyToGo", AssetIDType.Boolean);
                builtin_vars.Add("shouldActivate", AssetIDType.Boolean);
                builtin_vars.Add("destroyoffscreen", AssetIDType.Boolean);
                builtin_vars.Add("autocancel", AssetIDType.Boolean);
                builtin_vars.Add("destroyoncomplete", AssetIDType.Boolean);
                builtin_vars.Add("spelluse", AssetIDType.Boolean);
                builtin_vars.Add("animdone", AssetIDType.Boolean);
                builtin_vars.Add("bigcar", AssetIDType.Boolean);
                //builtin_vars.Add("forceypos", AssetIDType.Boolean);
                builtin_vars.Add("bump", AssetIDType.Boolean);
                builtin_vars.Add("drawpremonition", AssetIDType.Boolean);
                builtin_vars.Add("premonition", AssetIDType.Boolean);
                builtin_vars.Add("queenmode", AssetIDType.Boolean);
                builtin_vars.Add("arcade", AssetIDType.Boolean);
                builtin_vars.Add("arcadebaseballused", AssetIDType.Boolean);
                builtin_vars.Add("playerhitbykick", AssetIDType.Boolean);
                builtin_vars.Add("finalbaseballused", AssetIDType.Boolean);
                builtin_vars.Add("has_boss_done_pattern7", AssetIDType.Boolean);
                builtin_vars.Add("dead", AssetIDType.Boolean);
                builtin_vars.Add("filefound", AssetIDType.Boolean);
                builtin_vars.Add("loadcompletion", AssetIDType.Boolean);
                builtin_vars.Add("COMPLETEFILE_PREV", AssetIDType.Boolean);
                builtin_vars.Add("input_pressed", AssetIDType.Boolean);
                builtin_vars.Add("input_held", AssetIDType.Boolean);
                builtin_vars.Add("input_released", AssetIDType.Boolean);
                builtin_vars.Add("return_title", AssetIDType.Boolean);
                builtin_vars.Add("d_cancel", AssetIDType.Boolean);
                builtin_vars.Add("killactive", AssetIDType.Boolean);
                builtin_vars.Add("gameover", AssetIDType.Boolean);
                builtin_vars.Add("font_set", AssetIDType.Boolean);
                builtin_vars.Add("playcheck", AssetIDType.Boolean);
                builtin_vars.Add("play", AssetIDType.Boolean);
                builtin_vars.Add("mouthmove", AssetIDType.Boolean);
                builtin_vars.Add("spellanim", AssetIDType.Boolean);
                builtin_vars.Add("textwait", AssetIDType.Boolean);
                builtin_vars.Add("noneleft", AssetIDType.Boolean);
                builtin_vars.Add("actingsimul", AssetIDType.Boolean);
                builtin_vars.Add("halt", AssetIDType.Boolean);
                builtin_vars.Add("drawaster", AssetIDType.Boolean);
                builtin_vars.Add("formatted", AssetIDType.Boolean);
                builtin_vars.Add("forcebutton1", AssetIDType.Boolean);
                //builtin_vars.Add("inv", AssetIDType.Boolean);
                builtin_vars.Add("actsimulsus", AssetIDType.Boolean);
                builtin_vars.Add("actsimulral", AssetIDType.Boolean);
                builtin_vars.Add("actsimulnoe", AssetIDType.Boolean);
                builtin_vars.Add("actsimul", AssetIDType.Boolean);
                builtin_vars.Add("actingsus", AssetIDType.Boolean);
                builtin_vars.Add("actingral", AssetIDType.Boolean);
                builtin_vars.Add("actingnoe", AssetIDType.Boolean);
                builtin_vars.Add("shakereduct", AssetIDType.Boolean);
                builtin_vars.Add("_playsound", AssetIDType.Boolean);
                builtin_vars.Add("haveit", AssetIDType.Boolean);
                builtin_vars.Add("removed", AssetIDType.Boolean);
                builtin_vars.Add("_noroominventory", AssetIDType.Boolean);
                builtin_vars.Add("_pocketed", AssetIDType.Boolean);
                builtin_vars.Add("replaceable", AssetIDType.Boolean);
                builtin_vars.Add("invert", AssetIDType.Boolean);
                builtin_vars.Add("is_dualshock", AssetIDType.Boolean);
                builtin_vars.Add("isString", AssetIDType.Boolean);
                builtin_vars.Add("charauto", AssetIDType.Boolean);
                builtin_vars.Add("auto_length", AssetIDType.Boolean);
                builtin_vars.Add("simultotal_funny", AssetIDType.Boolean);
                builtin_vars.Add("actingsingle", AssetIDType.Boolean);
                builtin_vars.Add("talked", AssetIDType.Boolean);
                builtin_vars.Add("acting", AssetIDType.Boolean);
                builtin_vars.Add("__noactors", AssetIDType.Boolean);
                builtin_vars.Add("fatal", AssetIDType.Boolean);
                builtin_vars.Add("recruitable", AssetIDType.Boolean);
                builtin_vars.Add("__frozen", AssetIDType.Boolean);
                builtin_vars.Add("debug", AssetIDType.Boolean);
                builtin_vars.Add("oldcalculation", AssetIDType.Boolean);
                builtin_vars.Add("chemg_god_mode", AssetIDType.Boolean);
                builtin_vars.Add("debug_inv", AssetIDType.Boolean);
                builtin_vars.Add("gamepad_shoulderlb_reassign", AssetIDType.Boolean);
                builtin_vars.Add("ladef", AssetIDType.Boolean);
                builtin_vars.Add("armorconverted", AssetIDType.Boolean);
                builtin_vars.Add("armorchar1temp", AssetIDType.Boolean);
                builtin_vars.Add("armorchar2temp", AssetIDType.Boolean);
                builtin_vars.Add("armorchar3temp", AssetIDType.Boolean);
                builtin_vars.Add("armorchar4temp", AssetIDType.Boolean);
                builtin_vars.Add("weaponchar1temp", AssetIDType.Boolean);
                builtin_vars.Add("weaponchar2temp", AssetIDType.Boolean);
                builtin_vars.Add("weaponchar3temp", AssetIDType.Boolean);
                builtin_vars.Add("weaponchar4temp", AssetIDType.Boolean);
                builtin_vars.Add("legacy", AssetIDType.Boolean);
                builtin_vars.Add("jp_data_loaded", AssetIDType.Boolean);
                builtin_vars.Add("ingame", AssetIDType.Boolean);
                builtin_vars.Add("skipped", AssetIDType.Boolean);
                builtin_vars.Add("draw_screen", AssetIDType.Boolean);
                builtin_vars.Add("gamepad_active", AssetIDType.Boolean);
                builtin_vars.Add("screen_border_active", AssetIDType.Boolean);
                builtin_vars.Add("window_center_toggle", AssetIDType.Boolean);
                builtin_vars.Add("fullscreen_toggle", AssetIDType.Boolean);
                builtin_vars.Add("keyboard_active", AssetIDType.Boolean);
                builtin_vars.Add("_isConsole", AssetIDType.Boolean);
                builtin_vars.Add("pausing", AssetIDType.Boolean);
                builtin_vars.Add("store_prompt", AssetIDType.Boolean);
                builtin_vars.Add("visit_shop", AssetIDType.Boolean);
                builtin_vars.Add("loaded", AssetIDType.Boolean);
                builtin_vars.Add("commerce_dialog_open", AssetIDType.Boolean);
                builtin_vars.Add("beenset", AssetIDType.Boolean);
                builtin_vars.Add("menuOpened", AssetIDType.Boolean);
                builtin_vars.Add("game_won", AssetIDType.Boolean);
                builtin_vars.Add("timeruse", AssetIDType.Boolean);
                builtin_vars.Add("leapmode", AssetIDType.Boolean);
                //builtin_vars.Add("chapter_return", AssetIDType.Boolean);
                builtin_vars.Add("m_quit", AssetIDType.Boolean);
                builtin_vars.Add("border_select", AssetIDType.Boolean);
                builtin_vars.Add("check_border", AssetIDType.Boolean);
                builtin_vars.Add("disable_border", AssetIDType.Boolean);
                builtin_vars.Add("cancelnoise", AssetIDType.Boolean);
                builtin_vars.Add("_disable_border", AssetIDType.Boolean);
                builtin_vars.Add("restart", AssetIDType.Boolean);
                //builtin_vars.Add("battlemode", AssetIDType.Boolean);
                builtin_vars.Add("init", AssetIDType.Boolean);
                builtin_vars.Add("autobattle", AssetIDType.Boolean);
                builtin_vars.Add("acttoenemytalktransition", AssetIDType.Boolean); // Probably, haven't checked
                builtin_vars.Add("STARTGAME", AssetIDType.Boolean);
                builtin_vars.Add("SELNOISE", AssetIDType.Boolean);
                builtin_vars.Add("temp_comment_is_interesting", AssetIDType.Boolean);
                builtin_vars.Add("FILECHECK", AssetIDType.Boolean);
                builtin_vars.Add("input_enabled", AssetIDType.Boolean);
                builtin_vars.Add("INCOMPLETE_LOAD", AssetIDType.Boolean);
                builtin_vars.Add("is_console", AssetIDType.Boolean);
                builtin_vars.Add("CANQUIT", AssetIDType.Boolean);
                builtin_vars.Add("BGMADE", AssetIDType.Boolean);
                builtin_vars.Add("finished", AssetIDType.Boolean);
                builtin_vars.Add("is_active", AssetIDType.Boolean);
                builtin_vars.Add("spam_car", AssetIDType.Boolean);
                builtin_vars.Add("show_queen", AssetIDType.Boolean);
                builtin_vars.Add("queen_animate", AssetIDType.Boolean);
                builtin_vars.Add("actor_visible", AssetIDType.Boolean);
                builtin_vars.Add("drawcustom", AssetIDType.Boolean);
                builtin_vars.Add("alwayswalking", AssetIDType.Boolean);
                builtin_vars.Add("prepopulate", AssetIDType.Boolean);
                builtin_vars.Add("walking", AssetIDType.Boolean);
                builtin_vars.Add("speedadjust", AssetIDType.Boolean);
                builtin_vars.Add("fresh", AssetIDType.Boolean);
                builtin_vars.Add("canactnoe", AssetIDType.Boolean);
                builtin_vars.Add("canactral", AssetIDType.Boolean);
                builtin_vars.Add("canactsus", AssetIDType.Boolean);
                builtin_vars.Add("skip", AssetIDType.Boolean);
                builtin_vars.Add("stayVisible", AssetIDType.Boolean);
                builtin_vars.Add("playsound", AssetIDType.Boolean);
                builtin_vars.Add("noAlertSound", AssetIDType.Boolean);

                builtin_vars.Add("hitbox", AssetIDType.Sprite);
                builtin_vars.Add("sprite", AssetIDType.Sprite);
                builtin_vars.Add("writerimg", AssetIDType.Sprite);
                builtin_vars.Add("_sprite", AssetIDType.Sprite);
                builtin_vars.Add("specialsprite", AssetIDType.Sprite);
                builtin_vars.Add("o_boxingqueen_janky_sprite_index", AssetIDType.Sprite);
                builtin_vars.Add("character_sprite", AssetIDType.Sprite);
                builtin_vars.Add("victorySprite", AssetIDType.Sprite);
                builtin_vars.Add("contentsprite", AssetIDType.Sprite);
                builtin_vars.Add("sprite_palette", AssetIDType.Sprite);
                builtin_vars.Add("head_sprite", AssetIDType.Sprite);
                builtin_vars.Add("pilot_sprite", AssetIDType.Sprite);
                builtin_vars.Add("default_sprite_front", AssetIDType.Sprite);
                builtin_vars.Add("default_sprite_back", AssetIDType.Sprite);
                builtin_vars.Add("hurt_sprite_front", AssetIDType.Sprite);
                builtin_vars.Add("hurt_sprite_back", AssetIDType.Sprite);
                builtin_vars.Add("hurt_character_sprite", AssetIDType.Sprite);
                builtin_vars.Add("raspr", AssetIDType.Sprite);
                builtin_vars.Add("suspr", AssetIDType.Sprite);
                builtin_vars.Add("car_sprite", AssetIDType.Sprite);
                builtin_vars.Add("hsprite", AssetIDType.Sprite);
                builtin_vars.Add("vsprite", AssetIDType.Sprite);
                builtin_vars.Add("menuSprite", AssetIDType.Sprite);
                builtin_vars.Add("actor_startsprite", AssetIDType.Sprite);
                builtin_vars.Add("actor_endsprite", AssetIDType.Sprite);
                builtin_vars.Add("button_sprite", AssetIDType.Sprite);
                builtin_vars.Add("endanimation", AssetIDType.Sprite);
                builtin_vars.Add("lastSprite", AssetIDType.Sprite);
                builtin_vars.Add("myLastSprite", AssetIDType.Sprite);
                builtin_vars.Add("mySprite", AssetIDType.Sprite);
                builtin_vars.Add("current_sprites", AssetIDType.Sprite);
                builtin_vars.Add("fireworksprite", AssetIDType.Sprite);
                builtin_vars.Add("customSprite", AssetIDType.Sprite);
                builtin_vars.Add("queen_sprite", AssetIDType.Sprite);
                builtin_vars.Add("pic", AssetIDType.Sprite);
                builtin_vars.Add("picb", AssetIDType.Sprite);
                builtin_vars.Add("leftTurnSprite", AssetIDType.Sprite);
                builtin_vars.Add("rightTurnSprite", AssetIDType.Sprite);
                builtin_vars.Add("topsprite", AssetIDType.Sprite);
                builtin_vars.Add("frozensprite", AssetIDType.Sprite);
                builtin_vars.Add("layersprites", AssetIDType.Sprite);
                builtin_vars.Add("cursor_sprite", AssetIDType.Sprite);
                builtin_vars.Add("rimsprite", AssetIDType.Sprite);
                builtin_vars.Add("flashSprite", AssetIDType.Sprite);
                builtin_vars.Add("spriteindex1", AssetIDType.Sprite);
                builtin_vars.Add("spriteindex2", AssetIDType.Sprite);
                builtin_vars.Add("shieldpiece_sprite_index", AssetIDType.Sprite);
                builtin_vars.Add("shieldpiece_alpha", AssetIDType.Sprite);
                builtin_vars.Add("sabersprite", AssetIDType.Sprite);
                builtin_vars.Add("idlesprite", AssetIDType.Sprite);
                builtin_vars.Add("puzzle_icon", AssetIDType.Sprite);
                builtin_vars.Add("item0pic", AssetIDType.Sprite);
                builtin_vars.Add("item1pic", AssetIDType.Sprite);
                builtin_vars.Add("item2pic", AssetIDType.Sprite);
                builtin_vars.Add("item3pic", AssetIDType.Sprite);
                builtin_vars.Add("top", AssetIDType.Sprite);
                builtin_vars.Add("sparesprite", AssetIDType.Sprite);
                builtin_vars.Add("_rollSprites", AssetIDType.Sprite);
                builtin_vars.Add("_sprites", AssetIDType.Sprite);
                builtin_vars.Add("_spritesTea", AssetIDType.Sprite);
                builtin_vars.Add("fruit", AssetIDType.Sprite);
                builtin_vars.Add("chsprite", AssetIDType.Sprite);
                builtin_vars.Add("_headsprite", AssetIDType.Sprite);
                builtin_vars.Add("partsprite", AssetIDType.Sprite);

                builtin_vars.Add("fleetsize", AssetIDType.Other);
                builtin_vars.Add("ar", AssetIDType.Other);
                builtin_vars.Add("as", AssetIDType.Other);
                builtin_vars.Add("be", AssetIDType.Other);
                builtin_vars.Add("char", AssetIDType.Other);
                builtin_vars.Add("choice", AssetIDType.Other);
                builtin_vars.Add("direction", AssetIDType.Other);
                builtin_vars.Add("encounterno", AssetIDType.Other);
                builtin_vars.Add("flag", AssetIDType.Other);
                builtin_vars.Add("gi", AssetIDType.Other);
                builtin_vars.Add("gold", AssetIDType.Other);
                builtin_vars.Add("hg", AssetIDType.Other);
                builtin_vars.Add("la", AssetIDType.Other);
                builtin_vars.Add("lhp", AssetIDType.Other);
                builtin_vars.Add("na", AssetIDType.Other);
                builtin_vars.Add("nl", AssetIDType.Other);
                builtin_vars.Add("no", AssetIDType.Other);
                builtin_vars.Add("plot", AssetIDType.Other);
                builtin_vars.Add("qu", AssetIDType.Other);
                builtin_vars.Add("sa", AssetIDType.Other);
                builtin_vars.Add("side", AssetIDType.Other);
                builtin_vars.Add("st", AssetIDType.Other);
                builtin_vars.Add("sw", AssetIDType.Other);
                builtin_vars.Add("to", AssetIDType.Other);
                builtin_vars.Add("un", AssetIDType.Other);
                builtin_vars.Add("walkpoint", AssetIDType.Other);
                builtin_vars.Add("xx", AssetIDType.Other);
                builtin_vars.Add("yy", AssetIDType.Other);

                // Undertale 1.05+ and Deltarune console versions.
                builtin_funcs["scr_draw_background_ps4"] = new[] { AssetIDType.Background, AssetIDType.Other, AssetIDType.Other };
                builtin_vars.Add("room_id", AssetIDType.Room);

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
                // New built in vars found by Grossley
                builtin_vars.Add("interact", AssetIDType.Other);
                // Test me!
                builtin_vars.Add("sound0", AssetIDType.Sound);
                // From v1.11 Undertale comparison, not tested unlike v1.001!
                builtin_vars.Add("asprite", AssetIDType.Sprite);
                builtin_vars.Add("bsprite", AssetIDType.Sprite);
                builtin_vars.Add("tailobj", AssetIDType.GameObject);
                builtin_vars.Add("heart", AssetIDType.GameObject);
                builtin_vars.Add("draedmode", AssetIDType.Boolean);
                // Deltarune
                builtin_vars.Add("haveauto", AssetIDType.Boolean);
                builtin_vars.Add("goahead", AssetIDType.Boolean);
                builtin_vars.Add("is_auto_susie", AssetIDType.Boolean);
                builtin_vars.Add("techwon", AssetIDType.Boolean);
                builtin_vars.Add("itemed", AssetIDType.Boolean);
                builtin_vars.Add("critical", AssetIDType.Boolean);
                builtin_vars.Add("tile_fade", AssetIDType.Boolean);
                builtin_vars.Add("boss", AssetIDType.Boolean);
                builtin_vars.Add("skipvictory", AssetIDType.Boolean);
                builtin_vars.Add("victory", AssetIDType.Boolean);
                builtin_vars.Add("fighting", AssetIDType.Boolean);
                builtin_vars.Add("charmove", AssetIDType.Boolean);
                builtin_vars.Add("charcantarget", AssetIDType.Boolean);
                builtin_vars.Add("chardead", AssetIDType.Boolean);
                builtin_vars.Add("targeted", AssetIDType.Boolean);
                builtin_vars.Add("havechar", AssetIDType.Boolean);
                builtin_vars.Add("noreturn", AssetIDType.Boolean);
                builtin_vars.Add("timeron", AssetIDType.Boolean);
                builtin_vars.Add("flash", AssetIDType.Boolean);
                builtin_vars.Add("mercydraw", AssetIDType.Boolean);
                builtin_vars.Add("tireddraw", AssetIDType.Boolean);
                builtin_vars.Add("pacify_glow", AssetIDType.Boolean);
                builtin_vars.Add("drawsus", AssetIDType.Boolean);
                builtin_vars.Add("drawral", AssetIDType.Boolean);
                builtin_vars.Add("susblend", AssetIDType.Color);
                builtin_vars.Add("ralblend", AssetIDType.Color);
                builtin_vars.Add("hurt", AssetIDType.Boolean);
                builtin_vars.Add("skipme", AssetIDType.Boolean);
                builtin_vars.Add("darken", AssetIDType.Boolean);
                builtin_vars.Add("combatdarken", AssetIDType.Boolean);
                builtin_vars.Add("stepped", AssetIDType.Boolean);
                //warned being a bool is probably mostly correct.
                builtin_vars.Add("warned", AssetIDType.Boolean);
                builtin_vars.Add("tired", AssetIDType.Boolean);
                builtin_vars.Add("fixed", AssetIDType.Boolean);
                builtin_vars.Add("nexttry", AssetIDType.Boolean);
                builtin_vars.Add("floating", AssetIDType.Boolean);
                builtin_vars.Add("bodyfade", AssetIDType.Boolean);
                builtin_vars.Add("selected", AssetIDType.Boolean);
                builtin_vars.Add("hurk", AssetIDType.Boolean);
                builtin_vars.Add("persistent", AssetIDType.Boolean);
                builtin_vars.Add("dhaver", AssetIDType.Boolean);
                builtin_vars.Add("walk", AssetIDType.Boolean);
                builtin_vars.Add("fun", AssetIDType.Boolean);
                builtin_vars.Add("runmove", AssetIDType.Boolean);
                builtin_vars.Add("frozen", AssetIDType.Boolean);
                builtin_vars.Add("hadfrozen", AssetIDType.Boolean);
                builtin_vars.Add("offscreen_frozen", AssetIDType.Boolean);
                builtin_vars.Add("ignoresolid", AssetIDType.Boolean);
                builtin_vars.Add("eraser", AssetIDType.Boolean);
                builtin_vars.Add("bikeflip", AssetIDType.Boolean);
                builtin_vars.Add("checked", AssetIDType.Boolean);
                builtin_vars.Add("secondtime", AssetIDType.Boolean);
                builtin_vars.Add("ralsei_lecture", AssetIDType.Boolean);
                builtin_vars.Add("choiced", AssetIDType.Boolean);
                builtin_vars.Add("FINISH", AssetIDType.Boolean);
                builtin_vars.Add("LOCK", AssetIDType.Boolean);
                builtin_vars.Add("locked", AssetIDType.Boolean);
                builtin_vars.Add("ERASE", AssetIDType.Boolean);
                builtin_vars.Add("fastmode", AssetIDType.Boolean);
                builtin_vars.Add("fadeplease", AssetIDType.Boolean);
                builtin_vars.Add("active", AssetIDType.Boolean);
                builtin_vars.Add("alpha_changed", AssetIDType.Boolean);
                builtin_vars.Add("charinstance", AssetIDType.GameObject);
                builtin_vars.Add("reset", AssetIDType.Boolean);
                // globals pertaining to monsters in Deltarune
                builtin_vars.Add("monsterstatus", AssetIDType.Boolean);
                builtin_vars.Add("monster", AssetIDType.Boolean);
                // Cutscene
                builtin_vars.Add("cutscene", AssetIDType.Boolean);
                builtin_vars.Add("black", AssetIDType.Boolean);
                builtin_vars.Add("monsterinstancetype", AssetIDType.GameObject);
                //builtin_vars.Add("itemed", AssetIDType.Boolean);
                //builtin_vars.Add("itemed", AssetIDType.Boolean);
                //builtin_vars.Add("itemed", AssetIDType.Boolean);
                //builtin_vars.Add("itemed", AssetIDType.Boolean);
                //Undertale
                builtin_vars.Add("background_color", AssetIDType.Color);
                builtin_vars.Add("myblend", AssetIDType.Color);
                builtin_vars.Add("object0", AssetIDType.GameObject);
                builtin_vars.Add("part1", AssetIDType.GameObject);
                builtin_vars.Add("pap", AssetIDType.GameObject);
                builtin_vars.Add("fileerased", AssetIDType.Sprite);
                builtin_vars.Add("catty", AssetIDType.GameObject);
                builtin_vars.Add("bratty", AssetIDType.GameObject);
                builtin_vars.Add("creator", AssetIDType.GameObject);
                // It's not 100% accurate to resolve this way but it seems like this variable only gets directly assigned values and is used as a bool, it should be fine.
                builtin_vars.Add("parent", AssetIDType.GameObject);
                // These are not so consistent... ;-;
                // From v1.001 Undertale via comparison
                // A TIER quality
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
                // B TIER quality
                builtin_vars.Add("tspr5", AssetIDType.Sprite);
                builtin_vars.Add("tspr3", AssetIDType.Sprite);
                builtin_vars.Add("tspr2", AssetIDType.Sprite);
                builtin_vars.Add("tspr1", AssetIDType.Sprite);
                builtin_vars.Add("tspr4", AssetIDType.Sprite);
                builtin_vars.Add("snapper", AssetIDType.GameObject);
                builtin_vars.Add("subject", AssetIDType.GameObject);
                builtin_vars.Add("clip", AssetIDType.GameObject);
                // C TIER quality
                builtin_vars.Add("sound1", AssetIDType.Sound);
                builtin_vars.Add("sound2", AssetIDType.Sound);
            }

            // In 2.3.7+, booleans don't need to be typed.
            // Turn any boolean types (other than overrides) into AssetIDType.Other
            // so integers don't turn into booleans when they shouldn't
            if (data.IsVersionAtLeast(2, 3, 7))
            {
                foreach (KeyValuePair<string, AssetIDType> kvp in builtin_vars)
                {
                    if (kvp.Value == AssetIDType.Boolean)
                        builtin_vars.Remove(kvp.Key);
                }
                foreach (KeyValuePair<string, AssetIDType> kvp in return_types)
                {
                    if (kvp.Value == AssetIDType.Boolean)
                        builtin_vars.Remove(kvp.Key);
                }
                foreach (KeyValuePair<string, AssetIDType[]> kvp in builtin_funcs)
                {
                    AssetIDType[] arr = kvp.Value;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr[i] == AssetIDType.Boolean)
                            arr[i] = AssetIDType.Other;
                    }
                }
            }
        }
    }
}
