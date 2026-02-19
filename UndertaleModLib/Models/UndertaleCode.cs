using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using Underanalyzer;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Project;
using UndertaleModLib.Project.SerializableAssets;
using UndertaleModLib.Util;
// ReSharper disable All

namespace UndertaleModLib.Models;

/// <summary>
/// A bytecode instruction.
/// </summary>
public class UndertaleInstruction : UndertaleObject, IGMInstruction
{
    /// <summary>
    /// Possible opcodes an instruction can use.
    /// </summary>
    public enum Opcode : byte
    {
        Conv = 0x07, // Push((Types.Second)Pop) // DoubleTypeInstruction
        Mul = 0x08, // Push(Pop() * Pop()) // DoubleTypeInstruction
        Div = 0x09, // Push(Pop() / Pop()) // DoubleTypeInstruction
        Rem = 0x0A, // Push(Remainder(Pop(), Pop())) // DoubleTypeInstruction
        Mod = 0x0B, // Push(Pop() % Pop()) // DoubleTypeInstruction
        Add = 0x0C, // Push(Pop() + Pop()) // DoubleTypeInstruction
        Sub = 0x0D, // Push(Pop() - Pop()) // DoubleTypeInstruction
        And = 0x0E, // Push(Pop() & Pop()) // DoubleTypeInstruction
        Or = 0x0F, // Push(Pop() | Pop()) // DoubleTypeInstruction
        Xor = 0x10, // Push(Pop() ^ Pop()) // DoubleTypeInstruction
        Neg = 0x11, // Push(-Pop()) // SingleTypeInstruction
        Not = 0x12, // Push(~Pop()) // SingleTypeInstruction
        Shl = 0x13, // Push(Pop() << Pop()) // DoubleTypeInstruction
        Shr = 0x14, // Push(Pop() >>= Pop()) // DoubleTypeInstruction
        Cmp = 0x15, // Push(Pop() `cmp` Pop())// ComparisonInstruction
        Pop = 0x45, // Instance.Destination = Pop();
        Dup = 0x86, // Push(Peek()) // SingleTypeInstruction
        Ret = 0x9C, // return Pop() // SingleTypeInstruction
        Exit = 0x9D, // return; // SingleTypeInstruction
        Popz = 0x9E, // Pop(); // SingleTypeInstruction
        B = 0xB6, // goto Index + Offset*4; // GotoInstruction
        Bt = 0xB7, // if (Pop()) goto Index + Offset*4; // GotoInstruction
        Bf = 0xB8, // if (!Pop()) goto Index + Offset*4; // GotoInstruction
        PushEnv = 0xBA, // GotoInstruction
        PopEnv = 0xBB, // GotoInstruction
        Push = 0xC0, // Push(Value) // push constant
        PushLoc = 0xC1, // Push(Value) // push local
        PushGlb = 0xC2, // Push(Value) // push global
        PushBltn = 0xC3, // Push(Value) // push builtin variable
        PushI = 0x84, // Push(Value) // push int16
        Call = 0xD9, // Function(arg0, arg1, ..., argn) where arg = Pop() and n = ArgumentsCount
        CallV = 0x99, // TODO: Unknown, maybe to do with calling using the stack? Generates with "show_message((function(){return 5;})());"
        Break = 0xFF, // TODO: Several sub-opcodes in GMS 2.3
    }

    /// <summary>
    /// Possible types an instruction can be.
    /// </summary>
    public enum InstructionType
    {
        SingleTypeInstruction,
        DoubleTypeInstruction,
        ComparisonInstruction,
        GotoInstruction,
        PushInstruction,
        PopInstruction,
        CallInstruction,
        BreakInstruction
    }

    /// <summary>
    /// Determines the instruction type of an opcode and returns it.
    /// </summary>
    /// <param name="op">The opcode to get the instruction type of.</param>
    /// <returns>The instruction type of the supplied opcode.</returns>
    /// <exception cref="IOException">For unknown opcodes.</exception>
    public static InstructionType GetInstructionType(Opcode op)
    {
        return op switch
        {
            Opcode.Neg or Opcode.Not or Opcode.Dup or
                Opcode.Ret or Opcode.Exit or Opcode.Popz or
                Opcode.CallV
                => InstructionType.SingleTypeInstruction,

            Opcode.Conv or Opcode.Mul or Opcode.Div or
                Opcode.Rem or Opcode.Mod or Opcode.Add or
                Opcode.Sub or Opcode.And or Opcode.Or or
                Opcode.Xor or Opcode.Shl or Opcode.Shr
                => InstructionType.DoubleTypeInstruction,

            Opcode.Cmp => InstructionType.ComparisonInstruction,

            Opcode.B or Opcode.Bt or Opcode.Bf or
                Opcode.PushEnv or Opcode.PopEnv
                => InstructionType.GotoInstruction,

            Opcode.Pop => InstructionType.PopInstruction,

            Opcode.Push or Opcode.PushLoc or Opcode.PushGlb or
                Opcode.PushBltn or Opcode.PushI
                => InstructionType.PushInstruction,

            Opcode.Call => InstructionType.CallInstruction,
            Opcode.Break => InstructionType.BreakInstruction,

            _ => throw new IOException("Unknown opcode " + op.ToString().ToUpper(CultureInfo.InvariantCulture)),
        };
    }

    /// <summary>
    /// Converts from bytecode 14 instruction opcodes to modern opcodes.
    /// </summary>
    private static byte ConvertOldKindToNewKind(byte kind)
    {
        kind = kind switch
        {
            0x03 => 0x07,
            0x04 => 0x08,
            0x05 => 0x09,
            0x06 => 0x0A,
            0x07 => 0x0B,
            0x08 => 0x0C,
            0x09 => 0x0D,
            0x0A => 0x0E,
            0x0B => 0x0F,
            0x0C => 0x10,
            0x0D => 0x11,
            0x0E => 0x12,
            0x0F => 0x13,
            0x10 => 0x14,
            0x11 or 0x12 or 0x13 or 0x14 or 0x16 => 0x15,
            0x41 => 0x45,
            0x82 => 0x86,
            0xB7 => 0xB6,
            0xB8 => 0xB7,
            0xB9 => 0xB8,
            0xBB => 0xBA,
            0x9D => 0x9C,
            0x9E => 0x9D,
            0x9F => 0x9E,
            0xBC => 0xBB,
            0xDA => 0xD9,
            _ => kind
        };

        return kind;
    }

    /// <summary>
    /// Converts from modern instruction opcodes to bytecode 14 opcodes.
    /// </summary>
    private static byte ConvertNewKindToOldKind(byte kind, ComparisonType comparisonKind = default)
    {
        kind = kind switch
        {
            0x07 => 0x03,
            0x08 => 0x04,
            0x09 => 0x05,
            0x0A => 0x06,
            0x0B => 0x07,
            0x0C => 0x08,
            0x0D => 0x09,
            0x0E => 0x0A,
            0x0F => 0x0B,
            0x10 => 0x0C,
            0x11 => 0x0D,
            0x12 => 0x0E,
            0x13 => 0x0F,
            0x14 => 0x10,
            0x15 => (byte)(comparisonKind + 0x10), // Comparison kind is encoded into opcode
            0x45 => 0x41,
            0x84 => 0xC0,
            0x86 => 0x82,
            0x9C => 0x9D,
            0x9D => 0x9E,
            0x9E => 0x9F,
            0xB6 => 0xB7,
            0xB7 => 0xB8,
            0xB8 => 0xB9,
            0xBA => 0xBB,
            0xBB => 0xBC,
            0xD9 => 0xDA,
            0xC1 => 0xC0,
            0xC2 => 0xC0,
            0xC3 => 0xC0,
            _ => kind
        };

        return kind;
    }

    public enum DataType : byte
    {
        Double,
        Float,
        Int32,
        Int64,
        Boolean,
        Variable,
        String,
        [Obsolete("Unused")]
        Instance,
        Delete, // these 3 types apparently exist
        Undefined,
        UnsignedInt,
        Int16 = 0x0f
    }

