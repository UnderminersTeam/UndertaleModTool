using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

EnsureDataLoaded();

string exportedSoundsDir = PromptChooseDirectory();
if (exportedSoundsDir is null)
{
    return;
}

bool copyExternalAudio = ScriptQuestion("Export external audio files as well? (Will copy to a separate folder.)");
bool groupedExport = false;
if ((Data.AudioGroups?.Count ?? 0) > 0)
{
    groupedExport = ScriptQuestion("Group sounds by audio group?");
}

byte[] EMPTY_WAV_FILE_BYTES = Convert.FromBase64String("UklGRiQAAABXQVZFZm10IBAAAAABAAIAQB8AAAB9AAAEABAAZGF0YQAAAAA=");
string DEFAULT_AUDIOGROUP_NAME = "audiogroup_default";

int maxCount = Data.Sounds.Count;
SetProgressBar(null, "Sounds", 0, maxCount);
StartProgressBarUpdater();

Stopwatch totalStopwatch = Stopwatch.StartNew();
await Task.Run(DumpSounds);
totalStopwatch.Stop();

await StopProgressBarUpdater();
HideProgressBar();

WriteProfile();

void IncProgressLocal()
{
    if (GetProgress() < maxCount)
    {
        IncrementProgress();
    }
}

Dictionary<string, IList<UndertaleEmbeddedAudio>> loadedAudioGroups = null;

int soundsVisited = 0;
int nullSoundCount = 0;
int embeddedWriteCount = 0;
int externalCopyCount = 0;
int wavWriteCount = 0;
int oggWriteCount = 0;
int fallbackWriteCount = 0;
int audioGroupLoadCount = 0;
long embeddedWriteBytes = 0;
long externalCopyBytes = 0;
long pathTicks = 0;
long embeddedResolveTicks = 0;
long embeddedWriteTicks = 0;
long externalCopyTicks = 0;
long audioGroupLoadTicks = 0;

IList<UndertaleEmbeddedAudio> GetAudioGroupData(UndertaleSound sound)
{
    loadedAudioGroups ??= new();

    string audioGroupName = sound.AudioGroup is not null ? sound.AudioGroup.Name.Content : DEFAULT_AUDIOGROUP_NAME;
    if (loadedAudioGroups.TryGetValue(audioGroupName, out IList<UndertaleEmbeddedAudio> cachedAudioGroup))
    {
        return cachedAudioGroup;
    }

    string relativeAudioGroupPath;
    if (sound.AudioGroup is UndertaleAudioGroup { Path.Content: string customRelativePath })
    {
        relativeAudioGroupPath = customRelativePath;
    }
    else
    {
        relativeAudioGroupPath = $"audiogroup{sound.GroupID}.dat";
    }

    string groupFilePath = Paths.JoinVerifyWithinDirectory(Path.GetDirectoryName(FilePath), relativeAudioGroupPath);
    if (!File.Exists(groupFilePath))
    {
        return null;
    }

    Stopwatch loadStopwatch = Stopwatch.StartNew();
    try
    {
        UndertaleData data = null;
        using (var stream = new FileStream(groupFilePath, FileMode.Open, FileAccess.Read))
        {
            data = UndertaleIO.Read(stream, (warning, _) => ScriptWarning($"A warning occured while trying to load {audioGroupName}:\n{warning}"));
        }

        loadedAudioGroups[audioGroupName] = data.EmbeddedAudio;
        audioGroupLoadCount++;
        return data.EmbeddedAudio;
    }
    catch (Exception e)
    {
        ScriptError($"An error occured while trying to load {audioGroupName}:\n{e.Message}");
        return null;
    }
    finally
    {
        loadStopwatch.Stop();
        audioGroupLoadTicks += loadStopwatch.ElapsedTicks;
    }
}

byte[] GetSoundData(UndertaleSound sound)
{
    Stopwatch resolveStopwatch = Stopwatch.StartNew();
    try
    {
        if (sound.AudioFile is not null)
        {
            return sound.AudioFile.Data;
        }

        if (sound.GroupID > Data.GetBuiltinSoundGroupID())
        {
            IList<UndertaleEmbeddedAudio> audioGroup = GetAudioGroupData(sound);
            if (audioGroup is not null)
            {
                return audioGroup[sound.AudioID].Data;
            }
        }

        fallbackWriteCount++;
        return EMPTY_WAV_FILE_BYTES;
    }
    finally
    {
        resolveStopwatch.Stop();
        embeddedResolveTicks += resolveStopwatch.ElapsedTicks;
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
            nullSoundCount++;
            IncProgressLocal();
        }
    }
}

