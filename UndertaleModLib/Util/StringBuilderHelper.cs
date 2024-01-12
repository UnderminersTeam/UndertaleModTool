using System.Text;
using UndertaleModLib.Compiler;

namespace UndertaleModLib.Util;

/// <summary>
/// This is a helper class for <see cref="StringBuilder"/>. The main advantage that this one offers,
/// is that it contains functions to keep appending starting from a position, so that one doesn't have to do that
/// manually.
/// </summary>
/// <remarks>
/// This is an example usage of how this struct is intended to be used:
/// Instead of needing to do
/// <code>
/// var sb = new StringBuilder();
/// var index = 0;
/// sb.Append("Some String");
/// index = 4;
/// var stringToInsert = "insertion!";
/// sb.Insert(index, stringToInsert);
/// index += stringToInsert.Length;
/// var otherString = "another!";
/// sb.Insert(index, otherString);
/// index += otherString;
/// </code>
/// One can instead do:
/// <code>
/// var sbh = new StringBuilderHelper();
/// var sb = new StringBuilder();
/// sb.Append("Some String");
/// sbh.SetPosition(4);
/// sbh.Append("insertion!");
/// sbh.Append("another!");
/// </code>
/// </remarks>
public struct StringBuilderHelper
{
    /// <summary>
    /// The position from where functions will be executed from.
    /// </summary>
    public int Position;

    /// <summary>
    /// Initializes a new <see cref="StringBuilderHelper"/> instance.
    /// </summary>
    public StringBuilderHelper()
    {
        Position = 0;
    }
    
    /// <summary>
    /// Initializes a new <see cref="StringBuilderHelper"/> instance with a specific position.
    /// </summary>
    /// <param name="position">The position for this <see cref="StringBuilderHelper"/>.</param>
    public StringBuilderHelper(int position)
    {
        this.Position = position;
    }

    /// <summary>
    /// Appends a copy of the specified string to a <see cref="StringBuilder"/> at <see cref="Position"/> and increases it afterwards.
    /// </summary>
    /// <param name="sb">An instance on where a string should be appended to.</param>
    /// <param name="value"><inheritdoc cref="StringBuilder.Append(string)"/></param>
    public void Append(StringBuilder sb, string value)
    {
        if (Position == sb.Length)
            sb.Append(value);
        else
            sb.Insert(Position, value);
        Position += value.Length;
    }
    
    /// <summary>
    /// Appends the specified char to a <see cref="StringBuilder"/> at <see cref="Position"/> and increases it afterwards.
    /// </summary>
    /// <param name="sb">An instance on where a string should be appended to.</param>
    /// <param name="value"><inheritdoc cref="StringBuilder.Append(char)"/></param>
    public void Append(StringBuilder sb, char value)
    {
        if (Position == sb.Length)
            sb.Append(value);
        else
            sb.Insert(Position, value);
        Position += 1;
    }
    
    /// <summary>
    /// <inheritdoc cref="StringBuilderHelper.Append(StringBuilder, string)"/>
    /// </summary>
    /// <param name="sb">An instance on where a string should be appended to.</param>
    /// <param name="value"><inheritdoc cref="StringBuilder.Append(object)"/></param>
    public void Append(StringBuilder sb, object value) => this.Append(sb, value.ToString());
    
    /// <summary>
    /// <inheritdoc cref="StringBuilderHelper.Append(StringBuilder, string)"/>
    /// </summary>
    /// <param name="sb">An instance on where a string should be appended to.</param>
    /// <param name="value"><inheritdoc cref="StringBuilder.Append(int)"/></param>
    public void Append(StringBuilder sb, int value) => this.Append(sb, value.ToString());
    
    /// <summary>
    /// <inheritdoc cref="StringBuilderHelper.Append(StringBuilder, string)"/>
    /// </summary>
    /// <param name="sb">An instance on where a string should be appended to.</param>
    /// <param name="value"><inheritdoc cref="StringBuilder.Append(byte)"/></param>
    public void Append(StringBuilder sb, byte value) => this.Append(sb, value.ToString());
    
    
    
    // Add more overloads as needed.
}