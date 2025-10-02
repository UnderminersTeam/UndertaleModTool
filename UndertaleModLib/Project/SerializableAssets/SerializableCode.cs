﻿using System;
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

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    // Data asset that was located during pre-import, or during export.
    private UndertaleCode _dataAsset = null;

    /// <summary>
    /// Populates this serializable code with data from an actual code asset.
    /// </summary>
    internal void PopulateFromData(ProjectContext projectContext, UndertaleCode code)
    {
        // Update all main properties
        DataName = code.Name.Content;
        WeirdLocalFlag = code.WeirdLocalFlag;

        _dataAsset = code;
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        // Write main JSON, only if non-default properties are required
        if (DataName is not null || WeirdLocalFlag)
        {
            using FileStream fs = new(destinationFile, FileMode.Create);
            JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);
        }

        // Write GML
        string filename = $"{Path.GetFileNameWithoutExtension(destinationFile)}.gml";
        string directory = Path.GetDirectoryName(destinationFile);
        using (FileStream fs = new(Path.Join(directory, filename), FileMode.Create))
        {
            projectContext.TryGetCodeSource(_dataAsset, out string source);
            if (source is null)
            {
                throw new ProjectException($"Failed to find source code for {_dataAsset.Name?.Content ?? "<unknown code entry name>"}");
            }
            fs.Write(Encoding.UTF8.GetBytes(source));
        }
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Code.ByName(DataName) is UndertaleCode existing)
        {
            // Code found
            _dataAsset = existing;
        }
        else
        {
            // No code found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.Code.Add(_dataAsset);

            // Also create code locals, if applicable
            if (projectContext.Data.CodeLocals is not null)
            {
                UndertaleCodeLocals.CreateEmptyEntry(projectContext.Data, _dataAsset.Name);
            }
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleCode code = _dataAsset;

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
        if (!projectContext.AssetDataNamesToPaths.TryGetValue(new(DataName, AssetType), out string jsonFilename))
        {
            throw new ProjectException("Failed to get code asset path");
        }

        // Load GML from disk, to be compiled
        string filename = $"{Path.GetFileNameWithoutExtension(jsonFilename)}.gml";
        string directory = Path.GetDirectoryName(jsonFilename);
        try
        {
            // Read text
            string source = File.ReadAllText(Path.Join(directory, filename));

            // Queue for code replacement
            group.QueueReplace(_dataAsset, source);

            // Update source within the project itself
            projectContext.UpdateCodeSource(_dataAsset, source);
        }
        catch (Exception e)
        {
            throw new ProjectException($"Failed to import GML code file named \"{filename}\": {e.Message}", e);
        }

        // If this is a global init script, add to global init list if not already present
        const string globalScriptPrefix = "gml_GlobalScript_";
        string codeAssetName = _dataAsset.Name.Content;
        if (codeAssetName.StartsWith(globalScriptPrefix, StringComparison.Ordinal))
        {
            // Scan to see if there's a global init script already
            UndertaleGlobalInit existingGlobalInit = null;
            foreach (UndertaleGlobalInit globalInit in projectContext.Data.GlobalInitScripts)
            {
                if (globalInit.Code.Name.Content == codeAssetName)
                {
                    existingGlobalInit = globalInit;
                    break;
                }
            }
            if (existingGlobalInit is null)
            {
                // Create a new global init entry
                projectContext.Data.GlobalInitScripts.Add(new()
                {
                    Code = _dataAsset
                });
            }
            else
            {
                // Attach to existing global init entry
                existingGlobalInit.Code = _dataAsset;
            }
        }
    }
}
