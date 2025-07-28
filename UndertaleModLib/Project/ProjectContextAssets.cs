using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;
using UndertaleModLib.Project.SerializableAssets;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project;

partial class ProjectContext
{
    /// <summary>
    /// Performs the main project asset import operations.
    /// </summary>
    private void LoadProjectAssets()
    {
        // Ensure data is actually loaded
        if (Data is null)
        {
            throw new InvalidOperationException("Attempting to load project assets with no loaded game data");
        }

        // Initialize loading structures
        _audioGroups = new(Data.AudioGroups?.Count ?? 4);
        _streamedSoundFilenames = new(16);

        // Recursively find and load in all assets in subdirectories
        List<ISerializableProjectAsset> loadedAssets = new(128);
        List<SerializableCode> loadedCodeAssets = new(64);
        List<ISerializableTextureProjectAsset> loadedTextureAssets = new(64);
        HashSet<string> excludeDirectorySet = [.. _mainOptions.ExcludeDirectories];
        foreach (string directory in Directory.EnumerateDirectories(_mainDirectory))
        {
            // Skip directories that are irregular, start with ".", or are excluded based on main options
            DirectoryInfo info = new(directory);
            if (info.Attributes.HasFlag(FileAttributes.Hidden) || info.Attributes.HasFlag(FileAttributes.System))
            {
                continue;
            }
            if (info.Name.StartsWith('.'))
            {
                continue;
            }
            if (excludeDirectorySet.Contains(info.Name))
            {
                continue;
            }

            // Iterate over all JSON files in this directory
            foreach (string assetPath in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories))
            {
                // Read in asset JSON
                ISerializableProjectAsset asset;
                try
                {
                    using FileStream fs = new(assetPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    asset = JsonSerializer.Deserialize<ISerializableProjectAsset>(fs, JsonOptions);
                }
                catch (Exception e)
                {
                    throw new ProjectException($"Failed to load asset file \"{Path.GetFileName(assetPath)}\": {e.Message}", e);
                }

                // Add to list for later processing
                loadedAssets.Add(asset);
                if (asset is SerializableCode codeAsset)
                {
                    loadedCodeAssets.Add(codeAsset);
                }
                else if (asset is ISerializableTextureProjectAsset textureAsset)
                {
                    loadedTextureAssets.Add(textureAsset);
                }

                // If asset's data name is omitted, use the filename
                asset.DataName ??= Path.GetFileNameWithoutExtension(assetPath);

                // Associate the data name (and type) of this asset with its path
                if (!_assetDataNamesToPaths.TryAdd((asset.DataName, asset.AssetType), assetPath))
                {
                    throw new ProjectException($"Found multiple {asset.AssetType.ToFilesystemNameSingular()} assets with name \"{asset.DataName}\"");
                }
            }

            // Iterate over all GML files in this directory
            foreach (string assetPath in Directory.EnumerateFiles(directory, "*.gml", SearchOption.AllDirectories))
            {
                // If there's already a JSON file with the same filename as this one, ignore this GML file
                string filename = Path.GetFileNameWithoutExtension(assetPath);
                string fileDirectory = Path.GetDirectoryName(assetPath);
                string jsonPath = Path.Join(fileDirectory, $"{filename}.json");
                if (File.Exists(jsonPath))
                {
                    continue;
                }

                // Create blank code asset for this
                SerializableCode asset = new()
                {
                    DataName = filename,
                    WeirdLocalFlag = false
                };

                // Add to list for later processing
                loadedAssets.Add(asset);
                loadedCodeAssets.Add(asset);

                // Associate the data name (and type) of this asset with its path.
                // Note that we store a theoretical JSON file path, which doesn't actually exist - just in case a JSON is required later.
                if (!_assetDataNamesToPaths.TryAdd((asset.DataName, asset.AssetType), jsonPath))
                {
                    throw new ProjectException($"Found multiple {asset.AssetType.ToFilesystemNameSingular()} assets with name \"{asset.DataName}\"");
                }
            }
        }

        // Sort all assets so that they import deterministically
        loadedAssets.Sort(CompareSerializableAssets);
        loadedCodeAssets.Sort(CompareSerializableAssets);
        loadedTextureAssets.Sort(CompareSerializableAssets);

        // Perform pre-import on all loaded assets
        MainThreadAction(() =>
        {
            foreach (ISerializableProjectAsset asset in loadedAssets)
            {
                asset.PreImport(this);
            }
        });

        // Import code
        CodeImportGroup importGroup = new(Data)
        {
            AutoCreateAssets = false,
            MainThreadAction = MainThreadAction
        };
        foreach (SerializableCode asset in loadedCodeAssets)
        {
            asset.ImportCode(this, importGroup);
        }
        try
        {
            importGroup.Import(true);
        }
        catch (Exception e)
        {
            throw new ProjectException(e.Message, e);
        }

        // Pack textures
        // TODO: parallelize based on texture groups, and use their settings for the packer
        //       may need to move this out of the constructor for that...
        TextureGroupPacker packer = new();
        foreach (ISerializableTextureProjectAsset asset in loadedTextureAssets)
        {
            asset.ImportTextures(this, packer);
        }
        packer.PackPages();

        // Perform final import on all loaded assets
        MainThreadAction(() =>
        {
            packer.ImportToData(Data);
            foreach (ISerializableProjectAsset asset in loadedAssets)
            {
                asset.Import(this);
            }
        });

        // Save all audio groups that were loaded during import
        foreach ((int groupId, UndertaleData group) in _audioGroups)
        {
            try
            {
                string relativeAudioGroupPath;
                if (groupId < Data.AudioGroups.Count && Data.AudioGroups[groupId] is UndertaleAudioGroup { Path.Content: string customRelativePath })
                {
                    relativeAudioGroupPath = customRelativePath;
                }
                else
                {
                    relativeAudioGroupPath = $"audiogroup{groupId}.dat";
                }
                string fullAudioGroupPath = Path.Join(SaveDirectory, relativeAudioGroupPath);
                Paths.VerifyWithinDirectory(SaveDirectory, fullAudioGroupPath);
                FileBackup.BackupFile(fullAudioGroupPath);
                using FileStream stream = new(fullAudioGroupPath, FileMode.Create, FileAccess.Write);
                UndertaleIO.Write(stream, group);
            }
            catch (ProjectException)
            {
                // Propagate project-specific exceptions up
                throw;
            }
            catch (Exception e)
            {
                // Wrap all other exceptions
                throw new ProjectException($"Error occurred when saving audio group {groupId} during import: {e}", e);
            }
        }

        // Clean up loading structures
        _audioGroups = null;
        _streamedSoundFilenames = null;
    }

