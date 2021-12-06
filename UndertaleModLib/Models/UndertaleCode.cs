using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Models
{
    public class UndertaleInstruction : UndertaleObject
    {
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

                _ => throw new IOException("Unknown opcode " + op.ToString().ToUpper()),
            };
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
        public int JumpOffset { get; set; }
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
                NextOccurrenceOffset = (uint)int32Value & 0x00FFFFFF;
                Type = (VariableType)(int32Value >> 24);
            }

            public void Serialize(UndertaleWriter writer)
            {
                NextOccurrenceOffset = 0xdead;
                writer.WriteUInt24(NextOccurrenceOffset);
                writer.Write((byte)Type);
            }

            public void Unserialize(UndertaleReader reader)
            {
                NextOccurrenceOffset = reader.ReadUInt24();
                Type = (VariableType)reader.ReadByte();
            }

            public override string ToString()
            {
                if (typeof(T) == typeof(UndertaleVariable) && Type != VariableType.Normal)
                {
                    return String.Format("[{0}]{1}{2}", Type.ToString().ToLower(), ((Target as UndertaleVariable)?.InstanceType.ToString().ToLower() ?? "null") + ".", Target?.ToString() ?? "(null)");
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
                            writer.Position = writer.GetAddressForUndertaleObject(references[var][i].GetReference<T>());
                            writer.WriteInt24(addrDiff);
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
                    reference = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr).GetReference<T>(obj is UndertaleFunction);
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
            if (allowResolve && res == null && Value is int val)
            {
                Value = new Reference<T>(val);
                return (Reference<T>)Value;
            }
            return res;
        }

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
                    }
                    break;

                default:
                    throw new IOException("Unknown opcode " + Kind.ToString().ToUpper());
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint instructionStartAddress = reader.Position;
            reader.Position += 3; // skip for now, we'll read them later
            byte kind = reader.ReadByte();
            if (reader.Bytecode14OrLower)
            {
                // Convert opcode to our enum
                switch (kind)
                {
                    case 0x03:
                        kind = 0x07;
                        break;
                    case 0x04:
                        kind = 0x08;
                        break;
                    case 0x05:
                        kind = 0x09;
                        break;
                    case 0x06:
                        kind = 0x0A;
                        break;
                    case 0x07:
                        kind = 0x0B;
                        break;
                    case 0x08:
                        kind = 0x0C;
                        break;
                    case 0x09:
                        kind = 0x0D;
                        break;
                    case 0x0A:
                        kind = 0x0E;
                        break;
                    case 0x0B:
                        kind = 0x0F;
                        break;
                    case 0x0C:
                        kind = 0x10;
                        break;
                    case 0x0D:
                        kind = 0x11;
                        break;
                    case 0x0E:
                        kind = 0x12;
                        break;
                    case 0x0F:
                        kind = 0x13;
                        break;
                    case 0x10:
                        kind = 0x14;
                        break;
                    case 0x11:
                    case 0x12:
                    case 0x13:
                    case 0x14:
                    //  case 0x15:
                    case 0x16:
                        kind = 0x15;
                        break;
                    case 0xDA:
                        kind = 0xD9;
                        break;
                    case 0x41:
                        kind = 0x45;
                        break;
                    case 0x82:
                        kind = 0x86;
                        break;
                    case 0xB7:
                        kind = 0xB6;
                        break;
                    case 0xB8:
                        kind = 0xB7;
                        break;
                    case 0xB9:
                        kind = 0xB8;
                        break;
                    case 0x9D:
                        kind = 0x9C;
                        break;
                    case 0x9E:
                        kind = 0x9D;
                        break;
                    case 0x9F:
                        kind = 0x9E;
                        break;
                    case 0xBB:
                        kind = 0xBA;
                        break;
                    case 0xBC:
                        kind = 0xBB;
                        break;
                }
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
                            throw new IOException("Invalid padding in " + Kind.ToString().ToUpper());
#endif
                        ComparisonKind = (ComparisonType)reader.ReadByte();
                        //if (!bytecode14 && (Kind == Opcode.Cmp) != ((byte)ComparisonKind != 0))
                        //    throw new IOException("Got unexpected comparison type in " + Kind.ToString().ToUpper() + " (should be only in CMP)");
                        byte TypePair = reader.ReadByte();
                        Type1 = (DataType)(TypePair & 0xf);
                        Type2 = (DataType)(TypePair >> 4);
#if DEBUG
                        if (GetInstructionType(Kind) == InstructionType.SingleTypeInstruction && Type2 != (byte)0)
                            throw new IOException("Second type should be 0 in " + Kind.ToString().ToUpper());
#endif
                        //if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                        if (reader.Bytecode14OrLower && Kind == Opcode.Cmp)
                            ComparisonKind = (ComparisonType)(reader.ReadByte() - 0x10);
                        else
                            reader.ReadByte();

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
                            reader.ReadByte();
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
                        reader.ReadByte();
                    }
                    break;

                case InstructionType.PopInstruction:
                    {
                        TypeInst = (InstanceType)reader.ReadInt16();
                        byte TypePair = reader.ReadByte();
                        Type1 = (DataType)(TypePair & 0xf);
                        Type2 = (DataType)(TypePair >> 4);
                        //if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                        reader.ReadByte();
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
                        reader.ReadByte();
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
                        reader.ReadByte();
                        Function = reader.ReadUndertaleObject<Reference<UndertaleFunction>>();
                    }
                    break;

                case InstructionType.BreakInstruction:
                    {
                        Value = reader.ReadInt16();
                        Type1 = (DataType)reader.ReadByte();
                        if (reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                    }
                    break;

                default:
                    throw new IOException("Unknown opcode " + Kind.ToString().ToUpper());
            }
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(UndertaleCode code, List<uint> blocks = null)
        {
            StringBuilder sb = new StringBuilder();

            string kind = Kind.ToString();
            var type = GetInstructionType(Kind);
            bool unknownBreak = false;
            if (type == InstructionType.BreakInstruction)
            {
                if (!Assembler.BreakIDToName.TryGetValue((short)Value, out kind))
                {
                    kind = kind.ToLower();
                    unknownBreak = true;
                }
            }
            else
                kind = kind.ToLower();
            sb.Append(kind);

            switch (GetInstructionType(Kind))
            {
                case InstructionType.SingleTypeInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());

                    if (Kind == Opcode.Dup || Kind == Opcode.CallV)
                    {
                        sb.Append(' ');
                        sb.Append(Extra.ToString());
                        if (Kind == Opcode.Dup)
                        {
                            if ((byte)ComparisonKind != 0)
                            {
                                // Special dup instruction with extra parameters
                                sb.Append(' ');
                                sb.Append((byte)ComparisonKind & 0x7F);
                                sb.Append(" ;;; this is a weird GMS2.3+ swap instruction");
                            }
                        }
                    }
                    break;

                case InstructionType.DoubleTypeInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append("." + Type2.ToOpcodeParam());
                    break;

                case InstructionType.ComparisonInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append("." + Type2.ToOpcodeParam());
                    sb.Append(' ');
                    sb.Append(ComparisonKind.ToString());
                    break;

                case InstructionType.GotoInstruction:
                    sb.Append(' ');
                    string tgt;
                    if (code != null && Address + JumpOffset == code.Length / 4)
                        tgt = "[end]";
                    else if (JumpOffsetPopenvExitMagic)
                        tgt = "<drop>";
                    else if (blocks != null)
                        tgt = "[" + blocks.IndexOf((uint)(Address + JumpOffset)) + "]";
                    else
                        tgt = (Address + JumpOffset).ToString("D5");
                    sb.Append(tgt);
                    break;

                case InstructionType.PopInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append("." + Type2.ToOpcodeParam());
                    sb.Append(' ');
                    if (Type1 == DataType.Int16)
                    {
                        // Special scenario - the swap instruction
                        // TODO: Figure out the proper syntax, see #129
                        sb.Append(SwapExtra.ToString());
                        sb.Append(" ;;; this is a weird swap instruction, see #129");
                    }
                    else
                    {
                        if (Type1 == DataType.Variable && TypeInst != InstanceType.Undefined)
                        {
                            sb.Append(TypeInst.ToString().ToLower());
                            sb.Append('.');
                        }
                        sb.Append(Destination.ToString());
                    }
                    break;

                case InstructionType.PushInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append(' ');
                    if (Type1 == DataType.Variable && TypeInst != InstanceType.Undefined)
                    {
                        sb.Append(TypeInst.ToString().ToLower());
                        sb.Append('.');
                    }
                    sb.Append((Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? Value.ToString());
                    break;

                case InstructionType.CallInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append(' ');
                    sb.Append(Function.ToString());
                    sb.Append("(argc=");
                    sb.Append(ArgumentsCount.ToString());
                    sb.Append(')');
                    break;

                case InstructionType.BreakInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    if (unknownBreak)
                    {
                        sb.Append(" ");
                        sb.Append(Value.ToString());
                    }
                    break;
            }
            return sb.ToString();
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
            return 1;
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
                _ => type.ToString().ToLower(),
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

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleCode : UndertaleNamedResource, UndertaleObjectWithBlobs
    {
        public UndertaleString Name { get; set; }
        public uint Length { get; set; }
        public uint LocalsCount { get; set; } // Warning: Actually a ushort, left this way for compatibility
        public ushort ArgumentsCount { get; set; }
        public bool WeirdLocalsFlag { get; set; }
        public uint Offset { get; set; }
        public List<UndertaleInstruction> Instructions { get; } = new List<UndertaleInstruction>();
        public bool WeirdLocalFlag { get; set; }

        public UndertaleCode ParentEntry { get; set; } = null;
        public List<UndertaleCode> ChildEntries { get; set; } = new List<UndertaleCode>();

        internal uint _BytecodeAbsoluteAddress;
        internal byte[] _UnsupportedBuffer;

        public void SerializeBlobBefore(UndertaleWriter writer)
        {
            if (writer.undertaleData.UnsupportedBytecodeVersion || writer.Bytecode14OrLower)
                return;
            if (ParentEntry != null)
            {
                // In GMS 2.3, code entries repeat often
               _BytecodeAbsoluteAddress = writer.LastBytecodeAddress;
                Length = writer.Position - _BytecodeAbsoluteAddress;
                // todo? set Flags to something else?
            }
            else
            {
                writer.LastBytecodeAddress = writer.Position;
                _BytecodeAbsoluteAddress = writer.Position;
                uint start = writer.Position;
                foreach (UndertaleInstruction instr in Instructions)
                    writer.WriteUndertaleObject(instr);
                Length = writer.Position - start;
                // todo? clear Flags? how?
            }
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            if (writer.undertaleData.UnsupportedBytecodeVersion)
            {
                Length = (uint)_UnsupportedBuffer.Length;
                writer.Write(Length);
                writer.Write(_UnsupportedBuffer);
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
                int BytecodeRelativeAddress = (int)_BytecodeAbsoluteAddress - (int)writer.Position;
                writer.Write(BytecodeRelativeAddress);
                writer.Write(Offset);
            }

        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Length = reader.ReadUInt32();
            if (reader.undertaleData.UnsupportedBytecodeVersion)
            {
                _UnsupportedBuffer = reader.ReadBytes((int)Length);
            }
            else if (reader.Bytecode14OrLower)
            {
                Instructions.Clear();
                uint here = reader.Position;
                uint stop = here + Length;
                while (reader.Position < stop)
                {
                    uint a = (reader.Position - here) / 4;
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
                _BytecodeAbsoluteAddress = (uint)((int)reader.Position - 4 + BytecodeRelativeAddress);
                uint here = reader.Position;
                reader.Position = _BytecodeAbsoluteAddress;
                if (Length > 0 && reader.GMS2_3 && reader.GetOffsetMap().TryGetValue(_BytecodeAbsoluteAddress, out var i))
                {
                    ParentEntry = (i as UndertaleInstruction).Entry;
                    ParentEntry.ChildEntries.Add(this);
                }
                Instructions.Clear();
                while (reader.Position < _BytecodeAbsoluteAddress + Length)
                {
                    uint a = (reader.Position - _BytecodeAbsoluteAddress) / 4;
                    UndertaleInstruction instr = reader.ReadUndertaleObject<UndertaleInstruction>();
                    instr.Address = a;
                    Instructions.Add(instr);
                }
                if (ParentEntry == null && Instructions.Count != 0)
                    Instructions[0].Entry = this;
                reader.Position = here;
                Offset = reader.ReadUInt32();
            }
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

        public IList<UndertaleVariable> FindReferencedLocalVars()
        {
            return FindReferencedVars().Where((x) => x.InstanceType == UndertaleInstruction.InstanceType.Local).ToList();
        }

        public void Append(IList<UndertaleInstruction> instructions)
        {
            Instructions.AddRange(instructions);
            UpdateAddresses();
        }

        public void Replace(IList<UndertaleInstruction> instructions)
        {
            Instructions.Clear();
            Append(instructions);
        }

        public void AppendGML(string gmlCode, UndertaleData data)
        {
            CompileContext context = Compiler.Compiler.CompileGMLText(gmlCode, data, this);
            if (!context.SuccessfulCompile || context.HasError)
            {
                Console.WriteLine(gmlCode);
                throw new Exception("GML Compile Error: " + context.ResultError);
            }

            Append(context.ResultAssembly);

            try
            {
                // Attempt to write text in all modes, because this is a special case.
                string tempPath = Path.Combine(data.ToolInfo.AppDataProfiles, data.ToolInfo.CurrentMD5, "Temp", Name.Content + ".gml");
                if (File.Exists(tempPath))
                {
                    string readText = File.ReadAllText(tempPath) + "\n" + gmlCode;
                    File.WriteAllText(tempPath, readText);
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Error during writing of GML code to profile:\n" + exc.ToString());
            }
        }

        public void ReplaceGML(string gmlCode, UndertaleData data)
        {
            CompileContext context = Compiler.Compiler.CompileGMLText(gmlCode, data, this);
            if (!context.SuccessfulCompile || context.HasError)
            {
                Console.WriteLine(gmlCode);
                throw new Exception("GML Compile Error: " + context.ResultError);
            }

            Replace(context.ResultAssembly);

            try
            {
                // When necessary, write to profile.
                string tempPath = Path.Combine(data.ToolInfo.AppDataProfiles, data.ToolInfo.CurrentMD5, "Temp", Name.Content + ".gml");
                if (!data.GMS2_3 && (data.ToolInfo.ProfileMode || File.Exists(tempPath)))
                    File.WriteAllText(tempPath, gmlCode);
            }
            catch (Exception exc)
            {
                throw new Exception("Error during writing of GML code to profile:\n" + exc.ToString());
            }
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
