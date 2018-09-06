using System;
using System.Collections.Generic;
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

    public class UndertaleGameObject : UndertaleObject
    {
        public UndertaleString Name { get; set; }
        public UndertaleResourceById<UndertaleSprite> Sprite { get; } = new UndertaleResourceById<UndertaleSprite>("SPRT");
        public bool Visible { get; set; }
        public bool Solid { get; set; }
        public int Depth { get; set; }
        public bool Persistent { get; set; }
        public UndertaleResourceById<UndertaleGameObject> ParentId { get; } = new UndertaleResourceById<UndertaleGameObject>("OBJT");
        public UndertaleResourceById<UndertaleSprite> TextureMaskId { get; } = new UndertaleResourceById<UndertaleSprite>("SPRT");
        public bool UsesPhysics { get; set; }
        public bool IsSensor { get; set; }
        public CollisionShapeFlags CollisionShape { get; set; }
        // Physics
        public float Density { get; set; }
        public float Restitution { get; set; }
        public uint Group { get; set; }
        public float LinearDamping { get; set; }
        public float AngularDamping { get; set; }
        public float Unknown1 { get; set; }
        public float Friction { get; set; }
        public uint Unknown2 { get; set; }
        public bool Kinematic { get; set; }
        // End Physics
        public UndertalePointerList<UndertalePointerList<CodeEvent>> Events { get; private set; } = new UndertalePointerList<UndertalePointerList<CodeEvent>>();

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Sprite.Serialize(writer));
            writer.Write(Visible);
            writer.Write(Solid);
            writer.Write(Depth);
            writer.Write(Persistent);
            // This apparently has a different notation than everything else...
            if (ParentId.Resource == null)
            {
                writer.Write((int)-100);
            }
            else
            {
                writer.Write(ParentId.Serialize(writer));
            }
            writer.Write(TextureMaskId.Serialize(writer));
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
            Sprite.Unserialize(reader, reader.ReadInt32());
            Visible = reader.ReadBoolean();
            Solid = reader.ReadBoolean();
            Depth = reader.ReadInt32();
            Persistent = reader.ReadBoolean();
            int parent = reader.ReadInt32();
            if (parent == -100)
            {
                ParentId.Unserialize(reader, -1);
            }
            else
            {
                Debug.Assert(parent >= 0);
                ParentId.Unserialize(reader, parent);
            }
            TextureMaskId.Unserialize(reader, reader.ReadInt32());
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

        public class CodeEvent : UndertaleObject
        {
            public uint EventSubtype { get; set; } // the ID at the end of name, subtype for some events, 0 if unused
            public UndertalePointerList<EventCodeBlock> CodeBlock { get; private set; } = new UndertalePointerList<EventCodeBlock>(); // seems to always have 1 entry, maybe the games using drag-and-drop code are different // TODO: this is actually an index into FunctionDefinitions

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

        public class EventCodeBlock : UndertaleObject
        {
            public uint Unknown1 { get; set; } //1
            public uint Unknown2 { get; set; } //603
            public uint Unknown3 { get; set; } //7
            public uint Unknown4 { get; set; } //0
            public uint Unknown5 { get; set; } //0
            public uint Unknown6 { get; set; } //1
            public uint Unknown7 { get; set; } //2
            public UndertaleString Unknown8 { get; set; } //""
            public UndertaleResourceById<UndertaleCode> CodeId { get; } = new UndertaleResourceById<UndertaleCode>("CODE");
            public uint Unknown10 { get; set; } //1
            public int Unknown11 { get; set; } //-1
            public uint Unknown12 { get; set; } //0
            public uint Unknown13 { get; set; } //0
            public uint Unknown14 { get; set; } //0

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
                writer.Write(CodeId.Serialize(writer));
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
                CodeId.Unserialize(reader, reader.ReadInt32());
                Unknown10 = reader.ReadUInt32();
                Unknown11 = reader.ReadInt32();
                Unknown12 = reader.ReadUInt32();
                Unknown13 = reader.ReadUInt32();
                Unknown14 = reader.ReadUInt32();
            }
        }
    }
}
