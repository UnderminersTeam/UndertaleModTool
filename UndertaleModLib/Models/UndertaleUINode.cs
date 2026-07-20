using System;
using static UndertaleModLib.Models.IUndertaleFlexProperties;

namespace UndertaleModLib.Models;

/// <summary>
/// Interface for all UI layer node data.
/// </summary>
public interface IUndertaleUINodeData : UndertaleObject
{
}

/// <summary>
/// Structure containing a UI node.
/// </summary>
/// <remarks>
/// This abstraction is required to work with UI node polymorphism correctly.
/// </remarks>
public class UndertaleUINode : UndertaleObject
{
    /// <summary>
    /// Inner UI node for the node structure.
    /// </summary>
    public IUndertaleUINodeData Node { get; set; }

    /// <inheritdoc/>
    public void Serialize(UndertaleWriter writer)
    {
        // Write type discriminator
        writer.Write(Node switch
        {
            UndertaleUILayer => 0,
            UndertaleUIFlexPanel => 1,
            UndertaleUIGameObject => 3,
            UndertaleUISequenceInstance => 4,
            UndertaleUISpriteInstance => 5,
            UndertaleUITextItemInstance => 6,
            UndertaleUIEffectLayer => 7,
            _ => throw new Exception($"Unknown UI node type {Node.GetType().Name}")
        });

        // Write pointer to node data
        writer.WriteUndertaleObjectPointer(Node);

        // Write list of children, if required
        if (Node is UndertaleUIContainer container)
        {
            container.Children.Serialize(writer);
        }
        else
        {
            writer.Write(0);
        }

        // Write the rest of the node data
        writer.WriteUndertaleObject(Node);
    }

    /// <inheritdoc/>
    public void Unserialize(UndertaleReader reader)
    {
        // Handle type discriminator
        int typeId = reader.ReadInt32();
        Node = typeId switch
        {
            0 => new UndertaleUILayer(),
            1 => new UndertaleUIFlexPanel(),
            3 => new UndertaleUIGameObject(),
            4 => new UndertaleUISequenceInstance(),
            5 => new UndertaleUISpriteInstance(),
            6 => new UndertaleUITextItemInstance(),
            7 => new UndertaleUIEffectLayer(),
            _ => throw new Exception($"Unknown node type ID {typeId}")
        };

        // Read in pointer to node data
        uint dataPtr = reader.ReadUInt32();

        // Read list of children, if required
        if (Node is UndertaleUIContainer container)
        {
            container.Children.Unserialize(reader);
        }
        else
        {
            int alwaysZero = reader.ReadInt32();
            if (alwaysZero != 0)
            {
                throw new Exception("Expected 0 while reading UI node child count");
            }
        }

        // Unserialize the rest of the node data
        if (dataPtr != reader.AbsPosition)
        {
            reader.SubmitWarning($"Reading misaligned while reading UI node at {reader.AbsPosition:X8}, realigning to {dataPtr:X8}");
            reader.AbsPosition = dataPtr;
        }
        Node.Unserialize(reader);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        // Use type discriminator
        int typeId = reader.ReadInt32();
        reader.ReadInt32(); // skip data pointer

        if (typeId == 0 || typeId == 1)
        {
            // Add children objects to count
            count += UndertalePointerList<UndertaleUINode>.UnserializeChildObjectCount(reader);
        }
        else
        {
            // Skip past empty list
            reader.ReadInt32();
        }

        // Process each type
        count += typeId switch
        {
            0 => UndertaleUILayer.UnserializeChildObjectCount(reader),
            1 => UndertaleUIFlexPanel.UnserializeChildObjectCount(reader),
            3 => UndertaleUIGameObject.UnserializeChildObjectCount(reader),
            4 => UndertaleUISequenceInstance.UnserializeChildObjectCount(reader),
            5 => UndertaleUISpriteInstance.UnserializeChildObjectCount(reader),
            6 => UndertaleUITextItemInstance.UnserializeChildObjectCount(reader),
            7 => UndertaleUIEffectLayer.UnserializeChildObjectCount(reader),
            _ => throw new Exception($"Unknown node type ID {typeId}")
        };

        return count;
    }
}

/// <summary>
/// Root structure containing a root-level UI node.
/// </summary>
public class UndertaleUIRootNode : UndertaleUINode, UndertaleNamedResource
{
    /// <inheritdoc/>
    public UndertaleString Name
    {
        get => Node is UndertaleNamedResource named ? named.Name : null;
        set
        {
            if (Node is UndertaleNamedResource named)
            {
                named.Name = value;
            }
        }
    }
}

