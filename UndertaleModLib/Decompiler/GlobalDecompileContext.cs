using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Underanalyzer;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.GameSpecific;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModLib.Decompiler;

/// <summary>
/// The DecompileContext is global for the entire decompilation run, or possibly multiple runs. It caches the decompilation results which don't change often
/// to speedup decompilation.
/// </summary>
public class GlobalDecompileContext : Underanalyzer.IGameContext
{
    public UndertaleData Data;

    public bool EnableStringLabels;

    public List<string> DecompilerWarnings = new List<string>();

    /// <summary>
    /// A cache of resolved function argument types. This is kept here because decompiling is slow, and there is no need to do it every time
    /// unless the code has changed.
    /// </summary>
    public Dictionary<string, AssetIDType[]> ScriptArgsCache = new Dictionary<string, AssetIDType[]>();

    /// <summary>
    /// A cache of function to actual name mapping. GMS2.3+ sometimes (usually when dealing with global scripts) calls method functions
    /// using the legacy call operator, passing the anonymous function directly. This dictionary contains a map from UndertaleFunction
    /// to its actual name, obtained by decompiling the parent CodeObject and looking for the assignment to global variable with function
    /// name.
    /// </summary>
    public Dictionary<UndertaleFunction, string> AnonymousFunctionNameCache = new Dictionary<UndertaleFunction, string>();

    // Implementation of Underanalyzer properties
    public bool UsingGMLv2 => Data?.IsVersionAtLeast(2, 3) ?? false;
    public bool Bytecode14OrLower => (Data?.GeneralInfo?.BytecodeVersion ?? 15) <= 14;
    public bool UsingGMS2OrLater => Data?.IsVersionAtLeast(2) ?? false;
    public bool UsingFinallyBeforeThrow => !(Data?.IsVersionAtLeast(2024, 6) ?? false);
    public IGlobalFunctions GlobalFunctions => Data?.GlobalFunctions;
    public bool UsingTypedBooleans => Data?.IsVersionAtLeast(2, 3, 7) ?? false;
    public bool UsingAssetReferences => Data?.IsVersionAtLeast(2023, 8) ?? false;
    public bool UsingRoomInstanceReferences => Data?.IsVersionAtLeast(2024, 2) ?? false;
    public GameSpecificRegistry GameSpecificRegistry => Data?.GameSpecificRegistry;

    public GlobalDecompileContext(UndertaleData data, bool enableStringLabels)
    {
        this.Data = data;
        this.EnableStringLabels = enableStringLabels;
        Decompiler.BuildSubFunctionCache(data);
    }

    public void ClearDecompilationCache()
    {
        // This will not be done automatically, because it would cause significant slowdown having to recalculate this each time, and there's no reason to reset it if it's decompiling a bunch at once.
        // But, since it is possible to invalidate this data, we add this here so we'll be able to invalidate it if we need to.
        ScriptArgsCache.Clear();
        AnonymousFunctionNameCache.Clear();
    }

    // Implementation of Underanalyzer methods
    public string GetAssetName(AssetType assetType, int assetIndex)
    {
        if (assetIndex < 0)
        {
            return null;
        }

        switch (assetType)
        {
            case AssetType.Object:
                if (assetIndex > Data.GameObjects.Count)
                {
                    return null;
                }
                return Data.GameObjects[assetIndex].Name?.Content;
            case AssetType.Sprite:
                if (assetIndex > Data.Sprites.Count)
                {
                    return null;
                }
                return Data.Sprites[assetIndex].Name?.Content;
            case AssetType.Sound:
                if (assetIndex > Data.Sounds.Count)
                {
                    return null;
                }
                return Data.Sounds[assetIndex].Name?.Content;
            case AssetType.Room:
                if (assetIndex > Data.Rooms.Count)
                {
                    return null;
                }
                return Data.Rooms[assetIndex].Name?.Content;
            case AssetType.Background:
                if (assetIndex > Data.Backgrounds.Count)
                {
                    return null;
                }
                return Data.Backgrounds[assetIndex].Name?.Content;
            case AssetType.Path:
                if (assetIndex > Data.Paths.Count)
                {
                    return null;
                }
                return Data.Paths[assetIndex].Name?.Content;
            case AssetType.Script:
                if (assetIndex > Data.Scripts.Count)
                {
                    return null;
                }
                return Data.Scripts[assetIndex].Name?.Content;
            case AssetType.Font:
                if (assetIndex > Data.Fonts.Count)
                {
                    return null;
                }
                return Data.Fonts[assetIndex].Name?.Content;
            case AssetType.Timeline:
                if (assetIndex > Data.Timelines.Count)
                {
                    return null;
                }
                return Data.Timelines[assetIndex].Name?.Content;
            case AssetType.Shader:
                if (assetIndex > Data.Shaders.Count)
                {
                    return null;
                }
                return Data.Shaders[assetIndex].Name?.Content;
            case AssetType.Sequence:
                if (assetIndex > Data.Sequences.Count)
                {
                    return null;
                }
                return Data.Sequences[assetIndex].Name?.Content;
            case AssetType.AnimCurve:
                if (assetIndex > Data.AnimationCurves.Count)
                {
                    return null;
                }
                return Data.AnimationCurves[assetIndex].Name?.Content;
            case AssetType.ParticleSystem:
                if (assetIndex > Data.ParticleSystems.Count)
                {
                    return null;
                }
                return Data.ParticleSystems[assetIndex].Name?.Content;
            case AssetType.RoomInstance:
                if (assetIndex < 100000)
                {
                    return null;
                }
                return $"inst_id_{assetIndex}";
        }

        return null;
    }
}