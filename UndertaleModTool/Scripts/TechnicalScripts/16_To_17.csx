if (Data?.GeneralInfo.BytecodeVersion == 16)
{
    if (!ScriptQuestion("Upgrade bytecode from 16 to 17?"))
    {
        ScriptMessage("Cancelled.");
        return;
    }
    Data.GeneralInfo.BytecodeVersion = 17;
    Data.GMS2_2_2_302 = true;
    Data.FORM.Chunks["TGIN"] = new UndertaleChunkTGIN();
    String[] order = {"GEN8", "OPTN", "LANG", "EXTN", "SOND", "AGRP", "SPRT", "BGND", "PATH", "SCPT", "GLOB", "SHDR", "FONT", "TMLN", "OBJT", "ROOM", "DAFL", "EMBI", "TPAG", "TGIN", "CODE", "VARI", "FUNC", "STRG", "TXTR", "AUDO"};
    Dictionary<string, UndertaleChunk> newChunks = new Dictionary<string, UndertaleChunk>();
    foreach (String name in order)
        newChunks[name] = Data.FORM.Chunks[name];
    Data.FORM.Chunks = newChunks;
    UndertaleTextureGroupInfo tgin = new UndertaleTextureGroupInfo();
    tgin.Name = Data.Strings.MakeString("Default");
    for (var i = 0; i < Data.EmbeddedTextures.Count; i++)
    {
        tgin.TexturePages.Add(new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>() { Resource = Data.EmbeddedTextures[i] });
    }
    for (var i = 0; i < Data.Sprites.Count; i++)
    {
        tgin.Sprites.Add(new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>() { Resource = Data.Sprites[i] });
    }
    for (var i = 0; i < Data.Fonts.Count; i++)
    {
        tgin.Fonts.Add(new UndertaleResourceById<UndertaleFont, UndertaleChunkFONT>() { Resource = Data.Fonts[i] });
    }
    for (var i = 0; i < Data.Backgrounds.Count; i++)
    {
        tgin.Tilesets.Add(new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>() { Resource = Data.Backgrounds[i] });
    }
    Data.TextureGroupInfo.Add(tgin); 
    ScriptMessage("Upgraded from 16 to 17 successfully. This game can be run on any runner newer than GMS 2.2.2.302 but older than GMS 2.3. Save the game to apply the changes.");
}
