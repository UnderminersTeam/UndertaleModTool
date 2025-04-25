using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Underanalyzer.Decompiler;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Project.SerializableAssets;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project;

/// <summary>
/// Represents a context around a specific (mod) project, as exists on disk.
/// </summary>
public sealed class ProjectContext
{
    /// <summary>
    /// Name of the project.
    /// </summary>
    public string Name { get => _mainOptions.Name; }

    /// <summary>
    /// Current data file path associated with this project, for loading only.
    /// </summary>
    public string LoadDataPath { get; }

    /// <summary>
    /// Current data file path to save to, when saving the game data (not the project itself).
    /// </summary>
    public string SaveDataPath { get; }

    /// <summary>
    /// Whether this context has any unexported assets.
    /// </summary>
    public bool HasUnexportedAssets { get => _assetsMarkedForExport.Count > 0; }

    /// <summary>
    /// Event for when any assets are added to or removed from this project context.
    /// </summary>
    public event EventHandler UnexportedAssetsChanged;

    /// <summary>
    /// This action should be called when main-thread operations need to occur.
    /// </summary>
    internal Action<Action> MainThreadAction { get; set; } = static (f) => f();

    /// <summary>
    /// Current data context associated with this project.
    /// </summary>
    internal UndertaleData Data { get; }

    /// <summary>
    /// Lookup of asset paths that existed when loading on disk, by data name and asset type.
    /// </summary>
    internal IReadOnlyDictionary<(string DataName, SerializableAssetType AssetType), string> AssetDataNamesToPaths => _assetDataNamesToPaths;

    /// <summary>
    /// Texture worker used during exporting.
    /// </summary>
    internal TextureWorker TextureWorker { get; set; }

    /// <summary>
    /// Generic options to use for writing JSON.
    /// </summary>
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Relevant project paths
    private readonly string _mainFilePath;
    private readonly string _mainDirectory;

    // Main options of the project
    private readonly ProjectMainOptions _mainOptions;

    // Lookup of asset paths that existed when loading on disk, by data name and asset type
    private readonly Dictionary<(string DataName, SerializableAssetType AssetType), string> _assetDataNamesToPaths = new(128);

    // Set of all assets in the current data that are marked for export
    private readonly HashSet<IProjectAsset> _assetsMarkedForExport = new(64);

    // Set of all project source code, as associated with UndertaleCode objects
    private readonly Dictionary<UndertaleCode, string> _codeSources = new(64);

    // During project loading and saving *only*, this caches audio group data
    private Dictionary<int, UndertaleData> _audioGroups = null;

    // During project loading *only*, this holds a set of Filename strings on SerializableSound,
    // for duplicate detection (to catch this user error early)
    private HashSet<string> _streamedSoundFilenames = null;

