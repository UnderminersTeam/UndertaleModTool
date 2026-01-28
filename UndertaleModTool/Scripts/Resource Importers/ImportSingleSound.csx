using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSound;
using static UndertaleModLib.Models.UndertaleSound.AudioEntryFlags;
using static UndertaleModLib.UndertaleData;

EnsureDataLoaded();

UndertaleEmbeddedAudio audioFile = null;
int audioID = -1;
int audioGroupID = -1;
int embAudioID = -1;
bool usesAGRP = (Data.AudioGroups.Count > 0);

if (!usesAGRP)
{
    var q =
        "This game does not use audiogroups.\n"
        + "Importing to external audiogroups is therefore disabled.";
    ScriptWarning(q);
}

string soundPath = PromptLoadFile("", "Sound files (*.WAV;*.OGG)|*.WAV;*.OGG");
if (string.IsNullOrEmpty(soundPath))
{
    // User cancelled the operation, return without error.
    return;
}

// Determine basic sound name properties.
string filename = Path.GetFileName(soundPath);
string soundName = Path.GetFileNameWithoutExtension(soundPath);
bool isOGG = Path.GetExtension(soundPath).ToLower() == ".ogg";

bool embedSound = true;
bool decodeLoad = false;

if (isOGG)
{
    var q =
        "Your sound appears to be an OGG.\n"
        + "No - keep it external\n"
        + "Yes - embed sound into the game (use responsibly!)";
    embedSound = ScriptQuestion(q);
    if (embedSound)
    {
        decodeLoad = ScriptQuestion(
            "Do you want to decompress this sound on load? (higher memory, lower CPU)"
        );
    }
}

string audioGroupName = "";
string folderName = Path.GetFileName(Path.GetDirectoryName(soundPath));
bool needAGRP = false;

// Search for an existing sound with the given name.
UndertaleSound existingSound = null;
bool replaceSoundPropertiesCheck = false;
foreach (UndertaleSound sound in Data.Sounds)
{
    string name = sound.Name?.Content;
    if (name != soundName)
        continue;

    existingSound = sound;
    var q =
        $"Sound \"{name}\" already exists in the game;"
        + "it will be replaced instead of added. "
        + "Would you like to replace the sound properties as well?";
    replaceSoundPropertiesCheck = ScriptQuestion(q);
    break;
}

// Try to find an audiogroup (when not updating an existing sound).
if (embedSound && usesAGRP && existingSound is null)
{
    var q =
        $"Your last folder name is \"{folderName}\".\n"
        + "Do you want to treat it as the name of the sound's audiogroup?\n"
        + "(Answer No to use \"audiogroup_default\" instead.)";
    needAGRP = ScriptQuestion(q);
}

// Find or create audio group, if needed.
if (needAGRP)
{
    audioGroupName = folderName;

    // Find the audio group we need.
    for (int i = 0; i < Data.AudioGroups.Count; i++)
    {
        if (Data.AudioGroups[i]?.Name?.Content == audioGroupName)
        {
            audioGroupID = i;
            break;
        }
    }

    // If no existing audio group could be found, create a new one.
    if (audioGroupID == -1)
    {
        // These bytes represent an empty audio group file.
        // It consists of these 4-byte words:
        // * 'FORM'
        // * Data length (8)
        // * 'AUDO'
        // * Audio length (0)
        var bytes = Convert.FromBase64String("Rk9STQwAAABBVURPBAAAAAAAAAA=");

        var dir = Path.GetDirectoryName(FilePath);
        var filename = $"audiogroup{Data.AudioGroups.Count}.dat";
        var path = Path.Combine(dir, filename);

        File.WriteAllBytes(path, bytes);

        UndertaleAudioGroup newAudioGroup = new()
        {
            Name = Data.Strings.MakeString(audioGroupName),
        };

        Data.AudioGroups.Add(newAudioGroup);
    }
}

// If this is an existing sound, use its audio group ID.
if (existingSound is not null)
{
    audioGroupID = existingSound.GroupID;
}

// If the audiogroup ID is for the builtin audiogroup ID,
// it's embedded in the main data file and doesn't need to be loaded.
if (audioGroupID == Data.GetBuiltinSoundGroupID())
{
    needAGRP = false;
}

