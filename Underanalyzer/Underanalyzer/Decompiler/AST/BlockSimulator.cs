/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Handles simulating VM instructions within a single control flow block.
/// </summary>
internal class BlockSimulator
{
    private static readonly Dictionary<DataType, int> DataTypeToSize = [];

    /// <summary>
    /// Initializes precomputed data for VM simulation.
    /// </summary>
    static BlockSimulator()
    {
        // Load in data type sizes
        Type typeDataType = typeof(DataType);
        foreach (DataType dataType in Enum.GetValues(typeDataType))
        {
            var field = typeDataType.GetField(Enum.GetName(typeDataType, dataType) ?? throw new NullReferenceException()) 
                                                                                   ?? throw new NullReferenceException();
            var info = field.GetCustomAttribute<DataTypeInfo>() ?? throw new NullReferenceException();
            DataTypeToSize[dataType] = info.Size;
        }
    }

    /// <summary>
    /// Simulates a single control flow block, outputting to the output list.
    /// </summary>
    public static void Simulate(ASTBuilder builder, List<IStatementNode> output, ControlFlow.Block block)
    {
        for (int i = builder.StartBlockInstructionIndex; i < block.Instructions.Count; i++)
        {
            IGMInstruction instr = block.Instructions[i];

            switch (instr.Kind)
            {
                case Opcode.Add:
                case Opcode.Subtract:
                case Opcode.Multiply:
                case Opcode.Divide:
                case Opcode.And:
                case Opcode.Or:
                case Opcode.GMLModulo:
                case Opcode.GMLDivRemainder:
                case Opcode.Xor:
                case Opcode.ShiftLeft:
                case Opcode.ShiftRight:
                case Opcode.Compare:
                    SimulateBinary(builder, instr);
                    break;
                case Opcode.Not:
                case Opcode.Negate:
                    builder.ExpressionStack.Push(new UnaryNode(builder.ExpressionStack.Pop(), instr));
                    break;
                case Opcode.Convert:
                    SimulateConvert(builder, instr);
                    break;
                case Opcode.PopDelete:
                    SimulatePopDelete(builder, output);
                    break;
                case Opcode.Call:
                    SimulateCall(builder, output, instr);
                    break;
                case Opcode.CallVariable:
                    SimulateCallVariable(builder, instr);
                    break;
                case Opcode.Push:
                case Opcode.PushLocal:
                case Opcode.PushGlobal:
                case Opcode.PushBuiltin:
                    SimulatePush(builder, instr);
                    break;
                case Opcode.PushImmediate:
                    builder.ExpressionStack.Push(new Int16Node(instr.ValueShort, false));
                    break;
                case Opcode.Pop:
                    SimulatePopVariable(builder, output, instr);
                    break;
                case Opcode.Duplicate:
                    SimulateDuplicate(builder, instr);
                    break;
                case Opcode.Extended:
                    SimulateExtended(builder, output, instr);
                    break;
            }
        }

        builder.StartBlockInstructionIndex = 0;
    }