    /// <summary>
    /// Initializes a project context based on its existing main file path, and imports all of its data.
    /// </summary>
    /// <param name="currentData">Current data context to associate with the project.</param>
    /// <param name="loadedDataPath">Path of the data file that was loaded for the current data context.</param>
    /// <param name="savingDataPath">Path of the data file to be saved to.</param>
    /// <param name="mainFilePath">Main file path for the project.</param>
    /// <param name="mainThreadAction">
    /// For operations that should occur on the main thread, this will be 
    /// called to invoke those operations, if provided. This will stick around
    /// for the lifetime of the project context.
    /// </param>
    /// <exception cref="ProjectException">When a project-specific exception occurs</exception>
    public ProjectContext(UndertaleData currentData, string loadedDataPath, string savingDataPath, string mainFilePath, Action<Action> mainThreadAction = null)
    {
        Data = currentData;
        LoadDataPath = loadedDataPath;
        SaveDataPath = savingDataPath;
        _mainFilePath = mainFilePath;
        if (mainThreadAction is not null)
        {
            MainThreadAction = mainThreadAction;
        }
        using (FileStream fs = new(mainFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            _mainOptions = JsonSerializer.Deserialize<ProjectMainOptions>(fs, JsonOptions);
        }
        _mainDirectory = Path.GetDirectoryName(mainFilePath);

        // Initialize loading structures
        _audioGroups = new(currentData.AudioGroups?.Count ?? 4);
        _streamedSoundFilenames = new(16);

        // Recursively find and load in all assets in subdirectories
        List<ISerializableProjectAsset> loadedAssets = new(128);
        List<SerializableCode> loadedCodeAssets = new(64);
        List<ISerializableTextureProjectAsset> loadedTextureAssets = new(64);
        HashSet<string> excludeDirectorySet = new(_mainOptions.ExcludeDirectories);
        foreach (string directory in Directory.EnumerateDirectories(_mainDirectory))
        {
            // Skip directories that are irregular, start with ".", or are excluded based on main options
            DirectoryInfo info = new(directory);
            if (info.Attributes.HasFlag(FileAttributes.Hidden) || info.Attributes.HasFlag(FileAttributes.ReadOnly) || 
                info.Attributes.HasFlag(FileAttributes.System))
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
                string jsonPath = Path.Combine(fileDirectory, $"{filename}.json");
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
                using FileStream stream = new(
                    Path.Combine(Path.GetDirectoryName(SaveDataPath), $"audiogroup{groupId}.dat"), FileMode.Create, FileAccess.Write);
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
    /// Initializes a project context for a new project based on its main file path, and a name to give to it.
    /// </summary>
    /// <param name="currentData">Current data context to associate with the project.</param>
    /// <param name="loadedDataPath">Path of the data file that was loaded for the current data context.</param>
    /// <param name="savingDataPath">Path of the data file to be saved to.</param>
    /// <param name="mainFilePath">Main file path for the project.</param>
    /// <param name="newProjectName">Name of the new project being created.</param>
    /// <param name="mainThreadAction">
    /// For operations that should occur on the main thread, this will be 
    /// called to invoke those operations, if provided. This will stick around
    /// for the lifetime of the project context.
    /// </param>
    /// <exception cref="ProjectException">When a project-specific exception occurs</exception>
    public ProjectContext(UndertaleData currentData, string loadedDataPath, string savingDataPath, string mainFilePath, 
                          string newProjectName, Action<Action> mainThreadAction = null)
    {
        Data = currentData;
        LoadDataPath = loadedDataPath;
        SaveDataPath = savingDataPath;
        _mainFilePath = mainFilePath;
        if (mainThreadAction is not null)
        {
            MainThreadAction = mainThreadAction;
        }
        _mainDirectory = Path.GetDirectoryName(mainFilePath);

        // If the file already exists, we cannot overwrite it (give a friendly message)
        if (File.Exists(mainFilePath))
        {
            throw new ProjectException($"Project file already exists at \"{mainFilePath}\"");
        }

        // If the directory isn't empty, we don't want to overwrite anything else accidentally
        Directory.CreateDirectory(_mainDirectory);
        if (Directory.EnumerateFileSystemEntries(_mainDirectory).Any())
        {
            throw new ProjectException("Project directory is not empty");
        }

        // Create new main options and save it
        _mainOptions = new()
        {
            Name = newProjectName
        };
        using FileStream fs = new(mainFilePath, FileMode.CreateNew);
        JsonSerializer.Serialize(fs, _mainOptions, JsonOptions);
    }

    /// <summary>
    /// Marks an asset for export.
    /// </summary>
    /// <param name="asset">Asset to mark for export.</param>
    /// <returns>If the asset was not marked for export previously, returns <see langword="true"/>; <see langword="false"/> otherwise.</returns>
    public bool MarkAssetForExport(IProjectAsset asset)
    {
        // First, check if asset is eligible for export
        if (!asset.ProjectExportable)
        {
            return false;
        }

        // Try to mark, and return whether anything was changed
        if (_assetsMarkedForExport.Add(asset))
        {
            MainThreadAction(() =>
            {
                // Trigger event
                UnexportedAssetsChanged?.Invoke(this, new());
            });

            return true;
        }
        return false;
    }

    /// <summary>
    /// Unmarks an asset for export.
    /// </summary>
    /// <param name="asset">Asset to unmark for export.</param>
    /// <returns>If the asset was marked for export previously, returns <see langword="true"/>; <see langword="false"/> otherwise.</returns>
    public bool UnmarkAssetForExport(IProjectAsset asset)
    {
        if (_assetsMarkedForExport.Remove(asset))
        {
            MainThreadAction(() =>
            {
                // Trigger event
                UnexportedAssetsChanged?.Invoke(this, new());
            });

            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns whether the given asset is marked for export currently.
    /// </summary>
    /// <param name="asset">Asset to check whether marked for export.</param>
    /// <returns>If the asset is marked for export, returns <see langword="true"/>; <see langword="false"/> otherwise.</returns>
    public bool IsAssetMarkedForExport(IProjectAsset asset)
    {
        return _assetsMarkedForExport.Contains(asset);
    }

    /// <summary>
    /// Returns an enumerable over all currently unexported assets, in an arbitrary order.
    /// </summary>
    public IEnumerable<IProjectAsset> EnumerateUnexportedAssets()
    {
        return _assetsMarkedForExport;
    }

    /// <summary>
    /// Attempts to retrieve project source code for the given code entry.
    /// </summary>
    /// <param name="codeEntry">Code entry to look for source code for.</param>
    /// <param name="source">Output for source code, when found; <see langword="null"/> if nothing is found.</param>
    /// <returns><see langword="true"/> if source was successfully retrived; <see langword="false"/> otherwise.</returns>
    public bool TryGetCodeSource(UndertaleCode codeEntry, [NotNullWhen(true)] out string source)
    {
        return _codeSources.TryGetValue(codeEntry, out source);
    }

    /// <summary>
    /// Updates the source code associated with the given code entry.
    /// </summary>
    /// <param name="codeEntry">Code entry to update the associated source code for.</param>
    /// <param name="newSource">New source code to use.</param>
    public void UpdateCodeSource(UndertaleCode codeEntry, string newSource)
    {
        _codeSources[codeEntry] = newSource;
    }

    /// <summary>
    /// Exports all assets that are marked for export.
    /// </summary>
    /// <param name="clearMarkedAssets">Whether to clear the current set of assets marked for export.</param>
    /// <exception cref="ProjectException">When a project-specific exception occurs</exception>
    public void Export(bool clearMarkedAssets)
    {
        // Ensure project file still exists, just in case the user did something strange...
        if (!File.Exists(_mainFilePath))
        {
            throw new ProjectException($"Main project file no longer exists at \"{_mainFilePath}\"");
        }

        // Initialize saving structures
        _audioGroups = new(Data.AudioGroups?.Count ?? 4);

        // Before main asset export, ensure all code entries have source code - if they don't, use the decompiler to generate some
        GlobalDecompileContext globalDecompileContext = null;
        foreach (IProjectAsset asset in _assetsMarkedForExport)
        {
            if (asset is UndertaleCode code)
            {
                if (!_codeSources.ContainsKey(code))
                {
                    globalDecompileContext ??= new(Data);
                    try
                    {
                        string source = new DecompileContext(globalDecompileContext, code, Data.ToolInfo.DecompilerSettings).DecompileToString();
                        _codeSources[code] = source;
                    }
                    catch (Exception e)
                    {
                        _codeSources[code] = 
                            $"""
                            /*
                            Decompiler failed on project export.
                            Exception details:
                            {e}
                            */
                            """;
                    }
                }
            }
        }

        // Initialize texture worker for any image exports
        TextureWorker = new();

        // Export all assets that are marked as such
        try
        {
            foreach (IProjectAsset asset in _assetsMarkedForExport)
            {
                // If the asset isn't exportable anymore, throw an exception
                if (!asset.ProjectExportable)
                {
                    throw new ProjectException($"Project asset \"{asset.ProjectName}\" cannot currently be exported");
                }

                // Generate serializable version of the asset
                ISerializableProjectAsset serializableAsset = asset.GenerateSerializableProjectAsset(this);

                // Figure out a destination file path
                string destinationFile;
                if (_assetDataNamesToPaths.TryGetValue((serializableAsset.DataName, serializableAsset.AssetType), out string existingPath) &&
                    File.Exists(existingPath))
                {
                    // Existing file path existed from project load, and the file still exists; use that again
                    destinationFile = existingPath;
                }
                else
                {
                    // Generate new path
                    string friendlyName = MakeValidFilenameIdentifier(serializableAsset.AssetType, serializableAsset.DataName);
                    if (serializableAsset.IndividualDirectory)
                    {
                        // Asset needs its own directory
                        destinationFile = Path.Combine(_mainDirectory, serializableAsset.AssetType.ToFilesystemNamePlural(), friendlyName, $"{friendlyName}.json");
                    }
                    else
                    {
                        // Asset doesn't need its own directory
                        destinationFile = Path.Combine(_mainDirectory, serializableAsset.AssetType.ToFilesystemNamePlural(), $"{friendlyName}.json");
                    }

                    // If file already exists, add a suffix until there is no conflict
                    int attempts = 0;
                    while (File.Exists(destinationFile) && attempts < 10)
                    {
                        string directory = Path.GetDirectoryName(destinationFile);
                        destinationFile = Path.Combine(directory, $"{friendlyName}_{attempts + 2}.json");
                        attempts++;
                    }
                    if (attempts > 0 && File.Exists(destinationFile))
                    {
                        throw new ProjectException($"Too many naming conflicts for \"{friendlyName}\"");
                    }
                }

                // Ensure directories are created for this asset
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                // If the serializable asset name is identical to the data name, it can be omitted for ease of renaming/copying
                if (serializableAsset.DataName == Path.GetFileNameWithoutExtension(destinationFile))
                {
                    serializableAsset.DataName = null;
                }

                // Write out asset to disk
                serializableAsset.Serialize(this, destinationFile);
            }
        }
        finally
        {
            // Dispose of texture worker
            TextureWorker?.Dispose();
            TextureWorker = null;
        }

        // Clear out all assets marked for export, if desired
        if (clearMarkedAssets)
        {
            _assetsMarkedForExport.Clear();

            MainThreadAction(() =>
            {
                // Trigger event
                UnexportedAssetsChanged?.Invoke(this, new());
            });
        }

        // Clean up saving structures
        _audioGroups = null;
    }

    /// <summary>
    /// Returns a version of given string as a valid identifier to be used in a filename.
    /// </summary>
    private static string MakeValidFilenameIdentifier(SerializableAssetType assetType, string text)
    {
        // Ensure not empty/whitespace
        if (string.IsNullOrWhiteSpace(text))
        {
            return $"unknown_{assetType.ToFilesystemNameSingular()}_name";
        }

        // If length is way too long, it needs to be shortened
        const int lengthLimit = 100;
        if (text.Length >= lengthLimit)
        {
            text = text[..lengthLimit];
        }

        // Ensure first letter is an ASCII letter or an underscore
        char firstChar = text[0];
        if (!char.IsAsciiLetter(firstChar) && firstChar != '_')
        {
            // Replace first character with an underscore
            text = "_" + text[1..];
        }

        // Replace every other invalid character
        for (int i = 1; i < text.Length; i++)
        {
            char c = text[i];
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
            {
                text = text[1..i] + "_" + text[(i + 1)..];
            }
        }

        // Everything should now be valid
        return text;
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
            if (int.TryParse(gameObjectNameOrNull, out int fallbackIndex) && fallbackIndex >= 0 && fallbackIndex < Data.GameObjects.Count)
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

    /// <summary>
    /// Requests the <see cref="UndertaleData"/> for an audiogroup, from the loaded or saving data file's directory. Only works during loading a project.
    /// </summary>
    internal UndertaleData RequestAudiogroup(int index, bool fromLoadedDirectory, ISerializableProjectAsset forAsset)
    {
        // Check if cached, and return if so
        if (_audioGroups.TryGetValue(index, out UndertaleData cached))
        {
            return cached;
        }

        // Not cached - perform full load
        string filename = $"audiogroup{index}.dat";
        string path = Path.Combine(Path.GetDirectoryName(fromLoadedDirectory ? LoadDataPath : SaveDataPath), filename);
        if (File.Exists(path))
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                // Read and cache data
                return _audioGroups[index] = UndertaleIO.Read(stream, (string warning, bool _) =>
                {
                    throw new ProjectException($"Warning occurred when loading audio group {index}: {warning}");
                });
            }
            catch (ProjectException)
            {
                // Propagate project-specific exceptions up
                throw;
            }
            catch (Exception e)
            {
                // Wrap all other exceptions
                throw new ProjectException($"Error occurred when loading audio group {index}: {e}", e);
            }
        }
        else
        {
            throw new ProjectException($"Failed to find file \"{filename}\" for \"{forAsset.DataName}\"");
        }
    }

    /// <summary>
    /// Copies an audio file from the source path, to the destination filename (relative to <see cref="SaveDataPath"/>'s directory).
    /// </summary>
    /// <remarks>
    /// Only should be used during project loading.
    /// </remarks>
    internal void CopyStreamedSoundToSaveDirectory(string destinationFilename, string sourcePath, SerializableSound forSound)
    {
        // Perform duplicate destination filename check
        if (!_streamedSoundFilenames.Add(destinationFilename))
        {
            throw new ProjectException($"Found duplicate Filename in JSON for streamed sound asset \"{forSound.DataName}\"");
        }

        // Perform copy (and overwrite)
        try
        {
            File.Copy(sourcePath, Path.Combine(Path.GetDirectoryName(SaveDataPath), destinationFilename), true);
        }
        catch (Exception e)
        {
            throw new ProjectException($"Failed to copy streamed audio file for \"{forSound.DataName}\": {e}", e);
        }
    }
}
