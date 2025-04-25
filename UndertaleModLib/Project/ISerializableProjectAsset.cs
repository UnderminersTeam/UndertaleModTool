using System;
using System.Text.Json.Serialization;
using UndertaleModLib.Project.SerializableAssets;
using UndertaleModLib.Util;
using static UndertaleModLib.Project.SerializableAssetType;

namespace UndertaleModLib.Project;

/// <summary>
/// A project asset that has been converted into a serializable form, to be put on disk.
/// </summary>
[JsonDerivedType(typeof(SerializableGameObject), nameof(GameObject))]
[JsonDerivedType(typeof(SerializablePath), nameof(Path))]
[JsonDerivedType(typeof(SerializableCode), nameof(Code))]
[JsonDerivedType(typeof(SerializableScript), nameof(Script))]
[JsonDerivedType(typeof(SerializableSound), nameof(Sound))]
[JsonDerivedType(typeof(SerializableRoom), nameof(Room))]
[JsonDerivedType(typeof(SerializableBackground), nameof(Background))]
[JsonDerivedType(typeof(SerializableSprite), nameof(Sprite))]
public interface ISerializableProjectAsset
{
    /// <summary>
    /// Name to be used for this asset directly in game data.
    /// </summary>
    public string DataName { get; internal set; }

    /// <summary>
    /// Static asset type for this asset.
    /// </summary>
    [JsonIgnore]
    public SerializableAssetType AssetType { get; }

    /// <summary>
    /// Static boolean for whether this asset type requires an individual directory per each asset.
    /// </summary>
    [JsonIgnore]
    public bool IndividualDirectory { get; }

    /// <summary>
    /// Optional integer override for asset importing order. 
    /// This has higher precedence over all alphabetical <see cref="DataName"/> sorting, and is ordered ascending.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    /// <summary>
    /// Serializes this project asset to a destination (main) filename, along with any other files that need to be saved.
    /// </summary>
    public void Serialize(ProjectContext projectContext, string destinationFile);

    /// <summary>
    /// Performs any pre-import operations for importing this asset into game data, such as finding/creating a blank asset.
    /// </summary>
    /// <param name="projectContext">Context of the project.</param>
    public void PreImport(ProjectContext projectContext);

    /// <summary>
    /// Imports this serializable asset into game data, within the given project context.
    /// </summary>
    /// <param name="projectContext">Context of the project.</param>
    /// <returns>A raw/regular version of this serializable asset, as contained (or added) to game data.</returns>
    public IProjectAsset Import(ProjectContext projectContext);
}

public interface ISerializableTextureProjectAsset : ISerializableProjectAsset
{
    /// <summary>
    /// Imports any textures for the asset.
    /// </summary>
    /// <param name="projectContext">Context of the project.</param>
    /// <param name="texturePacker">Texture packer currently being used for packing.</param>
    public void ImportTextures(ProjectContext projectContext, TextureGroupPacker texturePacker);
}

/// <summary>
/// All supported serializable asset types.
/// </summary>
/// <remarks>
/// These names are directly used for JSON type discriminators; changing them should be avoided.
/// </remarks>
public enum SerializableAssetType
{
    GameObject,
    Path,
    Code,
    Script,
    Sound,
    Room,
    Background,
    Sprite,
}

/// <summary>
/// Extensions for serializable asset types.
/// </summary>
public static class SerializableAssetTypeExtensions
{
    /// <summary>
    /// Converts the serializable asset type to its friendly interface name representation.
    /// </summary>
    public static string ToInterfaceName(this SerializableAssetType assetType)
    {
        return assetType switch
        {
            GameObject => "Game Object",
            Path => "Path",
            Code => "Code",
            Script => "Script",
            Sound => "Sound",
            Room => "Room",
            Background => "Background",
            Sprite => "Sprite",
            _ => throw new NotImplementedException()
        };
    }
    /// <summary>
    /// Converts the serializable asset type to its filesystem/directory name representation.
    /// </summary>
    internal static string ToFilesystemNameSingular(this SerializableAssetType assetType)
    {
        return assetType switch
        {
            GameObject => "object",
            Path => "path",
            Code => "code",
            Script => "script",
            Sound => "sound",
            Room => "room",
            Background => "background",
            Sprite => "sprite",
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Converts the serializable asset type to its filesystem/directory name representation.
    /// </summary>
    internal static string ToFilesystemNamePlural(this SerializableAssetType assetType)
    {
        return assetType switch
        {
            GameObject => "objects",
            Path => "paths",
            Code => "code",
            Script => "scripts",
            Sound => "sounds",
            Room => "rooms",
            Background => "backgrounds",
            Sprite => "sprites",
            _ => throw new NotImplementedException()
        };
    }
}
