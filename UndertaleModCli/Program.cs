using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;

namespace UndertaleModCli
{
    public class Program : IScriptInterface
    {
        // taken fron the Linux programmer manual:
        const int EXIT_SUCCESS = 0;
        const int EXIT_FAILURE = 1;

        public static int Main(string[] args)
        {
            return (new Program()).CliMain(args);
        }

        public UndertaleData Data { get; set; }

        public string FilePath { get; set; }

        public string ScriptPath { get; set; }

        public object Highlighted { get; set; }

        public object Selected { get; set; }

        public bool CanSave { get; set; }

        public bool ScriptExecutionSuccess { get; set; }

        public string ScriptErrorMessage { get; set; }

        public string ExePath { get; set; }

        public string ScriptErrorType { get; set; }

        public ScriptOptions CliScriptOptions { get; set; }

        public bool FinishedMessageEnabled { get; set; }

        // need this on Windows when drag and dropping files.
        public string Dequote(string a) => a.TrimStart('"').TrimEnd('"');

        public void Pause()
        {
            // replica of the cmd pause command.
            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
            Console.WriteLine();
        }

        public void PrintUsage()
        {
            Console.WriteLine("UndertaleModCli Usage:");
            Console.WriteLine("UndertaleModCli.exe <path to the data.win/.ios/.droid/.unx file>");
            Console.WriteLine("If you're on Windows, you can also drag and drop the file onto the executable.");
        }

        public void RunCodeLine(string line)
        {
            string msg = "Script execution complete.";

            try
            {
                CSharpScript.EvaluateAsync(line, CliScriptOptions, this, typeof(IScriptInterface)).GetAwaiter().GetResult();
                ScriptExecutionSuccess = true;
                ScriptErrorMessage = "";
            }
            catch (Exception exc)
            {
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.ToString();
                ScriptErrorType = "Exception";
                msg = ScriptErrorMessage;
            }

            if (FinishedMessageEnabled)
            {
                Console.WriteLine(msg);
            }
        }

        public void RunCodeFile(string path)
        {
            string lines = null;
            try
            {
                lines = File.ReadAllText(path);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Script file not found or cannot be read.");
                Console.WriteLine(exc);
            }

            if (lines != null)
            {
                ScriptPath = path;
                RunCodeLine(lines);
                ScriptPath = null;
            }
        }

        public void OnWarning(string warning) => Console.WriteLine("[WARNING]: {0}", warning);

        public void OnMessage(string message) => Console.WriteLine("[MESSAGE]: {0}", message);

