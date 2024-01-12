using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
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
}