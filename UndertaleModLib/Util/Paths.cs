using System;
using System.IO;

namespace UndertaleModLib.Util;

/// <summary>
/// Path utility functions.
/// </summary>
public static class Paths
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="pathToTest"/> is contained within (starts with) <paramref name="directory"/>, 
    /// when the path is converted to be fully-qualified.
    /// </summary>
    /// <remarks>
    /// This can be used for error-checking and simple security purposes.
    /// </remarks>
    public static bool IsWithinDirectory(string directory, string pathToTest)
    {
        string fullDirectoryPath = Path.GetFullPath(directory);
        if (!fullDirectoryPath.EndsWith(Path.DirectorySeparatorChar))
        {
            fullDirectoryPath += Path.DirectorySeparatorChar;
        }
        string fullPathToTest = Path.GetFullPath(pathToTest);
        return fullPathToTest.StartsWith(fullDirectoryPath, StringComparison.Ordinal);
    }

    /// <summary>
    /// Throws an exception if <paramref name="pathToTest"/> is not contained within (starts with) <paramref name="directory"/>.
    /// </summary>
    /// <remarks>
    /// This can be used for error-checking and simple security purposes.
    /// </remarks>
    public static void VerifyWithinDirectory(string directory, string pathToTest)
    {
        if (!IsWithinDirectory(directory, pathToTest))
        {
            throw new Exception($"Path escapes its root directory ({pathToTest})");
        }
    }

    /// <summary>
    /// Similar to <see cref="Path.Join(string?, string?)"/>, but verifies that the end result is 
    /// within <paramref name="directory"/> using <see cref="VerifyWithinDirectory(string, string)"/>.
    /// </summary>
    public static string JoinVerifyWithinDirectory(string directory, string path)
    {
        string joined = Path.Join(directory, path);
        VerifyWithinDirectory(directory, joined);
        return joined;
    }

    /// <summary>
    /// Similar to <see cref="Path.Join(string?, string?, string?)"/>, but verifies that the end result is 
    /// within <paramref name="directory"/> using <see cref="VerifyWithinDirectory(string, string)"/>.
    /// </summary>
    public static string JoinVerifyWithinDirectory(string directory, string path1, string path2)
    {
        string joined = Path.Join(directory, path1, path2);
        VerifyWithinDirectory(directory, joined);
        return joined;
    }

    /// <summary>
    /// Similar to <see cref="Path.Join(string?, string?, string?, string?)"/>, but verifies that the end result is 
    /// within <paramref name="directory"/> using <see cref="VerifyWithinDirectory(string, string)"/>.
    /// </summary>
    public static string JoinVerifyWithinDirectory(string directory, string path1, string path2, string path3)
    {
        string joined = Path.Join(directory, path1, path2, path3);
        VerifyWithinDirectory(directory, joined);
        return joined;
    }

    /// <summary>
    /// Similar to <see cref="Path.Join(string?, string?)"/>, but verifies that the end result is 
    /// within <paramref name="directory"/> using <see cref="IsWithinDirectory(string, string)"/>.
    /// If not verified, this returns <see langword="null"/>.
    /// </summary>
    public static string TryJoinVerifyWithinDirectory(string directory, string path)
    {
        string joined = Path.Join(directory, path);
        if (!IsWithinDirectory(directory, joined))
        {
            return null;
        }
        return joined;
    }

    /// <summary>
    /// Similar to <see cref="Path.Join(string?, string?, string?)"/>, but verifies that the end result is 
    /// within <paramref name="directory"/> using <see cref="IsWithinDirectory(string, string)"/>.
    /// If not verified, this returns <see langword="null"/>.
    /// </summary>
    public static string TryJoinVerifyWithinDirectory(string directory, string path1, string path2)
    {
        string joined = Path.Join(directory, path1, path2);
        if (!IsWithinDirectory(directory, joined))
        {
            return null;
        }
        return joined;
    }

    /// <summary>
    /// Similar to <see cref="Path.Join(string?, string?, string?, string?)"/>, but verifies that the end result is 
    /// within <paramref name="directory"/> using <see cref="IsWithinDirectory(string, string)"/>.
    /// If not verified, this returns <see langword="null"/>.
    /// </summary>
    public static string TryJoinVerifyWithinDirectory(string directory, string path1, string path2, string path3)
    {
        string joined = Path.Join(directory, path1, path2, path3);
        if (!IsWithinDirectory(directory, joined))
        {
            return null;
        }
        return joined;
    }
}
