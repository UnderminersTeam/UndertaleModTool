/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

namespace Underanalyzer;

/// <summary>
/// Represents a single code entry, as seen in a game's data file.
/// </summary>
public interface IGMCode
{
    /// <summary>
    /// Name of the code entry.
    /// </summary>
    public IGMString Name { get; }

    /// <summary>
    /// Length of the code entry's VM instructions, in bytes.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets an instruction at the specified index, in this code entry.
    /// </summary>
    public IGMInstruction GetInstruction(int index);

    /// <summary>
    /// Returns the number of instructions in this code entry.
    /// </summary>
    public int InstructionCount { get; }

    /// <summary>
    /// Returns the offset within the instructions (in bytes) from which this code entry begins.
    /// </summary>
    public int StartOffset { get; }

    /// <summary>
    /// Parent code entry, if this is a sub-function entry. Otherwise, if a root code entry, this is <see langword="null"/>.
    /// </summary>
    public IGMCode? Parent { get; }

    /// <summary>
    /// Gets a child code entry at the specified index.
    /// </summary>
    public IGMCode GetChild(int index);

    /// <summary>
    /// Returns the number of children of this code entry. If this is a sub-function entry, this is <c>0</c>.
    /// </summary>
    public int ChildCount { get; }

    /// <summary>
    /// The number of arguments this code entry takes in. Expected to be <c>0</c> for root entries.
    /// </summary>
    public int ArgumentCount { get; }

    /// <summary>
    /// The number of local variables this code entry uses. Root entries tend to include an additional 1 for <c>arguments</c>.
    /// </summary>
    public int LocalCount { get; }
}

/// <summary>
/// Represents a single instruction within a code entry.
/// </summary>
public interface IGMInstruction
{
    /// <summary>
    /// Mnemonic attribute used for opcodes.
    /// </summary>
    /// <param name="mnemonic">A unique shorthand identifier.</param>
    [AttributeUsage(AttributeTargets.Field)]
    public class OpcodeInfo(string mnemonic) : Attribute
    {
        /// <summary>
        /// Unique shorthand identifier used for this opcode.
        /// </summary>
        public string Mnemonic { get; } = mnemonic;
    }

    /// <summary>
    /// Mnemonic attribute used for VM data types.
    /// </summary>
    /// <param name="mnemonic">A unique character to represent this type.</param>
    /// <param name="size">How many bytes the type takes on the VM stack.</param>
    [AttributeUsage(AttributeTargets.Field)]
    public class DataTypeInfo(char mnemonic, int size) : Attribute
    {
        /// <summary>
        /// Unique character used to represent this data type.
        /// </summary>
        public char Mnemonic { get; } = mnemonic;

        /// <summary>
        /// Size in bytes taken on the VM stack.
        /// </summary>
        public int Size { get; } = size;
    }

    /// <summary>
    /// The opcode types used by an instruction.
    /// </summary>
    public enum Opcode : byte
    {
        /// <summary>
        /// Converts the top of the stack from one type to another.
        /// Mnemonic: "conv"
        /// </summary>
        [OpcodeInfo("conv")]
        Convert = 0x07,

        /// <summary>
        /// Pops two values from the stack, multiplies them, and pushes the result.
        /// Mnemonic: "mul"
        /// </summary>
        [OpcodeInfo("mul")]
        Multiply = 0x08,

        /// <summary>
        /// Pops two values from the stack, divides them, and pushes the result.
        /// Mnemonic: "div"
        /// </summary>
        /// <remarks>The second popped value is divided by the first popped value.</remarks>
        [OpcodeInfo("div")]
        Divide = 0x09,

        /// <summary>
        /// Pops two values from the stack, performs a GML "div" operation (division with remainder), and pushes the result.
        /// Mnemonic: "rem"
        /// </summary>
        /// <remarks>The second popped value is divided (with remainder) by the first popped value.</remarks>
        [OpcodeInfo("rem")]
        GMLDivRemainder = 0x0A,

        /// <summary>
        /// Pops two values from the stack, performs a GML "mod"/% operation, and pushes the result.
        /// Mnemonic: "mod"
        /// </summary>
        /// <remarks>The second popped value is modulo'd against the first popped value.</remarks>
        [OpcodeInfo("mod")]
        GMLModulo = 0x0B,

        /// <summary>
        /// Pops two values from the stack, adds them, and pushes the result.
        /// Mnemonic: "add"
        /// </summary>
        [OpcodeInfo("add")]
        Add = 0x0C,

