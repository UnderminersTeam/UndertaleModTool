// TPAG 4 byte alignment toggler by Grossley
EnsureDataLoaded();
if (ScriptQuestion("TPAG 4-Byte alignment is currently: " + (Data.IsTPAG4ByteAligned ? "enabled" : "disabled") + ". Would you like to change it?"))
    Data.IsTPAG4ByteAligned = ScriptQuestion("Toggle TPAG 4-byte alignment by Grossley\n\nYes = Enable TPAG 4-byte alignment (Android/Vita)\nNo = Disable TPAG 4-byte alignment");
else
{
    ScriptMessage("Change cancelled.");
    return;
}
ScriptMessage("TPAG 4-Byte alignment is now: " + (Data.IsTPAG4ByteAligned ? "enabled" : "disabled") + ".");