    /// <summary>
    /// Simulates a single Duplicate instruction.
    /// </summary>
    private static void SimulateDuplicate(ASTBuilder builder, IGMInstruction instr)
    {
        DataType dupType = instr.Type1;
        int dupTypeSize = DataTypeToSize[dupType];
        int dupSize = instr.DuplicationSize;
        int dupSwapSize = instr.DuplicationSize2;

        if (dupSwapSize != 0)
        {
            // "Dup Swap" mode (GMLv2 version of "Pop Swap" mode)
            if (dupType == DataType.Variable && dupSize == 0)
            {
                // Exit early; basically a no-op instruction
                return;
            }

            // Load top data from stack
            int topSize = dupSize * dupTypeSize;
            Stack<IExpressionNode> topStack = new();
            while (topSize > 0)
            {
                IExpressionNode curr = builder.ExpressionStack.Pop();
                topStack.Push(curr);
                topSize -= DataTypeToSize[curr.StackType];
            }

            // Load bottom data from stack
            int bottomSize = dupSwapSize * dupTypeSize;
            Stack<IExpressionNode> bottomStack = new();
            while (bottomSize > 0)
            {
                IExpressionNode curr = builder.ExpressionStack.Pop();
                bottomStack.Push(curr);
                bottomSize -= DataTypeToSize[curr.StackType];
            }

            // Ensure we didn't read too much data accidentally
            if (topSize < 0 || bottomSize < 0)
            {
                throw new DecompilerException(
                    $"Dup swap read too much data from stack " +
                    $"({dupSize * dupTypeSize} -> {topSize}, {dupSwapSize * dupTypeSize} -> {bottomSize})");
            }

            // Push top data back first (so that it ends up at the bottom)
            while (topStack.Count > 0)
            {
                builder.ExpressionStack.Push(topStack.Pop());
            }

            // Push bottom data back second (so that it ends up at the top)
            while (bottomStack.Count > 0)
            {
                builder.ExpressionStack.Push(bottomStack.Pop());
            }
        }
        else
        {
            // Normal duplication mode
            int size = (dupSize + 1) * dupTypeSize;
            List<IExpressionNode> toDuplicate = [];
            while (size > 0)
            {
                IExpressionNode curr = builder.ExpressionStack.Pop();
                toDuplicate.Add(curr);
                curr.Duplicated = true;
                size -= DataTypeToSize[curr.StackType];
            }

            // Ensure we didn't read too much data accidentally
            if (size < 0)
            {
                throw new DecompilerException(
                    $"Dup read too much data from stack ({(dupSize + 1) * dupTypeSize} -> {size})");
            }

            // Push data back to the stack twice (duplicating it, while maintaining internal order)
            for (int i = 0; i < 2; i++)
            {
                for (int j = toDuplicate.Count - 1; j >= 0; j--)
                {
                    builder.ExpressionStack.Push(toDuplicate[j]);
                }
            }
        }
    }

    /// <summary>
    /// Simulates a single push instruction (besides <see cref="Opcode.PushImmediate"/>).
    /// </summary>
    private static void SimulatePush(ASTBuilder builder, IGMInstruction instr)
    {
        switch (instr.Type1)
        {
            case DataType.Int32:
                if (instr.Function is not null)
                {
                    // Function references in GMLv2 are pushed this way in certain versions
                    builder.ExpressionStack.Push(new FunctionReferenceNode(instr.Function));
                }
                else if (instr.Variable is not null)
                {
                    // Variable hashes in recent version of GMLv2 are pushed this way
                    builder.ExpressionStack.Push(new VariableHashNode(instr.Variable));
                }
                else
                {
                    builder.ExpressionStack.Push(new Int32Node(instr.ValueInt));
                }
                break;
            case DataType.String:
                builder.ExpressionStack.Push(new StringNode(instr.ValueString ?? throw new DecompilerException("Missing string on instruction")));
                break;
            case DataType.Double:
                builder.ExpressionStack.Push(new DoubleNode(instr.ValueDouble));
                break;
            case DataType.Int64:
                builder.ExpressionStack.Push(new Int64Node(instr.ValueLong));
                break;
            case DataType.Int16:
                builder.ExpressionStack.Push(new Int16Node(instr.ValueShort, true));
                break;
            case DataType.Variable:
                SimulatePushVariable(builder, instr);
                break;
        }
    }

