using System;
using System.IO;
using System.Reflection;

namespace UndertaleModLib.Util;

/// <summary>
/// Includes miscellaneous git information about the project to compile.
/// </summary>
public static class GitVersion
{
    public record GitVersionData(string Commit, string Branch, DateTimeOffset Time);

    /// <summary>
    /// Gets and returns the git commit and branch name.
    /// </summary>
    /// <returns>The git commit and branch name.</returns>
    public static string GetGitVersion()
    {
        GitVersionData data = GetGitVersionData();
        return data is null ? "unknownGitCommit" : $"{data.Commit} ({data.Branch})";
    }

    public static GitVersionData GetGitVersionData()
    {
        // Try to access the embedded resource
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "UndertaleModLib.gitversion.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                // \r is getting nuked just in case Windows is weird.
                string[] data = reader.ReadToEnd().Trim().Replace("\r", "").Split('\n');

                return new GitVersionData(
                    Commit: data[0],
                    Branch: data[1],
                    Time: DateTimeOffset.Parse(data[2])
                );
            }
        }
        // If accessing it fails, give it a default output
        catch
        {
            return null;
        }
    }
}