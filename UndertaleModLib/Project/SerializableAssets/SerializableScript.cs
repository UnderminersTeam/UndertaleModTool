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

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    /// <inheritdoc cref="UndertaleScript.Code"/>
    public string Code { get; set; }

    // Data asset that was located during pre-import.
    private UndertaleScript _dataAsset = null;

    /// <summary>
    /// Populates this serializable script with data from an actual script.
    /// </summary>
    internal void PopulateFromData(ProjectContext projectContext, UndertaleScript script)
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

            // Check whether this is a room creation script that needs an index-matching code entry
            const string roomPrefix = "gml_Room";
            if (DataName.StartsWith(roomPrefix) && projectContext.Data.IsVersionAtLeast(2023, 11))
            {
                // Check if code entry exists with this prefix already. If so, try to reuse its index.
                int insertIndex = projectContext.Data.Code.IndexOfName(DataName);
                if (insertIndex == -1)
                {
                    // No code found, so find script/code list divergence index, and use that
                    insertIndex = UndertaleCode.FindCodeScriptIndexDivergence(projectContext.Data);
                }
                else if (insertIndex > projectContext.Data.Scripts.Count)
                {
                    throw new ProjectException($"Failed to import room script {DataName} at correct index; " +
                                               $"code entry already exists at invalid index {insertIndex}");
                }
                else
                {
                    // Assign code entry we found to this new script, so divergence calculations are correct
                    _dataAsset.Code = projectContext.Data.Code[insertIndex];
                    _dataAsset.IsConstructor = false;
                }

                // Insert at desired index
                projectContext.Data.Scripts.Insert(insertIndex, _dataAsset);
            }
            else
            {
                projectContext.Data.Scripts.Add(_dataAsset);
            }
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
