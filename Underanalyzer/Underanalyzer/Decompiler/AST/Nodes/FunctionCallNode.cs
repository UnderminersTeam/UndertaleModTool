/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a function call in the AST.
/// </summary>
public class FunctionCallNode(IGMFunction function, List<IExpressionNode> arguments) 
    : IExpressionNode, IStatementNode, IMacroTypeNode, IMacroResolvableNode, IConditionalValueNode, IFunctionCallNode
{
    /// <summary>
    /// The function reference being called.
    /// </summary>
    public IGMFunction Function { get; } = function;

    /// <summary>
    /// Arguments being passed into the function call.
    /// </summary>
    public List<IExpressionNode> Arguments { get; } = arguments;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

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
    public string FunctionName { get => Function.Name.Content; }

    /// <inheritdoc/>
    public string ConditionalTypeName => "FunctionCall";

    /// <inheritdoc/>
    public string ConditionalValue => Function.Name.Content;

    /// <inheritdoc/>
    IExpressionNode IASTNode<IExpressionNode>.Clean(ASTCleaner cleaner)
    {
        // Clean up all arguments
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].Clean(cleaner);
        }

        // Handle special instance types
        switch (Function.Name.Content)
        {
            case VMConstants.SelfFunction:
                return new InstanceTypeNode(IGMInstruction.InstanceType.Self, true) { Duplicated = Duplicated, StackType = StackType };
            case VMConstants.OtherFunction:
                return new InstanceTypeNode(IGMInstruction.InstanceType.Other, true) { Duplicated = Duplicated, StackType = StackType };
            case VMConstants.GlobalFunction:
                return new InstanceTypeNode(IGMInstruction.InstanceType.Global, true) { Duplicated = Duplicated, StackType = StackType };
            case VMConstants.GetInstanceFunction:
                if (Arguments.Count == 0 || Arguments[0] is not Int16Node)
                {
                    throw new DecompilerException($"Expected 16-bit integer parameter to {VMConstants.GetInstanceFunction}");
                }
                Arguments[0].Duplicated = true;
                Arguments[0].StackType = StackType;
                return Arguments[0];
        }

        return CleanupMacroTypes(cleaner);
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        // Just clean up arguments here - special calls are only in expressions
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].Clean(cleaner);
        }

        return CleanupMacroTypes(cleaner);
    }

    /// <inheritdoc/>
    IExpressionNode IASTNode<IExpressionNode>.PostClean(ASTCleaner cleaner)
    {
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].PostClean(cleaner);
        }
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.PostClean(ASTCleaner cleaner)
    {
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].PostClean(cleaner);
        }
        return this;
    }

    /// <summary>
    /// During cleanup, determines/resolves macro types for this node if possible.
    /// </summary>
    private IFunctionCallNode CleanupMacroTypes(ASTCleaner cleaner)
    {
        string functionName = Function.Name.Content;

        if (functionName == VMConstants.ScriptExecuteFunction)
        {
            // Special case: our actual function name is the script index theoretically stored in the first argument.
            // Try finding the script/function name.
            if (Arguments is [Int16Node scriptIndexInt16, ..])
            {
                if (cleaner.Context.GameContext.GetAssetName(AssetType.Script, scriptIndexInt16.Value) is string name)
                {
                    // We found a script!
                    functionName = name;

                    // Update first argument with this name, as well, as it won't get resolved otherwise
                    Arguments[0] = new MacroValueNode(functionName);
                }
            }
            else if (Arguments is [FunctionReferenceNode functionReference, ..])
            {
                // We found a function!
                functionName = functionReference.Function.Name.Content;
            }
            else if (Arguments is [AssetReferenceNode { AssetType: AssetType.Script } assetReference, ..])
            {
                if (cleaner.Context.GameContext.GetAssetName(AssetType.Script, assetReference.AssetId) is string name)
                {
                    // We found a script!
                    functionName = name;
                }
            }
        }

        if (cleaner.GlobalMacroResolver.ResolveFunctionArgumentTypes(cleaner, functionName) is IMacroTypeFunctionArgs argsMacroType)
        {
            if (argsMacroType.Resolve(cleaner, this) is IFunctionCallNode resolved)
            {
                // We found a match!
                return resolved;
            }
        }

        // No resolution found
        return this;
    }

    /// <summary>
    /// Same as <see cref="Print(ASTPrinter)"/>, but with an overridable fragment context for function name lookup.
    /// </summary>
    public void Print(ASTPrinter printer, ASTFragmentContext? overrideFunctionLookupContext = null)
    {
        printer.Write(printer.LookupFunction(Function, overrideFunctionLookupContext));
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

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        Print(printer, null);
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
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
    public IMacroType? GetExpressionMacroType(ASTCleaner cleaner)
    {
        return cleaner.GlobalMacroResolver.ResolveReturnValueType(cleaner, Function.Name.Content);
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }

        // For choose(...), propagate type to all parameters
        if (Function.Name.Content == VMConstants.ChooseFunction)
        {
            bool didAnything = false;

            for (int i = 0; i < Arguments.Count; i++)
            {
                if (Arguments[i] is IMacroResolvableNode argResolvable &&
                    argResolvable.ResolveMacroType(cleaner, type) is IExpressionNode argResolved)
                {
                    Arguments[i] = argResolved;
                    didAnything = true;
                }
            }

            return didAnything ? this : null;
        }

        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return Arguments;
    }
}