/// <summary>
/// Represents a value used for UI flex nodes; a float corresponding with its unit.
/// </summary>
public readonly struct UndertaleFlexValue
{
    /// <summary>
    /// Units to be used for flex values.
    /// </summary>
    public enum UnitKind : int
    {
        Undefined = 0,
        Point = 1,
        Percent = 2,
        Auto = 3
    }

    /// <summary>
    /// Floating point value.
    /// </summary>
    public float Value { get; init; }

    /// <summary>
    /// Unit for the floating point value.
    /// </summary>
    public UnitKind Unit { get; init; }

    /// <summary>
    /// Initializes a flex value.
    /// </summary>
    public UndertaleFlexValue(float value, UnitKind unit)
    {
        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Reads a flex value from the given reader.
    /// </summary>
    /// <param name="reader"></param>
    public UndertaleFlexValue(UndertaleReader reader)
    {
        Value = reader.ReadSingle();
        Unit = (UnitKind)reader.ReadInt32();
    }

    /// <summary>
    /// Serializes a flex value to the given writer.
    /// </summary>
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Value);
        writer.Write((int)Unit);
    }
}

/// <summary>
/// Interface for UI layout nodes that have flex properties.
/// </summary>
public interface IUndertaleFlexProperties
{
    /// <summary>
    /// Alignment types that can be used for aligning content in a flexbox.
    /// </summary>
    public enum AlignmentKind : int
    {
        Auto = 0,
        FlexStart = 1,
        Center = 2,
        FlexEnd = 3,
        Stretch = 4,
        Baseline = 5,
        SpaceBetween = 6,
        SpaceAround = 7,
        SpaceEvenly = 8
    }

    /// <summary>
    /// Flex direction types that can be used in a flexbox.
    /// </summary>
    public enum FlexDirectionKind : int
    {
        Column = 0,
        ColumnReverse = 1,
        Row = 2,
        RowReverse = 3
    }

    /// <summary>
    /// Wrap types that can be used in a flexbox.
    /// </summary>
    public enum WrapKind : int
    {
        NoWrap = 0,
        Wrap = 1,
        WrapReverse = 2
    }

    /// <summary>
    /// Justification types that can be used in a flexbox.
    /// </summary>
    public enum JustifyKind : int
    {
        FlexStart = 0,
        Center = 1,
        FlexEnd = 2,
        SpaceBetween = 3,
        SpaceAround = 4,
        SpaceEvenly = 5
    }
    
    /// <summary>
    /// Layout direction types that can be used in a flexbox.
    /// </summary>
    public enum LayoutDirectionKind : int
    {
        Inherit = 0,
        LTR = 1,
        RTL = 2
    }

    /// <summary>
    /// Alignment of child items within the container (along cross axis).
    /// </summary>
    public AlignmentKind AlignItems { get; set; }

    /// <summary>
    /// Main direction that child items are laid out (affects direction of other properties).
    /// </summary>
    public FlexDirectionKind FlexDirection { get; set; }

    /// <summary>
    /// Wrapping type for the flexbox (when items exceed the bounds).
    /// </summary>
    public WrapKind FlexWrap { get; set; }

    /// <summary>
    /// Alignment of lines within the container (along cross axis), when items are wrapped to multiple lines.
    /// </summary>
    public AlignmentKind AlignContent { get; set; }

    /// <summary>
    /// Gap between rows of items.
    /// </summary>
    public float GapRow { get; set; }

    /// <summary>
    /// Gap between columns of items.
    /// </summary>
    public float GapColumn { get; set; }

    /// <summary>
    /// Left padding of the container.
    /// </summary>
    public UndertaleFlexValue PaddingLeft { get; set; }

    /// <summary>
    /// Right padding of the container.
    /// </summary>
    public UndertaleFlexValue PaddingRight { get; set; }

    /// <summary>
    /// Top padding of the container.
    /// </summary>
    public UndertaleFlexValue PaddingTop { get; set; }

    /// <summary>
    /// Bottom padding of the container.
    /// </summary>
    public UndertaleFlexValue PaddingBottom { get; set; }

