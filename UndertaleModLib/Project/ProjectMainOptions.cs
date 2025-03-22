using System.Collections.Generic;

namespace UndertaleModLib.Project;

/// <summary>
/// Represents the main options file for a project.
/// </summary>
internal sealed class ProjectMainOptions
{
    /// <summary>
    /// Name for the project.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// List of root-level directory names to exclude from the project.
    /// </summary>
    public List<string> ExcludeDirectories { get; set; } = new();
}
