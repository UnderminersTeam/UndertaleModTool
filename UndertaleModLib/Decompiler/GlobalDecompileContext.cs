using System.Collections.Generic;
using System.Diagnostics;
using System;
using Underanalyzer;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.GameSpecific;
using UndertaleModLib.Models;
using Underanalyzer.Compiler;
using UndertaleModLib.Compiler;
using static UndertaleModLib.Util.AssetReferenceTypes;

namespace UndertaleModLib.Decompiler;

/// <summary>
/// A global game context to track data used by GML decompilation and compilation.
/// </summary>
/// <remarks>
/// Can be used for multiple runs of both the Underanalyzer decompiler and compiler, and is generally thread-safe after initialization.
/// </remarks>
public class GlobalDecompileContext : IGameContext
{
    /// <summary>
    /// Associated game data for this decompile context.
    /// </summary>
    public UndertaleData Data { get; }

    // Implementation of Underanalyzer properties
    public bool UsingGMLv2 => Data?.IsVersionAtLeast(2, 3) ?? false;
    public bool Bytecode14OrLower => (Data?.GeneralInfo?.BytecodeVersion ?? 15) <= 14;
    public bool UsingGMS2OrLater => Data?.IsVersionAtLeast(2) ?? false;
    public bool UsingStringRealOptimizations => (Data?.GeneralInfo.Major is >= 2 || Data?.GeneralInfo.Build is 1539 or >= 1763);
    public bool UsingFinallyBeforeThrow => !(Data?.IsVersionAtLeast(2024, 6) ?? false);
    public IGlobalFunctions GlobalFunctions => Data?.GlobalFunctions;
    public bool UsingTypedBooleans => Data?.IsVersionAtLeast(2, 3, 7) ?? false;
    public bool UsingNullishOperator => Data?.IsVersionAtLeast(2, 3, 7) ?? false;
    public bool UsingAssetReferences => Data?.IsVersionAtLeast(2023, 8) ?? false;
    public bool UsingRoomInstanceReferences => Data?.IsVersionAtLeast(2024, 2) ?? false;
    public bool UsingFunctionScriptReferences => Data?.IsVersionAtLeast(2024, 2) ?? false;
    public bool UsingNewFunctionResolution => Data?.IsVersionAtLeast(2024, 13) ?? false;
    public bool UsingLogicalShortCircuit => Data?.ShortCircuit ?? true;
    public bool UsingLongCompoundBitwise => Data?.IsVersionAtLeast(2, 3, 2) ?? false;
    public bool UsingExtraRepeatInstruction => !(Data?.IsNonLTSVersionAtLeast(2022, 11) ?? false);
    public bool UsingConstructorSetStatic => Data?.IsVersionAtLeast(2024, 11) ?? false;
    public bool UsingReentrantStatic => !(Data?.IsVersionAtLeast(2024, 11) ?? false);
    public bool UsingNewFunctionVariables => Data?.IsVersionAtLeast(2024, 2) ?? false;
    public bool UsingSelfToBuiltin => Data?.IsVersionAtLeast(2024, 2) ?? false;
    public bool UsingGlobalConstantFunction => Data?.IsVersionAtLeast(2023, 11) ?? false;
    public bool UsingObjectFunctionForesight => Data?.IsVersionAtLeast(2024, 11) ?? false;
    public bool UsingBetterTryBreakContinue => Data?.IsVersionAtLeast(2024, 11) ?? false;
    public bool UsingBuiltinDefaultArguments => Data?.IsVersionAtLeast(2024, 11) ?? false;
    public bool UsingArrayCopyOnWrite => Data?.ArrayCopyOnWrite ?? false;
    public bool UsingNewArrayOwners => Data?.IsVersionAtLeast(2, 3, 2) ?? false;
    public GameSpecificRegistry GameSpecificRegistry => Data?.GameSpecificRegistry;
    public IBuiltins Builtins { get; private set; } = null;
    public ICodeBuilder CodeBuilder { get; private set; } = null;

    /// <summary>
    /// The current compile group being used for main compile, and for linking.
    /// </summary>
    internal CompileGroup CurrentCompileGroup { get; set; } = null;

    // Lookup from asset name to index (and potentially encoded asset type)
    private Dictionary<string, int> _assetIdLookup = null;

    // Lookup from script name to index (and potentially encoded asset type)
    private Dictionary<string, int> _scriptIdLookup = null;

    // Prefix used for instance IDs, cached per each context
    private readonly string _instanceIdPrefix;

