// Made by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string output = "";
int progress = 0;
bool clearStrings = ScriptQuestion("Clear unused strings?");
string exportFolder = PromptChooseDirectory("Write unused strings log file to");
if (exportFolder == null)
{
    ScriptError("The location of the unused strings log file was not set.");
    return;
}

//Overwrite Check One
if (File.Exists(Path.Combine(exportFolder, "unused_strings_log.txt")))
{
    bool overwriteCheckOne = ScriptQuestion(@"An 'unused_strings_log.txt' file already exists. 
Would you like to overwrite it?");
    if (overwriteCheckOne)
    {
        File.Delete(exportFolder + "unused_strings_log.txt");
    }
    else
    {
        ScriptError("An 'unused_strings_log.txt' file already exists. Please remove it and try again.");
        return;
    }
}

RemoveUnusedVariFunc();

int stringsCount = Data.Strings.Count;
uint[] stringsUsageCountArray = new uint[stringsCount];
bool[] stringsUsageMap = new bool[stringsCount];

stringsUsageCountArray = GetStringUsageCount();

progress = 0;
int found_count = 0;
int removed_count = 0;
for (var i = (stringsUsageCountArray.Length - 1); i >= 0; i--)
{
    if ((i % 1000) == 999)
    {
        UpdateProgress("Generating output", stringsCount);
        progress += 999;
    }
    if (stringsUsageCountArray[i] == 0)
    {
        stringsUsageMap[i] = false;
        output += ("Data.Strings[" + i.ToString() + "] Exists = " + stringsUsageMap[i].ToString() + ";\r\n" + "Data.Strings[" + i.ToString() + "] UsageCount = " + stringsUsageCountArray[i].ToString() + "; // Data.Strings[" + i.ToString() + "].Content = \"" + Data.Strings[i].Content + "\";\r\n");
        found_count += 1;
        if (clearStrings)
        {
            Data.Strings.Remove(Data.Strings[i]);
            removed_count += 1;
        }
    }
}
File.WriteAllText(Path.Combine(exportFolder, "unused_strings_log.txt"), output);
HideProgressBar();
ScriptMessage("Complete. " + found_count.ToString() + " unused strings were found and " + removed_count.ToString() + " were removed. Further details have been written to the log at " + Path.Combine(exportFolder, "unused_strings_log.txt"));
return;

uint[] GetStringUsageCount()
{
    for (var i = 0; i < stringsCount; i++)
    {
        stringsUsageCountArray[i] = 0;
    }
    for (var i = 0; i < stringsCount; i++)
    {
        stringsUsageMap[i] = true;
    }
    UpdateProgress("Checking strings in AnimationCurves");
    if (Data.AnimationCurves != null)
    {
        foreach (UndertaleAnimationCurve obj in Data.AnimationCurves)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            foreach (UndertaleAnimationCurve.Channel chan in obj.Channels)
            {
                if (chan.Name != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(chan.Name)] += 1;
                }
            }
        }
    }
    UpdateProgress("Checking strings in AudioGroups");
    if (Data.AudioGroups != null)
    {
        foreach (UndertaleAudioGroup obj in Data.AudioGroups)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Backgrounds");
    if (Data.Backgrounds != null)
    {
        foreach (UndertaleBackground obj in Data.Backgrounds)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Code");
    if (Data.Code != null)
    {
        foreach (UndertaleCode obj in Data.Code)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            foreach (UndertaleInstruction Instruction in obj.Instructions)
            {
                if ((int)Instruction.Kind == 0xC0 && (int)Instruction.Type1 == 6)
                {
                    if (((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)Instruction.Value).Resource != null)
                    {
                        if ((((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)Instruction.Value).Resource) != null)
                            stringsUsageCountArray[Data.Strings.IndexOf(((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)Instruction.Value).Resource)] += 1;
                    }
                }
            }
        }
    }
    UpdateProgress("Checking strings in Code Locals");
    if (Data.CodeLocals != null)
    {
        foreach (UndertaleCodeLocals obj in Data.CodeLocals)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            foreach (UndertaleCodeLocals.LocalVar locvar in obj.Locals)
            {
                if (locvar.Name != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(locvar.Name)] += 1;
                }
            }
        }
    }
    UpdateProgress("Checking strings in Extensions");
    if (Data.Extensions != null)
    {
        foreach (UndertaleExtension obj in Data.Extensions)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            if (obj.FolderName != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.FolderName)] += 1;
            }
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            if (obj.ClassName != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.ClassName)] += 1;
            }
            foreach (UndertaleExtensionFile exFile in obj.Files)
            {
                if (exFile.Filename != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(exFile.Filename)] += 1;
                }
                if (exFile.CleanupScript != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(exFile.CleanupScript)] += 1;
                }
                if (exFile.InitScript != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(exFile.InitScript)] += 1;
                }
                foreach (UndertaleExtensionFunction exFunc in exFile.Functions)
                {
                    if (exFunc.Name != null)
                    {
                        stringsUsageCountArray[Data.Strings.IndexOf(exFunc.Name)] += 1;
                    }
                    if (exFunc.ExtName != null)
                    {
                        stringsUsageCountArray[Data.Strings.IndexOf(exFunc.ExtName)] += 1;
                    }
                }
            }
        }
    }
    UpdateProgress("Checking strings in Fonts");
    if (Data.Fonts != null)
    {
        foreach (UndertaleFont obj in Data.Fonts)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            if (obj.DisplayName != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.DisplayName)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Functions");
    if (Data.Functions != null)
    {
        foreach (UndertaleFunction obj in Data.Functions)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in GameObjects");
    if (Data.GameObjects != null)
    {
        foreach (UndertaleGameObject obj in Data.GameObjects)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            try
            {
                for (var i = 0; i < obj.Events.Count; i++)
                {
                    foreach (UndertaleGameObject.Event evnt in obj.Events[i])
                    {
                        foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                        {
                            if (action.ActionName != null)
                            {
                                stringsUsageCountArray[Data.Strings.IndexOf(action.ActionName)] += 1;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Something went wrong, but probably because it's trying to check something non-existent
                // Just keep going
            }
        }
    }
    UpdateProgress("Checking strings in GeneralInfo");
    if (Data.GeneralInfo != null)
    {
        if (Data.GeneralInfo.Filename != null)
        {
            stringsUsageCountArray[Data.Strings.IndexOf(Data.GeneralInfo.Filename)] += 1;
        }
        if (Data.GeneralInfo.Config != null)
        {
            stringsUsageCountArray[Data.Strings.IndexOf(Data.GeneralInfo.Config)] += 1;
        }
        if (Data.GeneralInfo.Name != null)
        {
            stringsUsageCountArray[Data.Strings.IndexOf(Data.GeneralInfo.Name)] += 1;
        }
        if (Data.GeneralInfo.DisplayName != null)
        {
            stringsUsageCountArray[Data.Strings.IndexOf(Data.GeneralInfo.DisplayName)] += 1;
        }
    }
    UpdateProgress("Checking strings in LANG");
    if (Data.FORM.LANG != null)
    {
        for (var i = 0; i < Data.FORM.LANG.Object.EntryIDs.Count; i++)
        {
            if (Data.FORM.LANG.Object.EntryIDs[i] != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(Data.FORM.LANG.Object.EntryIDs[i])] += 1;
            }
        }
        for (var i = 0; i < Data.FORM.LANG.Object.Languages.Count; i++)
        {
            if (Data.FORM.LANG.Object.Languages[i].Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(Data.FORM.LANG.Object.Languages[i].Name)] += 1;
            }
            if (Data.FORM.LANG.Object.Languages[i].Region != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(Data.FORM.LANG.Object.Languages[i].Region)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Options");
    if (Data.Options != null)
    {
        foreach (UndertaleOptions.Constant constant in Data.Options.Constants)
        {
            if (constant.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(constant.Name)] += 1;
            }
            if (constant.Value != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(constant.Value)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Paths");
    if (Data.Paths != null)
    {
        foreach (UndertalePath obj in Data.Paths)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Rooms");
    if (Data.Rooms != null)
    {
        foreach (UndertaleRoom obj in Data.Rooms)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            if (obj.Caption != null)
            {
                if (obj.Caption != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(obj.Caption)] += 1;
                }
            }
            for (var i = 0; i < obj.Layers.Count; i++)
            {
                if (obj.Layers[i].LayerName != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(obj.Layers[i].LayerName)] += 1;
                }
                if (obj.Layers[i].AssetsData != null)
                {
                    if (obj.Layers[i].AssetsData.Sprites != null)
                    {
                        for (var j = 0; j < obj.Layers[i].AssetsData.Sprites.Count; j++)
                        {
                            if (obj.Layers[i].AssetsData.Sprites[j].Name != null)
                            {
                                stringsUsageCountArray[Data.Strings.IndexOf(obj.Layers[i].AssetsData.Sprites[j].Name)] += 1;
                            }
                        }
                    }
                    if (obj.Layers[i].AssetsData.Sequences != null)
                    {
                        for (var j = 0; j < obj.Layers[i].AssetsData.Sequences.Count; j++)
                        {
                            if (obj.Layers[i].AssetsData.Sequences[j].Name != null)
                            {
                                stringsUsageCountArray[Data.Strings.IndexOf(obj.Layers[i].AssetsData.Sequences[j].Name)] += 1;
                            }
                        }
                    }
                    if (obj.Layers[i].AssetsData.NineSlices != null)
                    {
                        for (var j = 0; j < obj.Layers[i].AssetsData.NineSlices.Count; j++)
                        {
                            if (obj.Layers[i].AssetsData.NineSlices[j].Name != null)
                            {
                                stringsUsageCountArray[Data.Strings.IndexOf(obj.Layers[i].AssetsData.NineSlices[j].Name)] += 1;
                            }
                        }
                    }
                }
            }
        }
    }
    UpdateProgress("Checking strings in Scripts");
    if (Data.Scripts != null)
    {
        foreach (UndertaleScript obj in Data.Scripts)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Sequences");
    if (Data.Sequences != null)
    {
        foreach (UndertaleSequence obj in Data.Sequences)
        {
            if ((obj as UndertaleSequence).Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf((obj as UndertaleSequence).Name)] += 1;
            }
            for (var i = 0; i < obj.Moments.Count; i++)
            {
                for (var j = 0; j < obj.Moments[i].Channels.Count; j++)
                {
                    if (obj.Moments[i].Channels[j].Event != null)
                    {
                        stringsUsageCountArray[Data.Strings.IndexOf(obj.Moments[i].Channels[j].Event)] += 1;
                    }
                }
            }
            for (var i = 0; i < obj.Tracks.Count; i++)
            {
                obj.Tracks[i] = RecurseTracks(obj.Tracks[i]);
            }
        }
    }
    UpdateProgress("Checking strings in Shaders");
    if (Data.Shaders != null)
    {
        foreach (UndertaleShader obj in Data.Shaders)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            if (obj.GLSL_ES_Fragment != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.GLSL_ES_Fragment)] += 1;
            }
            if (obj.GLSL_ES_Vertex != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.GLSL_ES_Vertex)] += 1;
            }
            if (obj.GLSL_Fragment != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.GLSL_Fragment)] += 1;
            }
            if (obj.GLSL_Vertex != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.GLSL_Vertex)] += 1;
            }
            if (obj.HLSL9_Fragment != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.HLSL9_Fragment)] += 1;
            }
            if (obj.HLSL9_Vertex != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.HLSL9_Vertex)] += 1;
            }
            for (var i = 0; i < obj.VertexShaderAttributes.Count; i++)
            {
                if (obj.VertexShaderAttributes[i].Name != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf(obj.VertexShaderAttributes[i].Name)] += 1;
                }
            }
        }
    }
    UpdateProgress("Checking strings in Sounds");
    if (Data.Sounds != null)
    {
        foreach (UndertaleSound obj in Data.Sounds)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
            if (obj.Type != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Type)] += 1;
            }
            if (obj.File != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.File)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Sprites");
    if (Data.Sprites != null)
    {
        foreach (UndertaleSprite obj in Data.Sprites)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Texture Group Info");
    if (Data.TextureGroupInfo != null)
    {
        foreach (UndertaleTextureGroupInfo obj in Data.TextureGroupInfo)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Timelines");
    if (Data.Timelines != null)
    {
        foreach (UndertaleTimeline obj in Data.Timelines)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    UpdateProgress("Checking strings in Variables");
    if (Data.Variables != null)
    {
        foreach (UndertaleVariable obj in Data.Variables)
        {
            if (obj.Name != null)
            {
                stringsUsageCountArray[Data.Strings.IndexOf(obj.Name)] += 1;
            }
        }
    }
    return stringsUsageCountArray;
}
UndertaleSequence.Track RecurseTracks(UndertaleSequence.Track trackRecurse)
{
    if (trackRecurse.ModelName != null)
    {
        stringsUsageCountArray[Data.Strings.IndexOf(trackRecurse.ModelName)] += 1;
    }
    if (trackRecurse.Name != null)
    {
        stringsUsageCountArray[Data.Strings.IndexOf(trackRecurse.Name)] += 1;
    }
    if (trackRecurse.GMAnimCurveString != null)
    {
        stringsUsageCountArray[Data.Strings.IndexOf(trackRecurse.GMAnimCurveString)] += 1;
    }
    if ((trackRecurse.ModelName.Content) == "GMStringTrack")
    {
        for (var j = 0; j < (trackRecurse.Keyframes as UndertaleSequence.StringKeyframes).List.Count; j++)
        {
            for (var k = 0; k < (trackRecurse.Keyframes as UndertaleSequence.StringKeyframes).List[j].Channels.Count; k++)
            {
                if ((trackRecurse.Keyframes as UndertaleSequence.StringKeyframes).List[j].Channels[k].Value != null)
                {
                    stringsUsageCountArray[Data.Strings.IndexOf((trackRecurse.Keyframes as UndertaleSequence.StringKeyframes).List[j].Channels[k].Value)] += 1;
                }
            }
        }
    }
    for (var j = 0; j < trackRecurse.Tracks.Count; j++)
    {
        trackRecurse.Tracks[j] = RecurseTracks(trackRecurse.Tracks[j]);
    }
    UpdateProgress("RecurseTracks");
    return trackRecurse;
}
void RemoveUnusedVariFunc()
{
    Dictionary<UndertaleVariable, List<UndertaleInstruction>> references_vari = CollectReferencesVar();
    Dictionary<UndertaleFunction, List<UndertaleInstruction>> references_func = CollectReferencesFunc();
    uint test_variable = 0;
    uint test_func = 0;
    for (var i = 0; i < Data.Variables.Count; i++)
    {
        UndertaleVariable vari = Data.Variables[i];
        test_variable = references_vari.ContainsKey(vari) ? (uint)references_vari[vari].Count : 0;
        if ((test_variable == 0) && (vari.Name.Content != "arguments") && (vari.Name.Content != "prototype") && (vari.Name.Content != "@@array@@"))
        {
            output += "Data.Variables[" + i.ToString() + "].Occurrences = " + test_variable.ToString() + ";" + " // Data.Variables[" + i.ToString() + "].Name.Content = \"" + vari.Name.Content + "\";" + "\r\n";
            Data.Variables.Remove(vari);
        }
    }
    UpdateProgress("Removing Unused Variables");
    for (var i = 0; i < Data.Functions.Count; i++)
    {
        UndertaleFunction func = Data.Functions[i];
        test_func = references_func.ContainsKey(func) ? (uint)references_func[func].Count : 0;
        if (test_func == 0)
        {
            output += "Data.Functions[" + i.ToString() + "].Occurrences = " + test_func.ToString() + ";" + " // Data.Functions[" + i.ToString() + "].Name.Content = \"" + func.Name.Content + "\";" + "\r\n";
            Data.Functions.Remove(func);
        }
    }
    UpdateProgress("Removing Unused Functions");
}
Dictionary<UndertaleVariable, List<UndertaleInstruction>> CollectReferencesVar()
{
    Dictionary<UndertaleVariable, List<UndertaleInstruction>> list = new Dictionary<UndertaleVariable, List<UndertaleInstruction>>();
    UpdateProgress("Searching For Unused Variables");
    foreach (UndertaleCode code in Data.Code)
    {
        if (code.Offset != 0) // GMS 2.3, skip duplicates
            continue;
        foreach (UndertaleInstruction instr in code.Instructions)
        {
            UndertaleVariable obj = instr.GetReference<UndertaleVariable>()?.Target;
            if (obj != null)
            {
                if (!list.ContainsKey(obj))
                    list.Add(obj, new List<UndertaleInstruction>());
                list[obj].Add(instr);
            }
        }
    }
    return list;
}
Dictionary<UndertaleFunction, List<UndertaleInstruction>> CollectReferencesFunc()
{
    Dictionary<UndertaleFunction, List<UndertaleInstruction>> list = new Dictionary<UndertaleFunction, List<UndertaleInstruction>>();
    UpdateProgress("Searching For Unused Functions");
    foreach (UndertaleCode code in Data.Code)
    {
        if (code.Offset != 0) // GMS 2.3, skip duplicates
            continue;
        foreach (UndertaleInstruction instr in code.Instructions)
        {
            UndertaleFunction obj = instr.GetReference<UndertaleFunction>()?.Target;
            if (obj != null)
            {
                if (!list.ContainsKey(obj))
                    list.Add(obj, new List<UndertaleInstruction>());
                list[obj].Add(instr);
            }
        }
    }
    return list;
}
void UpdateProgress(string name, int limit = 1)
{
    UpdateProgressBar(null, name, progress++, limit);
}
