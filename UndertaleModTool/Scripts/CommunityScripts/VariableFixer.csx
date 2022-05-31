// Made by Creepersbane

EnsureDataLoaded();

ScriptMessage(@"This script will most likely fix data files with the following symptoms:

If, after loading the data file after code editing, any of the following occur:

No window appears at all 
""Out of memory!"" error appears while loading
No "".gamelog.txt"" is produced
The "".gamelog.txt"" is produced but appears to fail after Steam initialization.
The game window appears but is frozen indefinitely
");
Data.GeneralInfo.DisableDebugger = true; 
int globalNum = 0;
int selfNum = 0;
foreach(var vari in Data.Variables)
{
    if (vari.InstanceType == UndertaleInstruction.InstanceType.Global)
    {
        vari.VarID = globalNum++;
    }
    else if ((vari.InstanceType == UndertaleInstruction.InstanceType.Self) && (vari.VarID >= 0))
    {
        vari.VarID = selfNum++;
    }
}
Data.VarCount1 = (uint)globalNum;
Data.VarCount2 = (uint)selfNum;

ScriptMessage(@"Complete, please save and run the game now to apply and test your changes.");