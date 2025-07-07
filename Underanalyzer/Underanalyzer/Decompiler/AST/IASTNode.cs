/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Base interface for all nodes in the AST.
/// </summary>
public interface IASTNode<T>
{
    /// <summary>
    /// Performs a cleanup pass on this node and all of its sub-nodes.
    /// Returns the cleaned version of the node (which is often the same reference).
    /// </summary>
    public T Clean(ASTCleaner cleaner);

    /// <summary>
    /// Prints this node using the provided printer.
    /// </summary>
    public void Print(ASTPrinter printer);

    /// <summary>
    /// Calculates and returns whether the node will require multiple lines when printed.
    /// </summary>
    public bool RequiresMultipleLines(ASTPrinter printer);
}