    /// <summary>
    /// Simulates a single variable push instruction.
    /// </summary>
    private static void SimulatePushVariable(ASTBuilder builder, IGMInstruction instr)
    {
        IGMVariable gmVariable = instr.Variable ?? throw new DecompilerException("Missing variable on instruction");

        // If this is a local variable, add it to the fragment context
        if (gmVariable.InstanceType == InstanceType.Local)
        {
            string localName = gmVariable.Name.Content;
            if (builder.LocalVariableNames.Add(localName))
            {
                builder.LocalVariableNamesList.Add(localName);
            }
        }

        // Update left side of the variable
        IExpressionNode left;
        List<IExpressionNode>? arrayIndices = null;
        if (instr.InstType == InstanceType.StackTop || instr.ReferenceVarType == VariableType.StackTop)
        {
            // Left side is just on the top of the stack
            left = builder.ExpressionStack.Pop();
        }
        else if (instr.ReferenceVarType == VariableType.Array)
        {
            // Left side comes after basic array indices
            arrayIndices = SimulateArrayIndices(builder);
            left = builder.ExpressionStack.Pop();
        }
        else if (instr.ReferenceVarType is VariableType.MultiPush or VariableType.MultiPushPop)
        {
            // Left side comes after a single array index
            arrayIndices = [builder.ExpressionStack.Pop()];
            left = builder.ExpressionStack.Pop();
        }
        else
        {
            // Simply use the instance type stored on the instruction as the left side
            left = new InstanceTypeNode(instr.InstType);
        }

        // If the left side of the variable is the instance type of StackTop, then we go one level further.
        // This is done in the VM for GMLv2's structs/objects, as they don't have instance IDs.
        if (left is Int16Node i16 && i16.Value == (short)InstanceType.StackTop)
        {
            left = builder.ExpressionStack.Pop();
        }

        builder.ExpressionStack.Push(new VariableNode(instr.Variable ?? throw new DecompilerException("Missing variable on instruction"),
                                                      instr.ReferenceVarType, left, arrayIndices, instr.Kind == Opcode.Push));
    }

