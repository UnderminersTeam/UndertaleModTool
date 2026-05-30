using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using UndertaleModLib.Scripting;

EnsureDataLoaded();

var builder = CreateScriptOptionsBuilder()
    .AddDirectory("folder", "Output Folder:")
    .AddText("patterns", "Names (one per line, leave empty for all):", multiline: true)
    .AddRadio("filterMode", "Filter mode:", "Exact", "Regex", "Wildcard")
    .AddBool("external", "Export external audio files as well? (Will copy to a separate folder.)");

if ((Data.AudioGroups?.Count ?? 0) > 0)
    builder.AddBool("grouped", "Group sounds by audio group");

var result = ShowScriptOptionsDialog("Export Sounds", builder);
if (result is null) return;

string exportedSoundsDir = result["folder"] as string;

if (!Directory.Exists(exportedSoundsDir))
{
    ScriptError("The specified output folder does not exist.");
    return;
}

string rawPatterns = result["patterns"] as string;
bool exportAll = string.IsNullOrWhiteSpace(rawPatterns);
string[] patterns = rawPatterns.Split("\n", StringSplitOptions.RemoveEmptyEntries);
NameFilterMode filterMode = Enum.Parse<NameFilterMode>(result["filterMode"] as string);

bool copyExternalAudio = result["external"] as bool? == true;
bool groupedExport = (Data.AudioGroups?.Count ?? 0) > 0 && result["grouped"] as bool? == true;

byte[] EMPTY_WAV_FILE_BYTES = System.Convert.FromBase64String("UklGRiQAAABXQVZFZm10IBAAAAABAAIAQB8AAAB9AAAEABAAZGF0YQAAAAA=");
string DEFAULT_AUDIOGROUP_NAME = "audiogroup_default";

int maxCount = Data.Sounds.Count;
SetProgressBar(null, "Sounds", 0, maxCount);
StartProgressBarUpdater();

await Task.Run(DumpSounds);

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

    string audioGroupName = sound.AudioGroup is not null ? sound.AudioGroup.Name.Content : DEFAULT_AUDIOGROUP_NAME;
    if (loadedAudioGroups.ContainsKey(audioGroupName))
    {
        return loadedAudioGroups[audioGroupName];
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
    if (!exportAll)
    {
        bool match = false;
        foreach (string pattern in patterns)
        {
            if (NameFilter.IsMatch(sound.Name.Content, pattern, filterMode))
            {
                match = true;
                break;
            }
        }
        if (!match) { IncProgressLocal(); return; }
    }

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

        if (copyExternalAudio)
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
            File.Copy(sourcePath, destPath, true);
        }
    }
    if (isEmbedded)
    {
        File.WriteAllBytes(soundFilePath + audioExt, GetSoundData(sound));
    }

    IncProgressLocal();
}
