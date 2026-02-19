using System.IO;
using System;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project;

partial class ProjectContext
{

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
    /// Imports all sub-projects, as defined in the project options.
    /// </summary>
    private void ImportSubProjects()
    {
        if (_mainOptions.SubProjects.Count == 0)
        {
            return;
        }
        _projectJsonPaths ??= [Path.GetFullPath(MainFilePath)];
        foreach (string subProjectRelativePath in _mainOptions.SubProjects)
        {
            string subProjectPath = Path.Join(MainDirectory, subProjectRelativePath);
            Paths.VerifyWithinDirectory(MainDirectory, subProjectPath);
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
    /// Imports all external files, as found in locations (file/directory paths) defined in the project options.
    /// </summary>
    private void ImportExternalFiles()
    {
        foreach (ProjectMainOptions.PathList.PathPair pair in _mainOptions.ExternalFiles)
        {
            // Make sure external file or directory exists
            string sourcePath = Path.Join(MainDirectory, pair.Source);
            Paths.VerifyWithinDirectory(MainDirectory, sourcePath);
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
            string fullPatchPath = Path.Join(MainDirectory, patch.PatchPath);
            Paths.VerifyWithinDirectory(MainDirectory, fullPatchPath);

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
}
