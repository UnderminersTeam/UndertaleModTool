using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public string Name { get => _mainOptions?.Name ?? "(unknown project name)"; }

    /// <summary>
    /// Project flags, denoting certain features that the project may use (including custom ones, which start with an underscore).
    /// </summary>
    public List<string> Flags { get => _mainOptions?.Flags ?? []; }

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
            Data = currentData;
        }
        else
        {
            // TODO: check options for data file that needs to be loaded/saved, and if one is found,
            // set LoadDataPath/SaveDataPath, as well as adjust LoadDirectory/SaveDirectory (e.g. for sub-projects)
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
                // TODO: load data here
                RunPreAssetImportScripts();
                LoadProjectAssets();
            }
            RunPostImportScripts();
            if (SaveDataPath is not null)
            {
                // TODO: save data here
                FileBackup.BackupFile(SaveDataPath);
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
    /// Handles non-custom flags defined in the project main options.
    /// </summary>
    private void ProcessFlags()
    {
        foreach (string flag in _mainOptions.Flags)
        {
            // Ignore custom flags
            if (flag.StartsWith('_'))
            {
                continue;
            }

            // Currently, no non-custom flags are supported, so throw an exception.
            throw new ProjectException($"Unsupported project flag \"{flag}\"");
        }
    }

    /// <summary>
    /// Creates a new backup using the default implementation, and restores game files to an unmodded state.
    /// </summary>
    private void CreateNewBackup()
    {
        try
        {
            FileBackup = new GameFileBackup(SaveDirectory);
            FileBackup.RestoreFiles();
        }
        catch (GameFileBackupException ex)
        {
            throw new ProjectException($"Restoring game files using backup failed: {ex}", ex);
        }
        catch (Exception ex)
        {
            throw new ProjectException($"Fatal error restoring game files using backup: {ex}", ex);
        }
    }

    /// <summary>
    /// Finishes the backup process, assuming that the backup was created by this project context, for this project context.
    /// </summary>
    private void FinishNewBackup()
    {
        try
        {
            FileBackup.SaveManifest();
        }
        catch (Exception ex)
        {
            throw new ProjectException($"Fatal error backing up original game files: {ex}", ex);
        }
    }

    /// <summary>
    /// Executes all pre-import scripts, as defined in the project options.
    /// </summary>
    private void RunPreImportScripts()
    {
        // TODO
    }

    /// <summary>
    /// Executes all pre-asset import scripts, as defined in the project options.
    /// </summary>
    private void RunPreAssetImportScripts()
    {
        // TODO
    }

    /// <summary>
    /// Executes all pre-import scripts, as defined in the project options.
    /// </summary>
    private void RunPostImportScripts()
    {
        // TODO
    }

    /// <summary>
    /// Imports all sub-projects, as defined in the project options.
    /// </summary>
    private void ImportSubProjects()
    {
        if (_mainOptions.SubProjects.Count == 0)
        {
            return;
        }
        _projectJsonPaths ??= [Path.GetFullPath(_mainFilePath)];
        foreach (string subProjectRelativePath in _mainOptions.SubProjects)
        {
            string subProjectPath = Path.Join(_mainDirectory, subProjectRelativePath);
            Paths.VerifyWithinDirectory(_mainDirectory, subProjectPath);
            if (!_projectJsonPaths.Add(Path.GetFullPath(subProjectPath)))
            {
                throw new ProjectException("Infinite sub-project recursion detected");
            }
            ProjectContext subContext = new(LoadDirectory, SaveDirectory, subProjectPath)
            {
                _projectJsonPaths = _projectJsonPaths
            };
            subContext.Import(null, FileBackup, MainThreadAction);
        }
    }

    /// <summary>
    /// Helper method to copy a directory to another directory, ignoring hidden folders and files.
    /// </summary>
    private void CopyDirectory(DirectoryInfo sourceInfo, string destPath)
    {
        // Sanity check the source directory's existence
        if (!sourceInfo.Exists)
        {
            throw new DirectoryNotFoundException(sourceInfo.FullName);
        }

        // Make sure destination directory is created
        if (!Directory.Exists(destPath))
        {
            FileBackup.BackupDirectory(destPath, true);
            Directory.CreateDirectory(destPath);
        }

        // Copy over files from this directory
        foreach (FileInfo sourceFileInfo in sourceInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
        {
            if (sourceFileInfo.Attributes.HasFlag(FileAttributes.Hidden) || sourceFileInfo.Attributes.HasFlag(FileAttributes.System))
            {
                continue;
            }
            string destFilePath = Path.Join(destPath, sourceFileInfo.Name);
            FileBackup.BackupFile(destFilePath);
            sourceFileInfo.CopyTo(destFilePath, true);
        }

        // Recursively copy sub-directories
        foreach (DirectoryInfo sourceDirInfo in sourceInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            if (sourceDirInfo.Attributes.HasFlag(FileAttributes.Hidden) || sourceDirInfo.Attributes.HasFlag(FileAttributes.System))
            {
                continue;
            }
            string destDirPath = Path.Join(destPath, sourceDirInfo.Name);
            CopyDirectory(sourceDirInfo, destDirPath);
        }
    }

    /// <summary>
    /// Imports all external files, as found in locations (file/directory paths) defined in the project options.
    /// </summary>
    private void ImportExternalFiles()
    {
        foreach (ProjectMainOptions.PathList.PathPair pair in _mainOptions.ExternalFiles)
        {
            // Make sure external file or directory exists
            string sourcePath = Path.Join(_mainDirectory, pair.Source);
            Paths.VerifyWithinDirectory(_mainDirectory, sourcePath);
            bool isDirectory;
            try
            {
                FileAttributes sourceAttributes = File.GetAttributes(sourcePath);
                isDirectory = sourceAttributes.HasFlag(FileAttributes.Directory);
            }
            catch (Exception e)
            {
                throw new ProjectException($"Failed to find external file or directory: {e}", e);
            }

            // Get destination path
            string destPath = Path.Join(SaveDirectory, pair.Destination);
            Paths.VerifyWithinDirectory(SaveDirectory, destPath);

            // Create directories if needed
            string destFullPath = Path.GetFullPath(destPath);
            string destDirectory = Path.GetDirectoryName(destFullPath);
            if (!Directory.Exists(destDirectory))
            {
                FileBackup.BackupDirectory(destDirectory, true);
                Directory.CreateDirectory(destDirectory);
            }

            if (isDirectory)
            {
                // If this is a directory, recursively copy its contents (and create directories).
                CopyDirectory(new DirectoryInfo(sourcePath), destFullPath);
            }
            else
            {
                // If this is a file, copy single file
                FileBackup.BackupFile(destFullPath);
                File.Copy(sourcePath, destPath, true);
            }
        }
    }

    /// <summary>
    /// Attempts to find a valid file from the given path list, outputting the first valid path pair found, or <see langword="null"/> if none are valid.
    /// </summary>
    /// <remarks>
    /// This method will search relative to <see cref="LoadDirectory"/>, and will also verify path destinations relative to <see cref="SaveDirectory"/>.
    /// </remarks>
    /// <returns><see langword="true"/> if a valid path pair was found; <see langword="false"/> otherwise.</returns>
    private bool TryFindFileFromPathList(ProjectMainOptions.PathList list, out string sourcePath, out string destPath)
    {
        foreach (ProjectMainOptions.PathList.PathPair pair in list.Paths)
        {
            // Attempt next path
            string attemptPath = Path.Join(LoadDirectory, pair.Source);
            Paths.VerifyWithinDirectory(LoadDirectory, attemptPath);
            if (!File.Exists(attemptPath))
            {
                continue;
            }

            // Successfully found a path!
            sourcePath = attemptPath;
            destPath = Path.Join(SaveDirectory, pair.Destination);
            Paths.VerifyWithinDirectory(SaveDirectory, destPath);
            return true;
        }

        // No valid path found
        sourcePath = null;
        destPath = null;
        return false;
    }

    /// <summary>
    /// Applies all file operations (copy/delete), as defined in the project options.
    /// </summary>
    private void ApplyFileOperations()
    {
        // Apply file copies first
        foreach (ProjectMainOptions.FileOperation operation in _mainOptions.FileCopies)
        {
            // Try finding valid source/destination file paths
            if (!TryFindFileFromPathList(operation.Paths, out string sourcePath, out string destPath))
            {
                // Throw exception only if required
                if (operation.Required)
                {
                    throw new ProjectException("Failed to find required file to be copied");
                }
                continue;
            }

            // Back up destination file
            string destFullPath = Path.GetFullPath(destPath);
            FileBackup.BackupFile(destFullPath);

            // Create directories if needed
            string destDirectory = Path.GetDirectoryName(destFullPath);
            if (!Directory.Exists(destDirectory))
            {
                FileBackup.BackupDirectory(destDirectory, true);
                Directory.CreateDirectory(destDirectory);
            }

            // Perform copy
            File.Copy(sourcePath, destPath, true);
        }

        // Apply file deletes last
        foreach (ProjectMainOptions.FileOperation operation in _mainOptions.FileDeletes)
        {
            // Try finding valid destination file path
            if (!TryFindFileFromPathList(operation.Paths, out _, out string destPath))
            {
                // Throw exception only if required
                if (operation.Required)
                {
                    throw new ProjectException("Failed to find required file to be deleted");
                }
                continue;
            }

            // Back up destination file
            FileBackup.BackupFile(Path.GetFullPath(destPath));

            // Perform delete (only if file actually exists in destination - it may have been deleted already,
            // but we already verified the file exists at the source path)
            File.Delete(destPath);
        }
    }

    /// <summary>
    /// Applies all file patches, located as defined in the project options.
    /// </summary>
    private void ApplyFilePatches()
    {
        foreach (ProjectMainOptions.Patch patch in _mainOptions.Patches)
        {
            // Get path to patch file
            string fullPatchPath = Path.Join(_mainDirectory, patch.PatchPath);
            Paths.VerifyWithinDirectory(_mainDirectory, fullPatchPath);

            // Try finding valid base/output file paths
            if (!TryFindFileFromPathList(patch.DataFilePath, out string baseFilePath, out string outputFilePath))
            {
                throw new ProjectException("Failed to find data file to be patched");
            }
            string baseFileFullPath = Path.GetFullPath(baseFilePath);
            string outputFileFullPath = Path.GetFullPath(outputFilePath);

            // Back up destination file
            FileBackup.BackupFile(outputFileFullPath);

            // Create directories if needed
            string outputDirectory = Path.GetDirectoryName(outputFileFullPath);
            if (!Directory.Exists(outputDirectory))
            {
                FileBackup.BackupDirectory(outputDirectory, true);
                Directory.CreateDirectory(outputDirectory);
            }

            // Apply patch
            const int bufferSize = 131072;
            using FileStream patchStream = new(fullPatchPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
            if (baseFileFullPath == outputFileFullPath)
            {
                byte[] baseFileBytes = File.ReadAllBytes(baseFileFullPath);
                using MemoryStream baseStream = new(baseFileBytes);
                using FileStream outputStream = new(outputFileFullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, bufferSize);
                BPS.ApplyPatch(baseStream, patchStream, outputStream);
            }
            else
            {
                using FileStream baseStream = new(baseFileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
                using FileStream outputStream = new(outputFileFullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, bufferSize);
                BPS.ApplyPatch(baseStream, patchStream, outputStream);
            }
        }
    }

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
        string relativeAudioGroupPath;
        if (index < Data.AudioGroups.Count && Data.AudioGroups[index] is UndertaleAudioGroup { Path.Content: string customRelativePath })
        {
            relativeAudioGroupPath = customRelativePath;
        }
        else
        {
            relativeAudioGroupPath = $"audiogroup{index}.dat";
        }
        string baseDirectory = fromLoadedDirectory ? LoadDirectory : SaveDirectory;
        string path = Path.Join(baseDirectory, relativeAudioGroupPath);
        Paths.VerifyWithinDirectory(baseDirectory, path);
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
            throw new ProjectException($"Failed to find file \"{relativeAudioGroupPath}\" for \"{forAsset.DataName}\"");
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
            string destPath = Path.Join(SaveDirectory, destinationFilename);
            Paths.VerifyWithinDirectory(SaveDirectory, destPath);

            // Backup original destination file status
            string destFullPath = Path.GetFullPath(destPath);
            FileBackup.BackupFile(destFullPath);

            // Create directories if needed
            string destDirectory = Path.GetDirectoryName(destFullPath);
            if (!Directory.Exists(destDirectory))
            {
                FileBackup.BackupDirectory(destDirectory, true);
                Directory.CreateDirectory(destDirectory);
            }

            // Actually copy the file
            File.Copy(sourcePath, destPath, true);
        }
        catch (Exception e)
        {
            throw new ProjectException($"Failed to copy streamed audio file for \"{forSound.DataName}\": {e}", e);
        }
    }
}