    /// <summary>
    /// Instantiates and initializes a global decompile context for the given <see cref="UndertaleData"/>.
    /// </summary>
    /// <remarks>
    /// Note: This will recalculate the global functions belonging to the given <see cref="UndertaleData"/>,
    /// mutating its state. Therefore, this initialization operation is not thread-safe on its own.
    /// </remarks>
    public GlobalDecompileContext(UndertaleData data)
    {
        Data = data;
        if (Data.ToolInfo?.InstanceIdPrefix is Func<string> prefixGetter)
        {
            _instanceIdPrefix = prefixGetter();
        }
        else
        {
            _instanceIdPrefix = "inst_";
        }
        BuildGlobalFunctionCache(data);
    }

    /// <summary>
    /// Builds the GlobalFunctions cache, required for Underanalyzer's decompiler to recognize function names in global scope.
    /// </summary>
    public static void BuildGlobalFunctionCache(UndertaleData data)
    {
        if (data is null)
        {
            // Nothing to calculate
            return;
        }

        if (data.IsVersionAtLeast(2, 3))
        {
            // In 2.3+, use Underanalyzer's built-in global function finder
            try
            {
                data.GlobalFunctions = new GlobalFunctions(GetGlobalScriptCodeEntries(data));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                // If an error occurs, just default to empty
                data.GlobalFunctions = new GlobalFunctions();
            }
        }
        else
        {
            // Prior to 2.3, just start empty
            data.GlobalFunctions = new GlobalFunctions();
        }

        // Prefix used for sub-functions
        const string subFunctionPrefix = "gml_Script_";

        // Add all functions that aren't sub-functions, if they aren't already there
        foreach (UndertaleFunction func in data.Functions)
        {
            if (func?.Name?.Content is string name && !name.StartsWith(subFunctionPrefix, StringComparison.Ordinal) &&
                !data.GlobalFunctions.FunctionNameExists(name))
            {
                data.GlobalFunctions.DefineFunction(name, func);
            }
        }

        // Add scripts to global functions lookup, if they aren't already there
        foreach (UndertaleScript script in data.Scripts)
        {
            if (script?.Name?.Content is string name && !name.StartsWith(subFunctionPrefix, StringComparison.Ordinal) &&
                !data.GlobalFunctions.FunctionNameExists(name) &&
                data.Functions.ByName(name) is UndertaleFunction function)
            {
                // Regular script asset (pre and post 2.3)
                data.GlobalFunctions.DefineFunction(name, function);
            }
            else if (script?.Code is UndertaleCode { ParentEntry: null } code)
            {
                // If code name starts with "gml_Script_", and there's no parent code entry,
                // then this is probably a GML-defined extension function.
                if (code.Name?.Content is string codeName && codeName.StartsWith(subFunctionPrefix, StringComparison.Ordinal) &&
                    data.Functions.ByName(codeName) is UndertaleFunction extFunction)
                {
                    data.GlobalFunctions.DefineFunction(codeName[subFunctionPrefix.Length..], extFunction);
                }
            }
        }

        // Add remaining extension functions to global functions lookup
        if (data.Extensions is not null)
        {
            foreach (UndertaleExtension extension in data.Extensions)
            {
                if (extension is null)
                {
                    continue;
                }
                foreach (UndertaleExtensionFile extensionFile in extension.Files)
                {
                    foreach (UndertaleExtensionFunction extensionFunc in extensionFile.Functions)
                    {
                        string functionName = extensionFunc.Name.Content;
                        if (data.Functions.ByName(functionName) is UndertaleFunction foundFunction)
                        {
                            data.GlobalFunctions.DefineFunction(functionName, foundFunction);
                        }
                    }
                }
            }
        }
    }

    // Enumerates over all global script code entries
    private static IEnumerable<IGMCode> GetGlobalScriptCodeEntries(UndertaleData data)
    {
        foreach (UndertaleGlobalInit script in data.GlobalInitScripts)
        {
            yield return script.Code;
        }
    }

    /// <summary>
    /// Prepares a global decompile context for compilation.
    /// </summary>
    /// <param name="forceReloadAssets">Whether asset lookups should be forced to be re-created.</param>
    internal void PrepareForCompilation(bool forceReloadAssets = true)
    {
        // Initialize builtin list if not already done (and create it for the game data if not already created)
        Builtins ??= (Data.BuiltinList ??= new BuiltinList(Data));

        // Initialize code builder if not already done
        CodeBuilder = new CodeBuilder(this);

        // Reload asset lists if necessary
        if (forceReloadAssets || _assetIdLookup is null || _scriptIdLookup is null)
        {
            CreateAssetLookups();
        }
    }

