using System;
using System.IO;
using UndertaleModLib.Models;
using UndertaleModLib.Project.SerializableAssets;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project;

partial class ProjectContext
{
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

    /// <summary>
    /// Loads the main assets data file from <see cref="LoadDataPath"/>.
    /// </summary>
    private void LoadAssetsDataFile()
    {
        try
        {
            using FileStream fs = new(LoadDataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            Data = UndertaleIO.Read(fs, (string warning, bool isImportant) =>
            {
                if (_mainOptions.ErrorOnWarnings)
                {
                    throw new ProjectException($"Warning occurred when loading data file: {warning}");
                }
            }, (_) => { });
        }
        catch (ProjectException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new ProjectException($"Error occurred when loading data file: {e}", e);
        }
    }

    /// <summary>
    /// Saves the main assets data file to <see cref="SaveDataPath"/>.
    /// </summary>
    private void SaveAssetsDataFile()
    {
        try
        {
            using FileStream fs = new(SaveDataPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            UndertaleIO.Write(fs, Data, (_) => { });
        }
        catch (ProjectException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new ProjectException($"Error occurred when saving data file: {e}", e);
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
}
