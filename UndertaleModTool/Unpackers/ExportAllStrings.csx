//Adapted from original script by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string exportFolder = PromptChooseDirectory("Export to where");
if (exportFolder == null)
    throw new System.Exception("The export folder was not set.");

//Overwrite Check One
if (File.Exists(exportFolder + "strings.txt"))
{
    bool overwriteCheckOne = ScriptQuestion(@"A 'strings.txt' file already exists. 
Would you like to overwrite it?");
    if (overwriteCheckOne)
        File.Delete(exportFolder + "strings.txt");
    if (!overwriteCheckOne)
    {
        ScriptError("A 'strings.txt' file already exists. Please remove it and try again.", "Error: Export already exists.");
        return;
    }
}

using (StreamWriter writer = new StreamWriter(exportFolder + "strings.txt"))
{
    foreach (var str in Data.Strings)
    {
        if (str.Content.Contains("\n") || str.Content.Contains("\r"))
            continue;
        writer.WriteLine(str.Content);
    }
}
