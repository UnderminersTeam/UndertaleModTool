// By Grossley

using System;
using System.ComponentModel;
using System.IO;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using UndertaleModLib.Models;
using System.Collections.Generic;

EnsureDataLoaded();

if ((Data.AudioGroups.ByName("audiogroup_default") == null) && Data.GeneralInfo.Major >= 2)
{
    throw new ScriptException("Currently loaded data file has no \"audiogroup_default\" but it is GMS2 or greater. AudioGroups count: " + Data.AudioGroups.Count.ToString());
}
if (Data.IsVersionAtLeast(2024, 14))
{
    ScriptWarning("This script may act erroneously on GameMaker version 2024.14 and later.");
}
List<string> splitStringsList = GetSplitStringsList("sound");
List<UndertaleSound> soundsList = GetSoundsList(splitStringsList);
foreach (UndertaleSound snd in soundsList)
{
    UndertaleSound donorSND = Data.Sounds.ByName(snd.Name.Content);
    UndertaleSound nativeSND = new UndertaleSound();
    string newName = "";
    if (donorSND.Name != null)
    {
        newName = donorSND.Name.Content + "_Copy";
        nativeSND.Name = Data.Strings.MakeString(newName);
    }
    Data.Sounds.Add(nativeSND);
    if (donorSND.Type != null)
        nativeSND.Type = Data.Strings.MakeString(donorSND.Type.Content);
    if (donorSND.File != null)
        nativeSND.File = Data.Strings.MakeString(donorSND.File.Content.Replace(donorSND.Name.Content, newName));
    nativeSND.Flags = donorSND.Flags;
    nativeSND.Effects = donorSND.Effects;
    nativeSND.Volume = donorSND.Volume;
    nativeSND.Preload = donorSND.Preload;
    nativeSND.Pitch = donorSND.Pitch;
    HandleAudioGroups(donorSND, nativeSND);
    byte[] donorSoundData = GetSoundData(donorSND);
    if (donorSoundData != null)
    {
        UndertaleEmbeddedAudio undertaleEmbeddedAudio = new UndertaleEmbeddedAudio();
        undertaleEmbeddedAudio.Data = donorSoundData;
        undertaleEmbeddedAudio.Name = new UndertaleString("EmbeddedSound " + Data.EmbeddedAudio.Count.ToString() + " (UndertaleEmbeddedAudio)");
        int audioID;
        if (nativeSND.AudioGroup == null || nativeSND.AudioGroup.Name.Content == "audiogroup_default" || (!Data.FORM.Chunks.ContainsKey("AGRP")))
        {
            audioID = Data.EmbeddedAudio.Count;
            Data.EmbeddedAudio.Add(undertaleEmbeddedAudio);
            nativeSND.GroupID = Data.AudioGroups.IndexOf(nativeSND.AudioGroup);
            if (nativeSND.AudioGroup == null)
                nativeSND.GroupID = Data.GetBuiltinSoundGroupID();
        }
        else
        {
            int audioGroupID = Data.AudioGroups.IndexOf(donorSND.AudioGroup);
            var audioGroupReadStream =
            (
                new FileStream(Path.Combine(GetFolder(FilePath), "audiogroup" + audioGroupID.ToString() + ".dat"), FileMode.Open, FileAccess.Read)
            ); // Load the audiogroup dat into memory
            UndertaleData audioGroupDat = UndertaleIO.Read(audioGroupReadStream); // Load as UndertaleData
            audioGroupReadStream.Dispose();
            audioGroupDat.EmbeddedAudio.Add(undertaleEmbeddedAudio); // Adds the embeddedaudio entry to the dat data in memory
            audioID = audioGroupDat.EmbeddedAudio.Count - 1;
            var audioGroupWriteStream =
            (
                new FileStream(Path.Combine(GetFolder(FilePath), "audiogroup" + audioGroupID.ToString() + ".dat"), FileMode.Create)
            );
            UndertaleIO.Write(audioGroupWriteStream, audioGroupDat); // Write it to the disk
            audioGroupWriteStream.Dispose();
            nativeSND.GroupID = audioGroupID;
        }
        nativeSND.AudioID = audioID;
        nativeSND.AudioFile = undertaleEmbeddedAudio;
    }
    else
    {
        string source = Path.Combine(GetFolder(FilePath), donorSND.Name.Content + ".ogg");
        string dest = Path.Combine(GetFolder(FilePath), nativeSND.Name.Content + ".ogg");
        string backupLocation = Path.Combine(GetFolder(FilePath), "OldExternalSounds_" + DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss"));
        if (File.Exists(dest))
        {
            Directory.CreateDirectory(backupLocation);
            File.Copy(dest, Path.Combine(backupLocation, nativeSND.Name.Content + ".ogg"));
            File.Delete(dest);
        }
        if (File.Exists(source))
        {
            File.Copy(source, dest, false);
        }
        else
        {
            ScriptError("Cannot copy " + donorSND.Name.Content + ".ogg as it does not exist!");
        }
    }
}

void ProcessAllSoundsToUseAudioGroupDefault()
{
    var newAudioGroup = new UndertaleAudioGroup();
    newAudioGroup.Name = Data.Strings.MakeString("audiogroup_default");
    Data.AudioGroups.Add(newAudioGroup);
    for (var i = 0; i < Data.Sounds.Count; i++)
    {
        Data.Sounds[i].AudioGroup = newAudioGroup;
    }
}

void HandleAudioGroups(UndertaleSound donorSND, UndertaleSound nativeSND)
{
    if (!Data.FORM.Chunks.ContainsKey("AGRP")) // No way to add
    {
        return;
    }
    if (donorSND.AudioGroup != null)
    {
        UndertaleAudioGroup audoToGive = Data.AudioGroups.ByName(donorSND.AudioGroup.Name.Content);
        if (audoToGive == null)
        {
            if (!(donorSND.AudioGroup.Name.Content == "audiogroup_default"))
            {
                if (Data.AudioGroups.ByName("audiogroup_default") == null && Data.AudioGroups.Count == 0)
                {
                    if (ScriptQuestion("You are trying to add a non-default audio group but no audiogroups exist yet. Move all sounds into the default audio group and create a new audio group?"))
                        ProcessAllSoundsToUseAudioGroupDefault();
                    else
                        return;
                }
                else if (Data.AudioGroups.ByName("audiogroup_default") == null)
                {
                    throw new ScriptException("Count is non-zero but audiogroup_default does not exist.");
                }
                File.WriteAllBytes(Path.Combine(GetFolder(FilePath), "audiogroup" + Data.AudioGroups.Count.ToString() + ".dat"), Convert.FromBase64String("Rk9STQwAAABBVURPBAAAAAAAAAA="));
            }
            if (!(donorSND.AudioGroup.Name.Content == "audiogroup_default" && Data.AudioGroups.ByName("audiogroup_default") == null))
            {
                var newAudioGroup = new UndertaleAudioGroup();
                newAudioGroup.Name = Data.Strings.MakeString(donorSND.AudioGroup.Name.Content);
                Data.AudioGroups.Add(newAudioGroup);
                nativeSND.AudioGroup = newAudioGroup;
            }
        }
        else
            nativeSND.AudioGroup = audoToGive;
    }
    return;
}

List<string> GetSplitStringsList(string assetType)
{
    ScriptMessage("Enter the " + assetType + "(s) to copy");
    List<string> splitStringsList = new List<string>();
    string InputtedText = "";
    InputtedText = SimpleTextInput("Menu", "Enter the name(s) of the " + assetType + "(s)", InputtedText, true);
    string[] IndividualLineArray = InputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    foreach (var OneLine in IndividualLineArray)
    {
        splitStringsList.Add(OneLine.Trim());
    }
    return splitStringsList;
}

List<UndertaleSound> GetSoundsList(List<string> splitStringsList)
{
    List<UndertaleSound> soundsList = new List<UndertaleSound>();
    for (var j = 0; j < splitStringsList.Count; j++)
    {
        foreach (UndertaleSound snd in Data.Sounds)
        {
            if (snd is null)
                continue;
            if (splitStringsList[j].ToLower() == snd.Name.Content.ToLower())
            {
                soundsList.Add(snd);
            }
        }
    }
    return soundsList;
}
string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

byte[] GetSoundData(UndertaleSound sound, UndertaleData dataToOperateOn = null, string winFolder = null)
{
    if (dataToOperateOn == null)
        dataToOperateOn = Data;
    if (winFolder == null)
        winFolder = GetFolder(FilePath);
    if (sound.AudioFile != null)
        return sound.AudioFile.Data;
    if (sound.GroupID > dataToOperateOn.GetBuiltinSoundGroupID())
    {
        IList<UndertaleEmbeddedAudio> audioGroup = GetAudioGroupData(sound, winFolder);
        if (audioGroup != null)
            return audioGroup[sound.AudioID].Data;
    }
    return null;
}

IList<UndertaleEmbeddedAudio> GetAudioGroupData(UndertaleSound sound, string winFolder)
{
    Dictionary<string, IList<UndertaleEmbeddedAudio>> loadedAudioGroups = null;
    if (loadedAudioGroups == null)
        loadedAudioGroups = new Dictionary<string, IList<UndertaleEmbeddedAudio>>();
    string audioGroupName = sound.AudioGroup != null ? sound.AudioGroup.Name.Content : null;
    if (loadedAudioGroups.ContainsKey(audioGroupName))
        return loadedAudioGroups[audioGroupName];
    string groupFilePath = Path.Combine(winFolder, "audiogroup" + sound.GroupID + ".dat");
    if (!File.Exists(groupFilePath))
        return null; // Doesn't exist.
    try
    {
        UndertaleData data = null;
        using (var stream = new FileStream(groupFilePath, FileMode.Open, FileAccess.Read))
            data = UndertaleIO.Read(stream, (warning, _) => ScriptMessage("A warning occured while trying to load " + audioGroupName + ":\n" + warning));

        loadedAudioGroups[audioGroupName] = data.EmbeddedAudio;
        return data.EmbeddedAudio;
    }
    catch (Exception e)
    {
        ScriptMessage("An error occured while trying to load " + audioGroupName + ":\n" + e.Message);
        return null;
    }
}
