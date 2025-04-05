using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertalePath"/>.
/// </summary>
internal sealed class SerializablePath : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Path;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => false;

    /// <inheritdoc cref="UndertalePath.IsSmooth"/>
    public bool IsSmooth { get; set; }

    /// <inheritdoc cref="UndertalePath.IsClosed"/>
    public bool IsClosed { get; set; }

    /// <inheritdoc cref="UndertalePath.Precision"/>
    public uint Precision { get; set; }

    /// <inheritdoc cref="UndertalePath.Points"/>
    public List<PathPoint> Points { get; set; }

    /// <inheritdoc cref="UndertalePath.PathPoint"/>
    public sealed class PathPoint
    {
        /// <inheritdoc cref="UndertalePath.PathPoint.X"/>
        public float X { get; set; }

        /// <inheritdoc cref="UndertalePath.PathPoint.Y"/>
        public float Y { get; set; }

        /// <inheritdoc cref="UndertalePath.PathPoint.Speed"/>
        public float Speed { get; set; }
    }

    // Data asset that was located during pre-import.
    private UndertalePath _dataAsset = null;

    /// <summary>
    /// Populates this serializable path with data from an actual path.
    /// </summary>
    public void PopulateFromData(ProjectContext projectContext, UndertalePath path)
    {
        // Update all main properties
        DataName = path.Name.Content;
        IsSmooth = path.IsSmooth;
        IsClosed = path.IsClosed;
        Precision = path.Precision;
        Points = new(path.Points.Count);
        foreach (UndertalePath.PathPoint p in path.Points)
        {
            Points.Add(new()
            {
                X = p.X,
                Y = p.Y,
                Speed = p.Speed
            });
        }
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
        if (projectContext.Data.Paths.ByName(DataName) is UndertalePath existing)
        {
            // Path found
            _dataAsset = existing;
        }
        else
        {
            // No path found; create new one
            _dataAsset = new()
            {
                Name = projectContext.Data.Strings.MakeString(DataName)
            };
            projectContext.Data.Paths.Add(_dataAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertalePath path = _dataAsset;

        // Update all main properties
        path.IsSmooth = IsSmooth;
        path.IsClosed = IsClosed;
        path.Precision = Precision;
        path.Points.Clear();
        path.Points.SetCapacity(Precision);
        foreach (PathPoint p in Points)
        {
            path.Points.Add(new()
            {
                X = p.X,
                Y = p.Y,
                Speed = p.Speed
            });
        }

        return path;
    }
}
