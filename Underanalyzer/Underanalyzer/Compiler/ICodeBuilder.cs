/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Compiler.Bytecode;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler;

/// <summary>
/// Represents an implementation for building code entries and emitting instructions, during compilation.
/// </summary>
/// <remarks>
/// All methods should be able to operate independently of thread; otherwise, the compiler is no longer thread-safe.
/// </remarks>
public interface ICodeBuilder
{
    /// <summary>
    /// Creates an instruction with an address and an opcode.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode);

    /// <summary>
    /// Creates an instruction with an address, an opcode, and a single data type.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, DataType dataType);

    /// <summary>
    /// Creates an instruction with an address, an opcode, and two data types.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, DataType dataType1, DataType dataType2);

    /// <summary>
    /// Creates an instruction with an address, an opcode, two data types, and a 16-bit integer value.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, short value, DataType dataType1, DataType dataType2);

    /// <summary>
    /// Creates an instruction with an address, an opcode, two data types, and a 32-bit integer value.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, int value, DataType dataType1, DataType dataType2);

    /// <summary>
    /// Creates an instruction with an address, an opcode, two data types, and a 64-bit integer value.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, long value, DataType dataType1, DataType dataType2);

    /// <summary>
    /// Creates an instruction with an address, an opcode, two data types, and a 64-bit floating point value.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, double value, DataType dataType1, DataType dataType2);

    /// <summary>
    /// Creates an instruction with an address, an opcode, two data types, and a comparsion type.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, ComparisonType comparisonType, DataType dataType1, DataType dataType2);

    /// <summary>
    /// Creates an instruction with an address, and an extended opcode.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, ExtendedOpcode extendedOpcode);

    /// <summary>
    /// Creates an instruction with an address, an extended opcode, and integer value.
    /// </summary>
    public IGMInstruction CreateInstruction(int address, ExtendedOpcode extendedOpcode, int value);

    /// <summary>
    /// Creates a duplication instruction with an address, data type, and single duplication size.
    /// </summary>
    /// <remarks>
    /// The instruction will be opcode <see cref="Opcode.Duplicate"/>, and have <see cref="IGMInstruction.DuplicationSize"/> as <paramref name="duplicationSize"/>.
    /// </remarks>
    public IGMInstruction CreateDuplicateInstruction(int address, DataType dataType, byte duplicationSize);

    /// <summary>
    /// Creates a duplication instruction with an address, data type, and two duplication sizes.
    /// </summary>
    /// <remarks>
    /// The instruction will be opcode <see cref="Opcode.Duplicate"/>, and have <see cref="IGMInstruction.DuplicationSize"/> as <paramref name="duplicationSize"/>,
    /// and <see cref="IGMInstruction.DuplicationSize2"/> as <paramref name="duplicationSize2"/>.
    /// </remarks>
    public IGMInstruction CreateDupSwapInstruction(int address, DataType dataType, byte duplicationSize, byte duplicationSize2);

    /// <summary>
    /// Creates a pop swap instruction with an address, and a swap size.
    /// </summary>
    /// <remarks>
    /// The instruction will be opcode <see cref="Opcode.Pop"/>, with data types <see cref="DataType.Int16"/> and <see cref="DataType.Variable"/>,
    /// and <see cref="IGMInstruction.PopSwapSize"/> as <paramref name="swapSize"/>.
    /// </remarks>
    public IGMInstruction CreatePopSwapInstruction(int address, byte swapSize);

    /// <summary>
    /// Creates an instruction with an address, which will exit early from within a with loop.
    /// </summary>
    /// <remarks>
    /// The instruction will be opcode <see cref="Opcode.PopWithContext"/>, and have <see cref="IGMInstruction.PopWithContextExit"/> as <see langword="true"/>.
    /// </remarks>
    public IGMInstruction CreateWithExitInstruction(int address);

    /// <summary>
    /// Creates a call instruction with an address and an argument count.
    /// </summary>
    /// <remarks>
    /// The instruction will be opcode <see cref="Opcode.Call"/>, with data type 1 being <see cref="DataType.Int32"/>.
    /// </remarks>
    public IGMInstruction CreateCallInstruction(int address, int argumentCount);

    /// <summary>
    /// Creates a variable call instruction with an address and an argument count.
    /// </summary>
    /// <remarks>
    /// The instruction will be opcode <see cref="Opcode.CallVariable"/>, with data type 1 being <see cref="DataType.Variable"/>.
    /// </remarks>
    public IGMInstruction CreateCallVariableInstruction(int address, int argumentCount);

    /// <summary>
    /// Patches an existing instruction with a variable reference.
    /// </summary>
    public void PatchInstruction(IGMInstruction instruction, string variableName, InstanceType variableInstanceType, InstanceType instructionInstanceType, VariableType variableType, bool isBuiltin, bool keepInstanceType);

    /// <summary>
    /// Patches an existing instruction with a function reference.
    /// </summary>
    public void PatchInstruction(IGMInstruction instruction, FunctionScope scope, string functionName, IBuiltinFunction? builtinFunction);

    /// <summary>
    /// Patches an existing instruction with a function reference, from a specific local function entry.
    /// </summary>
    public void PatchInstruction(IGMInstruction instruction, FunctionEntry functionEntry);

    /// <summary>
    /// Patches an existing instruction with a string reference.
    /// </summary>
    public void PatchInstruction(IGMInstruction instruction, string stringContent);

    /// <summary>
    /// Patches an existing instruction with an integer value (which is a branch offset for branch instructions).
    /// </summary>
    public void PatchInstruction(IGMInstruction instruction, int value);

    /// <summary>
    /// Returns whether a global function name of any kind exists with the given name.
    /// </summary>
    /// <remarks>
    /// This should return true if the function either already exists, or can be created during (or before) instruction patching.
    /// </remarks>
    public bool IsGlobalFunctionName(string name);

    /// <summary>
    /// Generates an ID to be used for local variable names used by a try statement.
    /// </summary>
    /// <remarks>
    /// These IDs should be non-negative and unique. The supplied <paramref name="internalIndex"/> 
    /// (guaranteed to be unique per each try statement being compiled by a single context) 
    /// may be returned directly as well, if desired.
    /// </remarks>
    public int GenerateTryVariableID(int internalIndex);

    /// <summary>
    /// Generates an array owner ID, given a variable name (if available), function index, and whether the variable is a dot variable.
    /// </summary>
    /// <param name="variableName">Variable name to be used for generating an array owner ID, or <see langword="null"/> if no name is available.</param>
    /// <param name="functionIndex">Function ID to be used for generating an array owner ID.</param>
    /// <param name="isDot">Whether the variable name was used on the right side of a dot.</param>
    /// <returns>Array owner ID. Note that values outside of unsigned 31-bit integer range will wrap around at runtime.</returns>
    public long GenerateArrayOwnerID(string? variableName, long functionIndex, bool isDot);

    /// <summary>
    /// Called whenever the parser encounters a name identifier (i.e., not a keyword, function, constant, or asset).
    /// </summary>
    public void OnParseNameIdentifier(string name);
}
