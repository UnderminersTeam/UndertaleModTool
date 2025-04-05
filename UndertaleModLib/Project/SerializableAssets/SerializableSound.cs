using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSound;
using System;
using System.Collections.Generic;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleSound"/>.
/// </summary>
internal sealed class SerializableSound : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Sound;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => true;

    /// <inheritdoc cref="UndertaleSound.File"/>
    public string Filename { get; set; }

    /// <inheritdoc cref="UndertaleSound.Type"/>
    public string Type { get; set; }

    /// <inheritdoc cref="UndertaleSound.Volume"/>
    public float Volume { get; set; }

    /// <inheritdoc cref="UndertaleSound.Pitch"/>
    public float Pitch { get; set; }

    /// <summary>
    /// Whether the sound file is streamed externally from the main data file (or any audio group files).
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="DecompressOnLoad"/>.
    /// </remarks>
    public bool Streamed { get; set; }

    /// <summary>
    /// Whether the sound if decompressed on load, if the sound data is OGG.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="Streamed"/>.
    /// </remarks>
    public bool DecompressOnLoad { get; set; }

    /// <inheritdoc cref="UndertaleSound.GroupID"/>
    public int GroupID { get; set; }

    /// <inheritdoc cref="UndertaleSound.Effects"/>
    public uint Effects { get; set; }

    /// <inheritdoc cref="UndertaleSound.AudioLength"/>
    public float AudioLength { get; set; }

    // Data asset that was located during pre-import, or during export.
    private UndertaleSound _dataAsset = null;

    /// <summary>
    /// Populates this serializable sound with data from an actual sound.
    /// </summary>
    public void PopulateFromData(ProjectContext projectContext, UndertaleSound sound)
    {
        // Update all main properties
        DataName = sound.Name.Content;
        Filename = sound.File.Content;
        Type = sound.Type?.Content;
        Volume = sound.Volume;
        Pitch = sound.Pitch;
        GroupID = sound.GroupID;
        Effects = sound.Effects;
        AudioLength = sound.AudioLength;

        // Streamed sounds *only* set the Regular flag
        Streamed = (sound.Flags == AudioEntryFlags.Regular);

        // Decompress-on-load sounds set *every* flag
        DecompressOnLoad = (sound.Flags == (AudioEntryFlags.IsEmbedded | AudioEntryFlags.IsCompressed | 
                                            AudioEntryFlags.IsDecompressedOnLoad | AudioEntryFlags.Regular));

        _dataAsset = sound;
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        // Export main JSON
        using FileStream fs = new(destinationFile, FileMode.Create);
        JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);

        // Export sound data/file itself
        string directory = Path.GetDirectoryName(destinationFile);
        string friendlyName = Path.GetFileNameWithoutExtension(destinationFile);
        if (Streamed)
        {
            // Get path for external sound being exported. Note that Filename can include subdirectories as well.
            string externalFilename = Filename;
            if (!externalFilename.Contains('.'))
            {
                // Add file extension if none already exists (assume OGG)
                externalFilename += ".ogg";
            }
            string externalPath = Path.Combine(Path.GetDirectoryName(projectContext.LoadDataPath), externalFilename);

            // Try to copy that sound to the project
            if (!File.Exists(externalPath))
            {
                throw new ProjectException($"Failed to find external sound to export with name \"{externalFilename}\"");
            }
            File.Copy(externalPath, Path.Combine(directory, Path.GetFileName(externalFilename)), true);
        }
        else if (_dataAsset.AudioFile is not null && (_dataAsset.AudioGroup is null || _dataAsset.GroupID == projectContext.Data.GetBuiltinSoundGroupID()))
        {
            // Determine file extension based on audio flags
            AudioEntryFlags wavFlags = AudioEntryFlags.IsEmbedded | AudioEntryFlags.Regular;
            string extension = ((_dataAsset.Flags & wavFlags) == wavFlags) ? ".wav" : ".ogg";

            // Save data to disk
            File.WriteAllBytes(Path.Combine(directory, $"{friendlyName}{extension}"), _dataAsset.AudioFile.Data);
        }
        else if (_dataAsset.AudioID != -1)
        {
            // Export embedded sound from external audio group file
            UndertaleData groupData = projectContext.RequestAudiogroup(_dataAsset.GroupID, false, this);
            int audioId = _dataAsset.AudioID;
            if (audioId >= (groupData.EmbeddedAudio?.Count ?? 0))
            {
                throw new ProjectException($"Failed to find embedded audio at index {audioId} in group {_dataAsset.GroupID} for \"{DataName}\"");
            }

            // Determine file extension based on audio flags
            AudioEntryFlags wavFlags = AudioEntryFlags.IsEmbedded | AudioEntryFlags.Regular;
            string extension = ((_dataAsset.Flags & wavFlags) == wavFlags) ? ".wav" : ".ogg";

            // Save data to disk
            File.WriteAllBytes(Path.Combine(directory, $"{friendlyName}{extension}"), groupData.EmbeddedAudio[audioId].Data);
        }
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Sounds.ByName(DataName) is UndertaleSound existing)
        {
            // Sound found
            _dataAsset = existing;
        }
        else
        {
            // No sound found; create new one
            _dataAsset = new()
            {
                Name = projectContext.Data.Strings.MakeString(DataName)
            };
            projectContext.Data.Sounds.Add(_dataAsset);
        }
    }

    // Order that file extensions will be searched for with the same base filename as the sound JSON
    private static readonly IReadOnlyList<string> _extensionSearchOrder = ["wav", "ogg", "mp3"];

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleSound sound = _dataAsset;

        // Update all main properties
        sound.File = projectContext.Data.Strings.MakeString(Filename);
        sound.Type = Type is null ? null : projectContext.Data.Strings.MakeString(Type);
        sound.Volume = Volume;
        sound.Pitch = Pitch;
        sound.Effects = Effects;
        sound.AudioLength = AudioLength;

        // Find file for sound
        if (!projectContext.AssetDataNamesToPaths.TryGetValue((DataName, AssetType), out string jsonFilename))
        {
            throw new ProjectException("Failed to get sound asset path");
        }
        string baseFilename = Path.Combine(Path.GetDirectoryName(jsonFilename), Path.GetFileNameWithoutExtension(jsonFilename));
        string filename = null;
        foreach (string extension in _extensionSearchOrder)
        {
            string attemptFilename = $"{baseFilename}.{extension}";
            if (File.Exists(attemptFilename))
            {
                filename = attemptFilename;
                break;
            }
        }
        if (filename is null)
        {
            throw new ProjectException($"Failed to find audio file for sound \"{DataName}\"");
        }
        bool isLossless = filename.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

        // Reconstruct flags and import sound data
        if (Streamed)
        {
            // Streamed audio can only be compressed - throw an error if it isn't
            if (isLossless)
            {
                throw new ProjectException(
                    $"Expected compressed audio file for streamed sound \"{DataName}\", found lossless audio file \"{Path.GetFileName(filename)}\"");
            }

            // Set flags accordingly
            sound.Flags = AudioEntryFlags.Regular;

            // Copy sound file to Filename destination
            projectContext.CopyStreamedSoundToSaveDirectory(Filename, filename, this);
        }
        else
        {
            if (DecompressOnLoad)
            {
                // Decompress-on-load audio can only be compressed - throw an error if it isn't
                if (isLossless)
                {
                    throw new ProjectException(
                        $"Expected compressed audio file for decompress-on-load sound \"{DataName}\", found lossless audio file \"{Path.GetFileName(filename)}\"");
                }

                // Set flags accordingly
                sound.Flags = AudioEntryFlags.IsEmbedded | AudioEntryFlags.IsCompressed |
                              AudioEntryFlags.IsDecompressedOnLoad | AudioEntryFlags.Regular;
            }
            else
            {
                // Set flags depending on whether compressed or not
                if (isLossless)
                {
                    sound.Flags = AudioEntryFlags.IsEmbedded | AudioEntryFlags.Regular;
                }
                else
                {
                    sound.Flags = AudioEntryFlags.IsCompressed | AudioEntryFlags.Regular;
                }
            }

            // Locate embedded audio (use existing if already assigned, otherwise create a new one)
            UndertaleEmbeddedAudio audio;
            if (sound.AudioFile is not null)
            {
                audio = sound.AudioFile;
            }
            else
            {
                // Not currently assigned to embedded audio
                if (GroupID != projectContext.Data.GetBuiltinSoundGroupID())
                {
                    // If sound is in an audio group, request it
                    UndertaleData groupData = projectContext.RequestAudiogroup(_dataAsset.GroupID, true, this);

                    // If the existing sound has the same group ID and an audio ID, reuse it
                    if (sound.GroupID == GroupID && sound.AudioID != -1)
                    {
                        // Ensure audio ID is valid before reusing it
                        int audioId = sound.AudioID;
                        if (audioId >= (groupData.EmbeddedAudio?.Count ?? 0))
                        {
                            throw new ProjectException($"Failed to find embedded audio at index {audioId} in group {_dataAsset.GroupID} for \"{DataName}\"");
                        }
                        audio = groupData.EmbeddedAudio[audioId];
                    }
                    else
                    {
                        // Nothing to reuse in the audio group; make new embedded audio
                        audio = new();
                        sound.AudioFile = null;
                        sound.AudioID = groupData.EmbeddedAudio.Count;
                        groupData.EmbeddedAudio.Add(audio);
                    }

                    // Assign to the corresponding audio group, if not the same one
                    if (sound.GroupID != GroupID)
                    {
                        sound.AudioGroup = null;
                        sound.GroupID = GroupID;
                    }
                }
                else
                {
                    // No group being used for this sound, and nothing to reuse; make new embedded audio
                    audio = new();
                    sound.AudioFile = audio;
                    sound.AudioID = projectContext.Data.EmbeddedAudio.Count;
                    projectContext.Data.EmbeddedAudio.Add(audio);
                }
            }

            // Read in data to the embedded audio
            try
            {
                audio.Data = File.ReadAllBytes(filename);
            }
            catch (Exception e)
            {
                throw new ProjectException($"Failed to read data from \"{filename}\": {e}", e);
            }
        }

        return sound;
    }
}
