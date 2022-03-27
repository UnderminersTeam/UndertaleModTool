using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    /// <summary>
    /// An animation curve entry in a data file.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleAnimationCurve : UndertaleNamedResource
    {
        public enum GraphTypeEnum : uint
        {
            Unknown0 = 0,
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


        public UndertaleSimpleList<Channel> Channels { get; set; }

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

        public override string ToString()
        {
            return Name.Content;
        }

        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class Channel : UndertaleObject
        {
            public enum FunctionType : uint
            {
                Linear = 0,
                Smooth = 1
            }

            public UndertaleString Name { get; set; }
            public FunctionType Function { get; set; }
            public uint Iterations { get; set; }
            public UndertaleSimpleList<Point> Points { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Name);
                writer.Write((uint)Function);
                writer.Write(Iterations);
                Points.Serialize(writer);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Name = reader.ReadUndertaleString();
                Function = (FunctionType)reader.ReadUInt32();
                Iterations = reader.ReadUInt32();
                Points = reader.ReadUndertaleObject<UndertaleSimpleList<Point>>();
            }

            public class Point : UndertaleObject
            {
                public float X;
                public float Value;

                public float BezierX0; // Bezier only
                public float BezierY0;
                public float BezierX1;
                public float BezierY1;

                public void Serialize(UndertaleWriter writer)
                {
                    writer.Write(X);
                    writer.Write(Value);

                    if (writer.undertaleData.GMS2_3_1)
                    {
                        writer.Write(BezierX0);
                        writer.Write(BezierY0);
                        writer.Write(BezierX1);
                        writer.Write(BezierY1);
                    }
                    else
                        writer.Write(0);
                }

                public void Unserialize(UndertaleReader reader)
                {
                    X = reader.ReadSingle();
                    Value = reader.ReadSingle();

                    if (reader.ReadUInt32() != 0) // in 2.3 a int with the value of 0 would be set here,
                    {                             // it cannot be version 2.3 if this value isn't 0
                        reader.undertaleData.GMS2_3_1 = true;
                        reader.Position -= 4;
                    }
                    else
                    {
                        if (reader.ReadUInt32() == 0)              // At all points (besides the first one)
                            reader.undertaleData.GMS2_3_1 = true; // if BezierX0 equals to 0 (the above check)
                        reader.Position -= 8;                        // then BezierY0 equals to 0 as well (the current check)
                    }

                    if (reader.undertaleData.GMS2_3_1)
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
}
