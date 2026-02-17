/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer;
using Underanalyzer.Compiler;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

internal static class TestUtil
{
    /// <summary>
    /// Utility function to reduce having to split lines in tests.
    /// </summary>
    public static GMCode GetCode(string assembly, IGameContext? context = null)
    {
        string[] lines = assembly.Split('\n');
        return VMAssembly.ParseAssemblyFromLines(lines, context);
    }

    /// <summary>
    /// Asserts that for every predecessor, there is a corresponding successor, and vice versa.
    /// Additionally asserts that for every parent, there is a child (and NOT the other way around).
    /// </summary>
    public static void VerifyFlowDirections(IEnumerable<IControlFlowNode> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var pred in node.Predecessors)
            {
                Assert.Contains(node, pred.Successors);
            }
            foreach (var succ in node.Successors)
            {
                Assert.Contains(node, succ.Predecessors);
            }
            if (node.Parent is not null)
            {
                Assert.Contains(node, node.Parent.Children);
            }
        }
    }

    /// <summary>
    /// Throws an exception if there's any detected continue/break statements that have yet to be
    /// processed. This indicates continue/break detection and/or processing is broken.
    /// </summary>
    public static void EnsureNoRemainingJumps(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;
        List<BinaryBranch> branches = ctx.BinaryBranchNodes!;

        foreach (BinaryBranch bb in branches)
        {
            int startIndex = ((Block)bb.Condition).BlockIndex;
            int endAddress = bb.EndAddress;
            for (int i = startIndex + 1; i < blocks.Count && blocks[i].StartAddress < endAddress; i++)
            {
                Block block = blocks[i];
                if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }] &&
                    block.Successors.Count >= 1 && block.Successors[0].StartAddress >= endAddress)
                {
                    throw new Exception("Found unprocessed break/continue");
                }
            }
        }
    }

    /// <summary>
    /// Asserts that the decompilation result of the assembly equals the provided GML, as a string.
    /// </summary>
    public static DecompileContext VerifyDecompileResult(string asm, string gml, GameContextMock? gameContext = null, DecompileSettings? decompileSettings = null)
    {
        gameContext ??= new();
        DecompileContext decompilerContext = new(gameContext, GetCode(asm, gameContext), decompileSettings);
        string decompileResult = decompilerContext.DecompileToString().Trim();
        Assert.Equal(gml.Trim().ReplaceLineEndings("\n"), decompileResult);
        return decompilerContext;
    }

    /// <summary>
    /// Utility function to lex GML code with the compiler, for testing.
    /// </summary>
    public static LexContext Lex(string code, GameContextMock? gameContext = null)
    {
        CompileContext compileContext = new(code, CompileScriptKind.Script, null, gameContext ?? new());
        LexContext rootLexContext = new(compileContext, compileContext.Code);
        rootLexContext.Tokenize();
        rootLexContext.PostProcessTokens();
        return rootLexContext;
    }

    /// <summary>
    /// Asserts that a list of tokens match a list of text and type pairs, corresponding to each expected token.
    /// </summary>
    public static void AssertTokens((string Text, Type Type)[] expected, List<IToken> tokens)
    {
        Assert.Equal(expected.Length, tokens.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Text.ReplaceLineEndings("\n"), tokens[i].ToString()?.ReplaceLineEndings("\n"));
            Assert.IsType(expected[i].Type, tokens[i]);
        }
    }

    /// <summary>
    /// Utility function to parse GML code with the compiler, for testing.
    /// </summary>
    public static ParseContext Parse(string code, GameContextMock? gameContext = null)
    {
        LexContext lexContext = Lex(code, gameContext);
        ParseContext parseContext = new(lexContext.CompileContext, lexContext.Tokens);
        parseContext.Parse();
        return parseContext;
    }

    /// <summary>
    /// Utility function to parse and post-process GML code with the compiler, for testing.
    /// </summary>
    public static ParseContext ParseAndPostProcess(string code, GameContextMock? gameContext = null)
    {
        ParseContext parseContext = Parse(code, gameContext);
        parseContext.PostProcessTree();
        return parseContext;
    }

    /// <summary>
    /// Compiles the given GML code using the given game context.
    /// </summary>
    public static GMCode CompileCode(string code, bool isGlobalScript = false, GameContextMock? gameContext = null)
    {
        // Compile code
        gameContext ??= new();
        string? globalScriptName = null;
        CompileScriptKind scriptKind = CompileScriptKind.ObjectEvent;
        if (isGlobalScript)
        {
            globalScriptName = "GlobalScriptMockName";
            scriptKind = CompileScriptKind.GlobalScript;
            gameContext.DefineMockAsset(AssetType.Script, 0, globalScriptName);
        }
        CompileContext context = new(code, scriptKind, globalScriptName, gameContext);
        context.Compile();

        // Throw if errors encountered
        if (context.HasErrors)
        {
            throw new TestCompileErrorException("Compile errors occurred");
        }

        // Resolve FunctionEntry instances
        int structCounter = 1;
        int anonCounter = 1;
        int scriptIndexCounter = 1;
        foreach (FunctionEntry functionEntry in context.OutputFunctionEntries!)
        {
            // Determine function name
            if (functionEntry is { FunctionName: string globalFuncName, DeclaredInRootScope: true })
            {
                // For global functions, declare them in global scope accordingly
                if (isGlobalScript)
                {
                    string name = $"global_func_{globalFuncName}";
                    GMFunction actualFunction = new(name);
                    functionEntry.ResolveFunction(actualFunction, "unused for tests");
                    ((GlobalFunctions)gameContext.GlobalFunctions).DefineFunction(globalFuncName, actualFunction);
                    gameContext.DefineMockAsset(AssetType.Script, scriptIndexCounter++, name);
                }
                else
                {
                    string name = $"regular_func_{globalFuncName}";
                    functionEntry.ResolveFunction(new GMFunction(name), "unused for tests");
                    gameContext.DefineMockAsset(AssetType.Script, scriptIndexCounter++, name);
                }
            }
            else if (functionEntry.FunctionName is string regularFuncName)
            {
                string name = $"regular_func_{regularFuncName}";
                functionEntry.ResolveFunction(new GMFunction(name), "unused for tests");
                gameContext.DefineMockAsset(AssetType.Script, scriptIndexCounter++, name);
            }
            else if (functionEntry.Kind == FunctionEntryKind.StructInstantiation)
            {
                string structName = $"__struct__{structCounter++}";
                functionEntry.ResolveStructName(structName);

                string name = $"struct_func_{structName}";
                functionEntry.ResolveFunction(new GMFunction(name), "unused for tests");
                gameContext.DefineMockAsset(AssetType.Script, scriptIndexCounter++, name);
            }
            else
            {
                string name;
                if (functionEntry.StaticVariableName is not null)
                {
                    name = $"{functionEntry.StaticVariableName}_anon_func_{anonCounter++}";
                }
                else
                {
                    name = $"anon_func_{anonCounter++}";
                }
                functionEntry.ResolveFunction(new GMFunction(name), "unused for tests");
                gameContext.DefineMockAsset(AssetType.Script, scriptIndexCounter++, name);
            }
        }

        // Link instructions to data
        context.Link();

        // Throw if errors encountered
        if (context.HasErrors)
        {
            throw new TestCompileErrorException("Link errors occurred");
        }

        // Create code entries
        GMCode rootEntry = new("root", new(context.OutputInstructions!.Count));
        foreach (IGMInstruction instr in context.OutputInstructions!)
        {
            GMInstruction castInstr = (GMInstruction)instr;
            rootEntry.Instructions.Add(castInstr);
            rootEntry.Length += IGMInstruction.GetSize(castInstr);
        }
        foreach (FunctionEntry func in context.OutputFunctionEntries!)
        {
            GMCode childEntry = new(func.Function!.Name.Content, rootEntry.Instructions)
            {
                Parent = rootEntry,
                StartOffset = func.BytecodeOffset,
                LocalCount = func.Scope.LocalCount,
                ArgumentCount = func.ArgumentCount,
                Length = rootEntry.Length
            };
            rootEntry.Children.Add(childEntry);
        }

        return rootEntry;
    }

    /// <summary>
    /// Asserts that the given GML code is equivalent to the given bytecode assembly.
    /// </summary>
    public static void AssertBytecode(string code, string assembly, bool isGlobalScript = false, GameContextMock? gameContext = null)
    {
        // Generate compiled code
        GMCode generated = CompileCode(code, isGlobalScript, gameContext);

        // Generate comparison code
        GMCode comparison = GetCode(assembly, gameContext);

        // Compare the instructions
        for (int i = 0; i < comparison.InstructionCount; i++)
        {
            if (i >= generated.InstructionCount)
            {
                Assert.Equal(comparison.InstructionCount, generated.Instructions.Count);
            }
            GMInstruction comparisonInstr = (GMInstruction)comparison.GetInstruction(i);
            GMInstruction actualInstr = generated.Instructions[i];
            Assert.Equal(comparisonInstr.Address, actualInstr.Address);
            Assert.Equal(comparisonInstr.Kind, actualInstr.Kind);
            Assert.Equal(comparisonInstr.Type1, actualInstr.Type1);
            Assert.Equal(comparisonInstr.Type2, actualInstr.Type2);
            Assert.Equal(comparisonInstr.InstType, actualInstr.InstType);
            Assert.Equal(comparisonInstr.ReferenceVarType, actualInstr.ReferenceVarType);
            Assert.Equal(comparisonInstr.ArgumentCount, actualInstr.ArgumentCount);
            Assert.Equal(comparisonInstr.AssetReferenceId, actualInstr.AssetReferenceId);
            Assert.Equal(comparisonInstr.AssetReferenceType, actualInstr.AssetReferenceType);
            Assert.Equal(comparisonInstr.BranchOffset, actualInstr.BranchOffset);
            Assert.Equal(comparisonInstr.ExtKind, actualInstr.ExtKind);
            Assert.Equal(comparisonInstr.PopWithContextExit, actualInstr.PopWithContextExit);
            Assert.Equal(comparisonInstr.PopSwapSize, actualInstr.PopSwapSize);
            Assert.Equal(comparisonInstr.DuplicationSize, actualInstr.DuplicationSize);
            Assert.Equal(comparisonInstr.DuplicationSize2, actualInstr.DuplicationSize2);
            Assert.Equal(comparisonInstr.ValueInt, actualInstr.ValueInt);
            Assert.Equal(comparisonInstr.ValueBool, actualInstr.ValueBool);
            Assert.Equal(comparisonInstr.ValueDouble, actualInstr.ValueDouble);
            Assert.Equal(comparisonInstr.ValueLong, actualInstr.ValueLong);
            Assert.Equal(comparisonInstr.ValueShort, actualInstr.ValueShort);
            if (comparisonInstr.ValueString is IGMString str)
            {
                Assert.Equal(str.Content, actualInstr.ValueString!.Content);
            }
            else
            {
                Assert.Null(actualInstr.ValueString);
            }
            if (comparisonInstr.ResolvedVariable is IGMVariable variable)
            {
                Assert.Equal(variable.Name.Content, actualInstr.ResolvedVariable!.Name.Content);

                // Self sometimes becomes builtin/stacktop for instructions, but not the actual variable itself
                if (actualInstr.ResolvedVariable!.InstanceType != IGMInstruction.InstanceType.Self ||
                    variable.InstanceType is not (IGMInstruction.InstanceType.Builtin or 
                                                  IGMInstruction.InstanceType.StackTop))
                {
                    Assert.Equal(variable.InstanceType, actualInstr.ResolvedVariable!.InstanceType);
                }
            }
            else
            {
                Assert.Null(actualInstr.ResolvedVariable);
            }
            if (comparisonInstr.ResolvedFunction is IGMFunction function)
            {
                Assert.Equal(function.Name.Content, actualInstr.ResolvedFunction!.Name.Content);
            }
            else
            {
                Assert.Null(actualInstr.ResolvedFunction);
            }
        }
        Assert.Equal(comparison.InstructionCount, generated.Instructions.Count);
    }

    /// <summary>
    /// Compiles the given GML code, then decompiles it, ensuring the decompilation result is identical to the source.
    /// </summary>
    public static void VerifyRoundTrip(string code, bool isGlobalScript = false, GameContextMock? gameContext = null, DecompileSettings? decompileSettings = null)
    {
        gameContext ??= new();

        // Compile code
        GMCode generated = CompileCode(code, isGlobalScript, gameContext);

        // Decompile generated code entry
        DecompileContext decompilerContext = new(gameContext, generated, decompileSettings);
        string decompileResult = decompilerContext.DecompileToString();

        // Ensure code is identical
        Assert.Equal(code.Trim().ReplaceLineEndings("\n"), decompileResult.Trim());
    }

    /// <summary>
    /// Compiles the given GML code, then decompiles it, ensuring the decompilation result is identical to the expected result.
    /// </summary>
    public static void VerifyRoundTrip(string code, string expected, bool isGlobalScript = false, GameContextMock? gameContext = null, DecompileSettings? decompileSettings = null)
    {
        gameContext ??= new();

        // Compile code
        GMCode generated = CompileCode(code, isGlobalScript, gameContext);

        // Decompile generated code entry
        DecompileContext decompilerContext = new(gameContext, generated, decompileSettings);
        string decompileResult = decompilerContext.DecompileToString();

        // Ensure code is identical to expected decompilation
        Assert.Equal(expected.Trim().ReplaceLineEndings("\n"), decompileResult.Trim());
    }
}
