/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler;

/// <summary>
/// A decompilation context belonging to a single code entry in a game.
/// </summary>
public class DecompileContext
{
    /// <summary>
    /// The game context this decompile context belongs to.
    /// </summary>
    public IGameContext GameContext { get; }

    /// <summary>
    /// The specific code entry within the game this decompile context belongs to.
    /// </summary>
    public IGMCode Code { get; private set; }

    /// <summary>
    /// The decompilation settings to be used for this decompile context in its operation.
    /// </summary>/
    public IDecompileSettings Settings { get; private set; }

    /// <summary>
    /// Any warnings produced throughout the decompilation process.
    /// </summary>
    public List<IDecompileWarning> Warnings { get; } = [];

    // Helpers to refer to data on game context
    internal bool OlderThanBytecode15 { get => GameContext.Bytecode14OrLower; }
    internal bool GMLv2 { get => GameContext.UsingGMLv2; }

    // Data structures used (and re-used) for decompilation, as well as tests
    internal List<Block>? Blocks { get; set; }
    internal Dictionary<int, Block>? BlocksByAddress { get; set; }
    internal List<Fragment>? FragmentNodes { get; set; }
    internal List<Loop>? LoopNodes { get; set; }
    internal List<Block>? ShortCircuitBlocks { get; set; }
    internal List<ShortCircuit>? ShortCircuitNodes { get; set; }
    internal List<StaticInit>? StaticInitNodes { get; set; }
    internal List<TryCatch>? TryCatchNodes { get; set; }
    internal List<Nullish>? NullishNodes { get; set; }
    internal List<BinaryBranch>? BinaryBranchNodes { get; set; }
    internal HashSet<IControlFlowNode>? SwitchEndNodes { get; set; }
    internal List<Switch.SwitchDetectionData>? SwitchData { get; set; }
    internal HashSet<Block>? SwitchContinueBlocks { get; set; }
    internal HashSet<Block>? SwitchIgnoreJumpBlocks { get; set; }
    internal List<Switch>? SwitchNodes { get; set; }
    internal Dictionary<Block, Loop>? BlockSurroundingLoops { get; set; }
    internal Dictionary<Block, int>? BlockAfterLimits { get; set; }
    internal List<GMEnum> EnumDeclarations { get; set; } = [];
    internal Dictionary<string, GMEnum> NameToEnumDeclaration { get; set; } = [];
    internal GMEnum? UnknownEnumDeclaration { get; set; } = null;
    internal int UnknownEnumReferenceCount { get; set; } = 0;

    public DecompileContext(IGameContext gameContext, IGMCode code, IDecompileSettings? settings = null)
    {
        GameContext = gameContext;
        Code = code;
        Settings = settings ?? new DecompileSettings();
    }

    // Constructor used for control flow tests
    internal DecompileContext(IGMCode code) 
    {
        Code = code;
        GameContext = new Mock.GameContextMock();
        Settings = new DecompileSettings();
    }

    // Solely decompiles control flow from the code entry
    private void DecompileControlFlow()
    {
        try
        {
            Block.FindBlocks(this);
            Fragment.FindFragments(this);
            StaticInit.FindStaticInits(this);
            Nullish.FindNullish(this);
            ShortCircuit.FindShortCircuits(this);
            Loop.FindLoops(this);
            ShortCircuit.InsertShortCircuits(this);
            TryCatch.FindTryCatch(this);
            Switch.FindSwitchStatements(this);
            BinaryBranch.FindBinaryBranches(this);
            Switch.InsertSwitchStatements(this);
            TryCatch.CleanTryEndBranches(this);
        }
        catch (DecompilerException ex)
        {
            throw new DecompilerException($"Decompiler error during control flow analysis: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new DecompilerException($"Unexpected exception thrown in decompiler during control flow analysis: {ex.Message}", ex);
        }
    }

    // Decompiles the AST from the code entry4
    private AST.IStatementNode DecompileAST()
    {
        try
        {
            return new AST.ASTBuilder(this).Build();
        }
        catch (DecompilerException ex)
        {
            throw new DecompilerException($"Decompiler error during AST building: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new DecompilerException($"Unexpected exception thrown in decompiler during AST building: {ex.Message}", ex);
        }
    }

    // Decompiles the AST from the code entry
    private AST.IStatementNode CleanupAST(AST.IStatementNode ast)
    {
        try
        {
            AST.ASTCleaner cleaner = new(this);
            AST.IStatementNode cleaned = ast.Clean(cleaner);
            if (Settings.CreateEnumDeclarations)
            {
                AST.EnumDeclNode.GenerateDeclarations(cleaner, cleaned);
            }
            return cleaned;
        }
        catch (DecompilerException ex)
        {
            throw new DecompilerException($"Decompiler error during AST cleanup: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new DecompilerException($"Unexpected exception thrown in decompiler during AST cleanup: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Decompiles the code entry, and returns the AST output.
    /// </summary>
    public AST.IStatementNode DecompileToAST()
    {
        DecompileControlFlow();
        AST.IStatementNode ast = DecompileAST();
        return CleanupAST(ast);
    }

    /// <summary>
    /// Decompiles the code entry, and returns the string output.
    /// </summary>
    public string DecompileToString()
    {
        AST.IStatementNode ast = DecompileToAST();
        try
        {
            AST.ASTPrinter printer = new(this);
            if (Settings.PrintWarnings)
            {
                printer.PrintRemainingWarnings(true);
            }
            ast.Print(printer);
            if (Settings.PrintWarnings)
            {
                printer.PrintRemainingWarnings(false);
            }
            return printer.OutputString;
        }
        catch (DecompilerException ex)
        {
            throw new DecompilerException($"Decompiler error during AST printing: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new DecompilerException($"Unexpected exception thrown in decompiler during AST printing: {ex.Message}", ex);
        }
    }
}