    /// <summary>
    /// Alignment of child items within the container (along main axis, i.e. main flex direction).
    /// </summary>
    public JustifyKind JustifyContent { get; set; }

    /// <summary>
    /// Layout direction of the container.
    /// </summary>
    public LayoutDirectionKind LayoutDirection { get; set; }

    /// <summary>
    /// Serializes flex properties to the given writer.
    /// </summary>
    public static void Serialize(UndertaleWriter writer, IUndertaleFlexProperties props)
    {
        writer.Write((int)props.AlignItems);
        writer.Write((int)props.FlexDirection);
        writer.Write((int)props.FlexWrap);
        writer.Write((int)props.AlignContent);
        writer.Write(props.GapRow);
        writer.Write(props.GapColumn);
        props.PaddingLeft.Serialize(writer);
        props.PaddingRight.Serialize(writer);
        props.PaddingTop.Serialize(writer);
        props.PaddingBottom.Serialize(writer);
        writer.Write((int)props.JustifyContent);
        writer.Write((int)props.LayoutDirection);
    }

    /// <summary>
    /// Unserializes flex properties from the given reader.
    /// </summary>
    public static void Unserialize(UndertaleReader reader, IUndertaleFlexProperties props)
    {
        props.AlignItems = (AlignmentKind)reader.ReadInt32();
        props.FlexDirection = (FlexDirectionKind)reader.ReadInt32();
        props.FlexWrap = (WrapKind)reader.ReadInt32();
        props.AlignContent = (AlignmentKind)reader.ReadInt32();
        props.GapRow = reader.ReadSingle();
        props.GapColumn = reader.ReadSingle();
        props.PaddingLeft = new(reader);
        props.PaddingRight = new(reader);
        props.PaddingTop = new(reader);
        props.PaddingBottom = new(reader);
        props.JustifyContent = (JustifyKind)reader.ReadInt32();
        props.LayoutDirection = (LayoutDirectionKind)reader.ReadInt32();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 16 * 4;
        return 0;
    }
}

/// <summary>
/// Interface for UI node instances that have flex properties.
/// </summary>
public interface IUndertaleFlexInstanceProperties
{
    /// <summary>
    /// Whether the instance is visible.
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Anchor of the instance's (0, 0) position, based on container.
    /// </summary>
    public int Anchor { get; set; }

    /// <summary>
    /// Whether the width of the instance is stretched to fit its container.
    /// </summary>
    public bool StretchWidth { get; set; }

    /// <summary>
    /// Whether the height of the instance is stretched to fit its container.
    /// </summary>
    public bool StretchHeight { get; set; }

    /// <summary>
    /// Whether the instance is tiled horizontally.
    /// </summary>
    public bool TileH { get; set; }

    /// <summary>
    /// Whether the instance is tiled vertically.
    /// </summary>
    public bool TileV { get; set; }

    /// <summary>
    /// Whether aspect ratio should be maintained when stretching width/height.
    /// </summary>
    public bool KeepAspect { get; set; }

    /// <summary>
    /// Serializes flex instance properties to the given writer.
    /// </summary>
    public static void Serialize(UndertaleWriter writer, IUndertaleFlexInstanceProperties props)
    {
        writer.Write(props.Visible);
        writer.Write(props.Anchor);
        writer.Write(props.StretchWidth);
        writer.Write(props.StretchHeight);
        writer.Write(props.TileH);
        writer.Write(props.TileV);
        writer.Write(props.KeepAspect);
    }

    /// <summary>
    /// Unserializes flex instance properties from the given reader.
    /// </summary>
    public static void Unserialize(UndertaleReader reader, IUndertaleFlexInstanceProperties props)
    {
        props.Visible = reader.ReadBoolean();
        props.Anchor = reader.ReadInt32();
        props.StretchWidth = reader.ReadBoolean();
        props.StretchHeight = reader.ReadBoolean();
        props.TileH = reader.ReadBoolean();
        props.TileV = reader.ReadBoolean();
        props.KeepAspect = reader.ReadBoolean();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 7 * 4;
        return 0;
    }
}

/// <summary>
/// Base class of all UI container nodes.
/// </summary>
public abstract class UndertaleUIContainer : IUndertaleUINodeData, IUndertaleFlexProperties
{
    /// <summary>
    /// Child nodes of the container.
    /// </summary>
    public UndertalePointerList<UndertaleUINode> Children { get; set; } = new();

    /// <inheritdoc/>
    public AlignmentKind AlignItems { get; set; }

