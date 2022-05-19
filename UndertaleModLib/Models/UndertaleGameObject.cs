using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UndertaleModLib.Models;

//TODO: shouldn't this be inside of the UGameObject class?
/// <summary>
/// Collision shapes an <see cref="UndertaleGameObject"/> can use.
/// </summary>
public enum CollisionShapeFlags : uint
{
    /// <summary>
    /// A circular collision shape.
    /// </summary>
    Circle = 0,
    /// <summary>
    /// A rectangular collision shape.
    /// </summary>
    Box = 1,
    /// <summary>
    /// A custom polygonal collision shape.
    /// </summary>
    Custom = 2,
}

/// <summary>
/// A game object in a data file.
/// </summary>
public class UndertaleGameObject : UndertaleNamedResource, INotifyPropertyChanged, IDisposable
{
    public UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _sprite = new();
    public UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _parentId = new();
    public UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _textureMaskId = new();

    /// <summary>
    /// The name of the game object.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The sprite this game object uses.
    /// </summary>
    public UndertaleSprite Sprite { get => _sprite.Resource; set { _sprite.Resource = value; OnPropertyChanged(); } }

    /// <summary>
    /// Whether the game object is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether the game object is solid.
    /// </summary>
    public bool Solid { get; set; }

    /// <summary>
    /// The depth level of the game object.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Whether the game object is persistent.
    /// </summary>
    public bool Persistent { get; set; }

    /// <summary>
    /// The parent game object this is inheriting from.
    /// </summary>
    public UndertaleGameObject ParentId { get => _parentId.Resource; set { _parentId.Resource = value; OnPropertyChanged(); } }

    /// <summary>
    /// The texture mask this game object is using.
    /// </summary>
    public UndertaleSprite TextureMaskId { get => _textureMaskId.Resource; set { _textureMaskId.Resource = value; OnPropertyChanged(); } }

    #region Physics related properties
    /// <summary>
    /// Whether this object uses Game Maker physics.
    /// </summary>
    public bool UsesPhysics { get; set; }

    /// <summary>
    /// Whether this game object should act as a sensor fixture.
    /// </summary>
    public bool IsSensor { get; set; }

    /// <summary>
    /// The collision shape the game object should use.
    /// </summary>
    public CollisionShapeFlags CollisionShape { get; set; } = CollisionShapeFlags.Circle;

    /// <summary>
    /// The physics density of the game object.
    /// </summary>
    public float Density { get; set; } = 0.5f;

    /// <summary>
    /// The physics restitution of the game object.
    /// </summary>
    public float Restitution { get; set; } = 0.1f;

    /// <summary>
    /// The physics collision group this game object belongs to.
    /// </summary>
    public uint Group { get; set; }

    /// <summary>
    /// The physics linear damping this game object uses.
    /// </summary>
    public float LinearDamping { get; set; } = 0.1f;

    /// <summary>
    /// The physics angular damping this game object uses.
    /// </summary>
    public float AngularDamping { get; set; } = 0.1f;

    /// <summary>
    /// The physics friction this game object uses.
    /// </summary>
    public float Friction { get; set; } = 0.2f;

    /// <summary>
    /// Whether this game object should start awake in the physics simulation.
    /// </summary>
    public bool Awake { get; set; }

    /// <summary>
    /// Whether this game object is kinematic.
    /// </summary>
    public bool Kinematic { get; set; }

    /// <summary>
    /// The vertices used for a <see cref="CollisionShape"/> of type <see cref="CollisionShapeFlags.Custom"/>.
    /// </summary>
    public List<UndertalePhysicsVertex> PhysicsVertices { get; private set; } = new List<UndertalePhysicsVertex>();

    #endregion

    /// <summary>
    /// All the events that this game object has.
    /// </summary>
    public UndertalePointerList<UndertalePointerList<Event>> Events { get; private set; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Initialized an instance of <see cref="UndertaleGameObject"/>.
    /// </summary>
    public UndertaleGameObject()
    {
        for (int i = 0; i < Enum.GetValues(typeof(EventType)).Length; i++)
            Events.Add(new UndertalePointerList<Event>());
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.WriteUndertaleObject(_sprite);
        writer.Write(Visible);
        writer.Write(Solid);
        writer.Write(Depth);
        writer.Write(Persistent);
        // This apparently has a different notation than everything else...
        if (_parentId.Resource == null)
        {
            writer.Write(-100);
        }
        else
        {
            writer.WriteUndertaleObject(_parentId);
        }
        writer.WriteUndertaleObject(_textureMaskId);
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

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        _sprite = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
        Visible = reader.ReadBoolean();
        Solid = reader.ReadBoolean();
        Depth = reader.ReadInt32();
        Persistent = reader.ReadBoolean();
        _parentId = new UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>();
        int parent = reader.ReadInt32();
        if (parent == -100)
        {
            _parentId.UnserializeById(reader, -1);
        }
        else
        {
            if (parent < 0 && parent != -1) // Technically can be -100 (undefined), -2 (other), or -1 (self). Other makes no sense here though
                throw new Exception("Invalid value for parent - should be -100 or object id, got " + parent);
            _parentId.UnserializeById(reader, parent);
        }
        _textureMaskId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
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

    #region EventHandlerFor() overloads
    //TODO: what do all these eventhandlers do? can't find any references right now.

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
            code = new UndertaleCode()
            {
                Name = name,
                LocalsCount = 1
            };
            action.CodeId = code;
            codelist.Add(code);

            UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
            argsLocal.Name = strg.MakeString("arguments");
            argsLocal.Index = 0;

            var locals = new UndertaleCodeLocals()
            {
                Name = name
            };
            locals.Locals.Add(argsLocal);
            localslist.Add(locals);
        }
        return code;
    }

