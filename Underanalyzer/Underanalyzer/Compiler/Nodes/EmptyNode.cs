/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents an empty node in the AST, which is equivalent to an empty block.
/// </summary>
internal sealed class EmptyNode : IASTNode
{
    // Reusable instance of this node, when no token is associated
    private static readonly EmptyNode _mainInstance = new(null);

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private EmptyNode(IToken? nearbyToken)
    {
        NearbyToken = nearbyToken;
    }

    /// <summary>
    /// Returns a generic empty node instance. The returned instance is reused globally.
    /// </summary>
    public static EmptyNode Create()
    {
        return _mainInstance;
    }

    /// <summary>
    /// Returns an empty node instance. The returned instance is unique, due to its associated token.
    /// </summary>
    public static EmptyNode Create(IToken? nearbyToken)
    {
        return new EmptyNode(nearbyToken);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return this;
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }
}
