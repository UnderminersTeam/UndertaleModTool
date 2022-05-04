using System.Diagnostics;
using System.IO;

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
        string commitOutput;
        string branchOutput;

        try
        {
            // Start git, get the commit number and assign that to commitOutput
            using (Process process = new Process())
            {
                process.StartInfo.FileName = Git;
                process.StartInfo.Arguments = GitCommit;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                StreamReader reader = process.StandardOutput;
                string output = reader.ReadToEnd();
                commitOutput = output.Trim();
                process.WaitForExit();
            }

            // Start git, get the branch name and assign that to branchOutput
            using (Process process = new Process())
            {
                process.StartInfo.FileName = Git;
                process.StartInfo.Arguments = GitBranch;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                StreamReader reader = process.StandardOutput;
                string output = reader.ReadToEnd();
                branchOutput = output.Trim();
                process.WaitForExit();
            }
        }
        // If git can't be found, assign default values
        catch
        {
            commitOutput = branchOutput = "unavailable";
        }

        // return combined commit + branch
        string finalGitVersion = $"{commitOutput} ({branchOutput})";
        return finalGitVersion;
    }
}