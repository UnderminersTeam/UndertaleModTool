using System;

namespace UndertaleModLib.Models;

/// <summary>
/// A string entry a data file can have.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleString : UndertaleResource, ISearchable, IDisposable, Underanalyzer.IGMString
{
    /// <summary>
    /// The contents of the string.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Initializes a new empty instance of type <see cref="UndertaleString"/>.
    /// </summary>
    public UndertaleString()
    {
    }

    /// <summary>
    /// Initializes a new instance of type <see cref="UndertaleString"/> with a specified content.
    /// </summary>
    /// <param name="content">The content for the string.</param>
    public UndertaleString(string content)
    {
        this.Content = content;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteGMString(Content);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Content = reader.ReadGMString();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(true);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Content = null;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <param name="isGMS2">Whether the string is from a GameMaker Studio 2 data file or not.</param>
    /// <returns>A string that represents the current object.</returns>
    public string ToString(bool isGMS2)
    {
        // TODO: someone clean this up please. this seems insane for a tostring method.
        if (Content == null)
            return "\"null\""; // NPE Fix.

        if (isGMS2)
            return "\"" + Content.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";

        // Handle GM:S 1's lack of escaping
        // Yes, in GM:S 1 you cannot escape quotation marks. You are expected to concatenate
        // single-quote strings with double-quote strings.
        string res = Content;
        bool front, back;
        if (res.StartsWith('"'))
        {
            front = true;
            res = res.Remove(0, 1);
            if (res.Length == 0)
                return "'\"'";
        }
        else
            front = false;
        if (res.EndsWith('"'))
        {
            res = res.Remove(res.Length - 1);
            back = true;
        }
        else
            back = false;
        res = res.Replace("\"", "\" + '\"' + \"");
        if (front)
            res = "'\"' + \"" + res;
        else
            res = "\"" + res;
        if (back)
            res += "\" + '\"'";
        else
            res += "\"";
        return res;
    }

    /// <inheritdoc />
    public bool SearchMatches(string filter)
    {
        // TODO: should this throw instead?
        if (filter is null) return false;
        return Content?.ToLower().Contains(filter.ToLower()) ?? false;
    }

    /// <summary>
    /// Unescapes text for the assembler.
    /// </summary>
    /// <param name="text">The text to unescape.</param>
    /// <returns>A string which features the <b>text</b> <c>\n</c>, <c>\r</c>, <c>"</c> and <c>\</c> being properly unescaped.</returns>
    public static string UnescapeText(string text)
    {
        // TODO: optimize this? seems like a very whacky thing to do... why do they have escaped text in the first place?
        return text.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}