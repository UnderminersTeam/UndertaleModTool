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
        string fullPathToTest = Path.GetFullPath(pathToTest);
        return fullPathToTest.StartsWith(fullDirectoryPath);
    }

    /// <summary>
    /// Throws an exception if <paramref name="pathToTest"/> is not contained within (starts with) <paramref name="directory"/>.
    /// </summary>
    /// <remarks>
    /// This can be used for error-checking and simple security purposes.
    /// </remarks>
    public static void VerifyWithinDirectory(string directory, string pathToTest)
    {
        string fullDirectoryPath = Path.GetFullPath(directory);
        string fullPathToTest = Path.GetFullPath(pathToTest);
        if (!fullPathToTest.StartsWith(fullDirectoryPath))
        {
            throw new Exception($"Path escapes its root directory ({fullPathToTest})");
        }
    }
}
