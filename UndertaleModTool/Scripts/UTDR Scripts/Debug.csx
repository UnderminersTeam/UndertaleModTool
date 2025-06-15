EnsureDataLoaded();

bool enable = ScriptQuestion("Enable debug mode?\n\n(No = disable debug mode, if enabled)");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data)
{
    ThrowOnNoOpFindReplace = true
};

string internalName = Data.GeneralInfo.Name.Content;
string displayName = Data.GeneralInfo.DisplayName.Content;
if (internalName == "NXTALE" || internalName.StartsWith("UNDERTALE"))
{
    // UNDERTALE
    if (Data.Scripts.ByName("SCR_GAMESTART", true)?.Code is UndertaleCode SCR_GAMESTART)
    {
        importGroup.QueueFindReplace(SCR_GAMESTART, "global.debug = ", $"global.debug = {(enable ? "1" : "0")}; //");
        importGroup.Import();
        ChangeSelection(SCR_GAMESTART);
    }
    else
    {
        ScriptError("Failed to find SCR_GAMESTART.");
        return;
    }
}
else if (!Data.IsVersionAtLeast(2, 3) && (displayName == "SURVEY_PROGRAM" || displayName == "DELTARUNE Chapter 1"))
{
    // DELTARUNE Chapter 1, before 1&2 demo
    if (Data.Scripts.ByName("scr_debug")?.Code is UndertaleCode scr_debug)
    {
        importGroup.QueueReplace(scr_debug, $"global.debug = {(enable ? "1" : "0")}; return global.debug;");
        importGroup.Import();
        ChangeSelection(scr_debug);
    }
    else
    {
        ScriptError("Failed to find scr_debug.");
        return;
    }
}
else if (displayName == "DELTARUNE Chapter 1&2")
{
    // DELATRUNE (1&2 demo prior to LTS)
    if (Data.Scripts.ByName("SCR_GAMESTART", true)?.Code is not UndertaleCode SCR_GAMESTART)
    {
        ScriptError("Failed to find SCR_GAMESTART.");
        return;
    }
    if (Data.Code.ByName("gml_Object_obj_debugcontroller_ch1_Create_0") is not UndertaleCode debugControllerCode)
    {
        ScriptError("Failed to find obj_debugcontroller_ch1 create event.");
        return;
    }
    if (Data.Code.ByName("gml_Object_obj_debugProfiler_Create_0") is not UndertaleCode debugProfilerCode)
    {
        ScriptError("Failed to find obj_debugProfiler create event.");
        return;
    }

    importGroup.QueueFindReplace(SCR_GAMESTART, "global.debug = ", $"global.debug = {(enable ? "1" : "0")}; //");
    importGroup.QueueFindReplace(debugControllerCode, "debug = ", $"debug = {(enable ? "true" : "false")}; //");
    if (debugProfilerCode.Instructions.Count == 0 && enable)
    {
        // 1.09+, debugProfiler blanked
        importGroup.QueueReplace(debugProfilerCode, "cutsceneshow = false;");
    }
    else if (debugProfilerCode.Instructions.Count == 2 && !enable)
    {
        importGroup.QueueReplace(debugProfilerCode, "");
    }
    importGroup.Import();
    ChangeSelection(SCR_GAMESTART);
}
else if (displayName.ToUpper().Contains("DELTARUNE"))
{
    if (Data.GameObjects.ByName("obj_event_manager") is null)
    {
        // DELTARUNE (1&2 LTS demo)
        if (displayName == "DELTARUNE Chapter 1")
        {
            if (Data.Code.ByName("gml_Object_obj_debugcontroller_Create_0") is not UndertaleCode debugControllerCode)
            {
                ScriptError("Failed to find obj_debugcontroller create event.");
                return;
            }

            importGroup.QueueFindReplace(debugControllerCode, "debug = ", $"debug = {(enable ? "true" : "false")}; //");
            importGroup.Import();
            ChangeSelection(debugControllerCode);
        }
        else if (displayName == "DELTARUNE Chapter 2")
        {
            if (Data.Scripts.ByName("scr_debug")?.Code is not UndertaleCode scr_debug)
            {
                ScriptError("Failed to find scr_debug.");
                return;
            }

            importGroup.QueueReplace(scr_debug, $"function scr_debug() {{ return {(enable ? "1" : "0")}; }}");
            importGroup.Import();
            ChangeSelection(scr_debug);
        }
        else
        {
            ScriptError("Unsupported game version.");
            return;
        }
    }
    else
    {
        // DELTARUNE (full release, handles all chapters)
        if (Data.Code.ByName("gml_Object_obj_initializer2_Create_0") is not UndertaleCode initializerCode)
        {
            ScriptError("Failed to find obj_initializer2 create event.");
            return;
        }

        if (enable && displayName == "DELTARUNE Chapter 3")
        {
            // Chapter 3 needs extra globals defined for debug to function correctly
            string existingCode = GetDecompiledText(initializerCode);
            if (!existingCode.Contains("global.chemg_show_room = ", StringComparison.Ordinal) ||
                !existingCode.Contains("global.chemg_show_val = ", StringComparison.Ordinal))
            {
                importGroup.QueueFindReplace(initializerCode, "global.debug = ",
                    "global.debug = 1; global.chemg_show_room = 0; global.chemg_show_val = 0; //");
            }
            else
            {
                importGroup.QueueFindReplace(initializerCode, "global.debug = ", "global.debug = 1; //");
            }
        }
        else
        {
            importGroup.QueueFindReplace(initializerCode, "global.debug = ", $"global.debug = {(enable ? "1" : "0")}; //");
        }

        if (Data.Code.ByName("gml_GlobalScript_scr_flag_get") is UndertaleCode flagGetCode &&
            Data.Code.ByName("gml_Script_scr_flag_name_get") is not null)
        {
            // Fix for chapters not including flag names
            importGroup.QueueFindReplace(flagGetCode,
                "var v = ",
                "var v = !variable_global_exists(\"flagname\") ? \"\" : global.flagname[arg0]; //");
        }

        importGroup.Import();
        ChangeSelection(initializerCode);
    }
}
else
{
    ScriptError("Unsupported game version.");
    return;
}

ScriptMessage($"Debug mode {(enable ? "enabled" : "disabled")}.");