void DumpSound(UndertaleSound sound)
{
    soundsVisited++;

    Stopwatch pathStopwatch = Stopwatch.StartNew();
    string soundName = sound.Name.Content;
    string soundFilePath;
    if (groupedExport)
    {
        soundFilePath = Paths.JoinVerifyWithinDirectory(exportedSoundsDir, sound.AudioGroup.Name.Content, soundName);
        Directory.CreateDirectory(Paths.JoinVerifyWithinDirectory(exportedSoundsDir, sound.AudioGroup.Name.Content));
    }
    else
    {
        soundFilePath = Paths.JoinVerifyWithinDirectory(exportedSoundsDir, soundName);
    }

    bool flagCompressed = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed);
    bool flagEmbedded = sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
    string audioExt = ".ogg";
    bool isEmbedded = true;
    if (flagEmbedded && !flagCompressed)
    {
        audioExt = ".wav";
    }
    else if (flagCompressed && !flagEmbedded)
    {
        audioExt = ".ogg";
    }
    else if (flagCompressed && flagEmbedded)
    {
        audioExt = ".ogg";
    }
    else if (!flagCompressed && !flagEmbedded)
    {
        isEmbedded = false;
        audioExt = ".ogg";
    }
    pathStopwatch.Stop();
    pathTicks += pathStopwatch.ElapsedTicks;

    if (audioExt == ".wav")
        wavWriteCount++;
    else if (audioExt == ".ogg")
        oggWriteCount++;

    if (!isEmbedded && copyExternalAudio)
    {
        string externalFilename = sound.File.Content;
        if (!externalFilename.Contains('.'))
        {
            externalFilename += ".ogg";
        }

        string sourcePath = Paths.JoinVerifyWithinDirectory(Path.GetDirectoryName(FilePath), externalFilename);
        string destPath;
        if (groupedExport)
        {
            destPath = Paths.JoinVerifyWithinDirectory(exportedSoundsDir, sound.AudioGroup.Name.Content, "external", soundName + audioExt);
            Directory.CreateDirectory(Paths.JoinVerifyWithinDirectory(exportedSoundsDir, sound.AudioGroup.Name.Content, "external"));
        }
        else
        {
            destPath = Paths.JoinVerifyWithinDirectory(exportedSoundsDir, "external", soundName + audioExt);
            Directory.CreateDirectory(Paths.JoinVerifyWithinDirectory(exportedSoundsDir, "external"));
        }

        Stopwatch copyStopwatch = Stopwatch.StartNew();
        File.Copy(sourcePath, destPath, true);
        copyStopwatch.Stop();
        externalCopyTicks += copyStopwatch.ElapsedTicks;
        externalCopyCount++;
        externalCopyBytes += new FileInfo(destPath).Length;
    }

    if (isEmbedded)
    {
        byte[] soundData = GetSoundData(sound);
        Stopwatch writeStopwatch = Stopwatch.StartNew();
        File.WriteAllBytes(soundFilePath + audioExt, soundData);
        writeStopwatch.Stop();
        embeddedWriteTicks += writeStopwatch.ElapsedTicks;
        embeddedWriteCount++;
        embeddedWriteBytes += soundData.Length;
    }

    IncProgressLocal();
}

double Ms(long ticks)
{
    return Math.Round((ticks * 1000.0) / Stopwatch.Frequency, 3);
}

void WriteProfile()
{
    string profilePath = Paths.JoinVerifyWithinDirectory(exportedSoundsDir, "_export_profile.txt");
    StringBuilder builder = new();
    builder.AppendLine("ExportAllSounds profile");
    builder.AppendLine($"totalMs={Math.Round(totalStopwatch.Elapsed.TotalMilliseconds, 3)}");
    builder.AppendLine($"soundsVisited={soundsVisited}");
    builder.AppendLine($"nullSoundCount={nullSoundCount}");
    builder.AppendLine($"embeddedWriteCount={embeddedWriteCount}");
    builder.AppendLine($"externalCopyCount={externalCopyCount}");
    builder.AppendLine($"wavWriteCount={wavWriteCount}");
    builder.AppendLine($"oggWriteCount={oggWriteCount}");
    builder.AppendLine($"fallbackWriteCount={fallbackWriteCount}");
    builder.AppendLine($"audioGroupLoadCount={audioGroupLoadCount}");
    builder.AppendLine($"embeddedWriteBytes={embeddedWriteBytes}");
    builder.AppendLine($"externalCopyBytes={externalCopyBytes}");
    builder.AppendLine($"pathMs={Ms(pathTicks)}");
    builder.AppendLine($"embeddedResolveMs={Ms(embeddedResolveTicks)}");
    builder.AppendLine($"embeddedWriteMs={Ms(embeddedWriteTicks)}");
    builder.AppendLine($"externalCopyMs={Ms(externalCopyTicks)}");
    builder.AppendLine($"audioGroupLoadMs={Ms(audioGroupLoadTicks)}");
    File.WriteAllText(profilePath, builder.ToString());
    ScriptMessage($"Profile written to {profilePath}");
}
