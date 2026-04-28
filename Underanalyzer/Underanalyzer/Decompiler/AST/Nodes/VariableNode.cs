/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a variable reference in the AST.
/// </summary>
public class VariableNode(IGMVariable variable, VariableType referenceType, IExpressionNode left, 
                          List<IExpressionNode>? arrayIndices = null, bool regularPush = false) 
    : IExpressionNode, IMacroTypeNode, IConditionalValueNode
{
    /// <summary>
    /// The variable being referenced.
    /// </summary>
    public IGMVariable Variable { get; } = variable;

    /// <summary>
    /// The type of the variable reference.
    /// </summary>
    public VariableType ReferenceType { get; } = referenceType;

    /// <summary>
    /// The left side of the variable (before a dot, usually).
    /// </summary>
    public IExpressionNode Left { get; internal set; } = left;

    /// <summary>
    /// For array accesses, this is not <see langword="null"/>, and contains all array indexing operations on this variable.
    /// </summary>
    public List<IExpressionNode>? ArrayIndices { get; internal set; } = arrayIndices;

    /// <summary>
    /// If true, means that this variable was pushed with a normal <see cref="Opcode.Push"/> opcode.
    /// </summary>
    public bool RegularPush { get; } = regularPush;

    /// <summary>
    /// Whether this variable node should be forced to print "self." if it is able to.
    /// Meant for tracking obscure compiler quirks.
    /// </summary>
    public bool ForceSelf { get; set; } = false;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public DataType StackType { get; set; } = DataType.Variable;

    /// <inheritdoc/>
    public string ConditionalTypeName => "Variable";

    /// <inheritdoc/>
    public string ConditionalValue => Variable.Name.Content;

    /// <summary>
    /// Returns true if the other variable is referencing an identical variable, within the same expression/statement.
    /// </summary>
    public bool IdenticalToInExpression(VariableNode other)
    {
        // Compare basic attributes
        if (Variable != other.Variable || ReferenceType != other.ReferenceType || Left.GetType() != other.Left.GetType())
        {
            return false;
        }

        // Compare left side
        if (Left is VariableNode leftVariable)
        {
            if (other.Left is not VariableNode otherLeftVariable)
            {
                return false;
            }
            if (!leftVariable.IdenticalToInExpression(otherLeftVariable))
            {
                return false;
            }
        }
        else if (Left is InstanceTypeNode leftInstType)
        {
            if (other.Left is not InstanceTypeNode otherLeftInstType)
            {
                return false;
            }
            if (leftInstType.InstanceType != otherLeftInstType.InstanceType)
            {
                return false;
            }
        }
        else if (Left is Int16Node leftI16)
        {
            if (other.Left is not Int16Node otherLeftI16)
            {
                return false;
            }
            if (leftI16.Value != otherLeftI16.Value)
            {
                return false;
            }
        }
        else if (Left != other.Left)
        {
            // Default; just compare references
            return false;
        }

        // Compare array indices
        if (ArrayIndices is not null)
        {
            if (other.ArrayIndices is null)
            {
                return false;
            }
            if (ArrayIndices.Count != other.ArrayIndices.Count)
            {
                return false;
            }
            for (int i = 0; i < ArrayIndices.Count; i++)
            {
                // Compare index references directly, as these should be duplicated if in the same expression
                if (ArrayIndices[i] != other.ArrayIndices[i])
                {
                    return false;
                }
            }
        }
        else
        {
            if (other.ArrayIndices is not null)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if the other variable is referencing a very similar variable, in the context of a for loop.
    /// Always returns false if the variables have array indices.
    /// </summary>
    public bool SimilarToInForIncrementor(VariableNode other)
    {
        // Compare basic attributes
        if (Variable != other.Variable || ReferenceType != other.ReferenceType || Left.GetType() != other.Left.GetType())
        {
            return false;
        }

        // Compare left side
        if (Left is VariableNode leftVariable)
        {
            if (other.Left is not VariableNode otherLeftVariable)
            {
                return false;
            }
            if (!leftVariable.IdenticalToInExpression(otherLeftVariable))
            {
                return false;
            }
        }
        else if (Left is InstanceTypeNode leftInstType)
        {
            if (other.Left is not InstanceTypeNode otherLeftInstType)
            {
                return false;
            }
            if (leftInstType.InstanceType != otherLeftInstType.InstanceType)
            {
                return false;
            }
        }
        else if (Left is Int16Node leftI16)
        {
            if (other.Left is not Int16Node otherLeftI16)
            {
                return false;
            }
            if (leftI16.Value != otherLeftI16.Value)
            {
                return false;
            }
        }
        else if (Left != other.Left)
        {
            // Default; just compare references
            return false;
        }

        // Don't allow array indices as for incrementor
        // TODO: perhaps relax this at some point, and do a deep expression comparison?
        if (ArrayIndices is not null || other.ArrayIndices is not null)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        // Clean up left side of variable, and get basic instance type, or 0 if none
        Left = Left.Clean(cleaner);
        int instType = (Left as Int16Node)?.Value ?? (int?)((Left as InstanceTypeNode)?.InstanceType) ?? 0;

        // Check if we're a builtin array variable, and if so, rewrite with no array accessor if
        // a 64-bit zero value is supplied as an index. This is a GML compiler quirk.
        if (instType == (int)InstanceType.Self || instType == (int)InstanceType.Builtin)
        {
            if (ArrayIndices is [Int64Node { Value: 0 }] &&
                VMConstants.BuiltinArrayVariables.Contains(Variable.Name.Content))
            {
                // This is most likely a compiler-generated array access; get rid of it
                if (cleaner.Context.Settings.CleanupBuiltinArrayVariables)
                {
                    ArrayIndices = null;
                }
            }
        }

        // Clean up array indices
        if (ArrayIndices is not null)
        {
            for (int i = 0; i < ArrayIndices.Count; i++)
            {
                ArrayIndices[i] = ArrayIndices[i].Clean(cleaner);
            }
        }

        // Determine if Left needs to be grouped
        if (Left is IMultiExpressionNode)
        {
            Left.Group = true;
        }

        // Check if we're a struct argument
        if (cleaner.StructArguments is not null)
        {
            // Verify this is an argument array access
            if (instType is (int)InstanceType.Argument or (int)InstanceType.Self &&
                Variable is { Name.Content: "argument" } &&
                ArrayIndices is [Int16Node arrayIndex])
            {
                if (arrayIndex.Value >= 0 && arrayIndex.Value < cleaner.StructArguments.Count)
                {
                    // We found an argument from the outer context! Clean it (in the outer context) and return it.
                    IExpressionNode arg = cleaner.StructArguments[arrayIndex.Value];
                    ASTFragmentContext context = cleaner.PopFragmentContext();
                    arg = arg.Clean(cleaner);
                    cleaner.PushFragmentContext(context);
                    return arg;
                }
            }
        }

        // Check if we're a regular argument, and set maximum referenced argument variable if so
        if (instType == (int)InstanceType.Argument)
        {
            int num = GetArgumentIndex(cleaner.TopFragmentContext!.MaxReferencedArgument);
            if (num != -1)
            {
                if (num > cleaner.TopFragmentContext!.MaxReferencedArgument)
                {
                    // We have a new maximum!
                    cleaner.TopFragmentContext.MaxReferencedArgument = num;
                }

                if (!cleaner.TopFragmentContext.IsRootFragment)
                {
                    // Generate named argument for later, in case it hasn't already been generated
                    cleaner.TopFragmentContext.GetNamedArgumentName(cleaner.Context, num);
                }
            }
        }

        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        Left = Left.PostClean(cleaner);
        if (ArrayIndices is not null)
        {
            for (int i = 0; i < ArrayIndices.Count; i++)
            {
                ArrayIndices[i] = ArrayIndices[i].PostClean(cleaner);
            }
        }

        if (cleaner.Context.Settings.CleanupLocalVarDeclarations &&
            Left is Int16Node { Value: (int)InstanceType.Local } or InstanceTypeNode { InstanceType: InstanceType.Local })
        {
            // Check if not declared already. If not, check to see if we can hoist an existing declaration.
            string localName = Variable.Name.Content;
            LocalScope currentLocalScope = cleaner.TopFragmentContext!.CurrentLocalScope!;
            if (!currentLocalScope.LocalDeclaredInAnyParentOrSelf(localName))
            {
                // Attempt hoist of declaration (if we can find an existing declaration to hoist)
                if (currentLocalScope.FindBestHoistLocation(localName) is LocalScope hoistScope)
                {
                    // Found a suitable scope to hoist before - mark it as such.
                    hoistScope.HoistedLocals.Add(localName);

                    // Parent scope of the hoist is where the local is actually declared.
                    hoistScope.Parent?.DeclaredLocals?.Add(localName);
                }
            }    
        }

        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        // Print out left side, if necessary
        Int16Node? leftI16 = Left as Int16Node;
        InstanceTypeNode? leftInstType = Left as InstanceTypeNode;
        if (leftI16 is not null || leftInstType is not null)
        {
            // Basic numerical instance type
            int value = leftI16?.Value ?? (int)leftInstType!.InstanceType;
            if (ReferenceType == VariableType.Instance)
            {
                // Room instance ID
                if (value < 0)
                {
                    // If negative, convert to an unsigned short (as old GameMaker versions use this for room instance IDs)
                    value = (ushort)value;
                }
                printer.Write('(');
                printer.Write(value + 100000);
                printer.Write(").");
            }
            else if (value < 0)
            {
                // GameMaker constant instance types
                switch (value)
                {
                    case (int)InstanceType.Self:
                    case (int)InstanceType.Builtin:
                        if (ForceSelf || 
                            (value == (int)InstanceType.Self && ArrayIndices is null && printer.Context.GameContext.UsingSelfToBuiltin) ||
                            leftInstType is { FromBuiltinFunction: true } ||
                            printer.LocalVariableNames.Contains(Variable.Name.Content) ||
                            printer.TopFragmentContext!.NamedArguments.Contains(Variable.Name.Content))
                        {
                            // Need an explicit self in order to not conflict with local,
                            // or a specific compiler quirk involving "self." was found.
                            printer.Write("self.");
                        }
                        break;
                    case (int)InstanceType.Other:
                        printer.Write("other.");
                        break;
                    case (int)InstanceType.All:
                        printer.Write("all.");
                        break;
                    case (int)InstanceType.Global:
                        printer.Write("global.");
                        break;
                }
            }
            else
            {
                // Check if we have an object asset name to use
                string? objectName = printer.Context.GameContext.GetAssetName(AssetType.Object, value);
                if (objectName is not null)
                {
                    // Object asset
                    printer.Write(objectName);
                    printer.Write('.');
                }
                else
                {
                    // Unknown number ID
                    printer.Write('(');
                    printer.Write(value);
                    printer.Write(").");
                }
            }
        }
        else
        {
            // Some expression on the left
            Left.Print(printer);
            printer.Write('.');
        }

        int argIndex = GetArgumentIndex(printer.TopFragmentContext!.MaxReferencedArgument);
        bool namedArgumentArray = false;
        if (argIndex == -1 || printer.TopFragmentContext.IsRootFragment)
        {
            // Variable name
            printer.Write(Variable.Name.Content);
        }
        else
        {
            // Argument name
            string? namedArg = printer.TopFragmentContext!.GetNamedArgumentName(printer.Context, argIndex);
            if (namedArg is not null)
            {
                printer.Write(namedArg);

                // If the variable is a case like "argument[16]", track this so we omit the array index later
                namedArgumentArray = Variable.Name.Content == "argument";
            }
            else
            {
                printer.Write(Variable.Name.Content);
            }
        }

        if (ArrayIndices is not null)
        {
            // Print array indices
            if (printer.Context.GMLv2)
            {
                // For GMLv2, an arbitrary number of array indices are supported
                if (namedArgumentArray)
                {
                    // Named argument array access; skip first index
                    for (int i = 1; i < ArrayIndices.Count; i++)
                    {
                        IExpressionNode index = ArrayIndices[i];
                        printer.Write('[');
                        index.Print(printer);
                        printer.Write(']');
                    }
                }
                else
                {
                    // Normal variable; print all of its indices
                    foreach (IExpressionNode index in ArrayIndices)
                    {
                        printer.Write('[');
                        index.Print(printer);
                        printer.Write(']');
                    }
                }
            }
            else
            {
                // For GMLv1, only two array indices are supported
                printer.Write('[');
                ArrayIndices[0].Print(printer);
                if (ArrayIndices.Count == 2)
                {
                    printer.Write(", ");
                    ArrayIndices[1].Print(printer);
                }
                printer.Write(']');
            }
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        if (Left.RequiresMultipleLines(printer))
        {
            return true;
        }
        if (ArrayIndices is not null)
        {
            foreach (IExpressionNode index in ArrayIndices)
            {
                if (index.RequiresMultipleLines(printer))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public IMacroType? GetExpressionMacroType(ASTCleaner cleaner)
    {
        return cleaner.GlobalMacroResolver.ResolveVariableType(cleaner, Variable.Name.Content);
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

    /// <summary>
    /// Returns the argument index this variable represents, or -1 if this is not an argument variable.
    /// </summary>
    /// <remarks>
    /// If <paramref name="onlyNamedArguments"/> is <see langword="true"/>, this returns -1 for cases such as a direct argument0 or argument[0].
    /// </remarks>
    public int GetArgumentIndex(int maxArgumentArrayIndex, bool onlyNamedArguments = true)
    {
        // Check for argument instance type, if we only accept named arguments
        if (onlyNamedArguments)
        {
            if (Left is not (InstanceTypeNode { InstanceType: InstanceType.Argument } or
                             Int16Node { Value: (short)InstanceType.Argument }))
            {
                return -1;
            }
        }

        // Check variable name and array accessor
        string variableName = Variable.Name.Content;
        if (variableName.StartsWith("argument", StringComparison.Ordinal))
        {
            if (variableName.Length >= "argument".Length + 1 &&
                variableName.Length <= "argument".Length + 2)
            {
                // Normal argument variable
                if (int.TryParse(variableName["argument".Length..], out int num) && num >= 0 && num <= 15)
                {
                    return num;
                }
            }
            else if (variableName == "argument" && ArrayIndices is [Int16Node { Value: >= 16 } index, ..] && index.Value <= maxArgumentArrayIndex)
            {
                // Argument, using array access (introduced in 2024.8)
                return index.Value;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns true if this variable represents the constant <c>undefined</c>, or false otherwise.
    /// </summary>
    public bool IsUndefinedVariable()
    {
        return Variable.Name.Content == "undefined";
    }

    /// <summary>
    /// Returns true if the variable is "simple," meaning that it can be referenced with 
    /// one instruction, or false otherwise.
    /// </summary>
    /// <remarks>
    /// This occurs in variables with no left-side expression, and no array indices.
    /// </remarks>
    public bool IsSimpleVariable()
    {
        return Left is InstanceTypeNode && ArrayIndices is null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        yield return Left;
        if (ArrayIndices is not null)
        {
            foreach (IExpressionNode node in ArrayIndices)
            {
                yield return node;
            }
        }
    }
}
