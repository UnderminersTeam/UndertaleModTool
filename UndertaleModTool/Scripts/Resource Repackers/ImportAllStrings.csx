// Adapted from original script by Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

if (Data.ToolInfo.ProfileMode)
{
    ScriptMessage("This script will not modify your existing edited GML code registered in your profile. Please use GML editing for text editing, or a script like FindAndReplace, for editing strings within these code entries.");
}
else
{
    if (!(ScriptQuestion("This script will recompile all code entries in your profile (if they exist) to the default decompiled output. Continue?")))
        return;
    foreach (UndertaleCode c in Data.Code)
        NukeProfileGML(c.Name.Content);
}

string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

//Overwrite Check One
if (!File.Exists(importFolder + "\\strings.txt"))
{
    ScriptError("No 'strings.txt' file exists!", "Error");
    return;
}

int file_length = 0;
string line = "";
using (StreamReader reader = new StreamReader(importFolder + "\\strings.txt"))
{
    while ((line = reader.ReadLine()) != null)
    {
        file_length += 1;
    }
}

int validStringsCount = 0;
foreach (var str in Data.Strings)
{
    if (str.Content.Contains("\n") || str.Content.Contains("\r"))
        continue;
    validStringsCount += 1;
}

if (file_length < validStringsCount)
{
    ScriptError("ERROR 0: Unexpected end of file at line: " + file_length.ToString() + ". Expected file length was: " + validStringsCount.ToString() + ". No changes have been made.", "Error");
    return;
}
else if (file_length > validStringsCount)
{
    ScriptError("ERROR 1: Line count exceeds expected count. Current count: " + file_length.ToString() + ". Expected count: " + validStringsCount.ToString() + ". No changes have been made.", "Error");
    return;
}

using (StreamReader reader = new StreamReader(importFolder + "\\strings.txt"))
{
    int line_no = 1;
    line = "";
    foreach (var str in Data.Strings)
    {
        if (str.Content.Contains("\n") || str.Content.Contains("\r"))
            continue;
        if (!((line = reader.ReadLine()) != null))
        {
            ScriptError("ERROR 2: Unexpected end of file at line: " + line_no.ToString() + ". Expected file length was: " + validStringsCount.ToString() + ". No changes have been made.", "Error");
            return;
        }
        line_no += 1;
    }
}

using (StreamReader reader = new StreamReader(importFolder + "\\strings.txt"))
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
            ScriptError("ERROR 3: Unexpected end of file at line: " + line_no.ToString() + ". Expected file length was: " + validStringsCount.ToString() + ". All lines within the file have been applied. Please check for errors.", "Error");
            return;
        }
        line_no += 1;
    }
}

ReapplyProfileCode();