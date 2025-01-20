using System.IO;

namespace UndertaleModCli;

/// <summary>
/// Cli options for the Dump command
/// </summary>
public class DumpOptions
{
	/// <summary>
	/// File path to the data file
	/// </summary>
	public FileInfo Datafile { get; set; }

	/// <summary>
	/// Directory path to where to dump all contents
	/// </summary>
	public DirectoryInfo Output { get; set; }

	/// <summary>
	/// Determines if Cli should print out verbose logs
	/// </summary>
	public bool Verbose { get; set; } = false;

	/// <summary>
	/// Names of the code entries that should get dumped
	/// </summary>
	public string[] Code { get; set; }

	/// <summary>
	/// Determines if strings should get dumped.
	/// </summary>
	public bool Strings { get; set; }

	/// <summary>
	/// Determines if embedded textures should get dumped
	/// </summary>
	public bool Textures { get; set; }
}