    public enum InstanceType : short
    {
        /// Note: This is just object 0, but also occurs in places where no instance type was set
        Undefined = 0,
        Self = -1,
        Other = -2,
        All = -3,
        Noone = -4,
        Global = -5,
        /// Note: Used only in UndertaleVariable.VarID (which is not really even InstanceType)
        Builtin = -6,
        Local = -7,
        Stacktop = -9,
        Arg = -15,
        Static = -16

        // anything > 0 --> GameObjectIndex (or Instance ID if VariableType.Instance)
    }

    public enum VariableType : byte
    {
        Array = 0x00,
        StackTop = 0x80,
        Normal = 0xA0,
        /// The InstanceType is an Instance ID inside a room, minus 100000
        Instance = 0xE0,
        /// GMS2.3+, multidimensional array with pushaf
        ArrayPushAF = 0x10,
        /// GMS2.3+, multidimensional array with pushaf or popaf
        ArrayPopAF = 0x90,
    }

    public enum ComparisonType : byte
    {
        LT = 1,
        LTE = 2,
        EQ = 3,
        NEQ = 4,
        GTE = 5,
        GT = 6,
    }

    public Opcode Kind 
    { 
        get => (Opcode)(_firstWord >> 24); 
        set => _firstWord = (_firstWord & 0x00FFFFFF) | (((uint)value & 0xFF) << 24); 
    }
    public ComparisonType ComparisonKind 
    { 
        get => (ComparisonType)((_firstWord & 0x0000FF00) >> 8);
        set => _firstWord = (_firstWord & 0xFFFF00FF) | (((uint)value & 0xFF) << 8);
    }
    public DataType Type1
    {
        get => (DataType)((_firstWord & 0x000F0000) >> 16);
        set => _firstWord = (_firstWord & 0xFFF0FFFF) | (((uint)value & 0xF) << 16);
    }
    public DataType Type2
    {
        get => (DataType)((_firstWord & 0x00F00000) >> 20);
        set => _firstWord = (_firstWord & 0xFF0FFFFF) | (((uint)value & 0xF) << 20);
    }
    public InstanceType TypeInst
    {
        get => (InstanceType)(_firstWord & 0x0000FFFF);
        set => _firstWord = (_firstWord & 0xFFFF0000) | ((uint)value & 0xFFFF);
    }
    public short ValueShort 
    { 
        get => (short)(_firstWord & 0x0000FFFF); 
        set => _firstWord = (_firstWord & 0xFFFF0000) | ((uint)value & 0xFFFF);
    }
    public int ValueInt { get => _primitiveValue.AsInt; set => _primitiveValue = new(value); }
    public long ValueLong { get => _primitiveValue.AsLong; set => _primitiveValue = new(value); }
    public double ValueDouble { get => _primitiveValue.AsDouble; set => _primitiveValue = new(value); }
    public UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> ValueString { get => _objectValue as UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>; set => _objectValue = value; }
    public UndertaleVariable ValueVariable { get => _objectValue as UndertaleVariable; set => _objectValue = value; }
    public UndertaleFunction ValueFunction { get => _objectValue as UndertaleFunction; set => _objectValue = value; }
    internal uint ReferenceNextOccurrenceOffset
    {
        get => (uint)(_primitiveValue.AsInt & 0x07FFFFFF); 
        set => _primitiveValue = new((_primitiveValue.AsInt & ~0x07FFFFFF) | (int)(value & 0x07FFFFFF));
    }
    public VariableType ReferenceType 
    { 
        get => (VariableType)((_primitiveValue.AsInt >> 24) & 0xF8); 
        set => _primitiveValue = new((_primitiveValue.AsInt & 0x07FFFFFF) | (((int)value & 0xF8) << 24));
    }
    public int JumpOffset
    {
        get
        {
            if ((_firstWord & 0x800000) != 0)
            {
                // Sign bit of 24-bit integer is set; sign extend to full 32 bits for usage.
                return (int)(_firstWord | 0xFF000000);
            }

            // Presumably, a positive number.
            return (int)(_firstWord & 0xFFFFFF);
        }
        set
        {
            // Remove sign-extended bits, but keep sign bit.
            _firstWord = (_firstWord & 0xFF000000) | ((uint)value & 0x00FFFFFF);
        }
    }
    public bool JumpOffsetPopenvExitMagic
    {
        get => (_firstWord & 0xFFFFFF) == 0xF00000;
        set => _firstWord = (_firstWord & 0xFF000000) | (value ? 0xF00000u : 0);
    }
    public ushort ArgumentsCount
    {
        get => (ushort)(_firstWord & 0x0000FFFF);
        set => _firstWord = (_firstWord & 0xFFFF0000) | value;
    }
    public byte Extra
    {
        get => (byte)(_firstWord & 0x000000FF);
        set => _firstWord = (_firstWord & 0xFFFFFF00) | value;
    }
    public ushort SwapExtra
    {
        get => (ushort)(_firstWord & 0x0000FFFF);
        set => _firstWord = (_firstWord & 0xFFFF0000) | value;
    }
    public short ExtendedKind
    {
        get => (short)(_firstWord & 0x0000FFFF);
        set => _firstWord = (_firstWord & 0xFFFF0000) | (ushort)value;
    }
    public int IntArgument { get => _primitiveValue.AsInt; set => _primitiveValue = new(value); }

    private uint _firstWord;
    private InstructionPrimitiveType _primitiveValue;
    private object _objectValue;

    /// <summary>
    /// Objects (functions and variables) that can be referenced as part of a reference chain, in instructions.
    /// </summary>
    public interface IReferencedObject
    {
        uint Occurrences { get; set; }
        UndertaleInstruction FirstAddress { get; set; }
        int NameStringID { get; set; }
    }

    /// <summary>
    /// Parse a reference chain. This function assumes that all of the object data was read already, it only fills in ValueVariable/ValueFunction on instructions.
    /// </summary>
    internal static void ParseReferenceChain<T>(UndertaleReader reader, T obj) where T : class, UndertaleObject, IReferencedObject
    {
        if (reader.undertaleData.UnsupportedBytecodeVersion)
        {
            return;
        }

        // Get address of first instruction in the reference chain
        uint addr = reader.GetAddressForUndertaleObject(obj.FirstAddress);

        // Loop over all occurrences in the chain, assigning the variable/function references
        UndertaleInstruction instr = null;
        for (int i = 0; i < obj.Occurrences; i++)
        {
            instr = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr) ?? throw new IOException($"Failed to find instruction at {addr}");
            if (typeof(T) == typeof(UndertaleVariable))
            {
                instr.ValueVariable = obj as UndertaleVariable;
            }
            if (typeof(T) == typeof(UndertaleFunction))
            {
                instr.ValueFunction = obj as UndertaleFunction;
            }
            addr += instr.ReferenceNextOccurrenceOffset; // For relevant instructions (such as pushref.i, push.i), this is equivalent to IntArgument/ValueInt
        }

