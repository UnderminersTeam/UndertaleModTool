// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

string importFolder = PromptChooseDirectory("Import from where");
if (importFolder == null)
    throw new System.Exception("The import folder was not set.");

//Overwrite Check One
if (!File.Exists(importFolder + "/strings.txt"))
{
    ScriptError("No 'strings.txt' file exists!", "Error");
    return;
}

int file_length = 0;
string line = "";
using (StreamReader reader = new StreamReader(importFolder + "/strings.txt"))
{
    while ((line = reader.ReadLine()) != null)
        file_length += 1;
}
if (file_length < Data.Strings.Count)
{
    ScriptError("Unexpected end of file at line: " + file_length.ToString() + ". Expected file length was: " + Data.Strings.Count.ToString() + ". No changes have been made.", "Error");
    return;
}
else if (file_length > Data.Strings.Count)
{
    ScriptError("Line count exceeds expected count. Current count: " + file_length.ToString() + ". Expected count: " + Data.Strings.Count.ToString() + ". No changes have been made.", "Error");
    return;
}

using (StreamReader reader = new StreamReader(importFolder + "/strings.txt"))
{
    int line_no = 1;
    line = "";
    foreach (var str in Data.Strings)
    {
        if (str.Content.Contains("\n") || str.Content.Contains("\r"))
            continue;
        if (!((line = reader.ReadLine()) != null))
        {
            ScriptError("Unexpected end of file at line: " + line_no.ToString() + ". Expected file length was: " + Data.Strings.Count.ToString() + ". No changes have been made.", "Error");
            return;
        }
        line_no += 1;
    }
}

using (StreamReader reader = new StreamReader(importFolder + "/strings.txt"))
{
    int line_no = 1;
    line = "";
    foreach (var str in Data.Strings)
    {
        if (str.Content.Contains("\n") || str.Content.Contains("\r"))
            continue;
        if ((line = reader.ReadLine()) != null)
            str.Content = line;
        else
        {
            ScriptError("Unexpected end of file at line: " + line_no.ToString() + ". Expected file length was: " + Data.Strings.Count.ToString() + ". All lines within the file have been applied. Please check for errors.", "Error");
            return;
        }
        line_no += 1;
    }
}