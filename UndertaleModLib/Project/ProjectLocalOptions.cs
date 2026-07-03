using System;
using System.Collections.Generic;
using System.Text;

namespace UndertaleModLib.Project;

/// <summary>
/// Represents the local options file for a project.
/// </summary>
internal sealed class ProjectLocalOptions
{
    /// <summary>
    /// Path to the load and save data file paths for this project.
    /// </summary>
    public ProjectMainOptions.PathList.PathPair DataFilePath { get; set; }
}
