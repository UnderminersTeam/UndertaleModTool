using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

string exportedSoundsDir = PromptChooseDirectory();
if (exportedSoundsDir is null)
{
    return;
}

// Prompt for export settings.
bool copyExternalAudio = ScriptQuestion("Export external audio files as well? (Will copy to a separate folder.)");
bool groupedExport = false;
if ((Data.AudioGroups?.Count ?? 0) > 0)
{
    groupedExport = ScriptQuestion("Group sounds by audio group?");
}

byte[] EMPTY_WAV_FILE_BYTES = System.Convert.FromBase64String("UklGRiQAAABXQVZFZm10IBAAAAABAAIAQB8AAAB9AAAEABAAZGF0YQAAAAA=");
string DEFAULT_AUDIOGROUP_NAME = "audiogroup_default";

int maxCount = Data.Sounds.Count;
SetProgressBar(null, "Sounds", 0, maxCount);
StartProgressBarUpdater();

await Task.Run(DumpSounds); // This runs synchronously, because it has to load audio groups.

await StopProgressBarUpdater();
HideProgressBar();

void IncProgressLocal()
{
    if (GetProgress() < maxCount)
    {
        IncrementProgress();
    }
}

Dictionary<string, IList<UndertaleEmbeddedAudio>> loadedAudioGroups = null;
IList<UndertaleEmbeddedAudio> GetAudioGroupData(UndertaleSound sound)
{
    loadedAudioGroups ??= new();

    // Try getting cached audio group by name.
    string audioGroupName = sound.AudioGroup is not null ? sound.AudioGroup.Name.Content : DEFAULT_AUDIOGROUP_NAME;
    if (loadedAudioGroups.ContainsKey(audioGroupName))
    {
        return loadedAudioGroups[audioGroupName];
    }

    // Not cached, so try locating audiogroup file.
    string relativeAudioGroupPath;
    if (sound.AudioGroup is UndertaleAudioGroup { Path.Content: string customRelativePath })
    {
        relativeAudioGroupPath = customRelativePath;
    }
    else
    {
        relativeAudioGroupPath = $"audiogroup{sound.GroupID}.dat";
    }
    string groupFilePath = Path.Combine(Path.GetDirectoryName(FilePath), relativeAudioGroupPath);
    if (!File.Exists(groupFilePath))
    {
        // Doesn't exist... don't try loading.
        return null;
    }

    // Load data file.
    try
    {
        UndertaleData data = null;
        using (var stream = new FileStream(groupFilePath, FileMode.Open, FileAccess.Read))
        {
            data = UndertaleIO.Read(stream, (warning, _) => ScriptWarning($"A warning occured while trying to load {audioGroupName}:\n{warning}"));
        }

        loadedAudioGroups[audioGroupName] = data.EmbeddedAudio;
        return data.EmbeddedAudio;
    } 
    catch (Exception e)
    {
        ScriptError($"An error occured while trying to load {audioGroupName}:\n{e.Message}");
        return null;
    }
}

byte[] GetSoundData(UndertaleSound sound)
{
    // Try to get audio directly, if embedded in main file.
    if (sound.AudioFile is not null)
    {
        return sound.AudioFile.Data;
    }

    // Try to get audio from its audiogroup.
    if (sound.GroupID > Data.GetBuiltinSoundGroupID())
    {
        IList<UndertaleEmbeddedAudio> audioGroup = GetAudioGroupData(sound);
        if (audioGroup is not null)
        {
            return audioGroup[sound.AudioID].Data;
        }
    }

    // All attempts to get data failed; just use empty WAV data.
    return EMPTY_WAV_FILE_BYTES;
}

void DumpSounds()
{
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
    // Determine output audio file path.
    string soundName = sound.Name.Content;
    string soundFilePath;
    if (groupedExport)
    {
        soundFilePath = Path.Combine(exportedSoundsDir, sound.AudioGroup.Name.Content, soundName);
        Directory.CreateDirectory(Path.Combine(exportedSoundsDir, sound.AudioGroup.Name.Content));
    }
    else
    {
        soundFilePath = Path.Combine(exportedSoundsDir, soundName);
    }

    // Determine output file type.
    bool flagCompressed = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);
    bool flagEmbedded = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
    string audioExt = ".ogg";
    bool isEmbedded = true;
    if (flagEmbedded && !flagCompressed)
    {
        // IsEmbedded, Regular: WAV, embedded.
        audioExt = ".wav";
    }
    else if (flagCompressed && !flagEmbedded)
    {
        // IsCompressed, Regular: OGG, embedded.
        audioExt = ".ogg";
    }
    else if (flagCompressed && flagEmbedded)
    {
        // IsEmbedded, IsCompressed, Regular: OGG, embedded.
        audioExt = ".ogg";
    }
    else if (!flagCompressed && !flagEmbedded)
    {
        // Regular: OGG, external.
        isEmbedded = false;
        audioExt = ".ogg";

        // Only copy external audio if enabled.
        if (copyExternalAudio)
        {
            string externalFilename = sound.File.Content;
            if (!externalFilename.Contains('.'))
            {
                // Add file extension if none already exists (assume OGG).
                externalFilename += ".ogg";
            }
            string sourcePath = Path.Combine(Path.GetDirectoryName(FilePath), externalFilename);
            string destPath;
            if (groupedExport)
            {
                destPath = Path.Combine(exportedSoundsDir, sound.AudioGroup.Name.Content, "external", soundName + audioExt);
                Directory.CreateDirectory(Path.Combine(exportedSoundsDir, sound.AudioGroup.Name.Content, "external"));
            }
            else
            {
                destPath = Path.Combine(exportedSoundsDir, "external", soundName + audioExt);
            }
            File.Copy(sourcePath, destPath, true);
        }
    }
    if (isEmbedded)
    {
        File.WriteAllBytes(soundFilePath + audioExt, GetSoundData(sound));
    }

    IncProgressLocal();
}

