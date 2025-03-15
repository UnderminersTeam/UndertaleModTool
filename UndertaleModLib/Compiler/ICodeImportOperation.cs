using System;
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
    (UndertaleCode CodeEntry, string Search, string Replacement, bool IsRegex, bool CaseSensitive) : ICodeImportOperation
{
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
            finalCodeToCompile = decompilation.Replace(Search, Replacement, comparison);
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
