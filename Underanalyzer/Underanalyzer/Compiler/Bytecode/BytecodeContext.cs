/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Compiler.Nodes;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Bytecode;

/// <summary>
/// A context for bytecode generation, for a single code entry.
/// </summary>
internal sealed class BytecodeContext : ISubCompileContext
{
    /// <inheritdoc/>
    public CompileContext CompileContext { get; }

    /// <inheritdoc/>
    public FunctionScope CurrentScope { get; set; }

    /// <inheritdoc/>
    public FunctionScope RootScope { get; set; }

    /// <summary>
    /// Root node used during bytecode generation.
    /// </summary>
    public IASTNode RootNode { get; }

    /// <summary>
    /// Current list of written instructions, in order by address.
    /// </summary>
    public List<IGMInstruction> Instructions { get; } = new(64);

    /// <summary>
    /// Current list of function entries, in order by address.
    /// </summary>
    public List<FunctionEntry> FunctionEntries { get; } = new(4);

    /// <summary>
    /// Current function entry being generated, or <see langword="null"/> if none exists.
    /// </summary>
    public FunctionEntry? CurrentFunctionEntry { get; set; }

    /// <summary>
    /// List of instruction patches generated during code generation.
    /// </summary>
    public InstructionPatches Patches { get; } = InstructionPatches.Create();

    /// <summary>
    /// Current instruction writing position.
    /// </summary>
    public int Position { get; private set; } = 0;

    /// <summary>
    /// Function to call before any exit/return instructions, as part of their cleanup.
    /// </summary>
    /// <remarks>
    /// Yes, due to not being attached to function scopes, this results in bugged code generation, 
    /// but this mimics official GML compiler behavior.
    /// </remarks>
    public string? FunctionCallBeforeExit { get; set; } = null;

    /// <summary>
    /// When <see cref="IGameContext.UsingArrayCopyOnWrite"/> is <see langword="true"/>, this is updated to
    /// the last function ID generated for a function declaration.
    /// </summary>
    public long LastFunctionID { get; set; } = 1;

    /// <summary>
    /// When <see cref="IGameContext.UsingArrayCopyOnWrite"/> is <see langword="true"/>, this is updated to
    /// the last array owner ID generated using <see cref="ArrayOwners.GenerateSetArrayOwner(BytecodeContext, IASTNode)"/>,
    /// or -1 if the last ID was invalidated by control flow.
    /// </summary>
    public long LastArrayOwnerID { get; set; } = -1;

    /// <summary>
    /// When <see cref="IGameContext.UsingArrayCopyOnWrite"/> is <see langword="true"/>, this is updated to
    /// reflect whether array owner IDs can currently be generated in the tree.
    /// </summary>
    public bool CanGenerateArrayOwners { get; set; } = false;

    // Stack used for storing data types as on the VM data stack.
    private readonly Stack<DataType> _dataTypeStack = new(16);

    // Code builder used for creating instructions, modifying them, and creating code entries.
    private readonly ICodeBuilder _codeBuilder;

    // Reference to the game context for quick access.
    private readonly IGameContext _gameContext;

    public BytecodeContext(CompileContext context, IASTNode rootNode, FunctionScope rootScope, HashSet<string>? localGlobalFunctions)
    {
        CompileContext = context;
        RootNode = rootNode;
        CurrentScope = rootScope;
        RootScope = rootScope;

        rootScope.ControlFlowContexts = new(8);
        _gameContext = context.GameContext;
        _codeBuilder = _gameContext.CodeBuilder;
    }

    /// <summary>
    /// Performs bytecode generation for a full code entry, from the root.
    /// </summary>
    public void GenerateCode(int initialPosition)
    {
        Position = initialPosition;
        CanGenerateArrayOwners = _gameContext.UsingArrayCopyOnWrite;
        RootNode.GenerateCode(this);

#if DEBUG
        if (_dataTypeStack.Count > 0)
        {
            throw new Exception("Data type stack not cleared by end of code generation");
        }
#endif
    }

