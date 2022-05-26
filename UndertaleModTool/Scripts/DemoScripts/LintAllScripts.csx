// By Grossley

using System;
using System.IO;
using UndertaleModLib.Util;

int progress = 0;

string UMTBaseDir = Path.Combine(ExePath, "Scripts");
string dirSampleScripts = Path.Combine(UMTBaseDir, "SampleScripts");
string dirCommunityScripts = Path.Combine(UMTBaseDir, "CommunityScripts");
string dirUnpackers = Path.Combine(UMTBaseDir, "Unpackers");
string dirRepackers = Path.Combine(UMTBaseDir, "Repackers");
string dirTechnicalScripts = Path.Combine(UMTBaseDir, "TechnicalScripts");
string dirDemoScripts = Path.Combine(UMTBaseDir, "DemoScripts");
string dirHelperScripts = Path.Combine(UMTBaseDir, "HelperScripts");

ScriptMessage("Linting all bundled scripts");
string[] directories = new string[7];
directories[0] = dirSampleScripts;
directories[1] = dirCommunityScripts;
directories[2] = dirUnpackers;
directories[3] = dirRepackers;
directories[4] = dirTechnicalScripts;
directories[5] = dirDemoScripts;
directories[6] = dirHelperScripts;
string output = "";
int scriptsCount = 0;
int successCount = 0;
int failedCount = 0;
foreach (string dir in directories)
{
    string[] filesCount = Directory.GetFiles(dir);
    scriptsCount += filesCount.Length;
}
foreach (string dir in directories)
{
    string[] dirFiles = Directory.GetFiles(dir);
    foreach (string file in dirFiles)
    {
        UpdateProgressBar(null, "Files", progress++, scriptsCount);
        if (!(Path.GetFileName(file).EndsWith(".csx")))
            continue;
        bool x = LintUMTScript(file);
        if (!x)
        {
            output += (Path.GetFileName(file) + " failed (full path \"" + file + "\") with the following error of type: " + ScriptErrorType + "\r\n\r\nError message: " + ScriptErrorMessage + "\r\n");
            failedCount += 1;
        }
        else
        {
            output += (Path.GetFileName(file) + " succeeded (full path \"" + file + "\")\r\n");
            successCount += 1;
        }
    }
}
string outputLogLocation = Path.Combine(ExePath, "Lint.txt");
bool failed = false;
try
{
    File.WriteAllText(outputLogLocation, output);
}
catch
{
    failed = true;
}
HideProgressBar();
ScriptMessage(successCount.ToString() + " have no errors, " + failedCount.ToString() + " have errors.\n\nFor more information see " + outputLogLocation);
if (failed)
{
    ScriptError("Could not write to " + outputLogLocation + ", the output log will be in the console at the bottom of the screen.");
    SetUMTConsoleText(output);
}
return;