    private void CreateAssetLookups()
    {
        // Clear out lookups if they already exist
        _assetIdLookup?.Clear();
        _scriptIdLookup?.Clear();

        // Calculate a guess at the maximum required capacity (may overshoot with unused asset removal)
        int scriptCountGuess = Data.Scripts?.Count ?? 0;
        int assetCountGuess = 0;
        assetCountGuess += Data.GameObjects?.Count ?? 0;
        assetCountGuess += Data.Sprites?.Count ?? 0;
        assetCountGuess += Data.Sounds?.Count ?? 0;
        assetCountGuess += Data.Backgrounds?.Count ?? 0;
        assetCountGuess += Data.Paths?.Count ?? 0;
        assetCountGuess += Data.Fonts?.Count ?? 0;
        assetCountGuess += Data.Timelines?.Count ?? 0;
        assetCountGuess += Data.Shaders?.Count ?? 0;
        assetCountGuess += Data.Rooms?.Count ?? 0;
        assetCountGuess += Data.AudioGroups?.Count ?? 0;
        assetCountGuess += Data.AnimationCurves?.Count ?? 0;
        assetCountGuess += Data.Sequences?.Count ?? 0;
        assetCountGuess += Data.ParticleSystems?.Count ?? 0;

        // Create lookups if they don't already exist; otherwise, ensure the guessed capacity
        if (_assetIdLookup is null || _scriptIdLookup is null)
        {
            _assetIdLookup = new(assetCountGuess);
            _scriptIdLookup = new(scriptCountGuess);
        }
        else
        {
            _assetIdLookup.EnsureCapacity(assetCountGuess);
            _scriptIdLookup.EnsureCapacity(scriptCountGuess);
        }

        // Add all assets
        AddAssetsFromList(_assetIdLookup, Data.GameObjects, RefType.Object);
        AddAssetsFromList(_assetIdLookup, Data.Sprites, RefType.Sprite);
        AddAssetsFromList(_assetIdLookup, Data.Sounds, RefType.Sound);
        AddAssetsFromList(_assetIdLookup, Data.Backgrounds, RefType.Background);
        AddAssetsFromList(_assetIdLookup, Data.Paths, RefType.Path);
        AddAssetsFromList(_assetIdLookup, Data.Fonts, RefType.Font);
        AddAssetsFromList(_assetIdLookup, Data.Timelines, RefType.Timeline);
        AddAssetsFromList(_assetIdLookup, Data.Shaders, RefType.Shader);
        AddAssetsFromList(_assetIdLookup, Data.Rooms, RefType.Room);
        AddAssetsFromList(_assetIdLookup, Data.AudioGroups, RefType.Sound);
        AddAssetsFromList(_assetIdLookup, Data.AnimationCurves, RefType.AnimCurve);
        AddAssetsFromList(_assetIdLookup, Data.Sequences, RefType.Sequence);
        AddAssetsFromList(_assetIdLookup, Data.ParticleSystems, RefType.ParticleSystem);

        // Add all scripts
        AddAssetsFromList(_scriptIdLookup, Data.Scripts, RefType.Script);
    }