    /// <summary>
    /// Simulates a single Pop instruction.
    /// </summary>
    private static void SimulatePopVariable(ASTBuilder builder, List<IStatementNode> output, IGMInstruction instr)
    {
        IGMVariable? gmVariable = instr.Variable;

        if (gmVariable is null)
        {
            // "Pop Swap" instruction variant - just moves stuff around on the stack
            IExpressionNode e1 = builder.ExpressionStack.Pop();
            IExpressionNode e2 = builder.ExpressionStack.Pop();
            if (instr.PopSwapSize == 5)
            {
                // Non-array variant (e3 should be stacktop parameter)
                IExpressionNode e3 = builder.ExpressionStack.Pop();
                builder.ExpressionStack.Push(e1);
                builder.ExpressionStack.Push(e3);
                builder.ExpressionStack.Push(e2);
            }
            else
            {
                // Array variant (e3 and e4 should be array parameters)
                IExpressionNode e3 = builder.ExpressionStack.Pop();
                IExpressionNode e4 = builder.ExpressionStack.Pop();
                builder.ExpressionStack.Push(e1);
                builder.ExpressionStack.Push(e4);
                builder.ExpressionStack.Push(e3);
                builder.ExpressionStack.Push(e2);
            }
            return;
        }

        IExpressionNode? valueToAssign = null;

        // If this is a local variable, add it to the fragment context
        if (gmVariable.InstanceType == InstanceType.Local)
        {
            string localName = gmVariable.Name.Content;
            if (builder.LocalVariableNames.Add(localName))
            {
                builder.LocalVariableNamesList.Add(localName);
            }
        }

        // Pop value immediately if first type is Int32
        if (instr.Type1 == DataType.Int32)
        {
            valueToAssign = builder.ExpressionStack.Pop();
        }

        // Update left side of the variable
        IExpressionNode left;
        List<IExpressionNode>? arrayIndices = null;
        if (instr.ReferenceVarType == VariableType.StackTop)
        {
            // Left side is just on the top of the stack
            left = builder.ExpressionStack.Pop();
        }
        else if (instr.ReferenceVarType == VariableType.Array)
        {
            // Left side comes after basic array indices
            arrayIndices = SimulateArrayIndices(builder);
            left = builder.ExpressionStack.Pop();
        }
        else
        {
            // Simply use the instance type stored on the instruction as the left side
            left = new InstanceTypeNode(instr.InstType);
        }

        // If the left side of the variable is the instance type of StackTop, then we go one level further.
        // This is done in the VM for GMLv2's structs/objects, as they don't have instance IDs.
        if (left is Int16Node i16 && i16.Value == (short)InstanceType.StackTop)
        {
            left = builder.ExpressionStack.Pop();
        }

        // Create actual variable node
        VariableNode variable = new(gmVariable, instr.ReferenceVarType, left, arrayIndices);

        // Pop value only now if first type isn't Int32
        if (instr.Type1 != DataType.Int32)
        {
            valueToAssign = builder.ExpressionStack.Pop();
        }

        // If the second type is a boolean, check if our value is a 16-bit int (0 or 1), and make it a boolean if so
        if (instr.Type2 == DataType.Boolean && valueToAssign is Int16Node valueI16 && valueI16.Value is 0 or 1)
        {
            valueToAssign = new BooleanNode(valueI16.Value == 1);
        }

        // If we have a binary being assigned, check for prefix/postfix/compound operations
        if (valueToAssign is BinaryNode binary)
        {
            // Check prefix/postfix if the right value of binary is 1
            if (binary is { Right: Int16Node rightInt16 } && rightInt16.Value == 1)
            {
                if (binary.Duplicated)
                {
                    // Prefix detected
                    // TODO: do we need to verify "binary.Left" is the same as "variable"?

                    // Pop off duplicate value (should be copy of "binary"), as we don't need it
                    builder.ExpressionStack.Pop();

                    builder.ExpressionStack.Push(new AssignNode(variable, AssignNode.AssignType.Prefix, binary.Instruction));
                    return;
                }
                if (binary.Left.Duplicated && builder.ExpressionStack.Count > 0 && builder.ExpressionStack.Peek() == binary.Left)
                {
                    // Postfix detected - pop off duplicate value, as we don't 
                    // TODO: do we need to verify "binary.Left" is the same as "variable"?

                    // Pop off duplicate value (should be copy of "binary.Left"), as we don't need it
                    builder.ExpressionStack.Pop();

                    builder.ExpressionStack.Push(new AssignNode(variable, AssignNode.AssignType.Postfix, binary.Instruction));
                    return;
                }
            }

            // Check for compound assignment
            if (variable.Left.Duplicated && binary is { Left: VariableNode })
            {
                // Compound detected
                // TODO: do we need to verify "binary.Left" is the same as "variable"?

                if (binary.Instruction.Kind is Opcode.Add or Opcode.Subtract &&
                    binary.Right is Int16Node compoundI16 && compoundI16.Value == 1 && compoundI16.RegularPush)
                {
                    // Special instruction pattern suggests that this is a postfix statement
                    output.Add(new AssignNode(variable, AssignNode.AssignType.Postfix, binary.Instruction));
                    return;
                }

                // Just a normal compound assignment
                output.Add(new AssignNode(variable, binary.Right, binary.Instruction));
                return;
            }
        }

        // Add statement to output list
        output.Add(new AssignNode(variable, valueToAssign ?? throw new DecompilerException("Failed to get assignment value")));
    }

    /// <summary>
    /// Returns list of array indices for a variable, checking whether 1D or 2D as needed.
    /// </summary>
    private static List<IExpressionNode> SimulateArrayIndices(ASTBuilder builder)
    {
        IExpressionNode index = builder.ExpressionStack.Pop();

        if (builder.Context.GMLv2)
        {
            // In GMLv2 and above, all basic array accesses are 1D
            return [index];
        }

        // Check if this is a 2D array index
        if (index is BinaryNode binary && 
            binary is { Instruction.Kind: Opcode.Add, Left: BinaryNode binary2 } &&
            binary2 is { Instruction.Kind: Opcode.Multiply, Right: Int32Node int32 } &&
            int32.Value == VMConstants.OldArrayLimit)
        {
            return [binary2.Left, binary.Right];
        }

        return [index];
    }

