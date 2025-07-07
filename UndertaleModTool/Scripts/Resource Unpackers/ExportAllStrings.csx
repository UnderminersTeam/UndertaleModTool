//Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string exportFolder = PromptChooseDirectory();
if (exportFolder is null)
    throw new ScriptException("The export folder was not set.");

string stringsPath = Path.Combine(exportFolder, "strings.txt");
//Overwrite Check One
if (File.Exists(stringsPath))
{
    bool overwriteCheckOne = ScriptQuestion(@"A 'strings.txt' file already exists.
Would you like to overwrite it?");
    if (!overwriteCheckOne)
    {
        ScriptError("A 'strings.txt' file already exists. Please remove it and try again.", "Error: Export already exists.");
        return;
    }
    File.Delete(stringsPath);
}

using (StreamWriter writer = new StreamWriter(stringsPath))
{
    foreach (var str in Data.Strings)
    {
        if (str.Content.Contains("\n") || str.Content.Contains("\r"))
            continue;
        writer.WriteLine(str.Content);
    }
}