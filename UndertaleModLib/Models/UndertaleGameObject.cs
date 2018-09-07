using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public enum CollisionShapeFlags : uint
    {
        Circle = 0,
        Box = 1,
        Custom = 2,
    }

    public class UndertaleGameObject : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private UndertaleResourceById<UndertaleSprite> _Sprite { get; } = new UndertaleResourceById<UndertaleSprite>("SPRT");
        private bool _Visible;
        private bool _Solid;
        private int _Depth;
        private bool _Persistent;
        private UndertaleResourceById<UndertaleGameObject> _ParentId { get; } = new UndertaleResourceById<UndertaleGameObject>("OBJT");
        private UndertaleResourceById<UndertaleSprite> _TextureMaskId { get; } = new UndertaleResourceById<UndertaleSprite>("SPRT");
        private bool _UsesPhysics;
        private bool _IsSensor;
        private CollisionShapeFlags _CollisionShape;
        private float _Density;
        private float _Restitution;
        private uint _Group;
        private float _LinearDamping;
        private float _AngularDamping;
        private float _Unknown1;
        private float _Friction;
        private uint _Unknown2;
        private bool _Kinematic;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleSprite Sprite { get => _Sprite.Resource; set { _Sprite.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Sprite")); } }
        public bool Visible { get => _Visible; set { _Visible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible")); } }
        public bool Solid { get => _Solid; set { _Solid = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Solid")); } }
        public int Depth { get => _Depth; set { _Depth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Depth")); } }
        public bool Persistent { get => _Persistent; set { _Persistent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Persistent")); } }
        public UndertaleGameObject ParentId { get => _ParentId.Resource; set { _ParentId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ParentId")); } }
        public UndertaleSprite TextureMaskId { get => _TextureMaskId.Resource; set { _TextureMaskId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TextureMaskId")); } }
        public bool UsesPhysics { get => _UsesPhysics; set { _UsesPhysics = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UsesPhysics")); } }
        public bool IsSensor { get => _IsSensor; set { _IsSensor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSensor")); } }
        public CollisionShapeFlags CollisionShape { get => _CollisionShape; set { _CollisionShape = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollisionShape")); } }
        public float Density { get => _Density; set { _Density = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Density")); } }
        public float Restitution { get => _Restitution; set { _Restitution = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Restitution")); } }
        public uint Group { get => _Group; set { _Group = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Group")); } }
        public float LinearDamping { get => _LinearDamping; set { _LinearDamping = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LinearDamping")); } }
        public float AngularDamping { get => _AngularDamping; set { _AngularDamping = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AngularDamping")); } }
        public float Unknown1 { get => _Unknown1; set { _Unknown1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown1")); } }
        public float Friction { get => _Friction; set { _Friction = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Friction")); } }
        public uint Unknown2 { get => _Unknown2; set { _Unknown2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown2")); } }
        public bool Kinematic { get => _Kinematic; set { _Kinematic = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kinematic")); } }
        public UndertalePointerList<UndertalePointerList<CodeEvent>> Events { get; private set; } = new UndertalePointerList<UndertalePointerList<CodeEvent>>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(_Sprite.Serialize(writer));
            writer.Write(Visible);
            writer.Write(Solid);
            writer.Write(Depth);
            writer.Write(Persistent);
            // This apparently has a different notation than everything else...
            if (_ParentId.Resource == null)
            {
                writer.Write((int)-100);
            }
            else
            {
                writer.Write(_ParentId.Serialize(writer));
            }
            writer.Write(_TextureMaskId.Serialize(writer));
            writer.Write(UsesPhysics);
            writer.Write(IsSensor);
            writer.Write((uint)CollisionShape);
            writer.Write(Density);
            writer.Write(Restitution);
            writer.Write(Group);
            writer.Write(LinearDamping);
            writer.Write(AngularDamping);
            writer.Write(Unknown1);
            writer.Write(Friction);
            writer.Write(Unknown2);
            writer.Write(Kinematic);
            writer.WriteUndertaleObject(Events);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            _Sprite.Unserialize(reader, reader.ReadInt32());
            Visible = reader.ReadBoolean();
            Solid = reader.ReadBoolean();
            Depth = reader.ReadInt32();
            Persistent = reader.ReadBoolean();
            int parent = reader.ReadInt32();
            if (parent == -100)
            {
                _ParentId.Unserialize(reader, -1);
            }
            else
            {
                Debug.Assert(parent >= 0);
                _ParentId.Unserialize(reader, parent);
            }
            _TextureMaskId.Unserialize(reader, reader.ReadInt32());
            UsesPhysics = reader.ReadBoolean();
            IsSensor = reader.ReadBoolean();
            CollisionShape = (CollisionShapeFlags)reader.ReadUInt32();
            Density = reader.ReadSingle();
            Restitution = reader.ReadSingle();
            Group = reader.ReadUInt32();
            LinearDamping = reader.ReadSingle();
            AngularDamping = reader.ReadSingle();
            Unknown1 = reader.ReadSingle();
            Friction = reader.ReadSingle();
            Unknown2 = reader.ReadUInt32();
            Kinematic = reader.ReadBoolean();
            Events = reader.ReadUndertaleObject<UndertalePointerList<UndertalePointerList<CodeEvent>>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public class CodeEvent : UndertaleObject, INotifyPropertyChanged
        {
            private uint _EventSubtype;

            public uint EventSubtype { get => _EventSubtype; set { _EventSubtype = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EventSubtype")); } } // the ID at the end of name, subtype for some events, 0 if unused
            public UndertalePointerList<EventCodeBlock> CodeBlock { get; private set; } = new UndertalePointerList<EventCodeBlock>(); // seems to always have 1 entry, maybe the games using drag-and-drop code are different // TODO: this is actually an index into FunctionDefinitions

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(EventSubtype);
                writer.WriteUndertaleObject(CodeBlock);
            }

            public void Unserialize(UndertaleReader reader)
            {
                EventSubtype = reader.ReadUInt32();
                CodeBlock = reader.ReadUndertaleObject<UndertalePointerList<EventCodeBlock>>();
            }
        }

        public enum EventType : uint
        {
            Create = 0, // no subtypes
            Destroy = 1, // no subtypes
            Alarm = 2, // subtype is alarm id (0-11)
            Step = 3, // subtype is EventSubtypeStep
            Collision = 4, // subtype is other game object ID
            Keyboard = 5, // subtype is key ID, values unknown
            Mouse = 6, // subtypes not really known, see game maker studio for possible values
            Other = 7, // subtype is EventSubtypeOther
            Draw = 8, // subtype is EventSubtypeDraw
            KeyPress = 9, // subtype is key ID, values unknown
            KeyRelease = 10, // subtype is key ID, values unknown, TODO: mapping is a guess
            Gesture = 11, // TODO: mapping is a guess
            Asynchronous = 12, // TODO: mapping is a guess
        }

        // TODO: mappings are guesses
        public enum EventSubtypeStep : uint
        {
            Step,
            BeginStep,
            EndStep,
        }

        public class EventCodeBlock : UndertaleObject, INotifyPropertyChanged
        {
            private uint _Unknown1;
            private uint _Unknown2;
            private uint _Unknown3;
            private uint _Unknown4;
            private uint _Unknown5;
            private uint _Unknown6;
            private uint _Unknown7;
            private UndertaleString _Unknown8;
            private UndertaleResourceById<UndertaleCode> _CodeId { get; } = new UndertaleResourceById<UndertaleCode>("CODE");
            private uint _Unknown10;
            private int _Unknown11;
            private uint _Unknown12;
            private uint _Unknown13;
            private uint _Unknown14;

            public uint Unknown1 { get => _Unknown1; set { _Unknown1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown1")); } } //1
            public uint Unknown2 { get => _Unknown2; set { _Unknown2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown2")); } } //603
            public uint Unknown3 { get => _Unknown3; set { _Unknown3 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown3")); } } //7
            public uint Unknown4 { get => _Unknown4; set { _Unknown4 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown4")); } } //0
            public uint Unknown5 { get => _Unknown5; set { _Unknown5 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown5")); } } //0
            public uint Unknown6 { get => _Unknown6; set { _Unknown6 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown6")); } } //1
            public uint Unknown7 { get => _Unknown7; set { _Unknown7 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown7")); } } //2
            public UndertaleString Unknown8 { get => _Unknown8; set { _Unknown8 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown8")); } } //""
            public UndertaleCode CodeId { get => _CodeId.Resource; set { _CodeId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CodeId")); } }
            public uint Unknown10 { get => _Unknown10; set { _Unknown10 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown10")); } } //1
            public int Unknown11 { get => _Unknown11; set { _Unknown11 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown11")); } } //-1
            public uint Unknown12 { get => _Unknown12; set { _Unknown12 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown12")); } } //0
            public uint Unknown13 { get => _Unknown13; set { _Unknown13 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown13")); } } //0
            public uint Unknown14 { get => _Unknown14; set { _Unknown14 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown14")); } } //0

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Unknown1);
                writer.Write(Unknown2);
                writer.Write(Unknown3);
                writer.Write(Unknown4);
                writer.Write(Unknown5);
                writer.Write(Unknown6);
                writer.Write(Unknown7);
                writer.WriteUndertaleString(Unknown8);
                writer.Write(_CodeId.Serialize(writer));
                writer.Write(Unknown10);
                writer.Write(Unknown11);
                writer.Write(Unknown12);
                writer.Write(Unknown13);
                writer.Write(Unknown14);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Unknown1 = reader.ReadUInt32();
                Unknown2 = reader.ReadUInt32();
                Unknown3 = reader.ReadUInt32();
                Unknown4 = reader.ReadUInt32();
                Unknown5 = reader.ReadUInt32();
                Unknown6 = reader.ReadUInt32();
                Unknown7 = reader.ReadUInt32();
                Unknown8 = reader.ReadUndertaleString();
                _CodeId.Unserialize(reader, reader.ReadInt32());
                Unknown10 = reader.ReadUInt32();
                Unknown11 = reader.ReadInt32();
                Unknown12 = reader.ReadUInt32();
                Unknown13 = reader.ReadUInt32();
                Unknown14 = reader.ReadUInt32();
            }
        }
    }
}
