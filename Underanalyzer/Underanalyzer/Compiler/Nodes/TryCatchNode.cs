/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.Compiler.Nodes.AssignNode;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a "try" statement in the AST.
/// </summary>
internal sealed class TryCatchNode : IASTNode
{
    /// <summary>
    /// Statement/block to be executed under the "try" part of the statement.
    /// </summary>
    public IASTNode Try { get; private set; }

    /// <summary>
    /// Statement/block to be executed under the "catch" part of the statement, if one exists.
    /// </summary>
    public IASTNode? Catch { get; private set; }

    /// <summary>
    /// Local variable name used for the value caught by the "catch" part of the statement.
    /// </summary>
    public string? CatchVariableName { get; private set; }

    /// <summary>
    /// Statement/block to be executed under the "finally" part of the statement, if one exists.
    /// </summary>
    public IASTNode? Finally { get; private set; }

    /// <summary>
    /// Whether the try block encountered an "escape" keyword during parsing.
    /// </summary>
    public bool TryEscape { get; init; }

    /// <summary>
    /// Whether the catch block encountered an "escape" keyword during parsing.
    /// </summary>
    public bool CatchEscape { get; init; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private TryCatchNode(IToken? nearbyToken, IASTNode @try, IASTNode? @catch, string? catchVariableName, IASTNode? @finally, bool tryEscape, bool catchEscape)
    {
        NearbyToken = nearbyToken;
        Try = @try;
        Catch = @catch;
        CatchVariableName = catchVariableName;
        Finally = @finally;
        TryEscape = tryEscape;
        CatchEscape = catchEscape;
    }

    /// <summary>
    /// Creates a try statement node, parsing from the given context's current position.
    /// </summary>
    public static IASTNode? Parse(ParseContext context)
    {
        // Parse "try" keyword
        if (context.EnsureToken(KeywordKind.Try) is not TokenKeyword tokenKeyword)
        {
            return null;
        }

        // Parse "try" part of the statement
        int previousEscapeCount = context.ExitReturnBreakContinueCount;
        if (Statements.ParseStatement(context) is not IASTNode @try)
        {
            return null;
        }

        // If exit/return/continue/break were encountered while parsing try, note that for later
        bool tryEscape = (context.ExitReturnBreakContinueCount != previousEscapeCount);

        // Parse "catch" part of the statement, if it exists
        IASTNode? @catch = null;
        string? catchVariableName = null;
        bool catchEscape = false;
        if (context.IsCurrentToken(KeywordKind.Catch))
        {
            context.Position++;

            // Parse the catch variable name
            context.EnsureToken(SeparatorKind.GroupOpen);
            if (!context.EndOfCode && context.Tokens[context.Position] is TokenVariable tokenVariable)
            {
                context.Position++;
                catchVariableName = tokenVariable.Text;

                // Add to this scope's local list
                // TODO: check for duplicates and conflicts with named arguments/statics?
                context.CurrentScope.DeclareLocal(tokenVariable.Text);
            }
            context.EnsureToken(SeparatorKind.GroupClose);

            // Parse the actual statement/body
            previousEscapeCount = context.ExitReturnBreakContinueCount + context.ThrowCount;
            @catch = Statements.ParseStatement(context);

            // If exit/return/continue/break/throw were encountered while parsing catch, note that for later
            catchEscape = (context.ExitReturnBreakContinueCount + context.ThrowCount != previousEscapeCount);
        }

        // Parse "finally" part of the statement, if it exists
        IASTNode? @finally = null;
        if (context.IsCurrentToken(KeywordKind.Finally))
        {
            context.Position++;
            @finally = Statements.ParseStatement(context);
        }

        // Create final statement
        if (@catch is null && @finally is null)
        {
            // Apparently it's valid to just have "try," which effectively does nothing
            return @try;
        }
        return new TryCatchNode(tokenKeyword, @try, @catch, catchVariableName, @finally, tryEscape, catchEscape);
    }

    /// <summary>
    /// Generates a loop around a block, for purposes of preventing break/continue from escaping.
    /// </summary>
    private WhileLoopNode GenerateBlockLoop(ParseContext context, TryStatementContext tryContext, IASTNode block)
    {
        // Create block inside of loop
        BlockNode innerBlock = BlockNode.CreateEmpty(NearbyToken, 3);

        // If continue variable is set at the top of the loop, break out of loop
        context.CurrentScope.DeclareLocal(tryContext.ContinueVariableName);
        innerBlock.Children.Add(new IfNode(NearbyToken, new SimpleVariableNode(tryContext.ContinueVariableName, null, InstanceType.Local), new BreakNode(NearbyToken), null));

        // Actual original block
        innerBlock.Children.Add(block);

        // Break at the end of the loop always (it shouldn't actually loop)
        innerBlock.Children.Add(new BreakNode(NearbyToken));

        // Create actual loop node
        return new WhileLoopNode(NearbyToken, new BooleanNode(true, NearbyToken), innerBlock);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Early post-process finally nodes, and add to finally nodes
        if (Finally is not null)
        {
            // TODO? official compiler can duplicate finally inside of itself if you throw inside of it...

            // Process finally, with special flag to ensure control flow statements are not used
            bool previousProcessingFinally = context.ProcessingFinally;
            context.ProcessingFinally = true;
            Finally = Finally.PostProcess(context);
            context.ProcessingFinally = previousProcessingFinally;

            // Add finally block to list of nodes (so they can be duplicated later...)
            context.CurrentScope.TryFinallyNodes.Add(Finally);
        }

        // Get unique names for this try statement's variables
        int uniqueIndex = context.CompileContext.GameContext.CodeBuilder.GenerateTryVariableID(context.TryStatementProcessIndex++);
        string breakName = $"{VMConstants.TryBreakVariable}{uniqueIndex}";
        string continueName = $"{VMConstants.TryContinueVariable}{uniqueIndex}";

        // Create context for this try statement
        TryStatementContext? previousTryContext = context.TryStatementContext;
        TryStatementContext newTryContext = new(breakName, continueName, Finally is not null);
        context.TryStatementContext = newTryContext;
        bool breakContinueUsedAnywhere = false;

        // Only generate finally code before throw if either an escape keyword was detected inside of
        // try block during parsing, or if there's no catch block.
        newTryContext.ThrowFinallyGeneration = TryEscape || Catch is null;

        // Post-process main try block
        newTryContext.ShouldGenerateBreakContinueCode = true;
        newTryContext.HasBreakContinueVariable = false;
        Try = Try.PostProcess(context);
        if (newTryContext.HasBreakContinueVariable)
        {
            // Break/continue used: generate loop around block to prevent escape
            Try = GenerateBlockLoop(context, newTryContext, Try);
            breakContinueUsedAnywhere = true;
        }

        // Post-process catch block
        if (Catch is not null)
        {
            // Only generate finally code before throw if an escape keyword was detected inside of
            // catch block during parsing.
            newTryContext.ThrowFinallyGeneration = CatchEscape;

            newTryContext.ShouldGenerateBreakContinueCode = true;
            newTryContext.HasBreakContinueVariable = false;
            Catch = Catch.PostProcess(context);
            if (newTryContext.HasBreakContinueVariable)
            {
                // Break/continue used: generate loop around block to prevent escape
                Catch = GenerateBlockLoop(context, newTryContext, Catch);
                breakContinueUsedAnywhere = true;
            }
        }

        // Restore previous try statement context
        context.TryStatementContext = previousTryContext;

        // Remove from finally nodes
        if (Finally is not null)
        {
            context.CurrentScope.TryFinallyNodes.RemoveAt(context.CurrentScope.TryFinallyNodes.Count - 1);
        }

        // If break or continue variables were used anywhere, generate their initial assignments.
        // Also, if inside of a relevant break/continue context (such as an outer loop),
        // generate code to propagate break/continue to those.
        if (breakContinueUsedAnywhere)
        {
            // Create block with enough room for generated code
            bool insideBreakContinueContext = context.CurrentScope.ProcessingBreakContinueContext;
            BlockNode newBlock = BlockNode.CreateEmpty(NearbyToken, insideBreakContinueContext ? 5 : 3);

            // Generate initial assignments
            context.CurrentScope.DeclareLocal(breakName);
            context.CurrentScope.DeclareLocal(continueName);
            newBlock.Children.Add(new AssignNode(AssignKind.Normal, new SimpleVariableNode(breakName, null, InstanceType.Local), new NumberNode(0, NearbyToken)));
            newBlock.Children.Add(new AssignNode(AssignKind.Normal, new SimpleVariableNode(continueName, null, InstanceType.Local), new NumberNode(0, NearbyToken)));

            // Actual main try statement
            newBlock.Children.Add(this);

            // Generate extra code to propagate break/continue if required
            if (insideBreakContinueContext)
            {
                newBlock.Children.Add(new IfNode(NearbyToken, new SimpleVariableNode(continueName, null, InstanceType.Local), new ContinueNode(NearbyToken), null));
                newBlock.Children.Add(new IfNode(NearbyToken, new SimpleVariableNode(breakName, null, InstanceType.Local), new BreakNode(NearbyToken), null));
            }

            return newBlock;
        }

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new TryCatchNode(
            NearbyToken,
            Try.Duplicate(context),
            Catch?.Duplicate(context),
            CatchVariableName,
            Finally?.Duplicate(context),
            TryEscape,
            CatchEscape
        );
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Hook the try statement with addresses to finally/catch blocks
        IGMInstruction finallyInstr = context.Emit(Opcode.Push, (int)-1, DataType.Int32);
        context.Emit(Opcode.Convert, DataType.Int32, DataType.Variable);
        IGMInstruction catchInstr = context.Emit(Opcode.Push, (int)-1, DataType.Int32);
        context.Emit(Opcode.Convert, DataType.Int32, DataType.Variable);
        context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.TryHookFunction), 2);
        context.Emit(Opcode.PopDelete, DataType.Variable);

