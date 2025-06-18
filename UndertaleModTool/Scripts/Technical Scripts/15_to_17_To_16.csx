//Upgrade from bytecode 13 (experimental), 14, 15, 17 to 16 - by Grossley
//13 and 14 do not work apparently due to variable issues that I don't know how to fix.
//Need to test this, once I do then I can obsolete the other two scripts
if (Data.IsVersionAtLeast(2, 3))
{
    bool x = RunUMTScript(Path.Combine(ExePath, "Scripts", "Technical Scripts", "ConvertFrom17to16_for_2.3.csx"));
    if (x == false)
        ScriptError("ConvertFrom17to16_for_2.3.csx failed!");
    return;
}

EnsureDataLoaded();

string currentBytecodeVersion = Data?.GeneralInfo.BytecodeVersion.ToString();
string game_name = Data.GeneralInfo.Name.Content;

bool is13 = false;

if (!(Data.FORM.Chunks.ContainsKey("AGRP")))
{
    /*    is13 = true;
        ScriptMessage("Bytecode 13 type game detected. The upgrading of this game is experimental.");
        currentBytecodeVersion = "13";*/
    ScriptError("Bytecode 13 is not supported.");
    return;
}
if (Data?.GeneralInfo.BytecodeVersion == 14)
{
    ScriptError("Bytecode 14 is not supported.");
    return;
}

if (Data.IsVersionAtLeast(2, 3))
{
    ScriptError(game_name + "is GMS 2.3+ and is ineligible", "Ineligible");
    return;
}

if (!(Data.FORM.Chunks.ContainsKey("AGRP")))
{
    is13 = true;
    ScriptMessage("Bytecode 13 type game detected. The upgrading of this game is experimental.");
    currentBytecodeVersion = "13";
}


if ((Data?.GeneralInfo.BytecodeVersion == 14) || (Data?.GeneralInfo.BytecodeVersion == 15) || (is13 == true))
{
    if (!ScriptQuestion("Upgrade bytecode from 13 (experimental), 14, or 15 to 16?\nCurrent bytecode: " + currentBytecodeVersion))
    {
        ScriptMessage("Cancelled.");
        return;
    }
    if (Data?.GeneralInfo.BytecodeVersion <= 14)
    {
        foreach (UndertaleCode code in Data.Code)
        {
            UndertaleCodeLocals locals = new UndertaleCodeLocals();
            locals.Name = code.Name;

            UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
            argsLocal.Name = Data.Strings.MakeString("arguments");
            argsLocal.Index = 0;

            locals.Locals.Add(argsLocal);

            code.LocalsCount = 1;
            Data.CodeLocals.Add(locals);
        }
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
        Data.GeneralInfo.Build = 1804;
        var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
        Data.FORM.EXTN.productIdData.Add(newProductID);
        Data.Options.Constants.Clear();
        Data.Options.Constants.Add(new UndertaleOptions.Constant() { Name = Data.Strings.MakeString("@@SleepMargin"), Value = Data.Strings.MakeString(1.ToString()) });
        Data.Options.Constants.Add(new UndertaleOptions.Constant() { Name = Data.Strings.MakeString("@@DrawColour"), Value = Data.Strings.MakeString(0xFFFFFFFF.ToString()) });
    }
    Data.FORM.Chunks["LANG"] = new UndertaleChunkLANG();
    Data.FORM.LANG.Object = new UndertaleLanguage();
    Data.FORM.Chunks["GLOB"] = new UndertaleChunkGLOB();
    String[] order = { "GEN8", "OPTN", "LANG", "EXTN", "SOND", "AGRP", "SPRT", "BGND", "PATH", "SCPT", "GLOB", "SHDR", "FONT", "TMLN", "OBJT", "ROOM", "DAFL", "TPAG", "CODE", "VARI", "FUNC", "STRG", "TXTR", "AUDO" };
    Dictionary<string, UndertaleChunk> newChunks = new Dictionary<string, UndertaleChunk>();
    foreach (String name in order)
        newChunks[name] = Data.FORM.Chunks[name];
    Data.FORM.Chunks = newChunks;
    Data.GeneralInfo.BytecodeVersion = 16;
    ScriptMessage("Upgraded from " + currentBytecodeVersion + " to 16 successfully. Save the game to apply the changes.");
}
else if (Data?.GeneralInfo.BytecodeVersion == 17)
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
else if (Data?.GeneralInfo.BytecodeVersion == 16)
{
    ScriptError("This is already bytecode 16.", "Error");
    return;
}
else
{
    string error = @"This game is not bytecode 13, 
14, 15, 16, or 17, and is not made in GameMaker 2.3
or greater. Please report this game to Grossley#2869
on Discord and provide the name of the game, where
you obtained it from, and additionally send the
data.win file of the game." + @"

Game and version: '" + Data.GeneralInfo.ToString() + "'";
    ScriptError(error, "Unknown game error");
    SetUMTConsoleText(error);
    SetFinishedMessage(false);
    return;
}
