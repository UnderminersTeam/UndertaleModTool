/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Macro type for GameMaker asset references.
/// </summary>
public class AssetMacroType(AssetType type) : IMacroTypeInt32
{
    public AssetType Type { get; } = type;

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data)
    {
        // Ensure we don't resolve this on newer GameMaker versions where this is unnecessary
        if (cleaner.Context.GameContext.UsingAssetReferences)
        {
            if (cleaner.Context.GameContext.UsingRoomInstanceReferences || Type != AssetType.RoomInstance)
            {
                return null;
            }
        }

        // Check for asset name with the given type
        string? assetName = cleaner.Context.GameContext.GetAssetName(Type, data);
        if (assetName is not null)
        {
            return new MacroValueNode(assetName);
        }

        return null;
    }
}
