// Original script by Kneesnap, updated by Grossley
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

int maxCount;

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.
bool usesAGRP               = (Data.AudioGroups.Count > 0);

//Overwrite Folder Check One
if (Directory.Exists(winFolder + "Exported_Sounds\\"))
{
    bool overwriteCheckOne = ScriptQuestion(@"A 'Exported_Sounds' folder already exists.
Would you like to remove it? This may some time.

Note: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.
");
    if (overwriteCheckOne)
        Directory.Delete(winFolder + "Exported_Sounds\\", true);
    if (!overwriteCheckOne)
    {
        ScriptError("A 'Exported_Sounds' folder already exists. Please remove it.", "Error: Export already exists.");
        return;
    }
}

var externalOGG_Copy = 0;

// EXTERNAL OGG CHECK
bool externalOGGs = ScriptQuestion(@"This script exports embedded sounds.
However, it can also export the external OGGs to a separate folder.
If you would like to export both, select 'YES'.
If you just want the embedded sounds, select 'NO'.
");

if (externalOGGs)
    externalOGG_Copy = 1;
if (!externalOGGs)
    externalOGG_Copy = 0;

// Overwrite Folder Check Two
if (Directory.Exists(winFolder + "External_Sounds\\") && externalOGG_Copy == 1)
{
    bool overwriteCheckTwo = ScriptQuestion(@"A 'External_Sounds' folder already exists.
Would you like to remove it? This may some time.

Note: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.
");
    if (overwriteCheckTwo)
        Directory.Delete(winFolder + "External_Sounds\\", true);
    if (!overwriteCheckTwo)
    {
        ScriptError("A 'External_Sounds' folder already exists. Please remove it.", "Error: Export already exists.");
        return;
    }
}

// Group by audio group check
var groupedExport = 0;
if (usesAGRP)
{
    bool groupedCheck = ScriptQuestion(@"Group sounds by audio group?
    ");
    if (groupedCheck)
        groupedExport = 1;
    if (!groupedCheck)
        groupedExport = 0;
}

byte[] EMPTY_WAV_FILE_BYTES = System.Convert.FromBase64String("UklGRiQAAABXQVZFZm10IBAAAAABAAIAQB8AAAB9AAAEABAAZGF0YQAAAAA=");
string DEFAULT_AUDIOGROUP_NAME = "audiogroup_default";

maxCount = Data.Sounds.Count;
SetProgressBar(null, "Sound", 0, maxCount);
StartProgressBarUpdater();

await Task.Run(DumpSounds); // This runs sync, because it has to load audio groups.

await StopProgressBarUpdater();
HideProgressBar();
if (Directory.Exists(winFolder + "External_Sounds\\"))
    ScriptMessage("Sounds exported to " + winFolder + " in the 'Exported_Sounds' and 'External_Sounds' folders.");
else
    ScriptMessage("Sounds exported to " + winFolder + " in the 'Exported_Sounds' folder.");

void IncProgressLocal()
{
    if (GetProgress() < maxCount)
        IncrementProgress();
}

void MakeFolder(String folderName)
{
    if (!Directory.Exists(winFolder + folderName + "/"))
        Directory.CreateDirectory(winFolder + folderName + "/");
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

    string audioGroupName = sound.AudioGroup != null ? sound.AudioGroup.Name.Content : DEFAULT_AUDIOGROUP_NAME;
    if (loadedAudioGroups.ContainsKey(audioGroupName))
        return loadedAudioGroups[audioGroupName];

    string groupFilePath = winFolder + "audiogroup" + sound.GroupID + ".dat";
    if (!File.Exists(groupFilePath))
        return null; // Doesn't exist.

    try
    {
        UndertaleData data = null;
        using (var stream = new FileStream(groupFilePath, FileMode.Open, FileAccess.Read))
            data = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + audioGroupName + ":\n" + warning));

        loadedAudioGroups[audioGroupName] = data.EmbeddedAudio;
        return data.EmbeddedAudio;
    } catch (Exception e)
    {
        ScriptMessage("An error occured while trying to load " + audioGroupName + ":\n" + e.Message);
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
    return EMPTY_WAV_FILE_BYTES;
}

void DumpSounds()
{
    //MakeFolder("Exported_Sounds");
    foreach (UndertaleSound sound in Data.Sounds)
        DumpSound(sound);
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
    if (groupedExport == 1)
        soundFilePath = winFolder + "Exported_Sounds\\" + sound.AudioGroup.Name.Content + "\\" + soundName;
    else
        soundFilePath = winFolder + "Exported_Sounds\\" + soundName;
    MakeFolder("Exported_Sounds");
    if (groupedExport == 1)
        MakeFolder("Exported_Sounds\\" + sound.AudioGroup.Name.Content);
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
        string source = winFolder + soundName + audioExt;
        string dest = winFolder + "External_Sounds\\" + soundName + audioExt;
        if (externalOGG_Copy == 1)
        {
            if (groupedExport == 1)
            {
                dest = winFolder + "External_Sounds\\" + sound.AudioGroup.Name.Content + "\\" + soundName + audioExt;
                MakeFolder("External_Sounds\\" + sound.AudioGroup.Name.Content);
            }
            MakeFolder("External_Sounds\\");
            System.IO.File.Copy(source, dest, false);
        }
    }
    if (process && !File.Exists(soundFilePath + audioExt))
        File.WriteAllBytes(soundFilePath + audioExt, GetSoundData(sound));

    IncProgressLocal();
}

