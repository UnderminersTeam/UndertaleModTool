using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleScript"/>.
/// </summary>
internal sealed class SerializableScript : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Script;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => false;

    /// <inheritdoc cref="UndertaleScript.Code"/>
    public string Code { get; set; }

    // Data script that was located during pre-import.
    private UndertaleScript _preImportAsset = null;

    /// <summary>
    /// Populates this serializable path with data from an actual path.
    /// </summary>
    public void PopulateFromData(ProjectContext projectContext, UndertaleScript script)
    {
        // Update all main properties
        DataName = script.Name.Content;
        Code = script.Code?.Name?.Content;
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        using FileStream fs = new(destinationFile, FileMode.Create);
        JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Scripts.ByName(DataName) is UndertaleScript existing)
        {
            // Script found
            _preImportAsset = existing;
        }
        else
        {
            // No script found; create new one
            _preImportAsset = new()
            {
                Name = projectContext.Data.Strings.MakeString(DataName)
            };
            projectContext.Data.Scripts.Add(_preImportAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleScript script = _preImportAsset;

        // Update all main properties
        script.Code = projectContext.FindCode(Code, this);
        script.IsConstructor = false;

        return script;
    }
}
