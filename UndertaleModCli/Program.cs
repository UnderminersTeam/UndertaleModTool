using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;
using static UndertaleModLib.UndertaleReader;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using Newtonsoft.Json;

namespace UndertaleModCli;

/// <summary>
/// Main CLI Program
/// </summary>
public partial class Program : IScriptInterface
{
    #region Properties

    // taken from the Linux programmer manual:
    /// <summary>
    /// Value that should be returned on a successful operation.
    /// </summary>
    private const int EXIT_SUCCESS = 0;

    /// <summary>
    /// Value that should be returned on a failed operation.
    /// </summary>
    private const int EXIT_FAILURE = 1;

    /// <summary>
    /// Value that determines if the current Program is running in interactive mode.
    /// </summary>
    private bool IsInteractive { get; }

    /// <summary>
    /// Value that determines if the current Program is running in verbose mode.
    /// </summary>
    private bool Verbose { get; }

    /// <summary>
    /// File path or directory path that determines an output for the current Program.
    /// </summary>
    private FileSystemInfo Output { get; }

    /// <summary>
    /// Constant, used to indicate that the user wants to replace everything in a replace command.
    /// </summary>
    private const string UMT_REPLACE_ALL = "UMT_REPLACE_ALL";

    /// <summary>
    /// Constant, used to indicate that the user wants to dump everything in a dump command
    /// </summary>
    private const string UMT_DUMP_ALL = "UMT_DUMP_ALL";

    //TODO: document these, these are intertwined with inherited updating methods
    private int progressValue;
    private Task updater;
    private CancellationTokenSource cTokenSource;
    private CancellationToken cToken;

    private string savedMsg, savedStatus;
    private double savedValue, savedValueMax;

    /// <summary>
    /// The ScriptOptions, only used for <see cref="CSharpScript"/>, aka running C# code.
    /// </summary>
    private ScriptOptions CliScriptOptions { get; }

    /// <summary>
    /// Determines if actions should show a "this is finished" text. Gets set by <see cref="SetFinishedMessage"/>.
    /// </summary>
    private bool FinishedMessageEnabled { get; set; }

    #endregion

    /// <summary>
    /// Main entrypoint for Cli
    /// </summary>
    /// <param name="args">Arguments passed on to program.</param>
    /// <returns>Result code of the program.</returns>
    public static int Main(string[] args)
    {
        var verboseOption = new Option<bool>(new[] { "-v", "--verbose" }, "Detailed logs");

        var dataFileArgument = new Argument<FileInfo>("datafile", "Path to the data.win/.ios/.droid/.unx file");

        // Setup new command
        Command newCommand = new Command("new", "Generates a blank data file")
        {
            new Option<FileInfo>(new[] { "-o", "--output" }, () => new NewOptions().Output),
            new Option<bool>(new[] { "-f", "--overwrite" }, "Overwrite destination file if it already exists"),
            new Option<bool>(new[] { "-", "--stdout" }, "Write new data content to stdout"), // "-" is often used in *nix land as a replacement for stdout
            verboseOption
        };
        newCommand.Handler = CommandHandler.Create<NewOptions>(Program.New);

        // Setup load command
        var scriptRunnerOption = new Option<FileInfo[]>(new[] { "-s", "--scripts" }, "Scripts to apply to the <datafile>. Ex. a.csx b.csx");
        Command loadCommand = new Command("load", "Load a data file and perform actions on it")
        {
            dataFileArgument,
            scriptRunnerOption,
            verboseOption,
            //TODO: why no force overwrite here, but needed for new?
            new Option<FileInfo>(new[] { "-o", "--output" }, "Where to save the modified data file"),
            new Option<string>(new[] { "-l", "--line" }, "Run C# string. Runs AFTER everything else"),
            //TODO: make interactive another Command
            new Option<bool>(new[] { "-i", "--interactive" }, "Interactive menu launch")
        };
        loadCommand.Handler = CommandHandler.Create<LoadOptions>(Program.Load);

        // Setup info command
        Command infoCommand = new Command("info", "Show basic info about the game data file") { dataFileArgument, verboseOption };
        infoCommand.Handler = CommandHandler.Create<InfoOptions>(Program.Info);

        // Setup dump command
        Command dumpCommand = new Command("dump", "Dump certain properties about the game data file")
        {
            dataFileArgument,
            verboseOption,
            new Option<DirectoryInfo>(new[] { "-o", "--output" }, "Where to dump data file properties to. Will default to path of the data file"),
            new Option<string[]>(new[] { "-c", "--code" },
                $"The code files to dump. Ex. gml_Script_init_map gml_Script_reset_map. Specify '{UMT_DUMP_ALL}' to dump all code entries"),
            new Option<bool>(new[] { "-s", "--strings" }, "Whether to dump all strings"),
            new Option<bool>(new[] { "-t", "--textures" }, "Whether to dump all embedded textures")
        };
        dumpCommand.Handler = CommandHandler.Create<DumpOptions>(Program.Dump);

        // Setup replace command
        Command replaceCommand = new Command("replace", "Replace certain properties in the game data file")
        {
            dataFileArgument,
            verboseOption,
            new Option<FileInfo>(new[] { "-o", "--output" }, "Where to save the modified data file"),
            new Option<string[]>(new[] { "-c", "--code" },
                $"Which code files to replace with which file. Ex. 'gml_Script_init_map=./newCode.gml'. It is possible to replace everything by using '{UMT_REPLACE_ALL}'"),
            new Option<string[]>(new[] { "-t", "--textures" },
                $"Which embedded texture entry to replace with which file. Ex. 'Texture 0=./newTexture.png'. It is possible to replace everything by using '{UMT_REPLACE_ALL}'")
        };
        replaceCommand.Handler = CommandHandler.Create<ReplaceOptions>(Program.Replace);

        // Merge everything together
        RootCommand rootCommand = new RootCommand
        {
            newCommand,
            loadCommand,
            infoCommand,
            dumpCommand,
            replaceCommand
        };
        rootCommand.Description = "CLI tool for modding, decompiling and unpacking Undertale (and other Game Maker: Studio games)!";
        Parser commandLine = new CommandLineBuilder(rootCommand)
            .UseDefaults() // automatically configures dotnet-suggest
            .Build();

        return commandLine.Invoke(args);
    }

