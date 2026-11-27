using System.IO;

namespace UndertaleModCli;

/// <summary>
/// Cli options for the Project Export command
/// </summary>
public class ProjectExportOptions
{
    /// <summary>
	/// File path to the project file
	/// </summary>
    public FileInfo ProjectFile { get; set; }

    /// <summary>
	/// File path to source data file
	/// </summary>
    public FileInfo Source { get; set; }

    /// <summary>
	/// File path to destination data file
	/// </summary>
    public FileInfo Destination { get; set; }

    /// <summary>
    /// Determines if Cli should print out verbose logs
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Names of assets to export
    /// </summary>
    public string[] Assets { get; set; }
}
