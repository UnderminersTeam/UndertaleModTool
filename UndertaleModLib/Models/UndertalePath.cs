using System;
using System.ComponentModel;
using UndertaleModLib.Project;
using UndertaleModLib.Project.SerializableAssets;

namespace UndertaleModLib.Models;

/// <summary>
/// A Path entry in a GameMaker data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertalePath : UndertaleNamedResource, IProjectAsset, INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The name of <see cref="UndertalePath"/>.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Whether the Path is smooth between points, or is completely straight.
    /// </summary>
    public bool IsSmooth { get; set; }

    /// <summary>
    /// Whether this Path is closed, aka if the last <see cref="PathPoint"/> connecting back to the first.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// A number from 1 to 8 detailing how smooth a <see cref="UndertalePath"/> is between <see cref="PathPoint"/>,
    /// with 1 being completely straight and 8 being the smoothest. <br/>
    /// Only used if <see cref="IsSmooth"/> is enabled.
    /// </summary>
    public uint Precision { get; set; } = 4;

    /// <summary>
    /// The collection of <see cref="PathPoint"/>s this <see cref="UndertalePath"/> has.
    /// </summary>
    public UndertaleSimpleList<PathPoint> Points { get; set; } = new UndertaleSimpleList<PathPoint>();

    /// <inheritdoc />
#pragma warning disable CS0067 // TODO: remove this suppression once Fody is no longer in use
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <summary>
    /// A point in a <see cref="UndertalePath"/>.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class PathPoint : UndertaleObject, INotifyPropertyChanged, IStaticChildObjectsSize
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 12;

        /// <summary>
        /// The X position of the <see cref="PathPoint"/>.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// The Y position of the <see cref="PathPoint"/>.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// A percentage of how fast an <see cref="UndertaleGameObject"/> moves until it hits the next <see cref="PathPoint"/>. <br/>.
        /// </summary>
        public float Speed { get; set; } = 1f;

        /// <inheritdoc />
#pragma warning disable CS0067 // TODO: remove this suppression once Fody is no longer in use
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Speed);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Speed = reader.ReadSingle();
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write(IsSmooth);
        writer.Write(IsClosed);
        writer.Write(Precision);
        writer.WriteUndertaleObject(Points);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        IsSmooth = reader.ReadBoolean();
        IsClosed = reader.ReadBoolean();
        Precision = reader.ReadUInt32();
        Points = reader.ReadUndertaleObject<UndertaleSimpleList<PathPoint>>();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 16;

        return 1 + UndertaleSimpleList<PathPoint>.UnserializeChildObjectCount(reader);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        Points = new();
    }

    /// <inheritdoc/>
    ISerializableProjectAsset IProjectAsset.GenerateSerializableProjectAsset(ProjectContext projectContext)
    {
        SerializablePath serializable = new();
        serializable.PopulateFromData(projectContext, this);
        return serializable;
    }

    /// <inheritdoc/>
    public string ProjectName => Name?.Content ?? "<unknown name>";

    /// <inheritdoc/>
    public SerializableAssetType ProjectAssetType => SerializableAssetType.Path;

    /// <inheritdoc/>
    public bool ProjectExportable => true;
}