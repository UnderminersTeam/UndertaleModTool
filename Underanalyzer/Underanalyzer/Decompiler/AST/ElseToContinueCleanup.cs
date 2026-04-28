/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Helper class for transforming empty if/else chains in the AST into continue statements.
/// </summary>
internal static class ElseToContinueCleanup
{
    /// <summary>
    /// Performs the rewrite on a loop body.
    /// </summary>
    public static void Clean(ASTCleaner cleaner, BlockNode body)
    {
        // Ensure our settings permit this
        if (!cleaner.Context.Settings.CleanupElseToContinue)
        {
            return;
        }

        // Get initial last statement
        if (body.Children.Count == 0)
        {
            return;
        }
        IStatementNode lastStatement = body.Children[^1];

        // Perform our operation as long as we have an empty true block, and an else block
        while (lastStatement is IfNode { TrueBlock.Children: [], ElseBlock: not null } ifNode)
        {
            // We can successfully rewrite this as a continue!
            ifNode.TrueBlock.Children.Add(new ContinueNode());
            body.Children.AddRange(ifNode.ElseBlock.Children);
            ifNode.ElseBlock = null;

            // Get next last statement
            if (body.Children.Count == 0)
            {
                break;
            }
            lastStatement = body.Children[^1];
        }
    }
}
