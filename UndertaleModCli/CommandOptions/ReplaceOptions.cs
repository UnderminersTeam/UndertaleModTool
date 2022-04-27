using System.IO;

namespace UndertaleModCli;

/// <summary>
/// Cli options for the Replace command
/// </summary>
public class ReplaceOptions
{
    /// <summary>
    /// File path to the data file
    /// </summary>
    public FileInfo Datafile { get; set; }

    /// <summary>
    /// File path to where to save the modified data file
    /// </summary>
    public FileInfo? Output { get; set; }

    /// <summary>
    /// Determines if Cli should print out verbose logs
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Equal separated values of code entry and the file to replace the code entry with.
    /// </summary>
    public string[] Code { get; set; }

    /// <summary>
    /// Equal separated values of embedded texture and the file to replace the embedded texture with.
    /// </summary>
    public string[] Textures { get; set; }
}