    /// <summary>
    /// Comparer for two serializable project assets, for deterministic imports.
    /// </summary>
    private int CompareSerializableAssets(ISerializableProjectAsset a, ISerializableProjectAsset b)
    {
        if (a.OverrideOrder < b.OverrideOrder)
        {
            return -1;
        }
        if (a.OverrideOrder > b.OverrideOrder)
        {
            return 1;
        }
        return a.DataName.CompareTo(b.DataName);
    }

    /// <summary>
    /// Tries to find a sprite with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Sprite that was found, or null.</returns>
    internal UndertaleSprite FindSprite(string spriteNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(spriteNameOrNull))
        {
            return null;
        }

        return Data.Sprites.ByName(spriteNameOrNull) ??
            throw new ProjectException($"Failed to find sprite \"{spriteNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find a background with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Background that was found, or null.</returns>
    internal UndertaleBackground FindBackground(string backgroundNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(backgroundNameOrNull))
        {
            return null;
        }

        return Data.Backgrounds.ByName(backgroundNameOrNull) ??
            throw new ProjectException($"Failed to find background \"{backgroundNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find a font with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Font that was found, or null.</returns>
    internal UndertaleFont FindFont(string fontNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(fontNameOrNull))
        {
            return null;
        }

        return Data.Fonts.ByName(fontNameOrNull) ??
            throw new ProjectException($"Failed to find font \"{fontNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find a font index with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Font index that was found. If not found, an exception is thrown.</returns>
    internal int FindFontIndex(string fontNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(fontNameOrNull))
        {
            throw new ProjectException($"No font name specified in property of \"{forAsset.DataName}\"");
        }

        int index = Data.Fonts.IndexOfName(fontNameOrNull);
        if (index < 0)
        {
            // Fallback option: parse integer and use that
            if (int.TryParse(fontNameOrNull, out int fallbackIndex) && fallbackIndex >= 0 && fallbackIndex < Data.Fonts.Count &&
                Data.Fonts[fallbackIndex] is not null)
            {
                return fallbackIndex;
            }

            throw new ProjectException($"Failed to find font \"{fontNameOrNull}\" for \"{forAsset.DataName}\"");
        }
        return index;
    }

    /// <summary>
    /// Tries to find a game object with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Game object that was found, or null.</returns>
    internal UndertaleGameObject FindGameObject(string gameObjectNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(gameObjectNameOrNull))
        {
            return null;
        }

        return Data.GameObjects.ByName(gameObjectNameOrNull) ??
            throw new ProjectException($"Failed to find object \"{gameObjectNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find a game object index with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Game object index that was found. If not found, an exception is thrown.</returns>
    internal int FindGameObjectIndex(string gameObjectNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(gameObjectNameOrNull))
        {
            throw new ProjectException($"No object name specified in property of \"{forAsset.DataName}\"");
        }

        int index = Data.GameObjects.IndexOfName(gameObjectNameOrNull);
        if (index < 0)
        {
            // Fallback option: parse integer and use that
            if (int.TryParse(gameObjectNameOrNull, out int fallbackIndex) && fallbackIndex >= 0 && fallbackIndex < Data.GameObjects.Count &&
                Data.GameObjects[fallbackIndex] is not null)
            {
                return fallbackIndex;
            }

            throw new ProjectException($"Failed to find object \"{gameObjectNameOrNull}\" for \"{forAsset.DataName}\"");
        }
        return index;
    }

    /// <summary>
    /// Tries to find a code entry with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Code entry that was found, or null.</returns>
    internal UndertaleCode FindCode(string codeEntryNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(codeEntryNameOrNull))
        {
            return null;
        }

        return Data.Code.ByName(codeEntryNameOrNull) ??
            throw new ProjectException($"Failed to find code entry \"{codeEntryNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find a sequence with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Sequence that was found, or null.</returns>
    internal UndertaleSequence FindSequence(string sequenceNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(sequenceNameOrNull))
        {
            return null;
        }

        return Data.Sequences.ByName(sequenceNameOrNull) ??
            throw new ProjectException($"Failed to find sequence \"{sequenceNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find a particle system with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Particle system that was found, or null.</returns>
    internal UndertaleParticleSystem FindParticleSystem(string particleSystemNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(particleSystemNameOrNull))
        {
            return null;
        }

        return Data.ParticleSystems.ByName(particleSystemNameOrNull) ??
            throw new ProjectException($"Failed to find particle system \"{particleSystemNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find a sound with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Sound that was found, or null.</returns>
    internal UndertaleSound FindSound(string soundNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(soundNameOrNull))
        {
            return null;
        }

        return Data.Sounds.ByName(soundNameOrNull) ??
            throw new ProjectException($"Failed to find sound \"{soundNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Tries to find an animation curve with the given name, if not null or whitespace, for the given serializable project asset.
    /// </summary>
    /// <returns>Animation curve that was found, or null.</returns>
    internal UndertaleAnimationCurve FindAnimationCurve(string animCurveNameOrNull, ISerializableProjectAsset forAsset)
    {
        if (string.IsNullOrWhiteSpace(animCurveNameOrNull))
        {
            return null;
        }

        return Data.AnimationCurves.ByName(animCurveNameOrNull) ??
            throw new ProjectException($"Failed to find animation curve \"{animCurveNameOrNull}\" for \"{forAsset.DataName}\"");
    }

    /// <summary>
    /// Finds or creates a string with the given string contents, or returns <see langword="null"/> if <paramref name="contents"/> is <see langword="null"/>.
    /// </summary>
    internal UndertaleString MakeString(string contents)
    {
        if (contents is null)
        {
            return null;
        }

        // TODO: possibly more optimized lookup later
        return Data.Strings.MakeString(contents);
    }
}
