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
    /// Whether loading/saving the project's data files should raise an error when any warnings occur.
    /// </summary>
    public bool ErrorOnWarnings { get; set; } = true;

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
    /// List of paths to directories and files that should be copied to the save directory when applying the project.
    /// </summary>
    public List<PathList.PathPair> ExternalFiles { get; set; } = [];

    /// <summary>
    /// List of file paths to be copied from one location to another, within the game data.
    /// </summary>
    public List<FileOperation> FileCopies { get; set; } = [];

    /// <summary>
    /// List of file paths to be deleted within the game data.
    /// </summary>
    public List<FileOperation> FileDeletes { get; set; } = [];

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
    internal struct Patch()
    {
        /// <summary>
        /// Relative path to the patch file.
        /// </summary>
        public string PatchPath { get; set; }

        /// <summary>
        /// List of possible paths to the data file to be patched.
        /// </summary>
        public PathList DataFilePath { get; set; } = new();
    }

    /// <summary>
    /// Represents a file operation to be performed upon project import.
    /// </summary>
    [JsonConverter(typeof(FileOperationConverter))]
    internal struct FileOperation()
    {
        /// <summary>
        /// Possible source/destination paths for the file operation, from which only one will be used.
        /// </summary>
        public PathList Paths { get; set; } = new();

        /// <summary>
        /// Whether the file operation is required. That is, if no valid source paths are found, this will
        /// be checked to know whether an error should be raised.
        /// </summary>
        public bool Required { get; set; } = true;
    }

    /// <summary>
    /// Represents a list of possible paths to be enumerated, with both sources and destinations (which may be the same).
    /// </summary>
    [JsonConverter(typeof(PathListConverter))]
    internal struct PathList()
    {
        /// <summary>
        /// List of paths (including source and destination, which may be the same).
        /// </summary>
        public List<PathPair> Paths { get; set; } = [];

        /// <summary>
        /// Source and destination path pair. Both paths may be the same.
        /// </summary>
        [JsonConverter(typeof(PathPairConverter))]
        public record struct PathPair(string Source, string Destination);

        /// <summary>
        /// Whether the path list is empty. 
        /// </summary>
        public readonly bool Empty => Paths.Count == 0;
    }

    /// <summary>
    /// JSON converter for path lists.
    /// </summary>
    internal class PathListConverter : JsonConverter<PathList>
    {
        /// <inheritdoc/>
        public override PathList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new PathList();
            }
            if (reader.TokenType is JsonTokenType.String or JsonTokenType.StartObject)
            {
                return new PathList()
                {
                    Paths = [JsonSerializer.Deserialize<PathList.PathPair>(ref reader, options)]
                };
            }
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected null, string path, path pair object, or array of paths");
            }

            List<PathList.PathPair> values = [];
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return new PathList()
                    {
                        Paths = values
                    };
                }
                values.Add(JsonSerializer.Deserialize<PathList.PathPair>(ref reader, options));
            }

            throw new JsonException();
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, PathList value, JsonSerializerOptions options)
        {
            if (value.Paths is null || value.Paths.Count == 0)
            {
                writer.WriteNullValue();
            }
            else if (value.Paths is [PathList.PathPair singlePath])
            {
                JsonSerializer.Serialize(writer, singlePath, options);
            }
            else
            {
                writer.WriteStartArray();
                foreach (PathList.PathPair path in value.Paths)
                {
                    JsonSerializer.Serialize(writer, path, options);
                }
                writer.WriteEndArray();
            }
        }
    }

    /// <summary>
    /// JSON converter for path pairs within path lists.
    /// </summary>
    internal class PathPairConverter : JsonConverter<PathList.PathPair>
    {
        /// <inheritdoc/>
        public override PathList.PathPair Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Simple case: single string, same for source and destination
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString() ?? throw new JsonException();
                return new(value, value);
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected string path or path pair object");
            }

            // Source and destination paths
            string sourceValue = null, destinationValue = null;
            for (int i = 0; i < 2; i++)
            {
                if (!reader.Read())
                {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name");
                }
                string propertyName = reader.GetString() ?? throw new JsonException();
                if (!reader.Read())
                {
                    throw new JsonException();
                }
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException("Expected string path");
                }
                string value = reader.GetString() ?? throw new JsonException();
                if (propertyName == nameof(PathList.PathPair.Source))
                {
                    sourceValue = value;
                }
                else if (propertyName == nameof(PathList.PathPair.Destination))
                {
                    destinationValue = value;
                }
            }
            if (!reader.Read())
            {
                throw new JsonException();
            }
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("Expected end of path pair object");
            }

            if (sourceValue is null || destinationValue is null)
            {
                throw new JsonException($"Expected \"{nameof(PathList.PathPair.Source)}\" and \"{nameof(PathList.PathPair.Destination)}\" properties for path pair object");
            }
            return new(sourceValue, destinationValue);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, PathList.PathPair value, JsonSerializerOptions options)
        {
            if (value.Source == value.Destination)
            {
                writer.WriteStringValue(value.Source);
            }
            else
            {
                writer.WriteStartObject();
                writer.WriteString(nameof(PathList.PathPair.Source), value.Source);
                writer.WriteString(nameof(PathList.PathPair.Destination), value.Destination);
                writer.WriteEndObject();
            }
        }
    }

    /// <summary>
    /// JSON converter for path lists.
    /// </summary>
    internal class FileOperationConverter : JsonConverter<FileOperation>
    {
        /// <inheritdoc/>
        public override FileOperation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // If not an object, just assume this is a required operation that only has a path list
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return new()
                {
                    Paths = JsonSerializer.Deserialize<PathList>(ref reader, options),
                    Required = true
                };
            }

            // Otherwise, parse each field.
            bool readPaths = false;
            PathList paths = new();
            bool required = true;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected property name \"{nameof(FileOperation.Paths)}\" or \"{nameof(FileOperation.Required)}\"");
                }
                string propertyName = reader.GetString() ?? throw new JsonException();
                if (propertyName == nameof(FileOperation.Paths))
                {
                    if (!reader.Read())
                    {
                        throw new JsonException();
                    }
                    paths = JsonSerializer.Deserialize<PathList>(ref reader, options);
                    readPaths = true;
                }
                else if (propertyName == nameof(FileOperation.Required))
                {
                    if (!reader.Read() || reader.TokenType is not (JsonTokenType.True or JsonTokenType.False))
                    {
                        throw new JsonException($"Expected boolean for \"{nameof(FileOperation.Required)}\"");
                    }
                    required = reader.GetBoolean();
                }
            }
            if (!readPaths)
            {
                throw new JsonException($"Expected \"{nameof(FileOperation.Paths)}\" property in file operation");
            }

            return new()
            {
                Paths = paths,
                Required = required
            };
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, FileOperation value, JsonSerializerOptions options)
        {
            if (value.Required)
            {
                JsonSerializer.Serialize(writer, value.Paths);
            }
            else
            {
                writer.WriteStartObject();
                writer.WritePropertyName(nameof(FileOperation.Paths));
                JsonSerializer.Serialize(writer, value.Paths);
                writer.WriteBoolean(nameof(FileOperation.Required), value.Required);
                writer.WriteEndObject();
            }
        }
    }
}
