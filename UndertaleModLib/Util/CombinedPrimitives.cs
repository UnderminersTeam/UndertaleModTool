using System.Runtime.InteropServices;
using UndertaleModLib.Models;

namespace UndertaleModLib.Util;

/// <summary>
/// 32-bit floating point or integer storage, explicitly defined with a backing type as an integer.
/// </summary>
/// <remarks>
/// This helps prevent issues with negative zero being inconsistently handled.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
internal readonly struct FloatAsInt
{
    [FieldOffset(0)]
    private readonly float _float;

    [FieldOffset(0)]
    private readonly uint _uint;

    /// <summary>
    /// Constructs a float from the given 32-bit unsigned integer bit representation.
    /// </summary>
    public FloatAsInt(uint value)
    {
        _uint = value;
    }

    /// <summary>
    /// Constructs a float from the given float value.
    /// </summary>
    private FloatAsInt(float value)
    {
        _float = value;
    }

    /// <summary>
    /// Constructs a float from the given float value.
    /// </summary>
    public static explicit operator FloatAsInt(float value)
    {
        return new(value);
    }

    /// <summary>
    /// Returns a float from the value.
    /// </summary>
    public float AsFloat()
    {
        return _float;
    }

    /// <summary>
    /// Returns a bit representation of the float as an unsigned 32-bit integer.
    /// </summary>
    public uint AsUInt()
    {
        return _uint;
    }
}

/// <summary>
/// Storage for int, long, and double, taking exactly the size of a long (64 bits).
/// </summary>
/// <remarks>
/// This is used to compact data sizes on <see cref="UndertaleInstruction"/>.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
internal readonly struct InstructionPrimitiveType
{
    [FieldOffset(0)]
    public readonly int AsInt;

    [FieldOffset(0)]
    public readonly double AsDouble;

    [FieldOffset(0)]
    public readonly long AsLong;

    /// <summary>
    /// Constructs from the given 32-bit integer.
    /// </summary>
    public InstructionPrimitiveType(int value)
    {
        AsInt = value;
    }

    /// <summary>
    /// Constructs from the given 64-bit integer.
    /// </summary>
    public InstructionPrimitiveType(double value)
    {
        AsDouble = value;
    }

    /// <summary>
    /// Constructs from the given 64-bit integer.
    /// </summary>
    public InstructionPrimitiveType(long value)
    {
        AsLong = value;
    }
}
