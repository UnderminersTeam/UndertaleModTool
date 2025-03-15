using System;
using Underanalyzer;
using Underanalyzer.Compiler;
using Underanalyzer.Compiler.Bytecode;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static Underanalyzer.IGMInstruction;

namespace UndertaleModLib.Compiler;

/// <summary>
/// Code builder, used to generate instructions during compilation.
/// </summary>
internal class CodeBuilder : ICodeBuilder
{
    // Data being used with this code builder.
    private readonly GlobalDecompileContext _globalContext;

    public CodeBuilder(GlobalDecompileContext globalContext)
    {
        _globalContext = globalContext;
    }

    /// <summary>
    /// Maps an opcode from Underanalyzer to an UndertaleInstruction opcode.
    /// Also handles bytecode 14 mappings.
    /// </summary>
    private UndertaleInstruction.Opcode MapOpcode(Opcode opcode)
    {
        UndertaleInstruction.Opcode utOpcode = (UndertaleInstruction.Opcode)opcode;

        if (!_globalContext.Bytecode14OrLower)
        {
            return utOpcode;
        }

        return utOpcode switch
        {
            UndertaleInstruction.Opcode.PushBltn => UndertaleInstruction.Opcode.Push,
            UndertaleInstruction.Opcode.PushLoc => UndertaleInstruction.Opcode.Push,
            UndertaleInstruction.Opcode.PushGlb => UndertaleInstruction.Opcode.Push,
            UndertaleInstruction.Opcode.PushI => UndertaleInstruction.Opcode.Push,
            _ => utOpcode
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode)
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, DataType dataType)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode),
            Type1 = (UndertaleInstruction.DataType)dataType
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, DataType dataType1, DataType dataType2)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode),
            Type1 = (UndertaleInstruction.DataType)dataType1,
            Type2 = (UndertaleInstruction.DataType)dataType2
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, short value, DataType dataType1, DataType dataType2)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode),
            Type1 = (UndertaleInstruction.DataType)dataType1,
            Type2 = (UndertaleInstruction.DataType)dataType2,
            Value = value
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, int value, DataType dataType1, DataType dataType2)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode),
            Type1 = (UndertaleInstruction.DataType)dataType1,
            Type2 = (UndertaleInstruction.DataType)dataType2,
            Value = value
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, long value, DataType dataType1, DataType dataType2)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode),
            Type1 = (UndertaleInstruction.DataType)dataType1,
            Type2 = (UndertaleInstruction.DataType)dataType2,
            Value = value
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, double value, DataType dataType1, DataType dataType2)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode),
            Type1 = (UndertaleInstruction.DataType)dataType1,
            Type2 = (UndertaleInstruction.DataType)dataType2,
            Value = value
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, Opcode opcode, ComparisonType comparisonType, DataType dataType1, DataType dataType2)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = MapOpcode(opcode),
            ComparisonKind = (UndertaleInstruction.ComparisonType)comparisonType,
            Type1 = (UndertaleInstruction.DataType)dataType1,
            Type2 = (UndertaleInstruction.DataType)dataType2
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, ExtendedOpcode extendedOpcode)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.Break,
            ExtendedKind = (short)extendedOpcode,
            Type1 = UndertaleInstruction.DataType.Int16
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateInstruction(int address, ExtendedOpcode extendedOpcode, int value)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.Break,
            ExtendedKind = (short)extendedOpcode,
            Type1 = UndertaleInstruction.DataType.Int32,
            IntArgument = value
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateDuplicateInstruction(int address, DataType dataType, byte duplicationSize)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.Dup,
            Type1 = (UndertaleInstruction.DataType)dataType,
            Extra = duplicationSize
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateDupSwapInstruction(int address, DataType dataType, byte duplicationSize, byte duplicationSize2)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.Dup,
            Type1 = (UndertaleInstruction.DataType)dataType,
            Extra = duplicationSize,
            ComparisonKind = (UndertaleInstruction.ComparisonType)((duplicationSize2 << 3) | 0x80)
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreatePopSwapInstruction(int address, byte swapSize)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.Pop,
            Type1 = UndertaleInstruction.DataType.Int16,
            Type2 = UndertaleInstruction.DataType.Variable,
            SwapExtra = swapSize
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateWithExitInstruction(int address)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.PopEnv,
            JumpOffsetPopenvExitMagic = true,
            JumpOffset = 0xF00000
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateCallInstruction(int address, int argumentCount)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ArgumentsCount = (ushort)argumentCount
        };
    }

    /// <inheritdoc/>
    public IGMInstruction CreateCallVariableInstruction(int address, int argumentCount)
    {
        return new UndertaleInstruction()
        {
            Address = (uint)(address / 4),
            Kind = UndertaleInstruction.Opcode.CallV,
            Type1 = UndertaleInstruction.DataType.Variable,
            Extra = (byte)argumentCount
        };
    }

    /// <inheritdoc/>
    public void PatchInstruction(IGMInstruction instruction, string variableName, InstanceType variableInstanceType, InstanceType instructionInstanceType, VariableType variableType, bool isBuiltin, bool isStructVariable)
    {
        if (instruction is UndertaleInstruction utInstruction)
        {
            // Transform instance type into Self when not using simple variables, either in GMLv2 or when instance type is an object/instance
            if (!isStructVariable && (variableInstanceType >= 0 || _globalContext.UsingGMLv2) && variableType != VariableType.Normal && variableType != VariableType.Instance)
            {
                variableInstanceType = InstanceType.Self;
                instructionInstanceType = InstanceType.Self;
            }

            // Transform Argument into Builtin, but not marked as an actual builtin (only on the variable registration itself)
            if (variableInstanceType == InstanceType.Argument)
            {
                variableInstanceType = InstanceType.Builtin;
                isBuiltin = false;
            }

            // Transform irregular instance types to Self (including Other, apparently)
            if (variableInstanceType is not (InstanceType.Self or InstanceType.Local or InstanceType.Builtin or 
                                             InstanceType.Global or InstanceType.Static))
            {
                variableInstanceType = InstanceType.Self;
            }

            // Create blank reference that will be populated with a target later
            UndertaleInstruction.Reference<UndertaleVariable> reference = new(null, (UndertaleInstruction.VariableType)variableType);

            // Lookup variable (or create new one)
            if (variableInstanceType == InstanceType.Local)
            {
                // Queue reference to be patched later
                _globalContext.CurrentCompileGroup.RegisterLocalVariable(reference, variableName);
            }
            else
            {
                // Register/define non-local variable, and update reference immediately
                _globalContext.CurrentCompileGroup.RegisterNonLocalVariable(variableName);
                UndertaleString nameString = _globalContext.CurrentCompileGroup.MakeString(variableName, out int nameStringId);
                reference.Target = _globalContext.Data.Variables.EnsureDefined(
                    nameString, nameStringId, (UndertaleInstruction.InstanceType)variableInstanceType, isBuiltin, _globalContext.Data);
            }

            // Update instruction
            if (utInstruction.Kind == UndertaleInstruction.Opcode.Pop)
            {
                // Pop instruction, set instruction's destination
                utInstruction.Destination = reference;
            }
            else
            {
                // All other instructions, just set instruction's value
                utInstruction.Value = reference;
            }
            if (variableType is VariableType.Normal or VariableType.Instance)
            {
                utInstruction.TypeInst = (UndertaleInstruction.InstanceType)instructionInstanceType;
            }
        }
    }

    /// <inheritdoc/>
    public void PatchInstruction(IGMInstruction instruction, FunctionScope scope, string functionName, IBuiltinFunction builtinFunction)
    {
        if (instruction is UndertaleInstruction utInstruction)
        {
            // Resolve reference
            UndertaleInstruction.Reference<UndertaleFunction> reference;
            if (scope.TryGetDeclaredFunction(functionName, out FunctionEntry entry))
            {
                reference = new(
                    entry.Function as UndertaleFunction ?? throw new InvalidOperationException("Function not resolved for function entry")
                );
            }
            else if (_globalContext.Builtins.LookupBuiltinFunction(functionName) is not null)
            {
                reference = new(
                    _globalContext.Data.Functions.EnsureDefined(functionName, _globalContext.Data.Strings)
                );
            }
            else if (_globalContext.GlobalFunctions.TryGetFunction(functionName, out IGMFunction function))
            {
                reference = new((UndertaleFunction)function);
            }
            else if (_globalContext.GetScriptId(functionName, out int _))
            {
                reference = new(
                    _globalContext.Data.Functions.EnsureDefined(functionName, _globalContext.Data.Strings)
                );
            }
            else
            {
                throw new CompilerException($"Failed to look up function \"{functionName}\"");
            }

            // Put reference on instruction
            if (utInstruction.Kind == UndertaleInstruction.Opcode.Push)
            {
                utInstruction.Value = reference;
            }
            else
            {
                utInstruction.Function = reference;
            }
        }
    }

    /// <inheritdoc/>
    public void PatchInstruction(IGMInstruction instruction, FunctionEntry functionEntry)
    {
        if (instruction is UndertaleInstruction utInstruction)
        {
            // Resolve reference
            UndertaleInstruction.Reference<UndertaleFunction> reference = new(
                functionEntry.Function as UndertaleFunction ?? throw new InvalidOperationException("Function not resolved for function entry"));

            // Put reference on instruction
            if (utInstruction.Kind == UndertaleInstruction.Opcode.Push)
            {
                utInstruction.Value = reference;
            }
            else
            {
                utInstruction.Function = reference;
            }
        }
    }

    /// <inheritdoc/>
    public void PatchInstruction(IGMInstruction instruction, string stringContent)
    {
        if (instruction is UndertaleInstruction utInstruction)
        {
            // Make/find string
            UndertaleString str = _globalContext.CurrentCompileGroup.MakeString(stringContent, out int strIndex);

            // Update instruction
            utInstruction.Value = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>(str, strIndex); 
        }
    }

    /// <inheritdoc/>
    public void PatchInstruction(IGMInstruction instruction, int value)
    {
        if (instruction is UndertaleInstruction utInstruction)
        {
            if (utInstruction.Kind == UndertaleInstruction.Opcode.Push)
            {
                utInstruction.Value = value;
            }
            else
            {
                utInstruction.JumpOffset = value / 4;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsGlobalFunctionName(string name)
    {
        // TODO: possibly have an UTMT setting to make this behavior define new functions in pre-2.3 games only
        return _globalContext.GlobalFunctions.FunctionNameExists(name);
    }

    /// <inheritdoc/>
    public int GenerateTryVariableID(int internalIndex)
    {
        // Use the internal index that the compiler generates for now
        // TODO: possibly allow compile context user to specify that this index should be global to the compile context?
        return internalIndex;
    }

    /// <inheritdoc/>
    public long GenerateArrayOwnerID(string variableName, long functionIndex, bool isDot)
    {
        long id = _globalContext.UsingNewArrayOwners ? (functionIndex << 16) : 0;
        if (isDot)
        {
            // Double the ID at this point, apparently...
            id += id;
        }
        if (variableName is not null)
        {
            id += _globalContext.CurrentCompileGroup.RegisterName(variableName);
        }
        id += _globalContext.CurrentCompileGroup.CurrentCodeEntryNameHash;

        // Wrap around 31-bit unsigned integer limit, to stay within a pushi.e or push.i instruction
        return Math.Abs(id % int.MaxValue);
    }

    /// <inheritdoc/>
    public void OnParseNameIdentifier(string name)
    {
        // TODO: possibly do things if enabling a setting (to try to replicate original ID numbers)
    }
}
