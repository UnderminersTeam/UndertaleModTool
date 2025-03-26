using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleGameObject"/>.
/// </summary>
internal sealed class SerializableGameObject : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.GameObject;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => false;

    /// <summary>
    /// Data name of the sprite assigned to the object, or <see langword="null"/> if none is assigned.
    /// </summary>
    /// <remarks>
    /// See <inheritdoc cref="UndertaleGameObject.Sprite"/>.
    /// </remarks>
    public string Sprite { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Visible"/>
    public bool Visible { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Solid"/>
    public bool Solid { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Depth"/>
    public int Depth { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Persistent"/>
    public bool Persistent { get; set; }

    /// <summary>
    /// Data name of the parent object assigned to the object, or <see langword="null"/> if none is assigned.
    /// </summary>
    /// <remarks>
    /// See <see cref="UndertaleGameObject.ParentId"/>.
    /// </remarks>
    public string ParentObject { get; set; }

    /// <summary>
    /// Data name of the mask sprite assigned to the object, or <see langword="null"/> if none is assigned.
    /// </summary>
    /// <remarks>
    /// See <see cref="UndertaleGameObject.TextureMaskId"/>.
    /// </remarks>
    public string MaskSprite { get; set; }

    #region Physics related properties
    /// <inheritdoc cref="UndertaleGameObject.UsesPhysics"/>
    public bool UsesPhysics { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.IsSensor"/>
    public bool IsSensor { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.CollisionShape"/>
    [JsonConverter(typeof(JsonStringEnumConverter))] public CollisionShapeFlags CollisionShape { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Density"/>
    public float Density { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Restitution"/>
    public float Restitution { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Group"/>
    public uint Group { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.LinearDamping"/>
    public float LinearDamping { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.AngularDamping"/>
    public float AngularDamping { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Friction"/>
    public float Friction { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Awake"/>
    public bool Awake { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.Kinematic"/>
    public bool Kinematic { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.PhysicsVertices"/>
    public List<PhysicsVertex> PhysicsVertices { get; set; }

    /// <inheritdoc cref="UndertaleGameObject.UndertalePhysicsVertex"/>
    public class PhysicsVertex
    {
        /// <inheritdoc cref="UndertaleGameObject.UndertalePhysicsVertex.X"/>
        public float X { get; set; }

        /// <inheritdoc cref="UndertaleGameObject.UndertalePhysicsVertex.Y"/>
        public float Y { get; set; }
    }
    #endregion

    /// <inheritdoc cref="UndertaleGameObject.Events"/>
    public List<GameObjectEvent> Events { get; set; }

    /// <summary>
    /// Simplified representation of <see cref="UndertaleGameObject.Event"/>, with one action, tied to a single code entry.
    /// </summary>
    public class GameObjectEvent
    {
        /// <summary>
        /// Event category.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))] public EventType Category { get; set; }

        /// <summary>
        /// Event subtype, differing depending on <see cref="Category"/>.
        /// </summary>
        /// <remarks>
        /// See <see cref="UndertaleGameObject.Event.EventSubtype"/>.
        /// </remarks>
        public string Subtype { get; set; }

        /// <summary>
        /// Data name of the code entry assigned to this event, or <see langword="null"/> if none is assigned.
        /// </summary>
        /// <remarks>
        /// See <see cref="UndertaleGameObject.EventAction.CodeId"/>.
        /// </remarks>
        public string CodeEntry { get; set; }
    }

    // Data game object that was located during pre-import.
    private UndertaleGameObject _preImportAsset = null;

    /// <summary>
    /// Populates this serializable game object with data from an actual game object.
    /// </summary>
    public void PopulateFromData(ProjectContext projectContext, UndertaleGameObject obj)
    {
        // Update all main properties
        DataName = obj.Name.Content;
        Sprite = obj.Sprite?.Name?.Content;
        Visible = obj.Visible;
        Solid = obj.Solid;
        Depth = obj.Depth;
        Persistent = obj.Persistent;
        ParentObject = obj.ParentId?.Name?.Content;
        MaskSprite = obj.TextureMaskId?.Name?.Content;
        UsesPhysics = obj.UsesPhysics;
        IsSensor = obj.IsSensor;
        CollisionShape = obj.CollisionShape;
        Density = obj.Density;
        Restitution = obj.Restitution;
        Group = obj.Group;
        LinearDamping = obj.LinearDamping;
        AngularDamping = obj.AngularDamping;
        Friction = obj.Friction;
        Awake = obj.Awake;
        Kinematic = obj.Kinematic;
        PhysicsVertices = new(obj.PhysicsVertices.Count);
        foreach (var vertex in obj.PhysicsVertices)
        {
            PhysicsVertices.Add(new()
            {
                X = vertex.X,
                Y = vertex.Y
            });
        }

        // Convert events
        Events = new(8);
        int categoryIndex = 0;
        foreach (var category in obj.Events)
        {
            EventType categoryType = (EventType)categoryIndex;
            foreach (var ev in category)
            {
                if (ev.Actions.Count == 0)
                {
                    continue;
                }
                Events.Add(new GameObjectEvent()
                {
                    Category = categoryType,
                    Subtype = EventSubtypeToString(projectContext, categoryType, ev.EventSubtype),
                    CodeEntry = ev.Actions[0].CodeId?.Name?.Content
                });
            }
            categoryIndex++;
        }
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        using FileStream fs = new(destinationFile, FileMode.Create);
        JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.GameObjects.ByName(DataName) is UndertaleGameObject existing)
        {
            // Object found
            _preImportAsset = existing;
        }
        else
        {
            // No object found; create new one
            _preImportAsset = new()
            {
                Name = projectContext.Data.Strings.MakeString(DataName)
            };
            projectContext.Data.GameObjects.Add(_preImportAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleGameObject obj = _preImportAsset;

        // Update all main properties
        obj.Sprite = projectContext.FindSprite(Sprite, this);
        obj.Visible = Visible;
        obj.Solid = Solid;
        obj.Depth = Depth;
        obj.Persistent = Persistent;
        obj.ParentId = projectContext.FindGameObject(ParentObject, this);
        obj.TextureMaskId = projectContext.FindSprite(MaskSprite, this);
        obj.UsesPhysics = UsesPhysics;
        obj.IsSensor = IsSensor;
        obj.CollisionShape = CollisionShape;
        obj.Density = Density;
        obj.Restitution = Restitution;
        obj.Group = Group;
        obj.LinearDamping = LinearDamping;
        obj.AngularDamping = AngularDamping;
        obj.Friction = Friction;
        obj.Awake = Awake;
        obj.Kinematic = Kinematic;
        obj.PhysicsVertices = new(PhysicsVertices.Count);
        foreach (var vertex in PhysicsVertices)
        {
            obj.PhysicsVertices.Add(new()
            {
                X = vertex.X,
                Y = vertex.Y
            });
        }

        // Load in events
        int eventCategoryCount;
        if (obj.Events.Count >= 12)
        {
            // Use existing object's event count
            eventCategoryCount = obj.Events.Count;
        }
        else if (projectContext.Data.GameObjects.Count > 0)
        {
            // Use count of first object's events if available
            eventCategoryCount = projectContext.Data.GameObjects[0].Events.Count;
        }
        else
        {
            // Use our own event count, otherwise...
            eventCategoryCount = UndertaleGameObject.EventTypeCount;
        }
        obj.Events.Clear();
        for (int i = 0; i < eventCategoryCount; i++)
        {
            obj.Events.InternalAdd(new UndertalePointerList<UndertaleGameObject.Event>());
        }
        foreach (GameObjectEvent ev in Events)
        {
            obj.Events[(int)ev.Category].Add(new UndertaleGameObject.Event()
            {
                EventSubtype = StringToEventSubtype(projectContext, ev.Category, ev.Subtype),
                Actions = new()
                {
                    new UndertaleGameObject.EventAction()
                    {
                        LibID = 1,
                        ID = 603,
                        Kind = 7,
                        UseRelative = false,
                        IsQuestion = false,
                        UseApplyTo = true,
                        ExeType = 2,
                        ActionName = projectContext.Data.Strings.MakeString(""),
                        CodeId = projectContext.FindCode(ev.CodeEntry, this),
                        ArgumentCount = 1,
                        Who = -1,
                        Relative = false,
                        IsNot = false,
                        UnknownAlwaysZero = 0
                    }
                }
            });
        }

        return obj;
    }

    /// <summary>
    /// Converts an event subtype (for a given category) to a string representation.
    /// </summary>
    private static string EventSubtypeToString(ProjectContext projectContext, EventType category, uint subtype)
    {
        return category switch
        {
            EventType.Create => "",
            EventType.Destroy => "",
            EventType.Step => ((EventSubtypeStep)subtype).ToString(),
            EventType.Collision => projectContext.Data.GameObjects[(int)subtype]?.Name?.Content ?? subtype.ToString(),
            EventType.Keyboard => ((EventSubtypeKey)subtype).ToString(),
            EventType.Mouse => ((EventSubtypeMouse)subtype).ToString(),
            EventType.Other => ((EventSubtypeOther)subtype).ToString(),
            EventType.Draw => ((EventSubtypeDraw)subtype).ToString(),
            EventType.KeyPress => ((EventSubtypeKey)subtype).ToString(),
            EventType.KeyRelease => ((EventSubtypeKey)subtype).ToString(),
            _ => $"{subtype}"
        };
    }

    /// <summary>
    /// Converts a string representation of an event subtype (for a given category) to its integer value.
    /// </summary>
    private uint StringToEventSubtype(ProjectContext projectContext, EventType category, string subtype)
    {
        return category switch
        {
            EventType.Create => 0,
            EventType.Destroy => 0,
            EventType.Step => (uint)(EventSubtypeStep)Enum.Parse(typeof(EventSubtypeStep), subtype, true),
            EventType.Collision => (uint)projectContext.FindGameObjectIndex(subtype, this),
            EventType.Keyboard => (uint)(EventSubtypeKey)Enum.Parse(typeof(EventSubtypeKey), subtype, true),
            EventType.Mouse => (uint)(EventSubtypeMouse)Enum.Parse(typeof(EventSubtypeMouse), subtype, true),
            EventType.Other => (uint)(EventSubtypeOther)Enum.Parse(typeof(EventSubtypeOther), subtype, true),
            EventType.Draw => (uint)(EventSubtypeDraw)Enum.Parse(typeof(EventSubtypeDraw), subtype, true),
            EventType.KeyPress => (uint)(EventSubtypeKey)Enum.Parse(typeof(EventSubtypeKey), subtype, true),
            EventType.KeyRelease => (uint)(EventSubtypeKey)Enum.Parse(typeof(EventSubtypeKey), subtype, true),
            _ => uint.Parse(subtype, NumberStyles.Integer)
        };
    }
}
