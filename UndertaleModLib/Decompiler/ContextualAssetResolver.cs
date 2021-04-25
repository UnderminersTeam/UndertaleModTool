using System;
using System.Collections.Generic;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public class ContextualAssetResolver
    {
        public static BuiltinList builtins;

        // Is there a better way to do this?
        // Probably
        public static Dictionary<string, Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string>> resolvers;

        internal static Dictionary<EventType, Dictionary<int, string>> event_subtypes;
        internal static Dictionary<int, string> blend_modes;

        public static void Initialize(UndertaleData data)
        {
            // TODO: make this nicer by not hacking
            // into the builtin list
            event_subtypes = new Dictionary<EventType, Dictionary<int, string>>();
            blend_modes = new Dictionary<int, string>();
            builtins = new BuiltinList(data);

            var constants = builtins.Constants;

            Func<string, EventType> GetEventTypeFromSubtype = (string subtype) =>
            {
                if (subtype.Contains("gesture"))
                    return EventType.ev_gesture;
                if (subtype.Contains("gui") || subtype.Contains("draw")) // DrawGUI events are apparently just prefixed with ev_gui...
                    return EventType.ev_draw;
                if (subtype.Contains("step"))
                    return EventType.ev_step;
                // End me
                if (subtype.Contains("user") || subtype.Contains("game_") ||
                   subtype.Contains("room_") || subtype.Contains("animation_end") ||
                   subtype.Contains("lives") || subtype.Contains("end_of_path") ||
                   subtype.Contains("health") || subtype.Contains("close_button") ||
                   subtype.Contains("outside") || subtype.Contains("boundary"))
                    return EventType.ev_other;

                // ev_close_button is handled above and the various joystick events are 
                // skipped in the loop
                if (subtype.Contains("button") || subtype.Contains("mouse") ||
                    subtype.Contains("global") || subtype.Contains("press") ||
                    subtype.Contains("release"))
                    return EventType.ev_mouse;


                // Note: events with arbitrary subtypes (keyboard, create, precreate, destroy, etc)
                // are not handled here.
                // It also appears to be impossible to manually trigger joystick events?

                // idk what exception to use
                throw new NotImplementedException("No event type for subtype " + subtype);
            };

            Func<EventType, Dictionary<int, string>> GetDictForEventType = (EventType type) =>
            {
                // These 3 resolve to the same thing
                if (type == EventType.ev_keypress || type == EventType.ev_keyrelease)
                    type = EventType.ev_keyboard;

                if (!event_subtypes.ContainsKey(type))
                    event_subtypes[type] = new Dictionary<int, string>();

                return event_subtypes[type];
            };

            foreach (string constant in constants.Keys)
            {
                if (constant.StartsWith("vk_"))
                    GetDictForEventType(EventType.ev_keyboard)[(int)constants[constant]] = constant;
                else if (constant.StartsWith("bm_") && !constant.Contains("colour"))
                    blend_modes[(int)constants[constant]] = constant;
                else if (constant.StartsWith("ev_") && !Enum.IsDefined(typeof(EventType), constant) && !constant.Contains("joystick"))
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

                return AssetTypeResolver.FindConstValue(Decompiler.ExpressionConstant.ConvertToEnumStr<EventType>(constExpr.Value));
            };

            Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string> resolve_event_perform = (context, func, index, self) =>
            {
                int? typeInt = GetTypeInt(func.Arguments[index - 1]);

                if(typeInt != null)
                {
                    EventType type = (EventType)typeInt;
                    int? initialVal = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                    if (initialVal == null)
                        return null;

                    int val = (int)initialVal.Value;

                    var subtypes = event_subtypes;
                    if (type == EventType.ev_collision && val >= 0 && val < data.GameObjects.Count)
                        return data.GameObjects[(int)val].Name.Content;
                    else if (type == EventType.ev_keyboard || type == EventType.ev_keypress || type == EventType.ev_keyrelease)
                    {
                        string key = self.GetAsKeyboard(context);
                        if (key != null)
                            return key;
                    }
                    else if (subtypes.ContainsKey(type))
                    {
                        var mappings = subtypes[type];
                        if (mappings.ContainsKey(val))
                            return mappings[val];
                    }
                }

                return null;
            };

            Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string> resolve_blend_mode = (context, func, index, self) => 
            {
                int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                if (val == null)
                    return null;

                return blend_modes.ContainsKey(val.Value) ? blend_modes[val.Value] : null;
            };

            resolvers = new Dictionary<string, Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string>>()
            {
                // TODO: __background* compatibility scripts
                { "event_perform", resolve_event_perform },
                { "event_perform_object", resolve_event_perform },
                { "draw_set_blend_mode", resolve_blend_mode },
                { "draw_set_blend_mode_ext", resolve_blend_mode },
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
                                        return ((UndertaleInstruction.InstanceType)self.Value).ToString().ToLower();
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
