using System;
using System.ComponentModel;
using PropertyChanged.SourceGenerator;

namespace UndertaleModLib.Models;

/// <summary>
/// A Path entry in a GameMaker data file.
/// </summary>
public partial class UndertalePath : UndertaleNamedResource, INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The name of <see cref="UndertalePath"/>.
    /// </summary>
    [Notify("Name")] private UndertaleString _name;

    /// <summary>
    /// Whether the Path is smooth between points, or is completely straight.
    /// </summary>
    [Notify("IsSmooth")] private bool _isSmooth;

    /// <summary>
    /// Whether this Path is closed, aka if the last <see cref="PathPoint"/> connecting back to the first.
    /// </summary>
    [Notify("IsClosed")] private bool _isClosed;

    /// <summary>
    /// A number from 1 to 8 detailing how smooth a <see cref="UndertalePath"/> is between <see cref="PathPoint"/>,
    /// with 1 being completely straight and 8 being the smoothest. <br/>
    /// Only used if <see cref="IsSmooth"/> is enabled.
    /// </summary>
    [Notify("Precision")] private uint _precision = 4;

    /// <summary>
    /// The collection of <see cref="PathPoint"/>s this <see cref="UndertalePath"/> has.
    /// </summary>
    [Notify("Points")] private UndertaleSimpleList<PathPoint> _points = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// A point in a <see cref="UndertalePath"/>.
    /// </summary>
    public partial class PathPoint : UndertaleObject, INotifyPropertyChanged, IStaticChildObjectsSize
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 12;

        /// <summary>
        /// The X position of the <see cref="PathPoint"/>.
        /// </summary>
        [Notify("X")] private float _x;

        /// <summary>
        /// The Y position of the <see cref="PathPoint"/>.
        /// </summary>
        [Notify("Y")] private float _y;

        /// <summary>
        /// A percentage of how fast an <see cref="UndertaleGameObject"/> moves until it hits the next <see cref="PathPoint"/>. <br/>.
        /// </summary>
        [Notify("Speed")] private float _speed = 1f;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(_x);
            writer.Write(_y);
            writer.Write(_speed);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            _x = reader.ReadSingle();
            _y = reader.ReadSingle();
            _speed = reader.ReadSingle();
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(_name);
        writer.Write(_isSmooth);
        writer.Write(_isClosed);
        writer.Write(_precision);
        writer.WriteUndertaleObject(_points);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        _name = reader.ReadUndertaleString();
        _isSmooth = reader.ReadBoolean();
        _isClosed = reader.ReadBoolean();
        _precision = reader.ReadUInt32();
        _points = reader.ReadUndertaleObject<UndertaleSimpleList<PathPoint>>();
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
}