    /// <summary>
    /// Adds all assets from the supplied asset list to the destination lookup dictionary, encoding the supplied reference type if required.
    /// </summary>
    private void AddAssetsFromList<T>(Dictionary<string, int> destination, IList<T> list, RefType type) where T : UndertaleNamedResource
    {
        // If list doesn't exist, exit early (nothing to do)
        if (list is null)
        {
            return;
        }

        if (UsingAssetReferences)
        {
            // Asset reference types being used, so encode them
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i]?.Name?.Content is string name)
                {
                    destination[name] = (i & 0xffffff) | ((ConvertFromRefType(Data, type) & 0x7f) << 24);
                }
            }
        }
        else
        {
            // Regular IDs being used
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i]?.Name?.Content is string name)
                {
                    destination[name] = i;
                }
            }
        }
    }

    // Implementation of Underanalyzer methods

    /// <inheritdoc/>
    public string GetAssetName(AssetType assetType, int assetIndex)
    {
        if (assetIndex < 0)
        {
            return null;
        }

        switch (assetType)
        {
            case AssetType.Object:
                if (assetIndex >= (Data.GameObjects?.Count ?? 0))
                {
                    return null;
                }
                return Data.GameObjects[assetIndex]?.Name?.Content;
            case AssetType.Sprite:
                if (assetIndex >= (Data.Sprites?.Count ?? 0))
                {
                    return null;
                }
                return Data.Sprites[assetIndex]?.Name?.Content;
            case AssetType.Sound:
                if (assetIndex >= (Data.Sounds?.Count ?? 0))
                {
                    return null;
                }
                return Data.Sounds[assetIndex]?.Name?.Content;
            case AssetType.Room:
                if (assetIndex >= (Data.Rooms?.Count ?? 0))
                {
                    return null;
                }
                return Data.Rooms[assetIndex]?.Name?.Content;
            case AssetType.Background:
                if (assetIndex >= (Data.Backgrounds?.Count ?? 0))
                {
                    return null;
                }
                return Data.Backgrounds[assetIndex]?.Name?.Content;
            case AssetType.Path:
                if (assetIndex >= (Data.Paths?.Count ?? 0))
                {
                    return null;
                }
                return Data.Paths[assetIndex]?.Name?.Content;
            case AssetType.Script:
                if (assetIndex >= (Data.Scripts?.Count ?? 0))
                {
                    return null;
                }
                return Data.Scripts[assetIndex]?.Name?.Content;
            case AssetType.Font:
                if (assetIndex >= (Data.Fonts?.Count ?? 0))
                {
                    return null;
                }
                return Data.Fonts[assetIndex]?.Name?.Content;
            case AssetType.Timeline:
                if (assetIndex >= (Data.Timelines?.Count ?? 0))
                {
                    return null;
                }
                return Data.Timelines[assetIndex]?.Name?.Content;
            case AssetType.Shader:
                if (assetIndex >= (Data.Shaders?.Count ?? 0))
                {
                    return null;
                }
                return Data.Shaders[assetIndex]?.Name?.Content;
            case AssetType.Sequence:
                if (assetIndex >= (Data.Sequences?.Count ?? 0))
                {
                    return null;
                }
                return Data.Sequences[assetIndex]?.Name?.Content;
            case AssetType.AnimCurve:
                if (assetIndex >= (Data.AnimationCurves?.Count ?? 0))
                {
                    return null;
                }
                return Data.AnimationCurves[assetIndex]?.Name?.Content;
            case AssetType.ParticleSystem:
                if (assetIndex >= (Data.ParticleSystems?.Count ?? 0))
                {
                    return null;
                }
                return Data.ParticleSystems[assetIndex]?.Name?.Content;
            case AssetType.RoomInstance:
                if (assetIndex < 100000)
                {
                    return null;
                }
                return $"{_instanceIdPrefix}{assetIndex}";
        }

        return null;
    }

    /// <inheritdoc/>
    public bool GetAssetId(string assetName, out int assetId)
    {
        // If lookup isn't generated, can't retrieve any asset IDs
        if (_assetIdLookup is null)
        {
            assetId = 0;
            return false;
        }

        // Perform lookup
        return _assetIdLookup.TryGetValue(assetName, out assetId);
    }

    /// <inheritdoc/>
    public bool GetRoomInstanceId(string roomInstanceName, out int assetId)
    {
        // Check for prefix, and parse ID if there
        ReadOnlySpan<char> prefix = _instanceIdPrefix;
        ReadOnlySpan<char> name = roomInstanceName;
        if (name.StartsWith(prefix, StringComparison.Ordinal) &&
            int.TryParse(name[prefix.Length..], out int instanceId) &&
            instanceId >= 100000)
        {
            // Room instance ID found!
            assetId = instanceId;
            if (UsingRoomInstanceReferences)
            {
                // Additionally encode room instance asset type
                assetId |= ((ConvertFromRefType(Data, RefType.RoomInstance) & 0x7f) << 24);
            }
            return true;
        }

        // Prefix or instance ID were not found
        assetId = 0;
        return false;
    }

    /// <inheritdoc/>
    public bool GetScriptId(string scriptName, out int assetId)
    {
        // If lookup isn't generated, can't retrieve any script IDs
        if (_scriptIdLookup is null)
        {
            assetId = 0;
            return false;
        }

        // Perform lookup
        return _scriptIdLookup.TryGetValue(scriptName, out assetId);
    }

    /// <inheritdoc/>
    public bool GetScriptIdByFunctionName(string functionName, out int assetId)
    {
        // If lookup isn't generated, can't retrieve any script IDs
        if (_scriptIdLookup is null)
        {
            assetId = 0;
            return false;
        }

        // Perform lookup, adding "gml_Script_" prefix that global function scripts have
        return _scriptIdLookup.TryGetValue($"gml_Script_{functionName}", out assetId);
    }
}