    public UndertaleCode EventHandlerFor(EventType type, UndertaleData data)
    {
        return EventHandlerFor(type, data.Strings, data.Code, data.CodeLocals);
    }

    public UndertaleCode EventHandlerFor(EventType type, uint subtype, UndertaleData data)
    {
        return EventHandlerFor(type, subtype, data.Strings, data.Code, data.CodeLocals);
    }

    public UndertaleCode EventHandlerFor(EventType type, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
    {
        return EventHandlerFor(type, 0u, strg, codelist, localslist);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeKey subtype, UndertaleData data)
    {
        return EventHandlerFor(type, subtype, data.Strings, data.Code, data.CodeLocals);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeKey subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
    {
        if (type != EventType.Keyboard && type != EventType.KeyPress && type != EventType.KeyRelease)
            throw new InvalidOperationException();
        return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeStep subtype, UndertaleData data)
    {
        return EventHandlerFor(type, subtype, data.Strings, data.Code, data.CodeLocals);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeStep subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
    {
        if (type != EventType.Step)
            throw new InvalidOperationException();
        return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeMouse subtype, UndertaleData data)
    {
        return EventHandlerFor(type, subtype, data.Strings, data.Code, data.CodeLocals);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeMouse subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
    {
        if (type != EventType.Mouse)
            throw new InvalidOperationException();
        return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeOther subtype, UndertaleData data)
    {
        return EventHandlerFor(type, subtype, data.Strings, data.Code, data.CodeLocals);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeOther subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
    {
        if (type != EventType.Other)
            throw new InvalidOperationException();
        return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeDraw subtype, UndertaleData data)
    {
        return EventHandlerFor(type, subtype, data.Strings, data.Code, data.CodeLocals);
    }

    public UndertaleCode EventHandlerFor(EventType type, EventSubtypeDraw subtype, IList<UndertaleString> strg, IList<UndertaleCode> codelist, IList<UndertaleCodeLocals> localslist)
    {
        if (type != EventType.Draw)
            throw new InvalidOperationException();
        return EventHandlerFor(type, (uint)subtype, strg, codelist, localslist);
    }
    #endregion

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _sprite.Dispose();
        _parentId.Dispose();
        _textureMaskId.Dispose();
        PhysicsVertices = new();
        foreach (var ev in Events)
            foreach (var subEv in ev)
                subEv?.Dispose();
        Name = null;
        Events = new();
    }

    /// <summary>
    /// Generic events that an <see cref="UndertaleGameObject"/> uses.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class Event : UndertaleObject, IDisposable
    {
        /// <summary>
        /// The subtype of this event.
        /// </summary>
        /// <remarks>Game Maker suffixes the action names with this id.</remarks>
        public uint EventSubtype { get; set; }

        /// <summary>
        /// The available actions that will be performed for this event.
        /// </summary>
        /// <remarks>This seems to always have 1 entry, it would need testing if maybe the games using drag-and-drop code are different</remarks>
        public UndertalePointerList<EventAction> Actions { get; private set; } = new UndertalePointerList<EventAction>();

        //TODO: not used, condense. Also UMT specific.
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

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(EventSubtype);
            writer.WriteUndertaleObject(Actions);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            EventSubtype = reader.ReadUInt32();
            Actions = reader.ReadUndertaleObject<UndertalePointerList<EventAction>>();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (EventAction action in Actions)
                action?.Dispose();
            Actions = new();
        }
    }

    /// <summary>
    /// An action in an event.
    /// </summary>
    public class EventAction : UndertaleObject, INotifyPropertyChanged, IDisposable
    {
        // All the unknown values seem to be provided for compatibility only - in older versions of GM:S they stored the drag and drop blocks,
        // but newer versions compile them down to GML bytecode anyway
        // Possible meaning of values: https://github.com/WarlockD/GMdsam/blob/26aefe3e90a7a7a1891cb83f468079546f32b4b7/GMdsam/GameMaker/ChunkTypes.cs#L466

        // Note from the future: these aren't always these values...

        public uint LibID { get; set; } // always 1
        public uint ID { get; set; } // always 603
        public uint Kind { get; set; } // always 7
        public bool UseRelative { get; set; } // always 0
        public bool IsQuestion { get; set; } // always 0
        public bool UseApplyTo { get; set; } // always 1
        public uint ExeType { get; set; } // always 2
        public UndertaleString ActionName { get; set; } // always ""
        private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _codeId = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();

        /// <summary>
        /// The code entry that gets executed.
        /// </summary>
        public UndertaleCode CodeId { get => _codeId.Resource; set { _codeId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CodeId))); } }
        public uint ArgumentCount { get; set; } // always 1
        public int Who { get; set; } // always -1
        public bool Relative { get; set; } // always 0
        public bool IsNot { get; set; } // always 0
        public uint UnknownAlwaysZero { get; set; } // always 0

        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
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
            writer.WriteUndertaleObject(_codeId);
            writer.Write(ArgumentCount);
            writer.Write(Who);
            writer.Write(Relative);
            writer.Write(IsNot);
            writer.Write(UnknownAlwaysZero);
        }

