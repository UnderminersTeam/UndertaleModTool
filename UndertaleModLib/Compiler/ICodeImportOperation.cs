using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UndertaleModLib.Models;

namespace UndertaleModLib.Compiler;

/// <summary>
/// Interface for different types of code import operations.
/// </summary>
internal interface ICodeImportOperation
{
    /// <summary>
    /// Code entry to be imported to, for this operation.
    /// </summary>
    public UndertaleCode CodeEntry { get; }

    /// <summary>
    /// Perform this import operation.
    /// </summary>
    public void Import(CodeImportGroup group);
}

/// <summary>
/// Represents a code replace operation.
/// </summary>
internal readonly record struct CodeReplaceOperation(UndertaleCode CodeEntry, string Code) : ICodeImportOperation
{
    /// <inheritdoc/>
    public void Import(CodeImportGroup group)
    {
        group.CompileGroup.QueueCodeReplace(CodeEntry, Code);
    }
}

/// <summary>
/// Represents a code append operation (by decompiling existing code, and appending the code string to the end).
/// </summary>
internal readonly record struct CodeAppendOperation(UndertaleCode CodeEntry, string Code) : ICodeImportOperation
{
    /// <inheritdoc/>
    public void Import(CodeImportGroup group)
    {
        string finalCodeToCompile = string.Concat(group.DecompileExistingCode(CodeEntry), "\n", Code);
        group.CompileGroup.QueueCodeReplace(CodeEntry, finalCodeToCompile);
    }
}

/// <summary>
/// Represents a code prepend operation (by decompiling existing code, and prepending the code string to the start).
/// </summary>
internal readonly record struct CodePrependOperation(UndertaleCode CodeEntry, string Code) : ICodeImportOperation
{
    /// <inheritdoc/>
    public void Import(CodeImportGroup group)
    {
        string finalCodeToCompile = string.Concat(Code, "\n", group.DecompileExistingCode(CodeEntry));
        group.CompileGroup.QueueCodeReplace(CodeEntry, finalCodeToCompile);
    }
}

/// <summary>
/// Represents a code find and replace operation (by decompiling existing code, and performing find/replace on that string).
/// </summary>
internal readonly record struct CodeFindReplaceOperation
    (UndertaleCode CodeEntry, string Search, string Replacement, bool IsRegex, bool CaseSensitive, bool TrimmedLines) : ICodeImportOperation
{
    private readonly record struct TrimmedLine(int WhitespaceStartIndex, int StartIndex, int EndIndex, int WhitespaceEndIndex);

    /// <summary>
    /// Returns a list of ranges, representing trimmed lines from the given string.
    /// </summary>
    private static List<TrimmedLine> SplitByTrimmedLines(ReadOnlySpan<char> str)
    {
        List<TrimmedLine> lines = new(16);
        int lineWhitespaceStartIndex = 0;
        int lineStartIndex = 0;
        int lineEndIndex = 0;
        bool beginningOfLine = true;
        for (int i = 0; i < str.Length; i++)
        {
            // End of line
            if (str[i] == '\n')
            {
                lines.Add(new(lineWhitespaceStartIndex, lineStartIndex, lineEndIndex, i));
                lineWhitespaceStartIndex = i + 1;
                lineStartIndex = i + 1;
                lineEndIndex = i + 1;
                beginningOfLine = true;
                continue;
            }
            
            // Find the beginning of a line
            if (beginningOfLine && !char.IsWhiteSpace(str[i]))
            {
                lineStartIndex = i;
                beginningOfLine = false;
            }

            // Find the end of a line
            if (!beginningOfLine && !char.IsWhiteSpace(str[i]))
            {
                lineEndIndex = i + 1;
            }
        }
        return lines;
    }

    /// <summary>
    /// Replaces Search with Replacement on the given string, processing on a line-by-line basis, trimming each line's whitespace.
    /// </summary>
    private string ReplaceTrimmedLines(ReadOnlySpan<char> code, StringComparison comparison)
    {
        ReadOnlySpan<char> search = Search;
        StringBuilder sb = new(code.Length);

        // Split search and code into each line being matched
        List<TrimmedLine> codeLines = SplitByTrimmedLines(code);
        List<TrimmedLine> searchLines = SplitByTrimmedLines(search);

        // Enumerate over code lines
        for (int i = 0; i < codeLines.Count; i++)
        {
            // Check for match starting at this line
            bool match = true;
            if ((i + searchLines.Count) > codeLines.Count)
            {
                match = false;
            }
            else
            {
                // Compare all lines until any difference is found
                for (int j = 0; j < searchLines.Count; j++)
                {
                    TrimmedLine codeLine = codeLines[i + j];
                    TrimmedLine searchLine = searchLines[j];
                    ReadOnlySpan<char> codeLineChars = code[codeLine.StartIndex..codeLine.EndIndex];
                    ReadOnlySpan<char> searchLineChars = search[searchLine.StartIndex..searchLine.EndIndex];
                    if (!codeLineChars.Equals(searchLineChars, comparison))
                    {
                        match = false;
                        break;
                    }
                }
            }

            // If a match was found, insert replacement; otherwise, append current code line
            if (match)
            {
                sb.Append(Replacement);
                i += searchLines.Count;
            }
            else
            {
                TrimmedLine codeLine = codeLines[i];
                ReadOnlySpan<char> fullCodeLineChars = code[codeLine.WhitespaceStartIndex..codeLine.WhitespaceEndIndex];
                sb.Append(fullCodeLineChars);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc/>
    public void Import(CodeImportGroup group)
    {
        // Decompile code
        string decompilation = group.DecompileExistingCode(CodeEntry);

        // Perform find & replace
        string finalCodeToCompile;
        if (IsRegex)
        {
            // Regex replace
            RegexOptions options = RegexOptions.Multiline | RegexOptions.CultureInvariant;
            if (!CaseSensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }
            finalCodeToCompile = Regex.Replace(decompilation, Search, Replacement, options);
        }
        else
        {
            // Regular replace
            StringComparison comparison = StringComparison.Ordinal;
            if (!CaseSensitive)
            {
                comparison = StringComparison.OrdinalIgnoreCase;
            }
            if (TrimmedLines)
            {
                finalCodeToCompile = ReplaceTrimmedLines(decompilation, comparison);
            }
            else
            {
                finalCodeToCompile = decompilation.Replace(Search, Replacement, comparison);
            }
        }

        // Queue lower level replace operation, if code has changed
        if (decompilation != finalCodeToCompile)
        {
            group.CompileGroup.QueueCodeReplace(CodeEntry, finalCodeToCompile);
        }
        else if (group.ThrowOnNoOpFindReplace)
        {
            throw new Exception($"No-op find and replace performed on {CodeEntry.Name?.Content}");
        }
    }
}