        // Generate main try block
        string? previousFunctionCallBeforeExit = context.FunctionCallBeforeExit;
        context.FunctionCallBeforeExit = VMConstants.TryUnhookFunction;
        Try.GenerateCode(context);
        context.FunctionCallBeforeExit = previousFunctionCallBeforeExit;

        // Reset array owner ID
        if (context.CanGenerateArrayOwners)
        {
            context.LastArrayOwnerID = -1;
        }

        // Generate catch block
        if (Catch is IASTNode catchBlock)
        {
            // Skip catch block if entering from end of main try block
            SingleForwardBranchPatch skipCatchPatch = new(context, context.Emit(Opcode.Branch));

            // Mark the catch block destination
            context.PatchPush(catchInstr, context.Position);

            // Store thrown value in local variable and unhook try statement
            context.Emit(Opcode.Pop, new VariablePatch(CatchVariableName!, InstanceType.Local), DataType.Variable, DataType.Variable);
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.TryUnhookFunction), 0);
            context.Emit(Opcode.PopDelete, DataType.Variable);

            // Generate catch block
            context.FunctionCallBeforeExit = VMConstants.FinishCatchFunction;
            catchBlock.GenerateCode(context);
            context.FunctionCallBeforeExit = previousFunctionCallBeforeExit;

            // Finish catch and skip try unhooking (so it's not done a second time)
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.FinishCatchFunction), 0);
            context.Emit(Opcode.PopDelete, DataType.Variable);
            SingleForwardBranchPatch skipRegularTryUnhookPatch = new(context, context.Emit(Opcode.Branch));

            // Mark "finally section" of the try statement
            context.PatchPush(finallyInstr, context.Position);

            // Regular try unhooking code
            skipCatchPatch.Patch(context);
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.TryUnhookFunction), 0);
            context.Emit(Opcode.PopDelete, DataType.Variable);
            skipRegularTryUnhookPatch.Patch(context);

            // Reset array owner ID
            if (context.CanGenerateArrayOwners)
            {
                context.LastArrayOwnerID = -1;
            }
        }
        else
        {
            // Mark "finally section" of the try statement
            context.PatchPush(finallyInstr, context.Position);

            // Try unhooking code for when there's no catch block
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.TryUnhookFunction), 0);
            context.Emit(Opcode.PopDelete, DataType.Variable);
        }

        // Generate finally block
        if (Finally is IASTNode finallyBlock)
        {
            // Generate block immediately (no function to call before exit, apparently)
            finallyBlock.GenerateCode(context);

            // Finish finally
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.FinishFinallyFunction), 0);
            context.Emit(Opcode.PopDelete, DataType.Variable);

            // Completely pointless branch here for some reason...
            SingleForwardBranchPatch uselessPatch = new(context, context.Emit(Opcode.Branch));
            uselessPatch.Patch(context);

            // Reset array owner ID
            if (context.CanGenerateArrayOwners)
            {
                context.LastArrayOwnerID = -1;
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Try;
        if (Catch is not null)
        {
            yield return Catch;
        }
        if (Finally is not null)
        {
            yield return Finally;
        }
    }
}
