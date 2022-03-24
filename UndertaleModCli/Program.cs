using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System;
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
using System.Threading;
using System.Threading.Tasks;

namespace UndertaleModCli
{
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
        /// File path to where to save the modified data file
        /// </summary>
        private FileInfo? Output { get; }

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
            var verboseOption = new Option<bool>(new []{"-v", "--verbose"}, "Detailed logs");

            var dataFileOption = new Argument<FileInfo>("datafile", "Path to the data.win/.ios/.droid/.unx file");

            // Setup new command
            Command newCommand = new Command("new", "Generates a blank data file")
            {
                new Option<FileInfo>(new []{"-o", "--output"}, () => new NewOptions().Output),
                new Option<bool>(new []{"-f", "--overwrite"}, "Overwrite destination file if it already exists"),
                new Option<bool>(new []{"-", "--stdout"}, "Write new data content to stdout"),  // "-" is often used in *nix land as a replacement for stdout
                verboseOption
            };
            newCommand.Handler = CommandHandler.Create<NewOptions>(Program.New);

            // Setup load command
            var scriptRunnerOption = new Option<FileInfo[]>(new []{ "-s", "--scripts"}, "Scripts to apply to the <datafile>. ex. a.csx b.csx");
            Command loadCommand = new Command("load", "Load a data file and perform actions on it") {
                dataFileOption,
                scriptRunnerOption,
                verboseOption,
                new Option<FileInfo>(new []{"-o", "--output"}, "Where to save the modified data file"),
                //TODO: why no force overwrite here?
                new Option<string>(new []{"-l","--line"}, "Run C# string. Runs AFTER everything else"),
                new Option<bool>(new []{"-i", "--interactive"}, "Interactive menu launch")
            };
            loadCommand.Handler = CommandHandler.Create<LoadOptions>(Program.Load);

            // Setup info command
            Command infoCommand = new Command("info", "Show basic info about the game data file")
            {
                dataFileOption,
                verboseOption
            };
            infoCommand.Handler = CommandHandler.Create<InfoOptions>(Program.Info);

            // Merge everything together
            RootCommand rootCommand = new RootCommand
            {
                newCommand,
                loadCommand,
                infoCommand
            };
            rootCommand.Description = "CLI tool for modding, decompiling and unpacking Undertale (and other Game Maker: Studio games)!";
            Parser commandLine = new CommandLineBuilder(rootCommand)
                                    .UseDefaults() // automatically configures dotnet-suggest
                                    .Build();

            return commandLine.Invoke(args);
        }

        public Program(FileInfo datafile, FileInfo[]? scripts, FileInfo? output, bool verbose = false, bool interactive = false)
        {
            this.Verbose = verbose;
            IsInteractive = interactive;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = Console.InputEncoding;


            Console.WriteLine($"Trying to load file: {datafile.FullName}");

            this.FilePath = datafile.FullName;
            this.ExePath = Environment.CurrentDirectory;
            this.Output = output;

            this.Data = ReadDataFile(datafile, this.Verbose ? WarningHandler : null, this.Verbose ? MessageHandler : null);

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
                                            typeof(TextureWorker).GetTypeInfo().Assembly)
                            // "WithEmitDebugInformation(true)" not only lets us to see a script line number which threw an exception,
                            // but also provides other useful debug info when we run UMT in "Debug".
                            .WithEmitDebugInformation(true);
        }

        public Program(FileInfo datafile, bool verbose)
        {
            this.Data = ReadDataFile(datafile, verbose ? WarningHandler : null, verbose ? MessageHandler : null);
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
                Console.Error.WriteLine($"{options.Output} already exists. Pass --overwrite to overwrite");
                return EXIT_FAILURE;
            }

            // We're not writing to STDOUT, and overwrite flag was given, so we write to specified file.
            if (options.Verbose) Console.WriteLine($"Attempting to write new Data file to {options.Output}...");
            using FileStream fs = options.Output.OpenWrite();
            UndertaleIO.Write(fs, data);
            if (options.Verbose) Console.WriteLine($"Successfully wrote new Data file to {options.Output}.");
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

            // try to load necessary values
            try
            {
                //TODO: can constructor ever fail?
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
                        RunCSharpFile(path);
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
            Console.WriteLine($"{Data.Code.Count} Code Entries, {Data.Variables.Count} Variables, {Data.Functions.Count} Functions");
            Console.WriteLine($"{Data.CodeLocals.Count} Code locals, {Data.Strings.Count} Strings, {Data.EmbeddedTextures.Count} Embedded Textures");
            Console.WriteLine($"{Data.EmbeddedAudio.Count} Embedded Audio");

            if (IsInteractive) Pause();
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
            catch (IOException exc)
            {
                Console.Error.WriteLine(exc.Message);
                return;
            }

            ScriptPath = path;
            RunCSharpCode(lines, ScriptPath);
        }

        /// <summary>
        /// Evaluates and executes given C# code.
        /// </summary>
        /// <param name="code">The C# string to execute</param>
        /// <param name="scriptFile">The path to the script file where <see cref="code"/> was executed from.
        /// Leave as null, if it wasn't executed from a script file</param>
        private void RunCSharpCode(string code, string? scriptFile = null)
        {
            if (Verbose)
                Console.WriteLine($"Attempting to execute {scriptFile ?? code}...");

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
                    Console.WriteLine($"Finished executing {scriptFile ?? code}");
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
                Console.WriteLine($"Saving new data file to {outputPath}");

            using FileStream fs = new FileInfo(outputPath).OpenWrite();
            UndertaleIO.Write(fs, Data, MessageHandler);
            if (Verbose)
                Console.WriteLine($"Saved data file to {outputPath}");
        }

        /// <summary>
        /// Read supplied filename and return the data file.
        /// </summary>
        /// <param name="datafile">The datafile to read</param>
        /// <param name="warningHandler">Handler for Warnings</param>
        /// <param name="messageHandler">Handler for Messages</param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">If the data file cannot be found</exception>
        private static UndertaleData ReadDataFile(FileInfo datafile, WarningHandlerDelegate? warningHandler = null, MessageHandlerDelegate? messageHandler = null)
        {
            try
            {
                using FileStream fs = datafile.OpenRead();
                return UndertaleIO.Read(fs, warningHandler, messageHandler);
            }
            catch (FileNotFoundException e)
            {
                throw new FileNotFoundException($"data file {e.FileName} does not exist");
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
            return s.TrimStart('"').TrimEnd('"');
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
}
