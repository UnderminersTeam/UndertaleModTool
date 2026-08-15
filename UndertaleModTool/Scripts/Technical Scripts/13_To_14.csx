//Upgrade from bytecode 13 (experimental), 14, 15 to 16 - by Grossley

EnsureDataLoaded();

string currentBytecodeVersion = Data?.GeneralInfo.BytecodeVersion.ToString();

if (!(Data.FORM.Chunks.ContainsKey("AGRP")))
{
    if (!ScriptQuestion("Upgrade bytecode from 13 (experimental) to 14?"))
    {
        ScriptMessage("Cancelled.");
        return;
    }
    if (!(Data.FORM.Chunks.ContainsKey("AGRP")))
    {
        Data.FORM.Chunks["AGRP"] = new UndertaleChunkAGRP();
        var previous = -1;
        var j = 0;
        for (var i = -1; i < Data.Sounds.Count - 1; i++)
        {
            UndertaleSound sound = Data.Sounds[i + 1];
            bool flagCompressed = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);
            bool flagEmbedded = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
            if (i == -1)
            {
                if (!flagCompressed && !flagEmbedded)
                {
                    sound.AudioID = -1;
                }
                else
                {
                    sound.AudioID = 0;
                    previous = 0; 
                    j = 1;
                }
            }
            else
            {
                if (!flagCompressed && !flagEmbedded)
                    sound.AudioID = previous;
                else
                {
                    sound.AudioID = j;
                    previous = j;
                    j++;
                }
            }
        }
        foreach (UndertaleSound sound in Data.Sounds)
        {
            if ((sound.AudioID >= 0) && (sound.AudioID < Data.EmbeddedAudio.Count))
            {
                sound.AudioFile = Data.EmbeddedAudio[sound.AudioID];
            }
            sound.GroupID = 0;
        }
        Data.Options.Constants.Clear();
    }
    IList<UndertaleRoom> rooms = Data.Rooms;
    IList<UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>> roomOrder = Data.GeneralInfo.RoomOrder;
    roomOrder.Clear();
    foreach(var room in rooms)
        roomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room });
    Data.Options.ShaderExtensionFlag = 2147483648;
    Data.Options.ShaderExtensionVersion = 2;
    String[] order = {"GEN8", "OPTN", "EXTN", "SOND", "AGRP", "SPRT", "BGND", "PATH", "SCPT", "SHDR", "FONT", "TMLN", "OBJT", "ROOM", "DAFL", "TPAG", "CODE", "VARI", "FUNC", "STRG", "TXTR", "AUDO"};
    Dictionary<string, UndertaleChunk> newChunks = new Dictionary<string, UndertaleChunk>();
    foreach (String name in order)
    {
        if (Data.FORM.Chunks[name] != null)
            newChunks[name] = Data.FORM.Chunks[name];
    }
    Data.FORM.Chunks = newChunks;
    Data.GeneralInfo.BytecodeVersion = 14;
    ScriptMessage("Upgraded from 13 to 14 successfully. Save the game to apply the changes.");
}
else
{
    ScriptError("Not bytecode 13.", "Error");
    return;
}
