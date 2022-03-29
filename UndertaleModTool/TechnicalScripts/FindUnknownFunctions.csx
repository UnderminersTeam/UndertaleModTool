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
    throw new ScriptException("The export folder was not set.");

//Overwrite Check One
if (File.Exists(exportFolder + "unknown_functions.txt"))
{
    bool overwriteCheckOne = ScriptQuestion(@"A 'unknown_functions.txt' file already exists. 
Would you like to overwrite it?");
    if (overwriteCheckOne)
    {
        File.Delete(exportFolder + "unknown_functions.txt");
    }
    else
    {
        ScriptError("A 'unknown_functions.txt' file already exists. Please remove it and try again.", "Error: Export already exists.");
        return;
    }
}

BuiltinList list = new BuiltinList();
list.Initialize(Data);

List<String> extensionFunctions = new List<String>();
List<String> unknownFunctions = new List<String>();
List<String> unknownFunctions2 = new List<String>();
foreach (UndertaleExtension extension in Data.Extensions)
{
    foreach (UndertaleExtensionFile exFile in extension.Files)
    {
        foreach (UndertaleExtensionFunction exFunc in exFile.Functions)
        {
            extensionFunctions.Add(exFunc.Name.Content);
        }
    }
}

using (StreamWriter writer = new StreamWriter(exportFolder + "unknown_functions.txt"))
{
    foreach (var func in Data.Functions)
    {
        if (func.Name.Content.Contains("\n") || func.Name.Content.Contains("\r"))
        {
            continue;
        }
        if ((Data.Scripts.ByName(func.Name.Content) != null) || (Data.Code.ByName(func.Name.Content) != null))
        {
            continue;
        }
        if (list.Functions.ContainsKey(func.Name.Content))
        {
            continue;
        }
        bool continue_var = false;
        for (var i = 0; i < extensionFunctions.Count; i++)
        {
            if (extensionFunctions[i] == func.Name.Content)
            {
                continue_var = true;
            }
        }
        if (continue_var)
        {
            continue;
        }
        unknownFunctions.Add(func.Name.Content);
        writer.WriteLine(func.Name.Content);
    }
}

if (unknownFunctions.Count > 0)
{
    if (ScriptQuestion("'unknown_functions.txt' generated. Remove unknown functions now?"))
    {
        string removed = "";
        string resultsToDisplay = "";
        for (var i = 0; i < unknownFunctions.Count; i++)
        {
            resultsToDisplay += (unknownFunctions[i] + "\r\n");
        }
        resultsToDisplay = SimpleTextInput("Prune Menu", "Delete one or more lines to remove those entries", resultsToDisplay, true);
        string[] IndividualLineArray = resultsToDisplay.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var OneLine in IndividualLineArray)
        {
            unknownFunctions2.Add(OneLine.Trim());
        }
        for (var i = 0; i < unknownFunctions.Count; i++)
        {
            bool exists = false;
            for (var j = 0; j < unknownFunctions2.Count; j++)
            {
                if (unknownFunctions[i] == unknownFunctions2[j])
                {
                    exists = true;
                }
            }
            if (!exists)
            {
                removed += (unknownFunctions[i] + "\r\n");
                Data.Functions.Remove(Data.Functions.ByName(unknownFunctions[i]));
            }
        }
        if (removed.Length < 1)
            removed = "No functions ";
        else
            removed = "The function(s)\r\n" + removed;
        ScriptMessage(removed + "were removed.");
    }
}

