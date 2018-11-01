// Enables debug mode

EnsureDataLoaded();

ScriptMessage("Debug mode enabler\nby krzys_h");

var scr_debug = Data.Scripts.ByName("scr_debug")?.Code;
if (scr_debug != null) // Deltarune
{
    scr_debug.Replace(Assembler.Assemble(@"
pushglb.v global.debug
ret.v
", Data.Functions, Data.Variables, Data.Strings));
}

bool ok = false;
var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART")?.Code;
if (SCR_GAMESTART == null)
    SCR_GAMESTART = Data.Scripts.ByName("scr_gamestart")?.Code; // Deltarune
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