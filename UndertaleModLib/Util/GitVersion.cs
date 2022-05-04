using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace UndertaleModLib.Util;

/// <summary>
/// Includes miscellaneous git information about the project to compile.
/// <b>Only intended for Debug use!</b>
/// </summary>
public static class GitVersion
{
    /// <summary>
    /// The constant for the git executable.
    /// </summary>
    private const string Git = "git";

    /// <summary>
    /// The constant to receive commit name.
    /// </summary>
    private const string GitCommit = "describe --always --dirty";

    /// <summary>
    /// The constant to receive branch name.
    /// </summary>
    private const string GitBranch = "rev-parse --abbrev-ref HEAD";

    /// <summary>
    /// Gets and returns the git commit and branch name.
    /// </summary>
    /// <returns>The git commit and branch name.</returns>
    public static string GetGitVersion()
    {
        string gitOutput = "";

        // try to access the embedded resource
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "UndertaleModLib.gitversion.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                // \r is getting nuked just in case Windows is weird.
                gitOutput = reader.ReadToEnd().Trim().Replace("\r", "");
            }

            // gets formatted as "<commit> (<branch>)"
            var outputAsArray = gitOutput.Split('\n');
            gitOutput = $"{outputAsArray[0]} ({outputAsArray[1]})";
        }
        // If accessing it fails, give it a default output
        catch
        {
            gitOutput = "unavailable";
        }

        // return combined commit + branch
        if (String.IsNullOrWhiteSpace(gitOutput)) gitOutput = "unavailable";
        return gitOutput;
    }
}