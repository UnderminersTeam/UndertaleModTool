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

	/// <summary>
	/// Names of the sprites to dump. Specify 'all' to dump all sprites.
	/// </summary>
	public string[] Sprites { get; set; }

	/// <summary>
	/// Whether to emit a .t3s manifest alongside exported sprite PNGs (for tex3ds / devkitPro).
	/// </summary>
	public bool T3s { get; set; }

	/// <summary>
	/// Names of the sounds to dump. Specify 'UMT_DUMP_ALL' to dump all sounds.
	/// </summary>
	public string[] Sounds { get; set; }

	/// <summary>
	/// Names of the game objects to dump metadata for. Specify 'UMT_DUMP_ALL' to dump all objects.
	/// </summary>
	public string[] Objects { get; set; }
}
