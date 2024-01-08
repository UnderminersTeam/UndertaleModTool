using System;
using System.Collections.Generic;
using System.Globalization;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public class ContextualAssetResolver
    {
        // Is there a better way to do this?
        // Probably
        public static Dictionary<string, Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string>> resolvers;
        public static Dictionary<string, Func<DecompileContext, string, object, string>> variable_resolvers;

        internal static Dictionary<Enum_EventType, Dictionary<int, string>> event_subtypes;
        internal static Dictionary<int, string> blend_modes, gamepad_controls;
        internal static Dictionary<string, string> macros;

        public static void Initialize(UndertaleData data)
        {
            // TODO: make this nicer by not hacking
            // into the builtin list
            event_subtypes = new Dictionary<Enum_EventType, Dictionary<int, string>>();
            gamepad_controls = new Dictionary<int, string>();
            blend_modes = new Dictionary<int, string>();
            macros = new Dictionary<string, string>();

            // Don't use
            // Error because of loading audiogroup 
            if (data.GeneralInfo != null)
            {   
                if (data.GeneralInfo.BytecodeVersion <= 14)
                {
                    foreach (var constant in data.Options.Constants)
                    {
                        if (!constant.Name.Content.StartsWith("@@", StringComparison.InvariantCulture))
                            macros[constant.Value.Content] = constant.Name.Content;
                    }
                }
            }

            var builtins = data.BuiltinList;
            var constants = builtins.Constants;

            Func<string, Enum_EventType> GetEventTypeFromSubtype = (string subtype) =>
            {
                if (subtype.Contains("gesture", StringComparison.InvariantCulture))
                    return Enum_EventType.ev_gesture;
                if (subtype.Contains("gui", StringComparison.InvariantCulture) || subtype.Contains("draw", StringComparison.InvariantCulture)) // DrawGUI events are apparently just prefixed with ev_gui...
                    return Enum_EventType.ev_draw;
                if (subtype.Contains("step", StringComparison.InvariantCulture))
                    return Enum_EventType.ev_step;
                // End me
                if (subtype.Contains("user", StringComparison.InvariantCulture) || subtype.Contains("game_", StringComparison.InvariantCulture) ||
                   subtype.Contains("room_", StringComparison.InvariantCulture) || subtype.Contains("animation_end", StringComparison.InvariantCulture) ||
                   subtype.Contains("lives", StringComparison.InvariantCulture) || subtype.Contains("end_of_path", StringComparison.InvariantCulture) ||
                   subtype.Contains("health", StringComparison.InvariantCulture) || subtype.Contains("close_button", StringComparison.InvariantCulture) ||
                   subtype.Contains("outside", StringComparison.InvariantCulture) || subtype.Contains("boundary", StringComparison.InvariantCulture))
                    return Enum_EventType.ev_other;

                // ev_close_button is handled above and the various joystick events are 
                // skipped in the loop
                if (subtype.Contains("button", StringComparison.InvariantCulture) || subtype.Contains("mouse", StringComparison.InvariantCulture) ||
                    subtype.Contains("global", StringComparison.InvariantCulture) || subtype.Contains("press", StringComparison.InvariantCulture) ||
                    subtype.Contains("release", StringComparison.InvariantCulture))
                    return Enum_EventType.ev_mouse;


                // Note: events with arbitrary subtypes (keyboard, create, precreate, destroy, etc)
                // are not handled here.
                // It also appears to be impossible to manually trigger joystick events?

                // idk what exception to use
                throw new NotImplementedException("No event type for subtype " + subtype);
            };

            Func<Enum_EventType, Dictionary<int, string>> GetDictForEventType = (Enum_EventType type) =>
            {
                // These 3 resolve to the same thing
                if (type == Enum_EventType.ev_keypress || type == Enum_EventType.ev_keyrelease)
                    type = Enum_EventType.ev_keyboard;

                Dictionary<int, string> subtypes;
                if (!event_subtypes.TryGetValue(type, out subtypes))
                    event_subtypes[type] = (subtypes = new Dictionary<int, string>());

                return subtypes;
            };

            // This is going to get bulky really quickly
            foreach (string constant in constants.Keys)
            {
                if (constant.StartsWith("vk_", StringComparison.InvariantCulture))
                    GetDictForEventType(Enum_EventType.ev_keyboard)[(int)constants[constant]] = constant;
                else if (constant.StartsWith("bm_") && !constant.Contains("colour", StringComparison.InvariantCulture))
                    blend_modes[(int)constants[constant]] = constant;
                else if (constant.StartsWith("gp_", StringComparison.InvariantCulture))
                    gamepad_controls[(int)constants[constant]] = constant;
                else if (constant.StartsWith("ev_") && !Enum.IsDefined(typeof(Enum_EventType), constant) && !constant.Contains("joystick", StringComparison.InvariantCulture))
                    GetDictForEventType(GetEventTypeFromSubtype(constant))[(int)constants[constant]] = constant;
            }


            // Uncurse this some time
            Func<Decompiler.Expression, Decompiler.ExpressionConstant> ConvertToConstExpression = (expr) =>
            {
                if (expr is Decompiler.ExpressionCast)
                    expr = (expr as Decompiler.ExpressionCast).Argument;

                if (expr is Decompiler.ExpressionConstant)
                    return expr as Decompiler.ExpressionConstant;

                return null;
            };

            Func<Decompiler.Expression, int?> GetTypeInt = (expr) =>
            {
                var constExpr = ConvertToConstExpression(expr);

                if (constExpr == null)
                    return null;

                return AssetTypeResolver.FindConstValue(Decompiler.ExpressionConstant.ConvertToEnumStr<Enum_EventType>(constExpr.Value));
            };

            Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string> resolve_event_perform = (context, func, index, self) =>
            {
                int? typeInt = GetTypeInt(func.Arguments[index - 1]);

                if(typeInt != null)
                {
                    Enum_EventType type = (Enum_EventType)typeInt;
                    int? initialVal = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                    if (initialVal == null)
                        return null;

                    int val = initialVal.Value;

                    var subtypes = event_subtypes;
                    if (type == Enum_EventType.ev_collision && val >= 0 && val < data.GameObjects.Count)
                        return data.GameObjects[val].Name.Content;
                    else if (type == Enum_EventType.ev_keyboard || type == Enum_EventType.ev_keypress || type == Enum_EventType.ev_keyrelease)
                    {
                        string key = self.GetAsKeyboard(context);
                        if (key != null)
                            return key;
                    }
                    else if (subtypes.TryGetValue(type, out var mappings))
                    {
                        if (mappings.TryGetValue(val, out var str))
                            return str;
                    }
                }

                return null;
            };

            // TODO: Finish context-dependent variable resolution
            variable_resolvers = new Dictionary<string, Func<DecompileContext, string, object, string>>()
            {
                { "scr_getbuttonsprite", (context, varname, value) =>
                    {
                        return null;
                    }
                }
            };

            resolvers = new Dictionary<string, Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string>>()
            {
                // TODO: __background* compatibility scripts
                { "event_perform", resolve_event_perform },
                { "event_perform_object", resolve_event_perform },
                { "draw_set_blend_mode", (context, func, index, self) =>
                    {
                        int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                        if (val != null)
                        {
                            switch(val)
                            {
                                case 0: return "bm_normal";
                                case 1: return "bm_add";
                                case 2: return "bm_max";
                                case 3: return "bm_subtract";
                            }
                        }
                        return null;
                    }
                },
                { "gpu_set_blendmode", (context, func, index, self) =>
                    {
                        int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                        if (val != null)
                        {
                            switch(val)
                            {
                                case 0: return "bm_normal";
                                case 1: return "bm_add";
                                case 2: return "bm_max";
                                case 3: return "bm_subtract";
                            }
                        }
                        return null;
                    }
                },
                { "draw_set_blend_mode_ext", (context, func, index, self) =>
                    {
                        int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                        if (val == null)
                            return null;

                        if (blend_modes.TryGetValue(val.Value, out var mode))
                            return mode;
                        
                        return null;
                    }
                },
                { "gpu_set_blendmode_ext", (context, func, index, self) =>
                    {
                        int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                        if (val == null)
                            return null;

                        if (blend_modes.TryGetValue(val.Value, out var mode))
                            return mode;

                        return null;
                    }
                },
                { "__view_set", (context, func, index, self) => 
                    {
                        var first = ConvertToConstExpression(func.Arguments[0]);
                        if (first == null)
                            return null;

                        int type = Decompiler.ExpressionConstant.ConvertToInt(first.Value) ?? -1;
                        int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);

                        if (val == null)
                            return null;

                        switch(type)
                        {
                            case 9:
                                {
                                    if (val < 0)
                                        return ((UndertaleInstruction.InstanceType)self.Value).ToString().ToLower(CultureInfo.InvariantCulture);
                                    else if (val < data.GameObjects.Count)
                                        return data.GameObjects[val.Value].Name.Content;
                                    
                                } break;

                            case 10:
                                {
                                    if (val == 0)
                                        return "false";
                                    else if (val == 1)
                                        return "true";
                                } break;
                        }

                        return null;
                    }
                },
            };
        }
    }
}