        /// <inheritdoc />
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
            _codeId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
            ArgumentCount = reader.ReadUInt32();
            Who = reader.ReadInt32();
            Relative = reader.ReadBoolean();
            IsNot = reader.ReadBoolean();
            UnknownAlwaysZero = reader.ReadUInt32();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _codeId.Dispose();
            ActionName = null;
        }
    }

    /// <summary>
    /// Class representing a physics vertex used for a <see cref="CollisionShape"/> of type <see cref="CollisionShapeFlags.Custom"/>.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertalePhysicsVertex : UndertaleObject
    {
        /// <summary>
        /// The x position of the vertex.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// The y position of the vertex.
        /// </summary>
        public float Y { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
        }
    }
}

/// <summary>
/// The types an <see cref="UndertaleGameObject.Event"/> from an <see cref="UndertaleGameObject"/> can be.
/// </summary>
/// <remarks>Note, that subtypes exist as well.</remarks>
public enum EventType : uint
{
    /// <summary>
    /// A creation event type. Has no subtypes, it's always 0
    /// </summary>
    Create = 0,
    /// <summary>
    /// A destroy event type. Has no subtypes, it's always 0.
    /// </summary>
    Destroy = 1,
    /// <summary>
    /// An alarm event type. The subtype is 0-11, depending on the alarm id.
    /// </summary>
    Alarm = 2,
    /// <summary>
    /// A step event type. The subtype is <see cref="EventSubtypeStep"/>.
    /// </summary>
    Step = 3, // subtype is EventSubtypeStep
    /// <summary>
    /// A collision event type. The subtype is the other <see cref="UndertaleGameObject"/>'s id.
    /// </summary>
    Collision = 4,
    /// <summary>
    /// A key down event type. The subtype is the key id, see <see cref="EventSubtypeKey"/>.
    /// </summary>
    Keyboard = 5,
    /// <summary>
    /// A mouse event type. The subtype is <see cref="EventSubtypeMouse"/>.
    /// </summary>
    Mouse = 6, // subtype is EventSubtypeMouse
    /// <summary>
    /// A miscellaneous event type. The subtype is <see cref="EventSubtypeOther"/>.
    /// </summary>
    Other = 7,
    /// <summary>
    /// A draw event type. The subtype is <see cref="EventSubtypeDraw"/>.
    /// </summary>
    Draw = 8,
    /// <summary>
    /// A key pressed event type. The subtype is the key id, see <see cref="EventSubtypeKey"/>.
    /// </summary>
    KeyPress = 9,
    /// <summary>
    /// A key released event type. The subtype is the key id, see <see cref="EventSubtypeKey"/>.
    /// </summary>
    KeyRelease = 10,
    /// <summary>
    /// A trigger event type. Only used in Pre- Game Maker: Studio.
    /// </summary>
    Trigger = 11, // no subtypes, always 0
    /// <summary>
    /// A cleanup event type. Has no subtypes, always 0.
    /// </summary>
    CleanUp = 12,
    /// <summary>
    /// A gesture event type. The subtype is <see cref="EventSubtypeGesture"/>.
    /// </summary>
    Gesture = 13,
    /// <summary>
    /// A pre-create event type. Unknown subtype. TODO?
    /// </summary>
    PreCreate = 14
}

/// <summary>
/// The subtypes for <see cref="EventType.Step"/>.
/// </summary>
public enum EventSubtypeStep : uint
{
    /// <summary>
    /// Normal step event.
    /// </summary>
    Step = 0,
    /// <summary>
    /// The begin step event.
    /// </summary>
    BeginStep = 1,
    /// <summary>
    /// The end step event.
    /// </summary>
    EndStep = 2,
}