        /// <summary>
        /// Pops two values from the stack, subtracts them, and pushes the result.
        /// Mnemonic: "sub"
        /// </summary>
        /// <remarks>The second popped value is subtracted by the first popped value.</remarks>
        [OpcodeInfo("sub")]
        Subtract = 0x0D,

        /// <summary>
        /// Pops two values from the stack, performs an AND operation, and pushes the result.
        /// This can be done bitwise or logically.
        /// Mnemonic: "and"
        /// </summary>
        [OpcodeInfo("and")]
        And = 0x0E,

        /// <summary>
        /// Pops two values from the stack, performs an OR operation, and pushes the result.
        /// This can be done bitwise or logically.
        /// Mnemonic: "or"
        /// </summary>
        [OpcodeInfo("or")]
        Or = 0x0F,

        /// <summary>
        /// Pops two values from the stack, performs an XOR operation, and pushes the result.
        /// This can be done bitwise or logically.
        /// Mnemonic: "xor"
        /// </summary>
        [OpcodeInfo("xor")]
        Xor = 0x10,

        /// <summary>
        /// Negates the top value of the stack.
        /// Mnemonic: "neg"
        /// </summary>
        [OpcodeInfo("neg")]
        Negate = 0x11,

        /// <summary>
        /// Performs a boolean or bitwise NOT operation on the top value of the stack (modifying it).
        /// Mnemonic: "not"
        /// </summary>
        [OpcodeInfo("not")]
        Not = 0x12,

        /// <summary>
        /// Pops two values from the stack, performs a bitwise left shift operation ( <c>&lt;&lt;</c> ), and pushes the result.
        /// Mnemonic: "shl"
        /// </summary>
        /// <remarks>The second popped value is shifted left by the first popped value.</remarks>
        [OpcodeInfo("shl")]
        ShiftLeft = 0x13,

        /// <summary>
        /// Pops two values from the stack, performs a bitwise right shift operation ( <c>&gt;&gt;</c> ), and pushes the result.
        /// Mnemonic: "shr"
        /// </summary>
        /// <remarks>The second popped value is shifted right by the first popped value.</remarks>
        [OpcodeInfo("shr")]
        ShiftRight = 0x14,

        /// <summary>
        /// Pops two values from the stack, compares them using a <see cref="ComparisonType"/>, and pushes a boolean result.
        /// Mnemonic: "set", and unofficially "cmp"
        /// </summary>
        [OpcodeInfo("cmp")]
        Compare = 0x15,

        /// <summary>
        /// Pops a value from the stack, and generally stores it in a variable, array, or otherwise.
        /// Has an alternate mode that can swap values around on the stack.
        /// Mnemonic: "pop"
        /// </summary>
        [OpcodeInfo("pop")]
        Pop = 0x45,

        /// <summary>
        /// Duplicates values on the stack, or swaps them around ("dup swap" mode).
        /// Behavior depends on instruction parameters, both in data sizes and mode.
        /// Mnemonic: "dup"
        /// </summary>
        [OpcodeInfo("dup")]
        Duplicate = 0x86,

        /// <summary>
        /// Pops a value from the stack, and returns from the current function/script with that value as the return value.
        /// Mnemonic: "ret"
        /// </summary>
        [OpcodeInfo("ret")]
        Return = 0x9C,

        /// <summary>
        /// Returns from the current function/script/event with no return value.
        /// Mnemonic: "exit"
        /// </summary>
        [OpcodeInfo("exit")]
        Exit = 0x9D,

        /// <summary>
        /// Pops a value from the stack, and discards it.
        /// Mnemonic: "popz"
        /// </summary>
        [OpcodeInfo("popz")]
        PopDelete = 0x9E,

        /// <summary>
        /// Branches to another instruction in the code entry.
        /// Mnemonic: "b"
        /// </summary>
        [OpcodeInfo("b")]
        Branch = 0xB6,

        /// <summary>
        /// Pops a boolean/int32 value from the stack. If <see langword="true"/>/nonzero, branches to another instruction in the code entry.
        /// Mnemonic: "bt"
        /// </summary>
        [OpcodeInfo("bt")]
        BranchTrue = 0xB7,

        /// <summary>
        /// Pops a boolean/int32 value from the stack. If <see langword="false"/>/zero, branches to another instruction in the code entry.
        /// Mnemonic: "bf"
        /// </summary>
        [OpcodeInfo("bf")]
        BranchFalse = 0xB8,