    public Program(FileInfo datafile, FileInfo[] scripts, FileInfo output, bool verbose = false, bool interactive = false)
    {
        this.Verbose = verbose;
        IsInteractive = interactive;
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Console.InputEncoding;


        Console.WriteLine($"Trying to load file: '{datafile.FullName}'");

        this.FilePath = datafile.FullName;
        this.ExePath = Environment.CurrentDirectory;
        this.Output = output;

        this.Data = ReadDataFile(datafile, this.Verbose ? WarningHandler : DummyHandler, this.Verbose ? MessageHandler : DummyHandler);

        FinishedMessageEnabled = true;
        this.CliScriptOptions = ScriptOptions.Default
            .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler",
                "UndertaleModLib.Scripting", "UndertaleModLib.Compiler",
                "UndertaleModLib.Util", "System", "System.IO", "System.Collections.Generic",
                "System.Text.RegularExpressions")
            .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly,
                GetType().GetTypeInfo().Assembly,
                typeof(JsonConvert).GetTypeInfo().Assembly,
                typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly,
                typeof(TextureWorker).GetTypeInfo().Assembly,
                typeof(Underanalyzer.Decompiler.DecompileContext).Assembly)
            // "WithEmitDebugInformation(true)" not only lets us to see a script line number which threw an exception,
            // but also provides other useful debug info when we run UMT in "Debug".
            .WithEmitDebugInformation(true);
    }

    public Program(FileInfo datafile, bool verbose, DirectoryInfo output = null)
    {
        if (datafile == null) throw new ArgumentNullException(nameof(datafile));

        Console.WriteLine($"Trying to load file: '{datafile.FullName}'");
        this.Verbose = verbose;
        this.Data = ReadDataFile(datafile, verbose ? WarningHandler : null, verbose ? MessageHandler : null);
        this.Output = output ?? new DirectoryInfo(datafile.DirectoryName);

        if (this.Verbose)
            Console.WriteLine("Output directory has been set to " + this.Output.FullName);
    }

    /// <summary>
    /// Method that gets executed on the "new" command
    /// </summary>
    /// <param name="options">The arguments that have been provided with the "new" command</param>
    /// <returns><see cref="EXIT_SUCCESS"/> and <see cref="EXIT_FAILURE"/> for being successful and failing respectively</returns>
    private static int New(NewOptions options)
    {
        //TODO: this should probably create a new Program instance, with just the properties that it needs

        UndertaleData data = UndertaleData.CreateNew();

        // If stdout flag is set, write new data to stdout and quit
        if (options.Stdout)
        {
            if (options.Verbose) Console.WriteLine("Attempting to write new Data file to STDOUT...");
            using MemoryStream ms = new MemoryStream();
            UndertaleIO.Write(ms, data);
            Console.OpenStandardOutput().Write(ms.ToArray(), 0, (int)ms.Length);
            Console.Out.Flush();
            if (options.Verbose) Console.WriteLine("Successfully wrote new Data file to STDOUT.");

            return EXIT_SUCCESS;
        }

        // If not STDOUT, write to file instead. Check first if we have permission to overwrite
        if (options.Output.Exists && !options.Overwrite)
        {
            Console.Error.WriteLine($"'{options.Output}' already exists. Pass --overwrite to overwrite");
            return EXIT_FAILURE;
        }

        // We're not writing to STDOUT, and overwrite flag was given, so we write to specified file.
        if (options.Verbose) Console.WriteLine($"Attempting to write new Data file to '{options.Output}'...");
        using FileStream fs = options.Output.OpenWrite();
        UndertaleIO.Write(fs, data);
        if (options.Verbose) Console.WriteLine($"Successfully wrote new Data file to '{options.Output}'.");
        return EXIT_SUCCESS;
    }

    /// <summary>
    /// Method that gets executed on the "load" command
    /// </summary>
    /// <param name="options">The arguments that have been provided with the "load" command</param>
    /// <returns><see cref="EXIT_SUCCESS"/> and <see cref="EXIT_FAILURE"/> for being successful and failing respectively</returns>
    private static int Load(LoadOptions options)
    {
        Program program;

        // Try to load necessary values.
        // This can throw if mandatory arguments are not given, in which case we want to exit cleanly without a stacktrace.
        try
        {
            program = new Program(options.Datafile, options.Scripts, options.Output, options.Verbose, options.Interactive);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            return EXIT_FAILURE;
        }

        // if interactive is enabled, launch the menu instead
        if (options.Interactive)
        {
            program.RunInteractiveMenu();
            return EXIT_SUCCESS;
        }

        // if we have any scripts to run, run every one of them
        if (options.Scripts != null)
        {
            foreach (FileInfo script in options.Scripts)
                program.RunCSharpFile(script.FullName);
        }

        // if line to execute was given, execute it
        if (options.Line != null)
        {
            program.ScriptPath = null;
            program.RunCSharpCode(options.Line);
        }

        // if parameter to save file was given, save the data file
        if (options.Output != null)
            program.SaveDataFile(options.Output.FullName);

        return EXIT_SUCCESS;
    }

    /// <summary>
    /// Method that gets executed on the "info" command
    /// </summary>
    /// <param name="options">The arguments that have been provided with the "info" command</param>
    /// <returns><see cref="EXIT_SUCCESS"/> and <see cref="EXIT_FAILURE"/> for being successful and failing respectively</returns>
    private static int Info(InfoOptions options)
    {
        Program program;
        try
        {
            program = new Program(options.Datafile, options.Verbose);
        }
        catch (FileNotFoundException e)
        {
            Console.Error.WriteLine(e.Message);
            return EXIT_FAILURE;
        }

        program.CliQuickInfo();
        return EXIT_SUCCESS;
    }

    /// <summary>
    /// Method that gets executed on the "dump" command
    /// </summary>
    /// <param name="options">The arguments that have been provided with the "dump" command</param>
    /// <returns><see cref="EXIT_SUCCESS"/> and <see cref="EXIT_FAILURE"/> for being successful and failing respectively</returns>
    private static int Dump(DumpOptions options)
    {
        Program program;
        try
        {
            program = new Program(options.Datafile, options.Verbose, options.Output);
        }
        catch (FileNotFoundException e)
        {
            Console.Error.WriteLine(e.Message);
            return EXIT_FAILURE;
        }

        if (program.Data.IsYYC())
        {
            Console.WriteLine("The game was made with YYC (YoYo Compiler), which means that the code was compiled into the executable. " +
                              "There is thus no code to dump. Exiting.");
            return EXIT_SUCCESS;
        }

        // If user provided code to dump, dump code
        if ((options.Code?.Length > 0) && (program.Data.Code.Count > 0))
        {
            // If user wanted to dump everything, do that, otherwise only dump what user provided
            string[] codeArray;
            if (options.Code.Contains(UMT_DUMP_ALL))
                codeArray = program.Data.Code.Select(c => c.Name.Content).ToArray();
            else
                codeArray = options.Code;

            foreach (string code in codeArray)
                program.DumpCodeEntry(code);
        }

        // If user wanted to dump strings, dump all of them in a text file
        if (options.Strings)
            program.DumpAllStrings();

        // If user wanted to dump embedded textures, dump all of them
        if (options.Textures)
            program.DumpAllTextures();

        return EXIT_SUCCESS;
    }

    /// <summary>
    /// Method that gets executed on the "replace" command
    /// </summary>
    /// <param name="options">The arguments that have been provided with the "replace" command</param>
    /// <returns><see cref="EXIT_SUCCESS"/> and <see cref="EXIT_FAILURE"/> for being successful and failing respectively</returns>
    private static int Replace(ReplaceOptions options)
    {
        Program program;
        try
        {
            program = new Program(options.Datafile, null, options.Output, options.Verbose);
        }
        catch (FileNotFoundException e)
        {
            Console.Error.WriteLine(e.Message);
            return EXIT_FAILURE;
        }

        // If user provided code to replace, replace them
        if ((options.Code?.Length > 0) && (program.Data.Code.Count > 0))
        {
            // get the values and put them into a dictionary for ease of use
            Dictionary<string, FileInfo> codeDict = new Dictionary<string, FileInfo>();
            foreach (string code in options.Code)
            {
                string[] splitText = code.Split('=');

                if (splitText.Length != 2)
                {
                    Console.Error.WriteLine($"{code} is malformed! Should be of format 'name_of_code=./newCode.gml' instead!");
                    return EXIT_FAILURE;
                }

                codeDict.Add(splitText[0], new FileInfo(splitText[1]));
            }

            // If user wants to replace all, we'll be handling it differently. Replace every file from the provided directory
            if (codeDict.ContainsKey(UMT_REPLACE_ALL))
            {
                string directory = codeDict[UMT_REPLACE_ALL].FullName;
                foreach (FileInfo file in new DirectoryInfo(directory).GetFiles())
                    program.ReplaceCodeEntryWithFile(Path.GetFileNameWithoutExtension(file.Name), file);
            }
            // Otherwise, just replace every file which was given
            else
            {
                foreach (KeyValuePair<string, FileInfo> keyValue in codeDict)
                    program.ReplaceCodeEntryWithFile(keyValue.Key, keyValue.Value);
            }
        }

        // If user provided texture to replace, replace them
        if (options.Textures?.Length > 0)
        {
            // get the values and put them into a dictionary for ease of use
            Dictionary<string, FileInfo> textureDict = new Dictionary<string, FileInfo>();
            foreach (string texture in options.Textures)
            {
                string[] splitText = texture.Split('=');

                if (splitText.Length != 2)
                {
                    Console.Error.WriteLine($"{texture} is malformed! Should be of format 'Name=./new.png' instead!");
                    return EXIT_FAILURE;
                }

                textureDict.Add(splitText[0], new FileInfo(splitText[1]));
            }

            // If user wants to replace all, we'll be handling it differently. Replace every file from the provided directory
            if (textureDict.ContainsKey(UMT_REPLACE_ALL))
            {
                string directory = textureDict[UMT_REPLACE_ALL].FullName;
                foreach (FileInfo file in new DirectoryInfo(directory).GetFiles())
                    program.ReplaceTextureWithFile(Path.GetFileNameWithoutExtension(file.Name), file);
            }
            // Otherwise, just replace every file which was given
            else
            {
                foreach ((string key, FileInfo value) in textureDict)
                    program.ReplaceTextureWithFile(key, value);
            }
        }

        // if parameter to save file was given, save the data file
        if (options.Output != null)
            program.SaveDataFile(options.Output.FullName);

        return EXIT_SUCCESS;
    }

    /// <summary>
    /// Runs the interactive menu indefinitely until user quits out of it.
    /// </summary>
    private void RunInteractiveMenu()
    {
        while (true)
        {
            Console.WriteLine("Interactive Menu:");
            Console.WriteLine("1 - Run script.");
            Console.WriteLine("2 - Run C# string.");
            Console.WriteLine("3 - Save and overwrite.");
            Console.WriteLine("4 - Save to different place.");
            Console.WriteLine("5 - Display quick info.");
            //TODO: add dumping and replacing options
            Console.WriteLine("6 - Quit without saving.");

            Console.Write("Input, please: ");
            ConsoleKey k = Console.ReadKey().Key;
            Console.WriteLine();

            switch (k)
            {
                // 1 - run script
                case ConsoleKey.NumPad1:
                case ConsoleKey.D1:
                {
                    Console.Write("File path (you can drag and drop)? ");
                    string path = RemoveQuotes(Console.ReadLine());
                    Console.WriteLine("Trying to run script {0}", path);
                    try
                    {
                        RunCSharpFile(path);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    break;
                }

                // 2 - run c# string
                case ConsoleKey.NumPad2:
                case ConsoleKey.D2:
                {
                    Console.Write("C# code line? ");
                    string line = Console.ReadLine();
                    ScriptPath = null;
                    RunCSharpCode(line);
                    break;
                }

                // Save and overwrite data file
                case ConsoleKey.NumPad3:
                case ConsoleKey.D3:
                {
                    SaveDataFile(FilePath);
                    break;
                }

                // Save data file to different path
                case ConsoleKey.NumPad4:
                case ConsoleKey.D4:
                {
                    Console.Write("Where to save? ");
                    string path = RemoveQuotes(Console.ReadLine());
                    SaveDataFile(path);
                    break;
                }

                // Print out Quick Info
                case ConsoleKey.NumPad5:
                case ConsoleKey.D5:
                {
                    CliQuickInfo();
                    break;
                }

                // Quit
                case ConsoleKey.NumPad6:
                case ConsoleKey.D6:
                {
                    Console.WriteLine("Are you SURE? You can press 'n' and save before the changes are gone forever!!!");
                    Console.WriteLine("(Y/N)? ");
                    bool isInputYes = Console.ReadKey(false).Key == ConsoleKey.Y;
                    Console.WriteLine();
                    if (isInputYes) return;

                    break;
                }

                default:
                {
                    Console.WriteLine("Unknown input. Try using the upper line of digits on your keyboard.");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Prints some basic info about the loaded data file.
    /// </summary>
    private void CliQuickInfo()
    {
        Console.WriteLine("Quick Information:");
        Console.WriteLine("Project Name - {0}", Data.GeneralInfo.Name);
        Console.WriteLine("Is GMS2 - {0}", Data.IsGameMaker2());
        Console.WriteLine("Is YYC - {0}", Data.IsYYC());
        Console.WriteLine("Bytecode version - {0}", Data.GeneralInfo.BytecodeVersion);
        Console.WriteLine("Configuration name - {0}", Data.GeneralInfo.Config);

        Console.WriteLine($"{Data.Sounds.Count} Sounds, {Data.Sprites.Count} Sprites, {Data.Backgrounds.Count} Backgrounds");
        Console.WriteLine($"{Data.Paths.Count} Paths, {Data.Scripts.Count} Scripts, {Data.Shaders.Count} Shaders");
        Console.WriteLine($"{Data.Fonts.Count} Fonts, {Data.Timelines.Count} Timelines, {Data.GameObjects.Count} Game Objects");
        Console.WriteLine($"{Data.Rooms.Count} Rooms, {Data.Extensions.Count} Extensions, {Data.TexturePageItems.Count} Texture Page Items");
        if (!Data.IsYYC())
        {
            Console.WriteLine($"{Data.Code.Count} Code Entries, {Data.Variables.Count} Variables, {Data.Functions.Count} Functions");
            Console.WriteLine($"{Data.CodeLocals.Count} Code locals, {Data.Strings.Count} Strings, {Data.EmbeddedTextures.Count} Embedded Textures");
        }
        else
        {
            Console.WriteLine("Unknown amount of Code entries and Code locals");
        }

        Console.WriteLine($"{Data.Strings.Count} Strings");
        Console.WriteLine($"{Data.EmbeddedTextures.Count} Embedded Textures");
        Console.WriteLine($"{Data.EmbeddedAudio.Count} Embedded Audio");

        if (IsInteractive) Pause();
    }

    /// <summary>
    /// Dumps a code entry from a data file.
    /// </summary>
    /// <param name="codeEntry">The code entry that should get dumped</param>
    private void DumpCodeEntry(string codeEntry)
    {
        UndertaleCode code = Data.Code.ByName(codeEntry);


        if (code == null)
        {
            Console.Error.WriteLine($"Data file does not contain a code entry named {codeEntry}!");
            return;
        }

        string directory = $"{Output.FullName}/CodeEntries/";

        Directory.CreateDirectory(directory);

        if (Verbose)
            Console.WriteLine($"Dumping {codeEntry}");

        File.WriteAllText($"{directory}/{codeEntry}.gml", GetDecompiledText(code));
    }

    /// <summary>
    /// Dumps all strings in a data file.
    /// </summary>
    private void DumpAllStrings()
    {
        string directory = Output.FullName;

        Directory.CreateDirectory(directory);

        StringBuilder combinedText = new StringBuilder();
        foreach (UndertaleString dataString in Data.Strings)
        {
            if (Verbose)
                Console.WriteLine($"Added {dataString.Content}");
            combinedText.Append($"{dataString.Content}\n");
        }

        if (Verbose)
            Console.WriteLine("Writing all strings to disk");
        File.WriteAllText($"{directory}/strings.txt", combinedText.ToString());
    }

    /// <summary>
    /// Dumps all embedded textures in a data file.
    /// </summary>
    private void DumpAllTextures()
    {
        string directory = $"{Output.FullName}/EmbeddedTextures/";

        Directory.CreateDirectory(directory);

        foreach (UndertaleEmbeddedTexture texture in Data.EmbeddedTextures)
        {
            if (Verbose)
                Console.WriteLine($"Dumping {texture.Name}");
            File.WriteAllBytes($"{directory}/{texture.Name.Content}.png", texture.TextureData.TextureBlob);
        }
    }

    /// <summary>
    /// Replaces a code entry with text from another file.
    /// </summary>
    /// <param name="codeEntry">The code entry to replace</param>
    /// <param name="fileToReplace">File path which should replace the code entry.</param>
    private void ReplaceCodeEntryWithFile(string codeEntry, FileInfo fileToReplace)
    {
        UndertaleCode code = Data.Code.ByName(codeEntry);

        if (code == null)
        {
            Console.Error.WriteLine($"Data file does not contain a code entry named {codeEntry}!");
            return;
        }

        if (Verbose)
            Console.WriteLine("Replacing " + codeEntry);

        ImportGMLString(codeEntry, File.ReadAllText(fileToReplace.FullName));
    }

    /// <summary>
    /// Replaces an embedded texture with contents from another file.
    /// </summary>
    /// <param name="textureEntry">Embedded texture to replace</param>
    /// <param name="fileToReplace">File path which should replace the embedded texture.</param>
    private void ReplaceTextureWithFile(string textureEntry, FileInfo fileToReplace)
    {
        UndertaleEmbeddedTexture texture = Data.EmbeddedTextures.ByName(textureEntry);

        if (texture == null)
        {
            Console.Error.WriteLine($"Data file does not contain an embedded texture named {textureEntry}!");
            return;
        }

        if (Verbose)
            Console.WriteLine("Replacing " + textureEntry);

        texture.TextureData.TextureBlob = File.ReadAllBytes(fileToReplace.FullName);
    }

    /// <summary>
    /// Evaluates and executes the contents of a file as C# Code.
    /// </summary>
    /// <param name="path">Path to file which contents to interpret as C# code</param>
    private void RunCSharpFile(string path)
    {
        string lines;
        try
        {
            lines = File.ReadAllText(path);
        }
        catch (Exception exc)
        {
            // rethrow as otherwise this will get interpreted as success
            Console.Error.WriteLine(exc.Message);
            throw;
        }

        lines = $"#line 1 \"{path}\"\n" + lines;
        ScriptPath = path;
        RunCSharpCode(lines, ScriptPath);
    }

    /// <summary>
    /// Evaluates and executes given C# code.
    /// </summary>
    /// <param name="code">The C# string to execute</param>
    /// <param name="scriptFile">The path to the script file where <see cref="code"/> was executed from.
    /// Leave as null, if it wasn't executed from a script file</param>
    private void RunCSharpCode(string code, string scriptFile = null)
    {
        if (Verbose)
            Console.WriteLine($"Attempting to execute '1{scriptFile ?? code}'...");

        try
        {
            CSharpScript.EvaluateAsync(code, CliScriptOptions, this, typeof(IScriptInterface)).GetAwaiter().GetResult();
            ScriptExecutionSuccess = true;
            ScriptErrorMessage = "";
        }
        catch (Exception exc)
        {
            ScriptExecutionSuccess = false;
            ScriptErrorMessage = exc.ToString();
            ScriptErrorType = "Exception";
        }

        if (!FinishedMessageEnabled) return;

        if (ScriptExecutionSuccess)
        {
            if (Verbose)
                Console.WriteLine($"Finished executing '{scriptFile ?? code}'");
        }
        else
        {
            Console.Error.WriteLine(ScriptErrorMessage);
        }
    }

    /// <summary>
    /// Saves the currently loaded <see cref="Data"/> to an output path.
    /// </summary>
    /// <param name="outputPath">The path where to save the data.</param>
    private void SaveDataFile(string outputPath)
    {
        if (Verbose)
            Console.WriteLine($"Saving new data file to '{outputPath}'");

        using FileStream fs = new FileInfo(outputPath).OpenWrite();
        UndertaleIO.Write(fs, Data, MessageHandler);
        if (Verbose)
            Console.WriteLine($"Saved data file to '{outputPath}'");
    }

    /// <summary>
    /// Read supplied filename and return the data file.
    /// </summary>
    /// <param name="datafile">The datafile to read</param>
    /// <param name="warningHandler">Handler for Warnings</param>
    /// <param name="messageHandler">Handler for Messages</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">If the data file cannot be found</exception>
    private static UndertaleData ReadDataFile(FileInfo datafile, WarningHandlerDelegate warningHandler = null, MessageHandlerDelegate messageHandler = null)
    {
        try
        {
            using FileStream fs = datafile.OpenRead();
            UndertaleData gmData = UndertaleIO.Read(fs, warningHandler, messageHandler);
            return gmData;
        }
        catch (FileNotFoundException e)
        {
            throw new FileNotFoundException($"Data file '{e.FileName}' does not exist");
        }
    }

    // need this on Windows when drag and dropping files.
    /// <summary>
    /// Trims <c>"</c> or <c>'</c> from the beginning and end of a string.
    /// </summary>
    /// <param name="s"><see cref="String"/> to remove <c>"</c> and/or <c>'</c> from</param>
    /// <returns>A new <see cref="String"/> that can be directly passed onto a FileInfo Constructor</returns>
    //TODO: needs some proper testing on how it behaves on Linux/MacOS and might need to get expanded
    private static string RemoveQuotes(string s)

    {
        return s.Trim('"', '\'');
    }

    /// <summary>
    /// Replicated the CMD Pause command. Waits for any key to be pressed before continuing.
    /// </summary>
    private static void Pause()
    {
        Console.WriteLine();
        Console.Write("Press any key to continue . . . ");
        Console.ReadKey(true);
        Console.WriteLine();
    }

    /// <summary>
    /// A simple warning handler that prints warnings to console.
    /// </summary>
    /// <param name="warning">The warning to print</param>
    private static void WarningHandler(string warning) => Console.WriteLine($"[WARNING]: {warning}");

    /// <summary>
    /// A simple message handler that prints messages to console.
    /// </summary>
    /// <param name="message">The message to print</param>
    private static void MessageHandler(string message) => Console.WriteLine($"[MESSAGE]: {message}");

    /// <summary>
    /// A dummy handler that does nothing.
    /// </summary>
    /// <param name="message">Not used.</param>
    private static void DummyHandler(string message)
    {  }

    //TODO: document these as well
    private void ProgressUpdater()
    {
        DateTime prevTime = default;
        int prevValue = 0;

        while (true)
        {
            if (cToken.IsCancellationRequested)
            {
                if (prevValue >= progressValue) //if reached maximum
                    return;

                if (prevTime == default)
                    prevTime = DateTime.UtcNow;                                       //begin measuring
                else if (DateTime.UtcNow.Subtract(prevTime).TotalMilliseconds >= 500) //timeout - 0.5 seconds
                    return;
            }

            UpdateProgressValue(progressValue);

            prevValue = progressValue;

            Thread.Sleep(100); //10 times per second
        }
    }
}