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

            string basePath = Path.GetTempPath() + "UndertaleModTool\\";

            // Check if Update.zip actually exists
            if (!File.Exists(basePath + "Update.zip")) {
                Console.WriteLine("Update.zip is missing! This program is not meant to be ran by itself, please update through UndertaleModTool.");
                Environment.Exit(1);
            }

            // If this exists from a failed update or something, then remove it
            if (Directory.Exists(basePath + "Update\\"))
            {
                Console.WriteLine("Removing Update folder...");
                Directory.Delete(basePath + "Update\\", true);
            }

            // Extract the update ZIP
            Console.WriteLine("Extracting Update.zip...");
            ZipFile.ExtractToDirectory(basePath + "Update.zip", basePath + "Update\\", true);
            Console.WriteLine("Deleting Update.zip...");
            File.Delete(basePath + "Update.zip");

            // No need to delete the updater, from my tests it can replace itself fine (which is odd...)

            //Console.WriteLine("Deleting UndertaleModToolUpdater.exe from update...");
            //File.Delete(basePath + "Update\\UndertaleModToolUpdater.exe");

            Console.WriteLine("Replacing files with update...");
            MoveDirectory(basePath + "Update\\", AppContext.BaseDirectory);

            Console.WriteLine("Finished updating, launching UTMT...");

            Process p = new Process
            {
                StartInfo = new ProcessStartInfo("UndertaleModTool.exe")
            };
            p.Start();

            Environment.Exit(0);
        }

        public static void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }
    }
}
