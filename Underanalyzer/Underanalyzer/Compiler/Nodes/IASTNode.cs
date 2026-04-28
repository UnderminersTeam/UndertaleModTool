/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a single node in the AST.
/// </summary>
internal interface IASTNode
{
    /// <summary>
    /// A token nearby to this node, for use in error messages.
    /// </summary>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Post-processes this node (e.g. for optimization/code rewriting), returning a new (or the same) node.
    /// </summary>
    public IASTNode PostProcess(ParseContext context);

    /// <summary>
    /// Duplicates this node and all sub-nodes.
    /// </summary>
    /// <remarks>
    /// For immutable leaf nodes, this can return the same instance.
    /// </remarks>
    public IASTNode Duplicate(ParseContext context);

    /// <summary>
    /// Generates bytecode for this node.
    /// </summary>
    public void GenerateCode(BytecodeContext context);

    /// <summary>
    /// Enumerates all children nodes in the tree.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IASTNode> EnumerateChildren();
}

/// <summary>
/// Represents a node in the AST that is a constant value.
/// </summary>
internal interface IConstantASTNode : IASTNode
{
}

/// <summary>
/// Represents a node in the AST that can be assigned to.
/// </summary>
internal interface IAssignableASTNode : IASTNode
{
    /// <summary>
    /// Generates assignment code for this node.
    /// </summary>
    public void GenerateAssignCode(BytecodeContext context);

    /// <summary>
    /// Generates compound assignment code for this node.
    /// </summary>
    public void GenerateCompoundAssignCode(BytecodeContext context, IASTNode expression, Opcode operationOpcode);

    /// <summary>
    /// Generates pre/post-increment/decrement assignment code for this node.
    /// </summary>
    public void GeneratePrePostAssignCode(BytecodeContext context, bool isIncrement, bool isPre, bool isStatement);
}

/// <summary>
/// Represents a node in the AST that may be a statement or an expression.
/// </summary>
internal interface IMaybeStatementASTNode : IASTNode
{
    /// <summary>
    /// Whether this call is a standalone statement, rather than an expression.
    /// </summary>
    public bool IsStatement { get; set; }
}

/// <summary>
/// Represents a node in the AST that directly references a variable name.
/// </summary>
internal interface IVariableASTNode : IASTNode
{
    /// <summary>
    /// Variable name being referenced by this node.
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// Builtin variable corresponding to the variable referenced by this node, or null if none.
    /// </summary>
    public IBuiltinVariable? BuiltinVariable { get; }
}
