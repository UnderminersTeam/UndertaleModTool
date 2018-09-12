using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertalePath : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private bool _IsSmooth = false;
        private bool _IsClosed = false;
        private uint _Precision = 4;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public bool IsSmooth { get => _IsSmooth; set { _IsSmooth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSmooth")); } }
        public bool IsClosed { get => _IsClosed; set { _IsClosed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsClosed")); } }
        public uint Precision { get => _Precision; set { _Precision = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Precision")); } }
        public UndertaleSimpleList<PathPoint> Points { get; private set; } = new UndertaleSimpleList<PathPoint>();

        public event PropertyChangedEventHandler PropertyChanged;

        public class PathPoint : UndertaleObject, INotifyPropertyChanged
        {
            private float _X;
            private float _Y;
            private float _Speed;

            public float X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public float Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }
            public float Speed { get => _Speed; set { _Speed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Speed")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.Write(Speed);
            }

            public void Unserialize(UndertaleReader reader)
            {
                X = reader.ReadSingle();
                Y = reader.ReadSingle();
                Speed = reader.ReadSingle();
            }
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(IsSmooth);
            writer.Write(IsClosed);
            writer.Write(Precision);
            writer.WriteUndertaleObject(Points);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            IsSmooth = reader.ReadBoolean();
            IsClosed = reader.ReadBoolean();
            Precision = reader.ReadUInt32();
            Points = reader.ReadUndertaleObject<UndertaleSimpleList<PathPoint>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
