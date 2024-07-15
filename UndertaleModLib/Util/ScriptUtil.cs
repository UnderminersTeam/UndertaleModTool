using System.IO;
using System.Text.RegularExpressions;

namespace UndertaleModLib.Util
{
    /// <summary>
    /// Utils related to running scripts
    /// </summary>
    public static class ScriptUtil
    {
        /// <summary>
        /// Processes a script text so that it can use relative `#load` preprocessing
        /// </summary>
        /// <param name="code">The script code</param>
        /// <param name="path">The path to the script</param>
        /// <returns></returns>
        public static string ProcessScriptText(string code, string path)
        {
            var output = code;
            // since attempting to load scripts with #load in a file will lead to it using the UTMT path,
            // we can circumvent this by hardcoding the absolute path to the script directory
            // in all instances of #load with a relative path
            var scriptDir = Path.GetDirectoryName(path);
            var matches = Regex.Matches(code, @"(?<=^#load\s+"").*\.csx(?=""\s*$)", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                if (Path.IsPathRooted(match.Value))
                {
                    continue;
                }
                output = output.Replace(match.Value, Path.Combine(scriptDir, match.Value));
            }

            return output;
        }
    }
}