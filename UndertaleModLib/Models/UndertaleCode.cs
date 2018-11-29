using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            PushVar = 0xC3, // Push(Value) // push other variable
            PushI = 0x84, // Push(Value) // push int16
            Call = 0xD9, // Function(arg0, arg1, ..., argn) where arg = Pop() and n = ArgumentsCount
            Break = 0xFF, // Invalid access guard?
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
            switch(op)
            {
                case Opcode.Neg:
                case Opcode.Not:
                case Opcode.Dup:
                case Opcode.Ret:
                case Opcode.Exit:
                case Opcode.Popz:
                    return InstructionType.SingleTypeInstruction;

                case Opcode.Conv:
                case Opcode.Mul:
                case Opcode.Div:
                case Opcode.Rem:
                case Opcode.Mod:
                case Opcode.Add:
                case Opcode.Sub:
                case Opcode.And:
                case Opcode.Or:
                case Opcode.Xor:
                case Opcode.Shl:
                case Opcode.Shr:
                    return InstructionType.DoubleTypeInstruction;

                case Opcode.Cmp:
                    return InstructionType.ComparisonInstruction;
                    
                case Opcode.B:
                case Opcode.Bt:
                case Opcode.Bf:
                case Opcode.PushEnv:
                case Opcode.PopEnv:
                    return InstructionType.GotoInstruction;

                case Opcode.Pop:
                    return InstructionType.PopInstruction;

                case Opcode.Push:
                case Opcode.PushLoc:
                case Opcode.PushGlb:
                case Opcode.PushVar:
                case Opcode.PushI:
                    return InstructionType.PushInstruction;

                case Opcode.Call:
                    return InstructionType.CallInstruction;

                case Opcode.Break:
                    return InstructionType.BreakInstruction;

                default:
                    throw new IOException("Unknown opcode " + op.ToString().ToUpper());
            }
        }

        public static int CalculateStackDiff(UndertaleInstruction instr)
        {
            switch (instr.Kind)
            {
                case Opcode.Neg:
                case Opcode.Not:
                    return 0;

                case Opcode.Dup:
                    return 1 + instr.DupExtra;

                case Opcode.Ret:
                    return -1;

                case Opcode.Exit:
                    return 0;

                case Opcode.Popz:
                    return -1;

                case Opcode.Conv:
                    return 0;

                case Opcode.Mul:
                case Opcode.Div:
                case Opcode.Rem:
                case Opcode.Mod:
                case Opcode.Add:
                case Opcode.Sub:
                case Opcode.And:
                case Opcode.Or:
                case Opcode.Xor:
                case Opcode.Shl:
                case Opcode.Shr:
                case Opcode.Cmp:
                    return -2 + 1;

                case Opcode.B:
                    return 0;
                case Opcode.Bt:
                case Opcode.Bf:
                case Opcode.PushEnv:
                    return -1;
                case Opcode.PopEnv:
                    return 0;

                case Opcode.Pop:
                    if (instr.Destination.Type == VariableType.StackTop)
                        return -1 - 1;
                    if (instr.Destination.Type == VariableType.Array)
                        return -1 - 2;
                    return -1;

                case Opcode.Push:
                case Opcode.PushLoc:
                case Opcode.PushGlb:
                case Opcode.PushVar:
                case Opcode.PushI:
                    if (instr.Value is Reference<UndertaleVariable>)
                    {
                        if ((instr.Value as Reference<UndertaleVariable>).Type == VariableType.StackTop)
                            return 1 - 1;
                        if ((instr.Value as Reference<UndertaleVariable>).Type == VariableType.Array)
                            return 1 - 2;
                    }
                    return 1;

                case Opcode.Call:
                    return -instr.ArgumentsCount + 1;

                case Opcode.Break:
                    return 0;

                default:
                    throw new IOException("Unknown opcode " + instr.Kind.ToString().ToUpper());
            }
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

            // anything > 0 => GameObjectIndex
        }

        public enum VariableType : byte
        {
            Array,
            StackTop = 0x80,
            Normal = 0xA0,
            Unknown = 0xE0,  // room scope?
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
        public byte DupExtra { get; set; }

        public interface ReferencedObject
        {
            uint Occurrences { get; set; }
            UndertaleInstruction FirstAddress { get; set; }
            int UnknownChainEndingValue { get; set; }
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
                    return String.Format("[{0}]{1}", Type.ToString().ToLower(), Target?.ToString() ?? "(null)");
                else
                    return String.Format("{0}", Target?.ToString() ?? "(null)");
            }

            public static Dictionary<T, List<UndertaleInstruction>> CollectReferences(IList<UndertaleCode> codes)
            {
                Dictionary<T, List<UndertaleInstruction>> list = new Dictionary<T, List<UndertaleInstruction>>();
                foreach(UndertaleCode code in codes)
                {
                    foreach(UndertaleInstruction instr in code.Instructions)
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
                                addrDiff = var.UnknownChainEndingValue;
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
                    reference = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr).GetReference<T>();
                    if (reference == null)
                        throw new IOException("Failed to find reference at " + addr);
                    reference.Target = obj;
                    addr += (uint)reference.NextOccurrenceOffset;
                }
                obj.UnknownChainEndingValue = (int)reference.NextOccurrenceOffset;
            }
        }

        public Reference<T> GetReference<T>() where T : class, UndertaleObject, ReferencedObject
        {
            return (Destination as Reference<T>) ?? (Function as Reference<T>) ?? (Value as Reference<T>);
        }

        public void Serialize(UndertaleWriter writer)
        {
            switch (GetInstructionType(Kind))
            {
                case InstructionType.SingleTypeInstruction:
                case InstructionType.DoubleTypeInstruction:
                case InstructionType.ComparisonInstruction:
                    {
                        writer.Write(DupExtra);
                        writer.Write((byte)ComparisonKind);
                        byte TypePair = (byte)((byte)Type2 << 4 | (byte)Type1);
                        writer.Write(TypePair);
                        writer.Write((byte)Kind);
                    }
                    break;
                    
                case InstructionType.GotoInstruction:
                    {
                        // See unserialize
                        if (JumpOffsetPopenvExitMagic)
                        {
                            writer.WriteInt24(0xF00000);
                        }
                        else
                        {
                            uint JumpOffsetFixed = (uint)JumpOffset;
                            JumpOffsetFixed &= ~0xFF800000;
                            writer.WriteInt24((int)JumpOffsetFixed);
                        }

                        writer.Write((byte)Kind);
                    }
                    break;

                case InstructionType.PopInstruction:
                    {
                        writer.Write((short)TypeInst);
                        byte TypePair = (byte)((byte)Type2 << 4 | (byte)Type1);
                        writer.Write(TypePair);
                        writer.Write((byte)Kind);
                        writer.WriteUndertaleObject(Destination);
                    }
                    break;

                case InstructionType.PushInstruction:
                    {
                        if (Type1 == DataType.Int16)
                        {
                            Debug.Assert(Value.GetType() == typeof(short));
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
                        writer.Write((byte)Kind);
                        switch (Type1)
                        {
                            case DataType.Double:
                                Debug.Assert(Value.GetType() == typeof(double));
                                writer.Write((double)Value);
                                break;
                            case DataType.Float:
                                Debug.Assert(Value.GetType() == typeof(float));
                                writer.Write((float)Value);
                                break;
                            case DataType.Int32:
                                Debug.Assert(Value.GetType() == typeof(int));
                                writer.Write((int)Value);
                                break;
                            case DataType.Int64:
                                Debug.Assert(Value.GetType() == typeof(long));
                                writer.Write((long)Value);
                                break;
                            case DataType.Boolean:
                                Debug.Assert(Value.GetType() == typeof(bool));
                                writer.Write((bool)Value ? 1 : 0);
                                break;
                            case DataType.Variable:
                                Debug.Assert(Value.GetType() == typeof(Reference<UndertaleVariable>));
                                writer.WriteUndertaleObject((Reference<UndertaleVariable>)Value);
                                break;
                            case DataType.String:
                                Debug.Assert(Value.GetType() == typeof(UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>));
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
                        writer.Write((byte)Kind);
                        writer.WriteUndertaleObject(Function);
                    }
                    break;

                case InstructionType.BreakInstruction:
                    {
                        Debug.Assert(Value.GetType() == typeof(short));
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
            reader.ReadByte(); reader.ReadByte(); reader.ReadByte(); // skip for now, we'll read them later
            Kind = (Opcode)reader.ReadByte();
            reader.Position = instructionStartAddress;
            switch (GetInstructionType(Kind))
            {
                case InstructionType.SingleTypeInstruction:
                case InstructionType.DoubleTypeInstruction:
                case InstructionType.ComparisonInstruction:
                    {
                        DupExtra = reader.ReadByte();
                        if (DupExtra != 0 && Kind != Opcode.Dup)
                            throw new IOException("Invalid padding in " + Kind.ToString().ToUpper());
                        ComparisonKind = (ComparisonType)reader.ReadByte();
                        if ((Kind == Opcode.Cmp) != ((byte)ComparisonKind != 0))
                            throw new IOException("Got unexpected comparison type in " + Kind.ToString().ToUpper() + " (should be only in CMP)");
                        byte TypePair = reader.ReadByte();
                        Type1 = (DataType)(TypePair & 0xf);
                        Type2 = (DataType)(TypePair >> 4);
                        if (GetInstructionType(Kind) == InstructionType.SingleTypeInstruction && Type2 != (byte)0)
                            throw new IOException("Second type should be 0 in " + Kind.ToString().ToUpper());
                        if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                    }
                    break;

                case InstructionType.GotoInstruction:
                    {
                        uint v = reader.ReadUInt24();

                        JumpOffsetPopenvExitMagic = (v & 0x800000) != 0;

                        // The rest is int23 signed value, so make sure
                        uint r = v & 0x003FFFFF;
                        if (JumpOffsetPopenvExitMagic && v != 0xF00000)
                            throw new Exception("Popenv magic doesn't work, call issue #90 again");
                        else
                        {
                            if ((v & 0x00C00000) != 0)
                                r |= 0xFFC00000;
                            JumpOffset = (int)r;
                        }

                        if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                    }
                    break;

                case InstructionType.PopInstruction:
                    {
                        TypeInst = (InstanceType)reader.ReadInt16();
                        byte TypePair = reader.ReadByte();
                        Type1 = (DataType)(TypePair & 0xf);
                        Type2 = (DataType)(TypePair >> 4);
                        if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                        Destination = reader.ReadUndertaleObject<Reference<UndertaleVariable>>();
                    }
                    break;

                case InstructionType.PushInstruction:
                    {
                        short val = reader.ReadInt16();
                        Type1 = (DataType)reader.ReadByte();
                        if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
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
                        if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                        Function = reader.ReadUndertaleObject<Reference<UndertaleFunction>>();
                    }
                    break;

                case InstructionType.BreakInstruction:
                    {
                        Value = reader.ReadInt16();
                        Type1 = (DataType)reader.ReadByte();
                        if(reader.ReadByte() != (byte)Kind) throw new Exception("really shouldn't happen");
                    }
                    break;

                default:
                    throw new IOException("Unknown opcode " + Kind.ToString().ToUpper());
            }
        }

        public override string ToString()
        {
            return ToString(null, null);
        }

        public string ToString(UndertaleCode code, IList<UndertaleVariable> vars)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Address.ToString("D5") + ": ");
            sb.Append(Kind.ToString().ToLower());

            switch (GetInstructionType(Kind))
            {
                case InstructionType.SingleTypeInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());

                    if (Kind == Opcode.Dup)
                    {
                        sb.Append(" ");
                        sb.Append(DupExtra.ToString());
                    }
                    break;

                case InstructionType.DoubleTypeInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append("." + Type2.ToOpcodeParam());
                    break;

                case InstructionType.ComparisonInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append("." + Type2.ToOpcodeParam());
                    sb.Append(" ");
                    sb.Append(ComparisonKind.ToString());
                    break;

                case InstructionType.GotoInstruction:
                    sb.Append(" ");
                    string tgt = (Address + JumpOffset).ToString("D5");
                    if (code != null && Address + JumpOffset == code.Length / 4)
                        tgt = "func_end";
                    if (JumpOffsetPopenvExitMagic)
                        tgt = "[drop]";
                    sb.Append(tgt);
                    break;

                case InstructionType.PopInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append("." + Type2.ToOpcodeParam());
                    sb.Append(" ");
                    if (Type1 == DataType.Variable && TypeInst != InstanceType.Undefined)
                    {
                        sb.Append(TypeInst.ToString().ToLower());
                        sb.Append(".");
                    }
                    sb.Append(Destination.ToString());
                    break;

                case InstructionType.PushInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append(" ");
                    if (Type1 == DataType.Variable && TypeInst != InstanceType.Undefined)
                    {
                        sb.Append(TypeInst.ToString().ToLower());
                        sb.Append(".");
                    }
                    sb.Append((Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? Value.ToString());
                    break;

                case InstructionType.CallInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append(" ");
                    sb.Append(Function.ToString());
                    sb.Append("(argc=");
                    sb.Append(ArgumentsCount.ToString());
                    sb.Append(")");
                    break;

                case InstructionType.BreakInstruction:
                    sb.Append("." + Type1.ToOpcodeParam());
                    sb.Append(" ");
                    sb.Append(Value.ToString());
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
            switch(type)
            {
                case UndertaleInstruction.DataType.Double:
                    return "d";
                case UndertaleInstruction.DataType.Float:
                    return "f";
                case UndertaleInstruction.DataType.Int32:
                    return "i";
                case UndertaleInstruction.DataType.Int64:
                    return "l";
                case UndertaleInstruction.DataType.Boolean:
                    return "b";
                case UndertaleInstruction.DataType.Variable:
                    return "v";
                case UndertaleInstruction.DataType.String:
                    return "s";
                case UndertaleInstruction.DataType.Int16:
                    return "e";
                default:
                    return type.ToString().ToLower();
            }
        }

        public static UndertaleInstruction.DataType FromOpcodeParam(string type)
        {
            switch (type)
            {
                case "d":
                    return UndertaleInstruction.DataType.Double;
                case "f":
                    return UndertaleInstruction.DataType.Float;
                case "i":
                    return UndertaleInstruction.DataType.Int32;
                case "l":
                    return UndertaleInstruction.DataType.Int64;
                case "b":
                    return UndertaleInstruction.DataType.Boolean;
                case "v":
                    return UndertaleInstruction.DataType.Variable;
                case "s":
                    return UndertaleInstruction.DataType.String;
                case "e":
                    return UndertaleInstruction.DataType.Int16;
                default:
                    return (UndertaleInstruction.DataType)Enum.Parse(typeof(UndertaleInstruction.DataType), type, true);
            }
        }
    }

    public class UndertaleCode : UndertaleNamedResource, UndertaleObjectWithBlobs, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private uint _Length;
        private uint _LocalsCount = 0; // Seems related do UndertaleCodeLocals, TODO: does it also seem unused?
        internal uint _BytecodeAbsoluteAddress;
        private uint _UnknownProbablyZero = 0;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public uint Length { get => _Length; internal set { _Length = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Length")); } }
        public uint LocalsCount { get => _LocalsCount; set { _LocalsCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LocalsCount")); } }
        public uint UnknownProbablyZero { get => _UnknownProbablyZero; set { _UnknownProbablyZero = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UnknownProbablyZero")); } }
        public List<UndertaleInstruction> Instructions { get; } = new List<UndertaleInstruction>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void SerializeBlobBefore(UndertaleWriter writer)
        {
            _BytecodeAbsoluteAddress = writer.Position;
            uint start = writer.Position;
            foreach (UndertaleInstruction instr in Instructions)
                writer.WriteUndertaleObject(instr);
            Length = writer.Position - start;
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Length);
            writer.Write(LocalsCount);
            int BytecodeRelativeAddress = (int)_BytecodeAbsoluteAddress - (int)writer.Position;
            writer.Write(BytecodeRelativeAddress);
            writer.Write(UnknownProbablyZero);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Length = reader.ReadUInt32();
            if (reader.undertaleData.UnsupportedBytecodeVersion && reader.undertaleData.GeneralInfo.BytecodeVersion < 15)
            {
                reader.ReadBytes((int)Length);
            } else
            {
                LocalsCount = reader.ReadUInt32();
                int BytecodeRelativeAddress = reader.ReadInt32();
                _BytecodeAbsoluteAddress = (uint)((int)reader.Position - 4 + BytecodeRelativeAddress);
                uint here = reader.Position;
                reader.Position = _BytecodeAbsoluteAddress;
                Instructions.Clear();
                while (reader.Position < _BytecodeAbsoluteAddress + Length)
                {
                    uint a = (reader.Position - _BytecodeAbsoluteAddress) / 4;
                    UndertaleInstruction instr = reader.ReadUndertaleObject<UndertaleInstruction>();
                    instr.Address = a;
                    Instructions.Add(instr);
                }
                reader.Position = here;
                UnknownProbablyZero = reader.ReadUInt32();
            }
        }

        public void UpdateAddresses()
        {
            uint addr = 0;
            foreach(UndertaleInstruction instr in Instructions)
            {
                instr.Address = addr;
                addr += instr.CalculateInstructionSize();
            }
            Length = addr * 4;
        }

        public IList<UndertaleVariable> FindReferencedVars()
        {
            List<UndertaleVariable> vars = new List<UndertaleVariable>();
            foreach (UndertaleInstruction instr in Instructions)
            {
                var v = instr.GetReference<UndertaleVariable>()?.Target;
                if (v == null)
                    continue;
                if (!vars.Contains(v))
                    vars.Add(v);
            }
            return vars;
        }

        public IList<UndertaleVariable> FindReferencedLocalVars()
        {
            return FindReferencedVars().Where((x) => x.InstanceType == UndertaleInstruction.InstanceType.Local).ToList();
        }

        public string GenerateLocalVarDefinitions(IList<UndertaleVariable> vars, UndertaleCodeLocals locals)
        {
            // Detection for if the code is not disassembled
            if (locals == null)
                return "Code failed to disassemble- likely due to unsupported bytecode version.";

            StringBuilder sb = new StringBuilder();

            var referenced = FindReferencedLocalVars();
            if (locals.Name != Name)
                throw new Exception("Name of the locals block does not match name of the code block");
            foreach (var arg in locals.Locals)
            {
                sb.Append(".localvar " + arg.Index + " " + arg.Name.Content);
                var refvar = referenced.Where((x) => x.Name == arg.Name && x.VarID == arg.Index).FirstOrDefault();
                if (refvar != null)
                {
                    sb.Append(" " + vars.IndexOf(refvar));
                }
                sb.Append("\n");
            }

            return sb.ToString();
        }

        public string Disassemble(IList<UndertaleVariable> vars, UndertaleCodeLocals locals)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GenerateLocalVarDefinitions(vars, locals));
            foreach (var inst in Instructions)
            {
                sb.Append(inst.ToString(this, vars));
                sb.Append("\n");
            }
            return sb.ToString();
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

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