        /// <summary>
        /// Pushes a "with" context, used for GML "with" statements, to the VM environment/self instance stack.
        /// Mnemonic: "pushenv"
        /// </summary>
        [OpcodeInfo("pushenv")]
        PushWithContext = 0xBA,

        /// <summary>
        /// Pops/ends a "with" context, used for GML "with" statements, from the VM environment/self instance stack.
        /// This instruction will branch to its encoded address until no longer iterating instances, where the context will finally be gone for good.
        /// If a flag is encoded in this instruction, then this will always terminate the loop, and branch to the encoded address.
        /// Mnemonic: "popenv"
        /// </summary>
        [OpcodeInfo("popenv")]
        PopWithContext = 0xBB,

        /// <summary>
        /// Pushes a constant value onto the stack. Can vary in size depending on value type.
        /// Mnemonic: "push"
        /// </summary>
        [OpcodeInfo("push")]
        Push = 0xC0,

        /// <summary>
        /// Pushes a value stored in a local variable onto the stack.
        /// Mnemonic: "pushl", or unofficially "pushloc"
        /// </summary>
        [OpcodeInfo("pushloc")]
        PushLocal = 0xC1,

        /// <summary>
        /// Pushes a value stored in a global variable onto the stack.
        /// Mnemonic: "pushg", or unofficially "pushglb"
        /// </summary>
        [OpcodeInfo("pushglb")]
        PushGlobal = 0xC2,

        /// <summary>
        /// Pushes a value stored in a GameMaker builtin variable onto the stack.
        /// Mnemonic: "pushb", or unofficially "pushbltn"
        /// </summary>
        [OpcodeInfo("pushbltn")]
        PushBuiltin = 0xC3,

        /// <summary>
        /// Pushes an immediate signed 32-bit integer value onto the stack, encoded as a signed 16-bit integer.
        /// Mnemonic: "pushi"
        /// </summary>
        [OpcodeInfo("pushi")]
        PushImmediate = 0x84,

        /// <summary>
        /// Calls a GML script/function, using its ID. Arguments are prepared prior to this instruction, in reverse order.
        /// Argument count is encoded in this instruction. Arguments are popped off of the stack.
        /// Mnemonic: "call"
        /// </summary>
        [OpcodeInfo("call")]
        Call = 0xD9,

        /// <summary>
        /// Pops two values off of the stack, and then calls a GML script/function using those values, representing
        /// the "self" instance to be used when calling, as well as the reference to the function being called. 
        /// Arguments are dealt with identically to "call".
        /// Mnemonic: "call.v" (an exception to normal rules!), or unofficially "callv"
        /// </summary>
        [OpcodeInfo("callv")]
        CallVariable = 0x99,

        /// <summary>
        /// Performs extended operations that are detailed in the <see cref="ExtendedOpcode"/> enum.
        /// Often referred to as "break", but there are multiple mnemonics for this opcode.
        /// </summary>
        [OpcodeInfo("break")]
        Extended = 0xFF
    }

    /// <summary>
    /// Represents multiple extended opcodes used by instructions with the Extended opcode.
    /// </summary>
    public enum ExtendedOpcode : short
    {
        /// <summary>
        /// Verifies an array index is within proper bounds, typically for multi-dimensional arrays.
        /// Mnemonic: "chkindex"
        /// </summary>
        [OpcodeInfo("chkindex")]
        CheckArrayIndex = -1,

        /// <summary>
        /// Pops two values from the stack, those being an index and an array reference.
        /// Then, pushes the value stored at the passed-in array at the desired index.
        /// That is, this is used only with multi-dimensional arrays, for the final/last index operation.
        /// Mnemonic: "pushaf"
        /// </summary>
        [OpcodeInfo("pushaf")]
        PushArrayFinal = -2,

        /// <summary>
        /// Pops three values from the stack, those being an index, an array reference, and a value.
        /// Then, assigns the value to the array at the specified index.
        /// Mnemonic: "popaf"
        /// </summary>
        [OpcodeInfo("popaf")]
        PopArrayFinal = -3,

        /// <summary>
        /// Pops two values from the stack, those being an array reference and an index.
        /// Then, pushes a new array reference from the passed-in array at the desired index, with the expectation that it will be further indexed into.
        /// That is, this is used only with multi-dimensional arrays, for all index operations from the second through the second to last.
        /// Mnemonic: "pushac"
        /// </summary>
        [OpcodeInfo("pushac")]
        PushArrayContainer = -4,