    /// <inheritdoc/>
    public FlexDirectionKind FlexDirection { get; set; }

    /// <inheritdoc/>
    public WrapKind FlexWrap { get; set; }

    /// <inheritdoc/>
    public AlignmentKind AlignContent { get; set; }

    /// <inheritdoc/>
    public float GapRow { get; set; }

    /// <inheritdoc/>
    public float GapColumn { get; set; }

    /// <inheritdoc/>
    public UndertaleFlexValue PaddingLeft { get; set; }

    /// <inheritdoc/>
    public UndertaleFlexValue PaddingRight { get; set; }

    /// <inheritdoc/>
    public UndertaleFlexValue PaddingTop { get; set; }

    /// <inheritdoc/>
    public UndertaleFlexValue PaddingBottom { get; set; }

    /// <inheritdoc/>
    public JustifyKind JustifyContent { get; set; }

    /// <inheritdoc/>
    public LayoutDirectionKind LayoutDirection { get; set; }

    /// <inheritdoc/>
    public abstract void Serialize(UndertaleWriter writer);

    /// <inheritdoc/>
    public abstract void Unserialize(UndertaleReader reader);
}

/// <summary>
/// Represents a UI layer node.
/// </summary>
public class UndertaleUILayer : UndertaleUIContainer, UndertaleNamedResource
{
    /// <summary>
    /// Coordinate spaces used for drawing UI layers.
    /// </summary>
    public enum DrawSpaceKind
    {
        GUI = 1,
        View = 2
    }

    /// <summary>
    /// Name of the UI layer node.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Coordinate space used for drawing the UI layer node.
    /// </summary>
    public DrawSpaceKind DrawSpace { get; set; }

    /// <summary>
    /// Whether the UI layer node is visible.
    /// </summary>
    public bool Visible { get; set; }

    /// <inheritdoc/>
    public override void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write((int)DrawSpace);
        writer.Write(Visible);
        IUndertaleFlexProperties.Serialize(writer, this);
    }

    /// <inheritdoc/>
    public override void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        DrawSpace = (DrawSpaceKind)reader.ReadInt32();
        Visible = reader.ReadBoolean();
        IUndertaleFlexProperties.Unserialize(reader, this);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 4 * 3;
        return IUndertaleFlexProperties.UnserializeChildObjectCount(reader);
    }
}

/// <summary>
/// Represents a UI flex panel node.
/// </summary>
public class UndertaleUIFlexPanel : UndertaleUIContainer
{
    /// <summary>
    /// Position types for flex panels.
    /// </summary>
    public enum PositionTypeKind
    {
        Static = 0,
        Relative = 1,
        Absolute = 2
    }

    /// <summary>
    /// Name of the UI flex panel node.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Width of the flex panel.
    /// </summary>
    public UndertaleFlexValue Width { get; set; }

    /// <summary>
    /// Height of the flex panel.
    /// </summary>
    public UndertaleFlexValue Height { get; set; }

    /// <summary>
    /// Minimum width of the flex panel.
    /// </summary>
    public UndertaleFlexValue MinimumWidth { get; set; }

    /// <summary>
    /// Minimum height of the flex panel.
    /// </summary>
    public UndertaleFlexValue MinimumHeight { get; set; }

    /// <summary>
    /// Maximum width of the flex panel.
    /// </summary>
    public UndertaleFlexValue MaximumWidth { get; set; }

    /// <summary>
    /// Maximum height of the flex panel.
    /// </summary>
    public UndertaleFlexValue MaximumHeight { get; set; }

    /// <summary>
    /// Left offset of the flex panel.
    /// </summary>
    public UndertaleFlexValue OffsetLeft { get; set; }

    /// <summary>
    /// Right offset of the flex panel.
    /// </summary>
    public UndertaleFlexValue OffsetRight { get; set; }

    /// <summary>
    /// Top offset of the flex panel.
    /// </summary>
    public UndertaleFlexValue OffsetTop { get; set; }

    /// <summary>
    /// Bottom offset of the flex panel.
    /// </summary>
    public UndertaleFlexValue OffsetBottom { get; set; }

    /// <summary>
    /// Whether the flex panel clips its contents.
    /// </summary>
    public bool ClipsContents { get; set; }

    /// <summary>
    /// Position type of the flex panel.
    /// </summary>
    public PositionTypeKind PositionType { get; set; }