/// <summary>
/// The subtypes for <see cref="EventType.Draw"/>.
/// </summary>
public enum EventSubtypeDraw : uint
{
    /// <summary>
    /// The draw event.
    /// </summary>
    Draw = 0,
    /// <summary>
    /// The draw GUI event.
    /// </summary>
    DrawGUI = 64,
    /// <summary>
    /// The resize event.
    /// </summary>
    Resize = 65,
    /// <summary>
    /// The draw begin event.
    /// </summary>
    DrawBegin = 72,
    /// <summary>
    /// The draw end event.
    /// </summary>
    DrawEnd = 73,
    /// <summary>
    /// The draw GUI begin event.
    /// </summary>
    DrawGUIBegin = 74,
    /// <summary>
    /// The draw GUI end event.
    /// </summary>
    DrawGUIEnd = 75,
    /// <summary>
    /// The pre-draw event.
    /// </summary>
    PreDraw = 76,
    /// <summary>
    /// The post-draw event.
    /// </summary>
    PostDraw = 77,
}

/// <summary>
/// The subtypes for <see cref="EventType.Keyboard"/>, <see cref="EventType.KeyPress"/> and <see cref="EventType.KeyRelease"/>.
/// </summary>
public enum EventSubtypeKey : uint
{
    // if doesn't match any of the below, then it's probably just chr(value)