    /// <summary>
    /// Simulates a single Call instruction.
    /// </summary>
    private static void SimulateCall(ASTBuilder builder, List<IStatementNode> output, IGMInstruction instr)
    {
        // Check if we're a special function we need to handle
        string? funcName = instr.Function?.Name?.Content;
        if (funcName is not null)
        {
            switch (funcName)
            {
                case VMConstants.NewObjectFunction:
                    SimulateNew(builder, instr);
                    return;
                case VMConstants.NewArrayFunction:
                    SimulateArrayInit(builder, instr);
                    return;
                case VMConstants.ThrowFunction:
                    // TODO: do we need to check if this is inside of an expression?
                    output.Add(new ThrowNode(builder.ExpressionStack.Pop()));
                    return;
                case VMConstants.CopyStaticFunction:
                    // Top of stack is function reference to base class (which we ignore), followed by parent call
                    builder.ExpressionStack.Pop();
                    builder.TopFragmentContext!.BaseParentCall = builder.ExpressionStack.Pop();
                    return;
                case VMConstants.FinishFinallyFunction:
                    builder.ExpressionStack.Push(new TryCatchNode.FinishFinallyNode());
                    return;
                case VMConstants.TryUnhookFunction:
                case VMConstants.FinishCatchFunction:
                    // We just ignore this call - no need to even put anything on the stack
                    return;
            }
        }

        // Load all arguments on stack into list
        int numArgs = instr.ArgumentCount;
        List<IExpressionNode> args = new(numArgs);
        for (int j = 0; j < numArgs; j++)
        {
            args.Add(builder.ExpressionStack.Pop());
        }

        builder.ExpressionStack.Push(new FunctionCallNode(instr.Function ?? throw new DecompilerException("Missing function on instruction"), args));
    }

    /// <summary>
    /// Simulates array initialization literals.
    /// </summary>
    private static void SimulateArrayInit(ASTBuilder builder, IGMInstruction instr)
    {
        // Load all arguments on stack into list
        int numArgs = instr.ArgumentCount;
        List<IExpressionNode> elems = new(numArgs);
        for (int j = 0; j < numArgs; j++)
        {
            elems.Add(builder.ExpressionStack.Pop());
        }

        builder.ExpressionStack.Push(new ArrayInitNode(elems));
    }

    /// <summary>
    /// Simulates the "new" keyword, for making new objects.
    /// </summary>
    private static void SimulateNew(ASTBuilder builder, IGMInstruction instr)
    {
        // Load function from first parameter
        IExpressionNode function = builder.ExpressionStack.Pop();

        // Load all arguments on stack into list
        int numArgs = instr.ArgumentCount - 1;
        List<IExpressionNode> args = new(numArgs);
        for (int j = 0; j < numArgs; j++)
        {
            args.Add(builder.ExpressionStack.Pop());
        }

        builder.ExpressionStack.Push(new NewObjectNode(function, args));
    }

    /// <summary>
    /// Simulates a single CallVariable instruction.
    /// </summary>
    private static void SimulateCallVariable(ASTBuilder builder, IGMInstruction instr)
    {
        // Load function/method and the instance to call it on from the stack
        IExpressionNode function = builder.ExpressionStack.Pop();
        IExpressionNode? instance = builder.ExpressionStack.Pop();

        // Load all arguments on stack into list
        int numArgs = instr.ArgumentCount;
        List<IExpressionNode> args = new(numArgs);
        for (int j = 0; j < numArgs; j++)
        {
            args.Add(builder.ExpressionStack.Pop());
        }

        // Prevent needless recursion on the variable's left side
        if (function is VariableNode variable && variable.Left == instance)
        {
            instance = null;
        }

        builder.ExpressionStack.Push(new VariableCallNode(function, instance, args));
    }

