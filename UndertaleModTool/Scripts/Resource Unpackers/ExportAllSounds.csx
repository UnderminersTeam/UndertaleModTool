// Original script by Kneesnap, updated by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.
bool usesAGRP = (Data.AudioGroups.Count > 0);
string exportedSoundsDir = Path.Combine(winFolder, "Exported_Sounds");

//Overwrite Folder Check One
if (Directory.Exists(exportedSoundsDir))
{
    bool overwriteCheckOne = ScriptQuestion(@"An 'Exported_Sounds' folder already exists.

Would you like to remove it? This may take some time.

Note: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.
");
    if (!overwriteCheckOne)
    {
        ScriptError("An 'Exported_Sounds' folder already exists. Please remove it.", "Error: Export already exists.");
        return;
    }
    Directory.Delete(exportedSoundsDir, true);
}

// EXTERNAL OGG CHECK
bool externalOGG_Copy = ScriptQuestion(@"This script exports embedded sounds.
However, it can also export the external OGGs to a separate folder.
If you would like to export both, select 'YES'.
If you just want the embedded sounds, select 'NO'.
");

// Overwrite Folder Check Two
if (Directory.Exists(exportedSoundsDir) && externalOGG_Copy)
{
    bool overwriteCheckTwo = ScriptQuestion(@"A 'External_Sounds' folder already exists.
Would you like to remove it? This may some time.

Note: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.
");
    if (!overwriteCheckTwo)
    {
        ScriptError("A 'External_Sounds' folder already exists. Please remove it.", "Error: Export already exists.");
        return;
    }
    
    Directory.Delete(exportedSoundsDir, true);
}

// Group by audio group check
bool groupedExport;
if (usesAGRP)
{
    groupedExport = ScriptQuestion("Group sounds by audio group?");

}

byte[] EMPTY_WAV_FILE_BYTES = System.Convert.FromBase64String("UklGRiQAAABXQVZFZm10IBAAAAABAAIAQB8AAAB9AAAEABAAZGF0YQAAAAA=");
string DEFAULT_AUDIOGROUP_NAME = "audiogroup_default";

int maxCount = Data.Sounds.Count;
SetProgressBar(null, "Sound", 0, maxCount);
StartProgressBarUpdater();

await Task.Run(DumpSounds); // This runs sync, because it has to load audio groups.

await StopProgressBarUpdater();
HideProgressBar();
if (Directory.Exists(exportedSoundsDir))
    ScriptMessage("Sounds exported to " + winFolder + " in the 'Exported_Sounds' and 'External_Sounds' folders.");
else
    ScriptMessage("Sounds exported to " + winFolder + " in the 'Exported_Sounds' folder.");

void IncProgressLocal()
{
    if (GetProgress() < maxCount)
        IncrementProgress();
}

void MakeFolder(string folderName)
{
    string fullPath = Path.Combine(winFolder, folderName);
    Directory.CreateDirectory(fullPath);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

Dictionary<string, IList<UndertaleEmbeddedAudio>> loadedAudioGroups;
IList<UndertaleEmbeddedAudio> GetAudioGroupData(UndertaleSound sound)
{
    if (loadedAudioGroups is null)
        loadedAudioGroups = new Dictionary<string, IList<UndertaleEmbeddedAudio>>();

    string audioGroupName = sound.AudioGroup is not null ? sound.AudioGroup.Name.Content : DEFAULT_AUDIOGROUP_NAME;
    if (loadedAudioGroups.ContainsKey(audioGroupName))
        return loadedAudioGroups[audioGroupName];

    string groupFilePath = Path.Combine(winFolder, "audiogroup" + sound.GroupID + ".dat");
    if (!File.Exists(groupFilePath))
        return null; // Doesn't exist.

    try
    {
        UndertaleData data = null;
        using (var stream = new FileStream(groupFilePath, FileMode.Open, FileAccess.Read))
            data = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + audioGroupName + ":\n" + warning));

        loadedAudioGroups[audioGroupName] = data.EmbeddedAudio;
        return data.EmbeddedAudio;
    } 
    catch (Exception e)
    {
        ScriptMessage("An error occured while trying to load " + audioGroupName + ":\n" + e.Message);
        return null;
    }
}

byte[] GetSoundData(UndertaleSound sound)
{
    if (sound.AudioFile is not null)
        return sound.AudioFile.Data;

    if (sound.GroupID > Data.GetBuiltinSoundGroupID())
    {
        IList<UndertaleEmbeddedAudio> audioGroup = GetAudioGroupData(sound);
        if (audioGroup is not null)
            return audioGroup[sound.AudioID].Data;
    }
    return EMPTY_WAV_FILE_BYTES;
}

void DumpSounds()
{
    //MakeFolder("Exported_Sounds");
    foreach (UndertaleSound sound in Data.Sounds)
    {
        if (sound is not null)
        {
            DumpSound(sound);
        }
        else
        {
            IncProgressLocal();
        }
    }
}

void DumpSound(UndertaleSound sound)
{
    string soundName = sound.Name.Content;
    bool flagCompressed = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);
    bool flagEmbedded = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
    // Compression, Streamed, Unpack on Load.
    // 1 = 000 = IsEmbedded, Regular.               '.wav' type saved in win.
    // 2 = 100 = IsCompressed, Regular.             '.ogg' type saved in win
    // 3 = 101 = IsEmbedded, IsCompressed, Regular. '.ogg' type saved in win.
    // 4 = 110 = Regular.                           '.ogg' type saved outside win.
    string audioExt = ".ogg";
    string soundFilePath;
    if (groupedExport)
        soundFilePath = Path.Combine(exportedSoundsDir, sound.AudioGroup.Name.Content, soundName);
    else
        soundFilePath = Path.Combine(exportedSoundsDir, soundName);
    MakeFolder("Exported_Sounds");
    if (groupedExport)
        MakeFolder(Path.Combine("Exported_Sounds", sound.AudioGroup.Name.Content));

    bool process = true;
    if (flagEmbedded && !flagCompressed) // 1.
        audioExt = ".wav";
    else if (flagCompressed && !flagEmbedded) // 2.
        audioExt = ".ogg";
    else if (flagCompressed && flagEmbedded) // 3.
        audioExt = ".ogg";
    else if (!flagCompressed && !flagEmbedded)
    {
        process = false;
        audioExt = ".ogg";
        string source = Path.Combine(winFolder, soundName + audioExt);
        string dest = Path.Combine(winFolder, "External_Sounds", soundName + audioExt);
        if (externalOGG_Copy)
        {
            if (groupedExport)
            {
                dest = Path.Combine(winFolder, "External_Sounds", sound.AudioGroup.Name.Content, soundName + audioExt);

                MakeFolder(Path.Combine("External_Sounds", sound.AudioGroup.Name.Content));
            }
            MakeFolder("External_Sounds");
            File.Copy(source, dest, false);
        }
    }
    if (process && !File.Exists(soundFilePath + audioExt))
        File.WriteAllBytes(soundFilePath + audioExt, GetSoundData(sound));

    IncProgressLocal();
}