    /// <summary>
    /// Keycode representing no key.
    /// </summary>
    vk_nokey = 0,
    /// <summary>
    /// Keycode representing that any key.
    /// </summary>
    vk_anykey = 1,
    /// <summary>
    /// Keycode representing Backspace.
    /// </summary>
    vk_backspace = 8,
    /// <summary>
    /// Keycode representing Tab.
    /// </summary>
    vk_tab = 9,
    /// <summary>
    /// Keycode representing Return.
    /// </summary>
    vk_return = 13,
    /// <summary>
    /// Keycode representing Enter.
    /// </summary>
    vk_enter = 13,
    /// <summary>
    /// Keycode representing any Shift key.
    /// </summary>
    vk_shift = 16,
    /// <summary>
    /// Keycode representing any Control key.
    /// </summary>
    vk_control = 17,
    /// <summary>
    /// Keycode representing any Alt key.
    /// </summary>
    vk_alt = 18,
    /// <summary>
    /// Keycode representing the Pause key.
    /// </summary>
    vk_pause = 19,
    /// <summary>
    /// Keycode representing the Escape key.
    /// </summary>
    vk_escape = 27,
    /// <summary>
    /// Keycode representing the Space key.
    /// </summary>
    vk_space = 32,
    /// <summary>
    /// Keycode representing PageUp.
    /// </summary>
    vk_pageup = 33,
    /// <summary>
    /// Keycode representing PageDown.
    /// </summary>
    vk_pagedown = 34,
    /// <summary>
    /// Keycode representing the End key.
    /// </summary>
    vk_end = 35,
    /// <summary>
    /// Keycode representing the Home key.
    /// </summary>
    vk_home = 36,
    /// <summary>
    /// Keycode representing the left arrow key.
    /// </summary>
    vk_left = 37,
    /// <summary>
    /// Keycode representing the up arrow key.
    /// </summary>
    vk_up = 38,
    /// <summary>
    /// Keycode representing the right arrow key.
    /// </summary>
    vk_right = 39,
    /// <summary>
    /// Keycode representing the down arrow key.
    /// </summary>
    vk_down = 40,
    /// <summary>
    /// Keycode representing the PrintScreen key.
    /// </summary>
    vk_printscreen = 44,
    /// <summary>
    /// Keycode representing the Insert key.
    /// </summary>
    vk_insert = 45,
    /// <summary>
    /// Keycode representing the Delete key.
    /// </summary>
    vk_delete = 46,
    /// <summary>
    /// Keycode representing the 0 key.
    /// </summary>
    Digit0 = 48,
    /// <summary>
    /// Keycode representing the 1 key.
    /// </summary>
    Digit1 = 49,
    /// <summary>
    /// Keycode representing the 2 key.
    /// </summary>
    Digit2 = 50,
    /// <summary>
    /// Keycode representing the 3 key.
    /// </summary>
    Digit3 = 51,
    /// <summary>
    /// Keycode representing the 4 key.
    /// </summary>
    Digit4 = 52,
    /// <summary>
    /// Keycode representing the 5 key.
    /// </summary>
    Digit5 = 53,
    /// <summary>
    /// Keycode representing the 6 key.
    /// </summary>
    Digit6 = 54,
    /// <summary>
    /// Keycode representing the 7 key.
    /// </summary>
    Digit7 = 55,
    /// <summary>
    /// Keycode representing the 8 key.
    /// </summary>
    Digit8 = 56,
    /// <summary>
    /// Keycode representing the 9 key.
    /// </summary>
    Digit9 = 57,
    /// <summary>
    /// Keycode representing the A key.
    /// </summary>
    A = 65,
    /// <summary>
    /// Keycode representing the B key.
    /// </summary>
    B = 66,
    /// <summary>
    /// Keycode representing the C key.
    /// </summary>
    C = 67,
    /// <summary>
    /// Keycode representing the D key.
    /// </summary>
    D = 68,
    /// <summary>
    /// Keycode representing the E key.
    /// </summary>
    E = 69,
    /// <summary>
    /// Keycode representing the F key.
    /// </summary>
    F = 70,
    /// <summary>
    /// Keycode representing the G key.
    /// </summary>
    G = 71,
    /// <summary>
    /// Keycode representing the H key.
    /// </summary>
    H = 72,
    /// <summary>
    /// Keycode representing the I key.
    /// </summary>
    I = 73,
    /// <summary>
    /// Keycode representing the J key.
    /// </summary>
    J = 74,
    /// <summary>
    /// Keycode representing the K key.
    /// </summary>
    K = 75,
    /// <summary>
    /// Keycode representing the L key.
    /// </summary>
    L = 76,
    /// <summary>
    /// Keycode representing the M key.
    /// </summary>
    M = 77,
    /// <summary>
    /// Keycode representing the N key.
    /// </summary>
    N = 78,
    /// <summary>
    /// Keycode representing the O key.
    /// </summary>
    O = 79,
    /// <summary>
    /// Keycode representing the P key.
    /// </summary>
    P = 80,
    /// <summary>
    /// Keycode representing the Q key.
    /// </summary>
    Q = 81,
    /// <summary>
    /// Keycode representing the R key.
    /// </summary>
    R = 82,
    /// <summary>
    /// Keycode representing the S key.
    /// </summary>
    S = 83,
    /// <summary>
    /// Keycode representing the T key.
    /// </summary>
    T = 84,
    /// <summary>
    /// Keycode representing the U key.
    /// </summary>
    U = 85,
    /// <summary>
    /// Keycode representing the V key.
    /// </summary>
    V = 86,
    /// <summary>
    /// Keycode representing the W key.
    /// </summary>
    W = 87,
    /// <summary>
    /// Keycode representing the X key.
    /// </summary>
    X = 88,
    /// <summary>
    /// Keycode representing the Y key.
    /// </summary>
    Y = 89,
    /// <summary>
    /// Keycode representing the Z key.
    /// </summary>
    Z = 90,
    /// <summary>
    /// Keycode representing the 0 key on the numeric keypad.
    /// </summary>
    vk_numpad0 = 96,
    /// <summary>
    /// Keycode representing the 1 key on the numeric keypad.
    /// </summary>
    vk_numpad1 = 97,
    /// <summary>
    /// Keycode representing the 2 key on the numeric keypad.
    /// </summary>
    vk_numpad2 = 98,
    /// <summary>
    /// Keycode representing the 3 key on the numeric keypad.
    /// </summary>
    vk_numpad3 = 99,
    /// <summary>
    /// Keycode representing the 4 key on the numeric keypad.
    /// </summary>
    vk_numpad4 = 100,
    /// <summary>
    /// Keycode representing the 5 key on the numeric keypad.
    /// </summary>
    vk_numpad5 = 101,
    /// <summary>
    /// Keycode representing the 6 key on the numeric keypad.
    /// </summary>
    vk_numpad6 = 102,
    /// <summary>
    /// Keycode representing the 7 key on the numeric keypad.
    /// </summary>
    vk_numpad7 = 103,
    /// <summary>
    /// Keycode representing the 8 key on the numeric keypad.
    /// </summary>
    vk_numpad8 = 104,
    /// <summary>
    /// Keycode representing the 9 key on the numeric keypad.
    /// </summary>
    vk_numpad9 = 105,
    /// <summary>
    /// Keycode representing the Multiply key on the numeric keypad.
    /// </summary>
    vk_multiply = 106,
    /// <summary>
    /// Keycode representing the Add key on the numeric keypad.
    /// </summary>
    vk_add = 107,
    /// <summary>
    /// Keycode representing the Subtract key on the numeric keypad.
    /// </summary>
    vk_subtract = 109,
    /// <summary>
    /// Keycode representing the Decimal Dot key on the numeric keypad.
    /// </summary>
    vk_decimal = 110,
    /// <summary>
    /// Keycode representing the Divide key on the numeric keypad.
    /// </summary>
    vk_divide = 111,
    /// <summary>
    /// Keycode representing the F1 key.
    /// </summary>
    vk_f1 = 112,
    /// <summary>
    /// Keycode representing the F2 key.
    /// </summary>
    vk_f2 = 113,
    /// <summary>
    /// Keycode representing the F3 key.
    /// </summary>
    vk_f3 = 114,
    /// <summary>
    /// Keycode representing the F4 key.
    /// </summary>
    vk_f4 = 115,
    /// <summary>
    /// Keycode representing the F5 key.
    /// </summary>
    vk_f5 = 116,
    /// <summary>
    /// Keycode representing the F6 key.
    /// </summary>
    vk_f6 = 117,
    /// <summary>
    /// Keycode representing the F7 key.
    /// </summary>
    vk_f7 = 118,
    /// <summary>
    /// Keycode representing the F8 key.
    /// </summary>
    vk_f8 = 119,
    /// <summary>
    /// Keycode representing the F9 key.
    /// </summary>
    vk_f9 = 120,
    /// <summary>
    /// Keycode representing the F10 key.
    /// </summary>
    vk_f10 = 121,
    /// <summary>
    /// Keycode representing the F11 key.
    /// </summary>
    vk_f11 = 122,
    /// <summary>
    /// Keycode representing the F12 key.
    /// </summary>
    vk_f12 = 123,
    /// <summary>
    /// Keycode representing the left Shift key.
    /// </summary>
    vk_lshift = 160,
    /// <summary>
    /// Keycode representing the right Shift key.
    /// </summary>
    vk_rshift = 161,
    /// <summary>
    /// Keycode representing the left Control key.
    /// </summary>
    vk_lcontrol = 162,
    /// <summary>
    /// Keycode representing the right Control key.
    /// </summary>
    vk_rcontrol = 163,
    /// <summary>
    /// Keycode representing the left Alt key.
    /// </summary>
    vk_lalt = 164,
    /// <summary>
    /// Keycode representing the right Alt key.
    /// </summary>
    vk_ralt = 165,
}