        /// <summary>
        /// Sets a global variable in the VM (popped from stack), designated for tracking the now-deprecated array copy-on-write functionality in GML.
        /// The value used is specific to certain locations in scripts. When array copy-on-write functionality is disabled, this
        /// extended opcode is not used.
        /// Mnemonic: "setowner"
        /// </summary>
        [OpcodeInfo("setowner")]
        SetArrayOwner = -5,

        /// <summary>
        /// Pushes a boolean value to the stack, indicating whether static initialization has already occurred for this function (true), or otherwise false.
        /// Mnemonic: "isstaticok"
        /// </summary>
        [OpcodeInfo("isstaticok")]
        HasStaticInitialized = -6,

        /// <summary>
        /// Marks the current function to no longer be able to enter its own static initialization.
        /// Mnemonic: "setstatic"
        /// </summary>
        /// <remarks>
        /// This can either occur at the beginning or end of a static block, depending on whether "AllowReentrantStatic" is enabled by a 
        /// game's developer (enabled by default before GameMaker 2024.11; disabled by default otherwise).
        /// </remarks>
        [OpcodeInfo("setstatic")]
        SetStaticInitialized = -7,

        /// <summary>
        /// Keeps track of an array reference temporarily. Used in multi-dimensional array compound assignment statements.
        /// Presumed to be used for garbage collection purposes.
        /// Mnemonic: "savearef"
        /// </summary>
        [OpcodeInfo("savearef")]
        SaveArrayReference = -8,

        /// <summary>
        /// Restores a previously-tracked array reference. Used in multi-dimensional array compound assignment statements.
        /// Presumed to be used for garbage collection purposes.
        /// Mnemonic: "restorearef"
        /// </summary>
        [OpcodeInfo("restorearef")]
        RestoreArrayReference = -9,

        /// <summary>
        /// Pops a value from the stack, and pushes a boolean result. The result is true if a "nullish" value, such as undefined or GML's pointer_null.
        /// Mnemonic: "isnullish"
        /// </summary>
        [OpcodeInfo("isnullish")]
        IsNullishValue = -10,

        /// <summary>
        /// Pushes an asset reference to the stack, encoded in an integer. Includes asset type and index.
        /// Mnemonic: "pushref"
        /// </summary>
        [OpcodeInfo("pushref")]
        PushReference = -11
    }

    /// <summary>
    /// Basic logical comparison types used in the VM.
    /// </summary>
    public enum ComparisonType : byte
    {
        LesserThan = 1,
        LesserEqualThan = 2,
        EqualTo = 3,
        NotEqualTo = 4,
        GreaterEqualThan = 5,
        GreaterThan = 6
    }

    /// <summary>
    /// Different value data types used in the VM. Multiple are unused in VM bytecode, and are omitted here.
    /// </summary>
    public enum DataType : byte
    {
        /// <summary>
        /// 64-bit floating point number.
        /// </summary>
        [DataTypeInfo('d', 8)]
        Double = 0,

        /// <summary>
        /// 32-bit signed integer.
        /// </summary>
        [DataTypeInfo('i', 4)]
        Int32 = 2,

        /// <summary>
        /// 64-bit signed integer.
        /// </summary>
        [DataTypeInfo('l', 8)]
        Int64 = 3,

        /// <summary>
        /// Boolean, represented as 1 or 0, with a 32-bit integer.
        /// </summary>
        [DataTypeInfo('b', 4)]
        Boolean = 4,

        /// <summary>
        /// Dynamic type representing any GML value. Externally known as a structure called <c>RValue</c>.
        /// 128 bits in size, or 16 bytes.
        /// </summary>
        [DataTypeInfo('v', 16)]
        Variable = 5,

        /// <summary>
        /// String, represented as a 32-bit ID.
        /// </summary>
        [DataTypeInfo('s', 4)]
        String = 6,

        /// <summary>
        /// Represents a 16-bit integer.
        /// </summary>
        [DataTypeInfo('e', 4)]
        Int16 = 15
    }

    /// <summary>
    /// Represents the special types of instance IDs used in VM bytecode, as well as in GML overall.
    /// Values greater than or equal to 0 can also be object asset IDs, depending on the version.
    /// </summary>
    public enum InstanceType : short
    {
        /// <summary>
        /// Represents the current <c>self</c> instance.
        /// </summary>
        Self = -1,

        /// <summary>
        /// Represents the <c>other</c> context, which has multiple definitions based on the location used.
        /// </summary>
        Other = -2,

