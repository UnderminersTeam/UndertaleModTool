//For use playing the Switch version of Deltarune on PC with the regular Deltarune runner - by Grossley

EnsureDataLoaded();

if (Data.IsVersionAtLeast(2, 3))
{
    bool x = RunUMTScript(Path.Combine(ExePath, "Scripts", "Technical Scripts", "ConvertFrom17to16_for_2.3.csx"));
    if (x == false)
        ScriptError("ConvertFrom17to16_for_2.3.csx failed!");
    return;
}

if (Data?.GeneralInfo.BytecodeVersion >= 17)
{
    if (!ScriptQuestion("Downgrade bytecode from 17 to 16?"))
    {
        ScriptMessage("Cancelled.");
        return;
    }
    Data.GeneralInfo.BytecodeVersion = 16;
    if (Data.FORM.Chunks.ContainsKey("TGIN"))
        Data.FORM.Chunks.Remove("TGIN");
    Data.SetGMS2Version(2);
    ScriptMessage("Downgraded from 17 to 16 successfully. Save the game to apply the changes.");
}
else
{
    ScriptError("This is already bytecode 16 or lower.", "Error");
    return;
}