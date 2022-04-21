using System.IO;

namespace UndertaleModCli
{
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

    /// <summary>
    /// Cli options for the Info command
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
        public DirectoryInfo? Output { get; set; }

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
}