    /// <summary>
    /// Performs post-processing on generated code, resolving references and patches.
    /// </summary>
    public static void PatchInstructions(CompileContext context, InstructionPatches patches)
    {
        ICodeBuilder codeBuilder = context.GameContext.CodeBuilder;

        // Resolve variable patches
        foreach (VariablePatch variablePatch in patches.VariablePatches!)
        {
            codeBuilder.PatchInstruction(variablePatch.Instruction!, variablePatch.Name, variablePatch.InstanceType, variablePatch.InstructionInstanceType, 
                                         variablePatch.VariableType, variablePatch.IsBuiltin, variablePatch.KeepInstanceType);
        }

        // Resolve function patches
        foreach (FunctionPatch functionPatch in patches.FunctionPatches!)
        {
            codeBuilder.PatchInstruction(functionPatch.Instruction!, functionPatch.Scope, functionPatch.Name, functionPatch.BuiltinFunction);
        }

        // Resolve local function patches
        foreach (LocalFunctionPatch functionPatch in patches.LocalFunctionPatches!)
        {
            FunctionEntry entry;
            if (functionPatch.FunctionEntry is not null)
            {
                entry = functionPatch.FunctionEntry;
            }
            else if (functionPatch.FunctionScope!.TryGetDeclaredFunction(context.GameContext, functionPatch.FunctionName!, out FunctionEntry? found))
            {
                entry = found;
            }
            else
            {
                throw new CompilerException($"Failed to resolve local function with name \"{functionPatch.FunctionName}\"");
            }
            codeBuilder.PatchInstruction(functionPatch.Instruction!, entry);
        }

        // Resolve struct variable patches
        foreach (StructVariablePatch variablePatch in patches.StructVariablePatches!)
        {
            codeBuilder.PatchInstruction(variablePatch.Instruction!, variablePatch.FunctionEntry.StructName ?? throw new InvalidOperationException("Struct name not resolved on function entry"), 
                                         variablePatch.InstanceType, variablePatch.InstructionInstanceType, variablePatch.VariableType, false, true);
        }

        // Resolve string patches
        foreach (StringPatch stringPatch in patches.StringPatches!)
        {
            codeBuilder.PatchInstruction(stringPatch.Instruction!, stringPatch.Content);
        }
    }

