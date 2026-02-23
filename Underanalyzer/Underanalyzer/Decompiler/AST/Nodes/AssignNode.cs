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
/// Represents an assignment statement in the AST.
/// </summary>
public class AssignNode : IStatementNode, IExpressionNode, IBlockCleanupNode
{
    /// <summary>
    /// The variable being assigned to.
    /// </summary>
    public IExpressionNode Variable { get; private set; }

    /// <summary>
    /// The value being assigned.
    /// </summary>
    public IExpressionNode? Value { get; private set; }

    /// <summary>
    /// The type of assignment being done.
    /// </summary>
    public AssignType AssignKind { get; internal set; }

    /// <summary>
    /// For prefix/postfix/compound, this is the instruction used to do the operation.
    /// </summary>
    public IGMInstruction? BinaryInstruction { get; private set; }

    /// <summary>
    /// Whether this assignment node should declare a local variable specifically.
    /// </summary>
    public bool DeclareLocalVar { get; set; } = false;

    /// <inheritdoc/>
    public bool SemicolonAfter { get => true; }

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public bool EmptyLineBefore 
    { 
        get => Value is IStatementNode stmt && stmt.EmptyLineBefore; 
        set 
        {
            if (Value is IStatementNode stmt)
            {
                stmt.EmptyLineBefore = value;
            }
        }
    }

    /// <inheritdoc/>
    public bool EmptyLineAfter
    {
        get => Value is IStatementNode stmt && stmt.EmptyLineAfter;
        set
        {
            if (Value is IStatementNode stmt)
            {
                stmt.EmptyLineAfter = value;
            }
        }
    }

    /// <inheritdoc/>
    public DataType StackType { get; set; } = DataType.Variable;

    /// <summary>
    /// Different types of assignments: normal (=), compound (e.g. +=), prefix/postfix (e.g. ++)
    /// </summary>
    public enum AssignType
    {
        Normal,
        Compound,
        Prefix,
        Postfix,
        NullishCoalesce
    }

    public AssignNode(IExpressionNode variable, IExpressionNode value)
    {
        Variable = variable;
        Value = value;
        AssignKind = AssignType.Normal;
    }

    public AssignNode(IExpressionNode variable, IExpressionNode value, IGMInstruction binaryInstruction)
    {
        Variable = variable;
        Value = value;
        AssignKind = AssignType.Compound;
        BinaryInstruction = binaryInstruction;
    }

    public AssignNode(IExpressionNode variable, AssignType assignKind, IGMInstruction binaryInstruction)
    {
        Variable = variable;
        Value = null;
        AssignKind = assignKind;
        BinaryInstruction = binaryInstruction;
        // TODO: do we need a special StackType to prevent nesting stack size issues?
    }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        if (AssignKind == AssignType.Normal)
        {
            // Handle resolution of macro types based on variable
            if (Variable is IMacroTypeNode variableTypeNode && Value is IMacroResolvableNode valueResolvable &&
                variableTypeNode.GetExpressionMacroType(cleaner) is IMacroType variableMacroType &&
                valueResolvable.ResolveMacroType(cleaner, variableMacroType) is IExpressionNode valueResolved)
            {
                Value = valueResolved;
            }
        }

        Variable = Variable.Clean(cleaner);
        Value = Value?.Clean(cleaner);

        // Clean up any remaining postfix/compound operations
        if (AssignKind == AssignType.Normal && cleaner.StructArguments is null &&
            Variable is VariableNode variable && Value is BinaryNode binary &&
            binary.Instruction.Kind is Opcode.Add or Opcode.Subtract or Opcode.Multiply or Opcode.Divide or 
                                       Opcode.GMLModulo or Opcode.And or Opcode.Or or Opcode.Xor)
        {
            if (binary.Left is VariableNode binVariable && binVariable.IdenticalToInExpression(variable) &&
                (variable.Duplicated || variable.IsSimpleVariable()))
            {
                // This is probably a compound operation

                // Check if we're a postfix operation
                if (binary.Instruction.Kind is Opcode.Add or Opcode.Subtract && 
                    binary.Right is Int16Node i16 && i16.Value == 1 && i16.RegularPush)
                {
                    AssignKind = AssignType.Postfix;
                    BinaryInstruction = binary.Instruction;
                    Value = null;

                    return this;
                }

                // Ensure we actually are a compound operation (Push vs. specialized Push instruction, as well as
                // quirk with division converting to double when NOT a compound assignment)
                if ((cleaner.Context.OlderThanBytecode15 || binVariable.RegularPush || binVariable.Variable.InstanceType == InstanceType.Self) &&
                    (binary.Instruction.Kind != Opcode.Divide || binary.Right is DoubleNode || binary.Right.StackType != DataType.Double))
                {
                    AssignKind = AssignType.Compound;
                    BinaryInstruction = binary.Instruction;
                    Value = binary.Right;

                    return this;
                }
            }
        }

