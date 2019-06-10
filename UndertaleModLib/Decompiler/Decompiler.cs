using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using static UndertaleModLib.Decompiler.Decompiler;

namespace UndertaleModLib.Decompiler
{
    public class DecompileContext
    {
        public UndertaleData Data;
        public UndertaleCode TargetCode;

        // Settings.
        public bool EnableStringLabels;

        // Decompilation Instance Data.
        public Dictionary<string, TempVarAssigmentStatement> TempVarMap = new Dictionary<string, TempVarAssigmentStatement>();
        public AssignmentStatement CompilerTempVar;

        public bool isGameMaker2 { get => Data != null && Data.IsGameMaker2(); }

        public void Setup(UndertaleCode code)
        {
            TargetCode = code;
            TempVarMap.Clear();
            CompilerTempVar = null;
        }

        public DecompileContext(UndertaleData data, bool enableStringLabels)
        {
            this.Data = data;
            this.EnableStringLabels = enableStringLabels;
        }
    }

    public static class Decompiler
    {

        /**
         * Howdy! Yeah, I don't know how any of this works anymore either, so... have fun
         */

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

            public Block(uint? address)
            {
                Address = address;
            }

            public override string ToString()
            {
                return "Block " + Address;
            }
        }

        public abstract class Statement
        {
            public abstract string ToString(DecompileContext context);
            internal abstract AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType);

