// Enables debug mode

if (Data == null)
	throw new Exception("Please load data.win first!");

ScriptMessage("Debug mode enabler\nby krzys_h");

bool ok = false;
var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART")?.Code;
if (SCR_GAMESTART == null)
	throw new Exception("Script SCR_GAMESTART not found");
for(int i = 0; i < SCR_GAMESTART.Instructions.Count; i++)
{
	if (SCR_GAMESTART.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && SCR_GAMESTART.Instructions[i].Destination.Target.Name.Content == "debug")
	{
		ok = true;
        bool enable = ScriptQuestion("Enable or disable?");
		SCR_GAMESTART.Instructions[i-1].Value = (short)(enable ? 1 : 0);
		ScriptMessage("Debug mode " + (enable ? "enabled" : "disabled"));
	}
}
if (!ok)
	throw new Exception("Patch point not found?");
ChangeSelection(SCR_GAMESTART);