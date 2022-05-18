using System.IO;

namespace UndertaleModCli;

// ReSharper disable NotNullMemberIsNotInitialized - Properties are applied via reflection.
/// <summary>
/// Cli options for the Load command
/// </summary>
public class LoadOptions
{
	/// <summary>
	/// File path to the data file
	/// </summary>
	public FileInfo Datafile { get; set; }

	/// <summary>
	/// File paths to the scripts that shall be run
	/// </summary>
	public FileInfo[] Scripts { get; set; }

	/// <summary>
	/// C# string that shall be executed
	/// </summary>
	public string? Line { get; set; }

	/// <summary>
	/// File path to where to save the modified data file
	/// </summary>
	public FileInfo? Output { get; set; }

	/// <summary>
	/// Determines if Cli should be run in interactive mode
	/// </summary>
	public bool Interactive { get; set; } = false;

	/// <summary>
	/// Determines if Cli should print out verbose logs
	/// </summary>
	public bool Verbose { get; set; } = false;
}