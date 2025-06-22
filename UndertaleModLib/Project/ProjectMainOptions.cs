using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace UndertaleModLib.Project;

/// <summary>
/// Represents the main options file for a project.
/// </summary>
internal sealed class ProjectMainOptions
{
    /// <summary>
    /// Name for the project.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// List of flags enabled for the project.
    /// </summary>
    /// <remarks>
    /// Flag names beginning with an underscore (_) are treated as custom flags, for any use.
    /// Non-custom flag names will throw an exception if not handled by the current library version.
    /// </remarks>
    public List<string> Flags { get; set; } = [];

    /// <summary>
    /// Path to the default data file path to have project assets imported to, or none to disable asset importing during installation.
    /// </summary>
    public PathList AssetsDataFilePath { get; set; } = new();

    /// <summary>
    /// List of root-level directory names to exclude from the project.
    /// </summary>
    public List<string> ExcludeDirectories { get; set; } = [];

    /// <summary>
    /// List of relative paths to sub-project JSON files.
    /// </summary>
    public List<string> SubProjects { get; set; } = [];

    /// <summary>
    /// List of file patches to be applied.
    /// </summary>
    public List<Patch> Patches { get; set; } = [];

    /// <summary>
    /// List of relative paths to scripts to be executed prior to all import operations.
    /// </summary>
    public List<string> PreImportScripts { get; set; } = [];

    /// <summary>
    /// List of relative paths to scripts to be executed just prior to project asset import operations.
    /// </summary>
    public List<string> PreAssetImportScripts { get; set; } = [];

    /// <summary>
    /// List of relative paths to scripts to be executed after all import operations.
    /// </summary>
    public List<string> PostImportScripts { get; set; } = [];

    /// <summary>
    /// Represents a patch file to be applied to a game data file.
    /// </summary>
    internal sealed class Patch
    {
        /// <summary>
        /// Relative path to the patch file.
        /// </summary>
        public string PatchPath { get; set; }

        /// <summary>
        /// List of possible paths to the data file to be patched.
        /// </summary>
        public PathList DataFilePath { get; set; }
    }

    /// <summary>
    /// Represents a list of possible paths to be enumerated.
    /// </summary>
    [JsonConverter(typeof(PathListConverter))]
    internal sealed class PathList
    {
        /// <summary>
        /// List of paths.
        /// </summary>
        public List<string> Paths { get; set; } = [];
    }

    /// <summary>
    /// JSON converter for path lists.
    /// </summary>
    internal class PathListConverter : JsonConverter<PathList>
    {
        public override PathList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new PathList();
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                return new PathList()
                {
                    Paths = [reader.GetString()]
                };
            }
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            List<string> values = [];
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return new PathList()
                    {
                        Paths = values
                    };
                }

                // Read string value
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException();
                }
                values.Add(reader.GetString() ?? throw new JsonException());
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, PathList value, JsonSerializerOptions options)
        {
            if (value.Paths is null || value.Paths.Count == 0)
            {
                writer.WriteNullValue();
            }
            else if (value.Paths is [string singlePath])
            {
                writer.WriteStringValue(singlePath);
            }
            else
            {
                writer.WriteStartArray();
                foreach (string path in value.Paths)
                {
                    writer.WriteStringValue(path);
                }
                writer.WriteEndArray();
            }
        }
    }
}
