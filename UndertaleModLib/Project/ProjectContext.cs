using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Underanalyzer.Decompiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project;

/// <summary>
/// Represents a context around a specific (mod) project, as exists on disk.
/// </summary>
public sealed partial class ProjectContext
{
    /// <summary>
    /// Name of the project.
    /// </summary>
    public string Name { get => _mainOptions?.Name ?? "(unknown project name)"; }

    /// <summary>
    /// Project flags, denoting certain features that the project may use (including custom ones, which start with an underscore).
    /// </summary>
    public List<string> Flags { get => _mainOptions?.Flags ?? []; }

    /// <summary>
    /// Whether script importing should be allowed on this project context.
    /// </summary>
    /// <remarks>
    /// This can be disabled as one layer of a security measure, but projects should already be trusted/audited before importing.
    /// </remarks>
    public bool AllowScripts { get; } = true;

    /// <summary>
    /// Current directory associated with loading data for this project. Must always be set.
    /// </summary>
    public string LoadDirectory { get; }

    /// <summary>
    /// Current directory associated with saving data for this project. Must always be set.
    /// </summary>
    public string SaveDirectory { get; }

    /// <summary>
    /// Current data file path associated with this project, for loading only, or <see langword="null"/> if none is assigned.
    /// </summary>
    public string LoadDataPath { get; private set; } = null;

    /// <summary>
    /// Current data file path to save to, when saving the game data (not the project itself), or <see langword="null"/> if none is assigned.
    /// </summary>
    public string SaveDataPath { get; private set; } = null;

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
    /// Current data context associated with this project, or <see langword="null"/> if none is assigned.
    /// </summary>
    internal UndertaleData Data { get; private set; } = null;

    /// <summary>
    /// Lookup of asset paths that existed when loading on disk, by data name and asset type.
    /// </summary>
    internal IReadOnlyDictionary<(string DataName, SerializableAssetType AssetType), string> AssetDataNamesToPaths => _assetDataNamesToPaths;

    /// <summary>
    /// Texture worker used during exporting.
    /// </summary>
    internal TextureWorker TextureWorker { get; set; }

    /// <summary>
    /// Game file backup instance.
    /// </summary>
    internal IGameFileBackup FileBackup { get; private set; }

    /// <summary>
    /// Generic options to use for writing JSON.
    /// </summary>
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Relevant project paths
    private readonly string _mainFilePath;
    private readonly string _mainDirectory;

    // Main options of the project
    private ProjectMainOptions _mainOptions = null;

    // Hashset of all fully-qualified paths to project JSONs, for when importing sub-projects (to avoid infinite recursion).
    // This is shared with parent projects, if a sub-project.
    private HashSet<string> _projectJsonPaths = null;

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
    /// Initializes a project context based on its existing main file path, as well as load/save directories.
    /// </summary>
    /// <param name="loadDirectory">Path of the directory for game data to be loaded from.</param>
    /// <param name="saveDirectory">Path of the directory for game data to be saved to.</param>
    /// <param name="mainFilePath">Main file path for the project.</param>
    private ProjectContext(string loadDirectory, string saveDirectory, string mainFilePath)
    {
        Data = null;
        LoadDirectory = Path.GetFullPath(loadDirectory);
        SaveDirectory = Path.GetFullPath(saveDirectory);
        _mainFilePath = mainFilePath;
        _mainDirectory = Path.GetFullPath(Path.GetDirectoryName(_mainFilePath));
    }

    /// <summary>
    /// Initializes a project context based on its existing main file path, as well as load/save directories.
    /// </summary>
    /// <param name="loadDirectory">Path of the directory for game data to be loaded from.</param>
    /// <param name="saveDirectory">Path of the directory for game data to be saved to.</param>
    /// <param name="mainFilePath">Main file path for the project.</param>
    public static ProjectContext CreateWithDirectories(string loadDirectory, string saveDirectory, string mainFilePath)
    {
        return new ProjectContext(loadDirectory, saveDirectory, mainFilePath);
    }

