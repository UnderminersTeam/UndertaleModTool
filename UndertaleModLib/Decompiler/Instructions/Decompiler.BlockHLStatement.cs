using System.Collections.Generic;
using System.Text;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
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
}