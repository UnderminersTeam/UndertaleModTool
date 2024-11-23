using System.IO;

namespace UndertaleModCli;

#nullable enable
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor - Properties are applied via reflection.

// ReSharper disable NotNullMemberIsNotInitialized - Properties are applied via reflection.
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

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor - Properties are applied via reflection.
#nullable restore
