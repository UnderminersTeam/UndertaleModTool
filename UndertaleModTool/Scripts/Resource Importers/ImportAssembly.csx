// Script by Jockeholm based off of a script by Kneesnap.
// Major help and edited by Samuel Roy

using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using UndertaleModLib.Util;

EnsureDataLoaded();

// Check code directory.
string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

string[] dirFiles = Directory.GetFiles(importFolder);
if (dirFiles.Length == 0)
    throw new ScriptException("The selected folder is empty.");
else if (!dirFiles.Any(x => x.EndsWith(".asm")))
    throw new ScriptException("The selected folder doesn't contain any ASM file.");

bool stopOnError = ScriptQuestion("Stop importing on error?");

SetProgressBar(null, "Files", 0, dirFiles.Length);
StartProgressBarUpdater();

SyncBinding("Strings, Code, CodeLocals, Scripts, GlobalInitScripts, GameObjects, Functions, Variables", true);
await Task.Run(() => 
{
    foreach (string file in dirFiles)
    {
        string asm = File.ReadAllText(file);
        string codeName = Path.GetFileNameWithoutExtension(file);

        if (Data.Code.ByName(codeName) is UndertaleCode code)
        {
            try
            {
                code.Replace(Assembler.Assemble(asm, Data));
            }
            catch (Exception e)
            {
                if (stopOnError)
                {
                    throw new ScriptException($"Error on code entry {codeName}:\n{e}");
                }
                else
                {
                    ScriptError($"Error on code entry {codeName}:\n{e}");
                }
            }
        }
        else
        {
            if (stopOnError)
            {
                throw new ScriptException($"Missing code entry {codeName} (must exist before importing)");
            }
            else
            {
                ScriptError($"Missing code entry {codeName} (must exist before importing)");
            }
        }

        IncrementProgress();
    }
});
DisableAllSyncBindings();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("All files successfully imported.");