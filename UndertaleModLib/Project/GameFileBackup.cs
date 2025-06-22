using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project;

/// <summary>
/// Base interface for managing game file backups and restoration, when installing/uninstalling mods.
/// </summary>
public interface IGameFileBackup
{
    /// <summary>
    /// Attempts to restore modified game files to their original state.
    /// </summary>
    /// <remarks>
    /// Throws a <see cref="GameFileBackupException"/> if a recoverable error occurs (i.e., game files are 
    /// not potentially left in a corrupted state). Any other exception could imply game file corruption.
    /// </remarks>
    public void RestoreFiles();

    /// <summary>
    /// Backs up a file at the given absolute path, whether or not it actually exists currently.
    /// </summary>
    /// <param name="path">Absolute path of the file to be backed up.</param>
    /// <remarks>
    /// Throws an exception if the backup fails for any reason; this should disallow all further game modifications.
    /// After all files in a mod are backed up, the manifest will need to be saved via <see cref="SaveManifest()"/> to release its lock.
    /// </remarks>
    public void BackupFile(string path);

    /// <summary>
    /// Saves the manifest that has been built using <see cref="BackupFile(string)"/>, or does nothing if no manifest has been built.
    /// </summary>
    public void SaveManifest();
}

/// <summary>
/// No-op implementation of game file backups and restoration (nothing is backed up, and nothing is restored).
/// </summary>
public sealed class GameFileNoOpBackup() : IGameFileBackup
{
    public void BackupFile(string path)
    {
    }

    public void RestoreFiles()
    {
    }

    public void SaveManifest()
    {
    }
}

/// <summary>
/// Default implementation of game file backups and restoration.
/// </summary>
public sealed class GameFileBackup(string gameDirectory) : IGameFileBackup
{
    /// <summary>
    /// Directory of the game files.
    /// </summary>
    public string GameDirectory { get; set; } = gameDirectory;

    /// <summary>
    /// Filename used for mod backup manifests.
    /// </summary>
    public const string ManifestFilename = "mod-backup-manifest.json";

    /// <summary>
    /// Version number of manifests that this library version is able to read/write. Generally should be backwards-compatible.
    /// </summary>
    public const string CurrentManifestVersion = "v1";

    /// <summary>
    /// All manifest versions that this library version is able to read.
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedManifestVersions = ["v1"];

    /// <summary>
    /// Default directory name used for storing mod backup files.
    /// </summary>
    /// <remarks>
    /// If the directory name is taken, an additional number will be appended to the end of the name.
    /// </remarks>
    public const string DefaultBackupDirectoryName = ".modbackup";

    /// <summary>
    /// Backup manifest currently being built as part of mod installation.
    /// </summary>
    private BackupManifest _buildingBackupManifest = null;

    /// <summary>
    /// FileStream used to hold a lock on the backup manifest.
    /// </summary>
    private FileStream _buildingBackupManifestStream = null;

    /// <summary>
    /// Set of relative paths to all files that are in the building backup manifest.
    /// </summary>
    private HashSet<string> _buildingBackupFilePaths = null;

