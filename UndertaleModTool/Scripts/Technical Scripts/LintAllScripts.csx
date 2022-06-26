using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UndertaleModLib.Util;

string umtBaseDir = Path.Combine(ExePath, "Scripts");
string[] scripts = GetFilesRecursively(umtBaseDir);
StringBuilder output = new StringBuilder();
int scriptsCount = scripts.Length;
int successCount = 0;
int failedCount = 0;

ScriptMessage("Linting all scripts!");
SetProgressBar(null, "Files", 0, scriptsCount);
StartProgressBarUpdater();

await Task.Run(() =>
{
    // Lint each script and create the output.
    foreach (string file in scripts)
    {
        bool lintSuccessful = LintUMTScript(file);
        if (lintSuccessful)
        {
            output.Append($"{Path.GetFileName(file)} succeeded (full path \"{file}\")\n");
            successCount++;
        }
        else
        {
            output.Append($"{Path.GetFileName(file)} failed (full path \"{file}\") with the following error of type: {ScriptErrorType}\n\n" +
                      $"Error message: {ScriptErrorMessage}\n");
            failedCount++;
        }
        IncrementProgress();
    }
});
await StopProgressBarUpdater();

// Write output either in results file, or console if former doesnt work and show results
string outputLogLocation = Path.Combine(ExePath, "LintResults.txt");
string infoLocation = outputLogLocation;
try
{
    File.WriteAllText(outputLogLocation, output.ToString());
}
catch
{
    infoLocation = "the console at the bottom of the screen";
    ScriptError($"Could not write to {outputLogLocation}, the output log will be in {infoLocation}.");
    SetUMTConsoleText(output.ToString());
}
ScriptMessage($"{successCount} scripts have no errors, {failedCount} have errors.\n\nFor more information see {infoLocation}.");
HideProgressBar();

// Searches for all csx files recursively and returns them.
string[] GetFilesRecursively(string directoryPath)
{
    List<string> files = new List<string>();
    DirectoryInfo directory = new DirectoryInfo(directoryPath);

    // Call this recursively for all directories
    foreach (DirectoryInfo subDir in directory.GetDirectories())
        files.AddRange(GetFilesRecursively(subDir.FullName));

    // Add all csx files
    foreach (FileInfo file in directory.GetFiles())
    {
        if (file.Extension != ".csx")
            continue;
        files.Add(file.FullName);
    }

    return files.ToArray();
}