            public abstract Statement CleanStatement(DecompileContext context, BlockHLStatement block);
        }

        public abstract class Expression : Statement
        {
            public UndertaleInstruction.DataType Type;
            public bool WasDuplicated = false;

            public static string OperationToPrintableString(UndertaleInstruction.Opcode op)
            {
                switch (op)
                {
                    case UndertaleInstruction.Opcode.Mul:
                        return "*";
                    case UndertaleInstruction.Opcode.Div:
                        return "/";
                    case UndertaleInstruction.Opcode.Rem:
                        return "div";
                    case UndertaleInstruction.Opcode.Mod:
                        return "%";
                    case UndertaleInstruction.Opcode.Add:
                        return "+";
                    case UndertaleInstruction.Opcode.Sub:
                        return "-";
                    case UndertaleInstruction.Opcode.And:
                        return "&";
                    case UndertaleInstruction.Opcode.Or:
                        return "|";
                    case UndertaleInstruction.Opcode.Xor:
                        return "^";
                    case UndertaleInstruction.Opcode.Neg:
                        return "-";
                    case UndertaleInstruction.Opcode.Not:
                        return "~";
                    case UndertaleInstruction.Opcode.Shl:
                        return "<<";
                    case UndertaleInstruction.Opcode.Shr:
                        return ">>";
                    default:
                        return op.ToString().ToUpper();
                }
            }

            public static string OperationToPrintableString(UndertaleInstruction.ComparisonType op)
            {
                switch (op)
                {
                    case UndertaleInstruction.ComparisonType.LT:
                        return "<";
                    case UndertaleInstruction.ComparisonType.LTE:
                        return "<=";
                    case UndertaleInstruction.ComparisonType.EQ:
                        return "==";
                    case UndertaleInstruction.ComparisonType.NEQ:
                        return "!=";
                    case UndertaleInstruction.ComparisonType.GTE:
                        return ">=";
                    case UndertaleInstruction.ComparisonType.GT:
                        return ">";
                    default:
                        return op.ToString().ToUpper();
                }
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

            private static int? ConvertToInt(object val)
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

             private static string ConvertToEnumStr<T>(object val)
             {
                 int? intVal = ConvertToInt(val);
                 if (intVal == null)
                     return val.ToString();
                 return ((T)(object) intVal).ToString();
             }

            public override string ToString(DecompileContext context)
            {

                if (Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>) // Export string.
                {
                    UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> resource = (UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)Value;

                    string resultStr = resource.Resource.ToString(context);
                    if (context.EnableStringLabels)
                        resultStr += resource.GetMarkerSuffix();
                    return resultStr;
                }

                if (AssetType == AssetIDType.GameObject && !(Value is Int64)) // When the value is Int64, an example value is 343434343434. It is unknown what it represents, but it's not an InstanceType.
                {
                    if (Convert.ToInt32(Value) < 0)
                        return ((UndertaleInstruction.InstanceType)Value).ToString().ToLower();
                }

                if (context.Data != null && AssetType != AssetIDType.Other && AssetType != AssetIDType.Color && AssetType != AssetIDType.KeyboardKey && AssetType != AssetIDType.e__VW && AssetType != AssetIDType.e__BG && AssetType != AssetIDType.Enum_HAlign && AssetType != AssetIDType.Enum_VAlign)
                {
                    IList assetList = null;
                    switch (AssetType)
                    {
                        case AssetIDType.Sprite:
                            assetList = (IList)context.Data.Sprites;
                            break;
                        case AssetIDType.Background:
                            assetList = (IList)context.Data.Backgrounds;
                            break;
                        case AssetIDType.Sound:
                            assetList = (IList)context.Data.Sounds;
                            break;
                        case AssetIDType.Font:
                            assetList = (IList)context.Data.Fonts;
                            break;
                        case AssetIDType.Path:
                            assetList = (IList)context.Data.Paths;
                            break;
                        case AssetIDType.Timeline:
                            assetList = (IList)context.Data.Timelines;
                            break;
                        case AssetIDType.Room:
                            assetList = (IList)context.Data.Rooms;
                            break;
                        case AssetIDType.GameObject:
                            assetList = (IList)context.Data.GameObjects;
                            break;
                        case AssetIDType.Shader:
                            assetList = (IList)context.Data.Shaders;
                            break;
                        case AssetIDType.Script:
                            assetList = (IList)context.Data.Scripts;
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

                if (AssetType == AssetIDType.e__VW)
                    return "e__VW." + ConvertToEnumStr<e__VW>(Value);
                if (AssetType == AssetIDType.e__BG)
                    return "e__BG." + ConvertToEnumStr<e__BG>(Value);

                if (AssetType == AssetIDType.Enum_HAlign)
                    return ConvertToEnumStr<HAlign>(Value);
                if (AssetType == AssetIDType.Enum_VAlign)
                    return ConvertToEnumStr<VAlign>(Value);
                if (AssetType == AssetIDType.Enum_OSType)
                    return ConvertToEnumStr<OSType>(Value);
                if (AssetType == AssetIDType.Enum_GamepadButton)
                    return ConvertToEnumStr<GamepadButton>(Value);
                if (AssetType == AssetIDType.Enum_PathEndAction)
                    return ConvertToEnumStr<PathEndAction>(Value);
                if (AssetType == AssetIDType.Enum_BufferKind)
                    return ConvertToEnumStr<BufferKind>(Value);
                if (AssetType == AssetIDType.Enum_BufferType)
                    return ConvertToEnumStr<BufferType>(Value);
                if (AssetType == AssetIDType.Enum_BufferSeek)
                    return ConvertToEnumStr<BufferSeek>(Value);
                if (AssetType == AssetIDType.Boolean)
                    return ConvertToEnumStr<Boolean>(Value);

                if (AssetType == AssetIDType.Color && Value is IFormattable && !(Value is float) && !(Value is double) && !(Value is decimal))
                    return (context.isGameMaker2 ? "0x" : "$") + ((IFormattable)Value).ToString("X8", CultureInfo.InvariantCulture);

                if (AssetType == AssetIDType.KeyboardKey)
                {
                    int? tryVal = ConvertToInt(Value);
                    if (tryVal != null)
                    {
                        int val = tryVal ?? -1;
                        if (val >= 0 && Enum.IsDefined(typeof(EventSubtypeKey), (uint)val))
                        {
                            bool isAlphaNumeric = val >= (int)EventSubtypeKey.Digit0 && val <= (int)EventSubtypeKey.Z;
                            return isAlphaNumeric ? "ord(\"" + (char)val + "\")" : ((EventSubtypeKey)val).ToString(); // Either return the key enum, or the right alpha-numeric key-press.
                        }

                        if (!Char.IsControl((char)val) && !Char.IsLower((char)val)) // The special keys overlay with the uppercase letters (ugh)
                            return ((char)val) == '\'' ? (context.isGameMaker2 ? "\"\\\"\"" : "'\"'")
                                : (((char)val) == '\\' ? (context.isGameMaker2 ? "\"\\\\\"" : "\"\\\"")
                                : "\"" + (char)val + "\"");
                    }
                }

                if (Value is float) // Prevents scientific notation by using high bit number.
                    return ((decimal)((float)Value)).ToString(CultureInfo.InvariantCulture);

                if (Value is double) // Prevents scientific notation by using high bit number.
                    return ((decimal)((double)Value)).ToString(CultureInfo.InvariantCulture);

                if (Value is Statement)
                    return ((Statement)Value).ToString(context);

                return ((Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? Value.ToString());
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                if (AssetType == AssetIDType.Other)
                    AssetType = suggestedType;
                return AssetType;
            }
        }

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
                //return String.Format("{0}({1})", Type != Argument.Type ? "(" + Type.ToString().ToLower() + ")" : "", Argument.ToString());
                return Argument.ToString(context);
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return Argument.DoTypePropagation(context, suggestedType);
            }
        }

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
                return String.Format("{0}({1} {2})", false && Type != Argument.Type ? "(" + Type.ToString().ToLower() + ")" : "", op, Argument.ToString(context));
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
                // TODO: better condition for casts
                return String.Format("{0}({1} {2} {3})", false && (Type != Argument1.Type || Type != Argument2.Type || Argument1.Type != Argument2.Type) ? "(" + Type.ToString().ToLower() + ")" : "", Argument1.ToString(context), Symbol, Argument2.ToString(context));
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                // The most likely, but probably rarely happens
                AssetIDType t = Argument1.DoTypePropagation(context, suggestedType);
                Argument2.DoTypePropagation(context, AssetIDType.Other);
                return t;
            }
        }

        public class ExpressionTwo : Expression
        {
            public UndertaleInstruction.Opcode Opcode;
            public Expression Argument1;
            public Expression Argument2;

            public ExpressionTwo(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, Expression argument1, Expression argument2)
            {
                this.Opcode = opcode;
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
                // TODO: better condition for casts
                return String.Format("{0}({1} {2} {3})", false && (Type != Argument1.Type || Type != Argument2.Type || Argument1.Type != Argument2.Type) ? "(" + Type.ToString().ToLower() + ")" : "", Argument1.ToString(context), OperationToPrintableString(Opcode), Argument2.ToString(context));
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                // The most likely, but probably rarely happens
                AssetIDType t = Argument1.DoTypePropagation(context, suggestedType);
                Argument2.DoTypePropagation(context, AssetIDType.Other);
                return t;
            }
        }

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
                return String.Format("({0} {1} {2})", Argument1.ToString(context), OperationToPrintableString(Opcode), Argument2.ToString(context));
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
                return AssetIDType.Other;
            }
        }

        public class OperationStatement : Statement
        {
            public UndertaleInstruction.Opcode Opcode;

            public OperationStatement(UndertaleInstruction.Opcode opcode)
            {
                this.Opcode = opcode;
            }

            public override string ToString(DecompileContext context)
            {
                return Opcode.ToString().ToUpper();
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


        public class TempVar
        {
            public string Name;
            public UndertaleInstruction.DataType Type;

            [ThreadStatic]
            public static int TempVarId;
            internal AssetIDType AssetType;

            public TempVar()
            {
                Name = MakeTemporaryVarName(++TempVarId); ;
            }

            public static string MakeTemporaryVarName(int id)
            {
                return "_temp_local_var_" + id;
            }
        }

        public class TempVarReference
        {
            public TempVar Var;

            public TempVarReference(TempVar var)
            {
                Var = var;
            }
        }

        public class TempVarAssigmentStatement : Statement
        {
            public TempVarReference Var;
            public Expression Value;

            public TempVarAssigmentStatement(TempVarReference var, Expression value)
            {
                Var = var;
                Value = value;
            }

            public override string ToString(DecompileContext context)
            {
                return String.Format("{0} = {1}", Var.Var.Name, Value.ToString(context));
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
                return String.Format("{0}{1}", /*Type != Var.Var.Type ? "(" + Type.ToString().ToLower() + ")" : ""*/ "", Var.Var.Name);
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                TempVarAssigmentStatement tempVarStatement = context.TempVarMap[Var.Var.Name];
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
                    return "return " + Value.ToString(context) + ";";
                else
                    return (context.isGameMaker2 ? "return;" : "exit");
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

        public class AssignmentStatement : Statement
        {
            public ExpressionVar Destination;
            public Expression Value;

            public AssignmentStatement(ExpressionVar destination, Expression value)
            {
                Destination = destination;
                Value = value;
            }

            public override string ToString(DecompileContext context)
            {
                if (Value is ExpressionTwo && ((Value as ExpressionTwo).Argument1 is ExpressionVar) && ((Value as ExpressionTwo).Argument1 as ExpressionVar).Var == Destination.Var
                    && ((Value as ExpressionTwo).Argument2 is ExpressionConstant) && ((Value as ExpressionTwo).Argument2 as ExpressionConstant).IsPushE
                    && Convert.ToInt32(((Value as ExpressionTwo).Argument2 as ExpressionConstant).Value) == 1)
                {
                    return String.Format("{0}" + (((Value as ExpressionTwo).Opcode == UndertaleInstruction.Opcode.Add) ? "++" : "--"), Destination.ToString(context));
                }
                else
                    return String.Format("{0} = {1}", Destination.ToString(context), Value.ToString(context));
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
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

        public class AssignmentEqualsStatement : Statement
        {
            public ExpressionVar Destination;
            public UndertaleInstruction.Opcode Operation;
            public Expression Value;

            public AssignmentEqualsStatement(ExpressionVar destination, UndertaleInstruction.Opcode operation, Expression value)
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

        public class FunctionCall : Expression
        {
            private UndertaleFunction Function;
            private UndertaleInstruction.DataType ReturnType;
            private List<Expression> Arguments;

            public FunctionCall(UndertaleFunction function, UndertaleInstruction.DataType returnType, List<Expression> args)
            {
                this.Function = function;
                this.ReturnType = returnType;
                this.Arguments = args;
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

                if (Function.Name.Content == "@@NewGMLArray@@") // Special-case.
                    return "[" + argumentString.ToString() + "]";

                return String.Format("{0}({1})", Function.Name.Content, argumentString.ToString());
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                for (var i = 0; i < Arguments.Count; i++)
                    Arguments[i] = Arguments[i]?.CleanExpression(context, block);
                return this;
            }

            [ThreadStatic]
            public static Dictionary<string, AssetIDType[]> scriptArgs; // TODO: damnit stop using globals you stupid... this needs a big refactor anyway

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                var script_code = context.Data?.Scripts.ByName(Function.Name.Content)?.Code;
                if (script_code != null && !scriptArgs.ContainsKey(Function.Name.Content))
                {
                    scriptArgs.Add(Function.Name.Content, null); // stop the recursion from looping
                    var xxx = ExpressionVar.assetTypes; // TODO: this is going bad
                    ExpressionVar.assetTypes = new Dictionary<UndertaleVariable, AssetIDType>(); // TODO: don't look at this
                    Dictionary<uint, Block> blocks = Decompiler.PrepareDecompileFlow(script_code);
                    Decompiler.DecompileFromBlock(context, blocks[0]);
                    Decompiler.DoTypePropagation(context, blocks); // TODO: This should probably put suggestedType through the "return" statement at the other end
                    scriptArgs[Function.Name.Content] = new AssetIDType[15];
                    for (int i = 0; i < 15; i++)
                    {
                        var v = ExpressionVar.assetTypes.Where((x) => x.Key.Name.Content == "argument" + i);
                        scriptArgs[Function.Name.Content][i] = v.Count() > 0 ? v.First().Value : AssetIDType.Other;
                    }
                    ExpressionVar.assetTypes = xxx; // restore
                }

                AssetIDType[] args = new AssetIDType[Arguments.Count];
                AssetTypeResolver.AnnotateTypesForFunctionCall(Function.Name.Content, args, scriptArgs);
                for (var i = 0; i < Arguments.Count; i++)
                {
                    Arguments[i].DoTypePropagation(context, args[i]);
                }
                return suggestedType; // TODO: maybe we should handle returned values too?
            }
        }

        public class ExpressionVar : Expression
        {
            public UndertaleVariable Var;
            public Expression InstType; // UndertaleInstruction.InstanceType
            public UndertaleInstruction.VariableType VarType;
            public Expression ArrayIndex1;
            public Expression ArrayIndex2;

            public ExpressionVar(UndertaleVariable var, Expression instType, UndertaleInstruction.VariableType varType)
            {
                Var = var;
                InstType = instType;
                VarType = varType;
            }

            internal override bool IsDuplicationSafe()
            {
                return (InstType?.IsDuplicationSafe() ?? true) && (ArrayIndex1?.IsDuplicationSafe() ?? true) && (ArrayIndex2?.IsDuplicationSafe() ?? true);
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                if (Var.Name?.Content == "$$$$temp$$$$" && context.CompilerTempVar != null)
                {
                    block.Statements.Remove(context.CompilerTempVar);
                    return context.CompilerTempVar.Value;
                }

                InstType = InstType?.CleanExpression(context, block);
                ArrayIndex1 = ArrayIndex1?.CleanExpression(context, block);
                ArrayIndex2 = ArrayIndex2?.CleanExpression(context, block);
                return this;
            }

            public static Tuple<Expression, Expression> Decompile2DArrayIndex(Expression index)
            {
                Expression ind1 = index;
                Expression ind2 = null;
                if (ind1 is ExpressionTwo && (ind1 as ExpressionTwo).Opcode == UndertaleInstruction.Opcode.Add) // Decompile 2D array access
                {
                    var arg1 = (ind1 as ExpressionTwo).Argument1;
                    var arg2 = (ind1 as ExpressionTwo).Argument2;
                    if (arg1 is ExpressionTwo && (arg1 as ExpressionTwo).Opcode == UndertaleInstruction.Opcode.Mul)
                    {
                        var arg11 = (arg1 as ExpressionTwo).Argument1;
                        var arg12 = (arg1 as ExpressionTwo).Argument2;
                        if (arg12 is ExpressionConstant && (arg12 as ExpressionConstant).Value.GetType() == typeof(int) && (int)(arg12 as ExpressionConstant).Value == 32000)
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
                //Debug.Assert((ArrayIndex != null) == NeedsArrayParameters);
                //Debug.Assert((InstanceIndex != null) == NeedsInstanceParameters);
                string name = Var.Name.Content;
                if (ArrayIndex1 != null && ArrayIndex2 != null)
                    name = name + "[" + ArrayIndex1.ToString(context) + ", " + ArrayIndex2.ToString(context) + "]";
                else if (ArrayIndex1 != null)
                    name = name + "[" + ArrayIndex1.ToString(context) + "]";

                //NOTE: The "var" prefix is handled in Decompiler.Decompile. 

                string prefix = InstType.ToString(context) + ".";
                if (InstType is ExpressionConstant) // Only use "global." and "other.", not "self." or "local.". GMS doesn't recognize those.
                {
                    ExpressionConstant constant = (ExpressionConstant)InstType;
                    if (!(constant.Value is Int64))
                    {
                        int val = Convert.ToInt32(constant.Value);
                        if (constant.AssetType == AssetIDType.GameObject && val < 0)
                        {
                            UndertaleInstruction.InstanceType instanceType = (UndertaleInstruction.InstanceType)val;
                            prefix = (instanceType == UndertaleInstruction.InstanceType.Global || instanceType == UndertaleInstruction.InstanceType.Other) ? prefix.ToLower() : "";
                        }
                    }
                }

                return prefix + name;
            }

            [ThreadStatic]
            public static Dictionary<UndertaleVariable, AssetIDType> assetTypes; // TODO: huge and ugly memory leak that I may fix one day... nah don't count on it
            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                InstType?.DoTypePropagation(context, AssetIDType.GameObject);
                ArrayIndex1?.DoTypePropagation(context, AssetIDType.Other);
                ArrayIndex2?.DoTypePropagation(context, AssetIDType.Other);

                AssetIDType current = assetTypes.ContainsKey(Var) ? assetTypes[Var] : AssetIDType.Other;
                if (current == AssetIDType.Other && suggestedType != AssetIDType.Other)
                    current = suggestedType;
                AssetIDType builtinSuggest = AssetTypeResolver.AnnotateTypeForVariable(Var.Name.Content);
                if (builtinSuggest != AssetIDType.Other)
                    current = builtinSuggest;

                if ((VarType != UndertaleInstruction.VariableType.Array || (ArrayIndex1 != null && !(ArrayIndex1 is ExpressionConstant))))
                    assetTypes[Var] = current; // This is a messy fix to arrays messing up exported variable types.
                return current;
            }

            public bool NeedsArrayParameters => VarType == UndertaleInstruction.VariableType.Array;
            public bool NeedsInstanceParameters => /*InstType == UndertaleInstruction.InstanceType.StackTopOrGlobal &&*/ VarType == UndertaleInstruction.VariableType.StackTop;
        }

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

        internal static void DecompileFromBlock(DecompileContext context, Block block, List<TempVarReference> tempvars, Stack<Tuple<Block, List<TempVarReference>>> workQueue)
        {
            if (block.TempVarsOnEntry != null && (block.nextBlockTrue != null || block.nextBlockFalse != null))
            {
                // Reroute tempvars to alias them to our ones
                if (block.TempVarsOnEntry.Count != tempvars.Count)
                {
                    throw new Exception("Reentered block with different amount of vars on stack");
                }
                else
                {
                    for (int i = 0; i < tempvars.Count; i++)
                    {
                        tempvars[i].Var = block.TempVarsOnEntry[i].Var;
                    }
                }
            }

            if (block.Statements != null)
                return; // don't decompile again :P

            block.TempVarsOnEntry = tempvars;

            Stack<Expression> stack = new Stack<Expression>();
            foreach (TempVarReference var in tempvars)
                stack.Push(new ExpressionTempVar(var, var.Var.Type));

            List<Statement> statements = new List<Statement>();
            bool end = false;
            bool returned = false;
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                if (end)
                    throw new Exception("Excepted end of block, but still has instructions");
                var instr = block.Instructions[i];
                switch (instr.Kind)
                {
                    case UndertaleInstruction.Opcode.Neg:
                    case UndertaleInstruction.Opcode.Not:
                        stack.Push(new ExpressionOne(instr.Kind, instr.Type1, stack.Pop()));
                        break;

                    case UndertaleInstruction.Opcode.Dup:
                        List<Expression> topExpressions1 = new List<Expression>();
                        List<Expression> topExpressions2 = new List<Expression>();
                        int count = ((instr.DupExtra + 1) * (instr.Type1 == UndertaleInstruction.DataType.Int64 ? 2 : 1));
                        for (int j = 0; j < count; j++)
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
                                TempVar var = new TempVar();
                                var.Type = item.Type;
                                TempVarReference varref = new TempVarReference(var);
                                statements.Add(new TempVarAssigmentStatement(varref, item));

                                topExpressions1.Add(new ExpressionTempVar(varref, varref.Var.Type) { WasDuplicated = true } );
                                topExpressions2.Add(new ExpressionTempVar(varref, instr.Type1) { WasDuplicated = true } );
                            }
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
                        ReturnStatement stmt = new ReturnStatement(instr.Kind == UndertaleInstruction.Opcode.Ret ? stack.Pop() : null);
                        /*
                        This shouldn't be necessary: all unused things on the stack get converted to tempvars at the end anyway, and this fixes decompilation of repeat()
                        See #85

                        foreach (var expr in stack.Reverse())
                            if (!(expr is ExpressionTempVar))
                                statements.Add(expr);
                        stack.Clear();*/
                        statements.Add(stmt);
                        end = true;
                        returned = true;
                        break;

                    case UndertaleInstruction.Opcode.Popz:
                        if (stack.Count > 0)
                        {
                            Expression popped = stack.Pop();
                            if (!(popped is ExpressionTempVar))
                                statements.Add(popped);
                        }
                        else
                        {
                            statements.Add(new CommentStatement("WARNING: Popz'd an empty stack."));
                        }
                        break;

                    case UndertaleInstruction.Opcode.Conv:
                        /*if (instr.Type1 != stack.Peek().Type)
                            stack.Push(new ExpressionCast(instr.Type1, stack.Pop()));*/

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
                        stack.Push(new ExpressionTwo(instr.Kind, instr.Type1, a1, a2)); // TODO: type
                        break;

                    case UndertaleInstruction.Opcode.Cmp:
                        Expression aa2 = stack.Pop();
                        Expression aa1 = stack.Pop();
                        stack.Push(new ExpressionCompare(instr.ComparisonKind, aa1, aa2)); // TODO: type
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
                        statements.Add(new PushEnvStatement(stack.Pop()));
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.PopEnv:
                        if (instr.JumpOffsetPopenvExitMagic)
                        {
                            // This is just an instruction to make sure the pushenv/popenv stack is cleared on early function return
                            // Works kinda like 'break', but doesn't have a high-level representation as it's immediately followed by a 'return'
                        }
                        else
                        {
                            statements.Add(new PopEnvStatement());
                        }
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.Pop:
                        if (instr.Destination == null)
                        {
                            // pop.e.v 5/6
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
                            throw new Exception("Oh no, what do I do with this POP? OH NOOOOOOOoooooooooo");
                        if (instr.Type1 == UndertaleInstruction.DataType.Int32)
                            val = stack.Pop();
                        if (target.NeedsInstanceParameters)
                            target.InstType = stack.Pop();
                        if (target.NeedsArrayParameters)
                        {
                            Tuple<Expression, Expression> ind = ExpressionVar.Decompile2DArrayIndex(stack.Pop());
                            target.ArrayIndex1 = ind.Item1;
                            target.ArrayIndex2 = ind.Item2;
                            target.InstType = stack.Pop();
                        }
                        if (instr.Type1 == UndertaleInstruction.DataType.Variable)
                            val = stack.Pop();
                        Debug.Assert(val != null);
                        if (val != null)
                        {
                            if ((target.NeedsInstanceParameters || target.NeedsArrayParameters) && target.InstType.WasDuplicated)
                            {
                                // Should be safe to assume that this is a +=, -=, etc.
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
                                                v.ArrayIndex1 == target.ArrayIndex1 && v.ArrayIndex2 == target.ArrayIndex2) // even if null
                                            {
                                                statements.Add(new AssignmentEqualsStatement(target, two.Opcode, two.Argument2));
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        statements.Add(new AssignmentStatement(target, val));
                        break;

                    case UndertaleInstruction.Opcode.Push:
                    case UndertaleInstruction.Opcode.PushLoc:
                    case UndertaleInstruction.Opcode.PushGlb:
                    case UndertaleInstruction.Opcode.PushVar:
                    case UndertaleInstruction.Opcode.PushI:
                        if (instr.Value is UndertaleInstruction.Reference<UndertaleVariable>)
                        {
                            ExpressionVar pushTarget = new ExpressionVar((instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Target, new ExpressionConstant(UndertaleInstruction.DataType.Int16, instr.TypeInst), (instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Type);
                            if (pushTarget.NeedsInstanceParameters)
                                pushTarget.InstType = stack.Pop();
                            if (pushTarget.NeedsArrayParameters)
                            {
                                Tuple<Expression, Expression> ind = ExpressionVar.Decompile2DArrayIndex(stack.Pop());
                                pushTarget.ArrayIndex1 = ind.Item1;
                                pushTarget.ArrayIndex2 = ind.Item2;
                                pushTarget.InstType = stack.Pop();
                            }
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
                                } else if (i + 2 < block.Instructions.Count && (block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Add || block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Sub) &&
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
                            } else
                            {
                                stack.Push(pushTarget);
                            }
                        }
                        break;

                    case UndertaleInstruction.Opcode.Call:
                        List<Expression> args = new List<Expression>();
                        for (int j = 0; j < instr.ArgumentsCount; j++)
                            args.Add(stack.Pop());
                        stack.Push(new FunctionCall(instr.Function.Target, instr.Type1, args));
                        break;

                    case UndertaleInstruction.Opcode.Break:
                        //statements.Add(new CommentStatement("// TODO: BREAK " + (short)instr.Value));
                        // This is used for checking bounds in 2D arrays
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
                    if (!(val is ExpressionTempVar) || (val as ExpressionTempVar).Var != tempvars[i])
                        statements.Add(new TempVarAssigmentStatement(tempvars[i], val));
                    leftovers.Add(tempvars[i]);
                }
                else
                {
                    Expression val = stack.Pop();
                    TempVar var = new TempVar();
                    var.Type = val.Type;
                    TempVarReference varref = new TempVarReference(var);
                    statements.Add(new TempVarAssigmentStatement(varref, val));
                    leftovers.Add(varref);
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

        public static void DecompileFromBlock(DecompileContext context, Block block)
        {
            Stack<Tuple<Block, List<TempVarReference>>> workQueue = new Stack<Tuple<Block, List<TempVarReference>>>();
            workQueue.Push(new Tuple<Block, List<TempVarReference>>(block, new List<TempVarReference>()));
            while (workQueue.Count > 0)
            {
                var item = workQueue.Pop();
                DecompileFromBlock(context, item.Item1, item.Item2, workQueue);
            }
        }

        public static Dictionary<uint, Block> DecompileFlowGraph(UndertaleCode code)
        {
            Dictionary<uint, Block> blockByAddress = new Dictionary<uint, Block>();
            blockByAddress[0] = new Block(0);
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
                if (canSkipBrackets && CanSkipBrackets(this))
                    return "    " + Statements[0].ToString(context).Replace("\n", "\n    ");
                else
                {
                    StringBuilder sb = new StringBuilder();
                    if (!forceSkipBrackets)
                        sb.Append("{\n");
                    foreach (var stmt in Statements)
                    {
                        string resultStr = stmt.ToString(context);
                        if (!forceSkipBrackets)
                        {
                            sb.Append("    ");
                            resultStr = resultStr.Replace("\n", "\n    ");
                        }
                        sb.Append(resultStr).Append("\n");
                    }
                    if (!forceSkipBrackets)
                        sb.Append("}");
                    return sb.ToString().Trim('\n');
                }
            }

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                for (var i = 0; i < Statements.Count; i++)
                {
                    var count = Statements.Count;
                    var Result = Statements[i]?.CleanStatement(context, this); //Yes, this uses "this" and not "block".
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
            public List<Pair<Expression, BlockHLStatement>> elseConditions = new List<Pair<Expression, BlockHLStatement>>();
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
                if ((condition as ExpressionCast)?.Argument is ExpressionTwo)
                {
                    ExpressionTwo conditionExpression = ((ExpressionCast)condition).Argument as ExpressionTwo;
                    Statement lastStatement = block.Statements[block.Statements.IndexOf(this) - 1];

                    if (conditionExpression.Argument1 is ExpressionTempVar && lastStatement is TempVarAssigmentStatement && conditionExpression.Argument2 is ExpressionConstant
                        && ((ExpressionTempVar)conditionExpression.Argument1).Var.Var == ((TempVarAssigmentStatement)lastStatement).Var.Var)
                        condition = conditionExpression.Argument1;
                }

                // Fixes breaks in both if outcomes. [Should go first, before other statements that use trueBlock and falseBlock]
                if (trueBlock.Statements.Count > 0 && falseBlock.Statements.Count > 0 && trueBlock.Statements.Last() is BreakHLStatement && falseBlock.Statements.Last() is BreakHLStatement)
                {
                    bool valid = true;
                    foreach (Pair<Expression, BlockHLStatement> tuple in elseConditions)
                        if (tuple.Item2.Statements.Count == 0 || !(tuple.Item2.Statements.Last() is BreakHLStatement))
                            valid = false;

                    if (valid)
                    {
                        trueBlock.Statements.Remove(trueBlock.Statements.Last());
                        falseBlock.Statements.Remove(falseBlock.Statements.Last());
                        foreach (Pair<Expression, BlockHLStatement> tuple in elseConditions)
                            tuple.Item2.Statements.Remove(tuple.Item2.Statements.Last());
                    }
                }

                // Use if -> else if, instead of nesting ifs.
                while (falseBlock.Statements.Count == 1 && falseBlock.Statements[0] is IfHLStatement) // The condition of one if statement.
                {
                    IfHLStatement nestedIf = (IfHLStatement)falseBlock.Statements[0];
                    elseConditions.Add(new Pair<Expression, BlockHLStatement>(nestedIf.condition, nestedIf.trueBlock));
                    elseConditions.AddRange(nestedIf.elseConditions);
                    falseBlock = nestedIf.falseBlock;
                }

                // Collapse conditions into && + || + ternary.
                if (HasElse && !HasElseIf && trueBlock.Statements.Count == 1 && falseBlock.Statements.Count == 1)
                {
                    TempVarAssigmentStatement trueAssign = trueBlock.Statements[0] as TempVarAssigmentStatement;
                    TempVarAssigmentStatement falseAssign = falseBlock.Statements[0] as TempVarAssigmentStatement;

                    if (trueAssign != null && falseAssign != null && trueAssign.Var.Var == falseAssign.Var.Var)
                    {
                        TempVarAssigmentStatement newAssign = null;
                        if (TestNumber(trueAssign.Value, 1) && (falseAssign.Var.Var.Type == UndertaleInstruction.DataType.Boolean || falseAssign.Value.Type == UndertaleInstruction.DataType.Boolean))
                            newAssign = new TempVarAssigmentStatement(trueAssign.Var, new ExpressionTwoSymbol("||", UndertaleInstruction.DataType.Boolean, condition, falseAssign.Value));
                        else if (TestNumber(falseAssign.Value, 0) && (trueAssign.Var.Var.Type == UndertaleInstruction.DataType.Boolean || trueAssign.Value.Type == UndertaleInstruction.DataType.Boolean))
                            newAssign = new TempVarAssigmentStatement(trueAssign.Var, new ExpressionTwoSymbol("&&", UndertaleInstruction.DataType.Boolean, condition, trueAssign.Value));
                        else
                            newAssign = new TempVarAssigmentStatement(trueAssign.Var, new ExpressionTernary(trueAssign.Value.Type, condition, trueAssign.Value, falseAssign.Value));

                        context.TempVarMap[newAssign.Var.Var.Name] = newAssign;
                        return newAssign;
                    }
                }

                // Create repeat loops.
                if (HasElse && !HasElseIf && trueBlock.Statements.Count == 0 && falseBlock.Statements.Count == 1 && falseBlock.Statements[0] is LoopHLStatement
                        && condition is ExpressionCompare && myIndex > 0 && block.Statements[myIndex - 1] is TempVarAssigmentStatement)
                {
                    ExpressionCompare compareCondition = condition as ExpressionCompare;
                    LoopHLStatement loop = falseBlock.Statements[0] as LoopHLStatement;
                    TempVarAssigmentStatement priorAssignment = block.Statements[myIndex - 1] as TempVarAssigmentStatement;
                    Expression startValue = priorAssignment.Value;

                    List<Statement> loopCode = loop.Block.Statements;
                    if (priorAssignment != null && loop.IsWhileLoop && loop.Condition == null && loopCode.Count > 2 && compareCondition.Opcode == UndertaleInstruction.ComparisonType.LTE && TestNumber(compareCondition.Argument2, 0) && compareCondition.Argument1.ToString(context) == startValue.ToString(context))
                    {
                        TempVarAssigmentStatement repeatAssignment = loopCode[loopCode.Count - 3] as TempVarAssigmentStatement;
                        IfHLStatement loopCheckStatement = loopCode[loopCode.Count - 2] as IfHLStatement;
                        Statement testBreakStatement = loopCode[loopCode.Count - 1];

                        if (repeatAssignment != null && loopCheckStatement != null && testBreakStatement is BreakHLStatement)
                        { // tempVar = (tempVar -1); -> if (tempVar) continue -> break

                            // if (tempVar) {continue} else {empty}
                            bool ifPass = loopCheckStatement.trueBlock.Statements.Count == 1 && !loopCheckStatement.HasElse && !loopCheckStatement.HasElseIf
                                && loopCheckStatement.trueBlock.Statements[0] is ContinueHLStatement
                                && loopCheckStatement.condition.ToString(context) == repeatAssignment.Value.ToString(context);

                            if (ifPass)
                            {
                                loopCode.Remove(repeatAssignment);
                                loopCode.Remove(loopCheckStatement);
                                loopCode.Remove(testBreakStatement);
                                block.Statements.Remove(priorAssignment);

                                loop.RepeatStartValue = startValue;
                                return loop;
                            }
                        }
                    }
                }

                foreach (Pair<Expression, BlockHLStatement> pair in elseConditions)
                {
                    pair.Item1 = pair.Item1?.CleanExpression(context, block);
                    pair.Item2 = pair.Item2?.CleanBlockStatement(context);
                }


                return this;
            }

            public override string ToString(DecompileContext context)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("if " + condition.ToString(context) + "\n");
                sb.Append(trueBlock.ToString(context));

                foreach (Pair<Expression, BlockHLStatement> tuple in elseConditions)
                {
                    sb.Append("\nelse if " + tuple.Item1.ToString(context) + "\n");
                    sb.Append(tuple.Item2.ToString(context));
                }

                if (HasElse)
                {
                    sb.Append("\nelse\n");
                    sb.Append(falseBlock.ToString(context));
                }
                return sb.ToString();
            }
        };

        public class LoopHLStatement : HLStatement
        {
            // Loop Block.
            public BlockHLStatement Block;

            // While / For Condition.
            public Statement Condition;

            // While Loop.
            public bool IsWhileLoop { get => !IsForLoop && !IsRepeatLoop && !IsDoUntilLoop; }
            public bool IsDoUntilLoop;

            // For Loop.
            public bool IsForLoop { get => InitializeStatement != null && StepStatement != null && Condition != null; }
            public AssignmentStatement InitializeStatement;
            public AssignmentStatement StepStatement;

            // Repeat Loop.
            public bool IsRepeatLoop { get => RepeatStartValue != null; }
            public Statement RepeatStartValue;

            public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
            {
                if (IsWhileLoop)
                {
                    // While loops have conditions.
                    if (Block.Statements.Count == 2)
                    {
                        Statement firstStatement = Block.Statements[0];
                        Statement secondStatement = Block.Statements[1];

                        if (firstStatement is IfHLStatement && secondStatement is BreakHLStatement)
                        {
                            IfHLStatement ifStatement = (IfHLStatement)firstStatement;
                            if (ifStatement.falseBlock is BlockHLStatement && ((BlockHLStatement)ifStatement.falseBlock).Statements.Count == 0)
                            {
                                Condition = ifStatement.condition;
                                Block.Statements.Remove(firstStatement); // Remove if statement.
                                Block.Statements.Remove(secondStatement); // Remove break.
                                Block.Statements.InsertRange(0, ifStatement.trueBlock.Statements); // Add if contents.
                            }
                        }
                    }

                    // Remove redundant continues at the end of the loop.
                    if (Block.Statements.Count > 0)
                    {
                        Statement lastStatement = Block.Statements.Last();
                        if (lastStatement is ContinueHLStatement)
                            Block.Statements.Remove(lastStatement);
                    }

                    // Convert into a for loop.
                    int myIndex = block.Statements.IndexOf(this);
                    if (myIndex > 0 && block.Statements[myIndex - 1] is AssignmentStatement && Block.Statements.Count > 0 && Block.Statements.Last() is AssignmentStatement && Condition is ExpressionCompare)
                    {
                        ExpressionCompare compare = (ExpressionCompare)Condition;
                        AssignmentStatement assignment = (AssignmentStatement)block.Statements[myIndex - 1];
                        AssignmentStatement increment = (AssignmentStatement)Block.Statements.Last();
                        UndertaleVariable variable = assignment.Destination.Var;

                        if (((compare.Argument1 is ExpressionVar && (((ExpressionVar)compare.Argument1).Var == variable)) || (compare.Argument2 is ExpressionVar && (((ExpressionVar)compare.Argument2).Var == variable))) && increment.Destination.Var == variable)
                        {
                            block.Statements.Remove(assignment);
                            InitializeStatement = assignment;
                            Block.Statements.Remove(increment);
                            StepStatement = increment;
                        }
                    }
                }

                Block = Block?.CleanBlockStatement(context);
                Condition = Condition?.CleanStatement(context, block);
                InitializeStatement = InitializeStatement?.CleanStatement(context, block) as AssignmentStatement;
                StepStatement = StepStatement?.CleanStatement(context, block) as AssignmentStatement;
                RepeatStartValue = RepeatStartValue?.CleanStatement(context, block);

                return this;
            }

            public override string ToString(DecompileContext context)
            {
                if (IsRepeatLoop)
                {
                    bool isConstant = RepeatStartValue is ExpressionConstant;
                    return "repeat " + (isConstant ? "(" : "") + RepeatStartValue.ToString(context) + (isConstant ? ")" : "") + "\n" + Block.ToString(context);
                }

                if (IsForLoop)
                {
                    string conditionStr = Condition.ToString(context); // Cut off parenthesis for the condition.
                    if (conditionStr.StartsWith("(") && conditionStr.EndsWith(")"))
                        conditionStr = conditionStr.Substring(1, conditionStr.Length - 2);

                    return "for (" + InitializeStatement.ToString(context) + "; " + conditionStr + "; " + StepStatement.ToString(context) + ")\n" + Block.ToString(context);
                }

                if (IsDoUntilLoop)
                    return "do " + Block.ToString(context, false) + " until " + Condition.ToString(context) + ";\n";

                return "while " + (Condition != null ? Condition.ToString(context) : "(true)") + "\n" + Block.ToString(context);
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
                return "with (" + NewEnv.ToString(context) + ")\n" + Block.ToString(context);
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
                sb.Append("{\n");
                foreach (var casee in Cases)
                {
                    sb.Append("    ");
                    sb.Append(casee.ToString(context).Replace("\n", "\n    "));
                    sb.Append("\n");
                }
                sb.Append("}\n");
                return sb.ToString();
            }
        }

        public class HLSwitchCaseStatement : HLStatement
        {
            public List<Expression> CaseExpressions;
            public BlockHLStatement Block;
            public bool ShowBreak = true;

            public HLSwitchCaseStatement(List<Expression> caseExpressions, BlockHLStatement block)
            {
                Debug.Assert(caseExpressions.Count > 0);
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
                foreach (Expression caseExpr in CaseExpressions)
                {
                    if (caseExpr != null)
                        sb.Append("case " + caseExpr.ToString(context) + ":\n");
                    else
                        sb.Append("default:\n");
                }
                if (Block.Statements.Count > 0)
                {
                    sb.Append("    ");
                    sb.Append(Block.ToString(context, false, true).Replace("\n", "\n    "));
                }
                if (ShowBreak && (Block.Statements.Count == 0 || !(Block.Statements.Last() is ReturnStatement)))
                    sb.Append((Block.Statements.Count > 0 ? "\n" : "") + "    break");
                return sb.ToString();
            }
        }

        // Based on http://www.backerstreet.com/decompiler/loop_analysis.php
        public static Dictionary<Block, List<Block>> ComputeDominators(Dictionary<uint, Block> blocks, Block entryBlock, bool reversed)
        {
            List<Block> blockList = blocks.Values.ToList();
            List<BitArray> dominators = new List<BitArray>();

            for (int i = 0; i < blockList.Count; i++)
            {
                dominators.Add(new BitArray(blockList.Count));
                dominators[i].SetAll(true);
            }

            var entryBlockId = blockList.IndexOf(entryBlock);
            dominators[entryBlockId].SetAll(false);
            dominators[entryBlockId].Set(entryBlockId, true);

            BitArray temp = new BitArray(blockList.Count);
            bool changed = true;
            do
            {
                changed = false;
                for (int i = 0; i < blockList.Count; i++)
                {
                    if (i == entryBlockId)
                        continue;

                    IEnumerable<Block> e = blockList[i].entryPoints;
                    if (reversed)
                        if (blockList[i].conditionalExit)
                            e = new Block[] { blockList[i].nextBlockTrue, blockList[i].nextBlockFalse };
                        else
                            e = new Block[] { blockList[i].nextBlockTrue };
                    foreach (Block pred in e)
                    {
                        if (pred == null)
                            continue; // Happens in do-until loops. No other known situations.

                        var predId = blockList.IndexOf(pred);
                        Debug.Assert(predId >= 0);
                        temp.SetAll(false);
                        temp.Or(dominators[i]);
                        dominators[i].And(dominators[predId]);
                        dominators[i].Set(i, true);
                        /*if (!dominators[i].SequenceEquals(temp))
                            changed = true;*/
                        for (var j = 0; j < blockList.Count; j++)
                            if (dominators[i][j] != temp[j])
                            {
                                changed = true;
                                break;
                            }
                    }
                }
            } while (changed);

            Dictionary<Block, List<Block>> result = new Dictionary<Block, List<Block>>();
            for (var i = 0; i < blockList.Count; i++)
            {
                result[blockList[i]] = new List<Block>();
                for (var j = 0; j < blockList.Count; j++)
                {
                    if (dominators[i].Get(j))
                        result[blockList[i]].Add(blockList[j]);
                }
            }
            return result;
        }

        private static List<Block> NaturalLoopForEdge(Block header, Block tail)
        {
            Stack<Block> workList = new Stack<Block>();
            List<Block> loopBlocks = new List<Block>();

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
            var dominators = ComputeDominators(blocks, entryBlock, false);
            Dictionary<Block, List<Block>> loopSet = new Dictionary<Block, List<Block>>();

            foreach (var block in blocks.Values)
            {
                // Every successor that dominates its predecessor
                // must be the header of a loop.
                // That is, block -> succ is a back edge.

                if (block.nextBlockTrue != null && !loopSet.ContainsKey(block.nextBlockTrue))
                {
                    if (dominators[block].Contains(block.nextBlockTrue))
                        loopSet.Add(block.nextBlockTrue, NaturalLoopForEdge(block.nextBlockTrue, block));
                }
                if (block.nextBlockFalse != null && block.nextBlockTrue != block.nextBlockFalse && !loopSet.ContainsKey(block.nextBlockFalse))
                {
                    if (dominators[block].Contains(block.nextBlockFalse))
                        loopSet.Add(block.nextBlockFalse, NaturalLoopForEdge(block.nextBlockFalse, block));
                }
            }

            return loopSet;
        }

        public static Block FindFirstMeetPoint(Block ifStart, Dictionary<Block, List<Block>> reverseDominators)
        {
            Debug.Assert(ifStart.conditionalExit);
            var commonDominators = reverseDominators[ifStart.nextBlockTrue].Intersect(reverseDominators[ifStart.nextBlockFalse]);

            // find the closest one of them
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

        /*public class ExpressionCollapsedCondition : Expression
        {
            public Expression left;
            public string op;
            public Expression right;

            public ExpressionCollapsedCondition(Expression left, string op, Expression right)
            {
                this.left = left;
                this.op = op;
                this.right = right;
            }

            public override string ToString()
            {
                return "(" + left.ToString() + " " + op + right.ToString() + ")";
            }
        }

        private static Block HLCollapseMultiIf(Block entryBlock, Expression expr)
        {
            bool? ifTrueThen = null;
            bool? ifFalseThen = null;
            if (!entryBlock.nextBlockTrue.conditionalExit && entryBlock.nextBlockTrue.Statements.Count == 1)
            {
                ifTrueThen = ((short)((entryBlock.nextBlockTrue.Statements[0] as TempVarAssigmentStatement)?.Value as ExpressionConstant).Value) != 0;
            }
            if (!entryBlock.nextBlockFalse.conditionalExit && entryBlock.nextBlockFalse.Statements.Count == 1)
            {
                ifFalseThen = ((short)((entryBlock.nextBlockFalse.Statements[0] as TempVarAssigmentStatement)?.Value as ExpressionConstant).Value) != 0;
            }
        }*/

        private static BlockHLStatement HLDecompileBlocks(DecompileContext context, ref Block block, Dictionary<uint, Block> blocks, Dictionary<Block, List<Block>> loops, Dictionary<Block, List<Block>> reverseDominators, List<Block> alreadyVisited, Block currentLoop = null, bool decompileTheLoop = false, Block stopAt = null, bool allowBreak = false)
        {
            BlockHLStatement output = new BlockHLStatement();
            bool foundBreak = false;

            while (block != stopAt && block != null)
            {
                if (loops.ContainsKey(block) && !decompileTheLoop)
                {
                    if (block != currentLoop)
                    {
                        output.Statements.Add(new LoopHLStatement() { Block = HLDecompileBlocks(context, ref block, blocks, loops, reverseDominators, alreadyVisited, block, true, null) });
                        continue;
                    }
                    else
                    {
                        // this is a continue statement
                        output.Statements.Add(new ContinueHLStatement());
                        break;
                    }
                }
                else if (currentLoop != null && !loops[currentLoop].Contains(block))
                {
                    foundBreak = true;
                    if (!allowBreak)
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

                if (output.Statements.Count >= 1 && output.Statements[output.Statements.Count - 1] is TempVarAssigmentStatement && block.Instructions.Count >= 1 && block.Instructions[block.Instructions.Count - 1].Kind == UndertaleInstruction.Opcode.Bt && block.conditionalExit && block.ConditionStatement is ExpressionCompare && (block.ConditionStatement as ExpressionCompare).Opcode == UndertaleInstruction.ComparisonType.EQ)
                {
                    // This is a switch statement!
                    Expression switchExpression = (output.Statements[output.Statements.Count - 1] as TempVarAssigmentStatement).Value;
                    TempVar switchTempVar = (output.Statements[output.Statements.Count - 1] as TempVarAssigmentStatement).Var.Var;
                    output.Statements.RemoveAt(output.Statements.Count - 1);

                    Block meetPoint = FindFirstMeetPoint(block, reverseDominators);
                    if (meetPoint == null)
                        throw new Exception("End of switch not found");

                    bool lastHasBreak = false;
                    bool defaultHasBreak = false;
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
                                throw new Exception("Malformed switch statement: bad contition type (" + cmp.Opcode.ToString().ToUpper() + ")");
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
                                else
                                    block = block.nextBlockTrue;
                            }

                            defaultHasBreak = (block.Instructions.Last()?.Kind == UndertaleInstruction.Opcode.B && block.nextBlockTrue == meetPoint && !block.conditionalExit);
                            break;
                        } else if (!block.nextBlockFalse.conditionalExit)
                            lastHasBreak = (block.nextBlockTrue.Instructions.Last()?.Kind == UndertaleInstruction.Opcode.B && block.nextBlockTrue.nextBlockTrue == meetPoint && !block.nextBlockTrue.conditionalExit);

                        block = block.nextBlockFalse;
                    }

                    List<HLSwitchCaseStatement> cases = new List<HLSwitchCaseStatement>();
                    HLSwitchCaseStatement defaultCase = null;

                    for (var i = 0; i < caseEntries.Count; i++)
                    {
                        var x = caseEntries.ElementAt(i);
                        Block temp = x.Key;

                        Block switchEnd = DetermineSwitchEnd(temp, caseEntries.Count > (i + 1) ? caseEntries.ElementAt(i + 1).Key : null, meetPoint);

                        HLSwitchCaseStatement result = new HLSwitchCaseStatement(x.Value, HLDecompileBlocks(context, ref temp, blocks, loops, reverseDominators, alreadyVisited, currentLoop, false, switchEnd));
                        result.ShowBreak = (switchEnd == meetPoint) && (caseEntries.Count != (i + 2) || lastHasBreak);
                        cases.Add(result);
                        if (result.CaseExpressions.Contains(null))
                            defaultCase = result;

                        Debug.Assert(temp == switchEnd);
                    }


                    if (defaultCase != null)
                    { // Handles default case.

                        defaultCase.ShowBreak = defaultHasBreak;
                        if (defaultCase.Block.Statements.Count == 0) {
                            UndertaleInstruction breakInstruction = context.TargetCode.GetInstructionFromAddress((uint)block.Address + 1);

                            if (breakInstruction.Kind == UndertaleInstruction.Opcode.B) // This is the default-case meet-point if it's b.
                            {
                                uint instructionId = ((uint)block.Address + 1 + (uint)breakInstruction.JumpOffset);
                                if (!blocks.ContainsKey(instructionId))
                                    Console.WriteLine("Error: Bad Target [" + block.Address + ", " + breakInstruction.JumpOffset + "]: " + breakInstruction.ToString());
                                Block switchEnd = blocks[instructionId];

                                Block temp = block;
                                defaultCase.Block = HLDecompileBlocks(context, ref temp, blocks, loops, reverseDominators, alreadyVisited, currentLoop, false, switchEnd);
                                block = switchEnd;
                            }
                            else // If there is no default-case, remove the default break, since that creates different bytecode.
                            {
                                cases.Remove(defaultCase);
                            }
                        }
                    }

                    output.Statements.Add(new HLSwitchStatement(switchExpression, cases));
                    block = block.nextBlockTrue;
                    continue;
                }

                if (block.Statements.Count > 0 && block.Statements.Last() is PushEnvStatement)
                {
                    Debug.Assert(!block.conditionalExit);
                    PushEnvStatement stmt = (block.Statements.Last() as PushEnvStatement);
                    block = block.nextBlockTrue;
                    output.Statements.Add(new WithHLStatement()
                    {
                        NewEnv = stmt.NewEnv,
                        Block = HLDecompileBlocks(context, ref block, blocks, loops, reverseDominators, alreadyVisited, null, false, stopAt, false)
                    });
                    if (block == null)
                        break;
                }
                else if (block.Statements.Count > 0 && block.Statements.Last() is PopEnvStatement)
                {
                    Debug.Assert(!block.conditionalExit);
                    break;
                }

                if (block.conditionalExit && block.ConditionStatement == null && block.nextBlockFalse != null && block.nextBlockFalse.ConditionStatement != null) // DO UNTIL STATEMENT!
                {
                    LoopHLStatement doUntilLoop = new LoopHLStatement();
                    doUntilLoop.Condition = block.nextBlockFalse.ConditionStatement;
                    doUntilLoop.Block = HLDecompileBlocks(context, ref block.nextBlockFalse, blocks, loops, reverseDominators, alreadyVisited, currentLoop, false, block.nextBlockTrue, true);
                    doUntilLoop.IsDoUntilLoop = true;
                    output.Statements.Add(doUntilLoop);
                }

                if (block.conditionalExit && block.ConditionStatement != null) // IF STATEMENT!
                {
                    Block meetPoint = FindFirstMeetPoint(block, reverseDominators);
                    if (meetPoint == null)
                        throw new Exception("End of if not found");

                    IfHLStatement cond = new IfHLStatement();
                    cond.condition = block.ConditionStatement;

                    Block blTrue = block.nextBlockTrue, blFalse = block.nextBlockFalse;
                    cond.trueBlock = HLDecompileBlocks(context, ref blTrue, blocks, loops, reverseDominators, alreadyVisited, currentLoop, false, meetPoint, true);
                    cond.falseBlock = HLDecompileBlocks(context, ref blFalse, blocks, loops, reverseDominators, alreadyVisited, currentLoop, false, meetPoint, true);
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
                        var lastKind = block.Instructions.Last().Kind;
                        block = ((lastKind != UndertaleInstruction.Opcode.Ret && lastKind != UndertaleInstruction.Opcode.Exit)
                            || (block.nextBlockTrue != null && block.nextBlockTrue.nextBlockFalse == null)) ? block.nextBlockTrue : stopAt;
                    }
                }
            }

            if (foundBreak)
                output.Statements.Add(new BreakHLStatement());

            return output;
        }

        private static Statement UnCast(Statement statement)
        {
            if (statement is ExpressionCast)
                return UnCast(((ExpressionCast)statement).Argument);

            return statement;
        }

        private static bool TestNumber(Statement statement, int number, DecompileContext context = null)
        {
            statement = UnCast(statement);
            return (statement is ExpressionConstant) && ((ExpressionConstant)statement).EqualsNumber(number);
        }

        private static List<Statement> HLDecompile(DecompileContext context, Dictionary<uint, Block> blocks, Block entryPoint, Block rootExitPoint)
        {
            Dictionary<Block, List<Block>> loops = ComputeNaturalLoops(blocks, entryPoint);
            /*foreach(var a in loops)
            {
                Debug.WriteLine("LOOP at " + a.Key.Address + " contains blocks: ");
                foreach (var b in a.Value)
                    Debug.WriteLine("* " + b.Address);
            }*/
            var reverseDominators = ComputeDominators(blocks, rootExitPoint, true);
            Block bl = entryPoint;
            return (HLDecompileBlocks(context, ref bl, blocks, loops, reverseDominators, new List<Block>()).CleanBlockStatement(context)).Statements;
        }

        private static Dictionary<uint, Block> PrepareDecompileFlow(UndertaleCode code)
        {
            code.UpdateAddresses();
            Dictionary<uint, Block> blocks = DecompileFlowGraph(code);

            // Throw away unreachable blocks
            // I guess this is a bug in GM:S compiler, it still generates a path to end of script after exit/return
            // and it's throwing off the loop detector for some reason
            bool changed;
            do
            {
                changed = false;
                foreach (var k in blocks.Where(pair => pair.Key != 0 && pair.Value.entryPoints.Count == 0).Select(pair => pair.Key).ToList())
                {
                    //Debug.WriteLine("Throwing away " + k);
                    foreach (var other in blocks.Values)
                        if (other.entryPoints.Contains(blocks[k]))
                            other.entryPoints.Remove(blocks[k]);
                    blocks.Remove(k);
                    changed = true;
                }
            } while (changed);

            // TODO: This doesn't propagate to tempvars in broken switch statements but that will be cleaned up at some point
            AssetIDType propType = AssetIDType.Other;
            // NOTE: This was disabled because it wasn't valid.
            // 1. The type only applied to the first case statement.
            // 2. Even if each case statement had the proper type, it still gives an error when compiling.
            //if (code.Name.Content == "gml_Script___view_set_internal" || code.Name.Content == "gml_Script___view_get")
            //    propType = AssetIDType.e__VW;
            //if (code.Name.Content == "gml_Script___background_set_internal" || code.Name.Content == "gml_Script___background_get_internal")
            //    propType = AssetIDType.e__BG;
            if (propType != AssetIDType.Other)
            {
                var v = code.FindReferencedVars().Where((x) => x.Name.Content == "__prop").FirstOrDefault();
                if (v != null)
                    ExpressionVar.assetTypes.Add(v, propType);
            }

            return blocks;
        }

        public static string Decompile(UndertaleCode code, DecompileContext context)
        {
            context.Setup(code);

            TempVar.TempVarId = 0; //TODO: This should be context-specific.
            ExpressionVar.assetTypes = new Dictionary<UndertaleVariable, AssetIDType>();
            Dictionary<uint, Block> blocks = PrepareDecompileFlow(code);
            DecompileFromBlock(context, blocks[0]);
            FunctionCall.scriptArgs = new Dictionary<string, AssetIDType[]>();
            // TODO: add self to scriptArgs
            DoTypePropagation(context, blocks);
            List<Statement> stmts = HLDecompile(context, blocks, blocks[0], blocks[code.Length / 4]);
            StringBuilder sb = new StringBuilder();

            // Mark local variables as local.
            StringBuilder tempBuilder = new StringBuilder();
            UndertaleCodeLocals locals = context.Data != null ? context.Data.CodeLocals.For(code) : null;

            // Write code.
            foreach (var stmt in stmts)
                sb.Append(stmt.ToString(context) + "\n");
            string decompiledCode = sb.ToString();

            if (locals != null)
            {
                foreach (var local in locals.Locals)
                {
                    if (local.Name.Content == "arguments" || local.Name.Content == "$$$$temp$$$$")
                        continue;

                    if (tempBuilder.Length > 0)
                        tempBuilder.Append(", ");

                    tempBuilder.Append(local.Name.Content);
                }
            }
            else
            {
                // Time to search through this thing manually.
                List<string> localSearch = new List<string>();
                for (int i = 0; i < code.Instructions.Count; i++)
                {
                    var inst = code.Instructions[i];
                    if (inst.Kind == UndertaleInstruction.Opcode.PushLoc)
                    {
                        string name = (inst.Value as UndertaleInstruction.Reference<UndertaleVariable>)?.Target?.Name?.Content;
                        if (name != null && !localSearch.Contains(name))
                            localSearch.Add(name);
                    }
                    else if (inst.Kind == UndertaleInstruction.Opcode.Pop && inst.TypeInst == UndertaleInstruction.InstanceType.Local)
                    {
                        string name = inst.Destination.Target?.Name?.Content;
                        if (name != null && !localSearch.Contains(name))
                            localSearch.Add(name);
                    }
                }
                foreach (string local in localSearch)
                {
                    if (local == "arguments" || local == "$$$$temp$$$$")
                        continue;

                    if (tempBuilder.Length > 0)
                        tempBuilder.Append(", ");

                    tempBuilder.Append(local);
                }
            }

            string oldStr = tempBuilder.ToString();
            for (int i = 0; i < TempVar.TempVarId; i++)
            {
                string tempVarName = TempVar.MakeTemporaryVarName(i + 1);
                if (decompiledCode.Contains(tempVarName) && !oldStr.Contains(tempVarName))
                {
                    if (tempBuilder.Length > 0)
                        tempBuilder.Append(", ");
                    tempBuilder.Append(tempVarName);
                }
            }

            if (tempBuilder.Length > 0)
                decompiledCode = "var " + tempBuilder.ToString() + ";\n" + decompiledCode;

            return decompiledCode;
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
            //sb.AppendLine("    graph [splines=polyline];");
            //sb.AppendLine("");
            foreach (var pair in blocks)
            {
                var block = pair.Value;
                sb.Append("    block_" + pair.Key + " [label=\"");
                sb.Append("[" + block.ToString() + ", Exit: " + block.conditionalExit + (block.nextBlockTrue != null ? ", T: " + block.nextBlockTrue.Address : "") + (block.nextBlockFalse != null ? ", F: " + block.nextBlockFalse.Address : "") + "]\n");
                foreach (var instr in block.Instructions)
                    sb.Append(instr.ToString().Replace("\"", "\\\"") + "\\n");
                sb.Append("\"");
                sb.Append(pair.Key == 0 ? ", color=\"blue\"" : "");
                sb.AppendLine(", shape=\"box\"];");
            }
            sb.AppendLine("");
            foreach (var block in blocks)
            {
                if (block.Value.conditionalExit)
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + " [color=\"green\"];"); //, headport=n, tailport=s
                    if (block.Value.nextBlockFalse != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockFalse.Address + " [color=\"red\"];"); // , headport=n, tailport=s
                }
                else
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + ";"); //  [headport=n, tailport=s]
                }
                /*foreach(var rev in block.Value.entryPoints)
                {
                    if (!rev.Address.HasValue)
                        continue;
                    sb.AppendLine("    block_" + block.Key + " -> block_" + rev.Address + " [color=\"gray\", weight=0]");
                }*/
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
