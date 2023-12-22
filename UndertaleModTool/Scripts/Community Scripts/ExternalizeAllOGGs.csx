// Made for simple use with all games by Grossley.
// Based on the work of: nik the neko.

using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// Setup root export folder.
string winFolder = GetFolder(FilePath); // The folder data.win is located in.

int sounds = 0;
bool usesAGRPs = (Data.AudioGroups.Count > 0);

if (Data.AudioGroups.Count > 1)
{
    bool warningCheck = ScriptQuestion(@"This game uses external audiogroup.dat files.
In order to externalize the audio files it will clear all data from the external audiogroup.dat files.
It is recommended that you make a backup of your game files.
If you have already made a backup and wish to continue, select 'Yes'.
Otherwise, select 'No', and make a backup of the game before using this script.
");
    if (!warningCheck)
        return;
}

//Overwrite Folder Check One
string exportedSoundsPath = Path.Combine(winFolder, "Exported_Sounds");
if (Directory.Exists(exportedSoundsPath))
{
    bool overwriteCheckOne = ScriptQuestion(@"An 'Exported_Sounds' folder already exists.
Would you like to remove it? This may some time.

Note: If an error window stating that 'the directory is not empty' appears, please try again or delete the folder manually.
");
    if (!overwriteCheckOne)
    {
        ScriptError("An 'Exported_Sounds' folder already exists. Please remove it.", "Error: Export already exists.");
        return;
    }
    
    Directory.Delete(exportedSoundsPath, true);
}

// Group by audio group check
bool groupedExport;
if (usesAGRPs)
{
    groupedExport = ScriptQuestion(@"Group sounds by audio group?

NOTE: Code using 'audio_group' functions will break.
You will have to check the code for these functions and change it accordingly.
");
}

SetProgressBar(null, "Externalizing Sounds...", 0, Data.Sounds.Count);
StartProgressBarUpdater();

SyncBinding("Strings", true);
await Task.Run(() => {
    DumpSounds(); // This runs sync, because it has to load audio groups.
    ExternalizeSounds(); // This runs sync, because it has to load audio groups.
});
DisableAllSyncBindings();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Externalization Complete.\nExternalized " + sounds.ToString() + " sounds.\n\nNOTE: You will need to convert any external WAV files into OGG files.\nThen replace the WAV file with the OGG file.\nOtherwise the sound will not play.\nA batch conversion tool such as 'LameXP' will help.\nCheck the #faq for more information or message Grossley#2869 on Discord.");


void ExternalizeSounds()
{
    foreach (UndertaleSound sound in Data.Sounds)
        ExternalizeSound(sound);
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

void ExternalizeSound(UndertaleSound sound)
{
    bool flagCompressed = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);
    bool flagEmbedded = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
    var _groupid = sound.GroupID;
    var _audioid = sound.AudioID;
    var _fname = sound.File.Content;
    string searchName = sound.Name.Content;
    string searchFilePath = exportedSoundsPath;
    // If it's not an external file already, setup the sound entry such that it is external.
    if (flagCompressed == true || flagEmbedded == true)
    {
        // 4.
        string[] files = Directory.GetFiles(searchFilePath, searchName + ".*", SearchOption.AllDirectories);
        var path_result = files[0];
        var path_relative = path_result.Replace(winFolder, "");
        var new_filename = Path.ChangeExtension(path_relative, ".ogg");
        sound.File = Data.Strings.MakeString(new_filename);
        if (sound.GroupID == Data.GetBuiltinSoundGroupID()) //For sounds embedded in the data.win itself.
        {
            sound.AudioFile.Data = new byte[1];
            sound.AudioFile.Data[0] = 0;
        }
        else //For sounds embedded in the external audiogroup.dat files.
        {
            string audioGroupPath = Path.Combine(winFolder, "audiogroup" + _groupid.ToString() + ".dat");
            var audioGroupReadStream = (new FileStream(audioGroupPath, FileMode.Open, FileAccess.Read)); // Load the audiogroup dat into memory
            UndertaleData audioGroupDat = UndertaleIO.Read(audioGroupReadStream); // Load as UndertaleData
            audioGroupReadStream.Dispose();
            //make a one cell array and make it 0.
            //Array.Resize(audioGroupDat.EmbeddedAudio[_audioid].Data, 1);
            audioGroupDat.EmbeddedAudio[_audioid].Data = new byte[1];
            audioGroupDat.EmbeddedAudio[_audioid].Data[0] = 0;

            var audioGroupWriteStream = (new FileStream(audioGroupPath, FileMode.Create));
            UndertaleIO.Write(audioGroupWriteStream, audioGroupDat); // Write it to the disk
            audioGroupWriteStream.Dispose();
        }
    }
    // Update audio entry to set AudioFile to null.
    sound.AudioID = -1;
    if (sound.AudioFile != null)
        sound.AudioFile = null;
    sound.Flags = UndertaleSound.AudioEntryFlags.Regular;
    if (sound.Type?.Content != null)
        sound.Type.Content = ".ogg";
    if (usesAGRPs)
    {
        // Reset audiogroup to audiogroup_default.
        sound.GroupID = Data.GetBuiltinSoundGroupID();
        sound.AudioGroup = Data.AudioGroups[Data.GetBuiltinSoundGroupID()];
    }
    // if it doesn't then we shouldn't care, it's always null.

    sounds++;
    IncrementProgress();
}

/////////////////////////////////////////////////////////////////////////////

byte[] EMPTY_WAV_FILE_BYTES = System.Convert.FromBase64String("UklGRiQAAABXQVZFZm10IBAAAAABAAIAQB8AAAB9AAAEABAAZGF0YQAAAAA=");
string DEFAULT_AUDIOGROUP_NAME = "audiogroup_default";

void MakeFolder(String folderName)
{
    Directory.CreateDirectory(Path.Combine(winFolder, folderName));
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
    string soundFilePath = Path.Combine(exportedSoundsPath, soundName);
    if (groupedExport)
        soundFilePath = Path.Combine(exportedSoundsPath, sound.AudioGroup.Name.Content, soundName);
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
    else if (!flagCompressed && !flagEmbedded) // 4.
        process = false;
    if (process && !File.Exists(soundFilePath + audioExt))
    {
        if (usesAGRPs)
            File.WriteAllBytes(soundFilePath + audioExt, GetSoundData(sound));
        else
            File.WriteAllBytes(soundFilePath + audioExt, sound.AudioFile.Data);
    }
}

