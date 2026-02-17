/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents an asset reference in the AST.
/// </summary>
public class AssetReferenceNode(int assetId, AssetType assetType) : IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// The ID of the asset being referenced.
    /// </summary>
    public int AssetId { get; } = assetId;

    /// <summary>
    /// The type of the asset being referenced.
    /// </summary>
    public AssetType AssetType { get; } = assetType;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    /// <inheritdoc/>
    public string ConditionalTypeName => "AssetReference";

    /// <inheritdoc/>
    public string ConditionalValue => $"{AssetType}:{AssetId}";

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        string? assetName = printer.Context.GameContext.GetAssetName(AssetType, AssetId);
        if (assetName is not null)
        {
            printer.Write(assetName);
        }
        else
        {
            // Unknown asset ID
            if (Group)
            {
                printer.Write('(');
            }
            printer.Write(AssetId);
            if (Group)
            {
                printer.Write(')');
            }
        }
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return [];
    }
}
