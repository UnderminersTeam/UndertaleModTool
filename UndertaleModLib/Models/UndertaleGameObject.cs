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
        public UndertalePointerList<UndertalePointerList<Event>> Events { get; private set; } = new UndertalePointerList<UndertalePointerList<Event>>();

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
            writer.Write(Unknown1); // possible meaning: https://github.com/WarlockD/GMdsam/blob/26aefe3e90a7a7a1891cb83f468079546f32b4b7/GMdsam/GameMaker/ChunkTypes.cs#L553
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
            Events = reader.ReadUndertaleObject<UndertalePointerList<UndertalePointerList<Event>>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public class Event : UndertaleObject, INotifyPropertyChanged
        {
            private uint _EventSubtype;

            public uint EventSubtype { get => _EventSubtype; set { _EventSubtype = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EventSubtype")); } } // (the same as the ID at the end of name)
            public UndertalePointerList<EventAction> Actions { get; private set; } = new UndertalePointerList<EventAction>(); // seems to always have 1 entry, maybe the games using drag-and-drop code are different

            public EventSubtypeKey EventSubtypeKey
            {
                get => (EventSubtypeKey)EventSubtype;
                set => EventSubtype = (uint)value;
            }

            public EventSubtypeStep EventSubtypeStep
            {
                get => (EventSubtypeStep)EventSubtype;
                set => EventSubtype = (uint)value;
            }

            public EventSubtypeOther EventSubtypeOther
            {
                get => (EventSubtypeOther)EventSubtype;
                set => EventSubtype = (uint)value;
            }

            public EventSubtypeDraw EventSubtypeDraw
            {
                get => (EventSubtypeDraw)EventSubtype;
                set => EventSubtype = (uint)value;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(EventSubtype);
                writer.WriteUndertaleObject(Actions);
            }

            public void Unserialize(UndertaleReader reader)
            {
                EventSubtype = reader.ReadUInt32();
                Actions = reader.ReadUndertaleObject<UndertalePointerList<EventAction>>();
            }
        }

        public enum EventType : uint
        {
            Create = 0, // no subtypes, always 0
            Destroy = 1, // no subtypes, always 0
            Alarm = 2, // subtype is alarm id (0-11)
            Step = 3, // subtype is EventSubtypeStep
            Collision = 4, // subtype is other game object ID
            Keyboard = 5, // subtype is key ID, see EventSubtypeKey
            Mouse = 6, // TODO: subtypes (see game maker studio for possible values)
            Other = 7, // subtype is EventSubtypeOther
            Draw = 8, // subtype is EventSubtypeDraw
            KeyPress = 9, // subtype is key ID, see EventSubtypeKey
            KeyRelease = 10, // subtype is key ID, values unknown
            Gesture = 11, // TODO: mapping is a guess // TODO: subtypes
            Asynchronous = 12, // TODO: mapping is a guess // TODO: subtypes
        }
        
        public enum EventSubtypeStep : uint
        {
            Step = 0,
            BeginStep = 1,
            EndStep = 2,
        }

        public enum EventSubtypeDraw : uint
        {
            Draw = 0,
            DrawGUI = 64,
            Resize = 65,
            DrawBegin = 72,
            DrawEnd = 73,
            DrawGUIBegin = 74,
            DrawGUIEnd = 75,
            PreDraw = 76,
            PostDraw = 77,
        }

        public enum EventSubtypeKey : uint
        {
            // if doesn't match any of the below, then it's probably just chr(value)
            vk_nokey = 0,
            vk_anykey = 1,

            vk_left = 37,
            vk_right = 39,
            vk_up = 38,
            vk_down = 40,
            vk_enter = 13,
            vk_return = 13,
            vk_escape = 27,
            vk_space = 32,
            vk_shift = 16,
            vk_control = 17,
            vk_alt = 18,
            vk_backspace = 8,
            vk_tab = 9,
            vk_home = 36,
            vk_end = 35,
            vk_delete = 46,
            vk_insert = 45,
            vk_pageup = 33,
            vk_pagedown = 34,
            vk_pause = 19,
            vk_printscreen = 44,
            vk_f1 = 112,
            vk_f2 = 113,
            vk_f3 = 114,
            vk_f4 = 115,
            vk_f5 = 116,
            vk_f6 = 117,
            vk_f7 = 118,
            vk_f8 = 119,
            vk_f9 = 120,
            vk_f10 = 121,
            vk_f11 = 122,
            vk_f12 = 123,
            vk_numpad0 = 96,
            vk_numpad1 = 97,
            vk_numpad2 = 98,
            vk_numpad3 = 99,
            vk_numpad4 = 100,
            vk_numpad5 = 101,
            vk_numpad6 = 102,
            vk_numpad7 = 103,
            vk_numpad8 = 104,
            vk_numpad9 = 105,
            vk_multiply = 106,
            vk_divide = 111,
            vk_add = 107,
            vk_subtract = 109,
            vk_decimal = 110,

            vk_lshift = 160,
            vk_lcontrol = 162,
            vk_lalt = 164,
            vk_rshift = 161,
            vk_rcontrol = 163,
            vk_ralt = 165,

            Digit0 = 48,
            Digit1 = 49,
            Digit2 = 50,
            Digit3 = 51,
            Digit4 = 52,
            Digit5 = 53,
            Digit6 = 54,
            Digit7 = 55,
            Digit8 = 56,
            Digit9 = 57,
            A = 65,
            B = 66,
            C = 67,
            D = 68,
            E = 69,
            F = 70,
            G = 71,
            H = 72,
            I = 73,
            J = 74,
            K = 75,
            L = 76,
            M = 77,
            N = 78,
            O = 79,
            P = 80,
            Q = 81,
            R = 82,
            S = 83,
            T = 84,
            U = 85,
            V = 86,
            W = 87,
            X = 88,
            Y = 89,
            Z = 90,
        }

        public enum EventSubtypeOther : uint
        {
            OutsideRoom = 0,
            IntersectBoundary = 1,
            OutsideView0 = 41,
            OutsideView1 = 42,
            OutsideView2 = 43,
            OutsideView3 = 44,
            OutsideView4 = 45,
            OutsideView5 = 45,
            OutsideView6 = 46,
            OutsideView7 = 47,
            BoundaryView0 = 51,
            BoundaryView1 = 52,
            BoundaryView2 = 53,
            BoundaryView3 = 54,
            BoundaryView4 = 55,
            BoundaryView5 = 55,
            BoundaryView6 = 56,
            BoundaryView7 = 57,
            GameStart = 2,
            GameEnd = 3,
            RoomStart = 4,
            RoomEnd = 5,
            NoMoreLives = 6,
            NoMoreHealth = 9,
            AnimationEnd = 7,
            AnimationUpdate = 58,
            AnimationEvent = 59,
            EndOfPath = 8,
            User0 = 10,
            User1 = 11,
            User2 = 12,
            User3 = 13,
            User4 = 14,
            User5 = 15,
            User6 = 16,
            User7 = 17,
            User8 = 18,
            User9 = 19,
            User10 = 20,
            User11 = 21,
            User12 = 22,
            User13 = 23,
            User14 = 24,
            User15 = 25,
        }

        public class EventAction : UndertaleObject, INotifyPropertyChanged
        {
            // All the unknown values seem to be provided for compatibility only - in older versions of GM:S they stored the drag and drop blocks,
            // but newer versions compile them down to GML bytecode anyway
            // Possible meaning of values: https://github.com/WarlockD/GMdsam/blob/26aefe3e90a7a7a1891cb83f468079546f32b4b7/GMdsam/GameMaker/ChunkTypes.cs#L466

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

            public uint Unknown1 { get => _Unknown1; set { _Unknown1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown1")); } } // always 1
            public uint Unknown2 { get => _Unknown2; set { _Unknown2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown2")); } } // always 603
            public uint Unknown3 { get => _Unknown3; set { _Unknown3 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown3")); } } // always 7
            public uint Unknown4 { get => _Unknown4; set { _Unknown4 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown4")); } } // always 0
            public uint Unknown5 { get => _Unknown5; set { _Unknown5 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown5")); } } // always 0
            public uint Unknown6 { get => _Unknown6; set { _Unknown6 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown6")); } } // always 1
            public uint Unknown7 { get => _Unknown7; set { _Unknown7 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown7")); } } // always 2
            public UndertaleString Unknown8 { get => _Unknown8; set { _Unknown8 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown8")); } } // always ""
            public UndertaleCode CodeId { get => _CodeId.Resource; set { _CodeId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CodeId")); } }
            public uint Unknown10 { get => _Unknown10; set { _Unknown10 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown10")); } } // always 1
            public int Unknown11 { get => _Unknown11; set { _Unknown11 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown11")); } } // always -1
            public uint Unknown12 { get => _Unknown12; set { _Unknown12 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown12")); } } // always 0
            public uint Unknown13 { get => _Unknown13; set { _Unknown13 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown13")); } } // always 0
            public uint Unknown14 { get => _Unknown14; set { _Unknown14 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown14")); } } // always 0

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
