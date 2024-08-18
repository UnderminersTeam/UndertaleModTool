using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Underanalyzer;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models;

/// <summary>
/// A bytecode instruction.
/// </summary>
public class UndertaleInstruction : UndertaleObject, IGMInstruction
{
    /// <summary>
    /// Possible opcodes an instruction can use.
    /// </summary>
    //TODO: document all these. i ain't smart enough to understand these.
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
    private static byte ConvertInstructionKind(byte kind)
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
            0xDA => 0xD9,
            0x41 => 0x45,
            0x82 => 0x86,
            0xB7 => 0xB6,
            0xB8 => 0xB7,
            0xB9 => 0xB8,
            0x9D => 0x9C,
            0x9E => 0x9D,
            0x9F => 0x9E,
            0xBB => 0xBA,
            0xBC => 0xBB,
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
        Undefined = 0, // actually, this is just object 0, but also occurs in places where no instance type was set

        Self = -1,
        Other = -2,
        All = -3,
        Noone = -4,
        Global = -5,
        Builtin = -6, // Note: Used only in UndertaleVariable.VarID (which is not really even InstanceType)
        Local = -7,
        Stacktop = -9,
        Arg = -15,
        Static = -16

        // anything > 0 => GameObjectIndex
    }

    public enum VariableType : byte
    {
        Array = 0x00,
        StackTop = 0x80,
        Normal = 0xA0,
        Instance = 0xE0, // the InstanceType is an instance ID inside the room -100000
        ArrayPushAF = 0x10, // GMS2.3+, multidimensional array with pushaf
        ArrayPopAF = 0x90, // GMS2.3+, multidimensional array with pushaf or popaf
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

    public uint Address { get; internal set; }
    public Opcode Kind { get; set; }
    public ComparisonType ComparisonKind { get; set; }
    public DataType Type1 { get; set; }
    public DataType Type2 { get; set; }
    public InstanceType TypeInst { get; set; }
    public object Value { get; set; }
    public Reference<UndertaleVariable> Destination { get; set; }
    public Reference<UndertaleFunction> Function { get; set; }
    private int _IntegerArgument;
    public int JumpOffset { get => _IntegerArgument; set => _IntegerArgument = value; }
    public int IntArgument { get => _IntegerArgument; set => _IntegerArgument = value; }
    public bool JumpOffsetPopenvExitMagic { get; set; }
    public ushort ArgumentsCount { get; set; }
    public byte Extra { get; set; }
    public ushort SwapExtra { get; set; }

    public UndertaleCode Entry { get; set; } // Set for the first instruction

    public interface ReferencedObject
    {
        uint Occurrences { get; set; }
        UndertaleInstruction FirstAddress { get; set; }
        int NameStringID { get; set; }
    }

    public class Reference<T> : UndertaleObject where T : class, UndertaleObject, ReferencedObject
    {
        public uint NextOccurrenceOffset { get; set; } = 0xdead;
        public VariableType Type { get; set; }
        public T Target { get; set; }

        public Reference()
        {
        }

        public Reference(T target)
        {
            Target = target;
        }

        public Reference(T target, VariableType type)
        {
            Type = type;
            Target = target;
        }

        public Reference(int int32Value)
        {
            NextOccurrenceOffset = (uint)int32Value & 0x07FFFFFF;
            Type = (VariableType)((int32Value >> 24) & 0xF8);
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            NextOccurrenceOffset = 0xdead;
            writer.Write((NextOccurrenceOffset & 0x07FFFFFF) | (((uint)Type & 0xF8) << 24));
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            int int32Value = reader.ReadInt32();
            NextOccurrenceOffset = (uint)int32Value & 0x07FFFFFF;
            Type = (VariableType)((int32Value >> 24) & 0xF8);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (typeof(T) == typeof(UndertaleVariable) && Type != VariableType.Normal)
            {
                return String.Format("[{0}]{1}{2}", Type.ToString().ToLower(CultureInfo.InvariantCulture), ((Target as UndertaleVariable)?.InstanceType.ToString().ToLower(CultureInfo.InvariantCulture) ?? "null") + ".", Target?.ToString() ?? "(null)");
            }
            else
                return String.Format("{0}", Target?.ToString() ?? "(null)");
        }

        public static Dictionary<T, List<UndertaleInstruction>> CollectReferences(IList<UndertaleCode> codes)
        {
            Dictionary<T, List<UndertaleInstruction>> list = new Dictionary<T, List<UndertaleInstruction>>();
            foreach (UndertaleCode code in codes)
            {
                if (code.ParentEntry != null) // GMS 2.3, skip inner entries
                    continue;
                foreach (UndertaleInstruction instr in code.Instructions)
                {
                    T obj = instr.GetReference<T>()?.Target;
                    if (obj != null)
                    {
                        if (!list.ContainsKey(obj))
                            list.Add(obj, new List<UndertaleInstruction>());
                        list[obj].Add(instr);
                    }
                }
            }
            return list;
        }

        /// <summary>
        ///  Serialize the reference chain. This functions assumes that the Reference objects have already been written to file (i.e. the CODE chunk was before FUNC/VARI,
        ///  which is normally always the case)
        /// </summary>
        public static void SerializeReferenceChain(UndertaleWriter writer, IList<UndertaleCode> codeList, IList<T> varList)
        {
            Dictionary<T, List<UndertaleInstruction>> references = CollectReferences(codeList);
            uint pos = writer.Position;
            foreach (T var in varList)
            {
                var.Occurrences = references.ContainsKey(var) ? (uint)references[var].Count : 0;
                if (var.Occurrences > 0)
                {
                    var.FirstAddress = references[var][0];
                    for (int i = 0; i < references[var].Count; i++)
                    {
                        uint thisAddr = writer.GetAddressForUndertaleObject(references[var][i]);
                        int addrDiff;
                        if (i < references[var].Count - 1)
                        {
                            uint nextAddr = writer.GetAddressForUndertaleObject(references[var][i + 1]);
                            addrDiff = (int)(nextAddr - thisAddr);
                        }
                        else
                            addrDiff = var.NameStringID;
                        Reference<T> thisRef = references[var][i].GetReference<T>();
                        writer.Position = writer.GetAddressForUndertaleObject(thisRef);
                        writer.Write((addrDiff & 0x07FFFFFF) | (((int)thisRef.Type & 0xF8) << 24));
                    }
                }
                else
                {
                    var.FirstAddress = null;
                }
            }
            writer.Position = pos;
        }

        /// <summary>
        ///  Parse the reference chain. This function assumes that all of the object data was read already, it only fills in the "Target" field of Reference objects
        /// </summary>
        public static void ParseReferenceChain(UndertaleReader reader, T obj)
        {
            if (reader.undertaleData.UnsupportedBytecodeVersion)
                return;
            Reference<T> reference = null;
            uint addr = reader.GetAddressForUndertaleObject(obj.FirstAddress);
            for (int i = 0; i < obj.Occurrences; i++)
            {
                reference = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr).GetReference<T>(true);
                if (reference == null)
                    throw new IOException("Failed to find reference at " + addr);
                reference.Target = obj;
                addr += (uint)reference.NextOccurrenceOffset;
            }
            obj.NameStringID = (int)reference.NextOccurrenceOffset;
        }
    }

    public Reference<T> GetReference<T>(bool allowResolve = false) where T : class, UndertaleObject, ReferencedObject
    {
        Reference<T> res = (Destination as Reference<T>) ?? (Function as Reference<T>) ?? (Value as Reference<T>);
        if (allowResolve && res == null)
        {
            if (Kind == Opcode.Break && Value is short breakType && breakType == -11 /* pushref */)
            {
                Function = new Reference<UndertaleFunction>(IntArgument);
                return Function as Reference<T>;
            }
            if (Value is int val)
            {
                Value = new Reference<T>(val);
                return (Reference<T>)Value;
            }
        }
        return res;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        switch (GetInstructionType(Kind))
        {
            case InstructionType.SingleTypeInstruction:
            case InstructionType.DoubleTypeInstruction:
            case InstructionType.ComparisonInstruction:
            {
                writer.Write(Extra);
                if (writer.Bytecode14OrLower && Kind == Opcode.Cmp)
                    writer.Write((byte)0);
                else
                    writer.Write((byte)ComparisonKind);
                byte TypePair = (byte)((byte)Type2 << 4 | (byte)Type1);
                writer.Write(TypePair);

                if (writer.Bytecode14OrLower)
                {
                    byte k = Kind switch
                    {
                        Opcode.Conv => 0x03,
                        Opcode.Mul => 0x04,
                        Opcode.Div => 0x05,
                        Opcode.Rem => 0x06,
                        Opcode.Mod => 0x07,
                        Opcode.Add => 0x08,
                        Opcode.Sub => 0x09,
                        Opcode.And => 0x0A,
                        Opcode.Or => 0x0B,
                        Opcode.Xor => 0x0C,
                        Opcode.Neg => 0x0D,
                        Opcode.Not => 0x0E,
                        Opcode.Shl => 0x0F,
                        Opcode.Shr => 0x10,
                        Opcode.Dup => 0x82,
                        Opcode.Cmp => (byte)(ComparisonKind + 0x10),
                        Opcode.Ret => 0x9D,
                        Opcode.Exit => 0x9E,
                        Opcode.Popz => 0x9F,
                        _ => (byte)Kind,
                    };
                    writer.Write(k);
                }
                else
                    writer.Write((byte)Kind);
            }
                break;

            case InstructionType.GotoInstruction:
            {
                // See unserialize
                if (writer.Bytecode14OrLower)
                    writer.WriteInt24(JumpOffset);
                else if (JumpOffsetPopenvExitMagic)
                {
                    writer.WriteInt24(0xF00000);
                }
                else
                {
                    uint JumpOffsetFixed = (uint)JumpOffset;
                    JumpOffsetFixed &= ~0xFF800000;
                    writer.WriteInt24((int)JumpOffsetFixed);
                }

                if (writer.Bytecode14OrLower)
                {
                    if (Kind == Opcode.B)
                        writer.Write((byte)0xB7);
                    else if (Kind == Opcode.Bt)
                        writer.Write((byte)0xB8);
                    else if (Kind == Opcode.Bf)
                        writer.Write((byte)0xB9);
                    else if (Kind == Opcode.PushEnv)
                        writer.Write((byte)0xBB);
                    else if (Kind == Opcode.PopEnv)
                        writer.Write((byte)0xBC);
                    else
                        writer.Write((byte)Kind);
                }
                else
                    writer.Write((byte)Kind);
            }
                break;

            case InstructionType.PopInstruction:
            {
                if (Type1 == DataType.Int16)
                {
                    // Special scenario - the swap instruction
                    // TODO: Figure out the proper syntax, see #129
                    writer.Write(SwapExtra);
                    byte TypePair = (byte)((byte)Type2 << 4 | (byte)Type1);
                    writer.Write(TypePair);
                    if (writer.Bytecode14OrLower && Kind == Opcode.Pop)
                        writer.Write((byte)0x41);
                    else
                        writer.Write((byte)Kind);
                }
                else
                {
                    writer.Write((short)TypeInst);
                    byte TypePair = (byte)((byte)Type2 << 4 | (byte)Type1);
                    writer.Write(TypePair);
                    if (writer.Bytecode14OrLower && Kind == Opcode.Pop)
                        writer.Write((byte)0x41);
                    else
                        writer.Write((byte)Kind);
                    writer.WriteUndertaleObject(Destination);
                }
            }
                break;

            case InstructionType.PushInstruction:
            {
                if (Type1 == DataType.Int16)
                {
                    //Debug.Assert(Value.GetType() == typeof(short));
                    writer.Write((short)Value);
                }
                else if (Type1 == DataType.Variable)
                {
                    writer.Write((short)TypeInst);
                }
                else
                {
                    writer.Write((short)0);
                }
                writer.Write((byte)Type1);
                if (writer.Bytecode14OrLower)
                    writer.Write((byte)0xC0);
                else
                    writer.Write((byte)Kind);
                switch (Type1)
                {
                    case DataType.Double:
                        //Debug.Assert(Value.GetType() == typeof(double));
                        writer.Write((double)Value);
                        break;
                    case DataType.Float:
                        //Debug.Assert(Value.GetType() == typeof(float));
                        writer.Write((float)Value);
                        break;
                    case DataType.Int32:
                        if (Value.GetType() == typeof(Reference<UndertaleFunction>))
                        {
                            writer.WriteUndertaleObject((Reference<UndertaleFunction>)Value);
                            break;
                        }
                        if (Value.GetType() == typeof(Reference<UndertaleVariable>))
                        {
                            writer.WriteUndertaleObject((Reference<UndertaleVariable>)Value);
                            break;
                        }
                        //Debug.Assert(Value.GetType() == typeof(int));
                        writer.Write((int)Value);
                        break;
                    case DataType.Int64:
                        //Debug.Assert(Value.GetType() == typeof(long));
                        writer.Write((long)Value);
                        break;
                    case DataType.Boolean:
                        //Debug.Assert(Value.GetType() == typeof(bool));
                        writer.Write((bool)Value ? 1 : 0);
                        break;
                    case DataType.Variable:
                        //Debug.Assert(Value.GetType() == typeof(Reference<UndertaleVariable>));
                        writer.WriteUndertaleObject((Reference<UndertaleVariable>)Value);
                        break;
                    case DataType.String:
                        //Debug.Assert(Value.GetType() == typeof(UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>));
                        writer.WriteUndertaleObject((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)Value);
                        break;
                    case DataType.Int16:
                        break;
                }
            }
                break;

            case InstructionType.CallInstruction:
            {
                writer.Write(ArgumentsCount);
                writer.Write((byte)Type1);
                if (writer.Bytecode14OrLower && Kind == Opcode.Call)
                    writer.Write((byte)0xDA);
                else
                    writer.Write((byte)Kind);
                writer.WriteUndertaleObject(Function);
            }
                break;

            case InstructionType.BreakInstruction:
            {
                //Debug.Assert(Value.GetType() == typeof(short));
                writer.Write((short)Value);
                writer.Write((byte)Type1);
                writer.Write((byte)Kind);
                if (Type1 == DataType.Int32)
                {
                    if (Function != null)
                        writer.WriteUndertaleObject(Function);
                    else
                        writer.Write(IntArgument);
                }
            }
                break;

            default:
                throw new IOException("Unknown opcode " + Kind.ToString().ToUpper(CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        long instructionStartAddress = reader.Position;
        reader.Position += 3; // skip for now, we'll read them later
        byte kind = reader.ReadByte();
        if (reader.Bytecode14OrLower)
        {
            // Convert opcode to our enum
            kind = ConvertInstructionKind(kind);
        }
        Kind = (Opcode)kind;
        reader.Position = instructionStartAddress;
        switch (GetInstructionType(Kind))
        {
            case InstructionType.SingleTypeInstruction:
            case InstructionType.DoubleTypeInstruction:
            case InstructionType.ComparisonInstruction:
            {
                Extra = reader.ReadByte();
#if DEBUG
                if (Extra != 0 && Kind != Opcode.Dup && Kind != Opcode.CallV)
                    throw new IOException("Invalid padding in " + Kind.ToString().ToUpper(CultureInfo.InvariantCulture));
#endif
                ComparisonKind = (ComparisonType)reader.ReadByte();
                //if (!bytecode14 && (Kind == Opcode.Cmp) != ((byte)ComparisonKind != 0))
                //    throw new IOException("Got unexpected comparison type in " + Kind.ToString().ToUpper(CultureInfo.InvariantCulture) + " (should be only in CMP)");
                byte TypePair = reader.ReadByte();
                Type1 = (DataType)(TypePair & 0xf);
                Type2 = (DataType)(TypePair >> 4);
#if DEBUG
                if (GetInstructionType(Kind) == InstructionType.SingleTypeInstruction && Type2 != (byte)0)
                    throw new IOException("Second type should be 0 in " + Kind.ToString().ToUpper(CultureInfo.InvariantCulture));
#endif
                //if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                if (reader.Bytecode14OrLower && Kind == Opcode.Cmp)
                    ComparisonKind = (ComparisonType)(reader.ReadByte() - 0x10);
                else
                    reader.Position++;

                if (Kind == Opcode.And || Kind == Opcode.Or)
                {
                    if (Type1 == DataType.Boolean && Type2 == DataType.Boolean)
                        reader.undertaleData.ShortCircuit = false;
                }
            }
                break;

            case InstructionType.GotoInstruction:
            {
                if (reader.Bytecode14OrLower)
                {
                    JumpOffset = reader.ReadInt24();
                    if (JumpOffset == -1048576) // magic? encoded in little endian as 00 00 F0, which is like below
                        JumpOffsetPopenvExitMagic = true;
                    reader.Position++;
                    break;
                }

                uint v = reader.ReadUInt24();

                JumpOffsetPopenvExitMagic = (v & 0x800000) != 0;

                // The rest is int23 signed value, so make sure
                uint r = v & 0x003FFFFF;
#if DEBUG
                if (JumpOffsetPopenvExitMagic && v != 0xF00000)
                    throw new Exception("Popenv magic doesn't work, call issue #90 again");
                else
#endif
                {
                    if ((v & 0x00C00000) != 0)
                        r |= 0xFFC00000;
                    JumpOffset = (int)r;
                }

                //if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                reader.Position++;
            }
                break;

            case InstructionType.PopInstruction:
            {
                TypeInst = (InstanceType)reader.ReadInt16();
                byte TypePair = reader.ReadByte();
                Type1 = (DataType)(TypePair & 0xf);
                Type2 = (DataType)(TypePair >> 4);
                //if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                reader.Position++;
                if (Type1 == DataType.Int16)
                {
                    // Special scenario - the swap instruction
                    // TODO: Figure out the proper syntax, see #129
                    SwapExtra = (ushort)TypeInst;
                    TypeInst = 0;
                }
                else
                {
                    Destination = reader.ReadUndertaleObject<Reference<UndertaleVariable>>();
                }
            }
                break;

            case InstructionType.PushInstruction:
            {
                short val = reader.ReadInt16();
                Type1 = (DataType)reader.ReadByte();
                if (reader.Bytecode14OrLower)
                {
                    if (Type1 == DataType.Variable)
                    {
                        switch (val)
                        {
                            case -5:
                                Kind = Opcode.PushGlb;
                                break;
                            case -6: // builtin
                                Kind = Opcode.PushBltn;
                                break;
                            case -7:
                                Kind = Opcode.PushLoc;
                                break;
                        }
                    }
                    else if (Type1 == DataType.Int16)
                    {
                        Kind = Opcode.PushI;
                    }
                }
                //if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                reader.Position++;
                switch (Type1)
                {
                    case DataType.Double:
                        Value = reader.ReadDouble();
                        break;
                    case DataType.Float:
                        Value = reader.ReadSingle();
                        break;
                    case DataType.Int32:
                        Value = reader.ReadInt32();
                        break;
                    case DataType.Int64:
                        Value = reader.ReadInt64();
                        break;
                    case DataType.Boolean:
                        Value = (reader.ReadUInt32() == 1); // TODO: double check
                        break;
                    case DataType.Variable:
                        TypeInst = (InstanceType)val;
                        Value = reader.ReadUndertaleObject<Reference<UndertaleVariable>>();
                        break;
                    case DataType.String:
                        Value = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>>();
                        break;
                    case DataType.Int16:
                        Value = val;
                        break;
                }
            }
                break;

            case InstructionType.CallInstruction:
            {
                ArgumentsCount = reader.ReadUInt16();
                Type1 = (DataType)reader.ReadByte();
                //if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                reader.Position++;
                Function = reader.ReadUndertaleObject<Reference<UndertaleFunction>>();
            }
                break;

            case InstructionType.BreakInstruction:
            {
                Value = reader.ReadInt16();
                Type1 = (DataType)reader.ReadByte();
                if (reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                if (Type1 == DataType.Int32)
                {
                    IntArgument = reader.ReadInt32();
                    if (!reader.undertaleData.IsVersionAtLeast(2023, 8))
                        reader.undertaleData.SetGMS2Version(2023, 8);
                    if (!reader.undertaleData.IsVersionAtLeast(2024, 4))
                    {
                        if (CheckIfAssetTypeIs2024_4(reader.undertaleData, IntArgument & 0xffffff, IntArgument >> 24))
                            reader.undertaleData.SetGMS2Version(2024, 4);
                    }
                }
                if (reader.undertaleData.IsVersionAtLeast(2, 3))
                {
                    if ((short)Value == -10) // chknullish instruction, added in 2.3.7
                    {
                        if (!reader.undertaleData.IsVersionAtLeast(2, 3, 7))
                            reader.undertaleData.SetGMS2Version(2, 3, 7);
                    }
                }
            }
                break;

            default:
                throw new IOException("Unknown opcode " + Kind.ToString().ToUpper(CultureInfo.InvariantCulture));
        }
    }
    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        long instructionStartAddress = reader.Position;
        reader.Position += 3; // skip for now, we'll read them later
        byte kind = reader.ReadByte();
        if (reader.Bytecode14OrLower)
        {
            // Convert opcode to our enum
            kind = ConvertInstructionKind(kind);
        }
        Opcode Kind = (Opcode)kind;
        reader.Position = instructionStartAddress;
        switch (GetInstructionType(Kind))
        {
            case InstructionType.SingleTypeInstruction:
            case InstructionType.DoubleTypeInstruction:
            case InstructionType.ComparisonInstruction:
            case InstructionType.GotoInstruction:
                reader.Position += 4;
                break;

            case InstructionType.PopInstruction:
                reader.Position += 2; // "TypeInst"
                int type1 = reader.ReadByte() & 0xf;
                if (type1 != 0x0f)
                {
                    reader.Position += 1 + 4;
                    return 1; // "Destination"
                }
                else
                    reader.Position++;
                break;

            case InstructionType.PushInstruction:
                {
                    reader.Position += 2;
                    DataType Type1 = (DataType)reader.ReadByte();
                    reader.Position++;
                    switch (Type1)
                    {
                        case DataType.Double:
                        case DataType.Int64:
                            reader.Position += 8;
                            break;

                        case DataType.Float:
                        case DataType.Int32:
                        case DataType.Boolean:
                            reader.Position += 4;
                            break;

                        case DataType.Variable:
                        case DataType.String:
                            reader.Position += 4;
                            return 1;
                    }
                }
                break;

            case InstructionType.CallInstruction:
                reader.Position += 8;
                return 1; // "Function"

            case InstructionType.BreakInstruction:
                {
                    reader.Position += 2;
                    DataType Type1 = (DataType)reader.ReadByte();
                    if (Type1 == DataType.Int32)
                        reader.Position += 5;
                    else
                        reader.Position += 1;
                    break;
                }

            default:
                throw new IOException("Unknown opcode " + Kind.ToString().ToUpper(CultureInfo.InvariantCulture));
        }

        return 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(null);
    }
    
    /// <summary>
    /// <inheritdoc cref="ToString()"/>
    /// </summary>
    /// <param name="code">The <see cref="UndertaleCode"/> code entry for which these instructions belong to.</param>
    /// <param name="blocks">A list of block addresses for the code entry for which these instructions belong to.</param>
    /// <returns></returns>
    public string ToString(UndertaleCode code, List<uint> blocks = null)
    {
        StringBuilder sb = new StringBuilder();
        ToString(sb, code, blocks);
        return sb.ToString();
    }
    
    /// <summary>
    /// Inserts a string representation of this object at a specified index in a <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/> instance on where to insert the string representation.</param>
    /// <param name="code"><inheritdoc cref="ToString(UndertaleCode, List{uint})"/></param>
    /// <param name="blocks"><inheritdoc cref="ToString(UndertaleCode, List{uint})"/></param>
    /// <param name="index">The index on where to insert the string representation. If this is <see langword="null"/>
    /// it will use <paramref name="stringBuilder.Length"/> as the index instead.</param>
    /// <remarks>Note that performance of this function can be drastically different, depending on <paramref name="index"/>.
    /// For best results, it's recommended to leave it at <see langword="null"/>.</remarks>
    public void ToString(StringBuilder stringBuilder, UndertaleCode code, List<uint> blocks = null, int? index = null)
    {
        if (index is null)
            index = stringBuilder.Length;

        StringBuilderHelper sbh = new StringBuilderHelper(index.Value);
        
        string kind = Kind.ToString();
        var type = GetInstructionType(Kind);
        bool unknownBreak = false;
        if (type == InstructionType.BreakInstruction)
        {
            if (!Assembler.BreakIDToName.TryGetValue((short)Value, out kind))
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

        switch (GetInstructionType(Kind))
        {
            case InstructionType.SingleTypeInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type1.ToOpcodeParam());

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
                sbh.Append(stringBuilder, Type1.ToOpcodeParam());
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type2.ToOpcodeParam());
                break;

            case InstructionType.ComparisonInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type1.ToOpcodeParam());
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type2.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                sbh.Append(stringBuilder, ComparisonKind.ToString());
                break;

            case InstructionType.GotoInstruction:
                sbh.Append(stringBuilder, ' ');
                string targetGoto;
                if (code is not null && Address + JumpOffset == code.Length / 4)
                    targetGoto = "[end]";
                else if (JumpOffsetPopenvExitMagic)
                    targetGoto = "<drop>";
                else if (blocks is not null)
                    targetGoto = $"[{blocks.IndexOf((uint)(Address + JumpOffset))}]";
                else
                    targetGoto = (Address + JumpOffset).ToString("D5");
                sbh.Append(stringBuilder, targetGoto);
                break;

            case InstructionType.PopInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type1.ToOpcodeParam());
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type2.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                if (Type1 == DataType.Int16)
                {
                    // Special scenario - the swap instruction
                    sbh.Append(stringBuilder, SwapExtra);
                }
                else
                {
                    if (Type1 == DataType.Variable && TypeInst != InstanceType.Undefined)
                    {
                        if (Destination.Type == VariableType.Instance)
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
                    sbh.Append(stringBuilder, Destination);
                }
                break;

            case InstructionType.PushInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type1.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                if (Type1 == DataType.Variable && TypeInst != InstanceType.Undefined)
                {
                    sbh.Append(stringBuilder, TypeInst.ToString().ToLower(CultureInfo.InvariantCulture));
                    sbh.Append(stringBuilder, '.');
                }
                if (Type1 == DataType.Int32)
                {
                    if (Value.GetType() == typeof(Reference<UndertaleFunction>))
                    {
                        sbh.Append(stringBuilder, "[function]");
                    }
                    else if (Value.GetType() == typeof(Reference<UndertaleVariable>))
                    {
                        sbh.Append(stringBuilder, "[variable]");
                        sbh.Append(stringBuilder, (Value as Reference<UndertaleVariable>).Target.Name?.Content ?? "<null>");
                        break;
                    }
                }
                sbh.Append(stringBuilder, (Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? Value.ToString());
                break;

            case InstructionType.CallInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type1.ToOpcodeParam());
                sbh.Append(stringBuilder, ' ');
                sbh.Append(stringBuilder, Function);
                sbh.Append(stringBuilder, "(argc=");
                sbh.Append(stringBuilder, ArgumentsCount);
                sbh.Append(stringBuilder, ')');
                break;

            case InstructionType.BreakInstruction:
                sbh.Append(stringBuilder, '.');
                sbh.Append(stringBuilder, Type1.ToOpcodeParam());
                if (unknownBreak)
                {
                    sbh.Append(stringBuilder, ' ');
                    sbh.Append(stringBuilder, Value);
                }
                if (Type1 == DataType.Int32)
                {
                    sbh.Append(stringBuilder, ' ');
                    if (Function != null)
                        sbh.Append(stringBuilder, Function);
                    else
                        sbh.Append(stringBuilder, IntArgument);
                }
                break;
        }
    }

    public uint CalculateInstructionSize()
    {
        if (GetReference<UndertaleVariable>() != null || GetReference<UndertaleFunction>() != null)
            return 2;
        else if (GetInstructionType(Kind) == InstructionType.PushInstruction)
            if (Type1 == DataType.Double || Type1 == DataType.Int64)
                return 3;
            else if (Type1 != DataType.Int16)
                return 2;
        if (Kind == Opcode.Break && Type1 == DataType.Int32)
            return 2;
        return 1;
    }

    // Underanalyzer implementations
    int IGMInstruction.Address => (int)Address * 4;
    IGMInstruction.Opcode IGMInstruction.Kind => (IGMInstruction.Opcode)Kind;
    IGMInstruction.ExtendedOpcode IGMInstruction.ExtKind => (IGMInstruction.ExtendedOpcode)Value;
    IGMInstruction.ComparisonType IGMInstruction.ComparisonKind => (IGMInstruction.ComparisonType)ComparisonKind;
    IGMInstruction.DataType IGMInstruction.Type1 => (IGMInstruction.DataType)Type1;
    IGMInstruction.DataType IGMInstruction.Type2 => (IGMInstruction.DataType)Type2;
    IGMInstruction.InstanceType IGMInstruction.InstType => (IGMInstruction.InstanceType)TypeInst;
    IGMVariable IGMInstruction.Variable => Destination?.Target ?? (Value as Reference<UndertaleVariable>)?.Target;
    IGMFunction IGMInstruction.Function => Function?.Target ?? (Value as Reference<UndertaleFunction>)?.Target;
    IGMInstruction.VariableType IGMInstruction.ReferenceVarType => (IGMInstruction.VariableType)(Destination?.Type ?? (Value as Reference<UndertaleVariable>)?.Type);
    double IGMInstruction.ValueDouble => (double)Value;
    short IGMInstruction.ValueShort => (short)Value;
    int IGMInstruction.ValueInt => (int)Value;
    long IGMInstruction.ValueLong => (long)Value;
    bool IGMInstruction.ValueBool => (bool)Value;
    IGMString IGMInstruction.ValueString => ((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)Value).Resource;
    int IGMInstruction.BranchOffset => JumpOffset * 4;
    bool IGMInstruction.PopWithContextExit => JumpOffsetPopenvExitMagic;
    byte IGMInstruction.DuplicationSize => Extra;
    byte IGMInstruction.DuplicationSize2 => (byte)(((byte)ComparisonKind & 0x7F) >> 3);
    int IGMInstruction.ArgumentCount => (Kind == Opcode.Call) ? ArgumentsCount : Extra;
    int IGMInstruction.PopSwapSize => SwapExtra;
    int IGMInstruction.AssetReferenceId => IntArgument & 0xffffff;
    AssetType IGMInstruction.GetAssetReferenceType(IGameContext context) => AdaptAssetType((context as GlobalDecompileContext).Data, IntArgument >> 24);

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
}

