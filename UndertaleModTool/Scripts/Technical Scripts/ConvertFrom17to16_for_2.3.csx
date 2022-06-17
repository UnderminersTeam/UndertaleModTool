using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

//For use playing the Switch version of Deltarune on PC with the regular Deltarune runner - by Grossley

EnsureDataLoaded();

string game_name = Data.GeneralInfo.Name.Content;

if ((Data.GMS2_3 == false) && (Data.GMS2_3_1 == false) && (Data.GMS2_3_2 == false))
{
    ScriptError(game_name + "is not GMS 2.3+ and is ineligible", "Ineligible");
    return;
}
else
{
    ScriptMessage("Current status of game '" + game_name + "':\r\nGMS 2.3 == " + Data.GMS2_3.ToString() + "\r\n" + "GMS 2.3.1 == " + Data.GMS2_3_1.ToString() + "\r\n" + "GMS 2.3.2 == " + Data.GMS2_3_2.ToString());
}

if (Data?.GeneralInfo.BytecodeVersion >= 17)
{
    string game_name_message = @"Experimental GMS 2.3 to GMS 2.0 converter

Made by Grossley#2869

Designed for the Zelda CDi remake versions on 
https://archive.org/details/FOER-and-WOGR
The current game is: 
";
string error_message = @"

This script may fail or the game may become unplayable!
You accept all risks if you continue past this point.
Do you wish to proceed anyways?

Select 'Yes' to continue
Select 'No' to abort (cancel) the script without
applying any changes to the game.";
//Now that I think about it, this isn't nearly stable enough to skip the warning for any game, even for FOER and WOGR
/*
    if ((game_name == "FOER") || (game_name == "WOGR"))
    {
        ScriptMessage(game_name_message + game_name);
    }
    else
    {
*/
    if (!(ScriptQuestion(game_name_message + game_name + error_message)))
    {
        ScriptError("Aborted!", "Aborted!");
        return;
    }
    //}
    if (!ScriptQuestion("Ran ExportAndConvert_2_3_ASM.csx and exported all GML first?"))
    {
        ScriptMessage("Run ExportAndConvert_2_3_ASM.csx and export all GML first. Script cancelled.");
        return;
    }
    if (!ScriptQuestion("Downgrade bytecode from 17 to 16?"))
    {
        ScriptMessage("Cancelled.");
        return;
    }
    Data.GeneralInfo.BytecodeVersion = 16;
    if (Data.FORM.Chunks.ContainsKey("TGIN"))
        Data.FORM.Chunks.Remove("TGIN");
    if (Data.FORM.Chunks.ContainsKey("ACRV"))
        Data.FORM.Chunks.Remove("ACRV");
    if (Data.FORM.Chunks.ContainsKey("SEQN"))
        Data.FORM.Chunks.Remove("SEQN");
    if (Data.FORM.Chunks.ContainsKey("TAGS"))
        Data.FORM.Chunks.Remove("TAGS");
    if (Data.FORM.Chunks.ContainsKey("EMBI"))
        Data.FORM.Chunks.Remove("EMBI");
    Data.GMS2_2_2_302 = false;
    Data.GMS2_3 = false;
    Data.GMS2_3_1 = false;
    Data.GMS2_3_2 = false;
    //Data.IsTPAG4ByteAligned = false;
    for (int i = 0; i < Data.Code.Count; i++)
    {
        UndertaleCode code = Data.Code[i];
        if (code.Name.Content.Contains("gml_Script_"))
        {
            //code.Name = MakeString(code.Name.Content);
            code.Name = MakeString(code.Name.Content, false);
        }
        Data.CodeLocals.ByName(code.Name.Content).Name = code.Name;
    }
    for (int i = 0; i < Data.Scripts.Count; i++)
    {
        UndertaleScript script = Data.Scripts[i];
        if (script.Name.Content.Contains("gml_Script_"))
        {
            UndertaleScript scr_dup = Data.Scripts.ByName(script.Name.Content.Replace("gml_Script_", ""));
            if (scr_dup != null)
            {
                UndertaleCode scr_dup_code = scr_dup.Code;
                if (scr_dup_code != null)
                {
                    UndertaleString scr_dup_code_name = scr_dup_code.Name;
                    if (scr_dup_code_name != null)
                    {
                        string scr_dup_code_name_con = scr_dup_code_name.Content;
                        UndertaleGlobalInit init_entry = null;
                        //This doesn't work, have to do it the hard way: UndertaleGlobalInit init_entry = Data.GlobalInitScripts.ByName(scr_dup_code_name_con);
                        foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
                        {
                            if (globalInit.Code.Name.Content == scr_dup_code_name_con)
                            {
                                init_entry = globalInit;
                                break;
                            }
                        }
                        if (init_entry != null)
                        {
                            Data.GlobalInitScripts.Remove(init_entry);
                        }
                        UndertaleCodeLocals local = Data.CodeLocals.ByName(scr_dup_code_name_con);
                        if (local != null)
                        {
                            Data.CodeLocals.Remove(local);
                        }
                        Data.Strings.Remove(scr_dup_code_name);
                    }
                    Data.Code.Remove(scr_dup_code);
                }
                Data.Scripts.Remove(scr_dup);
                i -= 5;
                if (i < 0)
                    i = -1;
                continue;
            }
            script.Name.Content = script.Name.Content.Replace("gml_Script_", "");
        }
    }
    foreach (UndertaleFunction func in Data.Functions)
    {
        func.GMS2_3 = false;
    }
    foreach (UndertaleCode code in Data.Code)
    {
        code.Offset = 0;
    }
    foreach (UndertaleCodeLocals local in Data.CodeLocals)
    {
        uint newIndex = 0;
        foreach (UndertaleCodeLocals.LocalVar localvar in local.Locals)
        {
            localvar.Index = newIndex;
            newIndex += 1;
        }
    }
    for (int i = 0; i < Data.Functions.Count; i++)
    {
        UndertaleFunction func = Data.Functions[i];
        if (Data.Code.ByName(func.Name.Content) != null)
        {
            Data.Functions.Remove(func);
            i -= 5;
            if (i < 0)
                i = -1;
            continue;
        }
    }
    //foreach(UndertaleRoom room in Data.Rooms)
    //{
    //    room.Flags = (UndertaleRoom.RoomEntryFlags)131072;
    //}
    //Convert flags faithfully
    foreach(UndertaleRoom room in Data.Rooms)
    {
        string binary = Convert.ToString((uint)room.Flags, 2);
        StringBuilder sb = new StringBuilder(binary);
        if (Data.GeneralInfo.Major == 1)
        {
            //if GMS1, also remove the GMS2 identifier flag
            sb[0] = '0';
        }
        //remove the GMS2.3 identifier flag
        sb[1] = '0';
        binary = sb.ToString();
        uint NewFlagsValue = Convert.ToUInt32(binary, 2);
        room.Flags = (UndertaleRoom.RoomEntryFlags)NewFlagsValue;
    }
    foreach(UndertaleSprite spr in Data.Sprites)
    {
        spr.SVersion = 1;
    }
    foreach(UndertaleEmbeddedTexture et in Data.EmbeddedTextures)
    {
        et.Scaled = 1;
        et.GeneratedMips = 0;
    }
    string res = "";
    string x = "";
    List<string> currentList = new List<string>();

    for (int i = 0; i < Data.GeneralInfo.RoomOrder.Count; i++)
    {
        x = Data.GeneralInfo.RoomOrder[i].ToString();
        string stringBeforeChar = x.Substring(0, x.IndexOf(" "));
        res += (stringBeforeChar + "\n");
        currentList.Add(stringBeforeChar);
    }
    Reorganize<UndertaleRoom>(Data.Rooms, currentList);

    Data.GMLCache?.Clear();
    Data.GMLCacheChanged?.Clear();
    Data.GMLCacheFailed?.Clear();
    Data.GMLEditedBefore?.Clear();
    Data.GMLCacheWasSaved = false;

    ScriptMessage("Downgraded from GMS 2.3 to 16 successfully. Save the game to apply the changes.");
}
else
{
    ScriptError("This is already bytecode 16 or lower.", "Error");
    return;
}