    /// <summary>
    /// Simulates a single binary instruction.
    /// </summary>
    private static void SimulateBinary(ASTBuilder builder, IGMInstruction instr)
    {
        IExpressionNode right = builder.ExpressionStack.Pop();
        IExpressionNode left = builder.ExpressionStack.Pop();

        // If either type is a boolean, check if our value is a 16-bit int (0 or 1), and make it a boolean if so
        if (instr.Type1 == DataType.Boolean && right is Int16Node valueRI16 && valueRI16.Value is 0 or 1)
        {
            right = new BooleanNode(valueRI16.Value == 1);
        }
        if (instr.Type2 == DataType.Boolean && left is Int16Node valueLI16 && valueLI16.Value is 0 or 1)
        {
            left = new BooleanNode(valueLI16.Value == 1);
        }

        builder.ExpressionStack.Push(new BinaryNode(left, right, instr));
    }

    /// <summary>
    /// Simulates a single Convert instruction.
    /// </summary>
    private static void SimulateConvert(ASTBuilder builder, IGMInstruction instr)
    {
        IExpressionNode top = builder.ExpressionStack.Peek();

        if (top is Int16Node i16 && i16.Value is 0 or 1)
        {
            // If we convert from integer to boolean, turn into true/false if 1 or 0, respectively
            if (instr is { Type1: DataType.Int32, Type2: DataType.Boolean })
            {
                builder.ExpressionStack.Pop();
                builder.ExpressionStack.Push(new BooleanNode(i16.Value == 1));
                return;
            }
            
            // If we convert from boolean to anything else, and we have an Int16 on the stack,
            // we know that we had a boolean on the stack previously, so change that.
            if (instr is { Type1: DataType.Boolean })
            {
                builder.ExpressionStack.Pop();
                builder.ExpressionStack.Push(new BooleanNode(i16.Value == 1)
                {
                    StackType = instr.Type2
                });
                return;
            }
        }

        // Update type on the top of the stack normally
        top.StackType = instr.Type2;
    }

    /// <summary>
    /// Simulates a single PopDelete instruction.
    /// </summary>
    private static void SimulatePopDelete(ASTBuilder builder, List<IStatementNode> output)
    {
        if (builder.ExpressionStack.Count == 0)
        {
            // Can occasionally occur with early exit cleanup
            return;
        }

        IExpressionNode node = builder.ExpressionStack.Pop();
        if (node.Duplicated || node is VariableNode)
        {
            // Disregard unnecessary expressions
            return;
        }

        if (node is IStatementNode statement)
        {
            // Node is simply a normal statement (often seen with function calls)
            output.Add(statement);
        }
        else
        {
            // This is a free-floating expression, somehow
            // TODO: probably just store this in a temp var instead of throwing this exception?
            throw new DecompilerException("Free-floating expression found");
        }
    }

    /// <summary>
    /// Simulates a single Extended instruction.
    /// </summary>
    private static void SimulateExtended(ASTBuilder builder, List<IStatementNode> output, IGMInstruction instr)
    {
        switch (instr.ExtKind)
        {
            case ExtendedOpcode.SetArrayOwner:
                // We ignore the array owner ID - not important for high-level code decompilation
                builder.ExpressionStack.Pop();
                break;
            case ExtendedOpcode.PushReference:
                SimulatePushReference(builder, instr);
                break;
            case ExtendedOpcode.PushArrayContainer:
            case ExtendedOpcode.PushArrayFinal:
                // For decompilation sake, we treat these two instructions identically (they just push an array index, or a reference to one)
                SimulateMultiArrayPush(builder);
                break;
            case ExtendedOpcode.PopArrayFinal:
                SimulateMultiArrayPop(builder, output);
                break;
        }
    }

