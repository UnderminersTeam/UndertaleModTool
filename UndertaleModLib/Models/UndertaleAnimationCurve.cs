using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: INotifyPropertyChanged
    public class UndertaleAnimationCurve : UndertaleNamedResource
    {
        public enum GraphTypeEnum : uint
        {
            Unknown0 = 0,
            Unknown1 = 1
        }

        public UndertaleString Name { get; set; }
        public GraphTypeEnum GraphType { get; set; }
        public UndertaleSimpleList<Channel> Channels { get; set; }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write((uint)GraphType);
            Channels.Serialize(writer);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            GraphType = (GraphTypeEnum)reader.ReadUInt32();
            Channels = reader.ReadUndertaleObject<UndertaleSimpleList<Channel>>();
        }

        public override string ToString()
        {
            return Name.Content;
        }

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
                public float X { get; set; }
                public float Value { get; set; }

                public void Serialize(UndertaleWriter writer)
                {
                    writer.Write(X);
                    writer.Write(Value);
                    writer.Write((int)0);
                }

                public void Unserialize(UndertaleReader reader)
                {
                    X = reader.ReadSingle();
                    Value = reader.ReadSingle();
                    if (reader.ReadInt32() != 0)
                        throw new Exception("Expected 0 in animation curve point"); // TODO? They might add some "control points" here later it seems
                }
            }
        }
    }
}
