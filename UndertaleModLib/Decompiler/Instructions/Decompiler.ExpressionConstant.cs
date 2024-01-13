using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a base constant expression, such as a number (no operations).
    public class ExpressionConstant : Expression
    {
        public object Value;
        public bool IsPushE;
        internal AssetIDType AssetType = AssetIDType.Other;

        public ExpressionConstant(UndertaleInstruction.DataType type, object value, bool isPushE = false)
        {
            Type = type;
            Value = value;
            IsPushE = isPushE;
        }

        internal override bool IsDuplicationSafe()
        {
            return true;
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Value = (Value as Statement)?.CleanStatement(context, block) ?? Value;
            return this;
        }

        public bool EqualsNumber(int TestNumber)
        {
            return (Value is Int16 || Value is Int32) && Convert.ToInt32(Value) == TestNumber;
        }

        // Helper function to carefully check if an object is in fact an integer, for asset types.
        public static int? ConvertToInt(object val)
        {
            if (val is int || val is short || val is ushort || val is UndertaleInstruction.InstanceType)
            {
                return Convert.ToInt32(val);
            }
            else if (val is double)
            {
                var v = Convert.ToDouble(val);
                int res = (int)v;
                if (v == res)
                    return res;
            }
            else if (val is float)
            {
                var v = Convert.ToSingle(val);
                int res = (int)v;
                if (v == res)
                    return res;
            }
            return null;
        }

        // Helper function, using the one above, to convert an object into its respective asset type enum, if possible.
        public static string ConvertToEnumStr<T>(object val)
        {
            int? intVal = ConvertToInt(val);
            if (intVal == null)
                return val.ToString();
            return ((T)(object)intVal).ToString();
        }

        public override string ToString(DecompileContext context)
        {
            if (Value is float f) // More accurate, larger range, double to string.
                return DoubleToString.StringOf(f);

            if (Value is Int64 i && i <= int.MaxValue && i >= int.MinValue) // Decompiler accuracy improvement.
            {
                return "(" + i + " << 0)";
            }

            if (Value is double d) // More accurate, larger range, double to string.
                return DoubleToString.StringOf(d);

            if (Value is Statement statement)
                return statement.ToString(context);

            if (Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> resource) // Export string.
            {
                string resultStr = resource.Resource.ToString(context.GlobalContext.Data?.IsGameMaker2() ?? false);
                if (context.GlobalContext.EnableStringLabels)
                    resultStr += resource.GetMarkerSuffix();
                return resultStr;
            }

            // Archie: If statements are inefficient! Use a switch jump table!
            if (AssetType == AssetIDType.GameObject && !(Value is Int64)) // When the value is Int64, an example value is 343434343434. It is unknown what it represents, but it's not an InstanceType.
            {
                int? val = ConvertToInt(Value);
                if (val != null && val < 0 && val >= -16)
                    return ((UndertaleInstruction.InstanceType)Value).ToString().ToLower(CultureInfo.InvariantCulture);
            }
            else switch (AssetType) // Need to put else because otherwise it gets terribly unoptimized with GameObject type
            {
                case AssetIDType.e__VW:
                    return "e__VW." + ConvertToEnumStr<e__VW>(Value);
                case AssetIDType.e__BG:
                    return "e__BG." + ConvertToEnumStr<e__BG>(Value);

                case AssetIDType.Enum_HAlign:
                    return ConvertToEnumStr<HAlign>(Value);
                case AssetIDType.Enum_VAlign:
                    return ConvertToEnumStr<VAlign>(Value);
                case AssetIDType.Enum_GameSpeed:
                    return ConvertToEnumStr<GameSpeed>(Value);
                case AssetIDType.Enum_OSType:
                    return ConvertToEnumStr<OSType>(Value);
                case AssetIDType.Enum_GamepadButton:
                    return ConvertToEnumStr<GamepadButton>(Value);
                case AssetIDType.MouseButton:
                    return ConvertToEnumStr<MouseButton>(Value);
                case AssetIDType.Enum_MouseCursor:
                    return ConvertToEnumStr<MouseCursor>(Value);
                case AssetIDType.Enum_PathEndAction:
                    return ConvertToEnumStr<PathEndAction>(Value);
                case AssetIDType.Enum_BufferKind:
                    return ConvertToEnumStr<BufferKind>(Value);
                case AssetIDType.Enum_BufferType:
                    return ConvertToEnumStr<BufferType>(Value);
                case AssetIDType.Enum_BufferSeek:
                    return ConvertToEnumStr<BufferSeek>(Value);
                case AssetIDType.Enum_Steam_UGC_FileType:
                    return ConvertToEnumStr<Steam_UGC_FileType>(Value);
                case AssetIDType.Enum_Steam_UGC_List:
                    return ConvertToEnumStr<Steam_UGC_List>(Value);
                case AssetIDType.Enum_Steam_UGC_MatchType:
                    return ConvertToEnumStr<Steam_UGC_MatchType>(Value);
                case AssetIDType.Enum_Steam_UGC_QueryType:
                    return ConvertToEnumStr<Steam_UGC_QueryType>(Value);
                case AssetIDType.Enum_Steam_UGC_SortOrder:
                    return ConvertToEnumStr<Steam_UGC_SortOrder>(Value);
                case AssetIDType.Enum_Steam_Overlay:
                    return ConvertToEnumStr<Steam_Overlay>(Value);
                case AssetIDType.Enum_Steam_LeaderBoard_Display:
                    return ConvertToEnumStr<Steam_LeaderBoard_Display>(Value);
                case AssetIDType.Enum_Steam_LeaderBoard_Sort:
                    return ConvertToEnumStr<Steam_LeaderBoard_Sort>(Value);
                case AssetIDType.Boolean:
                    return ConvertToEnumStr<Boolean>(Value);
                case AssetIDType.EventType:
                    return ConvertToEnumStr<Enum_EventType>(Value);
                case AssetIDType.ContextDependent:
                {
                    var func = context.currentFunction;
                    if (func != null && (ContextualAssetResolver.resolvers?.ContainsKey(func.Function.Name.Content) ?? false))
                    {
                        List<Expression> actualArguments = new List<Expression>();
                        foreach (var arg in func.Arguments)
                        {
                            if (arg is ExpressionCast)
                                actualArguments.Add((arg as ExpressionCast).Argument);
                            else
                                actualArguments.Add(arg);
                        }
                        string result = ContextualAssetResolver.resolvers[func.Function.Name.Content](context, func, actualArguments.IndexOf(this), this);
                        if (result != null)
                            return result;
                    }
                }
                    break;

                case AssetIDType.Color:
                    if (Value is IFormattable formattable && !(Value is float) && !(Value is double) && !(Value is decimal))
                    {
                        int vint = Convert.ToInt32(Value);
                        if (vint < 0) // negative value.
                            return vint.ToString();
                        else // guaranteed to be an unsigned int.
                        {
                            uint vuint = (uint)vint;
                            if (Decompiler.ColorDictionary.ContainsKey(vuint))
                                return Decompiler.ColorDictionary[vuint];
                            else
                                return (context.GlobalContext.Data?.IsGameMaker2() ?? false ? "0x" : "$") + formattable.ToString("X6", CultureInfo.InvariantCulture); // not a known color and not negative.
                        }
                    }
                    break;

                case AssetIDType.KeyboardKey:
                {
                    string key = GetAsKeyboard(context);
                    if (key != null)
                        return key;
                }
                    break;
                // Don't use this.
                // It will not recompile.
                case AssetIDType.Macro:
                    throw new NotImplementedException();/*
                        {
                            var macros = ContextualAssetResolver.macros;
                            var key = Value?.ToString();

                            if (key != null & macros.ContainsKey(key))
                                return macros[key];
                        }
                        break;*/
            }

            if ((context.AssetResolutionEnabled || AssetType == AssetIDType.Script) && context.GlobalContext.Data != null && AssetType != AssetIDType.Other)
            {
                IList assetList = null;
                switch (AssetType)
                {
                    case AssetIDType.Sprite:
                        assetList = (IList)context.GlobalContext.Data.Sprites;
                        break;
                    case AssetIDType.TileSet:
                    case AssetIDType.Background:
                        assetList = (IList)context.GlobalContext.Data.Backgrounds;
                        break;
                    case AssetIDType.Sound:
                        assetList = (IList)context.GlobalContext.Data.Sounds;
                        break;
                    case AssetIDType.Font:
                        assetList = (IList)context.GlobalContext.Data.Fonts;
                        break;
                    case AssetIDType.Path:
                        assetList = (IList)context.GlobalContext.Data.Paths;
                        break;
                    case AssetIDType.Timeline:
                        assetList = (IList)context.GlobalContext.Data.Timelines;
                        break;
                    case AssetIDType.Room:
                        assetList = (IList)context.GlobalContext.Data.Rooms;
                        break;
                    case AssetIDType.GameObject:
                        assetList = (IList)context.GlobalContext.Data.GameObjects;
                        break;
                    case AssetIDType.Shader:
                        assetList = (IList)context.GlobalContext.Data.Shaders;
                        break;
                    case AssetIDType.Script:
                        assetList = (IList)context.GlobalContext.Data.Scripts;
                        break;
                }

                if (!(Value is Int64)) // It is unknown what Int64 data represents, but it's not this.
                {
                    int? tryVal = ConvertToInt(Value);
                    int val;
                    if (tryVal != null)
                    {
                        val = tryVal ?? -1;
                        if (assetList != null && val >= 0 && val < assetList.Count)
                            return ((UndertaleNamedResource)assetList[val]).Name.Content;
                    }
                }
            }

            return ((Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? Value.ToString());
        }

        // Helper function
        public string GetAsKeyboard(DecompileContext context)
        {
            int? tryVal = ConvertToInt(Value);
            if (tryVal != null)
            {
                int val = tryVal ?? -1;

                bool isAlphaNumeric = val >= (int)EventSubtypeKey.Digit0 && val <= (int)EventSubtypeKey.Z;
                if (isAlphaNumeric)
                    return "ord(\"" + (char)val + "\")";

                if (val >= 0 && Enum.IsDefined(typeof(EventSubtypeKey), (uint)val))
                    return ((EventSubtypeKey)val).ToString(); // Either return the key enum, or the right alpha-numeric key-press.

                if (!Char.IsControl((char)val) && !Char.IsLower((char)val) && val > 0) // The special keys overlay with the uppercase letters (ugh)
                    return "ord(" + (((char)val) == '\'' ? (context.GlobalContext.Data?.IsGameMaker2() ?? false ? "\"\\\"\"" : "'\"'")
                        : (((char)val) == '\\' ? (context.GlobalContext.Data?.IsGameMaker2() ?? false ? "\"\\\\\"" : "\"\\\"")
                            : "\"" + (char)val + "\"")) + ")";
            }
            return null;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            if (AssetType == AssetIDType.Other)
                AssetType = suggestedType;
            return AssetType;
        }
    }
}