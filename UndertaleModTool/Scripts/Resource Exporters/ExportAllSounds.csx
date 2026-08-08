using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

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
string dataDirectory = Path.GetDirectoryName(FilePath);
int builtinAudioGroupId = Data.GetBuiltinSoundGroupID();
HashSet<string> createdDirectories = new(StringComparer.OrdinalIgnoreCase);
HashSet<string> reservedDestinationPaths = new(StringComparer.OrdinalIgnoreCase);
List<(string SoundName, string DestinationPath, byte[] Data, string SourcePath)> dumpJobs = new();

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

    string audioGroupName = GetAudioGroupName(sound);
    string relativeAudioGroupPath = GetAudioGroupPath(sound);
    string cacheKey = $"{sound.GroupID}:{relativeAudioGroupPath}";
    if (loadedAudioGroups.TryGetValue(cacheKey, out IList<UndertaleEmbeddedAudio> cachedAudioGroup))
    {
        return cachedAudioGroup;
    }

    string groupFilePath = Paths.JoinVerifyWithinDirectory(dataDirectory, relativeAudioGroupPath);
    if (!File.Exists(groupFilePath))
    {
        // Doesn't exist... don't try loading.
        loadedAudioGroups[cacheKey] = null;
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

        loadedAudioGroups[cacheKey] = data.EmbeddedAudio;
        return data.EmbeddedAudio;
    }
    catch (Exception e)
    {
        ScriptError($"An error occured while trying to load {audioGroupName}:\n{e.Message}");
        loadedAudioGroups[cacheKey] = null;
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
    if (sound.GroupID != builtinAudioGroupId && sound.AudioID >= 0)
    {
        IList<UndertaleEmbeddedAudio> audioGroup = GetAudioGroupData(sound);
        if (audioGroup is not null && sound.AudioID < audioGroup.Count)
        {
            return audioGroup[sound.AudioID].Data;
        }
    }

    // All attempts to get data failed; just use empty WAV data.
    return EMPTY_WAV_FILE_BYTES;
}

string GetAudioGroupName(UndertaleSound sound)
{
    string audioGroupName = sound.AudioGroup?.Name?.Content;
    return string.IsNullOrWhiteSpace(audioGroupName) ? DEFAULT_AUDIOGROUP_NAME : audioGroupName;
}

string GetAudioGroupPath(UndertaleSound sound)
{
    return sound.AudioGroup is UndertaleAudioGroup { Path.Content: string customRelativePath }
        ? customRelativePath
        : $"audiogroup{sound.GroupID}.dat";
}

void EnsureDirectory(string path)
{
    if (createdDirectories.Add(path))
    {
        Directory.CreateDirectory(path);
    }
}

string ReserveUniqueDestinationPath(string directory, string soundName, string extension)
{
    string destinationPath = Paths.JoinVerifyWithinDirectory(directory, soundName + extension);
    if (reservedDestinationPaths.Add(destinationPath))
    {
        return destinationPath;
    }

    for (int suffix = 1; ; suffix++)
    {
        destinationPath = Paths.JoinVerifyWithinDirectory(directory, $"{soundName}_{suffix}{extension}");
        if (reservedDestinationPaths.Add(destinationPath))
        {
            return destinationPath;
        }
    }
}

void CopyExternalSound(UndertaleSound sound, string soundName, string audioExt)
{
    string externalFilename = sound.File?.Content;
    if (string.IsNullOrWhiteSpace(externalFilename))
    {
        ScriptWarning($"Skipping external audio for \"{soundName}\": no source file is set.");
        return;
    }

    if (!externalFilename.Contains('.'))
    {
        // Add file extension if none already exists (assume OGG).
        externalFilename += ".ogg";
    }

    try
    {
        string sourcePath = Paths.JoinVerifyWithinDirectory(dataDirectory, externalFilename);
        if (!File.Exists(sourcePath))
        {
            ScriptWarning($"Skipping external audio for \"{soundName}\": source file was not found ({externalFilename}).");
            return;
        }

        string externalDirectory = groupedExport
            ? Paths.JoinVerifyWithinDirectory(exportedSoundsDir, GetAudioGroupName(sound), "external")
            : Paths.JoinVerifyWithinDirectory(exportedSoundsDir, "external");
        EnsureDirectory(externalDirectory);

        string destPath = ReserveUniqueDestinationPath(externalDirectory, soundName, audioExt);
        dumpJobs.Add((soundName, destPath, null, sourcePath));
    }
    catch (Exception ex)
    {
        ScriptWarning($"Skipping external audio for \"{soundName}\": {ex.Message}");
    }
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

    int maxParallelism = Math.Clamp(Environment.ProcessorCount, 1, 8);
    Parallel.ForEach(dumpJobs, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, job =>
    {
        try
        {
            if (job.Data is not null)
            {
                File.WriteAllBytes(job.DestinationPath, job.Data);
            }
            else
            {
                File.Copy(job.SourcePath, job.DestinationPath, true);
            }
        }
        catch (Exception ex)
        {
            ScriptWarning($"Skipping audio export for \"{job.SoundName}\": {ex.Message}");
        }
    });
}

void DumpSound(UndertaleSound sound)
{
    // Determine output audio file path.
    string soundName = sound.Name.Content;
    string soundDirectory;
    if (groupedExport)
    {
        soundDirectory = Paths.JoinVerifyWithinDirectory(exportedSoundsDir, GetAudioGroupName(sound));
        EnsureDirectory(soundDirectory);
    }
    else
    {
        soundDirectory = exportedSoundsDir;
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
            CopyExternalSound(sound, soundName, audioExt);
        }
    }
    if (isEmbedded)
    {
        string destinationPath = ReserveUniqueDestinationPath(soundDirectory, soundName, audioExt);
        dumpJobs.Add((soundName, destinationPath, GetSoundData(sound), null));
    }

    IncProgressLocal();
}
