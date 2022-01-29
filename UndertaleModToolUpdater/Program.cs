using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace UndertaleModToolUpdater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "UndertaleModTool updater";

            // Get all processes named UndertaleModTool
            Process[] utmtInstances = Process.GetProcessesByName("UndertaleModTool");
            
            if (utmtInstances.Length > 0)
            {
                // Found some! Wait for all of them to close
                Console.WriteLine("Waiting for UndertaleModTool to close...");
                foreach (var instance in utmtInstances)
                {
                    instance.WaitForExit();
                }
            }

            string basePath = Path.Combine(Path.GetTempPath(), "UndertaleModTool") + Path.DirectorySeparatorChar;
            string appPath = null;

            // Check if Update.zip actually exists
            if (!File.Exists(basePath + "Update.zip")) {
                Console.WriteLine("Update.zip is missing! This program is not meant to be ran by itself, please update through UndertaleModTool.");
                Console.WriteLine("Press any key to exit...");
                Console.Read();
                Environment.Exit(1);
            }

            // If this exists from a failed update or something, then remove it
            if (Directory.Exists(basePath + "Update"))
            {
                Console.WriteLine("Removing Update folder...");
                Directory.Delete(basePath + "Update", true);
            }

            if (!File.Exists("actualAppFolder"))
            {
                Console.WriteLine("\"actualAppFolder\" file is missing!");
                Console.WriteLine("Press any key to exit...");
                Console.Read();
                Environment.Exit(1);
            }
            else
            {
                appPath = File.ReadAllText("actualAppFolder");
                File.Delete("actualAppFolder");
            }

            // Extract the update ZIP
            Console.WriteLine("Extracting Update.zip...");
            ZipFile.ExtractToDirectory(basePath + "Update.zip", basePath + "Update", true);
            Console.WriteLine("Deleting Update.zip...");
            File.Delete(basePath + "Update.zip");

            Console.WriteLine("Replacing files with update...");
            MoveDirectory(basePath + "Update", appPath);

            Console.WriteLine("Finished updating, launching UTMT...");

            Process.Start(new ProcessStartInfo(Path.Combine(appPath, "UndertaleModTool.exe"))
            {
                Arguments = "deleteTempFolder"
            });
            
            Environment.Exit(0);
        }

        // source - https://stackoverflow.com/a/2553245/12136394
        static void MoveDirectory(string source, string target)
        {
            var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));

            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(source, target);
                Directory.CreateDirectory(targetFolder);

                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile))
                        File.Delete(targetFile);
                    
                    File.Move(file, targetFile);
                }
            }

            Directory.Delete(source, true);
        }
    }
}
