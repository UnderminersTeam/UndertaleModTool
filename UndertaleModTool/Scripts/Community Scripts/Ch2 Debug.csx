// Enables debug mode
EnsureDataLoaded();
bool enable = ScriptQuestion(@"
Debug Manager by krzys-h and Kneesnap
Chapter 2 fix by Agent 7
obj_debugProfiler fix by Jacky720

Yes = Enable Debug
No = Disable Debug");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true
};

var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART", true)?.Code;
var debugController = "gml_Object_obj_debugcontroller_ch1_Create_0";
var debugProfiler = Data.Code.ByName("gml_Object_obj_debugProfiler_Create_0");
if (SCR_GAMESTART == null)
    throw new ScriptException("Could not find SCR_GAMESTART.");
if (Data.Code.ByName(debugController) == null)
    throw new ScriptException("Could not find Chapter 1 debug controller.");
if (debugProfiler == null)
    throw new ScriptException("Could not find debug profiler.");

bool patch1 = false;
bool patch2 = false;
bool patchProfiler = false;
for(int i = 0; i < SCR_GAMESTART.Instructions.Count; i++) 
{
    if (SCR_GAMESTART.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && SCR_GAMESTART.Instructions[i].ValueVariable.Name.Content == "debug") 
    {
        SCR_GAMESTART.Instructions[i-1].ValueShort = (short)(enable ? 1 : 0);
        patch2 = true;
    }
}
for(int i = 0; i < 1; i++)
{
    importGroup.QueueFindReplace(debugController, @"debug = ", "debug = " + (enable ? "true;" : "false;") + "//");
    patch1 = true;
}
if (debugProfiler.Instructions.Count == 0 && enable) // 1.09+, debugProfiler blanked
{
    importGroup.QueueReplace(debugProfiler, "cutsceneshow = false");
    patchProfiler = true;
}
else if (debugProfiler.Instructions.Count == 2 && !enable)
{
    importGroup.QueueReplace(debugProfiler, "");
    patchProfiler = true;
}

if (!patch2) // Failed to patch Chapter 2.
    throw new ScriptException("Chapter 2 Patch point not found?");
if (!patch1) // Failed to patch Chapter 1.
    throw new ScriptException("Chapter 1 Patch point not found?");

importGroup.Import();

ChangeSelection(SCR_GAMESTART); // Show.
ScriptMessage("Debug Mode " + (enable ? "enabled" : "disabled") + ".");