    /// <summary>
    /// Initializes a project context based on its existing main file path, as well as load/save data file paths.
    /// </summary>
    /// <param name="loadPath">Path of the data file for game data to be loaded from.</param>
    /// <param name="savePath">Path of the data file for game data to be saved to.</param>
    /// <param name="mainFilePath">Main file path for the project.</param>
    public static ProjectContext CreateWithDataFilePaths(string loadPath, string savePath, string mainFilePath)
    {
        return new ProjectContext(Path.GetDirectoryName(loadPath), Path.GetDirectoryName(savePath), mainFilePath)
        {
            LoadDataPath = Path.GetFullPath(loadPath),
            SaveDataPath = Path.GetFullPath(savePath)
        };
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
        LoadDirectory = Path.GetFullPath(Path.GetDirectoryName(loadedDataPath));
        SaveDirectory = Path.GetFullPath(Path.GetDirectoryName(savingDataPath));
        _mainFilePath = mainFilePath;
        if (mainThreadAction is not null)
        {
            MainThreadAction = mainThreadAction;
        }
        _mainDirectory = Path.GetFullPath(Path.GetDirectoryName(mainFilePath));

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
    /// Imports all of the data of the project.
    /// </summary>
    /// <param name="currentData">Current data context to associate with the project.</param>
    /// <param name="mainThreadAction">
    /// For operations that should occur on the main thread, this will be 
    /// called to invoke those operations, if provided. This will stick around
    /// for the lifetime of the project context.
    /// </param>
    /// <param name="existingFileBackup">
    /// If this project context is being invoked as part of larger changes to game data,
    /// or an alternative backup implementation is desired (including no-op),
    /// an existing <see cref="IGameFileBackup"/> instance can be used to back up files.
    /// </param>
    /// <exception cref="ProjectException">When a project-specific exception occurs</exception>
    public void Import(UndertaleData currentData = null, IGameFileBackup existingFileBackup = null, Action<Action> mainThreadAction = null)
    {
        // Ensure project is imported
        if (_mainOptions is not null)
        {
            throw new InvalidOperationException("Project has already been imported");
        }

        // Set main thread action, if supplied
        if (mainThreadAction is not null)
        {
            MainThreadAction = mainThreadAction;
        }

        // Load options
        using (FileStream fs = new(_mainFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            _mainOptions = JsonSerializer.Deserialize<ProjectMainOptions>(fs, JsonOptions);
        }

        // Handle options
        ProcessFlags();

        if (currentData is not null)
        {
            // Use already-loaded assets data file, if provided
            Data = currentData;
        }
        else if (TryFindFileFromPathList(_mainOptions.AssetsDataFilePath, out string loadDataPath, out string saveDataPath))
        {
            // No data currently loaded - adjust load/save data file paths if a valid one is found.
            // Note that the load/save *directories* are unchanged.
            LoadDataPath = loadDataPath;
            SaveDataPath = saveDataPath;
        }
        else if (!_mainOptions.AssetsDataFilePath.Empty)
        {
            // If no assets data file was found, and at least one valid path is specified, throw a useful error
            throw new ProjectException("Failed to locate a valid assets data file path");
        }

        try
        {
            // Either use existing or create a new file backup
            if (existingFileBackup is not null)
            {
                FileBackup = existingFileBackup;
            }
            else
            {
                CreateNewBackup();
            }

            // Perform import
            RunPreImportScripts();
            ImportSubProjects();
            ImportExternalFiles();
            ApplyFileOperations();
            ApplyFilePatches();
            if (LoadDataPath is not null)
            {
                if (Data is null)
                {
                    // Data should be ready for loading now
                    LoadAssetsDataFile();
                }
                RunPreAssetImportScripts();
                LoadProjectAssets();
            }
            RunPostImportScripts();
            DeinitializeScripting();
            if (SaveDataPath is not null)
            {
                FileBackup.BackupFile(SaveDataPath);
                if (Data is not null && currentData is null)
                {
                    // Data was loaded as part of this import, so save it back now, and release it
                    SaveAssetsDataFile();
                    Data.Dispose();
                    Data = null;
                }
            }
        }
        finally
        {
            // If not using an existing file backup instance, finish the backup process
            if (existingFileBackup is null)
            {
                FinishNewBackup();
            }
        }
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
        // Ensure project is imported
        if (_mainOptions is null)
        {
            throw new InvalidOperationException("Project has not yet been imported");
        }

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
                    (File.Exists(existingPath) || (asset is UndertaleCode && File.Exists(Path.ChangeExtension(existingPath, "gml")))))
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
                        destinationFile = Path.Join(_mainDirectory, serializableAsset.AssetType.ToFilesystemNamePlural(), friendlyName, $"{friendlyName}.json");
                    }
                    else
                    {
                        // Asset doesn't need its own directory
                        destinationFile = Path.Join(_mainDirectory, serializableAsset.AssetType.ToFilesystemNamePlural(), $"{friendlyName}.json");
                    }

                    // If file already exists, add a suffix until there is no conflict
                    int attempts = 0;
                    while (File.Exists(destinationFile) && attempts < 10)
                    {
                        string directory = Path.GetDirectoryName(destinationFile);
                        destinationFile = Path.Join(directory, $"{friendlyName}_{attempts + 2}.json");
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
}
