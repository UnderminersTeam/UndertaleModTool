/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a with loop in the AST.
/// </summary>
public class WithLoopNode(IExpressionNode target, BlockNode body) : IStatementNode
{
    /// <summary>
    /// The target of the with loop (object/instance).
    /// </summary>
    public IExpressionNode Target { get; private set; } = target;

    /// <summary>
    /// The main block of the loop.
    /// </summary>
    public BlockNode Body { get; private set; } = body;

    public bool SemicolonAfter { get => false; }
    public bool EmptyLineBefore { get; private set; }
    public bool EmptyLineAfter { get; private set; }

    public IStatementNode Clean(ASTCleaner cleaner)
    {
        Target = Target.Clean(cleaner);
        if (Target is IMacroResolvableNode targetResolvable && cleaner.MacroInstanceIdOrObjectAsset is not null &&
            targetResolvable.ResolveMacroType(cleaner, cleaner.MacroInstanceIdOrObjectAsset) is IExpressionNode targetResolved)
        {
            Target = targetResolved;
        }
        ElseToContinueCleanup.Clean(cleaner, Body);
        Body.Clean(cleaner);

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return this;
    }

    public void Print(ASTPrinter printer)
    {
        printer.Write("with (");
        Target.Print(printer);
        printer.Write(')');
        if (printer.Context.Settings.RemoveSingleLineBlockBraces && !Body.RequiresMultipleLines(printer))
        {
            Body.PrintSingleLine(printer);
        }
        else
        {
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            Body.Print(printer);
        }
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return true;
    }
}
