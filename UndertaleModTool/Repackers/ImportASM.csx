// Script by Jockeholm based off of a script by Kneesnap.
// Major help and edited by Samuel Roy

using System;
using System.IO;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

if (Data.ToolInfo.ProfileMode)
{
    if (!ScriptQuestion("This will cause desyncs! As such, your copy of the code(s) you are importing will be cleared, and will be overwritten with a copy decompiled from this ASM. Continue?"))
        return;
}

// Check code directory.
string importFolder = PromptChooseDirectory("Import From Where");
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

// Ask whether they want to link code. If no, will only generate code entry.
// If yes, will try to add code to objects and scripts depending upon its name
bool doParse = ScriptQuestion("Do you want to automatically attempt to link imported code?");

string[] dirFiles = Directory.GetFiles(importFolder);

SetProgressBar(null, "Files", 0, dirFiles.Length);
StartUpdater();

SyncBinding("Strings, Code, CodeLocals, Scripts, GlobalInitScripts, GameObjects", true);
await Task.Run(() => {
    foreach (string file in dirFiles)
    {
        ImportASMFile(file, doParse, true);

        IncProgress();
    }
});
SyncBinding(false);

await StopUpdater();
HideProgressBar();
ScriptMessage("All files successfully imported.");