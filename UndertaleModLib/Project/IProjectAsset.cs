namespace UndertaleModLib.Project;

/// <summary>
/// Interface for extending an asset to be usable in a serializable project file format. 
/// </summary>
public interface IProjectAsset
{
    /// <summary>
    /// Generates a serializable project version of this asset, within the given project context.
    /// </summary>
    /// <param name="projectContext">Context of the project.</param>
    /// <returns>A serializable version of this asset.</returns>
    ISerializableProjectAsset GenerateSerializableProjectAsset(ProjectContext projectContext);
}
