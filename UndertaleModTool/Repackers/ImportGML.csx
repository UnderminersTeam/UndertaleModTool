// Script by Jockeholm based off of a script by Kneesnap.
// Major help and edited by Samuel Roy

using System;
using System.IO;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Check code directory.
string importFolder = PromptChooseDirectory("Import From Where");
if (importFolder == null)
    throw new System.Exception("The import folder was not set.");

// Ask whether they want to link code. If no, will only generate code entry.
// If yes, will try to add code to objects and scripts depending upon its name
bool doParse = ScriptQuestion("Do you want to automatically attempt to link imported code?");

int progress = 0;
string[] dirFiles = Directory.GetFiles(importFolder);
foreach (string file in dirFiles)
{
    UpdateProgressBar(null, "Files", progress++, dirFiles.Length);
    ImportGMLFile(file, doParse);
}

HideProgressBar();
ScriptMessage("All files successfully imported.");