/// <summary>
/// The subtypes for <see cref="EventType.Mouse"/>.
/// </summary>
public enum EventSubtypeMouse : uint
{
    /// <summary>
    /// The left-mouse button down event.
    /// </summary>
    LeftButton = 0,
    /// <summary>
    /// The right-mouse button down event.
    /// </summary>
    RightButton = 1,
    /// <summary>
    /// The middle-mouse button down event.
    /// </summary>
    MiddleButton = 2,
    /// <summary>
    /// The no-mouse input event.
    /// </summary>
    NoButton = 3,
    /// <summary>
    /// The left-mouse button pressed event.
    /// </summary>
    LeftPressed = 4,
    /// <summary>
    /// The right-mouse button pressed event.
    /// </summary>
    RightPressed = 5,
    /// <summary>
    /// The middle-mouse button pressed event.
    /// </summary>
    MiddlePressed = 6,
    /// <summary>
    /// The left-mouse button released event.
    /// </summary>
    LeftReleased = 7,
    /// <summary>
    /// The right-mouse button released event.
    /// </summary>
    RightReleased = 8,
    /// <summary>
    /// The middle-mouse button released event.
    /// </summary>
    MiddleReleased = 9,
    /// <summary>
    /// The mouse enter event.
    /// </summary>
    MouseEnter = 10,
    /// <summary>
    /// The mouse leave event.
    /// </summary>
    MouseLeave = 11,
    /// <summary>
    /// The Joystick1 left event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Left = 16,
    /// <summary>
    /// The Joystick1 right event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Right = 17,
    /// <summary>
    /// The Joystick1 up event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Up = 18,
    /// <summary>
    /// The Joystick1 down event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Down = 19,
    /// <summary>
    /// The Joystick1 button1 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button1 = 21,
    /// <summary>
    /// The Joystick1 button2 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button2 = 22,
    /// <summary>
    /// The Joystick1 button3 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button3 = 23,
    /// <summary>
    /// The Joystick1 button4 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button4 = 24,
    /// <summary>
    /// The Joystick1 button5 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button5 = 25,
    /// <summary>
    /// The Joystick1 button6 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button6 = 26,
    /// <summary>
    /// The Joystick1 button7 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button7 = 27,
    /// <summary>
    /// The Joystick1 button8 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick1Button8 = 28,
    /// <summary>
    /// The Joystick2 left event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Left = 31,
    /// <summary>
    /// The Joystick2 right event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Right = 32,
    /// <summary>
    /// The Joystick2 up event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Up = 33,
    /// <summary>
    /// The Joystick2 down event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Down = 34,
    /// <summary>
    /// The Joystick2 button1 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button1 = 36,
    /// <summary>
    /// The Joystick2 button2 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button2 = 37,
    /// <summary>
    /// The Joystick2 button3 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button3 = 38,
    /// <summary>
    /// The Joystick2 button4 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button4 = 39,
    /// <summary>
    /// The Joystick2 button5 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button5 = 40,
    /// <summary>
    /// The Joystick2 button6 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button6 = 41,
    /// <summary>
    /// The Joystick2 button7 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button7 = 42,
    /// <summary>
    /// The Joystick2 button8 event. Is only used in Pre-Game Maker: Studio.
    /// </summary>
    Joystick2Button8 = 43,
    /// <summary>
    /// The global left-mouse button down event.
    /// </summary>
    GlobLeftButton = 50,
    /// <summary>
    /// The global right-mouse button down event.
    /// </summary>
    GlobRightButton = 51,
    /// <summary>
    /// The global middle-mouse button down event.
    /// </summary>
    GlobMiddleButton = 52,
    /// <summary>
    /// The global left-mouse button pressed event.
    /// </summary>
    GlobLeftPressed = 53,
    /// <summary>
    /// The global right-mouse button pressed event.
    /// </summary>
    GlobRightPressed = 54,
    /// <summary>
    /// The global middle-mouse button pressed event.
    /// </summary>
    GlobMiddlePressed = 55,
    /// <summary>
    /// The global left-mouse button released event.
    /// </summary>
    GlobLeftReleased = 56,
    /// <summary>
    /// The global right-mouse button released event.
    /// </summary>
    GlobRightReleased = 57,
    /// <summary>
    /// The global middle-mouse button released event.
    /// </summary>
    GlobMiddleReleased = 58,
    /// <summary>
    /// The mouse-wheel up event.
    /// </summary>
    MouseWheelUp = 60,
    /// <summary>
    /// The mouse-wheel down event.
    /// </summary>
    MouseWheelDown = 61,
}

