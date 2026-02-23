/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents the "new" keyword being used to instantiate an object in the AST.
/// </summary>
public class NewObjectNode(IExpressionNode function, List<IExpressionNode> arguments) 
    : IExpressionNode, IStatementNode, IConditionalValueNode, IFunctionCallNode
{
    /// <summary>
    /// The function (constructor) being used.
    /// </summary>
    public IExpressionNode Function { get; private set; } = function;

    /// <summary>
    /// The arguments passed into the function (constructor).
    /// </summary>
    public List<IExpressionNode> Arguments { get; private set; } = arguments;

    /// <inheritdoc/>
    public bool Duplicated { get; set; }

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    /// <inheritdoc/>
    public bool SemicolonAfter => true;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get => false; set => _ = value; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get => false; set => _ = value; }

    /// <inheritdoc/>
    public string? FunctionName { get => (Function is FunctionReferenceNode functionRef) ? functionRef.Function.Name.Content : null; }

    /// <inheritdoc/>
    public string ConditionalTypeName => "NewObject";

    /// <inheritdoc/>
    public string ConditionalValue => ""; // TODO?

    /// <summary>
    /// Cleans up the function and arguments of this node (as well as identifying a compiler quirk with "self.").
    /// </summary>
    private void CleanFunctionAndArgs(ASTCleaner cleaner)
    {
        Function = Function.Clean(cleaner);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].Clean(cleaner);
        }

        // If function is a singular variable node with "self", that implies a compiler quirk when "self." is directly used.
        if (Function is VariableNode { Left: InstanceTypeNode { InstanceType: IGMInstruction.InstanceType.Self } } variable)
        {
            variable.ForceSelf = true;
        }
    }

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        CleanFunctionAndArgs(cleaner);

        if (cleaner.GlobalMacroResolver.ResolveFunctionArgumentTypes(cleaner, FunctionName) is IMacroTypeFunctionArgs argsMacroType)
        {
            if (argsMacroType.Resolve(cleaner, this) is IFunctionCallNode resolved)
            {
                // We found a match!
                return resolved;
            }
        }

        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        CleanFunctionAndArgs(cleaner);

        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        Function = Function.PostClean(cleaner);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].PostClean(cleaner);
        }
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.PostClean(ASTCleaner cleaner)
    {
        Function = Function.PostClean(cleaner);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].PostClean(cleaner);
        }
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }

        printer.Write("new ");
        Function.Print(printer);
        printer.Write('(');
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i].Print(printer);
            if (i != Arguments.Count - 1)
            {
                printer.Write(", ");
            }
        }
        printer.Write(')');

        if (Group)
        {
            printer.Write(')');
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        if (Function.RequiresMultipleLines(printer))
        {
            return true;
        }
        foreach (IExpressionNode arg in Arguments)
        {
            if (arg.RequiresMultipleLines(printer))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        yield return Function;
        foreach (IExpressionNode arg in Arguments)
        {
            yield return arg;
        }
    }
}