        public void CliSave(string to)
        {
            Console.WriteLine("Saving...");
            FileStream fs = new FileStream(to, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            UndertaleIO.Write(fs, Data, OnMessage);
            fs.Dispose(); fs = null;
            Console.WriteLine("Done!");
        }

        public void CliQuickInfo()
        {
            Console.WriteLine("Quick Information:");
            Console.WriteLine("Project Name - {0}", Data.GeneralInfo.Name);
            Console.WriteLine("Is GMS2 - {0}", Data.IsGameMaker2());
            Console.WriteLine("Is YYC - {0}", Data.IsYYC());
            Console.WriteLine("Bytecode version - {0}", Data.GeneralInfo.BytecodeVersion);
            Console.WriteLine("Configuration name - {0}", Data.GeneralInfo.Config);
            // TODO: add more things?
            Console.WriteLine("Quick Information end.");
            Pause();
        }

        public bool OnMenu()
        {
            Console.WriteLine("1 - Run script.");
            Console.WriteLine("2 - Run C# string.");
            Console.WriteLine("3 - Save and overwrite.");
            Console.WriteLine("4 - Save to different place.");
            Console.WriteLine("5 - Display quick info.");
            Console.WriteLine("6 - Quit. (please be sure you've saved everything!!)");

            Console.Write("Input, please: ");
            var k = Console.ReadKey().Key;
            Console.WriteLine();

            switch (k)
            {
                case ConsoleKey.D1:
                    {
                        Console.Write("File path (you can drag and drop on Windows)?: ");
                        string path = Dequote(Console.ReadLine());
                        Console.WriteLine("Trying to run script {0}", path);
                        RunCodeFile(path);
                        break;
                    }

                case ConsoleKey.D2:
                    {
                        Console.Write("C# code line?: ");
                        string line = Console.ReadLine();
                        ScriptPath = null;
                        RunCodeLine(line);
                        break;
                    }

                case ConsoleKey.D3:
                    {
                        CliSave(FilePath);
                        break;
                    }

                case ConsoleKey.D4:
                    {
                        Console.Write("Where to save?: ");
                        string path = Dequote(Console.ReadLine());
                        CliSave(path);
                        break;
                    }

                case ConsoleKey.D5:
                    {
                        CliQuickInfo();
                        break;
                    }

                case ConsoleKey.D6:
                    {
                        Console.WriteLine("Are you SURE? You can press 'n' and save before the changes are gone forever!!");
                        Console.WriteLine("(Y/N)?: ");
                        var yes = Console.ReadKey(false).Key == ConsoleKey.Y;
                        Console.WriteLine();
                        if (yes) return false;
                        break;
                    }

                default:
                    {
                        Console.WriteLine("Unknown input. Try using the upper line of digits on your keyboard.");
                        break;
                    }
            }

            return true;
        }

        public int CliMain(string[] args)
        {
            try
            {
                Console.InputEncoding = System.Text.Encoding.UTF8;
                Console.OutputEncoding = Console.InputEncoding;

                // no args supplied?
                if (args.Length <= 0)
                {
                    PrintUsage();
                    Pause();
                    return EXIT_SUCCESS;
                }

                Console.WriteLine("Trying to load file {0}", args[0]);

                FileStream fs = new FileStream(args[0], FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

                FinishedMessageEnabled = true;
                FilePath = args[0];
                ExePath = Environment.CurrentDirectory;
                CliScriptOptions = ScriptOptions.Default
                                .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler",
                                            "UndertaleModLib.Scripting", "UndertaleModLib.Compiler",
                                            "UndertaleModLib.Util", "System", "System.IO", "System.Collections.Generic",
                                            "System.Text.RegularExpressions")
                                .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly,
                                                GetType().GetTypeInfo().Assembly,
                                                typeof(JsonConvert).GetTypeInfo().Assembly,
                                                typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly,
                                                typeof(TextureWorker).GetTypeInfo().Assembly);

                Data = UndertaleIO.Read(fs, OnWarning, OnMessage);

                // excplicitly free.
                fs.Dispose(); fs = null;

                Console.WriteLine("File loaded.");

                while (OnMenu()) { }

                return EXIT_SUCCESS;
            }
            catch (Exception exc)
            {
                Console.WriteLine("An exception has occurred in UndertaleModCli:");
                Console.WriteLine(exc);
                Console.WriteLine("Report this as a bug.");
                return EXIT_FAILURE;
            }
        }

        public void EnsureDataLoaded()
        {
            if (Data is null)
                throw new Exception("No data file is loaded.");
        }

        public bool Make_New_File()
        {
            Data = UndertaleData.CreateNew();
            Console.WriteLine("Don't you have anything better to do?");
            return true;
        }

