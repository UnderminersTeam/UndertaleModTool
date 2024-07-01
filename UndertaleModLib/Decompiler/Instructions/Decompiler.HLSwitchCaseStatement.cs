using System.Collections.Generic;
using System.Text;
using UndertaleModLib.Util;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
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
}