public UndertaleString MakeString(string content, bool doDuplicateCheck = true)
{
    if (content == null)
        throw new ArgumentNullException("content");
    // TODO: without reference counting the strings, this may leave unused strings in the array
    if (doDuplicateCheck)
    {
        foreach (UndertaleString str in Data.Strings)
        {
            if (str.Content == content)
            {
                return str;
            }
        }
    }
    UndertaleString newString = new UndertaleString(content);
    Data.Strings.Add(newString);
    return newString;
}

void Reorganize<T>(IList<T> list, List<string> order) where T : UndertaleNamedResource, new()
{
    Dictionary<string, T> temp = new Dictionary<string, T>();
    for (int i = 0; i < list.Count; i++)
    {
        T asset = list[i];
        string assetName = asset.Name?.Content;
        if (order.Contains(assetName))
        {
            temp[assetName] = asset;
        }
    }
    
    List<T> addOrder = new List<T>();
    for (int i = order.Count - 1; i >= 0; i--)
    {
        T asset;
        try
        {
            asset = temp[order[i]];
        } catch (Exception e)
        {
            throw new ScriptException("Missing asset with name \"" + order[i] + "\"");
        }
        addOrder.Add(asset);
    }
    
    foreach (T asset in addOrder)
        list.Remove(asset);
    foreach (T asset in addOrder)
        list.Insert(0, asset);
}

