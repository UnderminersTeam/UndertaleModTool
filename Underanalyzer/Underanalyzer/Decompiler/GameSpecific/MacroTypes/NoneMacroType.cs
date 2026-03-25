/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// A macro type that does not resolve to anything.
/// </summary>
public class NoneMacroType : IMacroTypeInt32, IMacroTypeInt64, IMacroTypeFunctionArgs, IMacroTypeArrayInit, IMacroTypeConditional
{
    /// <summary>
    /// Returns a reusable instance of this macro type, as all instances are identical.
    /// </summary>
    public static NoneMacroType ReusableInstance { get; } = new();

    public bool Required => false;

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data) => null;

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, long data) => null;

    public IFunctionCallNode? Resolve(ASTCleaner cleaner, IFunctionCallNode functionCall) => null;

    public ArrayInitNode? Resolve(ASTCleaner cleaner, ArrayInitNode arrayInit) => null;

    public IExpressionNode? Resolve(ASTCleaner cleaner, IConditionalValueNode node) => null;
}
