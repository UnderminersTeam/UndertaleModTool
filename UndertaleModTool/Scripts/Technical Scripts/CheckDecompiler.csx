// Made by Grossley ( Grossley#2869 on Discord )
// Changes:
// Version 01 (November 13th, 2020): Initial release
// Version 02 (April 21st, 2021): Updated for GMS 2.3, general refactor
// Version 03 (April 29th, 2021): Refactored for the profile system, simplified, removed unnecessary components
// Version 04 (October 1st, 2021): Have CheckDecompiler and CheckDecompiler 2.3 use the same (cleaner) code base and combine them.
// Version 05 (July 18th, 2024): Updated to Underanalyzer decompiler, and some cleanup.
// Version 06 (March 1st, 2025): Complete rewrite to avoid disk in the first place, and to use Underanalyzer compiler.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

Regex setownerRegex = new Regex(@"pushi?\.[eil] \d+(\n|\r\n)setowner\.e(\n|\r\n)");

List<UndertaleCode> toCheck = Data.Code.Where(c => c.ParentEntry is null).ToList();

List<string> failedAssemblyList = new();
List<string> failedDecompiledList = new();
CheckCode();

string failedAssemblyPath = Path.Combine(Path.GetDirectoryName(FilePath), "failed-assembly.txt");
File.WriteAllText(failedAssemblyPath, string.Join('\n', failedAssemblyList));
string failedDecompiledPath = Path.Combine(Path.GetDirectoryName(FilePath), "failed-decompiled.txt");
File.WriteAllText(failedDecompiledPath, string.Join('\n', failedDecompiledList));

ScriptMessage(
    $"Decompiler and compiler check complete.\n" +
    $"{failedAssemblyList.Count}/{toCheck.Count} ({(failedAssemblyList.Count / (double)toCheck.Count) * 100.0:0.###}%) failed to exactly recompile\n" +
    $"{failedDecompiledList.Count}/{toCheck.Count} ({(failedDecompiledList.Count / (double)toCheck.Count) * 100.0:0.###}%) failed to recompile to same decompilation\n\n" +
    $"More details written to failed-assembly.txt and failed-decompiled.txt");

void CheckCode()
{
    foreach (UndertaleCode code in toCheck)
    {
        // Skip child code entries
        if (code.ParentEntry is not null)
        {
            continue;
        }

        try
        {
            // Perform initial decompilation & disassembly
            string disassembled = code.Disassemble(Data.Variables, Data.CodeLocals?.For(code));
            disassembled = setownerRegex.Replace(disassembled, "");
            string decompiled = new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString();

            // Perform re-compilation
            CompileGroup group = new(Data, globalDecompileContext);
            group.QueueCodeReplace(code, decompiled);
            CompileResult result = group.Compile();
            if (!result.Successful)
            {
                throw new Exception("Compile failed");
            }

            // Perform second decompilation & disassembly
            string secondDisassembled = code.Disassemble(Data.Variables, Data.CodeLocals?.For(code));
            string secondDecompilation = new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, code, decompilerSettings).DecompileToString();

            // Collect results
            if (disassembled != secondDisassembled)
            {
                failedAssemblyList.Add(code.Name?.Content ?? "<null>");
            }
            if (decompiled != secondDecompilation)
            {
                failedDecompiledList.Add(code.Name?.Content ?? "<null>");
            }
        }
        catch (Exception e)
        {
            failedAssemblyList.Add(code.Name?.Content ?? "<null>");
            failedDecompiledList.Add(code.Name?.Content ?? "<null>");
        }
        IncrementProgress();
    }
}