        public void ReplaceTempWithMain(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void ReplaceMainWithTemp(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void ReplaceTempWithCorrections(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void ReplaceCorrectionsWithTemp(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void UpdateCorrections(bool ImAnExpertBTW = false)
        {
            throw new NotImplementedException();
        }

        public void ScriptMessage(string message)
        {
            Console.WriteLine(message);
            Pause();
        }

        public void SetUMTConsoleText(string message)
        {
            Console.Title = message;
        }

        public bool ScriptQuestion(string message)
        {
            Console.WriteLine(message);
            Console.Write("Input (Y/N)?: ");
            var yes = Console.ReadKey(false).Key == ConsoleKey.Y;
            Console.WriteLine();
            return yes;
        }

        public void ScriptError(string error, string title = "Error", bool SetConsoleText = true)
        {
            // no need to care about SetConsoleText if we're in CLI.........
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("---------------------ERROR!-----------------------");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(title);
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(error);
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("---------------------ERROR!-----------------------");
            Console.WriteLine("--------------------------------------------------");
            Pause();
        }

        public void ScriptOpenURL(string url)
        {
            Process p;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            {
                p = Process.Start("xdg-open", url);  // Works ok on linux, should work on FreeBSD as it's very similar.
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                p = Process.Start("open", url); // Not tested
            }
            else
            {
                throw new InvalidOperationException("Unable to open the browser on this OS.");
            }

            if (p != null) p.Dispose();
        }

        public bool SendAUMIMessage(IpcMessage_t ipMessage, ref IpcReply_t outReply)
        {
            return false;
        }

        public bool RunUMTScript(string path)
        {
            try
            {
                RunCodeFile(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool LintUMTScript(string path)
        {
            throw new NotImplementedException();
        }

        public void InitializeScriptDialog()
        {
            throw new NotImplementedException();
        }

        public void ReapplyProfileCode()
        {
            throw new NotImplementedException();
        }

        public void NukeProfileGML(string codeName)
        {
            throw new NotImplementedException();
        }

        public string GetDecompiledText(string codeName)
        {
            throw new NotImplementedException();
        }

        public string GetDisassemblyText(string codeName)
        {
            throw new NotImplementedException();
        }

        public bool AreFilesIdentical(string File01, string File02)
        {
            try
            {
                using var fs1 = new FileStream(File01, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var fs2 = new FileStream(File01, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs1.Length != fs2.Length) return false; // different size.

                while (true)
                {
                    int b1 = fs1.ReadByte();
                    int b2 = fs2.ReadByte();
                    if (b1 != b2) return false; // different contents.
                    if (b1 == -1 || b2 == -1) break; // end of files.
                }

                // identical
                return true;
            }
            catch
            {
                // wtf?!
                return false;
            }
        }

        public string ScriptInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
        {
            throw new NotImplementedException();
        }

        public string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline)
        {
            throw new NotImplementedException();
        }

        public void SetFinishedMessage(bool isFinishedMessageEnabled)
        {
            FinishedMessageEnabled = isFinishedMessageEnabled;
        }

        public void UpdateProgressBar(string message, string status, double progressValue, double maxValue)
        {
            // i know, ugly
            Console.WriteLine("[{0}|{1}] {2} out of {3}", message, status, progressValue, maxValue);
        }

        public void HideProgressBar()
        {
            // nothing to hide..
        }

        public void ChangeSelection(object newsel)
        {
            Selected = newsel;
        }

        public string PromptChooseDirectory(string prompt)
        {
            Console.WriteLine("Please type a path (or drag and drop on Windows) to a directory:");
            Console.Write("Path: ");
            string p = Console.ReadLine();
            return p;
        }

        public string PromptLoadFile(string defaultExt, string filter)
        {
            throw new NotImplementedException("don't you have ANYTHING better to do?");
        }

        public void ImportGMLString(string codeName, string gmlCode, bool doParse = true, bool CheckDecompiler = false)
        {
            throw new NotImplementedException("don't you have ANYTHING better to do?");
        }

        public void ImportASMString(string codeName, string gmlCode, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            throw new NotImplementedException("don't you have ANYTHING better to do?");
        }

        public void ImportGMLFile(string fileName, bool doParse = true, bool CheckDecompiler = false)
        {
            throw new NotImplementedException("don't you have ANYTHING better to do?");
        }

        public void ImportASMFile(string fileName, bool doParse = true, bool destroyASM = true, bool CheckDecompiler = false)
        {
            throw new NotImplementedException("don't you have ANYTHING better to do?");
        }

        public void ReplaceTextInGML(string codeName, string keyword, string replacement, bool case_sensitive = false, bool isRegex = false)
        {
            throw new NotImplementedException("don't you have ANYTHING better to do?");
        }

        public bool DummyBool()
        {
            return false;
        }

        public void DummyVoid()
        {
            // i want hugs so bad :(
        }

        public string DummyString()
        {
            return "";
        }
    }
}
