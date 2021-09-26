// Enables debug mode
EnsureDataLoaded();
bool enable = ScriptQuestion("Debug Manager by krzys-h and Kneesnap\nChapter 2 fix by Agent 7\n\nYes = Enable Debug\nNo = Disable Debug");

var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART", true)?.Code;
var debugController = "gml_Object_obj_debugcontroller_ch1_Create_0";
if (SCR_GAMESTART == null)
    throw new System.Exception("Could not find SCR_GAMESTART.");
if (debugController == null)
    throw new System.Exception("Could not find Chapter 1 debug controller.");

bool patch2 = false;
bool patch1 = false;
for(int i = 0; i < SCR_GAMESTART.Instructions.Count; i++) 
{
	if (SCR_GAMESTART.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && SCR_GAMESTART.Instructions[i].Destination.Target.Name.Content == "debug") 
    {
		SCR_GAMESTART.Instructions[i-1].Value = (short)(enable ? 1 : 0);
        patch2 = true;
    }
}
for(int i = 0; i < 1; i++)
{
    if (enable)
    ReplaceTextInGML(debugController, @"debug = 0", @"debug = 1");
    else
    ReplaceTextInGML(debugController, @"debug = 1", @"debug = 0");
    patch1 = true;
}

if (!patch2) // Failed to patch Chapter 2.
	throw new System.Exception("Chapter 2 Patch point not found?");
if (!patch1) // Failed to patch Chapter 1.
    throw new System.Exception("Chapter 1 Patch point not found?");

ChangeSelection(SCR_GAMESTART); // Show.
ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");