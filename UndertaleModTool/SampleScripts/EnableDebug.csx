// Enables debug mode
EnsureDataLoaded();
bool enable = ScriptQuestion("Debug Manager by krzys-h and Kneesnap\n\nYes = Enable Debug\nNo = Disable Debug");

var scr_debug = Data.Scripts.ByName("scr_debug")?.Code;
if (scr_debug != null) // Deltarune debug check script.
    scr_debug.ReplaceGML(@"return global.debug;", Data);

var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART", true)?.Code;
if (SCR_GAMESTART == null)
    throw new System.Exception("Could not find SCR_GAMESTART.");

bool patch = false;
for(int i = 0; i < SCR_GAMESTART.Instructions.Count; i++) 
{
	if (SCR_GAMESTART.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && SCR_GAMESTART.Instructions[i].Destination.Target.Name.Content == "debug") 
    {
		SCR_GAMESTART.Instructions[i-1].Value = (short)(enable ? 1 : 0);
        patch = true;
    }
}

if (!patch) // Failed to patch.
	throw new System.Exception("Patch point not found?");

ChangeSelection(SCR_GAMESTART); // Show.
ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");