/// <summary>
/// The subtypes for <see cref="EventType.Other"/>.
/// </summary>
public enum EventSubtypeOther : uint
{
    /// <summary>
    /// The outside room event.
    /// </summary>
    OutsideRoom = 0,
    /// <summary>
    /// The intersect boundary event.
    /// </summary>
    IntersectBoundary = 1,
    /// <summary>
    /// The game start event.
    /// </summary>
    GameStart = 2,
    /// <summary>
    /// The game end event.
    /// </summary>
    GameEnd = 3,
    /// <summary>
    /// The room start event.
    /// </summary>
    RoomStart = 4,
    /// <summary>
    /// The room end event.
    /// </summary>
    RoomEnd = 5,
    /// <summary>
    /// The "No More Lives" event. Only used in Game Maker Studio: 1 and earlier.
    /// </summary>
    NoMoreLives = 6,
    /// <summary>
    /// The animation end event.
    /// </summary>
    AnimationEnd = 7,
    /// <summary>
    /// The path ended event.
    /// </summary>
    EndOfPath = 8,
    /// <summary>
    /// The "No More Health" event. Only used in Game Maker Studio: 1 and earlier.
    /// </summary>
    NoMoreHealth = 9,
    #region User events
    /// <summary>
    /// The User 0 event.
    /// </summary>
    User0 = 10,
    /// <summary>
    /// The User 1 event.
    /// </summary>
    User1 = 11,
    /// <summary>
    /// The User 2 event.
    /// </summary>
    User2 = 12,
    /// <summary>
    /// The User 3 event.
    /// </summary>
    User3 = 13,
    /// <summary>
    /// The User 4 event.
    /// </summary>
    User4 = 14,
    /// <summary>
    /// The User 5 event.
    /// </summary>
    User5 = 15,
    /// <summary>
    /// The User 6 event.
    /// </summary>
    User6 = 16,
    /// <summary>
    /// The User 7 event.
    /// </summary>
    User7 = 17,
    /// <summary>
    /// The User 8 event.
    /// </summary>
    User8 = 18,
    /// <summary>
    /// The User 9 event.
    /// </summary>
    User9 = 19,
    /// <summary>
    /// The User 10 event.
    /// </summary>
    User10 = 20,
    /// <summary>
    /// The User 11 event.
    /// </summary>
    User11 = 21,
    /// <summary>
    /// The User 12 event.
    /// </summary>
    User12 = 22,
    /// <summary>
    /// The User 13 event.
    /// </summary>
    User13 = 23,
    /// <summary>
    /// The User 14 event.
    /// </summary>
    User14 = 24,
    /// <summary>
    /// The User 15 event.
    /// </summary>
    User15 = 25,
    /// <summary>
    /// The User 16 event.
    /// </summary>
    User16 = 26,
    #endregion
    #region View events
    /// <summary>
    /// The Outside View 0 event.
    /// </summary>
    OutsideView0 = 40,
    /// <summary>
    /// The Outside View 1 event.
    /// </summary>
    OutsideView1 = 41,
    /// <summary>
    /// The Outside View 2 event.
    /// </summary>
    OutsideView2 = 42,
    /// <summary>
    /// The Outside View 3 event.
    /// </summary>
    OutsideView3 = 43,
    /// <summary>
    /// The Outside View 4 event.
    /// </summary>
    OutsideView4 = 44,
    /// <summary>
    /// The Outside View 5 event.
    /// </summary>
    OutsideView5 = 45,
    /// <summary>
    /// The Outside View 6 event.
    /// </summary>
    OutsideView6 = 46,
    /// <summary>
    /// The Outside View 7 event.
    /// </summary>
    OutsideView7 = 47,
    /// <summary>
    /// The Intersect View 0 Boundary event.
    /// </summary>
    BoundaryView0 = 50,
    /// <summary>
    /// The Intersect View 1 Boundary event.
    /// </summary>
    BoundaryView1 = 51,
    /// <summary>
    /// The Intersect View 2 Boundary event.
    /// </summary>
    BoundaryView2 = 52,
    /// <summary>
    /// The Intersect View 3 Boundary event.
    /// </summary>
    BoundaryView3 = 53,
    /// <summary>
    /// The Intersect View 4 Boundary event.
    /// </summary>
    BoundaryView4 = 54,
    /// <summary>
    /// The Intersect View 5 Boundary event.
    /// </summary>
    BoundaryView5 = 55,
    /// <summary>
    /// The Intersect View 6 Boundary event.
    /// </summary>
    BoundaryView6 = 56,
    /// <summary>
    /// The Intersect View 7 Boundary event.
    /// </summary>
    BoundaryView7 = 57,
    #endregion
    /// <summary>
    /// The animation Update event for Skeletal Animation functions.
    /// </summary>
    AnimationUpdate = 58,
    /// <summary>
    /// The animation event for Skeletal Animation functions.
    /// </summary>
    AnimationEvent = 59,
    #region Async events
    /// <summary>
    /// The async image loaded event.
    /// </summary>
    AsyncImageLoaded = 60,
    /// <summary>
    /// The async sound loaded event.
    /// </summary>
    AsyncSoundLoaded = 61,
    /// <summary>
    /// The async http event.
    /// </summary>
    AsyncHTTP = 62,
    /// <summary>
    /// The async dialog event.
    /// </summary>
    AsyncDialog = 63,
    /// <summary>
    /// The async in-app purchase event.
    /// </summary>
    AsyncIAP = 66,
    /// <summary>
    /// The async cloud event.
    /// </summary>
    AsyncCloud = 67,
    /// <summary>
    /// The async networking event.
    /// </summary>
    AsyncNetworking = 68,
    /// <summary>
    /// The async Steam event.
    /// </summary>
    AsyncSteam = 69,
    /// <summary>
    /// The async social event.
    /// </summary>
    AsyncSocial = 70,
    /// <summary>
    /// The async push notification event.
    /// </summary>
    AsyncPushNotification = 71,
    /// <summary>
    /// The async save/load event.
    /// </summary>
    AsyncSaveAndLoad = 72,
    /// <summary>
    /// The async audio recording event.
    /// </summary>
    AsyncAudioRecording = 73,
    /// <summary>
    /// The async audio playback event.
    /// </summary>
    AsyncAudioPlayback = 74,
    /// <summary>
    /// The async system event.
    /// </summary>
    AsyncSystem = 75,
    #endregion
}

