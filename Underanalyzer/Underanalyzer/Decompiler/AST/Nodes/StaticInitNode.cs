/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a static initialization block in the AST.
/// </summary>
public class StaticInitNode(BlockNode body) : IStatementNode
{
    /// <summary>
    /// The main block of the static initialization.
    /// </summary>
    public BlockNode Body { get; private set; } = body;

    public bool SemicolonAfter => false;
    public bool EmptyLineBefore { get; private set; }
    public bool EmptyLineAfter { get; private set; }

    public IStatementNode Clean(ASTCleaner cleaner)
    {
        Body.Clean(cleaner);
        Body.UseBraces = false;

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundStaticInitialization;

        return this;
    }

    public void Print(ASTPrinter printer)
    {
        bool prevStaticInitState = printer.TopFragmentContext!.InStaticInitialization;
        printer.TopFragmentContext.InStaticInitialization = true;

        Body.Print(printer);

        printer.TopFragmentContext.InStaticInitialization = prevStaticInitState;
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Body.RequiresMultipleLines(printer);
    }
}
