using System.Runtime.InteropServices;

namespace UndertaleModLib.Util;

/// <summary>
/// Floating point storage, explicitly defined with a backing type as an integer.
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
