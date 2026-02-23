/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Compiler;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Mock;

/// <summary>
/// A default implementation of <see cref="IGameContext"/>.
/// </summary>
public class GameContextMock : IGameContext
{
    /// <inheritdoc/>
    public bool UsingGMS2OrLater { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingGMLv2 { get; set; } = true;
    /// <inheritdoc/>
    public bool Bytecode14OrLower { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingStringRealOptimizations { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingLogicalShortCircuit { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingLongCompoundBitwise { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingFinallyBeforeThrow { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingConstructorSetStatic { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingReentrantStatic { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingArrayCopyOnWrite { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingNewArrayOwners { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingNullishOperator { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingNewFunctionVariables { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingSelfToBuiltin { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingGlobalConstantFunction { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingObjectFunctionForesight { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingBetterTryBreakContinue { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingBuiltinDefaultArguments { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingExtraRepeatInstruction { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingTypedBooleans { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingAssetReferences { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingRoomInstanceReferences { get; set; } = true;
    /// <inheritdoc/>
    public bool UsingFunctionScriptReferences { get; set; } = false;
    /// <inheritdoc/>
    public bool UsingNewFunctionResolution { get; set; } = false;
    /// <inheritdoc/>
    public IGlobalFunctions GlobalFunctions { get; } = new GlobalFunctions();
    /// <inheritdoc/>
    public GameSpecificRegistry GameSpecificRegistry { get; set; } = new();
    /// <inheritdoc/>
    public IBuiltins Builtins { get; } = new BuiltinsMock();
    /// <inheritdoc/>
    public ICodeBuilder CodeBuilder { get; }

    /// <summary>
    /// A Dictionary for maintaining the same variables throughout a compilation process.
    /// </summary>
    internal Dictionary<(string, IGMInstruction.InstanceType), GMVariable> MockVariables { get; } = [];

    /// <summary>
    /// A Dictionary that mocks asset types and their contents.
    /// </summary>
    private readonly Dictionary<AssetType, Dictionary<int, string>> _mockAssetsByType = [];

    /// <summary>
    /// A Dictionary that mocks assets by their name.
    /// </summary>
    private readonly Dictionary<string, int> _mockAssetsByName = [];

    /// <summary>
    /// A Dictionary that mocks script assets by their name.
    /// </summary>
    private readonly Dictionary<string, int> _mockScriptsByName = [];

    public GameContextMock()
    {
        CodeBuilder = new CodeBuilderMock(this);
    }

    /// <summary>
    /// Define a new mock asset that gets added to <see cref="_mockAssetsByType"/>.
    /// </summary>
    /// <param name="assetType">The type of the asset.</param>
    /// <param name="assetIndex">The index of the asset.</param>
    /// <param name="assetName">The name of the asset.</param>
    public void DefineMockAsset(AssetType assetType, int assetIndex, string assetName)
    {
        if (!_mockAssetsByType.TryGetValue(assetType, out Dictionary<int, string>? assets))
        {
            assets = [];
            _mockAssetsByType.Add(assetType, assets);
        }
        assets[assetIndex] = assetName;

        int id = assetIndex | (UsingAssetReferences ? ((int)assetType << 24) : 0);
        _mockAssetsByName[assetName] = id;
        if (assetType == AssetType.Script)
        {
            _mockScriptsByName[assetName] = id;
        }
    }
    
    /// <summary>
    /// Fetches an asset from <see cref="_mockAssetsByType"/>.
    /// </summary>
    /// <param name="assetType">The asset type of the asset that should be fetched.</param>
    /// <param name="assetIndex">The index of the asset that should be fetched.</param>
    /// <returns>The name of the asset, or <see langword="null"/> if it does not exist.</returns>
    public string? GetMockAsset(AssetType assetType, int assetIndex)
    {
        if (!_mockAssetsByType.TryGetValue(assetType, out var dict))
        {
            return null;
        }

        if (dict.TryGetValue(assetIndex, out string? name))
        {
            return name;
        }
        return null;
    }
    
    /// <inheritdoc/>
    public string? GetAssetName(AssetType assetType, int assetIndex)
    {
        return assetType switch
        {
            AssetType.RoomInstance => assetIndex >= 100000 ? $"inst_id_{assetIndex}" : null,
            _ => GetMockAsset(assetType, assetIndex)
        };
    }

    /// <inheritdoc/>
    public bool GetAssetId(string assetName, out int assetId)
    {
        return _mockAssetsByName.TryGetValue(assetName, out assetId);
    }

    /// <inheritdoc/>
    public bool GetRoomInstanceId(string assetName, out int assetId)
    {
        const string instIdPrefix = "inst_id_";
        if (assetName.StartsWith(instIdPrefix, StringComparison.Ordinal) &&
            int.TryParse(assetName.AsSpan()[instIdPrefix.Length..], out int instanceId) &&
            instanceId >= 100000)
        {
            assetId = instanceId;
            if (UsingRoomInstanceReferences)
            {
                assetId |= ((int)AssetType.RoomInstance << 24);
            }
            return true;
        }
        assetId = 0;
        return false;
    }

    /// <inheritdoc/>
    public bool GetScriptId(string scriptName, out int assetId)
    {
        return _mockScriptsByName.TryGetValue(scriptName, out assetId);
    }

    /// <inheritdoc/>
    public bool GetScriptIdByFunctionName(string functionName, out int assetId)
    {
        return _mockScriptsByName.TryGetValue($"global_func_{functionName}", out assetId);
    }
}
