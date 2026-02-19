using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UndertaleModLib.Scripting;

namespace UndertaleModLib.Util;

/// <summary>
/// Standard scripting utility functions.
/// </summary>
public static class ScriptingUtil
{
    /// <summary>
    /// Creates default scripting options for CSX scripts, with all standard imports/references.
    /// </summary>
    public static ScriptOptions CreateDefaultScriptOptions()
    {
        return ScriptOptions.Default
            .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler",
                        "UndertaleModLib.Scripting", "UndertaleModLib.Compiler",
                        "System", "System.IO", "System.Collections.Generic",
                        "System.Text.RegularExpressions")
            .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly,
                            typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly,
                            typeof(ImageMagick.MagickImage).GetTypeInfo().Assembly,
                            typeof(Underanalyzer.Decompiler.DecompileContext).Assembly)
            .WithEmitDebugInformation(true); // When script throws an exception, adds an exception location (line number)
    }

    /// <summary>
    /// Prettifies/stringifies a scripting-caused exception object, dealing with script stack traces and aggregate exceptions.
    /// </summary>
    public static string PrettifyException(in Exception exc)
    {
        // If this is a simple script exception, return its message
        if (exc is ScriptException)
        {
            return exc.Message;
        }

        // Collect all original trace lines that we want to parse
        List<string> traceLines = new();
        Dictionary<string, int> exTypesDict = null;
        if (exc is AggregateException)
        {
            List<string> exTypes = [];

            // Collect trace lines of inner exceptions, and track their exception type names
            foreach (Exception ex in (exc as AggregateException).InnerExceptions)
            {
                traceLines.AddRange(ex.StackTrace.Split(Environment.NewLine));
                exTypes.Add(ex.GetType().FullName);
            }

            // Create a mapping of each exception type to the number of its occurrences
            if (exTypes.Count > 1)
            {
                exTypesDict = exTypes.GroupBy(x => x)
                                     .Select(x => new { Name = x.Key, Count = x.Count() })
                                     .OrderByDescending(x => x.Count)
                                     .ToDictionary(x => x.Name, x => x.Count);
            }
        }
        else if (exc.InnerException is not null)
        {
            // Collect trace lines of single inner exception
            traceLines.AddRange(exc.InnerException.StackTrace.Split(Environment.NewLine));
        }
        traceLines.AddRange(exc.StackTrace.Split(Environment.NewLine));

        // Iterate over all lines in the stack trace, finding their line numbers and file names
        List<(string SourceFile, int LineNum)> loadedScriptLineNums = new();
        int expectedNumScriptTraceLines = 0;
        try
        {
            foreach (string traceLine in traceLines)
            {
                // Only handle trace lines that come from a script
                if (traceLine.TrimStart()[..13] == "at Submission")
                {
                    // Add to total count of expected script trace lines
                    expectedNumScriptTraceLines++;

                    // Get full path of the script file, within the line
                    string sourceFile = Regex.Match(traceLine, @"(?<=in ).*\.csx(?=:line \d+)").Value;
                    if (!File.Exists(sourceFile))
                        continue;

                    // Try to find line number from the line
                    const string pattern = ":line ";
                    int linePos = traceLine.IndexOf(pattern);
                    if (linePos > 0 && int.TryParse(traceLine[(linePos + pattern.Length)..], out int lineNum))
                    {
                        loadedScriptLineNums.Add((sourceFile, lineNum));
                    }
                }
            }
        }
        catch (Exception e)
        {
            string excString = exc.ToString();

            int endOfPrevStack = excString.IndexOf("--- End of stack trace from previous location ---");
            if (endOfPrevStack != -1)
            {
                // Keep only stack trace of the script
                excString = excString[..endOfPrevStack];
            }

            return $"An error occurred while processing the exception text.\nError message - \"{e.Message}\"\nThe unprocessed text is below.\n\n" + excString;
        }

        // Generate final exception text to show.
        // If we found the expected number of script trace lines, then use them; otherwise, use the regular exception text.
        string excText;
        if (loadedScriptLineNums.Count == expectedNumScriptTraceLines)
        {
            // Read the code for the files to know what the code line associated with the stack trace is
            Dictionary<string, List<string>> scriptsCode = [];
            foreach ((string sourceFile, int _) in loadedScriptLineNums)
            {
                if (!scriptsCode.ContainsKey(sourceFile))
                {
                    string scriptCode = null;
                    try
                    {
                        scriptCode = File.ReadAllText(sourceFile, Encoding.UTF8);
                    }
                    catch (Exception e)
                    {
                        string excString = exc.ToString();

                        return $"An error occurred while processing the exception text.\nError message - \"{e.Message}\"\nThe unprocessed text is below.\n\n" + excString;
                    }
                    scriptsCode.Add(sourceFile, scriptCode.Split('\n').ToList());
                }
            }

            // Generate custom stack trace
            string excLines = string.Join('\n', loadedScriptLineNums.Select(pair =>
            {
                string scriptName = Path.GetFileName(pair.SourceFile);
                string scriptLine = scriptsCode[pair.SourceFile][pair.LineNum - 1]; // - 1 because line numbers start from 1
                return $"Line {pair.LineNum} in script {scriptName}: {scriptLine}";
            }));

            if (exTypesDict is not null)
            {
                string exTypesStr = string.Join(",\n", exTypesDict.Select(x => $"{x.Key}{((x.Value > 1) ? " (x" + x.Value + ")" : string.Empty)}"));
                excText = $"{exc.GetType().FullName}: One on more errors occurred:\n{exTypesStr}\n\nThe current stacktrace:\n{excLines}";
            }
            else
            {
                excText = $"{exc.GetType().FullName}: {exc.Message}\n\nThe current stacktrace:\n{excLines}";
            }
        }
        else
        {
            string excString = exc.ToString();

            int endOfPrevStack = excString.IndexOf("--- End of stack trace from previous location ---");
            if (endOfPrevStack != -1)
            {
                // Keep only stack trace of the script
                excString = excString[..endOfPrevStack];
            }

            excText = excString;
        }

        return excText;
    }
}
