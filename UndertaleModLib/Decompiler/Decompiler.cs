using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using static UndertaleModLib.Decompiler.Decompiler;

namespace UndertaleModLib.Decompiler
{
    /// <summary>
    /// The DecompileContext is global for the entire decompilation run, or possibly multiple runs. It caches the decompilation results which don't change often
    /// to speedup decompilation.
    /// </summary>
    public class GlobalDecompileContext
    {
        public UndertaleData Data;

        public bool EnableStringLabels;

        public List<string> DecompilerWarnings = new List<string>();

        /// <summary>
        /// A cache of resolved function argument types. This is kept here because decompiling is slow, and there is no need to do it every time
        /// unless the code has changed.
        /// </summary>
        public Dictionary<string, AssetIDType[]> ScriptArgsCache = new Dictionary<string, AssetIDType[]>();

        /// <summary>
        /// A cache of function to actual name mapping. GMS2.3+ sometimes (usually when dealing with global scripts) calls method functions
        /// using the legacy call operator, passing the anonymous function directly. This dictionary contains a map from UndertaleFunction
        /// to its actual name, obtained by decompiling the parent CodeObject and looking for the assignment to global variable with function
        /// name.
        /// </summary>
        public Dictionary<UndertaleFunction, string> AnonymousFunctionNameCache = new Dictionary<UndertaleFunction, string>();

        public GlobalDecompileContext(UndertaleData data, bool enableStringLabels)
        {
            this.Data = data;
            this.EnableStringLabels = enableStringLabels;
        }

        public void ClearDecompilationCache()
        {
            // This will not be done automatically, because it would cause significant slowdown having to recalculate this each time, and there's no reason to reset it if it's decompiling a bunch at once.
            // But, since it is possible to invalidate this data, we add this here so we'll be able to invalidate it if we need to.
            ScriptArgsCache.Clear();
            AnonymousFunctionNameCache.Clear();
        }
    }

    /// <summary>
    /// The DecompileContext is bound to the currently decompiled code block
    /// </summary>
    public class DecompileContext
    {
        public GlobalDecompileContext GlobalContext;
        public UndertaleCode TargetCode;
        public UndertaleGameObject Object;
        public static bool GMS2_3;

        public DecompileContext(GlobalDecompileContext globalContext, UndertaleCode code, bool computeObject = true)
        {
            GlobalContext = globalContext;
            TargetCode = code;

            if (code.ParentEntry != null)
                throw new InvalidOperationException("This code block represents a function nested inside " + code.ParentEntry.Name + " - decompile that instead");

            if (computeObject && globalContext.Data != null)
            {
                // TODO: This is expensive, move it somewhere else as a dictionary
                // and have it update when events/objects are modified.
                foreach (var obj in globalContext.Data.GameObjects)
                    foreach (var event_list in obj.Events)
                        foreach (var subevent in event_list)
                            foreach (var ev in subevent.Actions)
                                if (ev.CodeId == code)
                                {
                                    Object = obj;
                                    goto LoopEnd;
                                }
            }
        LoopEnd: return;
        }

        #region Struct management
        public List<Expression> ArgumentReplacements;
        public bool DecompilingStruct;
        #endregion

        #region Indentation management
        public const string Indent = "    ";
        private int _indentationLevel = 0;
        private string _indentation = "";

        public int IndentationLevel
        {
            get
            {
                return _indentationLevel;
            }
            set
            {
                _indentationLevel = value;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < IndentationLevel; i++)
                {
                    sb.Append(Indent);
                }
                _indentation = sb.ToString();
            }
        }
        public string Indentation => _indentation;
        #endregion

        #region Temp var management
        /// <summary>
        /// Maps a temp var to a place where it was created
        /// </summary>
        public Dictionary<string, TempVarAssignmentStatement> TempVarMap = new Dictionary<string, TempVarAssignmentStatement>();
        /// <summary>
        /// If used for auto-naming temp vars
        /// </summary>
        public int TempVarId { get; private set; }
        public AssignmentStatement CompilerTempVar;

        public TempVar NewTempVar()
        {
            return new TempVar(++TempVarId);
        }
        #endregion

        #region Local var management
        public HashSet<string> LocalVarDefines = new HashSet<string>();
        #endregion

        #region GMS 2.3+ Function management
        /// <summary>
        /// Set containing already-decompiled child code entries.
        /// Used to prevent decompiling the same child entry multiple times.
        /// Only applies to function entries, struct and constructors are unaffected.
        /// </summary>
        public ISet<UndertaleCode> AlreadyProcessed = new HashSet<UndertaleCode>();
        #endregion

        #region Asset type resolution
        /// <summary>
        /// Contains the resolved asset type for every variable
        /// </summary>
        public Dictionary<UndertaleVariable, AssetIDType> assetTypes = new Dictionary<UndertaleVariable, AssetIDType>();
        public DirectFunctionCall currentFunction; // TODO: clean up this hack
        #endregion

        #region Decompilation results
        /// <summary>
        /// Contains the result of decompiling this code block.
        /// This is a map from an entry point address to a list of statements.
        /// Needs to be here to access it in ToString for inline function definitions.
        /// </summary>
        public Dictionary<uint, List<Statement>> Statements { get; internal set; }
        #endregion

