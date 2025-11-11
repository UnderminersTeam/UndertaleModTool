if (Data?.GeneralInfo.BytecodeVersion == 16)
{
    if (!ScriptQuestion("Upgrade bytecode from 16 to 17?"))
    {
        ScriptMessage("Cancelled.");
        return;
    }
    Data.GeneralInfo.BytecodeVersion = 17;
    if (!Data.IsVersionAtLeast(2, 2, 2, 302))
        Data.SetGMS2Version(2, 2, 2, 302);
    Data.FORM.Chunks["TGIN"] = new UndertaleChunkTGIN();
    String[] order = {"GEN8", "OPTN", "LANG", "EXTN", "SOND", "AGRP", "SPRT", "BGND", "PATH", "SCPT", "GLOB", "SHDR", "FONT", "TMLN", "OBJT", "ROOM", "DAFL", "EMBI", "TPAG", "TGIN", "CODE", "VARI", "FUNC", "STRG", "TXTR", "AUDO"};
    Dictionary<string, UndertaleChunk> newChunks = new Dictionary<string, UndertaleChunk>();
    foreach (String name in order)
    {
        if (Data.FORM.Chunks.ContainsKey(name))
            newChunks[name] = Data.FORM.Chunks[name];
    }
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
    
    // Fix background/tileset tile data if corrupted
    foreach (var bg in Data.Backgrounds)
    {
        if (bg.GMS2TileWidth > 0 && bg.GMS2TileHeight > 0 && bg.GMS2TileCount > 0)
        {
            int expectedLength = (int)bg.GMS2TileCount * (int)bg.GMS2ItemsPerTileCount;
            
            // Resize the list to match expected length
            while (bg.GMS2TileIds.Count < expectedLength)
            {
                bg.GMS2TileIds.Add(new UndertaleBackground.TileID() { ID = 0 });
            }
            while (bg.GMS2TileIds.Count > expectedLength)
            {
                bg.GMS2TileIds.RemoveAt(bg.GMS2TileIds.Count - 1);
            }
        }
    }
    
    ScriptMessage("Upgraded from 16 to 17 successfully. This game can be run on any runner newer than GMS 2.2.2.302 but older than GMS 2.3. Save the game to apply the changes.");
}