    /// <summary>
    /// How the flex panel aligns itself.
    /// </summary>
    public AlignmentKind AlignSelf { get; set; }

    /// <summary>
    /// Left margin of the flex panel.
    /// </summary>
    public UndertaleFlexValue MarginLeft { get; set; }

    /// <summary>
    /// Right margin of the flex panel.
    /// </summary>
    public UndertaleFlexValue MarginRight { get; set; }

    /// <summary>
    /// Top margin of the flex panel.
    /// </summary>
    public UndertaleFlexValue MarginTop { get; set; }

    /// <summary>
    /// Bottom margin of the flex panel.
    /// </summary>
    public UndertaleFlexValue MarginBottom { get; set; }

    /// <summary>
    /// Flex grow property of the flex panel.
    /// </summary>
    public float FlexGrow { get; set; }

    /// <summary>
    /// Flex shrink property of the flex panel.
    /// </summary>
    public float FlexShrink { get; set; }

    /// <inheritdoc/>
    public override void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        Width.Serialize(writer);
        Height.Serialize(writer);
        MinimumWidth.Serialize(writer);
        MinimumHeight.Serialize(writer);
        MaximumWidth.Serialize(writer);
        MaximumHeight.Serialize(writer);
        OffsetLeft.Serialize(writer);
        OffsetRight.Serialize(writer);
        OffsetTop.Serialize(writer);
        OffsetBottom.Serialize(writer);
        writer.Write(ClipsContents);
        writer.Write((int)PositionType);
        writer.Write((int)AlignSelf);
        MarginLeft.Serialize(writer);
        MarginRight.Serialize(writer);
        MarginTop.Serialize(writer);
        MarginBottom.Serialize(writer);
        writer.Write(FlexGrow);
        writer.Write(FlexShrink);
        IUndertaleFlexProperties.Serialize(writer, this);
    }

    /// <inheritdoc/>
    public override void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Width = new(reader);
        Height = new(reader);
        MinimumWidth = new(reader);
        MinimumHeight = new(reader);
        MaximumWidth = new(reader);
        MaximumHeight = new(reader);
        OffsetLeft = new(reader);
        OffsetRight = new(reader);
        OffsetTop = new(reader);
        OffsetBottom = new(reader);
        ClipsContents = reader.ReadBoolean();
        PositionType = (PositionTypeKind)reader.ReadInt32();
        AlignSelf = (AlignmentKind)reader.ReadInt32();
        MarginLeft = new(reader);
        MarginRight = new(reader);
        MarginTop = new(reader);
        MarginBottom = new(reader);
        FlexGrow = reader.ReadSingle();
        FlexShrink = reader.ReadSingle();
        IUndertaleFlexProperties.Unserialize(reader, this);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 20 * 4;
        return IUndertaleFlexProperties.UnserializeChildObjectCount(reader);
    }
}

/// <summary>
/// Represents a UI game object node.
/// </summary>
public class UndertaleUIGameObject : UndertaleRoom.GameObject, IUndertaleUINodeData, IUndertaleFlexInstanceProperties
{
    /// <inheritdoc/>
    public bool Visible { get; set; }

    /// <inheritdoc/>
    public int Anchor { get; set; }

    /// <inheritdoc/>
    public bool StretchWidth { get; set; }

    /// <inheritdoc/>
    public bool StretchHeight { get; set; }

    /// <inheritdoc/>
    public bool TileH { get; set; }

    /// <inheritdoc/>
    public bool TileV { get; set; }

    /// <inheritdoc/>
    public bool KeepAspect { get; set; }

    /// <inheritdoc/>
    public override void Serialize(UndertaleWriter writer)
    {
        base.Serialize(writer);
        IUndertaleFlexInstanceProperties.Serialize(writer, this);
    }

    /// <inheritdoc/>
    public override void Unserialize(UndertaleReader reader)
    {
        base.Unserialize(reader);
        IUndertaleFlexInstanceProperties.Unserialize(reader, this);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 12 * 4;
        return IUndertaleFlexInstanceProperties.UnserializeChildObjectCount(reader);
    }
}

/// <summary>
/// Represents a UI sequence instance node.
/// </summary>
public class UndertaleUISequenceInstance : UndertaleRoom.SequenceInstance, IUndertaleUINodeData, IUndertaleFlexInstanceProperties
{
    /// <inheritdoc/>
    public bool Visible { get; set; }