        /// <summary>
        /// Represents all active object instances. Assignment operations can perform a loop.
        /// </summary>
        All = -3,

        /// <summary>
        /// Represents no object/instance.
        /// </summary>
        Noone = -4,

        /// <summary>
        /// Used for global variables.
        /// </summary>
        Global = -5,

        /// <summary>
        /// Used for GML built-in variables.
        /// </summary>
        Builtin = -6,

        /// <summary>
        /// Used for local variables.
        /// </summary>
        Local = -7,

        /// <summary>
        /// Instance is stored in a Variable data type on the top of the stack.
        /// </summary>
        StackTop = -9,

        /// <summary>
        /// Used for function argument variables in GMLv2 (GMS 2.3).
        /// </summary>
        Argument = -15,

        /// <summary>
        /// Used for static variables.
        /// </summary>
        Static = -16
    }

    /// <summary>
    /// Encoded variable type, used when referencing variables and functions in an instruction.
    /// </summary>
    public enum VariableType : byte
    {
        /// <summary>
        /// Used for normal single-dimension array variables.
        /// </summary>
        Array = 0,

        /// <summary>
        /// Used when referencing a variable on another variable, e.g. a chain reference.
        /// </summary>
        StackTop = 0x80,

        /// <summary>
        /// Used for normal variables, without any arrays or chain references.
        /// </summary>
        Normal = 0xA0,

        /// <summary>
        /// Used when referencing variables on room instance IDs, e.g. something like "inst_01ABCDEF.x" in GML.
        /// </summary>
        Instance = 0xE0,

        /// <summary>
        /// Used in tandem with multi-dimensional array push operations (<see cref="ExtendedOpcode.PushArrayFinal"/> extended opcode).
        /// </summary>
        MultiPush = 0x10,

        /// <summary>
        /// Used in tandem with multi-dimensional array push and pop operations (<see cref="ExtendedOpcode.PushArrayFinal"/>/<see cref="ExtendedOpcode.PopArrayFinal"/> extended opcodes).
        /// </summary>
        MultiPushPop = 0x90
    }

    /// <summary>
    /// The opcode of this instruction. Generally indicates what operation the instruction will perform.
    /// </summary>
    public Opcode Kind { get; }

    /// <summary>
    /// The extended opcode of this instruction, if <see cref="Kind"/> is <see cref="Opcode.Extended"/>.
    /// </summary>
    public ExtendedOpcode ExtKind { get; }

    /// <summary>
    /// For comparison instructions, represents the comparison kind.
    /// </summary>
    public ComparisonType ComparisonKind { get; }

    /// <summary>
    /// Represents the first data type argument of the instruction. This is encoded for every instruction.
    /// </summary>
    public DataType Type1 { get; }

    /// <summary>
    /// Represents the second data type argument of the instruction. This is encoded for every instruction.
    /// </summary>
    public DataType Type2 { get; }

    /// <summary>
    /// For instructions that have an instance type, represents the kind of instance or object ID.
    /// </summary>
    public InstanceType InstType { get; }

    /// <summary>
    /// For instructions that reference a variable, represents the variable being referenced,
    /// if it has been resolved as part of a reference chain directly.
    /// </summary>
    public IGMVariable? ResolvedVariable { get; }

    /// <summary>
    /// For instructions that reference a function, represents the function being referenced,
    /// if it has been resolved as part of a reference chain directly.
    /// </summary>
    public IGMFunction? ResolvedFunction { get; }

    /// <summary>
    /// For instructions that reference a variable or function, this represents the variable type.
    /// </summary>
    public VariableType ReferenceVarType { get; }

    /// <summary>
    /// Represents a 64-bit floating point value for instructions that use it.
    /// </summary>
    public double ValueDouble { get; }

    /// <summary>
    /// Represents a 16-bit integer value for instructions that use it.
    /// </summary>
    public short ValueShort { get; }

    /// <summary>
    /// Represents a 32-bit integer value for instructions that use it.
    /// </summary>
    public int ValueInt { get; }

    /// <summary>
    /// Represents a 64-bit integer value for instructions that use it.
    /// </summary>
    public long ValueLong { get; }

    /// <summary>
    /// Represents a string value for instructions that push strings.
    /// </summary>
    public IGMString? ValueString { get; }

    /// <summary>
    /// Represents a branch offset for branch instructions, in bytes.
    /// </summary>
    public int BranchOffset { get; }

