using System;
using UndertaleModLib.Project.SerializableAssets;
using UndertaleModLib.Project;

namespace UndertaleModLib.Models;

/// <summary>
/// An animation curve entry in a data file. These were introduced in GameMaker 2.3.0
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleAnimationCurve : UndertaleNamedResource, IProjectAsset, IDisposable
{
    /// <summary>
    /// Unused enum: The curve type is set per channel unlike in GMS2 where it is set per animation curve
    /// </summary>
    public enum GraphTypeEnum : uint
    {
        /// <summary>
        /// Value is never set by the compiler
        /// </summary>
        Bezier = 0,
        /// <summary>
        /// Value is always set by the compiler
        /// </summary>
        Graph = 1
    }

    /// <summary>
    /// The name of this animation curve.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The graph type of this animation curve.
    /// </summary>
    public GraphTypeEnum GraphType { get; set; }
    
    /// <summary>
    /// The channels this animation curve has. 
    /// </summary>
    public UndertaleSimpleList<Channel> Channels { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        Serialize(writer, true);
    }

    /// <summary>
    /// Serializes the data file into a specified <see cref="UndertaleWriter"/>.
    /// </summary>
    /// <param name="writer">Where to serialize to.</param>
    /// <param name="includeName">Whether to include <see cref="Name"/> in the serialization.</param>
    public void Serialize(UndertaleWriter writer, bool includeName)
    {
        if (includeName)
            writer.WriteUndertaleString(Name);
        writer.Write((uint)GraphType);
        Channels.Serialize(writer);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Unserialize(reader, true);
    }

    /// <summary>
    /// Deserializes from a specified <see cref="UndertaleReader"/> to the current data file.
    /// </summary>
    /// <param name="reader">Where to deserialize from.</param>
    /// <param name="includeName">Whether to include <see cref="Name"/> in the deserialization.</param>
    public void Unserialize(UndertaleReader reader, bool includeName)
    {
        if (includeName)
            Name = reader.ReadUndertaleString();
        GraphType = (GraphTypeEnum)reader.ReadUInt32();
        Channels = reader.ReadUndertaleObject<UndertaleSimpleList<Channel>>();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        return UnserializeChildObjectCount(reader, true);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    /// <param name="reader">Where to deserialize from.</param>
    /// <param name="includeName">Whether to include <see cref="Name"/> in the deserialization.</param>
    public static uint UnserializeChildObjectCount(UndertaleReader reader, bool includeName)
    {
        if (!includeName)
            reader.Position += 4;     // "GraphType"
        else
            reader.Position += 4 + 4; // + "Name"

        return 1 + UndertaleSimpleList<Channel>.UnserializeChildObjectCount(reader);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (Channels is not null)
        {
            foreach (Channel channel in Channels)
                channel?.Dispose();
        }
        Name = null;
        Channels = null;
    }
    
    /// <summary>
    /// A channel in an animation curve.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class Channel : UndertaleNamedResource, IDisposable
    {
        /// <summary>
        /// The curve type determines how points flow to each other in a channel.
        /// </summary>
        public enum CurveType : uint
        {
            /// <summary>
            /// Creates a linear progression between points.
            /// </summary>
            Linear = 0,
            /// <summary>
            /// Creates a smooth progression between points using catmull-rom interpolation.
            /// </summary>
            Smooth = 1,
            /// <summary>
            /// Creates a curved progression by using points as the control points of a 2D Bézier curve.
            /// </summary>
            Bezier = 2
        }

        /// <inheritdoc />
        public UndertaleString Name { get; set; }
        
        /// <summary>
        /// The curve type this channel uses. 
        /// </summary>
        public CurveType Curve { get; set; }
        
        /// <summary>
        /// The amount of resolution generated between control points in both Catmull-Rom (smooth) and Bezier interpolation.
        /// </summary>
        public uint Iterations { get; set; }
        
        /// <summary>
        /// The points in the channel.
        /// </summary>
        public UndertaleSimpleList<Point> Points { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write((uint)Curve);
            writer.Write(Iterations);
            Points.Serialize(writer);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Curve = (CurveType)reader.ReadUInt32();
            Iterations = reader.ReadUInt32();

            Points = reader.ReadUndertaleObject<UndertaleSimpleList<Point>>();
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            reader.Position += 12;

            // Read the number of points in the curve
            uint pointCount = reader.ReadUInt32();

            // This check is partly duplicated from UndertaleChunks.cs, but it's necessary to handle embedded curves
            // (For example, those in SEQN in the TS!Underswap v1.0 demo; see issue #1414)
            if (!reader.undertaleData.IsVersionAtLeast(2, 3, 1))
            {
                long returnPosition = reader.AbsPosition;
                if (pointCount > 0)
                {
                    reader.AbsPosition += 8;
                    if (reader.ReadUInt32() != 0) // In 2.3 an int with the value of 0 would be set here,
                    {                             // It cannot be version 2.3 if this value isn't 0
                        reader.undertaleData.SetGMS2Version(2, 3, 1);
                    }
                    else
                    {
                        if (reader.ReadUInt32() == 0)                      // At all points (besides the first one)
                            reader.undertaleData.SetGMS2Version(2, 3, 1);  // If BezierX0 equals to 0 (the above check)
                                                                           // Then BezierY0 equals to 0 as well (the current check)
                    }
                }
                reader.AbsPosition = returnPosition;
            }

            // "Points"
            if (reader.undertaleData.IsVersionAtLeast(2, 3, 1))
                reader.Position += 24 * pointCount;
            else
                reader.Position += 12 * pointCount;

            return 1 + pointCount;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Name = null;
            Points = null;
        }

        /// <summary>
        /// A point which can exist on a <see cref="Channel"/>.
        /// </summary>
        public class Point : UndertaleObject
        {
            /// <summary>
            /// The X coordinate of this point. GameMaker abbreviates this to "h".
            /// </summary>
            public float X;
            
            /// <summary>
            /// The Y coordinate of this point. GameMaker abbreviates this to "v".
            /// </summary>
            public float Value;

            /// <summary>
            /// The Y position for the first bezier handle. Only used if the Channel is set to Bezier.
            /// </summary>
            public float BezierX0;
            
            /// <summary>
            /// The Y position for the first bezier handle. Only used if the Channel is set to Bezier.
            /// </summary>
            public float BezierY0;
            
            /// <summary>
            /// The X position for the second bezier handle. Only used if the Channel is set to Bezier.
            /// </summary>
            public float BezierX1;
            
            /// <summary>
            /// The Y position for the second bezier handle. Only used if the Channel is set to Bezier.
            /// </summary>
            public float BezierY1;

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Value);

                if (writer.undertaleData.IsVersionAtLeast(2, 3, 1))
                {
                    writer.Write(BezierX0);
                    writer.Write(BezierY0);
                    writer.Write(BezierX1);
                    writer.Write(BezierY1);
                }
                else
                    writer.Write(0);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                X = reader.ReadSingle();
                Value = reader.ReadSingle();

                if (reader.undertaleData.IsVersionAtLeast(2, 3, 1))
                {
                    BezierX0 = reader.ReadSingle();
                    BezierY0 = reader.ReadSingle();
                    BezierX1 = reader.ReadSingle();
                    BezierY1 = reader.ReadSingle();
                }
                else
                    reader.Position += 4;
            }
        }
    }

    /// <inheritdoc/>
    internal ISerializableProjectAsset GenerateSerializableProjectAsset(ProjectContext projectContext)
    {
        SerializableAnimationCurve serializable = new();
        serializable.PopulateFromData(projectContext, this);
        return serializable;
    }

    /// <inheritdoc/>
    ISerializableProjectAsset IProjectAsset.GenerateSerializableProjectAsset(ProjectContext projectContext)
    {
        SerializableAnimationCurve serializable = new();
        serializable.PopulateFromData(projectContext, this);
        return serializable;
    }

    /// <inheritdoc/>
    public string ProjectName => Name?.Content ?? "<unknown name>";

    /// <inheritdoc/>
    public SerializableAssetType ProjectAssetType => SerializableAssetType.AnimationCurve;

    /// <inheritdoc/>
    public bool ProjectExportable => Name?.Content is not null;
}
