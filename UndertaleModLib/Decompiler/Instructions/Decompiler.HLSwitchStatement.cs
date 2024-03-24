using System.Collections.Generic;
using System.Text;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
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
}