/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a variable being called as a method/function in the AST.
/// </summary>
public class VariableCallNode(IExpressionNode function, IExpressionNode? instance, List<IExpressionNode> arguments) 
    : IExpressionNode, IStatementNode, IConditionalValueNode, IFunctionCallNode
{
    /// <summary>
    /// The function/method variable being called.
    /// </summary>
    public IExpressionNode Function { get; private set; } = function;

    /// <summary>
    /// The instance the method is being called on, or <see langword="null"/> if none.
    /// </summary>
    public IExpressionNode? Instance { get; private set; } = instance;

    /// <summary>
    /// The arguments used in the call.
    /// </summary>
    public List<IExpressionNode> Arguments { get; } = arguments;

    public bool Duplicated { get; set; }
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;
    public bool SemicolonAfter => true;
    public bool EmptyLineBefore => false;
    public bool EmptyLineAfter => false;
    public string? FunctionName => null;

    public string ConditionalTypeName => "VariableCall";
    public string ConditionalValue => ""; // TODO?

    IExpressionNode IASTNode<IExpressionNode>.Clean(ASTCleaner cleaner)
    {
        Function = Function.Clean(cleaner);
        Instance = Instance?.Clean(cleaner);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].Clean(cleaner);
        }

        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        Function = Function.Clean(cleaner);
        Instance = Instance?.Clean(cleaner);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].Clean(cleaner);
        }

        return this;
    }

    public void Print(ASTPrinter printer)
    {
        bool canGenerateParentheses = true;
        if (Instance is not null)
        {
            if (Function is VariableNode variable && variable is { Left: InstanceTypeNode instType } &&
                instType.InstanceType == IGMInstruction.InstanceType.Builtin)
            {
                // We have a "builtin" type on our variable, so use what's on the stack instead.
                // Have to also check if we *need* "self." or not, if that's what Instance happens to be.
                if (Instance is not InstanceTypeNode instType2 || instType2.InstanceType != IGMInstruction.InstanceType.Self || // TODO: for later investigation: does Builtin also need to be checked in 2024 versions?
                    printer.LocalVariableNames.Contains(variable.Variable.Name.Content) ||
                    printer.TopFragmentContext!.NamedArguments.Contains(variable.Variable.Name.Content))
                {
                    Instance.Print(printer);
                    printer.Write('.');
                    canGenerateParentheses = false;
                }
            }
        }
        if (canGenerateParentheses && Function is IMultiExpressionNode)
        {
            printer.Write('(');
            Function.Print(printer);
            printer.Write(')');
        }
        else
        {
            Function.Print(printer);
        }
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
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        if (Instance is not null)
        {
            // TODO: need to check this
            if (Instance.RequiresMultipleLines(printer))
            {
                return true;
            }
        }
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

    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }
}