        // Name string ID uses only lower 24 bits, particularly in the case that the last reference is a pushref.i with a function
        obj.NameStringID = (int)instr.ReferenceNextOccurrenceOffset & 0xFFFFFF;
    }

    /// <summary>
    /// Serializes a reference chain. 
    /// This functions assumes that instructions have already been written to file (i.e. the CODE chunk was before FUNC/VARI, which is normally always the case).
    /// </summary>
    internal static void SerializeReferenceChain<T>(UndertaleWriter writer, IList<UndertaleCode> codeList, IList<T> objList) where T : class, UndertaleObject, IReferencedObject
    {
        // Store initial position to return to later
        uint initialPos = writer.Position;

        // Initialize all objects with no occurrences, and no first address
        foreach (T obj in objList)
        {
            obj.Occurrences = 0;
            obj.FirstAddress = null;
        }

        // Scan over all code entries for references
        Dictionary<T, long> lastReferencedAddressesAndTypes = new(objList.Count);
        foreach (UndertaleCode code in codeList)
        {
            // Skip non-root code entries
            if (code.ParentEntry is not null)
            {
                continue;
            }

            // Scan for references
            foreach (UndertaleInstruction instr in code.Instructions)
            {
                T obj = null;
                if (typeof(T) == typeof(UndertaleVariable))
                {
                    obj = instr.ValueVariable as T;
                }
                if (typeof(T) == typeof(UndertaleFunction))
                {
                    obj = instr.ValueFunction as T;
                }
                if (obj is not null)
                {
                    // Get address of this instruction's reference
                    uint thisAddress = writer.GetAddressForUndertaleObject(instr) + 4;

                    // Check to see if we have a previous occurrence to update
                    if (lastReferencedAddressesAndTypes.TryGetValue(obj, out long lastAddressAndType))
                    {
                        // Not the first occurrence - update previous occurrence
                        uint lastAddress = (uint)(lastAddressAndType & 0xFFFFFFFF);
                        int lastType = (int)(lastAddressAndType >> 32);
                        writer.Position = lastAddress;
                        writer.Write(((int)(thisAddress - lastAddress) & 0x07FFFFFF) | ((lastType & 0xF8) << 24));
                        obj.Occurrences++;

                        // Update last address/type to be this one
                        lastReferencedAddressesAndTypes[obj] = thisAddress | ((long)instr.ReferenceType << 32);
                    }
                    else
                    {
                        // First occurrence of the object
                        lastReferencedAddressesAndTypes.Add(obj, thisAddress | ((long)instr.ReferenceType << 32));
                        obj.FirstAddress = instr;
                        obj.Occurrences++;
                    }
                }
            }
        }

        // Restore original position
        writer.Position = initialPos;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        // Flag tracking whether we're writing bytecode 14 (old) instructions
        bool bytecode14 = writer.Bytecode14OrLower;

        // Switch on the basic format of instruction to encode
        switch (GetInstructionType(Kind))
        {
            case InstructionType.SingleTypeInstruction:
            case InstructionType.DoubleTypeInstruction:
            {
                // Write word, transforming opcode as needed for bytecode 14
                uint firstWord = _firstWord;
                if (bytecode14)
                {
                    firstWord = (firstWord & 0xFFFFFF) | ((uint)ConvertNewKindToOldKind((byte)(firstWord >> 24)) << 24);
                }
                writer.Write(firstWord);
                break;
            }

            case InstructionType.ComparisonInstruction:
            {
                // Write word
                uint firstWord = _firstWord;
                if (bytecode14)
                {
                    // Transform opcode for bytecode 14
                    firstWord = (firstWord & 0xFFFFFF) | 
                                 ((uint)ConvertNewKindToOldKind((byte)(firstWord >> 24), 
                                                                (ComparisonType)((firstWord & 0x0000FF00) >> 8)) << 24);

                    // Get rid of comparison type as well
                    firstWord &= ~0x0000FF00u;
                }
                writer.Write(firstWord);
                break;
            }

            case InstructionType.GotoInstruction:
            {
                // Write word
                uint firstWord = _firstWord;
                if (bytecode14)
                {
                    // Transform opcode as needed for bytecode 14
                    firstWord = (firstWord & 0xFFFFFF) | ((uint)ConvertNewKindToOldKind((byte)(firstWord >> 24)) << 24);
                }
                else if ((firstWord & 0xFFFFFF) != 0xF00000 && (firstWord & 0x800000) != 0)
                {
                    // Additionally, after bytecode 14, transform 24-bit negative branch into a 23-bit negative branch
                    
                    // Unset 24-bit sign bit
                    firstWord &= ~0x800000u;

                    // Set 23-bit sign bit
                    firstWord |= 0x400000;
                }
                writer.Write(firstWord);
                break;
            }

            case InstructionType.PopInstruction:
            {
                // Write first word, transforming opcode as needed for bytecode 14
                uint firstWord = _firstWord;
                if (bytecode14)
                {
                    firstWord = (firstWord & 0xFFFFFF) | ((uint)ConvertNewKindToOldKind((byte)(firstWord >> 24)) << 24);
                }
                writer.Write(firstWord);

                if (Type1 != DataType.Int16)
                {
                    // Write actual variable being stored to (reset next occurrence to string ID for now)
                    ReferenceNextOccurrenceOffset = (uint)(ValueVariable?.NameStringID ?? 0xDEAD);
                    writer.Write(_primitiveValue.AsInt);
                }
                break;
            }

            case InstructionType.PushInstruction:
            {
                // Write first word, transforming opcode as needed for bytecode 14
                uint firstWord = _firstWord;
                if (bytecode14)
                {
                    firstWord = (firstWord & 0xFFFFFF) | ((uint)ConvertNewKindToOldKind((byte)(firstWord >> 24)) << 24);
                }
                writer.Write(firstWord);

                // Write value being pushed
                switch (Type1)
                {
                    case DataType.Double:
                        writer.Write(ValueDouble);
                        break;
                    case DataType.Int32:
                        // For functions/variables, reset next occurrence to string ID for now
                        if (ValueFunction is UndertaleFunction function)
                        {
                            ReferenceNextOccurrenceOffset = (uint)function.NameStringID;
                            writer.Write(_primitiveValue.AsInt);
                            break;
                        }
                        if (ValueVariable is UndertaleVariable variable)
                        {
                            ReferenceNextOccurrenceOffset = (uint)variable.NameStringID;
                            writer.Write(_primitiveValue.AsInt);
                            break;
                        }

                        writer.Write(ValueInt);
                        break;
                    case DataType.Int64:
                        writer.Write(ValueLong);
                        break;
                    case DataType.Variable:
                        // Write variable (reset next occurrence to string ID for now)
                        ReferenceNextOccurrenceOffset = (uint)(ValueVariable?.NameStringID ?? 0xDEAD);
                        writer.Write(_primitiveValue.AsInt);
                        break;
                    case DataType.String:
                        writer.WriteUndertaleObject(ValueString);
                        break;
                    case DataType.Int16:
                        // Data is encoded in the first two bytes of the instruction (was already written above)
                        break;
                }

                break;
            }

            case InstructionType.CallInstruction:
            {
                // Write first word, transforming opcode as needed for bytecode 14
                uint firstWord = _firstWord;
                if (bytecode14)
                {
                    firstWord = (firstWord & 0xFFFFFF) | ((uint)ConvertNewKindToOldKind((byte)(firstWord >> 24)) << 24);
                }
                writer.Write(firstWord);

                // Write reference to the function being called (reset next occurrence to string ID for now)
                if (ValueFunction is UndertaleFunction function)
                {
                    ReferenceNextOccurrenceOffset = (uint)function.NameStringID;
                }
                writer.Write(_primitiveValue.AsInt);
                break;
            }

            case InstructionType.BreakInstruction:
            {
                // Write first word
                writer.Write(_firstWord);

                // Write integer argument, or function, if either is present
                if (Type1 == DataType.Int32)
                {
                    if (ValueFunction is UndertaleFunction function)
                    {
                        // Write function (reset next occurrence to string ID for now).
                        // We additionally encode the script asset type, which can appear in files if this reference is the final one.
                        ReferenceNextOccurrenceOffset = (uint)function.NameStringID;
                        writer.Write(_primitiveValue.AsInt | (AdaptAssetTypeId(writer.undertaleData, AssetType.Script) << 24));
                    }
                    else
                    {
                        writer.Write(IntArgument);
                    }
                }
                break;
            }

            default:
                throw new IOException($"Unknown opcode {Kind.ToString().ToUpper(CultureInfo.InvariantCulture)}");
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        // Flag tracking whether we're parsing bytecode 14 (old) instructions
        bool bytecode14 = reader.Bytecode14OrLower;

        // Read first word from instruction
        uint firstWord = reader.ReadUInt32();
        _firstWord = firstWord;

        // Read opcode from most significant byte
        byte kindByte = (byte)((firstWord & 0xFF000000) >> 24);
        Opcode kind = (Opcode)kindByte;
        if (bytecode14)
        {
            // Convert opcode from old format to new format
            Kind = kind = (Opcode)ConvertOldKindToNewKind(kindByte);
        }

        // Extract first three bytes from first word
        byte b0 = (byte)(firstWord & 0x000000FF);
        byte b1 = (byte)((firstWord & 0x0000FF00) >> 8);
        byte b2 = (byte)((firstWord & 0x00FF0000) >> 16);

        // Parse instruction contents
        InstructionType instructionType = GetInstructionType(kind);
        switch (instructionType)
        {
            case InstructionType.SingleTypeInstruction:
            case InstructionType.DoubleTypeInstruction:
            case InstructionType.ComparisonInstruction:
            {
                // Parse instruction components from bytes
                byte extra = b0;
                DataType type1 = (DataType)(b2 & 0xf);
                DataType type2 = (DataType)(b2 >> 4);

#if DEBUG
                // Ensure basic conditions hold, at least when in debug
                if (extra != 0 && kind is not (Opcode.Dup or Opcode.CallV))
                {
                    throw new IOException($"Invalid padding in {kind.ToString().ToUpper(CultureInfo.InvariantCulture)}");
                }
                if (instructionType == InstructionType.SingleTypeInstruction && type2 != 0)
                {
                    throw new IOException($"Second type should be 0 in {kind.ToString().ToUpper(CultureInfo.InvariantCulture)}");
                }
#endif

                // In bytecode 14, the comparison kind is encoded in the opcode itself
                if (bytecode14 && kind == Opcode.Cmp)
                {
                    ComparisonKind = (ComparisonType)(kindByte - 0x10);
                }

                // Check for "and.b.b" or "or.b.b", which imply the code was compiled without short-circuiting
                if ((kind is Opcode.And or Opcode.Or) && type1 == DataType.Boolean && type2 == DataType.Boolean)
                {
                    reader.undertaleData.ShortCircuit = false;
                }
                break;
            }

            case InstructionType.GotoInstruction:
            {
                // If after bytecode 14, make sure that negative 23-bit integers are sign extended to 24-bit.
                if (!bytecode14 && (firstWord & 0xFFFFFF) != 0xF00000 && (firstWord & 0x400000) != 0)
                {
                    _firstWord |= 0x800000;
                }
                break;
            }

            case InstructionType.PopInstruction:
            {
                // Parse instruction components from bytes
                DataType type1 = (DataType)(b2 & 0xf);
                if (type1 != DataType.Int16)
                {
                    // Destination is an actual variable
                    _primitiveValue = new(reader.ReadInt32());
                }
                break;
            }

            case InstructionType.PushInstruction:
            {
                // Parse instruction components from bytes
                DataType type1 = (DataType)b2;

                // Parse data being pushed
                switch (type1)
                {
                    case DataType.Double:
                        ValueDouble = reader.ReadDouble();
                        break;
                    case DataType.Int32:
                        ValueInt = reader.ReadInt32();
                        break;
                    case DataType.Int64:
                        ValueLong = reader.ReadInt64();
                        break;
                    case DataType.Variable:
                        _primitiveValue = new(reader.ReadInt32());
                        break;
                    case DataType.String:
                        ValueString = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>>();
                        break;
                    case DataType.Int16:
                        // Data is encoded in the first two bytes of the instruction (was already read above)
                        break;
                }

                break;
            }

            case InstructionType.CallInstruction:
            {
                // Parse function being called
                _primitiveValue = new(reader.ReadInt32());
                break;
            }

            case InstructionType.BreakInstruction:
            {
                // Parse instruction components from bytes
                short value = (short)(b0 | (b1 << 8));
                DataType type1 = (DataType)b2;

                // Parse integer argument, if provided
                if (type1 == DataType.Int32)
                {
                    IntArgument = reader.ReadInt32();

                    // Existence of this argument implies GameMaker 2023.8 or above
                    if (!reader.undertaleData.IsVersionAtLeast(2023, 8))
                    {
                        reader.undertaleData.SetGMS2Version(2023, 8);
                    }

                    // If this is an asset type found in GameMaker 2024.4 or above, track that as well
                    if (!reader.undertaleData.IsVersionAtLeast(2024, 4))
                    {
                        if (CheckIfAssetTypeIs2024_4(reader.undertaleData, IntArgument & 0xffffff, IntArgument >> 24))
                            reader.undertaleData.SetGMS2Version(2024, 4);
                    }
                }

                // If this is a setowner instruction, array copy-on-write is enabled
                if (value == -5)
                {
                    reader.undertaleData.ArrayCopyOnWrite = true;
                }

                // If this is a chknullish instruction (ID -10), then this implies GameMaker 2.3.7 or above
                if (value == -10 && !reader.undertaleData.IsVersionAtLeast(2, 3, 7))
                {
                    reader.undertaleData.SetGMS2Version(2, 3, 7);
                }
                break;
            }

            default:
                throw new IOException($"Unknown opcode {Kind.ToString().ToUpper(CultureInfo.InvariantCulture)}");
        }
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        // Flag tracking whether we're parsing bytecode 14 (old) instructions
        bool bytecode14 = reader.Bytecode14OrLower;

        // Read first word from instruction
        uint firstWord = reader.ReadUInt32();

        // Read opcode from most significant byte
        Opcode kind = (Opcode)((firstWord & 0xFF000000) >> 24);
        if (bytecode14)
        {
            // Convert opcode from old format to new format
            kind = (Opcode)ConvertOldKindToNewKind((byte)kind);
        }

        // Extract third byte from first word
        byte b2 = (byte)((firstWord & 0x00FF0000) >> 16);

        // Parse instruction contents
        InstructionType instructionType = GetInstructionType(kind);
        switch (instructionType)
        {
            case InstructionType.SingleTypeInstruction:
            case InstructionType.DoubleTypeInstruction:
            case InstructionType.ComparisonInstruction:
            case InstructionType.GotoInstruction:
                // No special handling required
                break;

            case InstructionType.PopInstruction:
            {
                // Skip destination of pop instruction, if present
                DataType type1 = (DataType)(b2 & 0xf);
                if (type1 != DataType.Int16)
                {
                    reader.Position += 4;
                }
                break;
            }

            case InstructionType.PushInstruction:
            {
                // Skip value being pushed, if present
                DataType type1 = (DataType)(b2 & 0xf);
                switch (type1)
                {
                    case DataType.Double:
                    case DataType.Int64:
                        reader.Position += 8;
                        break;

                    case DataType.Int32:
                        reader.Position += 4;
                        break;

                    case DataType.Variable:
                        reader.Position += 4;
                        break;

                    case DataType.String:
                        reader.Position += 4;
                        return 1;
                }
                break;
            }

            case InstructionType.CallInstruction:
                reader.Position += 4;
                break;

            case InstructionType.BreakInstruction:
            {
                // Skip past integer argument, if present
                DataType type1 = (DataType)(b2 & 0xf);
                if (type1 == DataType.Int32)
                {
                    reader.Position += 4;

                    // Existence of this argument implies GameMaker 2023.8 or above
                    if (!reader.undertaleData.IsVersionAtLeast(2023, 8))
                    {
                        reader.undertaleData.SetGMS2Version(2023, 8);
                    }
                }
                break;
            }

            default:
                throw new IOException($"Unknown opcode {kind.ToString().ToUpper(CultureInfo.InvariantCulture)}");
        }

        return 0;
    }

    /// <summary>
    /// Helper to print a variable reference to a StringBuilder.
    /// </summary>
    private void PrintVariableReference(ref StringBuilderHelper sbh, StringBuilder stringBuilder)
    {
        if (ReferenceType != VariableType.Normal)
        {
            sbh.Append(stringBuilder,
                $"[{ReferenceType.ToVariableTypeParam()}]" +
                $"{ValueVariable?.InstanceType.ToString().ToLower(CultureInfo.InvariantCulture) ?? "null"}." +
                $"{ValueVariable?.ToString() ?? "(null)"}");
        }
        else
        {
            sbh.Append(stringBuilder, ValueVariable?.ToString() ?? "(null)");
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(null, 0);
    }

    /// <summary>
    /// <inheritdoc cref="ToString()"/>
    /// </summary>
    /// <param name="code">The <see cref="UndertaleCode"/> code entry for which the instruction belongs.</param>
    /// <param name="address">Address of the instruction within its code entry.</param>
    /// <param name="blocks">A lookup of block addresses to block indices for the code entry for which the instruction belongs.</param>
    /// <returns></returns>
    public string ToString(UndertaleCode code, uint address, Dictionary<uint, uint> blocks = null)
    {
        StringBuilder sb = new();
        ToString(sb, code, address, blocks);
        return sb.ToString();
    }

    /// <summary>
    /// Inserts a string representation of this object at a specified index in a <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/> instance on where to insert the string representation.</param>
    /// <param name="code"><inheritdoc cref="ToString(UndertaleCode, uint, Dictionary{uint, uint})"/></param>
    /// <param name="address">Address of the instruction within its code entry.</param>
    /// <param name="blocks"><inheritdoc cref="ToString(UndertaleCode, uint, Dictionary{uint, uint})"/></param>
    /// <param name="index">The index on where to insert the string representation. If this is -1,
    /// it will use <paramref name="stringBuilder.Length"/> as the index instead.</param>
    /// <remarks>Note that performance of this function can be drastically different, depending on <paramref name="index"/>.
    /// For best results, it's recommended to leave it at -1.</remarks>
    public void ToString(StringBuilder stringBuilder, UndertaleCode code, uint address, Dictionary<uint, uint> blocks = null, int index = -1)
    {
        if (index < 0)
        {
            index = stringBuilder.Length;
        }
        StringBuilderHelper sbh = new(index);
        
        string kind = Kind.ToString();
        var type = GetInstructionType(Kind);
        bool unknownBreak = false;
        if (type == InstructionType.BreakInstruction)
        {
            if (!Assembler.ExtendedIDToName.TryGetValue(ExtendedKind, out kind))
            {
                kind = kind.ToLower(CultureInfo.InvariantCulture);
                unknownBreak = true;
            }
        }
        else
        {
            kind = kind.ToLower(CultureInfo.InvariantCulture);
        }

        sbh.Append(stringBuilder, kind);

        DataType type1 = Type1, type2 = Type2;
        switch (GetInstructionType(Kind))
        {
            case InstructionType.SingleTypeInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type1.ToOpcodeParam());

                if (Kind == Opcode.Dup || Kind == Opcode.CallV)
                {
                    sbh.Append(stringBuilder, ' ');
                    sbh.Append(stringBuilder, Extra);
                    if (Kind == Opcode.Dup)
                    {
                        if ((byte)ComparisonKind != 0)
                        {
                            // Special dup instruction with extra parameters
                            sbh.Append(stringBuilder, ' ');
                            sbh.Append(stringBuilder, (byte)ComparisonKind & 0x7F);
                        }
                    }
                }
                break;

            case InstructionType.DoubleTypeInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type1.ToOpcodeParam());
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type2.ToOpcodeParam());
                break;

            case InstructionType.ComparisonInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type1.ToOpcodeParam());
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type2.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                sbh.Append(stringBuilder, ComparisonKind.ToString());
                break;

            case InstructionType.GotoInstruction:
                sbh.Append(stringBuilder, ' ');
                string targetGoto;
                if (code is not null && address + JumpOffset == code.Length / 4)
                    targetGoto = "[end]";
                else if (JumpOffsetPopenvExitMagic)
                    targetGoto = "<drop>";
                else if (blocks is not null && blocks.TryGetValue((uint)(address + JumpOffset), out uint blockIndex))
                    targetGoto = $"[{blockIndex}]";
                else
                    targetGoto = (address + JumpOffset).ToString("D5");
                sbh.Append(stringBuilder, targetGoto);
                break;

            case InstructionType.PopInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type1.ToOpcodeParam());
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type2.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                if (type1 == DataType.Int16)
                {
                    // Special scenario - the swap instruction (see #129)
                    sbh.Append(stringBuilder, SwapExtra);
                }
                else
                {
                    if (type1 == DataType.Variable && TypeInst != InstanceType.Undefined)
                    {
                        if (ReferenceType == VariableType.Instance)
                        {
                            // Syntax here is a bit ugly (but maintaining compatibility) - this is a room instance ID
                            sbh.Append(stringBuilder, (short)TypeInst);
                        }
                        else
                        {
                            // Regular instance type
                            sbh.Append(stringBuilder, TypeInst.ToString().ToLower(CultureInfo.InvariantCulture));
                        }
                        sbh.Append(stringBuilder, '.');
                    }
                    PrintVariableReference(ref sbh, stringBuilder);
                }
                break;

            case InstructionType.PushInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type1.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                if (type1 == DataType.Variable)
                {
                    if (TypeInst != InstanceType.Undefined)
                    {
                        sbh.Append(stringBuilder, TypeInst.ToString().ToLower(CultureInfo.InvariantCulture));
                        sbh.Append(stringBuilder, '.');
                    }
                    PrintVariableReference(ref sbh, stringBuilder);
                    break;
                }
                if (type1 == DataType.Int32)
                {
                    if (ValueFunction is UndertaleFunction function)
                    {
                        sbh.Append(stringBuilder, "[function]");
                        sbh.Append(stringBuilder, function);
                        break;
                    }
                    if (ValueVariable is UndertaleVariable variable)
                    {
                        sbh.Append(stringBuilder, "[variable]");
                        sbh.Append(stringBuilder, variable.Name?.Content ?? "<null>");
                        break;
                    }
                    sbh.Append(stringBuilder, ValueInt.ToString(null, CultureInfo.InvariantCulture));
                    break;
                }
                if (type1 == DataType.String)
                {
                    sbh.Append(stringBuilder, ValueString);
                    break;
                }
                if (type1 == DataType.Int16)
                {
                    sbh.Append(stringBuilder, ValueShort.ToString(null, CultureInfo.InvariantCulture));
                    break;
                }
                if (type1 == DataType.Double)
                {
                    sbh.Append(stringBuilder, ValueDouble.ToString(null, CultureInfo.InvariantCulture));
                    break;
                }
                if (type1 == DataType.Int64)
                {
                    sbh.Append(stringBuilder, ValueLong.ToString(null, CultureInfo.InvariantCulture));
                }
                break;

            case InstructionType.CallInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type1.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                sbh.Append(stringBuilder, ValueFunction?.ToString() ?? "(null)");
                sbh.Append(stringBuilder, "(argc=");
                sbh.Append(stringBuilder, ArgumentsCount);
                sbh.Append(stringBuilder, ')');
                break;

            case InstructionType.BreakInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, type1.ToOpcodeParam());
                if (unknownBreak)
                {
                    sbh.Append(stringBuilder, ' ');
                    sbh.Append(stringBuilder, ExtendedKind);
                }
                if (type1 == DataType.Int32)
                {
                    sbh.Append(stringBuilder, ' ');
                    if (ValueFunction is not null)
                    {
                        sbh.Append(stringBuilder, ValueFunction.ToString() ?? "(null)");
                    }
                    else
                    {
                        sbh.Append(stringBuilder, IntArgument);
                    }
                }
                break;
        }
    }

    public uint CalculateInstructionSize()
    {
        if (ValueVariable is not null || ValueFunction is not null)
        {
            return 2;
        }
        if (GetInstructionType(Kind) == InstructionType.PushInstruction)
        {
            if (Type1 is DataType.Double or DataType.Int64)
            {
                return 3;
            }
            if (Type1 != DataType.Int16)
            {
                return 2;
            }
        }
        if (Kind == Opcode.Break && Type1 == DataType.Int32)
        {
            return 2;
        }
        return 1;
    }

    // Underanalyzer implementations
    IGMInstruction.Opcode IGMInstruction.Kind => (IGMInstruction.Opcode)Kind;
    IGMInstruction.ExtendedOpcode IGMInstruction.ExtKind => (IGMInstruction.ExtendedOpcode)ExtendedKind;
    IGMInstruction.ComparisonType IGMInstruction.ComparisonKind => (IGMInstruction.ComparisonType)ComparisonKind;
    IGMInstruction.DataType IGMInstruction.Type1 => (IGMInstruction.DataType)Type1;
    IGMInstruction.DataType IGMInstruction.Type2 => (IGMInstruction.DataType)Type2;
    IGMInstruction.InstanceType IGMInstruction.InstType => (IGMInstruction.InstanceType)TypeInst;
    IGMVariable IGMInstruction.ResolvedVariable => ValueVariable;
    IGMFunction IGMInstruction.ResolvedFunction => ValueFunction;
    IGMInstruction.VariableType IGMInstruction.ReferenceVarType => (IGMInstruction.VariableType)ReferenceType;
    double IGMInstruction.ValueDouble => ValueDouble;
    short IGMInstruction.ValueShort => ValueShort;
    int IGMInstruction.ValueInt => ValueInt;
    long IGMInstruction.ValueLong => ValueLong;
    IGMString IGMInstruction.ValueString => ValueString?.Resource;
    int IGMInstruction.BranchOffset => JumpOffset * 4;
    bool IGMInstruction.PopWithContextExit => JumpOffsetPopenvExitMagic;
    byte IGMInstruction.DuplicationSize => Extra;
    byte IGMInstruction.DuplicationSize2 => (byte)(((byte)ComparisonKind & 0x7F) >> 3);
    int IGMInstruction.ArgumentCount => (Kind == Opcode.Call) ? ArgumentsCount : Extra;
    int IGMInstruction.PopSwapSize => SwapExtra;
    int IGMInstruction.AssetReferenceId => IntArgument & 0xffffff;
    AssetType IGMInstruction.GetAssetReferenceType(IGameContext context) => AdaptAssetType((context as GlobalDecompileContext).Data, IntArgument >> 24);
    
    IGMVariable IGMInstruction.TryFindVariable(IGameContext context)
    {
        // Fast path - variable is already resolved
        if (ValueVariable is IGMVariable variable)
        {
            return variable;
        }

        // Search for variable with the encoded string ID, as a last resort
        if (context is not GlobalDecompileContext { Data: UndertaleData data })
        {
            return null;
        }
        int stringId = (int)ReferenceNextOccurrenceOffset;
        if (stringId < 0 || stringId >= data.Strings.Count)
        {
            return null;
        }
        string variableName = data.Strings[stringId].Content;
        return data.Variables.ByName(variableName) ?? new UndertaleVariable() { Name = new UndertaleString(variableName) };
    }

    IGMFunction IGMInstruction.TryFindFunction(IGameContext context)
    {
        // Fast path - function is already resolved
        if (ValueFunction is IGMFunction function)
        {
            return function;
        }

        // Get ahold of data
        if (context is not GlobalDecompileContext { Data: UndertaleData data })
        {
            return null;
        }

        // Pushref instructions with an asset type that isn't a script will never have
        // a function on it (if unresolved after parsing reference chains)
        if (Kind is Opcode.Break && (IGMInstruction.ExtendedOpcode)ExtendedKind is IGMInstruction.ExtendedOpcode.PushReference &&
            AdaptAssetType(data, IntArgument >> 24) is not AssetType.Script)
        {
            return null;
        }

        // Search for function with the encoded string ID, as a last resort
        int stringId = (int)ReferenceNextOccurrenceOffset;
        if (stringId < 0 || stringId >= data.Strings.Count)
        {
            return null;
        }
        string functionName = data.Strings[stringId].Content;
        return data.Functions.ByName(functionName) ?? new UndertaleFunction() { Name = new UndertaleString(functionName) };
    }
    
    /// <summary>
    /// Adapts asset type IDs to the <see cref="Underanalyzer.AssetType"/> enum, across versions.
    /// </summary>
    private static AssetType AdaptAssetType(UndertaleData data, int type)
    {
        if (data.IsVersionAtLeast(2024, 4))
        {
            return type switch
            {
                0 => AssetType.Object,
                1 => AssetType.Sprite,
                2 => AssetType.Sound,
                3 => AssetType.Room,
                4 => AssetType.Path,
                5 => AssetType.Script,
                6 => AssetType.Font,
                7 => AssetType.Timeline,
                8 => AssetType.Shader,
                9 => AssetType.Sequence,
                10 => AssetType.AnimCurve,
                11 => AssetType.ParticleSystem,
                13 => AssetType.Background,
                14 => AssetType.RoomInstance,
                _ => throw new Exception($"Unknown asset type {type}")
            };
        }

        return type switch
        {
            0 => AssetType.Object,
            1 => AssetType.Sprite,
            2 => AssetType.Sound,
            3 => AssetType.Room,
            4 => AssetType.Background,
            5 => AssetType.Path,
            6 => AssetType.Script,
            7 => AssetType.Font,
            8 => AssetType.Timeline,
            10 => AssetType.Shader,
            11 => AssetType.Sequence,
            12 => AssetType.AnimCurve,
            13 => AssetType.ParticleSystem,
            14 => AssetType.RoomInstance,
            _ => throw new Exception($"Unknown asset type {type}")
        };
    }

    /// <summary>
    /// Adapts the <see cref="Underanalyzer.AssetType"/> enum to asset type IDs, across versions.
    /// </summary>
    private static int AdaptAssetTypeId(UndertaleData data, AssetType type)
    {
        if (data.IsVersionAtLeast(2024, 4))
        {
            return type switch
            {
                AssetType.Object => 0,
                AssetType.Sprite => 1,
                AssetType.Sound => 2,
                AssetType.Room => 3,
                AssetType.Path => 4,
                AssetType.Script => 5,
                AssetType.Font => 6,
                AssetType.Timeline => 7,
                AssetType.Shader => 8,
                AssetType.Sequence => 9,
                AssetType.AnimCurve => 10,
                AssetType.ParticleSystem => 11,
                AssetType.Background => 13,
                AssetType.RoomInstance => 14,
                _ => throw new Exception($"Unknown asset type {type}")
            };
        }

        return type switch
        {
            AssetType.Object => 0,
            AssetType.Sprite => 1,
            AssetType.Sound => 2,
            AssetType.Room => 3,
            AssetType.Background => 4,
            AssetType.Path => 5,
            AssetType.Script => 6,
            AssetType.Font => 7,
            AssetType.Timeline => 8,
            AssetType.Shader => 10,
            AssetType.Sequence => 11,
            AssetType.AnimCurve => 12,
            AssetType.ParticleSystem => 13,
            AssetType.RoomInstance => 14,
            _ => throw new Exception($"Unknown asset type {type}")
        };
    }

    /// <summary>
    /// Checks whether the given pair of ID/type is guaranteed to be 2024.4+.
    /// That is, it does not exist in the game data when using old IDs.
    /// </summary>
    private static bool CheckIfAssetTypeIs2024_4(UndertaleData data, int resourceId, int resourceType)
    {
        switch (resourceType)
        {
            // cases 0-3 are unnecessary

            case 4:
                return resourceId >= data.Backgrounds.Count;
            case 5:
                return resourceId >= data.Paths.Count;
            case 6:
                return resourceId >= data.Scripts.Count;
            case 7:
                return resourceId >= data.Fonts.Count;
            case 8:
                return resourceId >= data.Timelines.Count;
            case 9:
                return true; // used to be unused, now are sequences
            case 10:
                return resourceId >= data.Shaders.Count;
            case 11:
                return resourceId >= data.Sequences.Count;

            // case 12 used to be animcurves, but now is unused (so would actually mean earlier than 2024.4)

            case 13:
                return resourceId >= data.ParticleSystems.Count;
        }

        return false;
    }
}

