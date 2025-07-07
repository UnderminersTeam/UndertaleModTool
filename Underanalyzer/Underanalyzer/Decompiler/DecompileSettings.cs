/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Underanalyzer.Decompiler;

/// <summary>
/// Describes the necessary settings properties for the decompiler.
/// </summary>
public interface IDecompileSettings
{
    /// <summary>
    /// String used to indent, e.g. tabs or some amount of spaces generally.
    /// </summary>
    public string IndentString { get; }

    /// <summary>
    /// If true, semicolons are emitted after statements that generally have them.
    /// If false, some statements may still use semicolons to prevent ambiguity.
    /// </summary>
    public bool UseSemicolon { get; }

    /// <summary>
    /// If true, color constants are written in "#RRGGBB" notation, rather than the normal BGR ordering.
    /// Only applicable if "Constant.Color" is resolved as a macro type.
    /// </summary>
    public bool UseCSSColors { get; }

    /// <summary>
    /// If true, decompiler warnings will be printed as comments in the code.
    /// </summary>
    public bool PrintWarnings { get; }

    /// <summary>
    /// If true, macro declarations (such as enums) will be placed at the top of the code output, rather than the bottom.
    /// </summary>
    public bool MacroDeclarationsAtTop { get; }

    /// <summary>
    /// If true, an empty line will be inserted after local variable declarations belonging to a block.
    /// </summary>
    public bool EmptyLineAfterBlockLocals { get; }

    /// <summary>
    /// If true, an empty line will be inserted either before/after enum declarations, 
    /// depending on if placed at the top or bottom of the code.
    /// </summary>
    public bool EmptyLineAroundEnums { get; }

    /// <summary>
    /// If true, empty lines will be inserted before and/or after branch statements, unless at the start/end of a block.
    /// </summary>
    /// <remarks>
    /// This applies to <c>if</c>/<c>else</c>, <c>switch</c>, <c>try</c>/<c>catch</c>/<c>finally</c>, as well as all loops.
    /// </remarks>
    public bool EmptyLineAroundBranchStatements { get; }

    /// <summary>
    /// If true, empty lines will be inserted before case statements, unless at the start of a block.
    /// </summary>
    public bool EmptyLineBeforeSwitchCases { get; }

    /// <summary>
    /// If true, empty lines will be inserted after case statements, unless at the end of a block.
    /// </summary>
    public bool EmptyLineAfterSwitchCases { get; }

    /// <summary>
    /// If true, empty lines will be inserted before and/or after function declarations, unless at the start/end of a block,
    /// or in an expression (with the exception of in the right side of assignment statements).
    /// </summary>
    public bool EmptyLineAroundFunctionDeclarations { get; }

    /// <summary>
    /// If true, empty lines will be inserted before and/or after static initialization blocks, unless at the start/end of a block.
    /// </summary>
    public bool EmptyLineAroundStaticInitialization { get; }

    /// <summary>
    /// If true, opening curly braces at the start of blocks will be placed on the same line as the 
    /// current code, rather than on the next line.
    /// </summary>
    public bool OpenBlockBraceOnSameLine { get; }

    /// <summary>
    /// If true, blocks with single-line contents in certain statements (e.g. if and loops) will have their braces removed.
    /// For if statements specifically, both the if and else branches must take a single line.
    /// </summary>
    public bool RemoveSingleLineBlockBraces { get; }

    /// <summary>
    /// True if try/catch/finally statements should have their compiler-generated control flow cleaned up.
    /// This cleanup can occasionally be inaccurate to the code that actually executes, due to multiple compiler bugs.
    /// </summary>
    public bool CleanupTry { get; }

    /// <summary>
    /// True if empty if/else chains at the end of a loop body should be rewritten as continue statements, when possible.
    /// </summary>
    public bool CleanupElseToContinue { get; }

    /// <summary>
    /// True if GMLv2 default argument values in functions should be detected and cleaned up.
    /// </summary>
    public bool CleanupDefaultArgumentValues { get; }

    /// <summary>
    /// True if built-in array variables used without an array accessor should be rewritten as such,
    /// rather than using the compiler-generated array accessor at index 0.
    /// </summary>
    public bool CleanupBuiltinArrayVariables { get; }

    /// <summary>
    /// If true, enum values that are detected in a code entry (including any unknown ones) will 
    /// be given declarations at the top of the code.
    /// </summary>
    public bool CreateEnumDeclarations { get; }

    /// <summary>
    /// Base type name for the enum representing all unknown enum values.
    /// Should be a valid enum name in GML, or <see langword="null"/> if the unknown enum should not be generated/used at all.
    /// </summary>
    public string UnknownEnumName { get; }

