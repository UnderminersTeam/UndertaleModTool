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
        private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _Sprite = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>();
        private bool _Visible = true;
        private bool _Solid = false;
        private int _Depth = 0;
        private bool _Persistent = false;
        private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _ParentId = new UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>();
        private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _TextureMaskId = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(); // TODO: ?
        private bool _UsesPhysics = false;
        private bool _IsSensor = false;
        private CollisionShapeFlags _CollisionShape = CollisionShapeFlags.Circle;
        private float _Density = 0.5f;
        private float _Restitution = 0.1f;
        private uint _Group = 0;
        private float _LinearDamping = 0.1f;
        private float _AngularDamping = 0.1f;
        private float _Friction = 0.2f;
        private bool _Awake = false;
        private bool _Kinematic = false;

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
        public float Friction { get => _Friction; set { _Friction = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Friction")); } }
        public bool Awake { get => _Awake; set { _Awake = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Awake")); } }
        public bool Kinematic { get => _Kinematic; set { _Kinematic = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kinematic")); } }
        public List<UndertalePhysicsVertex> PhysicsVertices { get; private set; } = new List<UndertalePhysicsVertex>();
        public UndertalePointerList<UndertalePointerList<Event>> Events { get; private set; } = new UndertalePointerList<UndertalePointerList<Event>>();

        public event PropertyChangedEventHandler PropertyChanged;

        public UndertaleGameObject()
        {
            for (int i = 0; i < Enum.GetValues(typeof(EventType)).Length; i++)
                Events.Add(new UndertalePointerList<Event>());
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleObject(_Sprite);
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
                writer.WriteUndertaleObject(_ParentId);
            }
            writer.WriteUndertaleObject(_TextureMaskId);
            writer.Write(UsesPhysics);
            writer.Write(IsSensor);
            writer.Write((uint)CollisionShape);
            writer.Write(Density);
            writer.Write(Restitution);
            writer.Write(Group);
            writer.Write(LinearDamping);
            writer.Write(AngularDamping);
            writer.Write(PhysicsVertices.Count); // possible (now confirmed) meaning: https://github.com/WarlockD/GMdsam/blob/26aefe3e90a7a7a1891cb83f468079546f32b4b7/GMdsam/GameMaker/ChunkTypes.cs#L553
            writer.Write(Friction);
            writer.Write(Awake);
            writer.Write(Kinematic);
            // Need to write these manually because the count is unfortunately separated
            foreach (UndertalePhysicsVertex v in PhysicsVertices)
            {
                v.Serialize(writer);
            }
            writer.WriteUndertaleObject(Events);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            _Sprite = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
            Visible = reader.ReadBoolean();
            Solid = reader.ReadBoolean();
            Depth = reader.ReadInt32();
            Persistent = reader.ReadBoolean();
            _ParentId = new UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>();
            int parent = reader.ReadInt32();
            if (parent == -100)
            {
                _ParentId.UnserializeById(reader, -1);
            }
            else
            {
                if (parent < 0 && parent != -1) // Technically can be -100 (undefined), -2 (other), or -1 (self). Other makes no sense here though
                    throw new Exception("Invalid value for parent - should be -100 or object id, got " + parent);
                _ParentId.UnserializeById(reader, parent);
            }
            _TextureMaskId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
            UsesPhysics = reader.ReadBoolean();
            IsSensor = reader.ReadBoolean();
            CollisionShape = (CollisionShapeFlags)reader.ReadUInt32();
            Density = reader.ReadSingle();
            Restitution = reader.ReadSingle();
            Group = reader.ReadUInt32();
            LinearDamping = reader.ReadSingle();
            AngularDamping = reader.ReadSingle();
            int physicsShapeVertexCount = reader.ReadInt32();
            Friction = reader.ReadSingle();
            Awake = reader.ReadBoolean();
            Kinematic = reader.ReadBoolean();
            // Needs to be done manually because count is separated
            for (int i = 0; i < physicsShapeVertexCount; i++)
            {
                UndertalePhysicsVertex v = new UndertalePhysicsVertex();
                v.Unserialize(reader);
                PhysicsVertices.Add(v);
            }
            Events = reader.ReadUndertaleObject<UndertalePointerList<UndertalePointerList<Event>>>();
        }

        public UndertaleCode EventHandlerFor(EventType type, uint subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
        {
            Event subtypeObj = Events[(int)type].Where((x) => x.EventSubtype == subtype).FirstOrDefault();
            if (subtypeObj == null)
                Events[(int)type].Add(subtypeObj = new Event() { EventSubtype = subtype });
            EventAction action = subtypeObj.Actions.FirstOrDefault();
            if (action == null)
            {
                subtypeObj.Actions.Add(action = new EventAction());
                action.ActionName = strg.MakeString("");
            }
            UndertaleCode code = action.CodeId;
            if (code == null)
            {
                var name = strg.MakeString("gml_Object_" + Name.Content + "_" + type.ToString() + "_" + subtype);
                code = action.CodeId = new UndertaleCode();
                code.Name = name;
                codelist.Add(code);
                localslist.Add(new UndertaleCodeLocals()
                {
                    Name = name
                });
            }
            return code;
        }

        public UndertaleCode EventHandlerFor(EventType type, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
        {
            return EventHandlerFor(type, 0u, strg, codelist, localslist);
        }

        public UndertaleCode EventHandlerFor(EventType type, EventSubtypeKey subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
        {
            if (type != EventType.Keyboard && type != EventType.KeyPress && type != EventType.KeyRelease)
                throw new InvalidOperationException();
            return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
        }

        public UndertaleCode EventHandlerFor(EventType type, EventSubtypeStep subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
        {
            if (type != EventType.Step)
                throw new InvalidOperationException();
            return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
        }

        public UndertaleCode EventHandlerFor(EventType type, EventSubtypeMouse subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
        {
            if (type != EventType.Mouse)
                throw new InvalidOperationException();
            return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
        }
		
		public UndertaleCode EventHandlerFor(EventType type, EventSubtypeOther subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
        {
            if (type != EventType.Other)
                throw new InvalidOperationException();
            return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
        }
        
        public UndertaleCode EventHandlerFor(EventType type, EventSubtypeDraw subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
        {
            if (type != EventType.Draw)
                throw new InvalidOperationException();
            return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
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

            public EventSubtypeMouse EventSubtypeMouse
            {
                get => (EventSubtypeMouse)EventSubtype;
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

            public EventSubtypeGesture EventSubtypeGesture
            {
                get => (EventSubtypeGesture)EventSubtype;
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

        public class EventAction : UndertaleObject, INotifyPropertyChanged
        {
            // All the unknown values seem to be provided for compatibility only - in older versions of GM:S they stored the drag and drop blocks,
            // but newer versions compile them down to GML bytecode anyway
            // Possible meaning of values: https://github.com/WarlockD/GMdsam/blob/26aefe3e90a7a7a1891cb83f468079546f32b4b7/GMdsam/GameMaker/ChunkTypes.cs#L466

            private uint _LibID;
            private uint _ID;
            private uint _Kind;
            private bool _UseRelative;
            private bool _IsQuestion;
            private bool _UseApplyTo;
            private uint _ExeType;
            private UndertaleString _ActionName;
            private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _CodeId = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
            private uint _ArgumentCount;
            private int _Who;
            private bool _Relative;
            private bool _IsNot;
            private uint _UnknownAlwaysZero;

            public uint LibID { get => _LibID; set { _LibID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LibID")); } } // always 1
            public uint ID { get => _ID; set { _ID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ID")); } } // always 603
            public uint Kind { get => _Kind; set { _Kind = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kind")); } } // always 7
            public bool UseRelative { get => _UseRelative; set { _UseRelative = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UseRelative")); } } // always 0
            public bool IsQuestion { get => _IsQuestion; set { _IsQuestion = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsQuestion")); } } // always 0
            public bool UseApplyTo { get => _UseApplyTo; set { _UseApplyTo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UseApplyTo")); } } // always 1
            public uint ExeType { get => _ExeType; set { _ExeType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ExeType")); } } // always 2
            public UndertaleString ActionName { get => _ActionName; set { _ActionName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ActionName")); } } // always ""
            public UndertaleCode CodeId { get => _CodeId.Resource; set { _CodeId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CodeId")); } }
            public uint ArgumentCount { get => _ArgumentCount; set { _ArgumentCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ArgumentCount")); } } // always 1
            public int Who { get => _Who; set { _Who = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Who")); } } // always -1
            public bool Relative { get => _Relative; set { _Relative = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Relative")); } } // always 0
            public bool IsNot { get => _IsNot; set { _IsNot = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsNot")); } } // always 0
            public uint UnknownAlwaysZero { get => _UnknownAlwaysZero; set { _UnknownAlwaysZero = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UnknownAlwaysZero")); } } // always 0

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(LibID);
                writer.Write(ID);
                writer.Write(Kind);
                writer.Write(UseRelative);
                writer.Write(IsQuestion);
                writer.Write(UseApplyTo);
                writer.Write(ExeType);
                writer.WriteUndertaleString(ActionName);
                writer.WriteUndertaleObject(_CodeId);
                writer.Write(ArgumentCount);
                writer.Write(Who);
                writer.Write(Relative);
                writer.Write(IsNot);
                writer.Write(UnknownAlwaysZero);
            }

            public void Unserialize(UndertaleReader reader)
            {
                LibID = reader.ReadUInt32();
                ID = reader.ReadUInt32();
                Kind = reader.ReadUInt32();
                UseRelative = reader.ReadBoolean();
                IsQuestion = reader.ReadBoolean();
                UseApplyTo = reader.ReadBoolean();
                ExeType = reader.ReadUInt32();
                ActionName = reader.ReadUndertaleString();
                _CodeId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
                ArgumentCount = reader.ReadUInt32();
                Who = reader.ReadInt32();
                Relative = reader.ReadBoolean();
                IsNot = reader.ReadBoolean();
                UnknownAlwaysZero = reader.ReadUInt32();
            }
        }

        public class UndertalePhysicsVertex : UndertaleObject, INotifyPropertyChanged
        {
            private float _X;
            private float _Y;

            public float X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public float Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
            }

            public void Unserialize(UndertaleReader reader)
            {
                X = reader.ReadSingle();
                Y = reader.ReadSingle();
            }
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
        Mouse = 6, // subtype is EventSubtypeMouse
        Other = 7, // subtype is EventSubtypeOther
        Draw = 8, // subtype is EventSubtypeDraw
        KeyPress = 9, // subtype is key ID, see EventSubtypeKey
        KeyRelease = 10, // subtype is key ID, see EventSubtypeKey
        Trigger = 11, // no subtypes, always 0
        CleanUp = 12, // no subtypes, always 0
        Gesture = 13, // subtype is EventSubtypeGesture
        PreCreate = 14
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
        vk_backspace = 8,
        vk_tab = 9,
        vk_return = 13,
        vk_enter = 13,
        vk_shift = 16,
        vk_control = 17,
        vk_alt = 18,
        vk_pause = 19,
        vk_escape = 27,
        vk_space = 32,
        vk_pageup = 33,
        vk_pagedown = 34,
        vk_end = 35,
        vk_home = 36,
        vk_left = 37,
        vk_up = 38,
        vk_right = 39,
        vk_down = 40,
        vk_printscreen = 44,
        vk_insert = 45,
        vk_delete = 46,
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
        vk_add = 107,
        vk_subtract = 109,
        vk_decimal = 110,
        vk_divide = 111,
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
        vk_lshift = 160,
        vk_rshift = 161,
        vk_lcontrol = 162,
        vk_rcontrol = 163,
        vk_lalt = 164,
        vk_ralt = 165,
    }
	
    public enum EventSubtypeMouse : uint
    {
        LeftButton = 0,
        RightButton = 1,
        MiddleButton = 2,
        NoButton = 3,
        LeftPressed = 4,
        RightPressed = 5,
        MiddlePressed = 6,
        LeftReleased = 7,
        RightReleased = 8,
        MiddleReleased = 9,
        MouseEnter = 10,
        MouseLeave = 11,
        Joystick1Left = 16,
        Joystick1Right = 17,
        Joystick1Up = 18,
        Joystick1Down = 19,
        Joystick1Button1 = 21,
        Joystick1Button2 = 22,
        Joystick1Button3 = 23,
        Joystick1Button4 = 24,
        Joystick1Button5 = 25,
        Joystick1Button6 = 26,
        Joystick1Button7 = 27,
        Joystick1Button8 = 28,
        Joystick2Left = 31,
        Joystick2Right = 32,
        Joystick2Up = 33,
        Joystick2Down = 34,
        Joystick2Button1 = 36,
        Joystick2Button2 = 37,
        Joystick2Button3 = 38,
        Joystick2Button4 = 39,
        Joystick2Button5 = 40,
        Joystick2Button6 = 41,
        Joystick2Button7 = 42,
        Joystick2Button8 = 43,
        GlobLeftButton = 50,
        GlobRightButton = 51,
        GlobMiddleButton = 52,
        GlobLeftPressed = 53,
        GlobRightPressed = 54,
        GlobMiddlePressed = 55,
        GlobLeftReleased = 56,
        GlobRightReleased = 57,
        GlobMiddleReleased = 58,
        MouseWheelUp = 60,
        MouseWheelDown = 61,
    }
	
    public enum EventSubtypeOther : uint
    {
        OutsideRoom = 0,
        IntersectBoundary = 1,
        GameStart = 2,
        GameEnd = 3,
        RoomStart = 4,
        RoomEnd = 5,
        NoMoreLives = 6,
        AnimationEnd = 7,
        EndOfPath = 8,
        NoMoreHealth = 9,
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
        User16 = 26,
        OutsideView0 = 40,
        OutsideView1 = 41,
        OutsideView2 = 42,
        OutsideView3 = 43,
        OutsideView4 = 44,
        OutsideView5 = 45,
        OutsideView6 = 46,
        OutsideView7 = 47,
        BoundaryView0 = 50,
        BoundaryView1 = 51,
        BoundaryView2 = 52,
        BoundaryView3 = 53,
        BoundaryView4 = 54,
        BoundaryView5 = 55,
        BoundaryView6 = 56,
        BoundaryView7 = 57,
        AnimationUpdate = 58,
        AnimationEvent = 59,
        AsyncImageLoaded = 60,
        AsyncSoundLoaded = 61,
        AsyncHTTP = 62,
        AsyncDialog = 63,
        AsyncIAP = 66,
        AsyncCloud = 67,
        AsyncNetworking = 68,
        AsyncSteam = 69,
        AsyncSocial = 70,
        AsyncPushNotification = 71,
        AsyncSaveAndLoad = 72,
        AsyncAudioRecording = 73,
        AsyncAudioPlayback = 74,
        AsyncSystem = 75,
    }

    public enum EventSubtypeGesture : uint
    {
        Tap = 0,
        DoubleTap = 1,
        DragStart = 2,
        DragMove = 3,
        DragEnd = 4,
        Flick = 5,
        PinchStart = 6,
        PinchIn = 7,
        PinchOut = 8,
        PinchEnd = 9,
        RotateStart = 10,
        Rotating = 11,
        RotateEnd = 12,
        GlobalTap = 64,
        GlobalDoubleTap = 65,
        GlobalDragStart = 66,
        GlobalDragMove = 67,
        GlobalDragEnd = 68,
        GlobalFlick = 69,
        GlobalPInchStart = 70,
        GlobalPInchIn = 71,
        GlobalPInchOut = 72,
        GlobalPInchEnd = 73,
        GlobalRotateStart = 74,
        GlobalRotating = 75,
    }
}