// Create embedded audio entry if required.
UndertaleEmbeddedAudio soundData = null;
if ((embedSound && !needAGRP) || needAGRP)
{
    var bytes = File.ReadAllBytes(soundPath);
    soundData = new UndertaleEmbeddedAudio() { Data = bytes };

    if (existingSound is null)
    {
        embAudioID = Data.EmbeddedAudio.Count;
        Data.EmbeddedAudio.Add(soundData);
    }
    else
    {
        embAudioID = existingSound.AudioID;
        Data.EmbeddedAudio[embAudioID] = soundData;
    }
}

// Update external audio group file if required.
if (needAGRP)
{
    // Load audiogroup into memory.
    UndertaleData audioGroupDat;
    string relativeAudioGroupPath;
    if (
        audioGroupID < Data.AudioGroups.Count
        && Data.AudioGroups[audioGroupID]
            is UndertaleAudioGroup { Path.Content: string customRelativePath }
    )
    {
        relativeAudioGroupPath = customRelativePath;
    }
    else
    {
        relativeAudioGroupPath = $"audiogroup{audioGroupID}.dat";
    }
    string audioGroupPath = Path.Combine(Path.GetDirectoryName(FilePath), relativeAudioGroupPath);
    using (FileStream audioGroupReadStream = new(audioGroupPath, FileMode.Open, FileAccess.Read))
    {
        audioGroupDat = UndertaleIO.Read(audioGroupReadStream);
    }

    // Add the EmbeddedAudio entry to the audiogroup data.
    if (existingSound is null)
    {
        audioID = audioGroupDat.EmbeddedAudio.Count;
        audioGroupDat.EmbeddedAudio.Add(soundData);
    }
    else
    {
        audioID = embAudioID;
        audioGroupDat.EmbeddedAudio[embAudioID] = soundData;
    }

    // Write audio group back to disk.
    using FileStream audioGroupWriteStream = new(audioGroupPath, FileMode.Create);
    UndertaleIO.Write(audioGroupWriteStream, audioGroupDat);
}

// Determine sound flags.
UndertaleSound.AudioEntryFlags flags = Regular;

if (embedSound && !decodeLoad)
{
    // For some reason, embedded OGGs that are not decoded on load
    // only get Regular and IsCompressed but not IsEmbedded.
    flags |= IsEmbedded;
}

if (decodeLoad)
{
    flags |= IsDecompressedOnLoad;
}

// Determine final embedded audio reference (or null).
UndertaleEmbeddedAudio finalAudioReference = null;
if (!embedSound)
{
    finalAudioReference = null;
}
if (embedSound && !needAGRP)
{
    finalAudioReference = Data.EmbeddedAudio[embAudioID];
}
if (embedSound && needAGRP)
{
    finalAudioReference = null;
}

// Determine final audio group ID and reference.
int finalGroupID = Data.GetBuiltinSoundGroupID();
if (needAGRP && usesAGRP)
{
    finalGroupID = audioGroupID;
}

UndertaleAudioGroup finalGroupReference = null;
if (usesAGRP)
{
    finalGroupReference = Data.AudioGroups[finalGroupID];
}

// Update/create actual sound asset.
if (existingSound is null)
{
    UndertaleSound newSound = new()
    {
        Name = Data.Strings.MakeString(soundName),
        Flags = flags,
        Type = isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav"),
        File = Data.Strings.MakeString(filename),
        Effects = 0,
        Volume = 1.0f,
        Pitch = 1.0f,
        AudioID = audioID,
        AudioFile = finalAudioReference,
        AudioGroup = finalGroupReference,
        GroupID = finalGroupID,
    };
    Data.Sounds.Add(newSound);
    ChangeSelection(newSound);
}
else if (replaceSoundPropertiesCheck)
{
    existingSound.Flags = flags;
    existingSound.Type = isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav");
    existingSound.File = Data.Strings.MakeString(filename);
    existingSound.Effects = 0;
    existingSound.Volume = 1.0f;
    existingSound.Pitch = 1.0f;
    existingSound.AudioID = audioID;
    existingSound.AudioFile = finalAudioReference;
    existingSound.AudioGroup = finalGroupReference;
    existingSound.GroupID = finalGroupID;
    ChangeSelection(existingSound);
}
else
{
    existingSound.AudioFile = finalAudioReference;
    existingSound.AudioID = audioID;
    ChangeSelection(existingSound);
}

ScriptMessage("Sound added successfully!");

