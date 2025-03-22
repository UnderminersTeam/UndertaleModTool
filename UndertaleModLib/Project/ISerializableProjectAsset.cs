using System;
using System.Text.Json.Serialization;
using UndertaleModLib.Project.SerializableAssets;
using static UndertaleModLib.Project.SerializableAssetType;

namespace UndertaleModLib.Project;

/// <summary>
/// A project asset that has been converted into a serializable form, to be put on disk.
/// </summary>
[JsonDerivedType(typeof(SerializableGameObject), nameof(GameObject))]
public interface ISerializableProjectAsset
{
    /// <summary>
    /// Name to be used for this asset directly in game data.
    /// </summary>
    public string DataName { get; }

    /// <summary>
    /// Static asset type for this asset.
    /// </summary>
    public SerializableAssetType AssetType { get; }

    /// <summary>
    /// Static boolean for whether this asset type requires an individual directory per each asset.
    /// </summary>
    public bool IndividualDirectory { get; }

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

/// <summary>
/// All supported serializable asset types.
/// </summary>
/// <remarks>
/// These names are directly used for JSON type discriminators; changing them should be avoided.
/// </remarks>
public enum SerializableAssetType
{
    GameObject
}

/// <summary>
/// Extensions for serializable asset types.
/// </summary>
internal static class SerializableAssetTypeExtensions
{
    /// <summary>
    /// Converts the serializable asset type to its filesystem/directory name representation.
    /// </summary>
    public static string ToFilesystemName(this SerializableAssetType assetType)
    {
        return assetType switch
        {
            GameObject => "object",
            _ => throw new NotImplementedException()
        };
    }
}