    /// <summary>
    /// Emits an instruction with the given opcode, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits a single-type instruction with the given opcode and data type, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, DataType dataType)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, dataType);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits a double-type instruction with the given opcode and data types, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, DataType dataType1, DataType dataType2)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given opcode, data types, and 16-bit integer, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, short value, DataType dataType1, DataType dataType2 = DataType.Double)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, value, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given opcode, data types, and 32-bit integer, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, int value, DataType dataType1, DataType dataType2 = DataType.Double)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, value, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 8;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given opcode, data types, and 64-bit integer, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, long value, DataType dataType1, DataType dataType2 = DataType.Double)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, value, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 12;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given opcode, data types, and 64-bit floating point number, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, double value, DataType dataType1, DataType dataType2 = DataType.Double)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, value, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 12;
        return instr;
    }

    /// <summary>
    /// Emits a double-type instruction with the given opcode, comparison type, and data types, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, ComparisonType comparisonType, DataType dataType1, DataType dataType2)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, comparisonType, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given extended opcode, at the current position.
    /// </summary>
    public IGMInstruction Emit(ExtendedOpcode extendedOpcode)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, extendedOpcode);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given extended opcode and local function, at the current position.
    /// </summary>
    public IGMInstruction Emit(ExtendedOpcode extendedOpcode, LocalFunctionPatch function)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, extendedOpcode, 0);
        Instructions.Add(instr);
        Position += 8;

        function.Instruction = instr;
        Patches.LocalFunctionPatches!.Add(function);

        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given extended opcode and function, at the current position.
    /// </summary>
    public IGMInstruction Emit(ExtendedOpcode extendedOpcode, FunctionPatch function)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, extendedOpcode, 0);
        Instructions.Add(instr);
        Position += 8;

        function.Instruction = instr;
        Patches.FunctionPatches!.Add(function);

        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given extended opcode and integer value, at the current position.
    /// </summary>
    public IGMInstruction Emit(ExtendedOpcode extendedOpcode, int extendedValue)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, extendedOpcode, extendedValue);
        Instructions.Add(instr);
        Position += 8;
        return instr;
    }

    /// <summary>
    /// Emits a dulication instruction with the given data type and duplication size, at the current position.
    /// </summary>
    public IGMInstruction EmitDuplicate(DataType dataType, byte duplicationSize)
    {
        IGMInstruction instr = _codeBuilder.CreateDuplicateInstruction(Position, dataType, duplicationSize);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits a dulication instruction with the given data type and duplication sizes, at the current position.
    /// </summary>
    public IGMInstruction EmitDupSwap(DataType dataType, byte duplicationSize, byte duplicationSize2)
    {
        IGMInstruction instr = _codeBuilder.CreateDupSwapInstruction(Position, dataType, duplicationSize, duplicationSize2);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits a pop swap instruction with the given swap size, at the current position.
    /// </summary>
    public IGMInstruction EmitPopSwap(byte swapSize)
    {
        IGMInstruction instr = _codeBuilder.CreatePopSwapInstruction(Position, swapSize);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with opcode <see cref="Opcode.PopWithContext"/>, and <see cref="IGMInstruction.PopWithContextExit"/> as <see langword="true"/>.
    /// </summary>
    public IGMInstruction EmitPopWithExit()
    {
        IGMInstruction instr = _codeBuilder.CreateWithExitInstruction(Position);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given opcode, data types, and given variable, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, VariablePatch variable, DataType dataType1, DataType dataType2 = DataType.Double)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 8;

        variable.Instruction = instr;
        Patches.VariablePatches!.Add(variable);

        // If this is a local variable, declare it if not already done.
        // This is done at this stage to match the order produced by the official compiler.
        if (variable.InstanceType == InstanceType.Local)
        {
            // Add to current scope's locals
            CurrentScope.DeclareLocal(variable.Name);
        }

        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given opcode, data types, and given struct variable, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, StructVariablePatch variable, DataType dataType1, DataType dataType2 = DataType.Double)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 8;

        variable.Instruction = instr;
        Patches.StructVariablePatches!.Add(variable);

        return instr;
    }

    /// <summary>
    /// Emits a <see cref="Opcode.Push"/> instruction with the given function, at the current position.
    /// </summary>
    public IGMInstruction EmitPushFunction(FunctionPatch function)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, Opcode.Push, DataType.Int32);
        Instructions.Add(instr);
        Position += 8;

        function.Instruction = instr;
        Patches.FunctionPatches!.Add(function);

        return instr;
    }

    /// <summary>
    /// Emits a <see cref="Opcode.Push"/> instruction with the given local function, at the current position.
    /// </summary>
    public IGMInstruction EmitPushFunction(LocalFunctionPatch function)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, Opcode.Push, DataType.Int32);
        Instructions.Add(instr);
        Position += 8;

        function.Instruction = instr;
        Patches.LocalFunctionPatches!.Add(function);

        return instr;
    }

    /// <summary>
    /// Emits a <see cref="Opcode.Call"/> instruction with the given argument count, and given function, at the current position.
    /// </summary>
    public IGMInstruction EmitCall(FunctionPatch function, int argumentCount)
    {
        IGMInstruction instr = _codeBuilder.CreateCallInstruction(Position, argumentCount);
        Instructions.Add(instr);
        Position += 8;

        function.Instruction = instr;
        Patches.FunctionPatches!.Add(function);

        return instr;
    }

    /// <summary>
    /// Emits a <see cref="Opcode.CallVariable"/> instruction with the given argument count, at the current position.
    /// </summary>
    public IGMInstruction EmitCallVariable(int argumentCount)
    {
        IGMInstruction instr = _codeBuilder.CreateCallVariableInstruction(Position, argumentCount);
        Instructions.Add(instr);
        Position += 4;
        return instr;
    }

    /// <summary>
    /// Emits an instruction with the given opcode, data types, and given string, at the current position.
    /// </summary>
    public IGMInstruction Emit(Opcode opcode, StringPatch stringPatch, DataType dataType1, DataType dataType2 = DataType.Double)
    {
        IGMInstruction instr = _codeBuilder.CreateInstruction(Position, opcode, dataType1, dataType2);
        Instructions.Add(instr);
        Position += 8;

        stringPatch.Instruction = instr;
        Patches.StringPatches!.Add(stringPatch);

        return instr;
    }

    /// <summary>
    /// Patches a single instruction with the given branch offset.
    /// </summary>
    public void PatchBranch(IGMInstruction instruction, int branchOffset)
    {
        _codeBuilder.PatchInstruction(instruction, branchOffset);
    }

    /// <summary>
    /// Patches a single push integer instruction with the given integer value.
    /// </summary>
    public void PatchPush(IGMInstruction instruction, int value)
    {
        _codeBuilder.PatchInstruction(instruction, value);
    }

    /// <summary>
    /// Pushes a data type to the data type stack.
    /// </summary>
    public void PushDataType(DataType dataType)
    {
        _dataTypeStack.Push(dataType);
    }

    /// <summary>
    /// Peeks the top data type from the data type stack.
    /// </summary>
    public DataType PeekDataType()
    {
        return _dataTypeStack.Peek();
    }

    /// <summary>
    /// Pops a data type from the data type stack.
    /// </summary>
    public DataType PopDataType()
    {
        return _dataTypeStack.Pop();
    }

    /// <summary>
    /// Emits a <see cref="Opcode.Convert"/> instruction from the current type at the 
    /// top of the data type stack, to the destination data type.
    /// </summary>
    /// <remarks>Pops the data type at the top of the stack, and does not push anything back.</remarks>
    /// <returns><see langword="true"/> if a conversion was emitted; <see langword="false"/> otherwise.</returns>
    public bool ConvertDataType(DataType destDataType)
    {
        // Pop data type from top of stack
        DataType srcDataType = _dataTypeStack.Pop();

        // Emit convert instruction if data type is different; otherwise do nothing
        if (srcDataType != destDataType)
        {
            Emit(Opcode.Convert, srcDataType, destDataType);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Possible instance conversion types. See <see cref="ConvertToInstanceId"/>.
    /// </summary>
    public enum InstanceConversionType
    {
        /// <summary>
        /// No conversion performed.
        /// </summary>
        None,

        /// <summary>
        /// Conversion directly to int32 performed.
        /// </summary>
        Int32,

        /// <summary>
        /// Magic stacktop ID used (<see cref="InstanceType.StackTop"/>).
        /// </summary>
        StacktopId
    }

    /// <summary>
    /// Converts the data type on the top of the stack to an instance ID, depending on GameMaker version.
    /// </summary>
    /// <remarks>
    /// Pops the top data type from the stack.
    /// </remarks>
    /// <returns>
    /// <see cref="InstanceConversionType"/> enumeration representing what conversion was performed.
    /// </returns>
    public InstanceConversionType ConvertToInstanceId()
    {
        // If data type isn't an integer, convert to one
        DataType dataType = PopDataType();
        if (dataType != DataType.Int32)
        {
            if (dataType == DataType.Variable && CompileContext.GameContext.UsingGMLv2)
            {
                // In GMLv2, use magic stacktop integer to reference variable types
                Emit(Opcode.PushImmediate, (short)InstanceType.StackTop, DataType.Int16);
                return InstanceConversionType.StacktopId;
            }

            // Otherwise, if either not GMLv2, or type is not a variable type, perform direct conversion
            Emit(Opcode.Convert, dataType, DataType.Int32);
            return InstanceConversionType.Int32;
        }

        // No conversion was performed
        return InstanceConversionType.None;
    }

    /// <summary>
    /// Returns whether any control flow contexts require cleanup currently, for early exits.
    /// </summary>
    public bool DoAnyControlFlowRequireCleanup()
    {
        foreach (IControlFlowContext context in CurrentScope.ControlFlowContexts!)
        {
            if (context.RequiresCleanup)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Generates control flow cleanup code.
    /// </summary>
    public void GenerateControlFlowCleanup()
    {
        foreach (IControlFlowContext context in CurrentScope.ControlFlowContexts!)
        {
            if (context.RequiresCleanup)
            {
                context.GenerateCleanupCode(this);
            }
        }
    }

    /// <summary>
    /// Returns whether the given name is a global function name of any kind.
    /// </summary>
    public bool IsGlobalFunctionName(string name)
    {
        // Check builtin functions
        if (_gameContext.Builtins.LookupBuiltinFunction(name) is not null)
        {
            return true;
        }

        // Check script assets
        if (_gameContext.GetScriptId(name, out int _))
        {
            return true;
        }

        // Do a general global function lookup (depending on ICodeBuilder's implementation)
        return _codeBuilder.IsGlobalFunctionName(name);
    }

    /// <summary>
    /// Pushes a control flow context onto the control flow context stack.
    /// </summary>
    public void PushControlFlowContext(IControlFlowContext context)
    {
        CurrentScope.ControlFlowContexts!.Push(context);
    }

    /// <summary>
    /// Pops a control flow context from the control flow context stack.
    /// </summary>
    public void PopControlFlowContext()
    {
        CurrentScope.ControlFlowContexts!.Pop();
    }

    /// <summary>
    /// Returns whether there are any control flow contexts on the control flow context stack.
    /// </summary>
    public bool AnyControlFlowContexts()
    {
        return CurrentScope.ControlFlowContexts!.Count > 0;
    }

    /// <summary>
    /// Returns whether there are any loop contexts on the control flow context stack.
    /// </summary>
    public bool AnyLoopContexts()
    {
        foreach (IControlFlowContext context in CurrentScope.ControlFlowContexts!)
        {
            if (context.IsLoop)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the top control flow context from the control flow context stack.
    /// </summary>
    public IControlFlowContext GetTopControlFlowContext()
    {
        return CurrentScope.ControlFlowContexts!.Peek();
    }

    /// <summary>
    /// Returns whether a function is declared in the current scope, depending on script type and GameMaker version.
    /// </summary>
    public bool IsFunctionDeclaredInCurrentScope(string name)
    {
        switch (CompileContext.ScriptKind)
        {
            case CompileScriptKind.GlobalScript:
            case CompileScriptKind.RoomCreationCode:
                // Global scripts and room creation code have foresight of future function declarations in the script
                return CurrentScope.IsFunctionDeclared(CompileContext.GameContext, name);

            case CompileScriptKind.ObjectEvent:
                // Object events only have foresight of future functions in certain versions
                if (CompileContext.GameContext.UsingObjectFunctionForesight)
                {
                    return CurrentScope.IsFunctionDeclared(CompileContext.GameContext, name);
                }

                // No foresight; attempt to retrieve function entry
                return CurrentScope.TryGetDeclaredFunction(CompileContext.GameContext, name, out _);

            default:
                // No foresight; attempt to retrieve function entry
                return CurrentScope.TryGetDeclaredFunction(CompileContext.GameContext, name, out _);
        }
    }

    /// <summary>
    /// Generates and returns an array owner ID using the code builder associated with this bytecode context.
    /// </summary>
    public long GenerateArrayOwnerID(string? variableName, long functionId, bool isDot)
    {
        return _codeBuilder.GenerateArrayOwnerID(variableName, functionId, isDot);
    }
}
