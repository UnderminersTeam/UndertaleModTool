using System.IO;

namespace UndertaleModCli;

/// <summary>
/// Cli options for the New command
/// </summary>
public class NewOptions
{
	/// <summary>
	/// File path for new data file
	/// </summary>
	public FileInfo Output { get; set; } = new FileInfo("data.win");

	/// <summary>
	/// If the existing file path at <see cref="Output"/> should be overwritten
	/// </summary>
	public bool Overwrite { get; set; } = false;

	/// <summary>
	/// Whether to write the new data to Stdout
	/// </summary>
	public bool Stdout { get; set; }

	/// <summary>
	/// Determines if Cli should print out verbose logs
	/// </summary>
	public bool Verbose { get; set; } = false;
}