    /// <inheritdoc/>
    public int Anchor { get; set; }

    /// <inheritdoc/>
    public bool StretchWidth { get; set; }

    /// <inheritdoc/>
    public bool StretchHeight { get; set; }

    /// <inheritdoc/>
    public bool TileH { get; set; }

    /// <inheritdoc/>
    public bool TileV { get; set; }

    /// <inheritdoc/>
    public bool KeepAspect { get; set; }

    /// <inheritdoc/>
    public override void Serialize(UndertaleWriter writer)
    {
        base.Serialize(writer);
        IUndertaleFlexInstanceProperties.Serialize(writer, this);
    }

    /// <inheritdoc/>
    public override void Unserialize(UndertaleReader reader)
    {
        base.Unserialize(reader);
        IUndertaleFlexInstanceProperties.Unserialize(reader, this);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 11 * 4;
        return IUndertaleFlexInstanceProperties.UnserializeChildObjectCount(reader);
    }
}

/// <summary>
/// Represents a UI sprite instance node.
/// </summary>
public class UndertaleUISpriteInstance : UndertaleRoom.SpriteInstance, IUndertaleUINodeData, IUndertaleFlexInstanceProperties
{
    /// <inheritdoc/>
    public bool Visible { get; set; }

    /// <inheritdoc/>
    public int Anchor { get; set; }

    /// <inheritdoc/>
    public bool StretchWidth { get; set; }

    /// <inheritdoc/>
    public bool StretchHeight { get; set; }

    /// <inheritdoc/>
    public bool TileH { get; set; }

    /// <inheritdoc/>
    public bool TileV { get; set; }

    /// <inheritdoc/>
    public bool KeepAspect { get; set; }

    /// <inheritdoc/>
    public override void Serialize(UndertaleWriter writer)
    {
        base.Serialize(writer);
        IUndertaleFlexInstanceProperties.Serialize(writer, this);
    }

    /// <inheritdoc/>
    public override void Unserialize(UndertaleReader reader)
    {
        base.Unserialize(reader);
        IUndertaleFlexInstanceProperties.Unserialize(reader, this);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 11 * 4;
        return IUndertaleFlexInstanceProperties.UnserializeChildObjectCount(reader);
    }
}

/// <summary>
/// Represents a UI text item instance node.
/// </summary>
public class UndertaleUITextItemInstance : UndertaleRoom.TextItemInstance, IUndertaleUINodeData, IUndertaleFlexInstanceProperties
{
    /// <inheritdoc/>
    public bool Visible { get; set; }

    /// <inheritdoc/>
    public int Anchor { get; set; }

    /// <inheritdoc/>
    public bool StretchWidth { get; set; }

    /// <inheritdoc/>
    public bool StretchHeight { get; set; }

    /// <inheritdoc/>
    public bool TileH { get; set; }

    /// <inheritdoc/>
    public bool TileV { get; set; }

    /// <inheritdoc/>
    public bool KeepAspect { get; set; }

    /// <inheritdoc/>
    public override void Serialize(UndertaleWriter writer)
    {
        base.Serialize(writer);
        IUndertaleFlexInstanceProperties.Serialize(writer, this);
    }

    /// <inheritdoc/>
    public override void Unserialize(UndertaleReader reader)
    {
        base.Unserialize(reader);
        IUndertaleFlexInstanceProperties.Unserialize(reader, this);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public new static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 17 * 4;
        return IUndertaleFlexInstanceProperties.UnserializeChildObjectCount(reader);
    }
}

/// <summary>
/// Represents a UI effect layer node.
/// </summary>
public class UndertaleUIEffectLayer : IUndertaleUINodeData
{
    /// <summary>
    /// Whether the effect layer is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Effect type for the effect layer.
    /// </summary>
    public UndertaleString EffectType { get; set; }

    /// <summary>
    /// List of effect properties for the effect layer.
    /// </summary>
    public UndertalePointerList<UndertaleRoom.EffectProperty> Properties { get; set; } = new();

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Enabled);
        writer.WriteUndertaleString(EffectType);
        Properties.Serialize(writer);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Enabled = reader.ReadBoolean();
        EffectType = reader.ReadUndertaleString();
        Properties.Unserialize(reader);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 8;
        return UndertalePointerList<UndertaleRoom.EffectProperty>.UnserializeChildObjectCount(reader);
    }
}