    /// <inheritdoc/>
    public void RestoreFiles()
    {
        // Don't allow a restore while producing backup files...
        if (_buildingBackupManifest is not null)
        {
            throw new InvalidOperationException();
        }

        // Load manifest JSON
        string manifestPath = Path.Combine(GameDirectory, ManifestFilename);
        if (!File.Exists(manifestPath))
        {
            // Nothing to restore
            return;
        }
        BackupManifest manifest;
        try
        {
            using FileStream fs = new(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            manifest = JsonSerializer.Deserialize<BackupManifest>(fs);
        }
        catch (Exception ex)
        {
            throw new GameFileBackupException($"Failed to load backup manifest: {ex}", ex);
        }

        // Verify we know how to process the backup manifest's version
        if (!SupportedManifestVersions.Contains(manifest.ManifestVersion))
        {
            throw new GameFileBackupException($"Unsupported backup manifest version: {manifest.ManifestVersion}");
        }

        // Verify backup directory exists
        string backupDirPath = Path.Combine(GameDirectory, manifest.Path);
        if (!Directory.Exists(backupDirPath))
        {
            throw new GameFileBackupException($"Mod backup directory is missing (as defined in {ManifestFilename})");
        }

        // For all backup files that exist, verify integrity
        foreach (BackupFileEntry file in manifest.Files)
        {
            // Ignore files with no backup
            if (string.IsNullOrWhiteSpace(file.BackupPath))
            {
                continue;
            }

            // Ensure backup file exists
            string backupFilePath = Path.Combine(backupDirPath, file.BackupPath);
            if (!File.Exists(backupFilePath))
            {
                throw new GameFileBackupException($"Missing backup file for \"{file.Path}\" (missing \"{file.BackupPath}\")");
            }

            // Verify file hash, if stored
            if (string.IsNullOrWhiteSpace(file.HashSHA1))
            {
                continue;
            }
            using SHA1 sha1 = SHA1.Create();
            using FileStream fs = new(backupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            string hashString = HashBytesToHex(sha1.ComputeHash(fs));
            if (hashString != file.HashSHA1)
            {
                throw new GameFileBackupException($"Backup file corrupted: checksum mismatch for file \"{file.Path}\" (stored at \"{file.BackupPath}\")");
            }
        }

        // Restore all files
        foreach (BackupFileEntry file in manifest.Files)
        {
            // If a backup file exists, move it to original location. Otherwise, delete file at given location.
            string gameDataLocation = Path.Combine(GameDirectory, file.Path);
            if (string.IsNullOrWhiteSpace(file.BackupPath))
            {
                // Delete mod-specific file, as long as it still exists
                if (File.Exists(gameDataLocation))
                {
                    File.Delete(gameDataLocation);
                }
            }
            else
            {
                // Move backup file and potentially overwrite original file.
                // Make directories along the path, just in case the user or modder did nefarious things with the structure.
                string backupFilePath = Path.Combine(backupDirPath, file.BackupPath);
                Directory.CreateDirectory(Path.GetDirectoryName(gameDataLocation));
                File.Move(backupFilePath, gameDataLocation, true);
            }
        }

        // Ensure backup directory is empty - if not, throw an exception to warn the user to never add files there
        DirectoryInfo info = new(backupDirPath);
        if (info.EnumerateFileSystemInfos().Any())
        {
            throw new GameFileBackupException($"Backup directory was not empty after restoring. Please clear the directory and remove {ManifestFilename}. (Backup directory located at {backupDirPath})");
        }

        // Delete manifest and backup directory itself
        Directory.Delete(backupDirPath, false);
        File.Delete(manifestPath);
    }

    /// <inheritdoc/>
    public void BackupFile(string path)
    {
        // Create manifest if one hasn't already been created
        if (_buildingBackupManifest is null)
        {
            // Make sure there's no existing backup manifest, and create a lock on a new one
            string manifestPath = Path.Join(GameDirectory, ManifestFilename);
            if (File.Exists(manifestPath))
            {
                throw new GameFileBackupException("Mod backup manifest still exists when trying to make a new backup");
            }
            _buildingBackupManifestStream = new(manifestPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

            try
            {
                // Choose and create a directory for backup, choosing alternatives just in case
                string backupDirName = DefaultBackupDirectoryName;
                string backupDirPath = Path.Join(GameDirectory, backupDirName);
                int loopCounter = 0;
                while (Directory.Exists(backupDirName))
                {
                    if (loopCounter >= 50)
                    {
                        throw new GameFileBackupException("Too many directory name conflicts for mod backup directory");
                    }
                    backupDirName = $"{DefaultBackupDirectoryName}{++loopCounter}";
                    backupDirPath = Path.Join(GameDirectory, backupDirName);
                }
                Directory.CreateDirectory(backupDirPath);

                _buildingBackupManifest = new()
                {
                    ManifestVersion = CurrentManifestVersion,
                    Path = backupDirName,
                    Files = []
                };
                _buildingBackupFilePaths = [];
            }
            catch
            {
                _buildingBackupManifestStream.Dispose();
                _buildingBackupManifestStream = null;
                _buildingBackupManifest = null;
                _buildingBackupFilePaths = null;
                throw;
            }
        }

        // Ensure we haven't added this path more than once, to avoid errors.
        if (!_buildingBackupFilePaths.Add(Path.GetFullPath(path)))
        {
            return;
        }

        // If the file exists, hash and make a copy of it. Otherwise, just declare it in the manifest (so it gets deleted when restoring, if necessary).
        string relativePath = Path.GetRelativePath(GameDirectory, path);
        if (File.Exists(path))
        {
            // Hash file
            string hashString;
            using (SHA1 sha1 = SHA1.Create())
            {
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                hashString = HashBytesToHex(sha1.ComputeHash(fs));
            }

            // Determine an unused filename based on hash and relative path
            string backupFileNameBase = $"backup_{MurmurHash.Hash(relativePath)}_{hashString}";
            string backupFileName = backupFileNameBase;
            string backupDirPath = Path.Combine(GameDirectory, _buildingBackupManifest.Path);
            string backupFilePath = Path.Join(backupDirPath, backupFileName);
            int loopCounter = 0;
            while (File.Exists(backupFilePath))
            {
                if (loopCounter >= 50)
                {
                    throw new GameFileBackupException($"Too many filename conflicts for mod file backup (path \"{relativePath}\")");
                }
                backupFileName = $"{backupFileNameBase}_{++loopCounter}";
                backupFilePath = Path.Join(backupDirPath, backupFileName);
            }

            // Copy file (don't allow overwrites, though - we're trying to *avoid* that...)
            File.Copy(path, backupFilePath, false);

            // Add to manifest
            _buildingBackupManifest.Files.Add(new BackupFileEntry()
            {
                Path = relativePath,
                BackupPath = Path.GetRelativePath(backupDirPath, backupFilePath),
                HashSHA1 = hashString
            });
        }
        else
        {
            // Declare (presumably) newly-added file in manifest only
            _buildingBackupManifest.Files.Add(new BackupFileEntry()
            {
                Path = relativePath,
                BackupPath = null,
                HashSHA1 = null
            });
        }
    }

    /// <summary>
    /// Saves the manifest that has been built using <see cref="BackupFile(string)"/>, or does nothing if no manifest has been built.
    /// </summary>
    public void SaveManifest()
    {
        if (_buildingBackupManifest is null)
        {
            return;
        }
        JsonSerializer.Serialize(_buildingBackupManifestStream, _buildingBackupManifest);
        _buildingBackupManifestStream.Dispose();
        _buildingBackupManifestStream = null;
        _buildingBackupManifest = null;
        _buildingBackupFilePaths = null;
    }

    // Simple hex character lookup
    private static readonly char[] _hexLookup = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'];

    /// <summary>
    /// Simple conversion from a byte array to string representation.
    /// </summary>
    private string HashBytesToHex(byte[] hash)
    {
        if (hash.Length > 512)
        {
            throw new NotImplementedException();
        }

        Span<char> chars = stackalloc char[hash.Length * 2];
        for (int i = 0; i < hash.Length; i++)
        {
            chars[i * 2] = _hexLookup[hash[i] >> 4];
            chars[(i * 2) + 1] = _hexLookup[hash[i] & 0xf];
        }
        return chars.ToString();
    }

    /// <summary>
    /// Used for serializing manifest information for backup files.
    /// </summary>
    internal sealed class BackupManifest
    {
        /// <summary>
        /// Manifest version number, for managing upgrades (and detecting incompatibility) correctly.
        /// </summary>
        public string ManifestVersion { get; set; }

        /// <summary>
        /// Relative path from the game data to the directory containing backup files.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// List of backup files.
        /// </summary>
        public List<BackupFileEntry> Files { get; set; }
    }

    internal sealed class BackupFileEntry
    {
        /// <summary>
        /// Path of the file in regular game data.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Path of the backed-up file, or <see langword="null"/> if no backup exists.
        /// </summary>
        /// <remarks>
        /// No backup can naturally occur when a file is newly-added as part of a mod (and should just be deleted during a restore).
        /// </remarks>
        public string BackupPath { get; set; }

        /// <summary>
        /// SHA-1 hash of the backed-up file, or <see langword="null"/> if this hash should not be verified.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string HashSHA1 { get; set; }
    }
}
