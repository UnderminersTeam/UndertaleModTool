namespace UndertaleModLib.Project;

/// <summary>
/// Interface for extending an asset to be usable in a serializable project file format. 
/// </summary>
public interface IProjectAsset
{
    /// <summary>
    /// User-friendly name to be used in project-related interfaces.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Serializble asset type to be used in project-related interfaces.
    /// </summary>
    public SerializableAssetType ProjectAssetType { get; }

    /// <summary>
    /// Whether this asset is eligible for being exported to a project.
    /// </summary>
    public bool ProjectExportable { get; }

    /// <summary>
    /// Generates a serializable project version of this asset, within the given project context.
    /// </summary>
    /// <param name="projectContext">Context of the project.</param>
    /// <returns>A serializable version of this asset.</returns>
    ISerializableProjectAsset GenerateSerializableProjectAsset(ProjectContext projectContext);
}