public static class UndertaleInstructionUtil
{
    public static string ToOpcodeParam(this UndertaleInstruction.DataType type)
    {
        return type switch
        {
            UndertaleInstruction.DataType.Double => "d",
            UndertaleInstruction.DataType.Float => "f",
            UndertaleInstruction.DataType.Int32 => "i",
            UndertaleInstruction.DataType.Int64 => "l",
            UndertaleInstruction.DataType.Boolean => "b",
            UndertaleInstruction.DataType.Variable => "v",
            UndertaleInstruction.DataType.String => "s",
            UndertaleInstruction.DataType.Int16 => "e",
            _ => type.ToString().ToLower(CultureInfo.InvariantCulture),
        };
    }

    public static UndertaleInstruction.DataType FromOpcodeParam(string type)
    {
        return type switch
        {
            "d" => UndertaleInstruction.DataType.Double,
            "f" => UndertaleInstruction.DataType.Float,
            "i" => UndertaleInstruction.DataType.Int32,
            "l" => UndertaleInstruction.DataType.Int64,
            "b" => UndertaleInstruction.DataType.Boolean,
            "v" => UndertaleInstruction.DataType.Variable,
            "s" => UndertaleInstruction.DataType.String,
            "e" => UndertaleInstruction.DataType.Int16,
            _ => (UndertaleInstruction.DataType)Enum.Parse(typeof(UndertaleInstruction.DataType), type, true),
        };
    }

