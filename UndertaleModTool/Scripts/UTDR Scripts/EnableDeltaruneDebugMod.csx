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
    
    var obj_initializer2 = Data.GameObjects.ByName("obj_initializer2");
    if (obj_initializer2 != null)
    {
        var initCode = obj_initializer2.EventHandlerFor(EventType.Create, (uint)0, Data);
        if (initCode != null)
        {
            importGroup.QueueFindReplace(initCode, "global.debug = 0;", "global.debug = 1;");
        }
    }

    var scr_board_objname = Data.Code.ByName("gml_GlobalScript_scr_board_objname");
    if (scr_board_objname != null)
    {
        ScriptMessage("Found gml_GlobalScript_scr_board_objname, replacing entire function");
        
        string newScr_board_objname = @"
function scr_board_objname()
{
    if (scr_debug())
    {
        if (1)
        {
            var __cx = board_tilex(12) - 2;
            var __cy = board_tiley(0);
            
            if (argument_count >= 1)
                __cx = argument0;
            
            if (argument_count >= 2)
                __cy = argument1;
            
            draw_set_halign(fa_right);
            draw_set_font(fnt_main);
            draw_set_color(c_aqua);
            draw_text_outline(__cx, __cy, string_copy(object_get_name(object_index), 5, 99));
            draw_set_font(fnt_small);
            draw_set_halign(fa_left);
            draw_set_color(c_white);
        }
    }
}

";
        
        importGroup.QueueReplace(scr_board_objname, newScr_board_objname);
    }
    else
    {
        ScriptMessage("Warning: Could not find scr_board_objname");
    }

    importGroup.Import();
    
    ScriptMessage("Debug mode and room display are now permanently enabled for DELTARUNE Chapter 3! Coded By Cyn-ically");
}
else if (displayName == "DELTARUNE Chapter 4")
{
    ScriptMessage("Detected DELTARUNE Chapter 4 - modifying multiple objects");
  

    
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