/// <summary>
/// A code entry in a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleCode : UndertaleNamedResource, UndertaleObjectWithBlobs, IDisposable, IGMCode
{
    /// <summary>
    /// The name of the code entry.
    /// </summary>
    public UndertaleString Name { get; set; }


    public uint Length { get; set; }

    public static int CurrCodeIndex { get; set; }


    /// <summary>
    /// The amount of local variables this code entry has. <br/>
    /// Warning: This is actually a ushort internally, it's an uint here for compatibility.
    /// </summary>
    public uint LocalsCount { get; set; }

    /// <summary>
    /// The amount of arguments this code entry accepts.
    /// </summary>
    public ushort ArgumentsCount { get; set; }

    public uint Offset { get; set; }


    /// <summary>
    /// A list of bytecode instructions this code entry has.
    /// </summary>
    public List<UndertaleInstruction> Instructions { get; } = new List<UndertaleInstruction>();
    public bool WeirdLocalFlag { get; set; }
    public UndertaleCode ParentEntry { get; set; } = null;
    public List<UndertaleCode> ChildEntries { get; set; } = new List<UndertaleCode>();

    internal uint _bytecodeAbsoluteAddress;
    internal byte[] _unsupportedBuffer;

    public void SerializeBlobBefore(UndertaleWriter writer)
    {
        if (writer.undertaleData.UnsupportedBytecodeVersion || writer.Bytecode14OrLower)
            return;
        if (ParentEntry != null)
        {
            // In GMS 2.3, code entries repeat often
            _bytecodeAbsoluteAddress = writer.LastBytecodeAddress;
            Length = writer.Position - _bytecodeAbsoluteAddress;
            // todo? set Flags to something else?
        }
        else
        {
            writer.LastBytecodeAddress = writer.Position;
            _bytecodeAbsoluteAddress = writer.Position;
            uint start = writer.Position;
            foreach (UndertaleInstruction instr in Instructions)
                writer.WriteUndertaleObject(instr);
            Length = writer.Position - start;
            // todo? clear Flags? how?
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        if (writer.undertaleData.UnsupportedBytecodeVersion)
        {
            Length = (uint)_unsupportedBuffer.Length;
            writer.Write(Length);
            writer.Write(_unsupportedBuffer);
        }
        else if (writer.Bytecode14OrLower)
        {
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
            writer.Write(Length);
            writer.Write((ushort)LocalsCount);
            writer.Write((ushort)(ArgumentsCount | (WeirdLocalFlag ? (ushort)0x8000 : 0)));
            int BytecodeRelativeAddress = (int)_bytecodeAbsoluteAddress - (int)writer.Position;
            writer.Write(BytecodeRelativeAddress);
            writer.Write(Offset);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Length = reader.ReadUInt32();
        if (reader.undertaleData.UnsupportedBytecodeVersion)
        {
            _unsupportedBuffer = reader.ReadBytes((int)Length);
        }
        else if (reader.Bytecode14OrLower)
        {
            Instructions.Clear();
            if (reader.InstructionArraysLengths is not null)
                Instructions.Capacity = reader.InstructionArraysLengths[CurrCodeIndex];

            long here = reader.AbsPosition;
            long stop = here + Length;
            while (reader.AbsPosition < stop)
            {
                uint a = (uint)(reader.AbsPosition - here) / 4;
                UndertaleInstruction instr = reader.ReadUndertaleObject<UndertaleInstruction>();
                instr.Address = a;
                Instructions.Add(instr);
            }
            WeirdLocalFlag = true;
        }
        else
        {
            LocalsCount = reader.ReadUInt16();
            ArgumentsCount = reader.ReadUInt16();
            if ((ArgumentsCount & 0x8000) == 0x8000)
            {
                ArgumentsCount &= 0x7FFF;
                WeirdLocalFlag = true;
            }
            int BytecodeRelativeAddress = reader.ReadInt32();
            _bytecodeAbsoluteAddress = (uint)((int)reader.AbsPosition - 4 + BytecodeRelativeAddress);
           
            if (Length > 0 && reader.undertaleData.IsVersionAtLeast(2, 3) && reader.GetOffsetMap().TryGetValue(_bytecodeAbsoluteAddress, out var i))
            {
                ParentEntry = (i as UndertaleInstruction).Entry;
                ParentEntry.ChildEntries.Add(this);

                Offset = reader.ReadUInt32();
                return;
            }

            long here = reader.AbsPosition;
            reader.AbsPosition = _bytecodeAbsoluteAddress;

            Instructions.Clear();
            if (reader.InstructionArraysLengths is not null)
                Instructions.Capacity = reader.InstructionArraysLengths[CurrCodeIndex];
            while (reader.AbsPosition < _bytecodeAbsoluteAddress + Length)
            {
                uint a = (uint)(reader.AbsPosition - _bytecodeAbsoluteAddress) / 4;
                UndertaleInstruction instr = reader.ReadUndertaleObject<UndertaleInstruction>();
                instr.Address = a;
                Instructions.Add(instr);
            }
            if (ParentEntry == null && Instructions.Count != 0)
                Instructions[0].Entry = this;

            reader.AbsPosition = here;
            Offset = reader.ReadUInt32();
        }

        if (reader.InstructionArraysLengths is not null)
            CurrCodeIndex++;
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 4; // "Name"
        uint length = reader.ReadUInt32();

        if (reader.Bytecode14OrLower)
        {
            long here = reader.Position;
            long stop = here + length;

            // Get instructions count
            uint instrCount = 0;
            uint instrSubCount = 0;
            while (reader.Position < stop)
            {
                instrCount++;
                instrSubCount += UndertaleInstruction.UnserializeChildObjectCount(reader);
            }

            reader.InstructionArraysLengths[CurrCodeIndex] = (int)instrCount;

            count += instrCount + instrSubCount;
        }
        else
        {
            reader.Position += 4;

            int bytecodeRelativeAddress = reader.ReadInt32();
            uint bytecodeAbsoluteAddress = (uint)((int)reader.Position - 4 + bytecodeRelativeAddress);

            if (length == 0 || reader.GMS2BytecodeAddresses.Contains(bytecodeAbsoluteAddress))
            {
                reader.Position += 4; // "Offset"
                return count;
            }

            reader.GMS2BytecodeAddresses.Add(bytecodeAbsoluteAddress);

            long here = reader.Position;
            reader.Position = bytecodeAbsoluteAddress;

            // Get instructions counts
            uint instrCount = 0;
            uint instrSubCount = 0;
            while (reader.Position < bytecodeAbsoluteAddress + length)
            {
                instrCount++;
                instrSubCount += UndertaleInstruction.UnserializeChildObjectCount(reader);
            }

            reader.InstructionArraysLengths[CurrCodeIndex] = (int)instrCount;

            reader.Position = here;
            reader.Position += 4; // "Offset"

            count += instrCount + instrSubCount;
        }

        CurrCodeIndex++;

        return count;
    }

    public void UpdateAddresses()
    {
        uint addr = 0;
        foreach (UndertaleInstruction instr in Instructions)
        {
            instr.Address = addr;
            addr += instr.CalculateInstructionSize();
        }
        Length = addr * 4;
    }

    public UndertaleInstruction GetInstructionFromAddress(uint address)
    {
        UpdateAddresses();
        foreach (UndertaleInstruction instr in Instructions)
            if (instr.Address == address)
                return instr;
        return null;
    }

    public UndertaleInstruction GetInstructionBeforeAddress(uint address)
    {
        UpdateAddresses();
        foreach (UndertaleInstruction instr in Instructions)
            if (instr.Address + instr.CalculateInstructionSize() == address)
                return instr;
        return null;
    }

    /// <summary>
    /// Finds and returns a list of all variables this code entry references.
    /// </summary>
    /// <returns>A list of all variables this code entry references.</returns>
    public IList<UndertaleVariable> FindReferencedVars()
    {
        List<UndertaleVariable> vars = new List<UndertaleVariable>();
        foreach (UndertaleInstruction instr in Instructions)
        {
            var v = instr.GetReference<UndertaleVariable>()?.Target;
            if (v != null && !vars.Contains(v))
                vars.Add(v);
        }
        return vars;
    }

    /// <summary>
    /// Finds and returns a list of all local variables this code entry references.
    /// </summary>
    /// <returns>A list of all local variables this code entry references.</returns>
    public IList<UndertaleVariable> FindReferencedLocalVars()
    {
        return FindReferencedVars().Where((x) => x.InstanceType == UndertaleInstruction.InstanceType.Local).ToList();
    }

    /// <summary>
    /// Append instructions at the end of this code entry.
    /// </summary>
    /// <param name="instructions">The instructions to append.</param>
    public void Append(IList<UndertaleInstruction> instructions)
    {
        if (ParentEntry is not null)
            return;

        Instructions.AddRange(instructions);
        UpdateAddresses();
    }

    /// <summary>
    /// Replaces <b>all</b> instructions currently existing in this code entry with another set of instructions.
    /// </summary>
    /// <param name="instructions">The new instructions for this code entry.</param>
    public void Replace(IList<UndertaleInstruction> instructions)
    {
        if (ParentEntry is not null)
            return;

        Instructions.Clear();
        Append(instructions);
    }

    /// <summary>
    /// Append GML instructions at the end of this code entry.
    /// </summary>
    /// <param name="gmlCode">The GML code to append.</param>
    /// <param name="data">From which data file the GML code is coming from.</param>
    /// <exception cref="Exception"> if the GML code does not compile or if there's an error writing the code to the profile entry.</exception>
    public void AppendGML(string gmlCode, UndertaleData data)
    {
        if (ParentEntry is not null)
            return;

        CompileContext context = Compiler.Compiler.CompileGMLText(gmlCode, data, this);
        if (!context.SuccessfulCompile || context.HasError)
        {
            Console.WriteLine(gmlCode);
            throw new Exception("GML Compile Error: " + context.ResultError);
        }

        Append(context.ResultAssembly);

        data.GMLCacheChanged?.Add(Name?.Content);

        try
        {
            // Attempt to write text in all modes, because this is a special case.
            string tempPath = Path.Combine(data.ToolInfo.AppDataProfiles, data.ToolInfo.CurrentMD5, "Temp", Name?.Content + ".gml");
            if (File.Exists(tempPath))
            {
                string readText = File.ReadAllText(tempPath) + "\n" + gmlCode;
                File.WriteAllText(tempPath, readText);
            }
        }
        catch (Exception exc)
        {
            throw new Exception("Error during writing of GML code to profile:\n" + exc);
        }
    }

    /// <summary>
    /// Replaces <b>all</b> instructions currently existing in this code entry with another set of GML instructions.
    /// </summary>
    /// <param name="gmlCode">The new GML code for this code entry.</param>
    /// <param name="data">From which data file the GML code is coming from.</param>
    /// <exception cref="Exception">If the GML code does not compile or if there's an error writing the code to the profile entry.</exception>
    public void ReplaceGML(string gmlCode, UndertaleData data)
    {
        if (ParentEntry is not null)
            return;

        CompileContext context = Compiler.Compiler.CompileGMLText(gmlCode, data, this);
        if (!context.SuccessfulCompile || context.HasError)
        {
            Console.WriteLine(gmlCode);
            throw new Exception("GML Compile Error: " + context.ResultError);
        }

        Replace(context.ResultAssembly);

        data.GMLCacheChanged?.Add(Name?.Content);

        //TODO: only do this if profile mode is enabled in the first place
        try
        {
            // When necessary, write to profile.
            string tempPath = Path.Combine(data.ToolInfo.AppDataProfiles, data.ToolInfo.CurrentMD5, "Temp", Name?.Content + ".gml");
            if (data.ToolInfo.ProfileMode || File.Exists(tempPath))
                File.WriteAllText(tempPath, gmlCode);
        }
        catch (Exception exc)
        {
            throw new Exception("Error during writing of GML code to profile:\n" + exc);
        }
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