using System;

namespace UndertaleModLib.Models;

/// <summary>
/// An animation curve entry in a data file. These were introduced in GameMaker 2.3.0
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleAnimationCurve : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// TODO: unknown
    /// </summary>
    public enum GraphTypeEnum : uint
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown0 = 0,
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown1 = 1
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
            Smooth = 1
            // TODO: What about bezier?
        }

        /// <inheritdoc />
        public UndertaleString Name { get; set; }
        
        /// <summary>
        /// The curve type this channel uses. 
        /// </summary>
        public CurveType Curve { get; set; }
        
        /// <summary>
        /// TODO: document this
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

            // "Points"
            uint count = reader.ReadUInt32();
            if (reader.undertaleData.IsVersionAtLeast(2, 3, 1))
                reader.Position += 24 * count;
            else
                reader.Position += 12 * count;

            return 1 + count;
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
}