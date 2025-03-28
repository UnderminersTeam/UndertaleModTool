using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleCode"/>.
/// </summary>
internal sealed class SerializableCode : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc cref="UndertaleCode.WeirdLocalFlag"/>
    public bool WeirdLocalFlag { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Code;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => false;

    // Data code that was located during pre-import, or during export.
    private UndertaleCode _foundAsset = null;

    /// <summary>
    /// Populates this serializable path with data from an actual path.
    /// </summary>
    public void PopulateFromData(ProjectContext projectContext, UndertaleCode code)
    {
        // Update all main properties
        DataName = code.Name.Content;
        WeirdLocalFlag = code.WeirdLocalFlag;

        _foundAsset = code;
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        // Write main JSON
        using (FileStream fs = new(destinationFile, FileMode.Create))
        {
            JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);
        }

        // Write GML
        string filename = $"{Path.GetFileNameWithoutExtension(destinationFile)}.gml";
        string directory = Path.GetDirectoryName(destinationFile);
        using (FileStream fs = new(Path.Combine(directory, filename), FileMode.Create))
        {
            projectContext.TryGetCodeSource(_foundAsset, out string source);
            source ??= "// Project system failed to retrieve source code for GML"; // This should actually never happen, but as a failsafe...
            fs.Write(Encoding.UTF8.GetBytes(source));
        }
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Code.ByName(DataName) is UndertaleCode existing)
        {
            // Code found
            _foundAsset = existing;
        }
        else
        {
            // No code found; create new one
            _foundAsset = new()
            {
                Name = projectContext.Data.Strings.MakeString(DataName)
            };
            projectContext.Data.Code.Add(_foundAsset);

            // Also create code locals, if applicable
            if (projectContext.Data.CodeLocals is not null)
            {
                UndertaleCodeLocals.CreateEmptyEntry(projectContext.Data, _foundAsset.Name);
            }
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleCode code = _foundAsset;

        // Update all main properties
        code.WeirdLocalFlag = WeirdLocalFlag;
        
        return code;
    }

    /// <summary>
    /// Queues this code asset to be compiled.
    /// </summary>
    public void ImportCode(ProjectContext projectContext, CodeImportGroup group)
    {
        // Get JSON filename (of main asset file)
        if (!projectContext.AssetDataNamesToPaths.TryGetValue((DataName, AssetType), out string jsonFilename))
        {
            throw new Exception("Failed to get code asset path");
        }

        // Load GML from disk, to be compiled
        string filename = $"{Path.GetFileNameWithoutExtension(jsonFilename)}.gml";
        string directory = Path.GetDirectoryName(jsonFilename);
        try
        {
            // Read text
            string source = File.ReadAllText(Path.Combine(directory, filename));

            // Queue for code replacement
            group.QueueReplace(_foundAsset, source);

            // Update source within the project itself
            projectContext.UpdateCodeSource(_foundAsset, source);
        }
        catch (Exception e)
        {
            throw new ProjectException($"Failed to import GML code file named \"{filename}\": {e.Message}", e);
        }
    }
}
