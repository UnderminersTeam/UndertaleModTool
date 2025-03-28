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

    /// <summary>
    /// Initializes a project context based on its existing main file path, and imports all of its data.
    /// </summary>
    /// <param name="currentData">Current data context to associate with the project.</param>
    /// <param name="mainFilePath">Main file path for the project.</param>
    /// <param name="mainThreadAction">
    /// For operations that should occur on the main thread, this will be 
    /// called to invoke those operations, if provided. This will stick around
    /// for the lifetime of the project context.
    /// </param>
    /// <exception cref="ProjectException">When a project-specific exception occurs</exception>
    public ProjectContext(UndertaleData currentData, string mainFilePath, Action<Action> mainThreadAction = null)
    {
        Data = currentData;
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

        // Recursively find and load in all assets in subdirectories
        List<ISerializableProjectAsset> loadedAssets = new(128);
        List<SerializableCode> loadedCodeAssets = new(64);
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

                // If asset's data name is omitted, use the filename
                asset.DataName ??= Path.GetFileNameWithoutExtension(assetPath);

                // Associate the data name (and type) of this asset with its path
                if (!_assetDataNamesToPaths.TryAdd((asset.DataName, asset.AssetType), assetPath))
                {
                    throw new ProjectException($"Found multiple {asset.AssetType} assets with name \"{asset.DataName}\"");
                }
            }
        }

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
        importGroup.Import(true);

        // TODO: texture page generation goes here, in parallel

        // Perform final import on all loaded assets
        MainThreadAction(() =>
        {
            foreach (ISerializableProjectAsset asset in loadedAssets)
            {
                asset.Import(this);
            }
        });
    }

    /// <summary>
    /// Initializes a project context for a new project based on its main file path, and a name to give to it.
    /// </summary>
    /// <param name="currentData">Current data context to associate with the project.</param>
    /// <param name="mainFilePath">Main file path for the project.</param>
    /// <param name="newProjectName">Name of the new project being created.</param>
    /// <param name="mainThreadAction">
    /// For operations that should occur on the main thread, this will be 
    /// called to invoke those operations, if provided. This will stick around
    /// for the lifetime of the project context.
    /// </param>
    /// <exception cref="ProjectException">When a project-specific exception occurs</exception>
    public ProjectContext(UndertaleData currentData, string mainFilePath, string newProjectName, Action<Action> mainThreadAction = null)
    {
        Data = currentData;
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

        // Export all assets that are marked as such
        foreach (IProjectAsset asset in _assetsMarkedForExport)
        {
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
}