/// <summary>
/// The subtypes for <see cref="EventType.Gesture"/>.
/// </summary>
public enum EventSubtypeGesture : uint
{
    /// <summary>
    /// The tap event.
    /// </summary>
    Tap = 0,
    /// <summary>
    /// The double tap event.
    /// </summary>
    DoubleTap = 1,
    /// <summary>
    /// The drag start event.
    /// </summary>
    DragStart = 2,
    /// <summary>
    /// The dragging event.
    /// </summary>
    DragMove = 3,
    /// <summary>
    /// The drag end event.
    /// </summary>
    DragEnd = 4,
    /// <summary>
    /// The flick event.
    /// </summary>
    Flick = 5,
    /// <summary>
    /// The pinch start event.
    /// </summary>
    PinchStart = 6,
    /// <summary>
    /// The pinch in event.
    /// </summary>
    PinchIn = 7,
    /// <summary>
    /// The pinch out event.
    /// </summary>
    PinchOut = 8,
    /// <summary>
    /// The pinch end event.
    /// </summary>
    PinchEnd = 9,
    /// <summary>
    /// The rotate start event.
    /// </summary>
    RotateStart = 10,
    /// <summary>
    /// The rotating event.
    /// </summary>
    Rotating = 11,
    /// <summary>
    /// The rotate end event.
    /// </summary>
    RotateEnd = 12,
    /// <summary>
    /// The global tap event.
    /// </summary>
    GlobalTap = 64,
    /// <summary>
    /// The global double tap event.
    /// </summary>
    GlobalDoubleTap = 65,
    /// <summary>
    /// The global drag start event.
    /// </summary>
    GlobalDragStart = 66,
    /// <summary>
    /// The global dragging event.
    /// </summary>
    GlobalDragMove = 67,
    /// <summary>
    /// The global drag end event.
    /// </summary>
    GlobalDragEnd = 68,
    /// <summary>
    /// The global flick event.
    /// </summary>
    GlobalFlick = 69,
    /// <summary>
    /// The global pinch start event.
    /// </summary>
    GlobalPinchStart = 70,
    /// <summary>
    /// The global pinch in event.
    /// </summary>
    GlobalPinchIn = 71,
    /// <summary>
    /// The global pinch out event.
    /// </summary>
    GlobalPinchOut = 72,
    /// <summary>
    /// The global pinch end event.
    /// </summary>
    GlobalPinchEnd = 73,
    /// <summary>
    /// The global rotate start event.
    /// </summary>
    GlobalRotateStart = 74,
    /// <summary>
    /// The global rotating event.
    /// </summary>
    GlobalRotating = 75,
    /// <summary>
    /// The global rotate end event.
    /// </summary>
    GlobalRotateEnd = 76,
}