    /// <summary>
    /// Format string for the values in the enum representing all unknown enum values.
    /// Should be a valid enum value name in GML.
    /// </summary>
    public string UnknownEnumValuePattern { get; }

    /// <summary>
    /// Format string for any arguments with an unknown name, in GMLv2 functions.
    /// </summary>
    public string UnknownArgumentNamePattern { get; }

    /// <summary>
    /// Whether leftover data on the simulated VM stack will be allowed in decompilation output. 
    /// If false, an exception is thrown when data is left over on the stack at the end of a fragment.
    /// If true, a warning is added to the decompile context.
    /// </summary>
    public bool AllowLeftoverDataOnStack { get; }

    /// <summary>
    /// Attempts to retrieve a predefined double value (such as <c>pi</c>), given its double form.
    /// </summary>
    /// <param name="value">The double as stored in the GML code</param>
    /// <param name="result">The resulting value to be printed</param>
    /// <param name="isResultMultiPart">True if parentheses may be needed around the value when printed (due to spaces or operations)</param>
    /// <returns>True if a predefined double value is found; false otherwise.</returns>
    public bool TryGetPredefinedDouble(double value, [MaybeNullWhen(false)] out string result, out bool isResultMultiPart);
}

/// <summary>
/// Provided settings class that can be used by default.
/// </summary>
public class DecompileSettings : IDecompileSettings
{
    public string IndentString { get; set; } = "    ";
    public bool UseSemicolon { get; set; } = true;
    public bool UseCSSColors { get; set; } = true;
    public bool PrintWarnings { get; set; } = true;
    public bool MacroDeclarationsAtTop { get; set; } = false;
    public bool EmptyLineAfterBlockLocals { get; set; } = true;
    public bool EmptyLineAroundEnums { get; set; } = true;
    public bool EmptyLineAroundBranchStatements { get; set; } = false;
    public bool EmptyLineBeforeSwitchCases { get; set; } = false;
    public bool EmptyLineAfterSwitchCases { get; set; } = false;
    public bool EmptyLineAroundFunctionDeclarations { get; set; } = true;
    public bool EmptyLineAroundStaticInitialization { get; set; } = true;
    public bool OpenBlockBraceOnSameLine { get; set; } = false;
    public bool RemoveSingleLineBlockBraces { get; set; } = false;
    public bool CleanupTry { get; set; } = true;
    public bool CleanupElseToContinue { get; set; } = true;
    public bool CleanupDefaultArgumentValues { get; set; } = true;
    public bool CleanupBuiltinArrayVariables { get; set; } = true;
    public bool CreateEnumDeclarations { get; set; } = true;
    public string UnknownEnumName { get; set; } = "UnknownEnum";
    public string UnknownEnumValuePattern { get; set; } = "Value_{0}";
    public string UnknownArgumentNamePattern { get; set; } = "arg{0}";
    public bool AllowLeftoverDataOnStack { get; set; } = false;

    // Some basic data populated from code seen in the wild
    // TODO: populate this with more values by default?
    public Dictionary<double, string> SinglePartPredefinedDoubles = new()
    {
        { 3.141592653589793, "pi" },
    };
    public Dictionary<double, string> MultiPartPredefinedDoubles = new()
    {
        { 6.283185307179586, "2 * pi" },
        { 12.566370614359172, "4 * pi" },
        { 31.41592653589793, "10 * pi" },
        { 0.3333333333333333, "1/3" },
        { 0.6666666666666666, "2/3" },
        { 1.3333333333333333, "4/3" },
        { 23.333333333333332, "70/3" },
        { 73.33333333333333, "220/3" },
        { 206.66666666666666, "620/3" },
        { 51.42857142857143, "360/7" },
        { 1.0909090909090908, "12/11" },
        { 0.06666666666666667, "1/15" },
        { 0.9523809523809523, "20/21" },
        { 0.03333333333333333, "1/30" },
        { 0.008333333333333333, "1/120" }
    };

    public bool TryGetPredefinedDouble(double value, [MaybeNullWhen(false)] out string result, out bool isResultMultiPart)
    {
        if (SinglePartPredefinedDoubles.TryGetValue(value, out result))
        {
            isResultMultiPart = false;
            return true;
        }

        if (MultiPartPredefinedDoubles.TryGetValue(value, out result))
        {
            isResultMultiPart = true;
            return true;
        }

        result = null;
        isResultMultiPart = false;
        return false;
    }
}