    /// <summary>
    /// For <see cref="Opcode.PopWithContext"/> instructions, represents whether the flag is set to exit the <c>with</c> loop early.
    /// </summary>
    public bool PopWithContextExit { get; }

    /// <summary>
    /// For duplication instructions, represents the size of the data to duplicate by (before considering data type).
    /// </summary>
    public byte DuplicationSize { get; }

    /// <summary>
    /// For duplication instructions, this is nonzero only when in "dup swap" mode.
    /// When nonzero, the value is in the same location as the comparison type, for basic single-type instructions.
    /// This value should be that byte, but bitwise AND'd against 0x7F, and then shifted to the right 3 bits.
    /// </summary>
    public byte DuplicationSize2 { get; }

    /// <summary>
    /// Returns the number of arguments encoded in this instruction, for <see cref="Opcode.Call"/> and <see cref="Opcode.CallVariable"/> instructions.
    /// </summary>
    public int ArgumentCount { get; }

    /// <summary>
    /// For <c>pop.e.v</c> instructions, this should return either 5 or 6, depending on the "pop swap" size.
    /// </summary>
    public int PopSwapSize { get; }

    /// <summary>
    /// For <see cref="Opcode.Extended"/> instructions with <see cref="ExtendedOpcode.PushReference"/> opcode,
    /// this is the ID of the asset supplied with the instruction, if <see cref="TryFindFunction(IGameContext?)"/> returns <see langword="null"/>.
    /// </summary>
    public int AssetReferenceId { get; }

    /// <summary>
    /// For <see cref="Opcode.Extended"/> instructions with <see cref="ExtendedOpcode.PushReference"/> opcode,
    /// this returns the type of the asset supplied with the instruction, if <see cref="TryFindFunction(IGameContext?)"/> returns <see langword="null"/>.
    /// </summary>
    public AssetType GetAssetReferenceType(IGameContext context);

    /// <summary>
    /// Returns size of an instruction, in bytes.
    /// </summary>
    internal static int GetSize(IGMInstruction instr)
    {
        return instr switch
        {
            // Opcodes with functions/variables take an extra 4 bytes for the reference
            { Kind: Opcode.Pop, Type1: not DataType.Int16 } => 8,
            { Kind: Opcode.Call } => 8,

            // Push instructions take extra space to store data (aside from 16-bit integers)
            { Kind: Opcode.Push or Opcode.PushLocal or Opcode.PushGlobal or Opcode.PushBuiltin or Opcode.PushImmediate } => 
                instr.Type1 switch
                {
                    DataType.Double or DataType.Int64 => 12,
                    not DataType.Int16 => 8,
                    _ => 4
                },

            // Extended opcodes with an integer argument take an extra 4 bytes
            { Kind: Opcode.Extended, Type1: DataType.Int32 } => 8,

            // All other instructions are just 4 bytes
            _ => 4
        };
    }

    /// <summary>
    /// For instructions that reference a variable, this is used to attempt retrieve the variable,
    /// even if the variable has not been resolved as part of a reference chain.
    /// (Generally, if the variable is not resolved, this means using the raw reference value as a string ID.)
    /// </summary>
    public IGMVariable? TryFindVariable(IGameContext? context);

    /// <summary>
    /// For instructions that reference a function, this is used to attempt retrieve the function,
    /// even if the function has not been resolved as part of a reference chain.
    /// (Generally, if the function is not resolved, this means using the raw reference value as a string ID.)
    /// </summary>
    public IGMFunction? TryFindFunction(IGameContext? context);
}

/// <summary>
/// Represents a GameMaker variable entry.
/// </summary>
public interface IGMVariable
{
    /// <summary>
    /// The name of the variable.
    /// </summary>
    public IGMString Name { get; }

    /// <summary>
    /// Represents the type of instance used for the variable.
    /// </summary>
    public IGMInstruction.InstanceType InstanceType { get; }

    /// <summary>
    /// The ID of the variable in the game's data file.
    /// This can sometimes be -6, representing the instance type of a built-in variable.
    /// </summary>
    public int VariableID { get; }
}

/// <summary>
/// Represents a GameMaker function entry.
/// </summary>
public interface IGMFunction
{
    /// <summary>
    /// The name of the function.
    /// </summary>
    public IGMString Name { get; }
}

/// <summary>
/// Represents a string reference from a game's data file.
/// Used to disambiguate identical string contents.
/// </summary>
public interface IGMString
{
    /// <summary>
    /// The actual content of the string.
    /// </summary>
    public string Content { get; }
}