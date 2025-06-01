// By Grossley

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

EnsureDataLoaded();

if (Data.IsVersionAtLeast(2024, 14))
{
    ScriptWarning("This script may act erroneously on GameMaker version 2024.14 and later.");
}
if (!ScriptQuestion("Remove unused sounds?"))
{
    return;
}

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.
bool determineUsageData = true;
string[] dirFiles = Directory.GetFiles(winFolder);
List<int> audioGroupNumbers = new List<int>();
List<int> audioGroupNumbersSorted = new List<int>();
List<int> audioGroupNumbersUsed = new List<int>();
List<int> audioGroupNumbersUsedSorted = new List<int>();
string backupLocation = Path.Combine(GetFolder(FilePath), "AudioGroups_" + DateTime.Now.ToString("MM-dd-yyyy_HH-mm-ss"));

foreach (string file in dirFiles) 
{
    string shortFileName = Path.GetFileName(file);
    if (shortFileName.StartsWith("audiogroup") && shortFileName.EndsWith(".dat"))
    {
        bool success = false;
        string shortFileNameNumber = shortFileName.Replace("audiogroup", "").Replace(".dat", "");
        int number;
        success = Int32.TryParse(shortFileNameNumber, out number);
        if (!success)
            continue;
        audioGroupNumbers.Add(number);
        Directory.CreateDirectory(backupLocation);
        File.Copy(file, Path.Combine(backupLocation, shortFileName));
    }
}

ProcessSounds();

if (audioGroupNumbers.Count > 0)
{
    audioGroupNumbersSorted = audioGroupNumbers;
    audioGroupNumbersSorted = audioGroupNumbersSorted.Distinct().ToList();
    audioGroupNumbersSorted.Sort();
}

determineUsageData = false;

if (audioGroupNumbersUsed.Count > 0)
{
    audioGroupNumbersUsedSorted = audioGroupNumbersUsed;
    audioGroupNumbersUsedSorted = audioGroupNumbersUsedSorted.Distinct().ToList();
    audioGroupNumbersUsedSorted.Sort();
}

List<bool> audioGroupNumbersUsedMap = new List<bool>();

for (var i = 0; i < audioGroupNumbersSorted.Count; i++)
{
    audioGroupNumbersUsedMap.Add(false);
}

for (var i = 0; i < audioGroupNumbersSorted.Count; i++)
{
    for (var j = 0; j < audioGroupNumbersUsedSorted.Count; j++)
    {
        if (audioGroupNumbersSorted[i] == audioGroupNumbersUsedSorted[j])
        {
            audioGroupNumbersUsedMap[i] = true;
        }
    }
}

for (var i = 0; i < audioGroupNumbersSorted.Count; i++)
{
    if (audioGroupNumbersUsedMap[i] == false)
    {
        File.Delete(Path.Combine(winFolder, "audiogroup" + audioGroupNumbersSorted[i].ToString() + ".dat"));
    }
}

for (var i = 0; i < audioGroupNumbersUsedSorted.Count; i++)
{
    string audioGroupDataPath = Path.Combine(winFolder, "audiogroup" + audioGroupNumbersUsedSorted[i].ToString() + ".dat");
    if (!File.Exists(audioGroupDataPath))
        continue; // This should not occur.
    UndertaleData audioGroupData = LoadAudioGroup(audioGroupDataPath);
    var audioGroupDataEmbeddedAudioCount = audioGroupData.EmbeddedAudio.Count;
    for (var j = (audioGroupDataEmbeddedAudioCount - 1); j >= 0; j--)
    {
        byte[] AudioGroupBytes = audioGroupData.EmbeddedAudio[j].Data;
        bool isEmbAudioUsed = false;
        for (var k = 0; k < Data.Sounds.Count; k++)
        {
            byte[] SoundBytes = GetSoundData(Data.Sounds[k]);
            if (SoundBytes != null)
            {
                var x = SoundBytes.SequenceEqual(AudioGroupBytes);
                if (x && Data.Sounds[k].GroupID == audioGroupNumbersUsedSorted[i] && Data.Sounds[k].AudioID == j)
                {
                    isEmbAudioUsed = true;
                    break;
                }
            }
        }
        if (!isEmbAudioUsed)
        {
            audioGroupData.EmbeddedAudio.Remove(audioGroupData.EmbeddedAudio[j]);
        }
    }
    var audioGroupWriteStream =
    (
        new FileStream(audioGroupDataPath, FileMode.Create)
    );
    UndertaleIO.Write(audioGroupWriteStream, audioGroupData); // Write it to the disk
    audioGroupWriteStream.Dispose();
}

var DataEmbeddedAudioCount = Data.EmbeddedAudio.Count;
for (var j = (DataEmbeddedAudioCount - 1); j >= 0; j--)
{
    byte[] AudioGroupBytes = Data.EmbeddedAudio[j].Data;
    bool isEmbAudioUsed = false;
    for (var k = 0; k < Data.Sounds.Count; k++)
    {
        byte[] SoundBytes = GetSoundData(Data.Sounds[k]);
        if (SoundBytes != null)
        {
            var x = SoundBytes.SequenceEqual(AudioGroupBytes);
            if (x)
            {
                isEmbAudioUsed = true;
                break;
            }
        }
    }
    if (!isEmbAudioUsed)
    {
        Data.EmbeddedAudio.Remove(Data.EmbeddedAudio[j]);
    }
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

Dictionary<string, IList<UndertaleEmbeddedAudio>> loadedAudioGroups;
IList<UndertaleEmbeddedAudio> GetAudioGroupData(UndertaleSound sound) 
{
    if (loadedAudioGroups == null)
        loadedAudioGroups = new Dictionary<string, IList<UndertaleEmbeddedAudio>>();
    
    string audioGroupName = sound.AudioGroup != null ? sound.AudioGroup.Name.Content : "audiogroup_default";
    if (loadedAudioGroups.ContainsKey(audioGroupName))
        return loadedAudioGroups[audioGroupName];
    
    string groupFilePath = winFolder + "audiogroup" + sound.GroupID + ".dat";
    if (!File.Exists(groupFilePath))
        return null; // Doesn't exist.
            
    UndertaleData data = LoadAudioGroup(groupFilePath);
    if (data != null)
    {
        loadedAudioGroups[audioGroupName] = data.EmbeddedAudio;
        if (determineUsageData)
            audioGroupNumbersUsed.Add(sound.GroupID);
        return data.EmbeddedAudio;
    }
    else
        return null;
}

UndertaleData LoadAudioGroup(string groupFilePath)
{
    try 
    {
        UndertaleData data = null;
        using (var stream = new FileStream(groupFilePath, FileMode.Open, FileAccess.Read))
            data = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + groupFilePath + ":\n" + warning));
        return data;
    }
    catch (Exception e) 
    {
        ScriptMessage("An error occured while trying to load " + groupFilePath + ":\n" + e.Message);
        return null;
    }
}

byte[] GetSoundData(UndertaleSound sound) 
{
    if (sound.AudioFile != null)
        return sound.AudioFile.Data;
    
    if (sound.GroupID > Data.GetBuiltinSoundGroupID()) 
    {
        IList<UndertaleEmbeddedAudio> audioGroup = GetAudioGroupData(sound);
        if (audioGroup != null)
            return audioGroup[sound.AudioID].Data;
    }
    return null;
}

void ProcessSounds() 
{
    foreach (UndertaleSound sound in Data.Sounds)
    {
        if (sound is not null)
            GetSoundData(sound);
    }
}