    public static string ToVariableTypeParam(this UndertaleInstruction.VariableType type)
    {
        return type switch
        {
            UndertaleInstruction.VariableType.StackTop => "stacktop",
            UndertaleInstruction.VariableType.Array => "array",
            UndertaleInstruction.VariableType.Instance => "instance",
            UndertaleInstruction.VariableType.ArrayPopAF => "arraypopaf",
            UndertaleInstruction.VariableType.ArrayPushAF => "arraypushaf",
            _ => type.ToString().ToLower(CultureInfo.InvariantCulture),
        };
    }
}

/// <summary>
/// A code entry in a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleCode : UndertaleNamedResource, UndertaleObjectWithBlobs, IProjectAsset, INotifyPropertyChanged, IDisposable, IGMCode
{
    /// <summary>
    /// The name of the code entry.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Length of the code entry, in bytes.
    /// </summary>
    public uint Length { get; set; }

    /// <summary>
    /// The amount of local variables this code entry has.
    /// </summary>
    /// <remarks>
    /// Warning: This is actually a ushort internally, but it's an uint here for compatibility.
    /// </remarks>
    public uint LocalsCount { get; set; }

    /// <summary>
    /// The amount of arguments this code entry accepts.
    /// </summary>
    public ushort ArgumentsCount { get; set; }

    /// <summary>
    /// Offset, in bytes, where code should begin executing from within the bytecode of this code entry.
    /// </summary>
    /// <remarks>
    /// Should be 0 for root-level (parent) code entries, and nonzero for child code entries.
    /// </remarks>
    public uint Offset { get; set; }

    /// <summary>
    /// A list of bytecode instructions this code entry has.
    /// </summary>
    public List<UndertaleInstruction> Instructions { get; } = new List<UndertaleInstruction>();

    /// <summary>
    /// A flag set on certain code entries, which usually don't have locals attached to them.
    /// </summary>
    public bool WeirdLocalFlag { get; set; }

    /// <summary>
    /// Parent entry of this code entry, if this is a child entry; <see langword="null"/> otherwise.
    /// </summary>
    public UndertaleCode ParentEntry { get; set; } = null;

    /// <summary>
    /// Child entries of this code entry, if a root-level (parent) entry; empty if a child entry.
    /// </summary>
    public List<UndertaleCode> ChildEntries { get; set; } = new List<UndertaleCode>();

    /// <inheritdoc />
#pragma warning disable CS0067 // TODO: remove this suppression once Fody is no longer in use
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    // Bytecode address to use during (de)serialization, since bytecode can be a separate blob
    private uint _bytecodeAbsoluteAddress;

    // If instruction data cannot be parsed due to an unsupported bytecode version, this is where it gets stored (raw).
    private byte[] _unsupportedBuffer;

    /// <summary>
    /// Creates an empty root code entry with the given name, along with an empty code locals entry (when necessary).
    /// </summary>
    /// <remarks>
    /// The code entry (and possibly code locals entry) will be immediately added to the data.
    /// </remarks>
    /// <param name="data">Data to add the new code to.</param>
    /// <param name="name">Name of the new code entry to create.</param>
    /// <returns>The new code entry.</returns>
    public static UndertaleCode CreateEmptyEntry(UndertaleData data, string name)
    {
        return CreateEmptyEntry(data, data.Strings.MakeString(name));
    }

    /// <summary>
    /// Creates an empty root code entry with the given name, along with an empty code locals entry (when necessary).
    /// </summary>
    /// <param name="data">Data to add the new code to.</param>
    /// <param name="name">Name of the new code entry to create.</param>
    /// <returns>The new code entry.</returns>
    public static UndertaleCode CreateEmptyEntry(UndertaleData data, UndertaleString name)
    {
        // Create entry
        UndertaleCode newEntry = new()
        {
            Name = name,
            LocalsCount = 1
        };
        data.Code.Add(newEntry);

        // Also attach code locals if necessary
        if (data.CodeLocals is not null)
        {
            UndertaleCodeLocals.CreateEmptyEntry(data, name);
        }
        else
        {
            newEntry.WeirdLocalFlag = true;
        }

        return newEntry;
    }

    /// <inheritdoc />
    public void SerializeBlobBefore(UndertaleWriter writer)
    {
        // If in bytecode 14 or lower (or an unsupported version), we don't have a separate instruction blob
        if (writer.undertaleData.UnsupportedBytecodeVersion || writer.Bytecode14OrLower)
        {
            return;
        }

        if (ParentEntry is not null)
        {
            // If this is a child code entry, simply update address and length of bytecode
            _bytecodeAbsoluteAddress = writer.LastBytecodeAddress;
            Length = writer.Position - _bytecodeAbsoluteAddress;
        }
        else
        {
            // If this is a root code entry, write all of the instructions,
            // then update address and length of bytecode
            writer.LastBytecodeAddress = writer.Position;
            _bytecodeAbsoluteAddress = writer.Position;
            uint start = writer.Position;
            foreach (UndertaleInstruction instr in Instructions)
                writer.WriteUndertaleObject(instr);
            Length = writer.Position - start;
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        // Write name only (length isn't necessarily known yet)
        writer.WriteUndertaleString(Name);

        // Change logic depending on bytecode version
        if (writer.undertaleData.UnsupportedBytecodeVersion)
        {
            // Unsupported version: simply write the buffer of data, and ignore contents
            Length = (uint)_unsupportedBuffer.Length;
            writer.Write(Length);
            writer.Write(_unsupportedBuffer);
        }
        else if (writer.Bytecode14OrLower)
        {
            // Bytecode 14 or lower: write instructions immediately, and patch in the length
            uint patch = writer.Position;
            writer.Write(0xDEADC0DE);
            uint start = writer.Position;
            foreach (UndertaleInstruction instr in Instructions)
                writer.WriteUndertaleObject(instr);
            Length = writer.Position - start;
            uint jumpBack = writer.Position;
            writer.Position = patch;
            writer.Write(Length);
            writer.Position = jumpBack;
        }
        else
        {
            // Bytecode 15 and above: write the rest of the fields
            // (no instructions get written here; they're in a separate blob)
            writer.Write(Length);
            writer.Write((ushort)LocalsCount);
            writer.Write((ushort)(ArgumentsCount | (WeirdLocalFlag ? (ushort)0x8000 : 0)));
            int bytecodeRelativeAddress = (int)_bytecodeAbsoluteAddress - (int)writer.Position;
            writer.Write(bytecodeRelativeAddress);
            writer.Write(Offset);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        // Parse basic fields
        Name = reader.ReadUndertaleString();
        Length = reader.ReadUInt32();

        // Change logic depending on bytecode version
        if (reader.undertaleData.UnsupportedBytecodeVersion)
        {
            // Unsupported version: simply read in a buffer of data, and ignore contents
            _unsupportedBuffer = reader.ReadBytes((int)Length);
        }
        else if (reader.Bytecode14OrLower)
        {
            // Bytecode 14 or lower: parse instructions immediately.
            long instructionStartPos = reader.AbsPosition;
            long instructionEndPos = instructionStartPos + Length;
            Instructions.Clear();
            Instructions.Capacity = (int)reader.BytecodeAddresses[(uint)instructionStartPos].InstructionCount;
            while (reader.AbsPosition < instructionEndPos)
            {
                Instructions.Add(reader.ReadUndertaleObject<UndertaleInstruction>());
            }

            // Set this flag for code editor, etc. to not get confused later
            WeirdLocalFlag = true;
        }
        else
        {
            // Bytecode 15 or above: parse locals & arguments count, then follow bytecode address to parse instructions
            LocalsCount = reader.ReadUInt16();
            ArgumentsCount = reader.ReadUInt16();
            if ((ArgumentsCount & 0x8000) == 0x8000)
            {
                // Locals flag is set; bitmask it out
                ArgumentsCount &= 0x7FFF;
                WeirdLocalFlag = true;
            }
            int bytecodeRelativeAddress = reader.ReadInt32();
            _bytecodeAbsoluteAddress = (uint)((int)reader.AbsPosition - 4 + bytecodeRelativeAddress);

            // Check if this is a child code entry (which shares the same bytecode address as its parent).
            // Note that we use TryGetValue here to account for 0-length code entries at the very end of the code list.
            reader.BytecodeAddresses.TryGetValue(_bytecodeAbsoluteAddress, out UndertaleReader.BytecodeInformation info);
            if (Length > 0 && info.RootEntry is UndertaleCode parentEntry)
            {
                // This is a child code entry; attach to parent. No need to parse any instructions.
                ParentEntry = parentEntry;
                parentEntry.ChildEntries.Add(this);
            }
            else
            {
                // Update information to mark this entry as the root (if we have at least 1 instruction)
                if (Length > 0)
                {
                    reader.BytecodeAddresses[_bytecodeAbsoluteAddress] = new(info.InstructionCount, this);
                }

                // Jump to instruction blob, storing position to return to for later
                long returnTo = reader.AbsPosition;
                reader.AbsPosition = _bytecodeAbsoluteAddress;

                // Parse instructions
                long instructionStartPos = _bytecodeAbsoluteAddress;
                long instructionEndPos = instructionStartPos + Length;
                Instructions.Clear();
                Instructions.Capacity = (int)info.InstructionCount;
                while (reader.AbsPosition < instructionEndPos)
                {
                    Instructions.Add(reader.ReadUndertaleObject<UndertaleInstruction>());
                }

                // Return from instruction blob
                reader.AbsPosition = returnTo;
            }

            // Read final offset field
            Offset = reader.ReadUInt32();
        }
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 4; // "Name"
        uint length = reader.ReadUInt32();

        if (reader.Bytecode14OrLower)
        {
            long instructionStart = reader.AbsPosition;
            long instructionStop = instructionStart + length;

            // Get instructions count
            uint instrCount = 0;
            uint instrSubCount = 0;
            while (reader.AbsPosition < instructionStop)
            {
                instrCount++;
                instrSubCount += UndertaleInstruction.UnserializeChildObjectCount(reader);
            }

            reader.BytecodeAddresses.Add((uint)instructionStart, new(instrCount, null));

            count += instrCount + instrSubCount;
        }
        else
        {
            reader.Position += 4;

            int bytecodeRelativeAddress = reader.ReadInt32();
            uint bytecodeAbsoluteAddress = (uint)((int)reader.AbsPosition - 4 + bytecodeRelativeAddress);

            if (length == 0 || reader.BytecodeAddresses.ContainsKey(bytecodeAbsoluteAddress))
            {
                reader.Position += 4; // "Offset"
                return count;
            }

            long here = reader.AbsPosition;
            reader.AbsPosition = bytecodeAbsoluteAddress;

            // Get instructions counts
            uint instrCount = 0;
            uint instrSubCount = 0;
            while (reader.AbsPosition < bytecodeAbsoluteAddress + length)
            {
                instrCount++;
                instrSubCount += UndertaleInstruction.UnserializeChildObjectCount(reader);
            }

            reader.BytecodeAddresses.Add(bytecodeAbsoluteAddress, new(instrCount, null));

            reader.AbsPosition = here;
            reader.Position += 4; // "Offset"

            count += instrCount + instrSubCount;
        }

        return count;
    }

    /// <summary>
    /// Recalculates this code entry's length, based on the size of all instructions contained within.
    /// </summary>
    public void UpdateLength()
    {
        uint addr = 0;
        foreach (UndertaleInstruction instr in Instructions)
        {
            addr += instr.CalculateInstructionSize();
        }
        Length = addr * 4;
    }

    /// <summary>
    /// Finds and returns a set of all variables this code entry references.
    /// </summary>
    /// <returns>A set of all variables this code entry references.</returns>
    public ISet<UndertaleVariable> FindReferencedVars()
    {
        HashSet<UndertaleVariable> vars = new();
        foreach (UndertaleInstruction instr in Instructions)
        {
            if (instr.ValueVariable is UndertaleVariable v)
            {
                vars.Add(v);
            }
        }
        return vars;
    }

    /// <summary>
    /// Finds and returns a list of all local variables this code entry references.
    /// </summary>
    /// <returns>A set of all local variables this code entry references.</returns>
    public ISet<UndertaleVariable> FindReferencedLocalVars()
    {
        HashSet<UndertaleVariable> vars = new();
        foreach (UndertaleInstruction instr in Instructions)
        {
            if (instr.ValueVariable is UndertaleVariable v && 
                v.InstanceType == UndertaleInstruction.InstanceType.Local)
            {
                vars.Add(v);
            }
        }
        return vars;
    }

    /// <summary>
    /// Finds and returns the index of the first try variable used, or -1 if none is found.
    /// </summary>
    public int FindFirstTryLocalIndex()
    {
        const string variablePrefix = "__yy_breakEx";
        foreach (UndertaleInstruction instr in Instructions)
        {
            if (instr.ValueVariable is UndertaleVariable v &&
                v.InstanceType == UndertaleInstruction.InstanceType.Local &&
                v.Name.Content.StartsWith(variablePrefix, StringComparison.Ordinal) &&
                int.TryParse(v.Name.Content[variablePrefix.Length..], out int index))
            {
                return index;
            }
        }
        return -1;
    }

    /// <summary>
    /// Append instructions at the end of this code entry.
    /// </summary>
    /// <param name="instructions">The instructions to append.</param>
    public void Append(IEnumerable<UndertaleInstruction> instructions)
    {
        if (ParentEntry is not null)
            return;

        Instructions.AddRange(instructions);
        UpdateLength();
    }

    /// <summary>
    /// Replaces <b>all</b> instructions currently existing in this code entry with another set of instructions.
    /// </summary>
    /// <param name="instructions">The new instructions for this code entry.</param>
    public void Replace(IEnumerable<UndertaleInstruction> instructions)
    {
        if (ParentEntry is not null)
            return;

        Instructions.Clear();
        Append(instructions);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Instructions?.Clear();
        ChildEntries = new();
        Name = null;
        _unsupportedBuffer = null;
    }

    /// <inheritdoc/>
    ISerializableProjectAsset IProjectAsset.GenerateSerializableProjectAsset(ProjectContext projectContext)
    {
        SerializableCode serializable = new();
        serializable.PopulateFromData(projectContext, this);
        return serializable;
    }

    /// <inheritdoc/>
    public string ProjectName => Name?.Content ?? "<unknown name>";

    /// <inheritdoc/>
    public SerializableAssetType ProjectAssetType => SerializableAssetType.Code;

    /// <inheritdoc/>
    public bool ProjectExportable => Name?.Content is not null && ParentEntry is null;

    // Underanalyzer implementations
    IGMString IGMCode.Name => Name;
    int IGMCode.Length => (int)Length;
    int IGMCode.InstructionCount => Instructions.Count;
    int IGMCode.StartOffset => (int)Offset;
    IGMCode IGMCode.Parent => ParentEntry;
    int IGMCode.ChildCount => ChildEntries.Count;
    int IGMCode.ArgumentCount => ArgumentsCount;
    int IGMCode.LocalCount => (int)LocalsCount;
    public IGMInstruction GetInstruction(int index) => Instructions[index];
    public IGMCode GetChild(int index) => ChildEntries[index];
}