        return this;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        if (cleaner.Context.Settings.CleanupLocalVarDeclarations &&
            AssignKind == AssignType.Normal &&
            Variable is VariableNode
            {
                Left: Int16Node { Value: (int)InstanceType.Local } or InstanceTypeNode { InstanceType: InstanceType.Local },
                Variable.Name: IGMString { Content: string localName }
            })
        {
            // We have a local variable which is (at least) declared by the time of this assignment.
            LocalScope currentLocalScope = cleaner.TopFragmentContext!.CurrentLocalScope!;
            if (!currentLocalScope.LocalDeclaredInAnyParentOrSelf(localName) && 
                currentLocalScope.DeclaredLocals.Add(localName))
            {
                // Track this AssignNode to potentially generate a declaration later.
                currentLocalScope.FirstLocalAssignments.Add(localName, this);
            }
        }

        Variable = Variable.PostClean(cleaner);
        Value = Value?.PostClean(cleaner);

        return this;
    }

    /// <inheritdoc/>
    IExpressionNode IASTNode<IExpressionNode>.Clean(ASTCleaner cleaner)
    {
        Variable = Variable.Clean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    IExpressionNode IASTNode<IExpressionNode>.PostClean(ASTCleaner cleaner)
    {
        Variable = Variable.PostClean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        // Check if our variable is a local that needs to be purged
        if (Variable is VariableNode assignVar && assignVar.Variable.InstanceType == InstanceType.Local &&
            block.FragmentContext.LocalVariablesToPurge.Contains(assignVar.Variable.Name.Content))
        {
            // Purge this assignment
            block.Children.RemoveAt(i);
            return i - 1;
        }

        // Cancel if our settings specify not to clean up try statements
        if (!cleaner.Context.Settings.CleanupTry)
        {
            return i;
        }

        if (i + 2 >= block.Children.Count)
        {
            // There can't be a try statement ahead - not enough room
            return i;
        }

        // Check for correct assignments and try
        if (block.Children[i + 1] is not AssignNode assign2 || block.Children[i + 2] is not TryCatchNode tryNode)
        {
            return i;
        }
        if (Variable is not VariableNode breakVariable ||
            !breakVariable.Variable.Name.Content.StartsWith(VMConstants.TryBreakVariable, StringComparison.Ordinal) ||
            Value is not Int16Node { Value: 0 })
        {
            return i;
        }
        if (assign2.Variable is not VariableNode continueVariable ||
            !continueVariable.Variable.Name.Content.StartsWith(VMConstants.TryContinueVariable, StringComparison.Ordinal) ||
            assign2.Value is not Int16Node { Value: 0 })
        {
            return i;
        }

        // Assign these variable names to the try statement
        tryNode.BreakVariableName = breakVariable.Variable.Name.Content;
        tryNode.ContinueVariableName = continueVariable.Variable.Name.Content;
        block.FragmentContext.LocalVariablesToPurge.Add(tryNode.BreakVariableName);
        block.FragmentContext.LocalVariablesToPurge.Add(tryNode.ContinueVariableName);

        // Remove assignments
        block.Children.RemoveRange(i, 2);

        // Additionally remove if statements after the try, if applicable
        if (i + 2 < block.Children.Count &&
            block.Children[i + 1] is IfNode { TrueBlock: BlockNode { Children: [ContinueNode] }, ElseBlock: null } ifNode1 &&
            block.Children[i + 2] is IfNode { TrueBlock: BlockNode { Children: [BreakNode] }, ElseBlock: null } ifNode2)
        {
            if (ifNode1.Condition is VariableNode ifVar1 && ifVar1.Variable == continueVariable.Variable &&
                ifNode2.Condition is VariableNode ifVar2 && ifVar2.Variable == breakVariable.Variable)
            {
                block.Children.RemoveRange(i + 1, 2);
            }
        }
        else
        {
            // If continue didn't resolve as a continue statement, then it resolves as an else if
            if (i + 1 < block.Children.Count &&
                block.Children[i + 1] is IfNode { TrueBlock: BlockNode { Children: [] }, ElseBlock: BlockNode { Children: [IfNode ifNode4] } } ifNode3 &&
                ifNode4 is { TrueBlock: BlockNode { Children: [BreakNode] }, ElseBlock: null })
            {
                if (ifNode3.Condition is VariableNode ifVar1 && ifVar1.Variable == continueVariable.Variable &&
                    ifNode4.Condition is VariableNode ifVar2 && ifVar2.Variable == breakVariable.Variable)
                {
                    block.Children.RemoveAt(i + 1);
                }
            }
        }

        // Set up for cleaning the try itself, next iteration
        return i - 1;
    }

    /// <summary>
    /// Returns whether a variable name is a valid GML identifier or not.
    /// </summary>
    private static bool VariableNameIsValidIdentifier(string name)
    {
        // If name is empty, it's clearly not valid
        if (name.Length == 0)
        {
            return false;
        }

        // Check first character
        char firstChar = name[0];
        if ((firstChar < 'a' || firstChar > 'z') && 
            (firstChar < 'A' || firstChar > 'Z') && 
            firstChar != '_')
        {
            return false;
        }

        // Check all other characters
        for (int i = 1; i < name.Length; i++)
        {
            char c = name[i];
            if ((c < 'a' || c > 'z') &&
                (c < 'A' || c > 'Z') &&
                (c < '0' || c > '9') &&
                c != '_')
            {
                return false;
            }
        }

        // Every character was valid!
        return true;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        switch (AssignKind)
        {
            case AssignType.Normal:
                if (printer.StructArguments is not null)
                {
                    // We're inside a struct initialization block
                    if (Variable is VariableNode { Variable.Name.Content: string variableName })
                    {
                        // Write just the variable name if possible
                        if (VariableNameIsValidIdentifier(variableName))
                        {
                            printer.Write(variableName);
                        }
                        else
                        {
                            StringNode.PrintGMS2String(printer, variableName);
                        }
                    }
                    else
                    {
                        Variable.Print(printer);
                    }
                    printer.Write(": ");
                    Value!.Print(printer);
                }
                else
                {
                    if (printer.TopFragmentContext!.InStaticInitialization)
                    {
                        // In static initialization, we prepend the "static" keyword to the assignment
                        printer.Write("static ");
                    }
                    else if (DeclareLocalVar &&
                             printer.Context.Settings.CleanupLocalVarDeclarations &&
                             Variable is VariableNode { Variable.Name: IGMString { Content: string localName } } variable)
                    {
                        // Local variable getting declared here
                        printer.Write("var ");
                        
                        // If array indices are used here, we actually need to split
                        // this into two lines (avoiding invalid syntax).
                        if (variable.ArrayIndices is not null)
                        {
                            printer.Write(localName);
                            printer.Semicolon();
                            printer.EndLine();
                            printer.StartLine();
                        }
                    }

                    // Normal assignment
                    Variable.Print(printer);
                    printer.Write(" = ");
                    Value!.Print(printer);
                }
                break;
            case AssignType.Prefix:
                printer.Write((BinaryInstruction!.Kind == Opcode.Add) ? "++" : "--");
                Variable.Print(printer);
                break;
            case AssignType.Postfix:
                Variable.Print(printer);
                printer.Write((BinaryInstruction!.Kind == Opcode.Add) ? "++" : "--");
                break;
            case AssignType.Compound:
                Variable.Print(printer);
                printer.Write(BinaryInstruction!.Kind switch
                {
                    Opcode.Add => " += ",
                    Opcode.Subtract => " -= ",
                    Opcode.Multiply => " *= ",
                    Opcode.Divide => " /= ",
                    Opcode.GMLModulo => " %= ",
                    Opcode.And => " &= ",
                    Opcode.Or => " |= ",
                    Opcode.Xor => " ^= ",
                    _ => throw new DecompilerException("Unknown binary instruction opcode in compound assignment")
                });
                Value!.Print(printer);
                break;
            case AssignType.NullishCoalesce:
                Variable.Print(printer);
                printer.Write(" ??= ");
                Value!.Print(printer);
                break;
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        if (Variable.RequiresMultipleLines(printer))
        {
            return true;
        }
        if (Value is not null && Value.RequiresMultipleLines(printer))
        {
            return true;
        }
        if (DeclareLocalVar && printer.Context.Settings.CleanupLocalVarDeclarations && Variable is VariableNode { ArrayIndices: not null })
        {
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        yield return Variable;
        if (Value is not null)
        {
            yield return Value;
        }
    }
}