    /// <summary>
    /// Simulates a single PushArrayContainer or PushArrayFinal extended opcode instruction.
    /// </summary>
    private static void SimulateMultiArrayPush(ASTBuilder builder)
    {
        IExpressionNode index = builder.ExpressionStack.Pop();
        if (builder.ExpressionStack.Pop() is not VariableNode variable)
        {
            throw new DecompilerException("Expected variable in multi-array push");
        }

        // Make a copy of the variable we already have, with the new index at the end of array indices
        List<IExpressionNode> existingArrayIndices = variable.ArrayIndices ?? throw new DecompilerException("Expected existing array indices");
        VariableNode extendedVariable = new(variable.Variable, variable.ReferenceType, variable.Left, new(existingArrayIndices) { index }, variable.RegularPush);
        builder.ExpressionStack.Push(extendedVariable);
    }

    /// <summary>
    /// Simulates a single PopArrayFinal extended opcode instruction.
    /// </summary>
    private static void SimulateMultiArrayPop(ASTBuilder builder, List<IStatementNode> output)
    {
        IExpressionNode index = builder.ExpressionStack.Pop();
        if (builder.ExpressionStack.Pop() is not VariableNode variable)
        {
            throw new DecompilerException("Expected variable in multi-array pop");
        }

        // Make a copy of the variable we already have, with the new index at the end of array indices
        List<IExpressionNode> existingArrayIndices = variable.ArrayIndices ?? throw new DecompilerException("Expected existing array indices");
        VariableNode extendedVariable = new(variable.Variable, variable.ReferenceType, variable.Left, new(existingArrayIndices) { index }, variable.RegularPush)
        {
            Duplicated = variable.Duplicated
        };

        // Make assignment node with this variable, and the value remaining at the top of the stack
        IExpressionNode value = builder.ExpressionStack.Pop();

        // If we have a binary being assigned, check for prefix/postfix operations
        if (value is BinaryNode binary)
        {
            // Check prefix/postfix if the right value of binary is 1
            if (binary is { Right: Int16Node rightInt16 } && rightInt16.Value == 1)
            {
                if (binary.Duplicated)
                {
                    // Prefix detected
                    // TODO: do we need to verify "binary.Left" is the same as "extendedVariable"?

                    // Pop off duplicate value (should be copy of "binary"), as we don't need it
                    builder.ExpressionStack.Pop();

                    builder.ExpressionStack.Push(new AssignNode(extendedVariable, AssignNode.AssignType.Prefix, binary.Instruction));
                    return;
                }
                if (binary.Left.Duplicated && builder.ExpressionStack.Count > 0 && builder.ExpressionStack.Peek() == binary.Left)
                {
                    // Postfix detected - pop off duplicate value, as we don't 
                    // TODO: do we need to verify "binary.Left" is the same as "extendedVariable"?

                    // Pop off duplicate value (should be copy of "binary.Left"), as we don't need it
                    builder.ExpressionStack.Pop();

                    builder.ExpressionStack.Push(new AssignNode(extendedVariable, AssignNode.AssignType.Postfix, binary.Instruction));
                    return;
                }
            }
        }

        // Just a normal assignment statement - let AST cleanup code determine pre/postfix and compounds
        output.Add(new AssignNode(extendedVariable, value));
    }

    /// <summary>
    /// Simulates a single PushReference extended opcode instruction.
    /// </summary>
    private static void SimulatePushReference(ASTBuilder builder, IGMInstruction instr)
    {
        if (instr.Function is not null)
        {
            // Simply push a function reference.
            // Note that this is *specifically* a Variable data type on the stack, not Int32.
            builder.ExpressionStack.Push(new FunctionReferenceNode(instr.Function)
            {
                StackType = DataType.Variable
            });
        }
        else
        {
            // Simply push reference
            builder.ExpressionStack.Push(new AssetReferenceNode(instr.AssetReferenceId, instr.GetAssetReferenceType(builder.Context.GameContext)));
        }
    }
}
