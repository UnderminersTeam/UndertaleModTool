EnsureDataLoaded();

ScriptMessage("Enabling debug mode");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

string displayName = Data.GeneralInfo.DisplayName.Content;

if (displayName == "DELTARUNE Chapter 1")
{
    ScriptMessage("Detected DELTARUNE Chapter 1 - modifying obj_initializer2");
    
    var obj_initializer2 = Data.GameObjects.ByName("obj_initializer2");
    if (obj_initializer2 == null)
    {
        ScriptError("Could not find obj_initializer2");
        return;
    }

    var createCode = obj_initializer2.EventHandlerFor(EventType.Create, (uint)0, Data);
    if (createCode == null)
    {
        ScriptError("Could not find Create event for obj_initializer2");
        return;
    }

    importGroup.QueueFindReplace(createCode,
        "global.debug = 0;",
        "global.debug = 1;"
    );

    importGroup.Import();
    ChangeSelection(createCode);
    
    ScriptMessage("Debug mode is now permanently enabled for DELTARUNE Chapter 1! Coded By Cyn-ically");
}
else if (displayName == "DELTARUNE Chapter 2")
{
    ScriptMessage("Detected DELTARUNE Chapter 2 - modifying multiple objects");
    
    var obj_dialoguer = Data.GameObjects.ByName("obj_dialoguer");
    if (obj_dialoguer != null)
    {
        var dialoguerCode = obj_dialoguer.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (dialoguerCode != null)
        {
            importGroup.QueueFindReplace(dialoguerCode, "drawdebug = 0;", "drawdebug = 1;");
        }
    }
    
    var obj_custommenu = Data.GameObjects.ByName("obj_custommenu");
    if (obj_custommenu != null)
    {
        var custommenuCode = obj_custommenu.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (custommenuCode != null)
        {
            importGroup.QueueFindReplace(custommenuCode, "edgedebug = 0;", "edgedebug = 1;");
        }
    }
    
    var obj_queen_enemy = Data.GameObjects.ByName("obj_queen_enemy");
    if (obj_queen_enemy != null)
    {
        var queenCode = obj_queen_enemy.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (queenCode != null)
        {
            importGroup.QueueFindReplace(queenCode, "difficultydebug = 0;", "difficultydebug = 1;");
        }
    }
    
    var obj_queen_bulletcontroller = Data.GameObjects.ByName("obj_queen_bulletcontroller");
    if (obj_queen_bulletcontroller != null)
    {
        var bulletCode = obj_queen_bulletcontroller.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (bulletCode != null)
        {
            importGroup.QueueFindReplace(bulletCode, "winedebug = 0;", "winedebug = 1;");
        }
    }
    
    var obj_spamton_neo_enemy = Data.GameObjects.ByName("obj_spamton_neo_enemy");
    if (obj_spamton_neo_enemy != null)
    {
        var spamtonCode = obj_spamton_neo_enemy.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (spamtonCode != null)
        {
            importGroup.QueueFindReplace(spamtonCode, "musicdebug = 0;", "musicdebug = 1;");
        }
    }
    
    var obj_initializer2 = Data.GameObjects.ByName("obj_initializer2");
    if (obj_initializer2 != null)
    {
        var initCode = obj_initializer2.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (initCode != null)
        {
            importGroup.QueueFindReplace(initCode, "global.debug = 0;", "global.debug = 1;");
        }
    }

    importGroup.Import();
    ScriptMessage("Debug mode is now permanently enabled for DELTARUNE Chapter 2! Coded By Cyn-ically");
}
else if (displayName == "DELTARUNE Chapter 3")
{
    ScriptMessage("Detected DELTARUNE Chapter 3 - modifying multiple objects");
    
    var obj_dialoguer = Data.GameObjects.ByName("obj_dialoguer");
    if (obj_dialoguer != null)
    {
        var dialoguerCode = obj_dialoguer.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (dialoguerCode != null)
        {
            importGroup.QueueFindReplace(dialoguerCode, "drawdebug = 0;", "drawdebug = 1;");
        }
    }
    
    var obj_mainchara = Data.GameObjects.ByName("obj_mainchara");
    if (obj_mainchara != null)
    {
        var maincharaCode = obj_mainchara.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (maincharaCode != null)
        {
            importGroup.QueueFindReplace(maincharaCode, "drawdebug = 0;", "drawdebug = 1;");
        }
    }
    
    var obj_custommenu = Data.GameObjects.ByName("obj_custommenu");
    if (obj_custommenu != null)
    {
        var custommenuCode = obj_custommenu.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (custommenuCode != null)
        {
            importGroup.QueueFindReplace(custommenuCode, "edgedebug = 0;", "edgedebug = 1;");
        }
    }
    
    var obj_initializer2 = Data.GameObjects.ByName("obj_initializer2");
    if (obj_initializer2 != null)
    {
        var initCode = obj_initializer2.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (initCode != null)
        {
            importGroup.QueueFindReplace(initCode, "global.debug = 0;", "global.debug = 1;");
        }
    }

    importGroup.Import();
    ScriptMessage("Debug mode is now permanently enabled for DELTARUNE Chapter 3! Coded By Cyn-ically");
}
else if (displayName == "DELTARUNE Chapter 4")
{
    ScriptMessage("Detected DELTARUNE Chapter 4 - modifying multiple objects");
    
    var obj_dialoguer = Data.GameObjects.ByName("obj_dialoguer");
    if (obj_dialoguer != null)
    {
        var dialoguerCode = obj_dialoguer.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (dialoguerCode != null)
        {
            importGroup.QueueFindReplace(dialoguerCode, "drawdebug = 0;", "drawdebug = 1;");
        }
    }
    
    var obj_homealone_heart = Data.GameObjects.ByName("obj_homealone_heart");
    if (obj_homealone_heart != null)
    {
        var heartCode = obj_homealone_heart.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (heartCode != null)
        {
            importGroup.QueueFindReplace(heartCode, "drawdebug = 0;", "drawdebug = 1;");
        }
    }
    
    var obj_sound_of_justice_enemy = Data.GameObjects.ByName("obj_sound_of_justice_enemy");
    if (obj_sound_of_justice_enemy != null)
    {
        var soundCode = obj_sound_of_justice_enemy.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (soundCode != null)
        {
            importGroup.QueueFindReplace(soundCode, "musicdebug = 0;", "musicdebug = 1;");
        }
    }
    
    var obj_mainchara = Data.GameObjects.ByName("obj_mainchara");
    if (obj_mainchara != null)
    {
        var maincharaCode = obj_mainchara.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (maincharaCode != null)
        {
            importGroup.QueueFindReplace(maincharaCode, "drawdebug = 0;", "drawdebug = 1;");
        }
    }
    
    var obj_custommenu = Data.GameObjects.ByName("obj_custommenu");
    if (obj_custommenu != null)
    {
        var custommenuCode = obj_custommenu.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (custommenuCode != null)
        {
            importGroup.QueueFindReplace(custommenuCode, "edgedebug = 0;", "edgedebug = 1;");
        }
    }
    
    var obj_hammer_of_justice_enemy = Data.GameObjects.ByName("obj_hammer_of_justice_enemy");
    if (obj_hammer_of_justice_enemy != null)
    {
        var hammerCode = obj_hammer_of_justice_enemy.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (hammerCode != null)
        {
            importGroup.QueueFindReplace(hammerCode, "musicdebug = 0;", "musicdebug = 1;");
        }
    }
    
    var obj_initializer2 = Data.GameObjects.ByName("obj_initializer2");
    if (obj_initializer2 != null)
    {
        var initCode = obj_initializer2.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (initCode != null)
        {
            importGroup.QueueFindReplace(initCode, "global.debug = 0;", "global.debug = 1;");
        }
    }

    importGroup.Import();
    ScriptMessage("Debug mode is now permanently enabled for DELTARUNE Chapter 4! Coded By Cyn-ically");
}
else if (displayName == "DELTARUNE")
{
    ScriptMessage("Detected DELTARUNE - modifying obj_CHAPTER_SELECT");
    
    var obj_chapter_select = Data.GameObjects.ByName("obj_CHAPTER_SELECT");
    if (obj_chapter_select == null)
    {
        ScriptError("Could not find obj_CHAPTER_SELECT");
        return;
    }

    var createCode = obj_chapter_select.EventHandlerFor(EventType.Create, (uint)0, Data);
    if (createCode == null)
    {
        ScriptError("Could not find Create event for obj_CHAPTER_SELECT");
        return;
    }

    importGroup.QueueFindReplace(createCode,
        "global.debug = 0;",
        "global.debug = 1;"
    );

    importGroup.Import();
    ChangeSelection(createCode);
    
    ScriptMessage("Debug mode is now permanently enabled for DELTARUNE! Coded By Cyn-ically");
}
else
{
    ScriptError("Unsupported version how? Current game: " + displayName);
    return;
}
