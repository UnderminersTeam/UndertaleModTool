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

    // Data asset that was located during pre-import.
    private UndertaleScript _dataAsset = null;

    /// <summary>
    /// Populates this serializable script with data from an actual script.
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
            _dataAsset = existing;
        }
        else
        {
            // No script found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.Scripts.Add(_dataAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleScript script = _dataAsset;

        // Update all main properties
        script.Code = projectContext.FindCode(Code, this);
        script.IsConstructor = false;

        return script;
    }
}
