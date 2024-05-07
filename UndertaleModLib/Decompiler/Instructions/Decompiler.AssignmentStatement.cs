using System;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    /// <summary>
    /// Represents a high-level assignment statement.
    /// </summary>
    public class AssignmentStatement : Statement
    {
        /// <summary>
        /// The variable that will get something assigned to.
        /// </summary>
        public ExpressionVar Destination;
        /// <summary>
        /// The value that will be assigned.
        /// </summary>
        public Expression Value;

        /// <summary>
        /// Whether the destination variable is a local variable.
        /// </summary>
        public bool IsLocalVariable;

        private bool _isStructDefinition, _checkedForDefinition;
        
        /// <summary>
        /// Whether the destination variable is a struct.
        /// </summary>
        public bool IsStructDefinition
        {
            get
            {
                // Quick hack
                if (_checkedForDefinition) 
                    return _isStructDefinition;

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

            if (gms2 && !IsLocalVariable)
            {
                var data = context.GlobalContext.Data;
                var locals = data?.CodeLocals.For(context.TargetCode);
                // Stop decompiler from erroring on missing CodeLocals
                if (locals is not null && locals.HasLocal(varName) && context.LocalVarDefines.Add(varName))
                    IsLocalVariable = true;
            }

            // Someone enlighten me on structs, I'm steering clear for now.
            // And find the "right" way to do this.
            if (Value is FunctionDefinition functionVal && functionVal.Subtype != FunctionDefinition.FunctionType.Struct)
            {
                functionVal.IsStatement = true;
                return functionVal.ToString(context);
            }

            string varPrefix = (IsLocalVariable ? "var " : "");

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

            // Since parenthesized is now the default output of ExpressionTwo (mainly math operations)
            // It is specifically safe in variable assignment to not parenthesize this (e.g. "foo = a + b")
            string stringOfValue;
            if (Value is ExpressionTwo expressionTwoVal)
                stringOfValue = expressionTwoVal.ToStringNoParens(context);
            else
                stringOfValue = Value.ToString(context);

            return String.Format("{0}{1}{2} {3}", varPrefix, varName, context.DecompilingStruct ? ":" : " =", stringOfValue);
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
            if (Value is ExpressionCompare)
                suggestedType = AssetIDType.Boolean;
            return Value.DoTypePropagation(context, Destination.DoTypePropagation(context, suggestedType));
        }
    }
}