        /// <summary>
        /// Allows to disable the anonymous code name resolution to prevent recursion
        /// </summary>
        public bool DisableAnonymousFunctionNameResolution = false;
    }

    public static class Decompiler
    {
        // Color dictionary for resolving.
        public static readonly Dictionary<uint, string> ColorDictionary = new Dictionary<uint, string>
        {
            [16776960] = "c_aqua",
            [0] = "c_black",
            [16711680] = "c_blue",
            [4210752] = "c_dkgray",
            [16711935] = "c_fuchsia",
            [8421504] = "c_gray",
            [32768] = "c_green",
            [65280] = "c_lime",
            //[12632256] = "c_ltgray",
            [128] = "c_maroon",
            [8388608] = "c_navy",
            [32896] = "c_olive",
            [8388736] = "c_purple",
            [255] = "c_red",
            [12632256] = "c_silver",
            [8421376] = "c_teal",
            [16777215] = "c_white", // maximum color value
            [65535] = "c_yellow",
            [4235519] = "c_orange"
        };

        // Represents a block node of instructions from GML bytecode (for control flow).
        public class Block
        {
            public uint? Address;
            public List<UndertaleInstruction> Instructions = new List<UndertaleInstruction>();
            public List<Statement> Statements = null;
            public Expression ConditionStatement = null;
            public bool conditionalExit;
            public Block nextBlockTrue;
            public Block nextBlockFalse;
            public List<Block> entryPoints = new List<Block>();
            internal List<TempVarReference> TempVarsOnEntry;

            public int _CachedIndex;

            public Block(uint? address)
            {
                Address = address;
            }

            public override string ToString()
            {
                return "Block " + Address;
            }
        }

        // Represents all kinds of high-level decompilation results, to be stringified at the end.
        public abstract class Statement
        {
            public abstract string ToString(DecompileContext context);
            internal abstract AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType);
            public abstract Statement CleanStatement(DecompileContext context, BlockHLStatement block);
        }

        // Represents all expressions.
        public abstract class Expression : Statement
        {
            public UndertaleInstruction.DataType Type;
            public bool WasDuplicated = false;

            // Helper function to convert opcode operations to "printable" strings.
            public static string OperationToPrintableString(UndertaleInstruction.Opcode op)
            {
                return op switch
                {
                    UndertaleInstruction.Opcode.Mul => "*",
                    UndertaleInstruction.Opcode.Div => "/",
                    UndertaleInstruction.Opcode.Rem => "div",
                    UndertaleInstruction.Opcode.Mod => "%",
                    UndertaleInstruction.Opcode.Add => "+",
                    UndertaleInstruction.Opcode.Sub => "-",
                    UndertaleInstruction.Opcode.And => "&",
                    UndertaleInstruction.Opcode.Or => "|",
                    UndertaleInstruction.Opcode.Xor => "^",
                    UndertaleInstruction.Opcode.Neg => "-",
                    UndertaleInstruction.Opcode.Not => "~",
                    UndertaleInstruction.Opcode.Shl => "<<",
                    UndertaleInstruction.Opcode.Shr => ">>",
                    _ => op.ToString().ToUpper(CultureInfo.InvariantCulture),
                };
            }

            // Helper function to convert opcode comparisons to "printable" strings.
            public static string OperationToPrintableString(UndertaleInstruction.ComparisonType op)
            {
                return op switch
                {
                    UndertaleInstruction.ComparisonType.LT => "<",
                    UndertaleInstruction.ComparisonType.LTE => "<=",
                    UndertaleInstruction.ComparisonType.EQ => "==",
                    UndertaleInstruction.ComparisonType.NEQ => "!=",
                    UndertaleInstruction.ComparisonType.GTE => ">=",
                    UndertaleInstruction.ComparisonType.GT => ">",
                    _ => op.ToString().ToUpper(CultureInfo.InvariantCulture),
                };
            }

            internal virtual bool IsDuplicationSafe()
            {
                return false;
            }

            public Expression CleanExpression(DecompileContext context, BlockHLStatement block)
            {
                return CleanStatement(context, block) as Expression;
            }
        }

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
                    return RoundTrip.ToRoundTrip(f);

                if (Value is Int64 i && i <= int.MaxValue && i >= int.MinValue) // Decompiler accuracy improvement.
                {
                    return "(" + i + " << 0)";
                }

                if (Value is double d) // More accurate, larger range, double to string.
                    return RoundTrip.ToRoundTrip(d);

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

                if (context.GlobalContext.Data != null && AssetType != AssetIDType.Other)
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

        // Represents an expression converted to one of another data type - makes no difference on high-level code.
        public class ExpressionCast : Expression
        {
            public Expression Argument;

            public ExpressionCast(UndertaleInstruction.DataType targetType, Expression argument)
            {
                this.Type = targetType;
                this.Argument = argument;
            }

            internal override bool IsDuplicationSafe()
            {
                return Argument.IsDuplicationSafe();
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Argument = Argument?.CleanExpression(context, block);
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                return Argument.ToString(context);
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Argument.DoTypePropagation(context, suggestedType);
            }
        }

        // Represents a unary expression.
        public class ExpressionOne : Expression
        {
            public UndertaleInstruction.Opcode Opcode;
            public Expression Argument;

            public ExpressionOne(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, Expression argument)
            {
                this.Opcode = opcode;
                this.Type = targetType;
                this.Argument = argument;
            }

            internal override bool IsDuplicationSafe()
            {
                return Argument.IsDuplicationSafe();
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Argument = Argument?.CleanExpression(context, block);
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                string op = OperationToPrintableString(Opcode);
                if (Opcode == UndertaleInstruction.Opcode.Not && Type == UndertaleInstruction.DataType.Boolean)
                    op = "!"; // This is a logical negation instead, see #93
                string arg = Argument.ToString(context);
                if (arg.Contains(' ', StringComparison.InvariantCulture))
                    return String.Format("({0}({1}))", op, arg);
                return String.Format("({0}{1})", op, arg);
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Argument.DoTypePropagation(context, suggestedType);
            }
        }

        // This is basically ExpressionTwo, but allows for using symbols like && or || without creating new opcodes.
        public class ExpressionTernary : Expression
        {
            public Expression Condition;
            public Expression TrueExpression;
            public Expression FalseExpression;

            public ExpressionTernary(UndertaleInstruction.DataType targetType, Expression Condition, Expression argument1, Expression argument2)
            {
                this.Type = targetType;
                this.Condition = Condition;
                this.TrueExpression = argument1;
                this.FalseExpression = argument2;
            }

            internal override bool IsDuplicationSafe()
            {
                return Condition.IsDuplicationSafe() && TrueExpression.IsDuplicationSafe() && FalseExpression.IsDuplicationSafe();
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Condition = Condition?.CleanExpression(context, block);
                TrueExpression = TrueExpression?.CleanExpression(context, block);
                FalseExpression = FalseExpression?.CleanExpression(context, block);
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                string condStr = Condition.ToString(context);
                if (TestNumber(TrueExpression, 1) && TestNumber(FalseExpression, 0))
                    return condStr; // Default values, yes = true, no = false.

                return "(" + condStr + " ? " + TrueExpression.ToString(context) + " : " + FalseExpression.ToString(context) + ")";
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                // The most likely, but probably rarely happens
                AssetIDType t = TrueExpression.DoTypePropagation(context, suggestedType);
                FalseExpression.DoTypePropagation(context, AssetIDType.Other);
                return t;
            }
        }

        // Represents post increments and decrements, such as a++ and a--.
        public class ExpressionPost : Expression
        {
            public UndertaleInstruction.Opcode Opcode;
            public Expression Variable;

            public ExpressionPost(UndertaleInstruction.Opcode opcode, Expression variable)
            {
                Opcode = opcode;
                Variable = variable;
            }

            internal override bool IsDuplicationSafe()
            {
                return Variable.IsDuplicationSafe();
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Variable = Variable?.CleanStatement(context, block) as Expression;
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                return Variable.ToString(context) + (Opcode == UndertaleInstruction.Opcode.Add ? "++" : "--");
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Variable.DoTypePropagation(context, suggestedType);
            }
        }

        // Represents pre increments and decrements, such as ++a and --a.
        public class ExpressionPre : Expression
        {
            public UndertaleInstruction.Opcode Opcode;
            public Expression Variable;

            public ExpressionPre(UndertaleInstruction.Opcode opcode, Expression variable)
            {
                Opcode = opcode;
                Variable = variable;
            }

            internal override bool IsDuplicationSafe()
            {
                return Variable.IsDuplicationSafe();
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Variable = Variable?.CleanExpression(context, block);
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                return (Opcode == UndertaleInstruction.Opcode.Add ? "++" : "--") + Variable.ToString(context);
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Variable.DoTypePropagation(context, suggestedType);
            }
        }

        // This is basically ExpressionTwo, but allows for using symbols like && or || without creating new opcodes.
        public class ExpressionTwoSymbol : Expression
        {
            public string Symbol;
            public Expression Argument1;
            public Expression Argument2;

            public ExpressionTwoSymbol(string symbol, UndertaleInstruction.DataType targetType, Expression argument1, Expression argument2)
            {
                this.Symbol = symbol;
                this.Type = targetType;
                this.Argument1 = argument1;
                this.Argument2 = argument2;
            }

            internal override bool IsDuplicationSafe()
            {
                return Argument1.IsDuplicationSafe() && Argument2.IsDuplicationSafe();
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Argument1 = Argument1?.CleanExpression(context, block);
                Argument2 = Argument2?.CleanExpression(context, block);
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                string arg1;
                if (Argument1 is ExpressionTwoSymbol && (Argument1 as ExpressionTwoSymbol).Symbol == Symbol)
                    arg1 = (Argument1 as ExpressionTwoSymbol).ToStringNoParen(context);
                else
                    arg1 = Argument1.ToString(context);
                string arg2;
                if (Argument2 is ExpressionTwoSymbol && (Argument2 as ExpressionTwoSymbol).Symbol == Symbol)
                    arg2 = (Argument2 as ExpressionTwoSymbol).ToStringNoParen(context);
                else
                    arg2 = Argument2.ToString(context);
                return String.Format("({0} {1} {2})", arg1, Symbol, arg2);
            }

            public string ToStringNoParen(DecompileContext context)
            {
                string arg1;
                if (Argument1 is ExpressionTwoSymbol && (Argument1 as ExpressionTwoSymbol).Symbol == Symbol)
                    arg1 = (Argument1 as ExpressionTwoSymbol).ToStringNoParen(context);
                else
                    arg1 = Argument1.ToString(context);
                string arg2;
                if (Argument2 is ExpressionTwoSymbol && (Argument2 as ExpressionTwoSymbol).Symbol == Symbol)
                    arg2 = (Argument2 as ExpressionTwoSymbol).ToStringNoParen(context);
                else
                    arg2 = Argument2.ToString(context);
                return String.Format("{0} {1} {2}", arg1, Symbol, arg2);
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                // The most likely, but probably rarely happens
                AssetIDType t = Argument1.DoTypePropagation(context, suggestedType);
                Argument2.DoTypePropagation(context, AssetIDType.Other);
                return t;
            }
        }

        // Represents a binary expression.
        public class ExpressionTwo : Expression
        {
            public UndertaleInstruction.Opcode Opcode;
            public UndertaleInstruction.DataType Type2;
            public Expression Argument1;
            public Expression Argument2;

            public ExpressionTwo(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, UndertaleInstruction.DataType type2, Expression argument1, Expression argument2)
            {
                this.Opcode = opcode;
                this.Type = targetType;
                this.Type2 = type2;
                this.Argument1 = argument1;
                this.Argument2 = argument2;
            }

            internal override bool IsDuplicationSafe()
            {
                return Argument1.IsDuplicationSafe() && Argument2.IsDuplicationSafe();
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Argument1 = Argument1?.CleanExpression(context, block);
                Argument2 = Argument2?.CleanExpression(context, block);
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                if (Opcode == UndertaleInstruction.Opcode.Or || Opcode == UndertaleInstruction.Opcode.And)
                {
                    // If both arguments are a boolean type, this is a non-short-circuited logical condition
                    if (Type == UndertaleInstruction.DataType.Boolean && Type2 == UndertaleInstruction.DataType.Boolean)
                        return String.Format("({0} {1}{1} {2})", Argument1.ToString(context), OperationToPrintableString(Opcode), Argument2.ToString(context));
                }
                return String.Format("({0} {1} {2})", Argument1.ToString(context), OperationToPrintableString(Opcode), Argument2.ToString(context));
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                // The most likely, but probably rarely happens
                AssetIDType t = Argument1.DoTypePropagation(context, suggestedType);
                Argument2.DoTypePropagation(context, AssetIDType.Other);
                return t;
            }
        }

        // Represents a binary comparison expression.
        public class ExpressionCompare : Expression
        {
            public UndertaleInstruction.ComparisonType Opcode;
            public Expression Argument1;
            public Expression Argument2;

            public ExpressionCompare(UndertaleInstruction.ComparisonType opcode, Expression argument1, Expression argument2)
            {
                this.Opcode = opcode;
                this.Type = UndertaleInstruction.DataType.Boolean;
                this.Argument1 = argument1;
                this.Argument2 = argument2;
            }

            internal override bool IsDuplicationSafe()
            {
                return Argument1.IsDuplicationSafe() && Argument2.IsDuplicationSafe();
            }

            public override string ToString(DecompileContext context)
            {
                string arg1, arg2;
                if (Argument1 is ExpressionCompare)
                    arg1 = (Argument1 as ExpressionCompare).ToStringWithParen(context);
                else
                    arg1 = Argument1.ToString(context);
                if (Argument2 is ExpressionCompare)
                    arg2 = (Argument2 as ExpressionCompare).ToStringWithParen(context);
                else
                    arg2 = Argument2.ToString(context);
                return String.Format("{0} {1} {2}", arg1, OperationToPrintableString(Opcode), arg2);
            }

            public string ToStringWithParen(DecompileContext context)
            {
                return "(" + ToString(context) + ")";
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Argument1 = Argument1?.CleanExpression(context, block);
                Argument2 = Argument2?.CleanExpression(context, block);
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                // TODO: This should be probably able to go both ways...
                Argument2.DoTypePropagation(context, Argument1.DoTypePropagation(context, suggestedType));

                /*
                if (Opcode != UndertaleInstruction.ComparisonType.EQ && Opcode != UndertaleInstruction.ComparisonType.NEQ)
                {
                    if (Argument1 is ExpressionConstant arg1)
                        if (arg1.AssetType == AssetIDType.Script)
                            arg1.AssetType = AssetIDType.Other;

                    if (Argument2 is ExpressionConstant arg2)
                        if (arg2.AssetType == AssetIDType.Script)
                            arg2.AssetType = AssetIDType.Other;
                }*/

                return AssetIDType.Other;
            }
        }

        // Represents an unnamed value that gets passed around the stack.
        // Theoretically, these should be cleaned up and removed by the end of decompilation.
        public class TempVar
        {
            public string Name;
            public UndertaleInstruction.DataType Type;
            internal AssetIDType AssetType;

            public TempVar(int id)
            {
                Name = MakeTemporaryVarName(id);
            }

            public static string MakeTemporaryVarName(int id)
            {
                return "_temp_local_var_" + id;
            }
        }

        // A reference class for tempvars.
        public class TempVarReference
        {
            public TempVar Var;

            public TempVarReference(TempVar var)
            {
                Var = var;
            }
        }

        // Assignment statement for tempvars.
        public class TempVarAssignmentStatement : Statement
        {
            public TempVarReference Var;
            public Expression Value;

            public bool HasVarKeyword;

            public TempVarAssignmentStatement(TempVarReference var, Expression value)
            {
                Var = var;
                Value = value;
            }

            public override string ToString(DecompileContext context)
            {
                //TODO: why is there a GMS2Check for this? var exists in gms1.4 as well
                if (context.GlobalContext.Data?.IsGameMaker2() ?? false && !HasVarKeyword && context.LocalVarDefines.Add(Var.Var.Name))
                    HasVarKeyword = true;

                return String.Format("{0}{1} = {2}", (HasVarKeyword ? "var " : ""), Var.Var.Name, Value.ToString(context));
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Value = Value?.CleanExpression(context, block);

                if (Var != null && Var.Var != null && Var.Var.Name != null)
                {
                    if ((Value as ExpressionTempVar)?.Var?.Var?.Name == Var.Var.Name) // This is literally set to itself. No thank you.
                    {
                        block.Statements.Remove(context.TempVarMap[Var.Var.Name]);
                        return context.TempVarMap[Var.Var.Name].CleanStatement(context, block);
                    }

                    context.TempVarMap[Var.Var.Name] = this;
                }
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                if (Var.Var.AssetType == AssetIDType.Other)
                    Var.Var.AssetType = suggestedType;
                return Value.DoTypePropagation(context, Var.Var.AssetType);
            }
        }

        // Represents a tempvar inside of an expression.
        public class ExpressionTempVar : Expression
        {
            public TempVarReference Var;

            public ExpressionTempVar(TempVarReference var, UndertaleInstruction.DataType targetType)
            {
                this.Var = var;
                this.Type = targetType;
            }

            internal override bool IsDuplicationSafe()
            {
                return true;
            }

            public override string ToString(DecompileContext context)
            {
                return Var.Var.Name;
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                TempVarAssignmentStatement tempVarStatement = context.TempVarMap[Var.Var.Name];
                if (tempVarStatement != null)
                {
                    block.Statements.Remove(tempVarStatement);
                    return tempVarStatement.Value.CleanStatement(context, block);
                }

                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                if (Var.Var.AssetType == AssetIDType.Other)
                    Var.Var.AssetType = suggestedType;
                return Var.Var.AssetType;
            }
        }

        // Represents a high-level return statement, or an exit in Studio version < 2 if there is no value.
        public class ReturnStatement : Statement
        {
            public Expression Value;

            public ReturnStatement(Expression value)
            {
                Value = value;
            }

            public override string ToString(DecompileContext context)
            {
                if (Value != null)
                {
                    if (AssetTypeResolver.return_types.ContainsKey(context.TargetCode.Name.Content))
                        Value.DoTypePropagation(context, AssetTypeResolver.return_types[context.TargetCode.Name.Content]);
                    if (context.GlobalContext.Data != null && !DecompileContext.GMS2_3)
                    {
                        // We might be decompiling a legacy script - resolve it's name
                        UndertaleScript script = context.GlobalContext.Data.Scripts.FirstOrDefault(x => x.Code == context.TargetCode);
                        if (script != null && AssetTypeResolver.return_types.ContainsKey(script.Name.Content))
                            Value.DoTypePropagation(context, AssetTypeResolver.return_types[script.Name.Content]);
                    }

                    string cleanVal = Value.ToString(context);
                    if (cleanVal.EndsWith("\n", StringComparison.InvariantCulture))
                        cleanVal = cleanVal.Substring(0, cleanVal.Length - 1);

                    return "return " + cleanVal + ";";
                }
                else
                    return (context.GlobalContext.Data?.IsGameMaker2() ?? false ? "return;" : "exit");
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Value = Value?.CleanExpression(context, block);
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Value?.DoTypePropagation(context, suggestedType) ?? suggestedType;
            }
        }

        // Represents a high-level assignment statement.
        public class AssignmentStatement : Statement
        {
            public ExpressionVar Destination;
            public Expression Value;

            public bool HasVarKeyword;

            private bool _isStructDefinition, _checkedForDefinition;
            public bool IsStructDefinition
            {
                get
                {
                    // Quick hack
                    if (!_checkedForDefinition)
                    {
                        try
                        {
                            if (Destination.Var.Name.Content.StartsWith("___struct___", StringComparison.InvariantCulture))
                            {
                                Expression val = Value;
                                while (val is ExpressionCast cast)
                                    val = cast;

                                if (val is FunctionDefinition def)
                                {
                                    def.PromoteToStruct();
                                    _isStructDefinition = true;
                                }
                            }
                        }
                        catch (Exception) { }
                        _checkedForDefinition = true;
                    }
                    return _isStructDefinition;
                }
            }

            public AssignmentStatement(ExpressionVar destination, Expression value)
            {
                Destination = destination;
                Value = value;
            }

            public override string ToString(DecompileContext context)
            {
                bool gms2 = context.GlobalContext.Data?.IsGameMaker2() ?? false;

                if (gms2 && IsStructDefinition)
                    return "";

                string varName = Destination.ToString(context);

                if (gms2 && !HasVarKeyword)
                {
                    var data = context.GlobalContext.Data;
                    if (data != null)
                    {
                        var locals = data.CodeLocals.For(context.TargetCode);
                        // Stop decompiler from erroring on missing CodeLocals
                        if (locals != null && locals.HasLocal(varName) && context.LocalVarDefines.Add(varName))
                            HasVarKeyword = true;
                    }
                }

                // Someone enlighten me on structs, I'm steering clear for now.
                // And find the "right" way to do this.
                if (Value is FunctionDefinition functionVal && functionVal.Subtype != FunctionDefinition.FunctionType.Struct)
                {
                    functionVal.IsStatement = true;
                    return functionVal.ToString(context);
                }

                string varPrefix = (HasVarKeyword ? "var " : "");

                // Check for possible ++, --, or operation equal (for single vars)
                if (Value is ExpressionTwo two && (two.Argument1 is ExpressionVar) &&
                    (two.Argument1 as ExpressionVar).Var == Destination.Var)
                {
                    if (two.Argument2 is ExpressionConstant c && c.IsPushE && ExpressionConstant.ConvertToInt(c.Value) == 1)
                        return varName + (two.Opcode == UndertaleInstruction.Opcode.Add ? "++" : "--");

                    // Not ++ or --, could potentially be an operation equal
                    bool checkEqual(ExpressionVar a, ExpressionVar b)
                    {
                        if (a.InstType.GetType() != b.InstType.GetType())
                            return false;
                        ExpressionConstant ac = (a.InstType as ExpressionConstant), bc = (b.InstType as ExpressionConstant);
                        bool res = ac.Value.Equals(bc.Value) && ac.IsPushE == bc.IsPushE && ac.Type == bc.Type && ac.WasDuplicated == bc.WasDuplicated &&
                               a.VarType == b.VarType;
                        res &= (a.ArrayIndices != null) == (b.ArrayIndices != null);
                        if (a.ArrayIndices != null)
                        {
                            res &= a.ArrayIndices.Count == b.ArrayIndices.Count;
                            if (res)
                            {
                                for (int i = 0; i < a.ArrayIndices.Count; i++)
                                    res &= a.ArrayIndices[i] == b.ArrayIndices[i];
                            }
                        }
                        return res;
                    }
                    if (Destination.InstType is ExpressionConstant)
                    {
                        ExpressionVar v1 = (ExpressionVar)two.Argument1;
                        if (checkEqual(Destination, v1) && two.Opcode != UndertaleInstruction.Opcode.Shl && two.Opcode != UndertaleInstruction.Opcode.Shr && two.Opcode != UndertaleInstruction.Opcode.Rem)
                        {
                            if (!(context.GlobalContext.Data?.GeneralInfo?.BytecodeVersion > 14 && v1.Opcode != UndertaleInstruction.Opcode.Push && Destination.Var.InstanceType != UndertaleInstruction.InstanceType.Self))
                                return String.Format("{0}{1} {2}= {3}", varPrefix, varName, Expression.OperationToPrintableString(two.Opcode), two.Argument2.ToString(context));
                        }
                    }
                }
                return String.Format("{0}{1}{2} {3}", varPrefix, varName, context.DecompilingStruct ? ":" : " =", Value.ToString(context));
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Expression expr = Destination.CleanExpression(context, block);

                if (expr is ExpressionVar expvar)
                    Destination = expvar;

                Value = Value.CleanExpression(context, block);
                if (Destination.Var.Name?.Content == "$$$$temp$$$$")
                    context.CompilerTempVar = this;

                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Value.DoTypePropagation(context, Destination.DoTypePropagation(context, suggestedType));
            }
        }

        // Represents a high-level operation-equals statement, such as a += 1.
        public class OperationEqualsStatement : Statement
        {
            public ExpressionVar Destination;
            public UndertaleInstruction.Opcode Operation;
            public Expression Value;

            public OperationEqualsStatement(ExpressionVar destination, UndertaleInstruction.Opcode operation, Expression value)
            {
                Destination = destination;
                Operation = operation;
                Value = value;
            }

            public override string ToString(DecompileContext context)
            {
                return String.Format("{0} {1}= {2}", Destination.ToString(context), Expression.OperationToPrintableString(Operation), Value.ToString(context));
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                Value = Value?.CleanExpression(context, block);
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Value.DoTypePropagation(context, Destination.DoTypePropagation(context, suggestedType));
            }
        }

        // Represents a code comment, for debugging use (or minor error reporting).
        public class CommentStatement : Statement
        {
            public string Message;

            public CommentStatement(string message)
            {
                Message = message;
            }

            public override string ToString(DecompileContext context)
            {
                return "// " + Message;
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return suggestedType;
            }
        }

        // Represents an inline function definition
        public class FunctionDefinition : Expression
        {
            public enum FunctionType
            {
                Function,
                Constructor,
                Struct
            }

            public UndertaleFunction Function { get; private set; }
            public UndertaleCode FunctionBodyCodeEntry { get; private set; }
            public Block FunctionBodyEntryBlock { get; private set; }
            public FunctionType Subtype { get; private set; }
            public bool IsStatement = false; // I know it's an expression, yes. But I'm not duplicating the rest.

            internal List<Expression> Arguments;

            public FunctionDefinition(UndertaleFunction target, UndertaleCode functionBodyCodeEntry, Block functionBodyEntryBlock, FunctionType type)
            {
                Subtype = type;
                Function = target;
                FunctionBodyCodeEntry = functionBodyCodeEntry;
                FunctionBodyEntryBlock = functionBodyEntryBlock;
            }

            public void PromoteToStruct()
            {
                if (Subtype == FunctionType.Function)
                    throw new InvalidOperationException("Cannot promote function to struct");

                Subtype = FunctionType.Struct;
            }

            public void PopulateArguments(params Expression[] arguments)
            {
                PopulateArguments(arguments.ToList());
            }

            public void PopulateArguments(List<Expression> arguments)
            {
                if (Subtype != FunctionType.Struct)
                    throw new InvalidOperationException("Cannot populate arguments of non-struct");

                if (Arguments == null)
                    Arguments = new List<Expression>();

                Arguments.AddRange(arguments);
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                StringBuilder sb = new StringBuilder();
                if (context.Statements.ContainsKey(FunctionBodyEntryBlock.Address.Value))
                {
                    FunctionDefinition def;
                    var oldDecompilingStruct = context.DecompilingStruct;
                    var oldReplacements = context.ArgumentReplacements;

                    if (Subtype == FunctionType.Struct)
                        context.DecompilingStruct = true;
                    else
                    {
                        context.DecompilingStruct = false;
                        sb.Append("function");
                        if (IsStatement)
                        {
                            sb.Append(" ");

                            // For further optimization, we could *probably* create a dictionary that's just flipped KVPs (assuming there are no dup. values).
                            // Doing so would save the need for LINQ and what-not. Not that big of an issue, but still an option.
                            Dictionary<string, UndertaleFunction> subFuncs = context.GlobalContext.Data.KnownSubFunctions;
                            KeyValuePair<string, UndertaleFunction> kvp = subFuncs.FirstOrDefault(x => x.Value == Function);

                            // If we found an associated sub-function, use the key as the name.
                            if (kvp.Key != null)
                                sb.Append(kvp.Key);
                            else
                            {
                                //Attempt to find function names before going with the last functions' name
                                bool gotFuncName = false;
                                if (Function.Name.Content.StartsWith("gml_Script_"))
                                {
                                    string funcName = Function.Name.Content.Substring("gml_Script_".Length);
                                    if (context.Statements[0].Any(x => x is AssignmentStatement && (x as AssignmentStatement).Destination.Var.Name.Content == funcName))
                                    {
                                        sb.Append(funcName);
                                        gotFuncName = true;
                                    }
                                }
                                if(!gotFuncName)
                                    sb.Append((context.Statements[0].Last() as AssignmentStatement).Destination.Var.Name.Content);
                            }
                        }
                        sb.Append("(");
                        for (int i = 0; i < FunctionBodyCodeEntry.ArgumentsCount; ++i)
                        {
                            if (i != 0)
                                sb.Append(", ");
                            sb.Append("argument");
                            sb.Append(i);
                        }
                        sb.Append(") ");
                        if (Subtype == FunctionType.Constructor)
                            sb.Append("constructor ");
                        sb.Append("//");
                        sb.Append(Function.Name.Content);
                    }

                    var statements = context.Statements[FunctionBodyEntryBlock.Address.Value];
                    int numNotReturn = statements.FindAll(stmt => !(stmt is ReturnStatement)).Count;

                    if (numNotReturn > 0 || Subtype != FunctionType.Struct)
                    {
                        sb.Append("\n");
                        sb.Append(context.Indentation);
                        sb.Append("{\n");
                        context.IndentationLevel++;
                        context.ArgumentReplacements = Arguments;

                        int count = 0;
                        foreach (Statement stmt in statements)
                        {
                            count++;
                            if ((Subtype != FunctionType.Function && stmt is ReturnStatement) || (stmt is AssignmentStatement assign && assign.IsStructDefinition))
                                continue;

                            sb.Append(context.Indentation);

                            // See #614
                            // This is not the place to monkey patch this
                            // issue, but it's like 2am and quite frankly
                            // I don't care anymore.
                            def = null;
                            if (stmt is FunctionDefinition)
                                def = stmt as FunctionDefinition;
                            else if (stmt is TempVarAssignmentStatement reference && reference.Value is FunctionDefinition)
                                def = reference.Value as FunctionDefinition;

                            if (def?.Function == Function)
                            {
                                //sb.Append("// Error decompiling function: function contains its own declaration???\n");
                                sb.Append("\n");
                                break;
                            }
                            else
                            {
                                sb.Append(stmt.ToString(context));
                                if (Subtype == FunctionType.Struct && count < numNotReturn)
                                    sb.Append(",");
                            }
                            sb.Append("\n");
                        }
                        context.DecompilingStruct = oldDecompilingStruct;
                        context.ArgumentReplacements = oldReplacements;
                        context.IndentationLevel--;
                        sb.Append(context.Indentation);
                        sb.Append("}");
                        if(!oldDecompilingStruct)
                            sb.Append("\n");
                    }
                    else
                        sb.Append("{}");
                }
                else
                {
                    sb.Append(Function.Name.Content);
                }
                return sb.ToString();
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return suggestedType;
            }
        }

        // Represents a high-level function or script call.
        public abstract class FunctionCall : Expression
        {
            internal UndertaleInstruction.DataType ReturnType;
            internal List<Expression> Arguments;

            protected FunctionCall(UndertaleInstruction.DataType returnType, List<Expression> args)
            {
                this.ReturnType = returnType;
                this.Arguments = args;
            }

            internal override bool IsDuplicationSafe()
            {
                // Function calls are never duplication safe - they can have side effects
                return false;
            }
        }

        public class DirectFunctionCall : FunctionCall
        {
            internal string OverridenName = string.Empty;
            internal UndertaleFunction Function;

            public DirectFunctionCall(string overridenName, UndertaleFunction function, UndertaleInstruction.DataType returnType, List<Expression> args) : base(returnType, args)
            {
                this.OverridenName = overridenName;
                this.Function = function;
            }

            public DirectFunctionCall(UndertaleFunction function, UndertaleInstruction.DataType returnType, List<Expression> args) : base(returnType, args)
            {
                this.Function = function;
            }

            public override string ToString(DecompileContext context)
            {
                StringBuilder argumentString = new StringBuilder();

                if (Function.Name.Content == "@@NewGMLObject@@") // Creating a new "object" via a constructor OR this is a struct definition
                {
                    context.currentFunction = this;

                    string constructor;
                    var actualArgs = Arguments.Skip(1).ToList();
                    if (Arguments[0] is FunctionDefinition def)
                    {
                        if (def.Subtype == FunctionDefinition.FunctionType.Struct) // Struct moment
                        {
                            def.PopulateArguments(actualArgs);
                            return def.ToString(context);
                        }
                        else
                            constructor = def.FunctionBodyCodeEntry.Name.Content;
                    }
                    else
                        constructor = Arguments[0].ToString(context);

                    if (constructor.StartsWith("gml_Script_", StringComparison.InvariantCulture))
                        constructor = constructor.Substring(11);
                    if (constructor.EndsWith(context.TargetCode.Name.Content, StringComparison.InvariantCulture))
                        constructor = constructor.Substring(0, constructor.Length - context.TargetCode.Name.Content.Length - 1);

                    if (AssetTypeResolver.builtin_funcs.TryGetValue(constructor, out AssetIDType[] types))
                    {
                        int index = 0;
                        foreach (var arg in actualArgs)
                            arg.DoTypePropagation(context, types[index++]);
                    }

                    // Don't ask
                    if (Arguments[0] is ExpressionCast cast &&
                        cast.Argument is ExpressionConstant constant &&
                        constant.Value is UndertaleInstruction.Reference<UndertaleFunction> reference)
                    {
                        var call = new DirectFunctionCall(reference.Target, ReturnType, actualArgs) {
                            OverridenName = constructor
                        };

                        return "new " + call.ToString(context);
                    }
                    else
                    {
                        foreach (Expression exp in actualArgs)
                        {
                            context.currentFunction = this;
                            if (argumentString.Length > 0)
                                argumentString.Append(", ");
                            argumentString.Append(exp.ToString(context));
                        }
                    }
                    context.currentFunction = null;

                    return String.Format("new {0}({1})", constructor, argumentString);
                }
                else
                {
                    foreach (Expression exp in Arguments)
                    {
                        context.currentFunction = this;
                        if (argumentString.Length > 0)
                            argumentString.Append(", ");
                        argumentString.Append(exp.ToString(context));

                    }
                    context.currentFunction = null;

                    if (Function.Name.Content == "@@NewGMLArray@@") // Inline array definitions
                        return "[" + argumentString.ToString() + "]";

                    return String.Format("{0}({1})", OverridenName != string.Empty ? OverridenName : Function.Name.Content, argumentString.ToString());
                }


            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                // Special case for these functions which don't have any purpose in decompiled code
                if (Function?.Name?.Content == "@@This@@")
                {
                    return new ExpressionConstant(UndertaleInstruction.DataType.Variable, "self");
                }
                if (Function?.Name?.Content == "@@Other@@")
                {
                    return new ExpressionConstant(UndertaleInstruction.DataType.Variable, "other");
                }
                if (Function?.Name?.Content == "@@GetInstance@@")
                {
                    Statement res = Arguments[0];
                    if (res is ExpressionCast cast)
                        return cast.Argument;
                    return res;
                }
                for (var i = 0; i < Arguments.Count; i++)
                    Arguments[i] = Arguments[i]?.CleanExpression(context, block);
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                string funcName = OverridenName != string.Empty ? OverridenName : Function.Name.Content;
                var script_code = context.GlobalContext.Data?.Scripts.ByName(funcName)?.Code;
                if (script_code != null && !context.GlobalContext.ScriptArgsCache.ContainsKey(funcName))
                {
                    context.GlobalContext.ScriptArgsCache.Add(funcName, null); // stop the recursion from looping
                    DecompileContext childContext;
                    try
                    {
                        if (script_code.ParentEntry != null)
                        {
                            childContext = new DecompileContext(context.GlobalContext, script_code.ParentEntry);
                            Dictionary<uint, Block> blocks = Decompiler.PrepareDecompileFlow(script_code.ParentEntry, new List<uint>() { script_code.Offset / 4 });
                            Decompiler.DecompileFromBlock(childContext, blocks, blocks[script_code.Offset / 4]);
                            Decompiler.DoTypePropagation(childContext, blocks); // TODO: This should probably put suggestedType through the "return" statement at the other end
                        }
                        else
                        {
                            childContext = new DecompileContext(context.GlobalContext, script_code);
                            Dictionary<uint, Block> blocks = Decompiler.PrepareDecompileFlow(script_code, new List<uint>() { 0 });
                            Decompiler.DecompileFromBlock(childContext, blocks, blocks[0]);
                            Decompiler.DoTypePropagation(childContext, blocks); // TODO: This should probably put suggestedType through the "return" statement at the other end
                        }
                        context.GlobalContext.ScriptArgsCache[funcName] = new AssetIDType[15];
                        for (int i = 0; i < 15; i++)
                        {
                            var v = childContext.assetTypes.Where((x) => x.Key.Name.Content == "argument" + i);
                            context.GlobalContext.ScriptArgsCache[funcName][i] = v.Any() ? v.First().Value : AssetIDType.Other;
                        }
                    }
                    catch (Exception e)
                    {
                        context.GlobalContext.DecompilerWarnings.Add("/*\nWARNING: Recursive script decompilation (for asset type resolution) failed for " + Function.Name.Content + "\n\n" + e.ToString() + "\n*/");
                    }
                }

                AssetIDType[] args = new AssetIDType[Arguments.Count];
                AssetTypeResolver.AnnotateTypesForFunctionCall(funcName, args, context, this);
                for (var i = 0; i < Arguments.Count; i++)
                    Arguments[i].DoTypePropagation(context, args[i]);

                return suggestedType; // TODO: maybe we should handle returned values too?
            }
        }

        public class IndirectFunctionCall : FunctionCall
        {
            internal Expression FunctionThis;
            internal Expression Function;

            public IndirectFunctionCall(Expression func_this, Expression func, UndertaleInstruction.DataType returnType, List<Expression> args) : base(returnType, args)
            {
                this.FunctionThis = func_this;
                this.Function = func;
            }

            public override string ToString(DecompileContext context)
            {
                StringBuilder argumentString = new StringBuilder();
                foreach (Expression exp in Arguments)
                {
                    if (argumentString.Length > 0)
                        argumentString.Append(", ");
                    argumentString.Append(exp.ToString(context));
                }

                if (Function is FunctionDefinition)
                    return String.Format("{0}({1})", Function.ToString(context), argumentString.ToString());

                return String.Format("{0}.{1}({2})", FunctionThis.ToString(context), Function.ToString(context), argumentString.ToString());
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                FunctionThis = (Expression)FunctionThis?.CleanStatement(context, block);
                Function = (Expression)Function?.CleanStatement(context, block);
                for (var i = 0; i < Arguments.Count; i++)
                    Arguments[i] = Arguments[i]?.CleanExpression(context, block);
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                FunctionThis.DoTypePropagation(context, AssetIDType.GameObject);
                Function.DoTypePropagation(context, suggestedType);
                AssetIDType[] args = new AssetIDType[Arguments.Count];
                for (var i = 0; i < Arguments.Count; i++)
                    Arguments[i].DoTypePropagation(context, args[i]);

                return suggestedType;
            }
        }

        // Represents a variable in an expression, of any type.
        public class ExpressionVar : Expression
        {
            public UndertaleVariable Var;
            public Expression InstType; // UndertaleInstruction.InstanceType
            public UndertaleInstruction.VariableType VarType;
            public List<Expression> ArrayIndices = null;
            public UndertaleInstruction.Opcode Opcode;

            public ExpressionVar(UndertaleVariable var, Expression instType, UndertaleInstruction.VariableType varType)
            {
                Var = var;
                InstType = instType;
                VarType = varType;
            }

            internal override bool IsDuplicationSafe()
            {
                bool res = (InstType?.IsDuplicationSafe() ?? true);

                if (ArrayIndices == null)
                    return res;
                foreach (Expression e in ArrayIndices)
                    res &= (e?.IsDuplicationSafe() ?? true);

                return res;
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                if (Var.Name?.Content == "$$$$temp$$$$" && context.CompilerTempVar != null)
                {
                    block.Statements.Remove(context.CompilerTempVar);
                    return context.CompilerTempVar.Value.CleanStatement(context, block);
                }

                InstType = InstType?.CleanExpression(context, block);
                if (ArrayIndices == null)
                    return this;
                foreach (Expression e in ArrayIndices)
                    e?.CleanExpression(context, block);
                return this;
            }

            public static Tuple<Expression, Expression> Decompile2DArrayIndex(Expression index)
            {
                Expression ind1 = index;
                Expression ind2 = null;
                if (ind1 is ExpressionTwo && (ind1 as ExpressionTwo).Opcode == UndertaleInstruction.Opcode.Add)
                {
                    var arg1 = (ind1 as ExpressionTwo).Argument1;
                    var arg2 = (ind1 as ExpressionTwo).Argument2;
                    if (arg1 is ExpressionTwo && (arg1 as ExpressionTwo).Opcode == UndertaleInstruction.Opcode.Mul)
                    {
                        var arg11 = (arg1 as ExpressionTwo).Argument1;
                        var arg12 = (arg1 as ExpressionTwo).Argument2;
                        if (arg12 is ExpressionConstant && (arg12 as ExpressionConstant).Value is int && (int)(arg12 as ExpressionConstant).Value == 32000)
                        {
                            ind1 = arg11;
                            ind2 = arg2;
                        }
                    }
                }
                return new Tuple<Expression, Expression>(ind1, ind2);
            }

            public override string ToString(DecompileContext context)
            {
                string name = Var.Name.Content;
                if (ArrayIndices != null)
                {
                    if (DecompileContext.GMS2_3 == true)
                    {
                        if (name == "argument" && context.DecompilingStruct && context.ArgumentReplacements != null && ArrayIndices.Count == 1)
                        {
                            var replacements = context.ArgumentReplacements;
                            if (int.TryParse(ArrayIndices[0].ToString(context), out int index) && index >= 0 && index < replacements.Count && this != replacements[index])
                                return replacements[index].ToString(context);
                        }
                        foreach (Expression e in ArrayIndices)
                            name += "[" + e.ToString(context) + "]";

                    }
                    else
                    {
                        if (ArrayIndices.Count == 2 && ArrayIndices[0] != null && ArrayIndices[1] != null)
                            name += "[" + ArrayIndices[0].ToString(context) + ", " + ArrayIndices[1].ToString(context) + "]";
                        else if (ArrayIndices[0] != null)
                            name += "[" + ArrayIndices[0].ToString(context) + "]";
                    }
                }

                // NOTE: The "var" prefix is handled in Decompiler.Decompile.

                if (VarType == UndertaleInstruction.VariableType.Instance)
                {
                    if (InstType is ExpressionConstant c)
                    {
                        int? val = ExpressionConstant.ConvertToInt(c.Value);
                        if (val == null)
                            throw new InvalidOperationException("Unable to parse the instance ID to int");
                        // TODO: This is a reference to an object instance in the room. Resolving these is non-trivial since you don't exactly have a reference to the room where this script is used when decompiling...
                        return (val + 100000) + "." + name;
                    }
                    else throw new InvalidOperationException("Instance variable type used with non-const InstType"); // TODO: can this happen?
                }
                if (InstType is ExpressionConstant constant) // Only use "global." and "other.", not "self." or "local.". GMS doesn't recognize those.
                {
                    string prefix = InstType.ToString(context) + ".";
                    if (!(constant.Value is Int64))
                    {
                        int? val = ExpressionConstant.ConvertToInt(constant.Value);
                        if (val != null)
                        {
                            if (constant.AssetType == AssetIDType.GameObject && val < 0)
                            {
                                UndertaleInstruction.InstanceType instanceType = (UndertaleInstruction.InstanceType)val;
                                prefix = (instanceType == UndertaleInstruction.InstanceType.Global || instanceType == UndertaleInstruction.InstanceType.Other) ? prefix.ToLower(CultureInfo.InvariantCulture) : "";
                            }
                        }
                    }
                    return prefix + name;
                }
                else if (InstType is ExpressionCast cast && !(cast.Argument is ExpressionVar))
                {
                    return "(" + InstType.ToString(context) + ")." + name; // Make sure to put parentheses around these cases
                }

                return InstType.ToString(context) + "." + name;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                InstType?.DoTypePropagation(context, AssetIDType.GameObject);
                if (ArrayIndices != null)
                {
                    foreach (Expression e in ArrayIndices)
                        e?.DoTypePropagation(context, AssetIDType.Other);
                }

                AssetIDType current = context.assetTypes.ContainsKey(Var) ? context.assetTypes[Var] : AssetIDType.Other;
                if (current == AssetIDType.Other && suggestedType != AssetIDType.Other)
                    current = suggestedType;
                AssetIDType builtinSuggest = AssetTypeResolver.AnnotateTypeForVariable(context, Var.Name.Content);
                if (builtinSuggest != AssetIDType.Other)
                    current = builtinSuggest;

                if ((VarType != UndertaleInstruction.VariableType.Array || (ArrayIndices != null && !(ArrayIndices[0] is ExpressionConstant))))
                    context.assetTypes[Var] = current; // This is a messy fix to arrays messing up exported variable types.
                return current;
            }
        }

        // Represents a with statement beginning (pushing to the env stack).
        // This is not seen in high-level output.
        public class PushEnvStatement : Statement
        {
            public Expression NewEnv;

            public PushEnvStatement(Expression newEnv)
            {
                this.NewEnv = newEnv;
            }

            public override string ToString(DecompileContext context)
            {
                return "pushenv " + NewEnv;
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                NewEnv = NewEnv?.CleanExpression(context, block);
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                NewEnv.DoTypePropagation(context, AssetIDType.GameObject);
                return suggestedType;
            }
        }

        // Represents a with statement ending (popping from or clearing the env stack).
        // This is not seen in high-level output.
        public class PopEnvStatement : Statement
        {
            public override string ToString(DecompileContext context)
            {
                return "popenv";
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return suggestedType;
            }
        }

        static int GetTypeSize(UndertaleInstruction.DataType type)
        {
            switch (type)
            {
                case UndertaleInstruction.DataType.Int16:
                case UndertaleInstruction.DataType.Int32:
                    return 4;
                case UndertaleInstruction.DataType.Double: // Fallthrough
                case UndertaleInstruction.DataType.Int64:
                    return 8;
                case UndertaleInstruction.DataType.Variable:
                    return 16;
                default:
                    throw new NotImplementedException("Unknown size for data type " + type);
            }
        }
        static int GetTypeSize(Expression e)
        {
            if (e is ExpressionVar || e is ExpressionTempVar)
                return GetTypeSize(UndertaleInstruction.DataType.Variable);
            if (e is FunctionCall)  // function call returns an internal variable
                return GetTypeSize(UndertaleInstruction.DataType.Variable);
            if (e is FunctionDefinition)
                return GetTypeSize(UndertaleInstruction.DataType.Variable);
            if (e is ExpressionTwo exprTwo)
                return GetTypeSize(exprTwo.Type2); // for add.i.v, the output is a var
            return GetTypeSize(e.Type);
        }

        // The core function to decompile a specific block.
        internal static void DecompileFromBlock(DecompileContext context, Dictionary<uint, Block> blocks, Block block, List<TempVarReference> tempvars, Stack<Tuple<Block, List<TempVarReference>>> workQueue)
        {
            if (block.TempVarsOnEntry != null && (block.nextBlockTrue != null || block.nextBlockFalse != null))
            {
                // Reroute tempvars to alias them to our ones
                if (block.TempVarsOnEntry.Count != tempvars.Count)
                {
                    throw new Exception("Reentered block with different amount of vars on stack (Entry: " + block.TempVarsOnEntry.Count + ", Actual Count: " + tempvars.Count + ")");
                }
                else
                {
                    for (int i = 0; i < tempvars.Count; i++)
                    {
                        tempvars[i].Var = block.TempVarsOnEntry[i].Var;
                    }
                }
            }

            // Don't decompile more than once
            if (block.Statements != null)
                return;

            // Recover stack tempvars which may be needed
            block.TempVarsOnEntry = tempvars;
            Stack<Expression> stack = new Stack<Expression>();
            foreach (TempVarReference var in tempvars)
                stack.Push(new ExpressionTempVar(var, var.Var.Type));

            // Iterate through all of the sta
            List<Statement> statements = new List<Statement>();
            bool end = false;
            bool returned = false;
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                if (end)
                    throw new Exception("Expected end of block, but still has instructions");

                var instr = block.Instructions[i];
                switch (instr.Kind)
                {
                    case UndertaleInstruction.Opcode.Neg:
                    case UndertaleInstruction.Opcode.Not:
                        stack.Push(new ExpressionOne(instr.Kind, instr.Type1, stack.Pop()));
                        break;

                    case UndertaleInstruction.Opcode.Dup:
                        if (instr.ComparisonKind != 0)
                        {
                            // This is the GMS 2.3+ stack move / swap instruction
                            if (instr.Type1 == UndertaleInstruction.DataType.Variable)
                            {
                                // This variant seems to do literally nothing...?
                                break;
                            }

                            int bytesToTake = instr.Extra * 4;
                            Stack<Expression> taken = new Stack<Expression>();
                            while (bytesToTake > 0)
                            {
                                Expression e = stack.Pop();
                                taken.Push(e);
                                bytesToTake -= GetTypeSize(e);
                                if (bytesToTake < 0)
                                    throw new InvalidOperationException("The stack got misaligned? Error 0");
                            }

                            int b2 = (byte)instr.ComparisonKind & 0x7F;
                            if ((b2 & 0b111) != 0)
                                throw new InvalidOperationException("Don't know what to do with this");
                            int bytesToMove = (b2 >> 3) * 4;
                            Stack<Expression> moved = new Stack<Expression>();
                            while (bytesToMove > 0)
                            {
                                Expression e = stack.Pop();
                                moved.Push(e);
                                bytesToMove -= GetTypeSize(e);
                                if (bytesToMove < 0)
                                    throw new InvalidOperationException("The stack got misaligned? Error 1");
                            }

                            while (taken.Count > 0)
                                stack.Push(taken.Pop());
                            while (moved.Count > 0)
                                stack.Push(moved.Pop());

                            break;
                        }

                        // Normal dup instruction

                        List<Expression> topExpressions1 = new List<Expression>();
                        List<Expression> topExpressions2 = new List<Expression>();
                        int bytesToDuplicate = (instr.Extra + 1) * GetTypeSize(instr.Type1);
                        while (bytesToDuplicate > 0)
                        {
                            var item = stack.Pop();

                            if (item.IsDuplicationSafe())
                            {
                                item.WasDuplicated = true;
                                topExpressions1.Add(item);
                                topExpressions2.Add(item);
                            }
                            else
                            {
                                TempVar var = context.NewTempVar();
                                var.Type = item.Type;
                                TempVarReference varref = new TempVarReference(var);
                                statements.Add(new TempVarAssignmentStatement(varref, item));

                                topExpressions1.Add(new ExpressionTempVar(varref, varref.Var.Type) { WasDuplicated = true });
                                topExpressions2.Add(new ExpressionTempVar(varref, instr.Type1) { WasDuplicated = true });
                            }

                            bytesToDuplicate -= GetTypeSize(item);
                            if (bytesToDuplicate < 0)
                                throw new InvalidOperationException("The stack got misaligned? Error 2: Attempted to duplicate "
                                    + GetTypeSize(item)
                                    + " bytes, only found "
                                    + (bytesToDuplicate + GetTypeSize(item)));
                        }
                        topExpressions1.Reverse();
                        topExpressions2.Reverse();
                        for (int j = 0; j < topExpressions1.Count; j++)
                            stack.Push(topExpressions1[j]);
                        for (int j = 0; j < topExpressions2.Count; j++)
                            stack.Push(topExpressions2[j]);
                        break;

                    case UndertaleInstruction.Opcode.Ret:
                    case UndertaleInstruction.Opcode.Exit:
                        // 2.3 scripts add exits to every script, even those that lack a return
                        // This detects that type of exit using the next block.
                        Block nextBlock = null;
                        if (DecompileContext.GMS2_3 && instr.Kind == UndertaleInstruction.Opcode.Exit)
                        {
                            uint[] blockAddresses = blocks.Keys.ToArray();
                            Array.Sort(blockAddresses);
                            int nextBlockIndex = Array.IndexOf(blockAddresses, block.Address ?? 0) + 1;
                            if (blockAddresses.Length > nextBlockIndex)
                            {
                                uint nextBlockAddress = blockAddresses[nextBlockIndex];
                                nextBlock = blocks[nextBlockAddress];
                            }
                        }

                        if (!(nextBlock is not null
                            && nextBlock.Instructions.Count > 0
                            && nextBlock.Instructions[0].Kind == UndertaleInstruction.Opcode.Push
                            && nextBlock.Instructions[0].Value.GetType() != typeof(int)))
                        {
                            ReturnStatement stmt = new ReturnStatement(instr.Kind == UndertaleInstruction.Opcode.Ret ? stack.Pop() : null);
                            /*
                                This shouldn't be necessary: all unused things on the stack get converted to tempvars at the end anyway, and this fixes decompilation of repeat()
                                See #85

                                foreach (var expr in stack.Reverse())
                                    if (!(expr is ExpressionTempVar))
                                        statements.Add(expr);
                                stack.Clear();
                            */
                            statements.Add(stmt);
                        }
                        end = true;
                        returned = true;
                        break;

                    case UndertaleInstruction.Opcode.Popz:
                        Expression popped = stack.Pop();
                        if (!popped.IsDuplicationSafe()) // <- not duplication safe = has side effects and needs to be listed in the output
                            statements.Add(popped);
                        break;

                    case UndertaleInstruction.Opcode.Conv:
                        stack.Push(new ExpressionCast(instr.Type2, stack.Pop()));
                        break;

                    case UndertaleInstruction.Opcode.Mul:
                    case UndertaleInstruction.Opcode.Div:
                    case UndertaleInstruction.Opcode.Rem:
                    case UndertaleInstruction.Opcode.Mod:
                    case UndertaleInstruction.Opcode.Add:
                    case UndertaleInstruction.Opcode.Sub:
                    case UndertaleInstruction.Opcode.And:
                    case UndertaleInstruction.Opcode.Or:
                    case UndertaleInstruction.Opcode.Xor:
                    case UndertaleInstruction.Opcode.Shl:
                    case UndertaleInstruction.Opcode.Shr:
                        Expression a2 = stack.Pop();
                        Expression a1 = stack.Pop();
                        stack.Push(new ExpressionTwo(instr.Kind, instr.Type1, instr.Type2, a1, a2));
                        break;

                    case UndertaleInstruction.Opcode.Cmp:
                        Expression aa2 = stack.Pop();
                        Expression aa1 = stack.Pop();
                        stack.Push(new ExpressionCompare(instr.ComparisonKind, aa1, aa2));
                        break;

                    case UndertaleInstruction.Opcode.B:
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.Bt:
                    case UndertaleInstruction.Opcode.Bf:
                        block.ConditionStatement = stack.Pop();
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.PushEnv:
                        if (DecompileContext.GMS2_3 == true)
                        {
                            Expression expr = stack.Pop();

                            // -9 signifies stacktop
                            if (expr is ExpressionConstant c &&
                                c.Type == UndertaleInstruction.DataType.Int16 && (short)c.Value == -9)
                                expr = stack.Pop();

                            statements.Add(new PushEnvStatement(expr));
                        }
                        else
                            statements.Add(new PushEnvStatement(stack.Pop()));
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.PopEnv:
                        if (!instr.JumpOffsetPopenvExitMagic)
                            statements.Add(new PopEnvStatement());
                        // For JumpOffsetPopenvExitMagic:
                        //  This is just an instruction to make sure the pushenv/popenv stack is cleared on early function return
                        //  Works kinda like 'break', but doesn't have a high-level representation as it's immediately followed by a 'return'
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.Pop:
                        {
                            if (instr.Destination == null)
                            {
                                // pop.e.v 5/6, strange magic stack operation
                                // TODO: this is probably an older version of the GMS2.3+ swap hidden in dup, but I'm not gonna touch it if it works
                                Expression e1 = stack.Pop();
                                Expression e2 = stack.Pop();
                                for (int j = 0; j < instr.SwapExtra - 4; j++)
                                    stack.Pop();
                                stack.Push(e2);
                                stack.Push(e1);
                                break;
                            }
                            ExpressionVar target = new ExpressionVar(instr.Destination.Target, new ExpressionConstant(UndertaleInstruction.DataType.Int16, instr.TypeInst), instr.Destination.Type);
                            Expression val = null;
                            if (instr.Type1 != UndertaleInstruction.DataType.Int32 && instr.Type1 != UndertaleInstruction.DataType.Variable)
                                throw new Exception("Unrecognized pop instruction, doesn't conform to pop.i.X, pop.v.X, or pop.e.v");
                            if (instr.Type1 == UndertaleInstruction.DataType.Int32)
                                val = stack.Pop();
                            switch (target.VarType)
                            {
                                case UndertaleInstruction.VariableType.Normal:
                                case UndertaleInstruction.VariableType.Instance:
                                    break;
                                case UndertaleInstruction.VariableType.StackTop:
                                    target.InstType = stack.Pop();
                                    break;
                                case UndertaleInstruction.VariableType.Array:
                                    Tuple<Expression, Expression> ind = ExpressionVar.Decompile2DArrayIndex(stack.Pop());
                                    target.ArrayIndices = new List<Expression> { ind.Item1 };
                                    if (ind.Item2 != null)
                                        target.ArrayIndices.Add(ind.Item2);
                                    target.InstType = stack.Pop();
                                    break;
                                default:
                                    throw new NotImplementedException("Don't know how to decompile variable type " + target.VarType);
                            }

                            // Check if instance type is "StackTop"
                            ExpressionConstant instanceTypeConstExpr = null;
                            if (target.InstType is ExpressionConstant c1) {
                                instanceTypeConstExpr = c1;
                            } else if (target.InstType is ExpressionTempVar tempVar) {
                                TempVarAssignmentStatement assignment = context.TempVarMap[tempVar.Var.Var.Name];
                                if (assignment != null && assignment.Value is ExpressionConstant c2) {
                                    instanceTypeConstExpr = c2;
                                }
                            }
                            if (instanceTypeConstExpr != null &&
                                instanceTypeConstExpr.Type == UndertaleInstruction.DataType.Int16 &&
                                (short)instanceTypeConstExpr.Value == (short)UndertaleInstruction.InstanceType.Stacktop) {
                                target.InstType = stack.Pop();
                            }

                            if (instr.Type1 == UndertaleInstruction.DataType.Variable)
                                val = stack.Pop();
                            if (val != null)
                            {
                                if ((target.VarType == UndertaleInstruction.VariableType.StackTop || target.VarType == UndertaleInstruction.VariableType.Array) && target.InstType.WasDuplicated)
                                {
                                    // Almost safe to assume that this is a +=, -=, etc.
                                    // Need to confirm a few things first. It's not certain, could be ++ even.
                                    if (val is ExpressionTwo)
                                    {
                                        var two = (val as ExpressionTwo);
                                        if (two.Opcode != UndertaleInstruction.Opcode.Rem && // Not possible in GML, but possible in bytecode. Don't deal with these,
                                            two.Opcode != UndertaleInstruction.Opcode.Shl && // frankly we don't care enough.
                                            two.Opcode != UndertaleInstruction.Opcode.Shr)
                                        {
                                            var arg = two.Argument1;
                                            if (arg is ExpressionVar)
                                            {
                                                var v = arg as ExpressionVar;
                                                if (v.Var == target.Var && v.InstType == target.InstType &&
                                                    ((v.ArrayIndices == null && target.ArrayIndices == null) ||
                                                      v.ArrayIndices?.SequenceEqual(target.ArrayIndices) == true) && // even if null
                                                    (!(two.Argument2 is ExpressionConstant) || // Also check to make sure it's not a ++ or --
                                                    (!((two.Argument2 as ExpressionConstant).IsPushE && ExpressionConstant.ConvertToInt((two.Argument2 as ExpressionConstant).Value) == 1))))
                                                {
                                                    if (!(context.GlobalContext.Data?.GeneralInfo?.BytecodeVersion > 14 && v.Opcode != UndertaleInstruction.Opcode.Push && instr.Destination.Target.InstanceType != UndertaleInstruction.InstanceType.Self))
                                                    {
                                                        statements.Add(new OperationEqualsStatement(target, two.Opcode, two.Argument2));
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                Debug.Fail("Pop value is null.");
                            statements.Add(new AssignmentStatement(target, val));
                        }
                        break;

                    case UndertaleInstruction.Opcode.Push:
                    case UndertaleInstruction.Opcode.PushLoc:
                    case UndertaleInstruction.Opcode.PushGlb:
                    case UndertaleInstruction.Opcode.PushBltn:
                    case UndertaleInstruction.Opcode.PushI:
                        if (instr.Value is UndertaleInstruction.Reference<UndertaleVariable>)
                        {
                            ExpressionVar pushTarget = new ExpressionVar((instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Target, new ExpressionConstant(UndertaleInstruction.DataType.Int16, instr.TypeInst), (instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Type);
                            pushTarget.Opcode = instr.Kind;
                            switch(pushTarget.VarType)
                            {
                                case UndertaleInstruction.VariableType.Normal:
                                case UndertaleInstruction.VariableType.Instance:
                                    break;
                                case UndertaleInstruction.VariableType.StackTop:
                                    pushTarget.InstType = stack.Pop();
                                    break;
                                case UndertaleInstruction.VariableType.Array:
                                    Tuple<Expression, Expression> ind = ExpressionVar.Decompile2DArrayIndex(stack.Pop());
                                    pushTarget.ArrayIndices = new List<Expression>() { ind.Item1 };
                                    if (ind.Item2 != null)
                                        pushTarget.ArrayIndices.Add(ind.Item2);
                                    pushTarget.InstType = stack.Pop();
                                    break;
                                case UndertaleInstruction.VariableType.ArrayPopAF:
                                case UndertaleInstruction.VariableType.ArrayPushAF:
                                    pushTarget.ArrayIndices = new List<Expression>() { stack.Pop() };
                                    pushTarget.InstType = stack.Pop();
                                    break;
                                default:
                                    throw new NotImplementedException("Don't know how to decompile variable type " + pushTarget.VarType);
                            }
                            if (pushTarget.InstType is ExpressionConstant c &&
                                c.Type == UndertaleInstruction.DataType.Int16 && (short)c.Value == -9)
                                pushTarget.InstType = stack.Pop();
                            stack.Push(pushTarget);
                        }
                        else
                        {
                            bool isPushE = (instr.Kind == UndertaleInstruction.Opcode.Push && instr.Type1 == UndertaleInstruction.DataType.Int16);
                            Expression pushTarget = new ExpressionConstant(instr.Type1, instr.Value, isPushE);
                            if (isPushE && pushTarget.Type == UndertaleInstruction.DataType.Int16 && Convert.ToInt32((pushTarget as ExpressionConstant).Value) == 1)
                            {
                                // Check for expression ++ or --
                                if (((i >= 1 && block.Instructions[i - 1].Kind == UndertaleInstruction.Opcode.Dup && block.Instructions[i - 1].Type1 == UndertaleInstruction.DataType.Variable) ||
                                     (i >= 2 && block.Instructions[i - 2].Kind == UndertaleInstruction.Opcode.Dup && block.Instructions[i - 2].Type1 == UndertaleInstruction.DataType.Variable &&
                                      block.Instructions[i - 1].Kind == UndertaleInstruction.Opcode.Pop && block.Instructions[i - 1].Type1 == UndertaleInstruction.DataType.Int16 && block.Instructions[i - 1].Type2 == UndertaleInstruction.DataType.Variable)) &&
                                    (i + 1 < block.Instructions.Count && (block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Add || block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Sub)))
                                {
                                    // We've detected a post increment/decrement (i.e., x = y++)
                                    // Remove duplicate from stack
                                    stack.Pop();

                                    // Do the magic
                                    stack.Push(new ExpressionPost(block.Instructions[i + 1].Kind, stack.Pop()));

                                    while (i < block.Instructions.Count && (block.Instructions[i].Kind != UndertaleInstruction.Opcode.Pop || (block.Instructions[i].Type1 == UndertaleInstruction.DataType.Int16 && block.Instructions[i].Type2 == UndertaleInstruction.DataType.Variable)))
                                        i++;
                                }
                                else if (i + 2 < block.Instructions.Count && (block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Add || block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Sub) &&
                                        block.Instructions[i + 2].Kind == UndertaleInstruction.Opcode.Dup && block.Instructions[i + 2].Type1 == UndertaleInstruction.DataType.Variable)
                                {
                                    // We've detected a pre increment/decrement (i.e., x = ++y)
                                    // Do the magic
                                    stack.Push(new ExpressionPre(block.Instructions[i + 1].Kind, stack.Pop()));

                                    while (i < block.Instructions.Count && block.Instructions[i].Kind != UndertaleInstruction.Opcode.Pop)
                                        i++;
                                    var _inst = block.Instructions[i];
                                    if (_inst.Type1 == UndertaleInstruction.DataType.Int16 && _inst.Type2 == UndertaleInstruction.DataType.Variable)
                                    {
                                        Expression e = stack.Pop();
                                        stack.Pop();
                                        stack.Push(e);
                                        i++;
                                    }
                                }
                                else
                                {
                                    stack.Push(pushTarget);
                                }
                            }
                            else
                            {
                                stack.Push(pushTarget);
                            }
                        }
                        break;

                    case UndertaleInstruction.Opcode.Call:
                        {
                            List<Expression> args = new List<Expression>();
                            for (int j = 0; j < instr.ArgumentsCount; j++)
                                args.Add(stack.Pop());

                            if (instr.Function.Target.Name.Content == "method" && args.Count == 2)
                            {
                                // Special case - method creation
                                // See if the body should be inlined

                                Expression arg1 = args[0];
                                while (arg1 is ExpressionCast cast)
                                    arg1 = cast.Argument;
                                Expression arg2 = args[1];
                                while (arg2 is ExpressionCast cast)
                                    arg2 = cast.Argument;

                                if (arg2 is ExpressionConstant argCode && argCode.Type == UndertaleInstruction.DataType.Int32 &&
                                    argCode.Value is UndertaleInstruction.Reference<UndertaleFunction> argCodeFunc)
                                {
                                    UndertaleCode functionBody = context.GlobalContext.Data.Code.First(x => x.Name.Content == argCodeFunc.Target.Name.Content);

                                    FunctionDefinition.FunctionType type = FunctionDefinition.FunctionType.Function;
                                    bool processChildEntry;

                                    if (arg1 is DirectFunctionCall call && call.Function.Name.Content == "@@NullObject@@")
                                    {
                                        type = FunctionDefinition.FunctionType.Constructor;
                                        processChildEntry = true;
                                    }
                                    else
                                        processChildEntry = context.AlreadyProcessed.Add(functionBody);

                                    if (context.TargetCode.ChildEntries.Contains(functionBody) && processChildEntry)
                                    {
                                        // This function is somewhere inside this UndertaleCode block
                                        // inline the definition
                                        Block functionBodyEntryBlock = blocks[functionBody.Offset / 4];
                                        stack.Push(new FunctionDefinition(argCodeFunc.Target, functionBody, functionBodyEntryBlock, type));
                                        workQueue.Push(new Tuple<Block, List<TempVarReference>>(functionBodyEntryBlock, new List<TempVarReference>()));
                                        break;
                                    }
                                }
                            }

                            UndertaleCode callTargetBody = context.GlobalContext.Data?.Code.FirstOrDefault(x => x.Name.Content == instr.Function.Target.Name.Content);
                            if (callTargetBody != null && callTargetBody.ParentEntry != null && !context.DisableAnonymousFunctionNameResolution)
                            {
                                // Special case: this is a direct reference to a method variable
                                // Figure out what its actual name is

                                static string FindActualNameForAnonymousCodeObject(DecompileContext context, UndertaleCode anonymousCodeObject)
                                {
                                    // Decompile the parent object, and find the anonymous function assignment
                                    DecompileContext childContext = new DecompileContext(context.GlobalContext, anonymousCodeObject.ParentEntry);
                                    childContext.DisableAnonymousFunctionNameResolution = true; // prevent recursion - we don't even need the names in the child block
                                    try
                                    {
                                        Dictionary<uint, Block> blocks2 = PrepareDecompileFlow(anonymousCodeObject.ParentEntry, new List<uint>() { 0 });
                                        DecompileFromBlock(childContext, blocks2, blocks2[0]);
                                        // This hack handles decompilation of code entries getting shorter, but not longer or out of place.
                                        // Probably is no longer needed since we now update Length mostly-correctly
                                        Block lastBlock;
                                        if (!blocks2.TryGetValue(anonymousCodeObject.Length / 4, out lastBlock))
                                            lastBlock = blocks2[blocks2.Keys.Max()];
                                        List<Statement> statements = HLDecompile(childContext, blocks2, blocks2[0], lastBlock);
                                        foreach (Statement stmt2 in statements)
                                        {
                                            if (stmt2 is AssignmentStatement assign &&
                                                assign.Value is FunctionDefinition funcDef &&
                                                funcDef.FunctionBodyCodeEntry == anonymousCodeObject)
                                            {
                                                if (funcDef.FunctionBodyEntryBlock.Address == anonymousCodeObject.Offset / 4)
                                                    return assign.Destination.Var.Name.Content;
                                                else
                                                    return string.Empty; //throw new Exception("Non-matching offset: " + funcDef.FunctionBodyEntryBlock.Address.ToString() + " versus " + (anonymousCodeObject.Offset / 4).ToString() + " (got name " + assign.Destination.Var.Name.Content + ")");
                                            }
                                        }
                                        throw new Exception("Unable to find the var name for anonymous code object " + anonymousCodeObject.Name.Content);
                                    }
                                    catch (Exception e)
                                    {
                                        context.GlobalContext.DecompilerWarnings.Add("/*\nWARNING: Recursive script decompilation (for member variable name resolution) failed for " + anonymousCodeObject.Name.Content + "\n\n" + e.ToString() + "\n*/");
                                        return string.Empty;
                                    }
                                }

                                string funcName;
                                if (!context.GlobalContext.AnonymousFunctionNameCache.TryGetValue(instr.Function.Target, out funcName))
                                {
                                    funcName = FindActualNameForAnonymousCodeObject(context, callTargetBody);
                                    context.GlobalContext.AnonymousFunctionNameCache.Add(instr.Function.Target, funcName);
                                }
                                if (funcName != string.Empty)
                                {
                                    stack.Push(new DirectFunctionCall(funcName, instr.Function.Target, instr.Type1, args));
                                    break;
                                }
                            }

                            stack.Push(new DirectFunctionCall(instr.Function.Target, instr.Type1, args));
                        }
                        break;

                    case UndertaleInstruction.Opcode.CallV:
                        {
                            Expression func = stack.Pop();
                            Expression func_this = stack.Pop();
                            List<Expression> args = new List<Expression>();
                            for (int j = 0; j < instr.Extra; j++)
                                args.Add(stack.Pop());
                            stack.Push(new IndirectFunctionCall(func_this, func, instr.Type1, args));
                        }
                        break;

                    case UndertaleInstruction.Opcode.Break:
                        // GMS 2.3 sub-opcodes
                        if (DecompileContext.GMS2_3 == true)
                        {
                            switch ((short)instr.Value)
                            {
                                case -2: // GMS2.3+, pushaf
                                    {
                                        // TODO, work out more specifics here, like ++
                                        Expression ind = stack.Pop();
                                        Expression target = stack.Pop();
                                        if (target is ExpressionVar targetVar)
                                        {
                                            if (targetVar.VarType != UndertaleInstruction.VariableType.ArrayPushAF && targetVar.VarType != UndertaleInstruction.VariableType.ArrayPopAF) // The popaf arrays support pushaf as well, judging by how they are used with dup
                                                throw new InvalidOperationException("Tried to pushaf on var of type " + targetVar.VarType);

                                            ExpressionVar newVar = new ExpressionVar(targetVar.Var, targetVar.InstType, targetVar.VarType);
                                            newVar.Opcode = instr.Kind;
                                            newVar.ArrayIndices = new List<Expression>(targetVar.ArrayIndices);
                                            newVar.ArrayIndices.Add(ind);
                                            stack.Push(newVar);
                                        }
                                        else
                                            throw new InvalidOperationException("Tried to pushaf on something that is not a var");
                                    }
                                    break;
                                case -3: // GMS2.3+, popaf
                                    {
                                        // TODO, work out more specifics here, like ++
                                        Expression ind = stack.Pop();
                                        Expression target = stack.Pop();
                                        if (target is ExpressionVar targetVar)
                                        {
                                            if (targetVar.VarType != UndertaleInstruction.VariableType.ArrayPopAF)
                                                throw new InvalidOperationException("Tried to popaf on var of type " + targetVar.VarType);

                                            ExpressionVar newVar = new ExpressionVar(targetVar.Var, targetVar.InstType, targetVar.VarType);
                                            newVar.Opcode = instr.Kind;
                                            newVar.ArrayIndices = new List<Expression>(targetVar.ArrayIndices);
                                            newVar.ArrayIndices.Add(ind);

                                            Expression value = stack.Pop();
                                            statements.Add(new AssignmentStatement(newVar, value));
                                        }
                                        else
                                            throw new InvalidOperationException("Tried to popaf on something that is not a var");
                                    }
                                    break;
                                case -4: // GMS2.3+, pushac
                                    {
                                        Expression ind = stack.Pop();
                                        Expression target = stack.Pop();
                                        if (target is ExpressionVar targetVar)
                                        {
                                            if (targetVar.VarType != UndertaleInstruction.VariableType.ArrayPushAF && targetVar.VarType != UndertaleInstruction.VariableType.ArrayPopAF)
                                                throw new InvalidOperationException("Tried to pushac on var of type " + targetVar.VarType);

                                            ExpressionVar newVar = new ExpressionVar(targetVar.Var, targetVar.InstType, targetVar.VarType);
                                            newVar.Opcode = instr.Kind;
                                            newVar.ArrayIndices = new List<Expression>(targetVar.ArrayIndices);
                                            newVar.ArrayIndices.Add(ind);
                                            stack.Push(newVar);
                                        }
                                        else
                                            throw new InvalidOperationException("Tried to pushac on something that is not a var");
                                    }
                                    break;
                                case -5: // GMS2.3+, setowner
                                    // Stop 'setowner' values from leaking into the decompiled output as tempvars.
                                    // Used in the VM to let copy-on-write functionality work, but unnecessary for decompilation
                                    stack.Pop();
                                    /*
                                    var statement = stack.Pop();
                                    object owner;
                                    if (statement is ExpressionConstant)
                                        owner = (statement as ExpressionConstant).Value?.ToString();
                                    else
                                        owner = statement.ToString(context);
                                    statements.Add(new CommentStatement("setowner: " + (owner ?? "<null>")));
                                    */
                                    break;
                                case -10: // GMS2.3+, chknullish

                                    // TODO: Implement nullish operator in decompiled output.
                                    // Appearance in assembly is:

                                    /* <push var>
                                     * chknullish
                                     * bf [block2]
                                     *
                                     * :[block1]
                                     * popz.v
                                     * <var is nullish, evaluate new value>
                                     *
                                     * :[block2]
                                     * <use value>
                                     */

                                    // Note that this operator peeks from the stack, it does not pop directly.
                                    break;
                            }
                        }

                        // chkindex is used for checking bounds in 2D arrays
                        // I'm not sure of the specifics but I guess it causes a debug breakpoint if the top of the stack is >= 32000
                        // anyway, that's not important when decompiling to high-level code so just ignore it
                        break;
                }
            }

            // Convert everything that remains on the stack to a temp var
            List<TempVarReference> leftovers = new List<TempVarReference>();
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (i < tempvars.Count)
                {
                    Expression val = stack.Pop();
                    if (!(val is ExpressionTempVar) || (val as ExpressionTempVar).Var != tempvars[i]) {
                        var assignment = new TempVarAssignmentStatement(tempvars[i], val);
                        statements.Add(assignment);

                        if (val is ExpressionConstant) {
                            context.TempVarMap[tempvars[i].Var.Name] = assignment;
                        }
                    }

                    leftovers.Add(tempvars[i]);
                }
                else
                {
                    Expression val = stack.Pop();
                    TempVar var = context.NewTempVar();
                    var.Type = val.Type;
                    TempVarReference varref = new TempVarReference(var);
                    var assignment = new TempVarAssignmentStatement(varref, val);
                    statements.Add(assignment);
                    leftovers.Add(varref);

                    if (val is ExpressionConstant) {
                        context.TempVarMap[var.Name] = assignment;
                    }
                }
            }
            leftovers.Reverse();

            block.Statements = statements;
            // If we returned from this block, don't go to the "next" block, because that's totally wrong.
            if (!returned)
            {
                if (block.nextBlockFalse != null)
                    workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockFalse, leftovers));
                if (block.nextBlockTrue != null)
                    workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockTrue, leftovers));
            }
            else if (block.nextBlockFalse != null && block.nextBlockFalse.nextBlockFalse == null)
            {
                // Last block- make an exception for this one.
                workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockFalse, leftovers));
            }
        }

        public static void DecompileFromBlock(DecompileContext context, Dictionary<uint, Block> blocks, Block block)
        {
            Stack<Tuple<Block, List<TempVarReference>>> workQueue = new Stack<Tuple<Block, List<TempVarReference>>>();
            workQueue.Push(new Tuple<Block, List<TempVarReference>>(block, new List<TempVarReference>()));
            while (workQueue.Count > 0)
            {
                var item = workQueue.Pop();
                DecompileFromBlock(context, blocks, item.Item1, item.Item2, workQueue);
            }
        }

        public static Dictionary<uint, Block> DecompileFlowGraph(UndertaleCode code, List<uint> entryPoints)
        {
            Dictionary<uint, Block> blockByAddress = new Dictionary<uint, Block>();
            foreach(uint entryPoint in entryPoints)
                blockByAddress[entryPoint] = new Block(entryPoint);
            Block entryBlock = new Block(null);
            Block finalBlock = new Block(code.Length / 4);
            blockByAddress[code.Length / 4] = finalBlock;
            Block currentBlock = entryBlock;

            foreach (var instr in code.Instructions)
            {
                if (blockByAddress.ContainsKey(instr.Address))
                {
                    if (currentBlock != null)
                    {
                        currentBlock.conditionalExit = false;
                        currentBlock.nextBlockTrue = blockByAddress[instr.Address];
                        currentBlock.nextBlockFalse = blockByAddress[instr.Address];
                        blockByAddress[instr.Address].entryPoints.Add(currentBlock);
                    }
                    currentBlock = blockByAddress[instr.Address];
                }

                if (currentBlock == null)
                {
                    blockByAddress[instr.Address] = currentBlock = new Block(instr.Address);
                }

                currentBlock.Instructions.Add(instr);

                Func<uint, Block> GetBlock = (uint addr) =>
                {
                    Block nextBlock;
                    if (!blockByAddress.TryGetValue(addr, out nextBlock))
                    {
                        if (addr <= instr.Address)
                        {
                            // We have a jump into INSIDE one of previous blocks
                            // This is likely a loop or something
                            // We'll have to split that block into two

                            // First, find the block we have to split
                            Block blockToSplit = null;
                            foreach (var block in blockByAddress)
                            {
                                if (block.Key < addr && (blockToSplit == null || block.Key > blockToSplit.Address))
                                    blockToSplit = block.Value;
                            }

                            // Now, split the list of instructions into two
                            List<UndertaleInstruction> instrBefore = new List<UndertaleInstruction>();
                            List<UndertaleInstruction> instrAfter = new List<UndertaleInstruction>();
                            foreach (UndertaleInstruction inst in blockToSplit.Instructions)
                            {
                                if (inst.Address < addr)
                                    instrBefore.Add(inst);
                                else
                                    instrAfter.Add(inst);
                            }

                            // Create the newly split block
                            Block newBlock = new Block(addr);
                            blockToSplit.Instructions = instrBefore;
                            newBlock.Instructions = instrAfter;
                            newBlock.conditionalExit = blockToSplit.conditionalExit;
                            newBlock.nextBlockTrue = blockToSplit.nextBlockTrue;
                            newBlock.nextBlockFalse = blockToSplit.nextBlockFalse;
                            blockToSplit.conditionalExit = false;
                            blockToSplit.nextBlockTrue = newBlock;
                            blockToSplit.nextBlockFalse = newBlock;
                            blockByAddress[addr] = newBlock;
                            return newBlock;
                        }
                        else
                        {
                            blockByAddress.Add(addr, nextBlock = new Block(addr));
                        }
                    }
                    return nextBlock;
                };

                if (instr.Kind == UndertaleInstruction.Opcode.B)
                {
                    uint addr = (uint)(instr.Address + instr.JumpOffset);
                    Block nextBlock = GetBlock(addr);
                    currentBlock.conditionalExit = false;
                    currentBlock.nextBlockTrue = nextBlock;
                    currentBlock.nextBlockFalse = nextBlock;
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf)
                {
                    Block nextBlockIfMet = GetBlock((uint)(instr.Address + instr.JumpOffset));
                    Block nextBlockIfNotMet = GetBlock(instr.Address + 1);
                    currentBlock.conditionalExit = true;
                    currentBlock.nextBlockTrue = instr.Kind == UndertaleInstruction.Opcode.Bt ? nextBlockIfMet : nextBlockIfNotMet;
                    currentBlock.nextBlockFalse = instr.Kind == UndertaleInstruction.Opcode.Bt ? nextBlockIfNotMet : nextBlockIfMet;
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.PushEnv || instr.Kind == UndertaleInstruction.Opcode.PopEnv)
                {
                    Block nextBlock = GetBlock(instr.Address + 1);
                    currentBlock.conditionalExit = false;
                    currentBlock.nextBlockTrue = nextBlock;
                    currentBlock.nextBlockFalse = nextBlock;
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                {
                    Block nextBlock = GetBlock(instr.Address + 1);
                    currentBlock.conditionalExit = false;
                    currentBlock.nextBlockTrue = nextBlock;
                    currentBlock.nextBlockFalse = nextBlock;
                    currentBlock = null;
                }
            }
            if (currentBlock != null)
            {
                currentBlock.nextBlockTrue = finalBlock;
                currentBlock.nextBlockFalse = finalBlock;
            }
            foreach (var block in blockByAddress.Values)
            {
                if (block.nextBlockTrue != null && !block.nextBlockTrue.entryPoints.Contains(block))
                    block.nextBlockTrue.entryPoints.Add(block);
                if (block.nextBlockFalse != null && !block.nextBlockFalse.entryPoints.Contains(block))
                    block.nextBlockFalse.entryPoints.Add(block);
            }
            return blockByAddress;
        }

        public abstract class HLStatement : Statement
        {
            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                throw new NotImplementedException();
            }
        };

        public class BlockHLStatement : HLStatement
        {
            public List<Statement> Statements = new List<Statement>();

            public string ToString(DecompileContext context, bool canSkipBrackets = true, bool forceSkipBrackets = false)
            {
                context.IndentationLevel++;
                if (canSkipBrackets && CanSkipBrackets(this))
                {
                    string res = DecompileContext.Indent + Statements[0].ToString(context);
                    context.IndentationLevel--;
                    return res;
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    if (!forceSkipBrackets)
                        sb.Append("{\n");
                    foreach (var stmt in Statements)
                    {
                        if (stmt is AssignmentStatement assign && assign.IsStructDefinition)
                            continue;

                        sb.Append(context.Indentation);
                        string resultStr = stmt.ToString(context);
                        sb.Append(resultStr).Append('\n');
                    }
                    context.IndentationLevel--;
                    if (!forceSkipBrackets)
                        sb.Append(context.Indentation + "}");
                    return sb.ToString().Trim('\n');
                }
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                for (var i = 0; i < Statements.Count; i++)
                {
                    var count = Statements.Count;
                    var Result = Statements[i]?.CleanStatement(context, this); // Yes, this uses "this" and not "block".
                    i -= (count - Statements.Count); // If removed.
                    Statements[i] = Result;
                }
                return this;
            }

            public BlockHLStatement CleanBlockStatement(DecompileContext context)
            {
                return CleanStatement(context, null) as BlockHLStatement;
            }

            public override string ToString(DecompileContext context)
            {
                return ToString(context, true);
            }

            private static bool CanSkipBrackets(BlockHLStatement blockStatement)
            {
                if (blockStatement == null || blockStatement.Statements.Count != 1)
                    return false; // Nope! Need brackets!

                Statement statement = blockStatement.Statements[0];
                return !(statement is IfHLStatement || statement is LoopHLStatement || statement is HLSwitchStatement || statement is WithHLStatement); // Nesting these can cause issues.
            }
        };

        public class IfHLStatement : HLStatement
        {
            public Expression condition;
            public BlockHLStatement trueBlock;
            public List<ValueTuple<Expression, BlockHLStatement>> elseConditions = new List<ValueTuple<Expression, BlockHLStatement>>();
            public BlockHLStatement falseBlock;

            public bool HasElseIf { get => elseConditions != null && elseConditions.Count > 0; }
            public bool HasElse { get => falseBlock != null && falseBlock.Statements.Count > 0; }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                condition = condition?.CleanExpression(context, block);
                trueBlock = trueBlock?.CleanBlockStatement(context);
                falseBlock = falseBlock?.CleanBlockStatement(context);

                int myIndex = block.Statements.IndexOf(this);

                // Prevents if (tempvar - 1), when it should be if (tempvar)
                if ((condition as ExpressionCast)?.Argument is ExpressionTwo && myIndex > 0)
                {
                    ExpressionTwo conditionExpression = ((ExpressionCast)condition).Argument as ExpressionTwo;
                    Statement lastStatement = block.Statements[myIndex - 1];

                    if (conditionExpression.Argument1 is ExpressionTempVar tempVar && lastStatement is TempVarAssignmentStatement statement && conditionExpression.Argument2 is ExpressionConstant
                        && tempVar.Var.Var == statement.Var.Var)
                        condition = conditionExpression.Argument1;
                }

                // Use if -> else if, instead of nesting ifs.
                while (falseBlock.Statements.Count == 1 && falseBlock.Statements[0] is IfHLStatement nestedIf) // The condition of one if statement.
                {
                    elseConditions.Add(new ValueTuple<Expression, BlockHLStatement>(nestedIf.condition, nestedIf.trueBlock));
                    elseConditions.AddRange(nestedIf.elseConditions);
                    falseBlock = nestedIf.falseBlock;
                }

                // Collapse conditions into && + || + ternary.
                if (HasElse && !HasElseIf && trueBlock.Statements.Count == 1 && falseBlock.Statements.Count == 1)
                {
                    TempVarAssignmentStatement trueAssign = trueBlock.Statements[0] as TempVarAssignmentStatement;
                    TempVarAssignmentStatement falseAssign = falseBlock.Statements[0] as TempVarAssignmentStatement;

                    if (trueAssign != null && falseAssign != null && trueAssign.Var.Var == falseAssign.Var.Var)
                    {
                        TempVarAssignmentStatement newAssign;
                        if (TestNumber(trueAssign.Value, 1) && (falseAssign.Var.Var.Type == UndertaleInstruction.DataType.Boolean || falseAssign.Value.Type == UndertaleInstruction.DataType.Boolean))
                            newAssign = new TempVarAssignmentStatement(trueAssign.Var, new ExpressionTwoSymbol("||", UndertaleInstruction.DataType.Boolean, condition, falseAssign.Value));
                        else if (TestNumber(falseAssign.Value, 0) && (trueAssign.Var.Var.Type == UndertaleInstruction.DataType.Boolean || trueAssign.Value.Type == UndertaleInstruction.DataType.Boolean))
                            newAssign = new TempVarAssignmentStatement(trueAssign.Var, new ExpressionTwoSymbol("&&", UndertaleInstruction.DataType.Boolean, condition, trueAssign.Value));
                        else
                            newAssign = new TempVarAssignmentStatement(trueAssign.Var, new ExpressionTernary(trueAssign.Value.Type, condition, trueAssign.Value, falseAssign.Value));

                        context.TempVarMap[newAssign.Var.Var.Name] = newAssign;
                        return newAssign;
                    }
                }

                // Create repeat loops.
                if (HasElse && !HasElseIf && trueBlock.Statements.Count == 0 && falseBlock.Statements.Count == 1 && falseBlock.Statements[0] is LoopHLStatement
                        && condition is ExpressionCompare && myIndex > 0 && block.Statements[myIndex - 1] is TempVarAssignmentStatement)
                {
                    ExpressionCompare compareCondition = condition as ExpressionCompare;
                    LoopHLStatement loop = falseBlock.Statements[0] as LoopHLStatement;
                    TempVarAssignmentStatement priorAssignment = block.Statements[myIndex - 1] as TempVarAssignmentStatement;
                    Expression startValue = priorAssignment.Value;

                    List<Statement> loopCode = loop.Block.Statements;
                    if (priorAssignment != null &&
                        loop.IsWhileLoop &&
                        loop.Condition == null &&
                        //loopCode.Count > 2 &&
                        compareCondition.Opcode == UndertaleInstruction.ComparisonType.LTE &&
                        TestNumber(compareCondition.Argument2, 0) &&
                        compareCondition.Argument1.ToString(context) == startValue.ToString(context))
                    {
                        TempVarAssignmentStatement repeatAssignment = null;
                        IfHLStatement loopCheckStatement = null;
                        bool hasBreak = false;
                        List<Statement> insideElseBlock = null;

                        if (loopCode.Count > 2)
                        {
                            repeatAssignment = loopCode[loopCode.Count - 2] as TempVarAssignmentStatement;
                            loopCheckStatement = loopCode[loopCode.Count - 1] as IfHLStatement;
                        }

                        if ((repeatAssignment == null || loopCheckStatement == null) &&
                            loopCode[loopCode.Count - 1] is IfHLStatement wrapperIfStatement &&
                            wrapperIfStatement.HasElse
                           ) // single-level break detection
                        {
                            insideElseBlock = wrapperIfStatement.falseBlock.Statements;
                            wrapperIfStatement.trueBlock.Statements.Add(new BreakHLStatement());
                            if (insideElseBlock.Count > 2)
                            {
                                repeatAssignment = insideElseBlock[insideElseBlock.Count - 2] as TempVarAssignmentStatement;
                                loopCheckStatement = insideElseBlock[insideElseBlock.Count - 1] as IfHLStatement;
                                hasBreak = true;
                            }
                        }

                        if (repeatAssignment != null && loopCheckStatement != null)
                        { // tempVar = (tempVar -1); -> if (tempVar) continue -> break

                            // if (tempVar) {continue} else {empty}

                            if (loopCheckStatement.trueBlock.Statements.Count == 1
                                && !loopCheckStatement.HasElse
                                && !loopCheckStatement.HasElseIf
                                && loopCheckStatement.trueBlock.Statements[0] is ContinueHLStatement
                                && loopCheckStatement.condition.ToString(context) == repeatAssignment.Value.ToString(context))
                            {
                                (hasBreak ? insideElseBlock : loopCode).Remove(repeatAssignment);
                                (hasBreak ? insideElseBlock : loopCode).Remove(loopCheckStatement);
                                block.Statements.Remove(priorAssignment);

                                loop.RepeatStartValue = startValue;
                                return loop;
                            }
                        }
                    }
                }

                for (int i = 0; i < elseConditions.Count; i++)
                {
                    var pair = elseConditions[i];
                    pair.Item1 = pair.Item1?.CleanExpression(context, block);
                    pair.Item2 = pair.Item2?.CleanBlockStatement(context);
                }


                return this;
            }

            public override string ToString(DecompileContext context)
            {
                StringBuilder sb = new StringBuilder();
                string cond;
                if (condition is ExpressionCompare)
                    cond = (condition as ExpressionCompare).ToStringWithParen(context);
                else
                    cond = condition.ToString(context);
                sb.Append("if " + cond + "\n");
                sb.Append(context.Indentation + trueBlock.ToString(context));

                foreach (ValueTuple<Expression, BlockHLStatement> tuple in elseConditions)
                {
                    if (tuple.Item1 is ExpressionCompare)
                        cond = (tuple.Item1 as ExpressionCompare).ToStringWithParen(context);
                    else
                        cond = tuple.Item1.ToString(context);
                    sb.Append("\n" + context.Indentation + "else if " + cond + "\n");
                    sb.Append(context.Indentation + tuple.Item2.ToString(context));
                }

                if (HasElse)
                {
                    sb.Append("\n" + context.Indentation + "else\n");
                    sb.Append(context.Indentation + falseBlock.ToString(context));
                }
                return sb.ToString();
            }
        };

        public class LoopHLStatement : HLStatement
        {
            // Loop block
            public BlockHLStatement Block;

            // While / for condition
            public Statement Condition;

            // While loop
            public bool IsWhileLoop { get => !IsForLoop && !IsRepeatLoop && !IsDoUntilLoop; }
            public bool IsDoUntilLoop;

            // For loop
            public bool IsForLoop { get => InitializeStatement != null && StepStatement != null && Condition != null; }
            public AssignmentStatement InitializeStatement;
            public AssignmentStatement StepStatement;

            // Repeat loop
            public bool IsRepeatLoop { get => RepeatStartValue != null; }
            public Statement RepeatStartValue;

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                int myIndex = block.Statements.IndexOf(this);
                Block = Block?.CleanBlockStatement(context);
                Condition = Condition?.CleanStatement(context, block);
                InitializeStatement = InitializeStatement?.CleanStatement(context, block) as AssignmentStatement;
                StepStatement = StepStatement?.CleanStatement(context, block) as AssignmentStatement;
                RepeatStartValue = RepeatStartValue?.CleanStatement(context, block);

                if (IsWhileLoop)
                {
                    // While loops have conditions.
                    if (Block.Statements.Count == 1)
                    {
                        Statement firstStatement = Block.Statements[0];

                        if (firstStatement is IfHLStatement)
                        {
                            IfHLStatement ifStatement = (IfHLStatement)firstStatement;
                            if (ifStatement.falseBlock is BlockHLStatement statement && statement.Statements.Count == 0 && !ifStatement.HasElseIf)
                            {
                                Condition = ifStatement.condition;
                                Block.Statements.Remove(firstStatement); // Remove if statement.
                                Block.Statements.InsertRange(0, ifStatement.trueBlock.Statements); // Add if contents.
                            }
                        }
                    }

                    // If it's been shoved into an if else, take it out of there.
                    //TODO: This is disabled until further notice. The problem is that for loops that do this can use continues. The issue there is we don't have an easy way to remove the increment from the decompile at the moment.
                    /*if (Block.Statements.Count > 0 && Block.Statements.Last() is IfHLStatement)
                    {
                        IfHLStatement ifStatement = Block.Statements.Last() as IfHLStatement;
                        BlockHLStatement blockStatement = ifStatement.falseBlock as BlockHLStatement;

                        if (blockStatement != null && blockStatement.Statements.Count > 0 && blockStatement.Statements.Last() is ContinueHLStatement)
                        {
                            blockStatement.Statements.Remove(blockStatement.Statements.Last());
                            Block.Statements.AddRange(blockStatement.Statements);
                            ifStatement.falseBlock.Statements.Clear();
                        }
                    }*/

                    // Remove redundant continues at the end of the loop.
                    if (Block.Statements.Count > 0)
                    {
                        Statement lastStatement = Block.Statements.Last();
                        if (lastStatement is ContinueHLStatement)
                            Block.Statements.Remove(lastStatement);
                    }

                    // Convert into a for loop.
                    if (myIndex > 0 && block.Statements[myIndex - 1] is AssignmentStatement assignment
                        && Block.Statements.Count > 0 && Block.Statements.Last() is AssignmentStatement increment
                        && Condition is ExpressionCompare compare)
                    {
                        UndertaleVariable variable = assignment.Destination.Var;

                        if (((compare.Argument1 is ExpressionVar exprVar && (exprVar.Var == variable)) || (compare.Argument2 is ExpressionVar exprVar2 && (exprVar2.Var == variable))) && increment.Destination.Var == variable)
                        {
                            block.Statements.Remove(assignment);
                            InitializeStatement = assignment;
                            Block.Statements.Remove(increment);
                            StepStatement = increment;
                        }
                    }

                    if (Condition == null)
                    {
                        if (Block.Statements.Last() is IfHLStatement)
                        {
                            IfHLStatement ifStatement = Block.Statements.Last() as IfHLStatement;
                            if (ifStatement.trueBlock.Statements.Count == 0 && ifStatement.falseBlock.Statements.Count == 1 && ifStatement.falseBlock.Statements[0] is ContinueHLStatement && ifStatement.elseConditions.Count == 0)
                            {
                                IsDoUntilLoop = true;
                                Condition = ifStatement.condition;
                                Block.Statements.Remove(ifStatement);
                            }
                        }
                    }
                }

                return this;
            }

            public override string ToString(DecompileContext context)
            {
                if (IsRepeatLoop)
                {
                    bool needsParen = RepeatStartValue is ExpressionConstant || RepeatStartValue is ExpressionCompare;
                    return "repeat " + (needsParen ? "(" : "") + RepeatStartValue.ToString(context) + (needsParen ? ")" : "") + "\n" + context.Indentation + Block.ToString(context);
                }

                if (IsForLoop)
                {
                    string conditionStr = Condition.ToString(context); // Cut off parenthesis for the condition.
                    if (conditionStr.StartsWith("(", StringComparison.InvariantCulture) && conditionStr.EndsWith(")", StringComparison.InvariantCulture))
                        conditionStr = conditionStr.Substring(1, conditionStr.Length - 2);

                    return "for (" + InitializeStatement.ToString(context) + "; " + conditionStr + "; " + StepStatement.ToString(context) + ")\n" + context.Indentation + Block.ToString(context);
                }

                string cond;
                if (Condition is ExpressionCompare)
                    cond = (Condition as ExpressionCompare).ToStringWithParen(context);
                else if (IsDoUntilLoop)
                    cond = Condition?.ToString(context) ?? "(false)";
                else
                    cond = Condition != null ? Condition.ToString(context) : "(true)";

                if (IsDoUntilLoop)
                    return "do\n" + context.Indentation + Block.ToString(context, false) + " until " + cond + ";";

                return "while " + cond + "\n" + context.Indentation + Block.ToString(context);
            }
        };

        public class ContinueHLStatement : HLStatement
        {
            public override string ToString(DecompileContext context)
            {
                return "continue";
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                return this;
            }
        }

        public class BreakHLStatement : HLStatement
        {
            public override string ToString(DecompileContext context)
            {
                return "break";
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                return this;
            }
        }

        public class WithHLStatement : HLStatement
        {
            public Expression NewEnv;
            public BlockHLStatement Block;

            public override string ToString(DecompileContext context)
            {
                return "with (" + NewEnv.ToString(context) + ")\n" + context.Indentation + Block.ToString(context);
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                NewEnv = NewEnv?.CleanExpression(context, block);
                Block = Block?.CleanBlockStatement(context);
                return this;
            }
        }

        public class HLSwitchStatement : HLStatement
        {
            private Expression SwitchExpression;
            private List<HLSwitchCaseStatement> Cases;

            public HLSwitchStatement(Expression switchExpression, List<HLSwitchCaseStatement> cases)
            {
                this.SwitchExpression = switchExpression;
                this.Cases = cases;
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                SwitchExpression = SwitchExpression?.CleanExpression(context, block);
                for (var i = 0; i < Cases.Count; i++)
                    Cases[i] = Cases[i]?.CleanStatement(context, block) as HLSwitchCaseStatement;

                return this;
            }

            public override string ToString(DecompileContext context)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("switch " + SwitchExpression.ToString(context) + "\n");
                sb.Append(context.Indentation + "{\n");
                context.IndentationLevel++;
                foreach (var casee in Cases)
                {
                    sb.Append(context.Indentation + casee.ToString(context));
                    sb.Append('\n');
                }
                context.IndentationLevel--;
                sb.Append(context.Indentation + "}\n");
                return sb.ToString();
            }
        }

        public class HLSwitchCaseStatement : HLStatement
        {
            public List<Expression> CaseExpressions;
            public BlockHLStatement Block;

            public HLSwitchCaseStatement(List<Expression> caseExpressions, BlockHLStatement block)
            {
                DebugUtil.Assert(caseExpressions.Count > 0, "Switch statement lacks any cases.");
                this.CaseExpressions = caseExpressions;
                this.Block = block;
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                for (var i = 0; i < CaseExpressions.Count; i++)
                    CaseExpressions[i] = CaseExpressions[i]?.CleanExpression(context, block);
                Block = Block?.CleanBlockStatement(context);
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < CaseExpressions.Count; i++)
                {
                    Expression caseExpr = CaseExpressions[i];
                    if (i != 0)
                        sb.Append(context.Indentation);
                    if (caseExpr != null)
                        sb.Append("case " + caseExpr.ToString(context) + ":\n");
                    else
                        sb.Append("default:\n");
                }
                if (Block.Statements.Count > 0)
                {
                    sb.Append(Block.ToString(context, false, true));
                }
                return sb.ToString();
            }
        }

        // Based on http://www.backerstreet.com/decompiler/loop_analysis.php
        public static Dictionary<Block, List<Block>> ComputeReverseDominators(Dictionary<uint, Block> blocks, Block entryBlock)
        {
            Block[] blockList = blocks.Values.ToArray();
            CustomBitArray[] dominators = new CustomBitArray[blockList.Length];

            int entryBlockId = -1;
            {
                int i;
                for (i = 0; i < blockList.Length; i++)
                {
                    Block b = blockList[i];
                    b._CachedIndex = i;

                    CustomBitArray ba;

                    if (blockList[i] == entryBlock)
                    {
                        entryBlockId = i;
                        ba = new CustomBitArray(blockList.Length);
                        ba.SetTrue(i);
                        dominators[i] = ba;
                        break;
                    }

                    ba = new CustomBitArray(blockList.Length);
                    ba.SetAllTrue();
                    dominators[i] = ba;
                }
                for (i++; i < blockList.Length; i++)
                {
                    blockList[i]._CachedIndex = i;
                    CustomBitArray ba = new CustomBitArray(blockList.Length);
                    ba.SetAllTrue();
                    dominators[i] = ba;
                }
            }

            bool changed;
            Block[] reverseUse1 = { null };
            Block[] reverseUse2 = { null, null };
            do
            {
                changed = false;
                for (int i = 0; i < blockList.Length; i++)
                {
                    if (i == entryBlockId)
                        continue;

                    Block b = blockList[i];

                    IEnumerable<Block> e;
                    if (b.conditionalExit)
                    {
                        reverseUse2[0] = b.nextBlockTrue;
                        reverseUse2[1] = b.nextBlockFalse;
                        e = reverseUse2;
                    }
                    else
                    {
                        reverseUse1[0] = b.nextBlockTrue;
                        e = reverseUse1;
                    }

                    foreach (Block pred in e)
                        changed |= pred != null && dominators[i].And(dominators[pred._CachedIndex], i);
                }
            } while (changed);

            Dictionary<Block, List<Block>> result = new Dictionary<Block, List<Block>>(blockList.Length);
            for (var i = 0; i < blockList.Length; i++)
            {
                CustomBitArray curr = dominators[i];
                result[blockList[i]] = new List<Block>(4);
                for (var j = 0; j < blockList.Length; j++)
                {
                    if (curr.Get(j))
                        result[blockList[i]].Add(blockList[j]);
                }
            }

            return result;
        }

        private static List<Block> NaturalLoopForEdge(Block header, Block tail)
        {
            Stack<Block> workList = new Stack<Block>(16);
            List<Block> loopBlocks = new List<Block>(8);

            loopBlocks.Add(header);
            if (header != tail)
            {
                loopBlocks.Add(tail);
                workList.Push(tail);
            }

            while (workList.Count > 0)
            {
                Block block = workList.Pop();
                foreach (Block pred in block.entryPoints)
                {
                    if (!loopBlocks.Contains(pred))
                    {
                        loopBlocks.Add(pred);
                        workList.Push(pred);
                    }
                }
            }

            return loopBlocks;
        }

        private static Dictionary<Block, List<Block>> ComputeNaturalLoops(Dictionary<uint, Block> blocks, Block entryBlock)
        {
            Dictionary<Block, List<Block>> loopSet = new Dictionary<Block, List<Block>>();

            foreach (var block in blocks.Values)
            {
                // Every successor that dominates its predecessor
                // must be the header of a loop.
                // That is, block -> succ is a back edge.

                // Future update: We're going to take a much more efficient but assuming
                // route that the compiler outputs in a specific order, which it always should

                if (block.nextBlockTrue != null && !loopSet.ContainsKey(block.nextBlockTrue))
                {
                    if (block.nextBlockTrue.Address <= block.Address)
                        loopSet.Add(block.nextBlockTrue, NaturalLoopForEdge(block.nextBlockTrue, block));
                }
                if (block.nextBlockFalse != null && block.nextBlockTrue != block.nextBlockFalse && !loopSet.ContainsKey(block.nextBlockFalse))
                {
                    if (block.nextBlockFalse.Address <= block.Address)
                        loopSet.Add(block.nextBlockFalse, NaturalLoopForEdge(block.nextBlockFalse, block));
                }
            }

            return loopSet;
        }

        public static Block FindFirstMeetPoint(Block ifStart, Dictionary<Block, List<Block>> reverseDominators)
        {
            DebugUtil.Assert(ifStart.conditionalExit, "If start does not have a conditional exit");
            var commonDominators = reverseDominators[ifStart.nextBlockTrue].Intersect(reverseDominators[ifStart.nextBlockFalse]);

            // Find the closest one of them
            List<Block> visited = new List<Block>();
            visited.Add(ifStart);
            Queue<Block> q = new Queue<Block>();
            q.Enqueue(ifStart.nextBlockTrue);
            q.Enqueue(ifStart.nextBlockFalse);
            while (q.Count > 0)
            {
                Block b = q.Dequeue();
                if (commonDominators.Contains(b))
                    return b;
                visited.Add(b);
                if (b.nextBlockTrue != null && !visited.Contains(b.nextBlockTrue) && !q.Contains(b.nextBlockTrue))
                    q.Enqueue(b.nextBlockTrue);
                if (b.nextBlockFalse != null && !visited.Contains(b.nextBlockFalse) && !q.Contains(b.nextBlockFalse))
                    q.Enqueue(b.nextBlockFalse);
            }
            return null;
        }

        // Process the base decompilation: clean up, make it readable, identify structures
        private static BlockHLStatement HLDecompileBlocks(DecompileContext context, ref Block block, Dictionary<uint, Block> blocks, Dictionary<Block, List<Block>> loops, Dictionary<Block, List<Block>> reverseDominators, List<Block> alreadyVisited, Block currentLoop = null, Block stopAt = null, Block breakTo = null, bool decompileTheLoop = false, uint depth = 0)
        {
            if (depth > 200)
                throw new Exception("Excessive recursion while processing blocks.");

            BlockHLStatement output = new BlockHLStatement();

            Block lastBlock = null;
            bool popenvDrop = false;
            while (block != stopAt && block != null)
            {
                lastBlock = block;

                if (loops.ContainsKey(block) && !decompileTheLoop)
                {
                    if (block == currentLoop)
                    {
                        output.Statements.Add(new ContinueHLStatement());
                        break;
                    }
                    else
                    {
                        LoopHLStatement statement = new LoopHLStatement() { Block = HLDecompileBlocks(context, ref block, blocks, loops, reverseDominators, alreadyVisited, block, null, block.nextBlockFalse, true, depth + 1) };
                        output.Statements.Add(statement);
                        continue;
                    }
                }
                else if (currentLoop != null && !loops[currentLoop].Contains(block) && decompileTheLoop)
                {
                    break;
                }

                if (block.Statements == null)
                {
                    // This is possible with unused blocks (due to return)
                    block = stopAt;
                    continue;
                }

                if (!alreadyVisited.Contains(block))
                    alreadyVisited.Add(block);


                for (int i = 0; i < block.Statements.Count; i++)
                {
                    Statement stmt = block.Statements[i];
                    if (!(stmt is PushEnvStatement) && !(stmt is PopEnvStatement))
                        output.Statements.Add(stmt);
                }

                if (output.Statements.Count >= 1 && output.Statements[output.Statements.Count - 1] is TempVarAssignmentStatement &&
                    block.Instructions.Count >= 1 && block.Instructions[block.Instructions.Count - 1].Kind == UndertaleInstruction.Opcode.Bt &&
                    block.conditionalExit && block.ConditionStatement is ExpressionCompare &&
                    (block.ConditionStatement as ExpressionCompare).Opcode == UndertaleInstruction.ComparisonType.EQ)
                {
                    // Switch statement
                    Expression switchExpression = (output.Statements[output.Statements.Count - 1] as TempVarAssignmentStatement).Value;
                    TempVar switchTempVar = (output.Statements[output.Statements.Count - 1] as TempVarAssignmentStatement).Var.Var;
                    output.Statements.RemoveAt(output.Statements.Count - 1);

                    Block meetPoint = FindFirstMeetPoint(block, reverseDominators);
                    if (meetPoint == null)
                        throw new Exception("End of switch not found");

                    Dictionary<Block, List<Expression>> caseEntries = new Dictionary<Block, List<Expression>>();
                    while (block != meetPoint)
                    {
                        Expression caseExpr = null;
                        if (block.ConditionStatement != null)
                        {
                            ExpressionCompare cmp = (ExpressionCompare)block.ConditionStatement;
                            if (cmp.Argument1 != switchExpression &&
                                (!(cmp.Argument1 is ExpressionTempVar) || !(switchExpression is ExpressionTempVar) || (cmp.Argument1 as ExpressionTempVar).Var.Var != (switchExpression as ExpressionTempVar).Var.Var) &&
                                (!(cmp.Argument1 is ExpressionTempVar) || (cmp.Argument1 as ExpressionTempVar).Var.Var != switchTempVar))
                                throw new Exception("Malformed switch statement: bad condition var (" + cmp.Argument1.ToString(context) + ")");
                            if (cmp.Opcode != UndertaleInstruction.ComparisonType.EQ)
                                throw new Exception("Malformed switch statement: bad contition type (" + cmp.Opcode.ToString().ToUpper(CultureInfo.InvariantCulture) + ")");
                            caseExpr = cmp.Argument2;
                        }

                        if (!caseEntries.ContainsKey(block.nextBlockTrue))
                            caseEntries.Add(block.nextBlockTrue, new List<Expression>());
                        caseEntries[block.nextBlockTrue].Add(caseExpr);

                        if (!block.conditionalExit)
                        {
                            // Seems to be "default", and we simply want to go to the exit now.
                            // This is a little hack, but it should fully work. The compiler always
                            // emits "default" at the end it looks like. Also this navigates down the
                            // "false" branching paths over others- this should lead to the correct
                            // block. Without this, branching at the start of "default" will break
                            // this switch detection.
                            while (block.nextBlockTrue != meetPoint)
                            {
                                if (block.nextBlockFalse != null)
                                    block = block.nextBlockFalse;
                                else if (block.nextBlockTrue != null)
                                    block = block.nextBlockTrue;
                                else
                                    break;
                            }

                            break;
                        }

                        block = block.nextBlockFalse;
                    }

                    List<HLSwitchCaseStatement> cases = new List<HLSwitchCaseStatement>();
                    HLSwitchCaseStatement defaultCase = null;

                    for (var i = 0; i < caseEntries.Count; i++)
                    {
                        var x = caseEntries.ElementAt(i);
                        Block temp = x.Key;

                        Block switchEnd = DetermineSwitchEnd(temp, caseEntries.Count > (i + 1) ? caseEntries.ElementAt(i + 1).Key : null, meetPoint);

                        HLSwitchCaseStatement result = new HLSwitchCaseStatement(x.Value, HLDecompileBlocks(context, ref temp, blocks, loops, reverseDominators, alreadyVisited, currentLoop, switchEnd, switchEnd, false, depth + 1));
                        cases.Add(result);
                        if (result.CaseExpressions.Contains(null))
                            defaultCase = result;

                        DebugUtil.Assert(temp == switchEnd, "temp != switchEnd");
                    }


                    if (defaultCase != null && defaultCase.Block.Statements.Count == 0)
                    {
                        // Handles default case.
                        UndertaleInstruction breakInstruction = context.TargetCode.GetInstructionFromAddress((uint)block.Address + 1);

                        if (breakInstruction.Kind == UndertaleInstruction.Opcode.B)
                        {
                            // This is the default-case meet-point if it is b.
                            uint instructionId = ((uint)block.Address + 1 + (uint)breakInstruction.JumpOffset);
                            if (!blocks.ContainsKey(instructionId))
                                Debug.Fail("Switch statement default: Bad target [" + block.Address + ", " + breakInstruction.JumpOffset + "]: " + breakInstruction.ToString());
                            Block switchEnd = blocks[instructionId];

                            Block start = meetPoint;
                            defaultCase.Block = HLDecompileBlocks(context, ref start, blocks, loops, reverseDominators, alreadyVisited, currentLoop, switchEnd, switchEnd, false, depth + 1);
                            block = start; // Start changed in HLDecompileBlocks.
                        }
                        else
                        {
                            // If there is no default-case, remove the default break, since that creates different bytecode.
                            cases.Remove(defaultCase);
                        }
                    }
                    else
                    {
                        block = block.nextBlockTrue;
                    }

                    output.Statements.Add(new HLSwitchStatement(switchExpression, cases));
                    continue;
                }

                if (block.Statements.Count > 0 && block.Statements.Last() is PushEnvStatement)
                {
                    DebugUtil.Assert(!block.conditionalExit, "Block ending with pushenv does not have a conditional exit");
                    PushEnvStatement stmt = (block.Statements.Last() as PushEnvStatement);
                    block = block.nextBlockTrue;
                    output.Statements.Add(new WithHLStatement()
                    {
                        NewEnv = stmt.NewEnv,
                        Block = HLDecompileBlocks(context, ref block, blocks, loops, reverseDominators, alreadyVisited, null, stopAt, null, false, depth + 1)
                    });
                    if (block == null)
                        break;
                }
                else if (block.Statements.Count > 0 && block.Statements.Last() is PopEnvStatement)
                {
                    DebugUtil.Assert(!block.conditionalExit, "Block ending in popenv does not have a conditional exit");
                    break;
                }

                if (popenvDrop)
                    break;

                if (block.conditionalExit && block.ConditionStatement != null) // If statement
                {
                    Block meetPoint = FindFirstMeetPoint(block, reverseDominators);
                    if (meetPoint == null)
                        throw new Exception("End of if not found");

                    IfHLStatement cond = new IfHLStatement();
                    cond.condition = block.ConditionStatement;

                    Block blTrue = block.nextBlockTrue, blFalse = block.nextBlockFalse;
                    cond.trueBlock = HLDecompileBlocks(context, ref blTrue, blocks, loops, reverseDominators, alreadyVisited, currentLoop, meetPoint, breakTo, false, depth + 1);
                    cond.falseBlock = HLDecompileBlocks(context, ref blFalse, blocks, loops, reverseDominators, alreadyVisited, currentLoop, meetPoint, breakTo, false, depth + 1);
                    output.Statements.Add(cond); // Add the if statement.
                    block = meetPoint;
                }
                else
                {
                    // Don't continue if there's a return/exit, except for last block
                    if (block.Instructions.Count == 0)
                        block = block.nextBlockTrue;
                    else
                    {
                        var last = block.Instructions.Last();
                        var lastKind = last.Kind;
                        if (lastKind == UndertaleInstruction.Opcode.PopEnv && last.JumpOffsetPopenvExitMagic)
                        {
                            block = block.nextBlockTrue;
                            popenvDrop = true;
                        } else
                            block = ((lastKind != UndertaleInstruction.Opcode.Ret && lastKind != UndertaleInstruction.Opcode.Exit)
                                || (block.nextBlockTrue != null && block.nextBlockTrue.nextBlockFalse == null)) ? block.nextBlockTrue : stopAt;
                    }
                }
            }

            if (breakTo != null && lastBlock?.nextBlockFalse == breakTo && lastBlock?.Instructions.Last()?.Kind == UndertaleInstruction.Opcode.B)
                output.Statements.Add(new BreakHLStatement());

            return output;
        }

        private static Statement UnCast(Statement statement)
        {
            if (statement is ExpressionCast cast)
                return UnCast(cast.Argument);

            return statement;
        }

        private static bool TestNumber(Statement statement, int number, DecompileContext context = null)
        {
            statement = UnCast(statement);
            return (statement is ExpressionConstant constant) && constant.EqualsNumber(number);
        }

        public static List<Statement> HLDecompile(DecompileContext context, Dictionary<uint, Block> blocks, Block entryPoint, Block rootExitPoint)
        {
            Dictionary<Block, List<Block>> loops = ComputeNaturalLoops(blocks, entryPoint);
            var reverseDominators = ComputeReverseDominators(blocks, rootExitPoint);
            Block bl = entryPoint;
            return (HLDecompileBlocks(context, ref bl, blocks, loops, reverseDominators, new List<Block>()).CleanBlockStatement(context)).Statements;
        }

        public static Dictionary<uint, Block> PrepareDecompileFlow(UndertaleCode code, List<uint> entryPoints)
        {
            if (code.ParentEntry != null)
                throw new InvalidOperationException("This code block represents a function nested inside " + code.ParentEntry.Name + " - decompile that instead");
            code.UpdateAddresses();

            Dictionary<uint, Block> blocks = DecompileFlowGraph(code, entryPoints);

            return blocks;
        }

        public static Dictionary<uint, Block> PrepareDecompileFlow(UndertaleCode code)
        {
            List<uint> entryPoints = new List<uint>();
            entryPoints.Add(0);
            foreach (UndertaleCode duplicate in code.ChildEntries)
                entryPoints.Add(duplicate.Offset / 4);

            return PrepareDecompileFlow(code, entryPoints);
        }

        private static string MakeLocalVars(DecompileContext context, string decompiledCode)
        {
            // Mark local variables as local.
            UndertaleCode code = context.TargetCode;
            StringBuilder tempBuilder = new StringBuilder();
            UndertaleCodeLocals locals = context.GlobalContext.Data?.CodeLocals.For(code);

            List<string> possibleVars = new List<string>();
            if (locals != null)
            {
                foreach (var local in locals.Locals)
                    possibleVars.Add(local.Name.Content);
            }
            else
            {
                // Time to search through this thing manually.
                for (int i = 0; i < code.Instructions.Count; i++)
                {
                    var inst = code.Instructions[i];
                    if (inst.Kind == UndertaleInstruction.Opcode.PushLoc)
                    {
                        string name = (inst.Value as UndertaleInstruction.Reference<UndertaleVariable>)?.Target?.Name?.Content;
                        if (name != null && !possibleVars.Contains(name))
                            possibleVars.Add(name);
                    }
                    else if (inst.Kind == UndertaleInstruction.Opcode.Pop && inst.TypeInst == UndertaleInstruction.InstanceType.Local)
                    {
                        string name = inst.Destination.Target?.Name?.Content;
                        if (name != null && !possibleVars.Contains(name))
                            possibleVars.Add(name);
                    }
                }
            }

            foreach (var possibleName in possibleVars)
            {
                if (possibleName == "arguments" || possibleName == "$$$$temp$$$$" || context.LocalVarDefines.Contains(possibleName))
                    continue;

                if (tempBuilder.Length > 0)
                    tempBuilder.Append(", ");

                tempBuilder.Append(possibleName);
            }

            // Add tempvars to locals
            string oldStr = tempBuilder.ToString();
            for (int i = 0; i < context.TempVarId; i++)
            {
                string tempVarName = TempVar.MakeTemporaryVarName(i + 1);
                if (decompiledCode.Contains(tempVarName) && !oldStr.Contains(tempVarName))
                {
                    if (tempBuilder.Length > 0)
                        tempBuilder.Append(", ");
                    tempBuilder.Append(tempVarName);
                }
            }

            string result = "";
            if (tempBuilder.Length > 0)
                result = "var " + tempBuilder.ToString() + ";\n";
            return result;
        }

        public static string Decompile(UndertaleCode code, GlobalDecompileContext globalContext, Action<string> msgDelegate = null)
        {
            globalContext.DecompilerWarnings.Clear();
            DecompileContext context = new DecompileContext(globalContext, code);

            if (msgDelegate is not null)
                msgDelegate("Building the cache of all sub-functions...");
            BuildSubFunctionCache(globalContext.Data);
            if (msgDelegate is not null)
                msgDelegate("Decompiling, please wait... This can take a while on complex scripts.");

            try
            {
                if (globalContext.Data != null && globalContext.Data.ToolInfo.ProfileMode)
                {
                    string GMLPath = Path.Combine(globalContext.Data.ToolInfo.AppDataProfiles,
                                                  globalContext.Data.ToolInfo.CurrentMD5, "Temp", code.Name.Content + ".gml");
                    if (File.Exists(GMLPath))
                        return File.ReadAllText(GMLPath);
                }
            }
            catch
            {
                // Just ignore the exception and decompile normally
            }

            Dictionary<uint, Block> blocks = PrepareDecompileFlow(code);
            DecompileFromBlock(context, blocks, blocks[0]);
            DoTypePropagation(context, blocks);
            context.Statements = new Dictionary<uint, List<Statement>>();
            context.Statements.Add(0, HLDecompile(context, blocks, blocks[0], blocks[code.Length / 4]));
            foreach (UndertaleCode duplicate in code.ChildEntries)
                context.Statements.Add(duplicate.Offset / 4, HLDecompile(context, blocks, blocks[duplicate.Offset / 4], blocks[code.Length / 4]));

            // Write code.
            context.IndentationLevel = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var warn in globalContext.DecompilerWarnings)
                sb.Append(warn + "\n");
            foreach (var stmt in context.Statements[0])
            {
                // Ignore initial struct definitions, they clutter
                // decompiled output and generally make code more
                // confusing to read.
                if (stmt is AssignmentStatement assign && assign.IsStructDefinition)
                    continue;

                sb.Append(stmt.ToString(context) + "\n");
            }

            globalContext.DecompilerWarnings.Clear();
            context.Statements = null;

            string decompiledCode = sb.ToString();
            return MakeLocalVars(context, decompiledCode) + decompiledCode;
        }

        public static void BuildSubFunctionCache(UndertaleData data)
        {
            // Find all functions defined in GlobalScripts
            // Use the cache so this only gets calculated once
            if (data == null || !data.IsVersionAtLeast(2, 3) || data.KnownSubFunctions != null)
                return;

            // There's no "ConcurrentHashSet<>"; values aren't used.
            ConcurrentDictionary<string, string> processingCodeList = new();
            byte elapsedSec = 1;
            Task mainTask = Task.Run(() =>
            {
                HashSet<string> codeNames = new(data.Code.Select(c => c.Name?.Content));
                foreach (var func in data.Functions)
                {
                    if (codeNames.Contains(func.Name.Content))
                        func.Autogenerated = true;
                }
                data.KnownSubFunctions = new Dictionary<string, UndertaleFunction>();
                GlobalDecompileContext globalDecompileContext = new GlobalDecompileContext(data, false);

                Parallel.ForEach(data.GlobalInitScripts, globalScript =>
                {
                    UndertaleCode scriptCode = globalScript.Code;
                    processingCodeList[scriptCode.Name.Content] = null;
                    try
                    {
                        DecompileContext childContext = new DecompileContext(globalDecompileContext, scriptCode, false);
                        childContext.DisableAnonymousFunctionNameResolution = true;
                        Dictionary<uint, Block> blocks2 = PrepareDecompileFlow(scriptCode, new List<uint>() { 0 });
                        DecompileFromBlock(childContext, blocks2, blocks2[0]);
                        List<Statement> statements = HLDecompile(childContext, blocks2, blocks2[0], blocks2[scriptCode.Length / 4]);
                        foreach (Statement stmt2 in statements)
                        {
                            if (stmt2 is AssignmentStatement assign &&
                                assign.Value is FunctionDefinition funcDef)
                            {
                                lock (data.KnownSubFunctions)
                                {
                                    data.KnownSubFunctions.Add(assign.Destination.Var.Name.Content, funcDef.Function);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }

                    processingCodeList.Remove(scriptCode.Name.Content, out _);
                });

                elapsedSec = 3 * 60;
            });

            Task timeoutTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);

                    if (++elapsedSec > 3 * 60)
                        return;
                }
            });

            // If the timeout task ended earlier than the main task
            if (Task.WaitAny(mainTask, timeoutTask) == 1)
            {
                throw new TimeoutException("The building cache process hung.\n" +
                                           "The function code entries that didn't manage to decompile:\n" +
                                           String.Join('\n', processingCodeList.Keys) + "\n\n" + 
                                           "You should save the game data (if it's necessary) and re-open the app.\n");
            }
        }

        private static void DoTypePropagation(DecompileContext context, Dictionary<uint, Block> blocks)
        {
            foreach (var b in blocks.Values.Cast<Block>().Reverse())
            {
                if (b.Statements != null) // With returns not allowing all blocks coverage, make sure it's even been processed
                    foreach (var s in b.Statements.Cast<Statement>().Reverse())
                        s.DoTypePropagation(context, AssetIDType.Other);

                b.ConditionStatement?.DoTypePropagation(context, AssetIDType.Other);
            }
        }

        private static Block DetermineSwitchEnd(Block start, Block end, Block meetPoint)
        {
            if (end == null)
                return meetPoint;

            Queue<Block> blocks = new Queue<Block>();

            blocks.Enqueue(start);
            while (blocks.Count > 0)
            {
                Block test = blocks.Dequeue();

                if (test == end)
                    return end;
                if (test == meetPoint)
                    return meetPoint;

                blocks.Enqueue(test.nextBlockTrue);
                if (test.nextBlockTrue != test.nextBlockFalse)
                    blocks.Enqueue(test.nextBlockFalse);

            }

            return meetPoint;
        }

        public static string ExportFlowGraph(Dictionary<uint, Block> blocks)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("digraph G {");
            foreach (var pair in blocks)
            {
                var block = pair.Value;
                sb.Append("    block_" + pair.Key + " [label=\"");
                sb.Append("[" + block.ToString() + ", Exit: " + block.conditionalExit + (block.nextBlockTrue != null ? ", T: " + block.nextBlockTrue.Address : "") + (block.nextBlockFalse != null ? ", F: " + block.nextBlockFalse.Address : "") + "]\n");
                foreach (var instr in block.Instructions)
                    sb.Append(instr.ToString().Replace("\"", "\\\"", StringComparison.InvariantCulture) + "\\n");
                sb.Append('"');
                sb.Append(pair.Key == 0 ? ", color=\"blue\"" : "");
                sb.AppendLine(", shape=\"box\"];");
            }
            sb.AppendLine("");
            foreach (var block in blocks)
            {
                if (block.Value.conditionalExit)
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + " [color=\"green\"];");
                    if (block.Value.nextBlockFalse != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockFalse.Address + " [color=\"red\"];");
                }
                else
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + ";");
                }
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
