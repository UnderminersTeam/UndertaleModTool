using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;
using UndertaleModLib.Project.Json;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleAnimationCurve"/>.
/// </summary>
internal sealed class SerializableAnimationCurve : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.AnimationCurve;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => false;

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    /// <inheritdoc cref="UndertaleAnimationCurve.GraphType"/>
    public int GraphType { get; set; }

    /// <inheritdoc cref="UndertaleAnimationCurve.Channels"/>
    public List<Channel> Channels { get; set; }

    /// <inheritdoc cref="UndertaleAnimationCurve.Channel"/>
    public sealed class Channel
    {
        /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Name"/>
        public string Name { get; set; }

        /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Curve"/>
        public int Curve { get; set; }

        /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Iterations"/>
        public uint Iterations { get; set; }

        /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Points"/>
        [JsonConverter(typeof(NoPrettyPrintJsonConverter<List<Point>>))]
        public List<Point> Points { get; set; }

        /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Point"/>
        public sealed class Point
        {
            /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Point.X"/>
            public float X { get; set; }

            /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Point.Value"/>
            public float Value { get; set; }

            /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Point.BezierX0"/>
            public float BezierX0 { get; set; }

            /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Point.BezierY0"/>
            public float BezierY0 { get; set; }

            /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Point.BezierX1"/>
            public float BezierX1 { get; set; }

            /// <inheritdoc cref="UndertaleAnimationCurve.Channel.Point.BezierY1"/>
            public float BezierY1 { get; set; }
        }
    }

    // Data asset that was located during pre-import.
    private UndertaleAnimationCurve _dataAsset = null;

    /// <summary>
    /// Populates this serializable animation curve with data from an actual animation curve.
    /// </summary>
    internal void PopulateFromData(ProjectContext projectContext, UndertaleAnimationCurve curve)
    {
        // Update all main properties
        DataName = curve.Name?.Content;
        GraphType = (int)curve.GraphType;
        Channels = new(curve.Channels.Count);
        foreach (var channel in curve.Channels)
        {
            Channels.Add(new Channel()
            {
                Name = channel.Name?.Content,
                Curve = (int)channel.Curve,
                Iterations = channel.Iterations,
                Points = [.. channel.Points.Select((point) => new Channel.Point()
                {
                    X = point.X,
                    Value = point.Value,
                    BezierX0 = point.BezierX0,
                    BezierX1 = point.BezierX1,
                    BezierY0 = point.BezierY0,
                    BezierY1 = point.BezierY1
                })]
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
        if (projectContext.Data.AnimationCurves.ByName(DataName) is UndertaleAnimationCurve existing)
        {
            // Animation curve found
            _dataAsset = existing;
        }
        else
        {
            // No animation curve found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.AnimationCurves.Add(_dataAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleAnimationCurve curve = _dataAsset;

        // Update all main properties
        curve.GraphType = (UndertaleAnimationCurve.GraphTypeEnum)GraphType;
        if (curve.Channels is null)
        {
            curve.Channels = new(Channels.Count);
        }
        else
        {
            curve.Channels.Clear();
            curve.Channels.SetCapacity(Channels.Count);
        }
        foreach (var channel in Channels)
        {
            curve.Channels.Add(new UndertaleAnimationCurve.Channel()
            {
                Name = projectContext.MakeString(channel.Name),
                Curve = (UndertaleAnimationCurve.Channel.CurveType)channel.Curve,
                Iterations = channel.Iterations,
                Points = [.. channel.Points.Select((point) => new UndertaleAnimationCurve.Channel.Point()
                {
                    X = point.X,
                    Value = point.Value,
                    BezierX0 = point.BezierX0,
                    BezierX1 = point.BezierX1,
                    BezierY0 = point.BezierY0,
                    BezierY1 = point.BezierY1
                })]
            });
        }

        return curve;
    }

    /// <summary>
    /// Imports this asset as a sub-resource of another asset.
    /// </summary>
    internal UndertaleAnimationCurve ImportSubResource(ProjectContext projectContext)
    {
        _dataAsset = new UndertaleAnimationCurve();
        if (DataName is not null)
        {
            _dataAsset.Name = projectContext.MakeString(DataName);
        }
        Import(projectContext);
        return _dataAsset;
    }
}
