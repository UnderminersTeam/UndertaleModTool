using System.Collections.Generic;
using System.Diagnostics;
using System;
using Underanalyzer;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.GameSpecific;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

/// <summary>
/// The DecompileContext is global for the entire decompilation run, or possibly multiple runs. It caches the decompilation results which don't change often
/// to speedup decompilation.
/// </summary>
public class GlobalDecompileContext : IGameContext
{
    public UndertaleData Data;

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

    public GlobalDecompileContext(UndertaleData data)
    {
        Data = data;
        BuildGlobalFunctionCache(data);
    }

    /// <summary>
    /// Builds the GlobalFunctions cache, required for Underanalyzer's decompiler to recognize function names in global scope.
    /// </summary>
    public static void BuildGlobalFunctionCache(UndertaleData data)
    {
        if (data == null || data.GlobalFunctions != null)
        {
            // Nothing to calculate
            return;
        }

        if (!data.IsVersionAtLeast(2, 3))
        {
            // Make an empty instance
            data.GlobalFunctions = new GlobalFunctions();
            return;
        }

        // Use Underanalyzer's built-in global function finder
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

        // TODO: add extension functions to global functions lookup
    }

    // Enumerates over all global script code entries
    private static IEnumerable<IGMCode> GetGlobalScriptCodeEntries(UndertaleData data)
    {
        foreach (UndertaleGlobalInit script in data.GlobalInitScripts)
        {
            // TODO: is this correct?
            if (script is null || script.Code is null)
                continue;
            yield return script.Code;
        }
    }

    // Implementation of Underanalyzer methods
    public string GetAssetName(AssetType assetType, int assetIndex)
    {
        if (assetIndex < 0)
        {
            return null;
        }

        UndertaleNamedResource asset = null;
        switch (assetType)
        {
            case AssetType.Object:
                if (assetIndex >= Data.GameObjects.Count)
                {
                    return null;
                }
                asset = Data.GameObjects[assetIndex];
                break;
            case AssetType.Sprite:
                if (assetIndex >= Data.Sprites.Count)
                {
                    return null;
                }
                asset = Data.Sprites[assetIndex];
                break;
            case AssetType.Sound:
                if (assetIndex >= Data.Sounds.Count)
                {
                    return null;
                }
                asset = Data.Sounds[assetIndex];
                break;
            case AssetType.Room:
                if (assetIndex >= Data.Rooms.Count)
                {
                    return null;
                }
                asset = Data.Rooms[assetIndex];
                break;
            case AssetType.Background:
                if (assetIndex >= Data.Backgrounds.Count)
                {
                    return null;
                }
                asset = Data.Backgrounds[assetIndex];
                break;
            case AssetType.Path:
                if (assetIndex >= Data.Paths.Count)
                {
                    return null;
                }
                asset = Data.Paths[assetIndex];
                break;
            case AssetType.Script:
                if (assetIndex >= Data.Scripts.Count)
                {
                    return null;
                }
                asset = Data.Scripts[assetIndex];
                break;
            case AssetType.Font:
                if (assetIndex >= Data.Fonts.Count)
                {
                    return null;
                }
                asset = Data.Fonts[assetIndex];
                break;
            case AssetType.Timeline:
                if (assetIndex >= Data.Timelines.Count)
                {
                    return null;
                }
                asset = Data.Timelines[assetIndex];
                break;
            case AssetType.Shader:
                if (assetIndex >= Data.Shaders.Count)
                {
                    return null;
                }
                asset = Data.Shaders[assetIndex];
                break;
            case AssetType.Sequence:
                if (assetIndex >= Data.Sequences.Count)
                {
                    return null;
                }
                asset = Data.Sequences[assetIndex];
                break;
            case AssetType.AnimCurve:
                if (assetIndex >= Data.AnimationCurves.Count)
                {
                    return null;
                }
                asset = Data.AnimationCurves[assetIndex];
                break;
            case AssetType.ParticleSystem:
                if (assetIndex >= Data.ParticleSystems.Count)
                {
                    return null;
                }
                asset = Data.ParticleSystems[assetIndex];
                break;
            case AssetType.RoomInstance:
                if (assetIndex < 100000)
                {
                    return null;
                }
                return $"{Data.ToolInfo.InstanceIdPrefix()}{assetIndex}";
        }

        if (asset is null)
        {
            return null;
        }
        return asset.Name?.Content;
    }
}