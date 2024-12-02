using System.IO;

namespace UndertaleModCli;

/// <summary>
/// Cli options for the Info command
/// </summary>
public class InfoOptions
{
	/// <summary>
	/// File path to the data file
	/// </summary>
	public FileInfo Datafile { get; set; }

	/// <summary>
	/// Determines if Cli should print out verbose logs
	/// </summary>
	public bool Verbose { get; set; } = false;
}
