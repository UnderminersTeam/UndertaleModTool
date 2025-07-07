/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text.Json;
using Underanalyzer.Decompiler.GameSpecific.Json;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// A registry for macro types, macro type resolvers, and other game-specific data.
/// Nothing within this should be modified while decompilation is in progress that uses this object.
/// </summary>
public class GameSpecificRegistry
{
    private Dictionary<string, IMacroType> MacroTypes { get; }
    public GlobalMacroTypeResolver MacroResolver { get; }
    public NamedArgumentResolver NamedArgumentResolver { get; }

    /// <summary>
    /// Initializes an empty macro type registry, with an empty global type resolver.
    /// </summary>
    public GameSpecificRegistry()
    {
        MacroTypes = [];
        MacroResolver = new();
        NamedArgumentResolver = new();
    }

    /// <summary>
    /// Deserializes a macro type registry from the given JSON, merging/appending it with the existing registry.
    /// </summary>
    public void DeserializeFromJson(ReadOnlySpan<char> json)
    {
        JsonSerializerOptions options = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        options.Converters.Add(new EnumMacroTypeConverter());
        options.Converters.Add(new ConstantsMacroTypeConverter());
        options.Converters.Add(new IMacroTypeConverter(this));
        options.Converters.Add(new GameSpecificRegistryConverter(this));
        JsonSerializer.Deserialize<GameSpecificRegistry>(json, options);
    }

    /// <summary>
    /// Registers a macro type with the given name.
    /// If one with the name already exists, it will be overridden.
    /// </summary>
    public void RegisterType(string name, IMacroType type)
    {
        MacroTypes[name] = type;
    }

    /// <summary>
    /// Registers basic asset, constant, and object instance macro types.
    /// </summary>
    public void RegisterBasic()
    {
        RegisterType("Bool", new BooleanMacroType());
        RegisterType("Id.Instance", new InstanceMacroType());
        RegisterType("Asset.Object", new AssetMacroType(AssetType.Object));
        RegisterType("Asset.Sprite", new AssetMacroType(AssetType.Sprite));
        RegisterType("Asset.Sound", new AssetMacroType(AssetType.Sound));
        RegisterType("Asset.Room", new AssetMacroType(AssetType.Room));
        RegisterType("Asset.Background", new AssetMacroType(AssetType.Background));
        RegisterType("Asset.Tileset", new AssetMacroType(AssetType.Background)); // same type as previous line - convenience
        RegisterType("Asset.Path", new AssetMacroType(AssetType.Path));
        RegisterType("Asset.Script", new AssetMacroType(AssetType.Script));
        RegisterType("Asset.Font", new AssetMacroType(AssetType.Font));
        RegisterType("Asset.Timeline", new AssetMacroType(AssetType.Timeline));
        RegisterType("Asset.Shader", new AssetMacroType(AssetType.Shader));
        RegisterType("Asset.Sequence", new AssetMacroType(AssetType.Sequence));
        RegisterType("Asset.AnimationCurve", new AssetMacroType(AssetType.AnimCurve));
        RegisterType("Asset.ParticleSystem", new AssetMacroType(AssetType.ParticleSystem));
        RegisterType("Asset.RoomInstance", new AssetMacroType(AssetType.RoomInstance));
        RegisterType("Constant.Color", new ColorMacroType());
        RegisterType("Constant.VirtualKey", new VirtualKeyMacroType());
    }

    public bool TypeExists(string name)
    {
        return MacroTypes.ContainsKey(name);
    }

    public IMacroType FindType(string name)
    {
        if (MacroTypes.TryGetValue(name, out IMacroType? type))
        {
            return type;
        }
        throw new InvalidOperationException($"Macro type \"{name}\" missing");
    }

    public UnionMacroType FindTypeUnion(IEnumerable<string> names)
    {
        return new UnionMacroType(EnumerateFindTypes(names));
    }

    private IEnumerable<IMacroType> EnumerateFindTypes(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            yield return FindType(name);
        }
    }
}
