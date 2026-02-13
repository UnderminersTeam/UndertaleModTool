using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UndertaleModLib.Models;

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleParticleSystem : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// The name of the particle system.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The x-coordinate of the origin of the particle system.
    /// </summary>
    public int OriginX { get; set; }

    /// <summary>
    /// The y-coordinate of the origin of the particle system.
    /// </summary>
    public int OriginY { get; set; }

    /// <summary>
    /// The draw order of the particle system, determining whether new particles are drawn on top of or below older particles.
    /// </summary>
    public DrawOrderEnum DrawOrder { get; set; }

    // TODO: Documentation on this value
    public bool GlobalSpaceParticles { get; set; }

    /// <summary>
    /// The Emitters of the particle system
    /// </summary>
    public UndertaleSimpleResourcesList<UndertaleParticleSystemEmitter, UndertaleChunkPSEM> Emitters { get; set; } = new();

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write(OriginX);
        writer.Write(OriginY);
        writer.Write((int)DrawOrder);
        if (writer.undertaleData.IsVersionAtLeast(2023, 8))
            writer.Write(GlobalSpaceParticles);
        writer.WriteUndertaleObject(Emitters);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        OriginX = reader.ReadInt32();
        OriginY = reader.ReadInt32();
        DrawOrder = (DrawOrderEnum)reader.ReadInt32();
        if (reader.undertaleData.IsVersionAtLeast(2023, 8))
            GlobalSpaceParticles = reader.ReadBoolean();
        Emitters = reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleParticleSystemEmitter, UndertaleChunkPSEM>>();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 16;

        if (reader.undertaleData.IsVersionAtLeast(2023, 8))
            reader.Position += 4;

        return 1 + UndertaleSimpleResourcesList<UndertaleParticleSystemEmitter, UndertaleChunkPSEM>.UnserializeChildObjectCount(reader);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        Emitters = null;
    }
    public enum DrawOrderEnum
    {
        NewOnTop,
        OldOnTop,
    }
}


public class UndertaleParticleSystemEmitter : UndertaleNamedResource, INotifyPropertyChanged, IDisposable,
                                              IStaticChildObjectsSize
{
    /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
    public static readonly uint ChildObjectsSize = 176;

    private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _sprite = new();
    private UndertaleResourceById<UndertaleParticleSystemEmitter, UndertaleChunkPSEM> _spawnOnDeath = new();
    private UndertaleResourceById<UndertaleParticleSystemEmitter, UndertaleChunkPSEM> _spawnOnUpdate = new();

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// The name of the emitter
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Whether the emitter is enabled. Only in 2023.6
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The emit mode of the emitter.
    /// </summary>
    public EmitMode Mode { get; set; }

    /// <summary>
    /// The amount of particles to emit.
    /// </summary>
    public int EmitCount { get; set; } = 1; // Note: technically float in 2023.8

    // 2023.8
    // TODO: document this, always 0?
    public bool EmitRelative { get; set; }
    /// <summary>
    /// The minimum delay between particle emissions.
    /// </summary>
    public float DelayMin { get; set; }

    /// <summary>
    /// The maximum delay between particle emissions.
    /// </summary>
    public float DelayMax { get; set; }

    /// <summary>
    /// The Time unit to use for <see cref="DelayMin"/> and <see cref="DelayMax"/>.
    /// </summary>
    public TimeUnitEnum DelayUnit { get; set; }

    // TODO: find how interval and delay are different?

    /// <summary>
    /// The minimum interval between particle emissions.
    /// </summary>
    public float IntervalMin { get; set; }

    /// <summary>
    /// The maximum interval between particle emissions.
    /// </summary>
    public float IntervalMax { get; set; }

    /// <summary>
    /// The Time unit to use for <see cref="IntervalMin"/> and <see cref="IntervalMax"/>.
    /// </summary>
    public TimeUnitEnum IntervalUnit { get; set; }

    /// <summary>
    /// The distribution type of the emitter, determining how they are distributed around the emitter.
    /// </summary>
    public DistributionEnum Distribution { get; set; }
    
    /// <summary>
    /// The shape of the emitter.
    /// </summary>
    public EmitterShape Shape { get; set; }

    /// <summary>
    /// The x-coordinate of the emitter.
    /// </summary>
    public float RegionX { get; set; }

    /// <summary>
    /// The y-coordinate of the emitter.
    /// </summary>
    public float RegionY { get; set; }

    /// <summary>
    /// The width (in pixels) of the emitter.
    /// </summary>
    public float RegionWidth { get; set; } = 64;

    /// <summary>
    /// The height (in pixels) of the emitter.
    /// </summary>
    public float RegionHeight { get; set; } = 64;

    /// <summary>
    /// The angle of the emitter.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// The sprite that this emitter uses for its particles.
    /// </summary>
    public UndertaleSprite Sprite { get => _sprite.Resource; set { _sprite.Resource = value; OnPropertyChanged(); } }

    /// <summary>
    /// The preset textures for the emitter to use.
    /// </summary>
    public TextureEnum Texture { get; set; }

    // TODO: finish documenting these values.

    public float FrameIndex { get; set; }

    // 2023.4
    public bool Animate { get; set; }
    public bool Stretch { get; set; }
    public bool IsRandom { get; set; }

    public uint StartColor { get; set; } = 0xFFFFFFFF;

    public uint MidColor { get; set; } = 0xFFFFFFFF;

    public uint EndColor { get; set; } = 0xFFFFFFFF;

    public bool AdditiveBlend { get; set; }

    public float LifetimeMin { get; set; } = 80;

    public float LifetimeMax { get; set; } = 80;

    public float ScaleX { get; set; } = 1;

    public float ScaleY { get; set; } = 1;

    // 2023.8
    public float SizeMinX { get; set; } = 1;
    public float SizeMaxX { get; set; } = 1;
    public float SizeIncreaseX { get; set; }
    public float SizeWiggleX { get; set; }
    public float SizeMinY { get; set; } = 1;
    public float SizeMaxY { get; set; } = 1;
    public float SizeIncreaseY { get; set; }
    public float SizeWiggleY { get; set; }

    // These two are used and serialized prior to 2023.8, retained for compatibility.
    public float SizeMin { get => (SizeMinX + SizeMinY) / 2; set { SizeMinX = value; SizeMinY = value; } }
    public float SizeMax { get => (SizeMaxX + SizeMaxY) / 2; set { SizeMaxX = value; SizeMaxY = value; } }

    public float SizeIncrease { get => (SizeIncreaseX + SizeIncreaseY) / 2; set { SizeIncreaseX = value; SizeIncreaseY = value; } }

    public float SizeWiggle { get => (SizeWiggleX + SizeWiggleY) / 2; set { SizeWiggleX = value; SizeWiggleY = value; } }

    public float SpeedMin { get; set; } = 5;

    public float SpeedMax { get; set; } = 5;

    public float SpeedIncrease { get; set; }

    public float SpeedWiggle { get; set; }

    public float GravityForce { get; set; }

    public float GravityDirection { get; set; } = 270;

    public float DirectionMin { get; set; } = 80;

    public float DirectionMax { get; set; } = 100;

    public float DirectionIncrease { get; set; }

    public float DirectionWiggle { get; set; }

    public float OrientationMin { get; set; }

    public float OrientationMax { get; set; }

    public float OrientationIncrease { get; set; }

    public float OrientationWiggle { get; set; }

    public bool OrientationRelative { get; set; }

    public UndertaleParticleSystemEmitter SpawnOnDeath { get => _spawnOnDeath.Resource; set { _spawnOnDeath.Resource = value; OnPropertyChanged(); } }

    public int SpawnOnDeathCount { get; set; } = 1;

    public UndertaleParticleSystemEmitter SpawnOnUpdate { get => _spawnOnUpdate.Resource; set { _spawnOnUpdate.Resource = value; OnPropertyChanged(); } }

    public int SpawnOnUpdateCount { get; set; } = 1;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        if (writer.undertaleData.IsVersionAtLeast(2023, 6))
            writer.Write(Enabled);
        writer.Write((int)Mode);
        if (writer.undertaleData.IsVersionAtLeast(2023, 8))
        {
            writer.Write((float)EmitCount);
            writer.Write(EmitRelative);
            writer.Write(DelayMin);
            writer.Write(DelayMax);
            writer.Write((int)DelayUnit);
            writer.Write(IntervalMin);
            writer.Write(IntervalMax);
            writer.Write((int)IntervalUnit);
        }
        else
            writer.Write(EmitCount);
        writer.Write((int)Distribution);
        writer.Write((int)Shape);
        writer.Write(RegionX);
        writer.Write(RegionY);
        writer.Write(RegionWidth);
        writer.Write(RegionHeight);
        writer.Write(Rotation);
        writer.Write(_sprite.SerializeById(writer));
        writer.Write((int)Texture);
        writer.Write(FrameIndex);
        if (writer.undertaleData.IsVersionAtLeast(2023, 4))
        {
            writer.Write(Animate);
            writer.Write(Stretch);
            writer.Write(IsRandom);
        }
        writer.Write(StartColor);
        writer.Write(MidColor);
        writer.Write(EndColor);
        writer.Write(AdditiveBlend);
        writer.Write(LifetimeMin);
        writer.Write(LifetimeMax);
        writer.Write(ScaleX);
        writer.Write(ScaleY);
        if (writer.undertaleData.IsVersionAtLeast(2023, 8))
        {
            writer.Write(SizeMinX);
            writer.Write(SizeMaxX);
            writer.Write(SizeMinY);
            writer.Write(SizeMaxY);
            writer.Write(SizeIncreaseX);
            writer.Write(SizeIncreaseY);
            writer.Write(SizeWiggleX);
            writer.Write(SizeWiggleY);
        }
        else
        {
            writer.Write(SizeMin);
            writer.Write(SizeMax);
            writer.Write(SizeIncrease);
            writer.Write(SizeWiggle);
        }
        writer.Write(SpeedMin);
        writer.Write(SpeedMax);
        writer.Write(SpeedIncrease);
        writer.Write(SpeedWiggle);
        writer.Write(GravityForce);
        writer.Write(GravityDirection);
        writer.Write(DirectionMin);
        writer.Write(DirectionMax);
        writer.Write(DirectionIncrease);
        writer.Write(DirectionWiggle);
        writer.Write(OrientationMin);
        writer.Write(OrientationMax);
        writer.Write(OrientationIncrease);
        writer.Write(OrientationWiggle);
        writer.Write(OrientationRelative);

        writer.Write(_spawnOnDeath.SerializeById(writer));
        writer.Write(SpawnOnDeathCount);
        writer.Write(_spawnOnUpdate.SerializeById(writer));
        writer.Write(SpawnOnUpdateCount);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        if (reader.undertaleData.IsVersionAtLeast(2023, 6))
            Enabled = reader.ReadBoolean();
        Mode = (EmitMode)reader.ReadInt32();
        if (reader.undertaleData.IsVersionAtLeast(2023, 8))
        {
            EmitCount = (int)reader.ReadSingle(); // The GUI still only allows integer input...
            EmitRelative = reader.ReadBoolean(); // Always 0
            DelayMin = reader.ReadSingle();
            DelayMax = reader.ReadSingle();
            DelayUnit = (TimeUnitEnum)reader.ReadInt32();
            IntervalMin = reader.ReadSingle();
            IntervalMax = reader.ReadSingle();
            IntervalUnit = (TimeUnitEnum)reader.ReadInt32();
        }
        else
            EmitCount = reader.ReadInt32();
        Distribution = (DistributionEnum)reader.ReadInt32();
        Shape = (EmitterShape)reader.ReadInt32();
        RegionX = reader.ReadSingle();
        RegionY = reader.ReadSingle();
        RegionWidth = reader.ReadSingle();
        RegionHeight = reader.ReadSingle();
        Rotation = reader.ReadSingle();
        _sprite = new();
        _sprite.Unserialize(reader);
        Texture = (TextureEnum)reader.ReadInt32();
        FrameIndex = reader.ReadSingle();
        if (reader.undertaleData.IsVersionAtLeast(2023, 4))
        {
            Animate = reader.ReadBoolean();
            Stretch = reader.ReadBoolean();
            IsRandom = reader.ReadBoolean();
        }
        StartColor = reader.ReadUInt32();
        MidColor = reader.ReadUInt32();
        EndColor = reader.ReadUInt32();
        AdditiveBlend = reader.ReadBoolean();
        LifetimeMin = reader.ReadSingle();
        LifetimeMax = reader.ReadSingle();
        ScaleX = reader.ReadSingle();
        ScaleY = reader.ReadSingle();
        if (reader.undertaleData.IsVersionAtLeast(2023, 8))
        {
            SizeMinX = reader.ReadSingle();
            SizeMaxX = reader.ReadSingle();
            SizeMinY = reader.ReadSingle();
            SizeMaxY = reader.ReadSingle();
            SizeIncreaseX = reader.ReadSingle();
            SizeIncreaseY = reader.ReadSingle();
            SizeWiggleX = reader.ReadSingle();
            SizeWiggleY = reader.ReadSingle();
        }
        else
        {
            SizeMin = reader.ReadSingle();
            SizeMax = reader.ReadSingle();
            SizeIncrease = reader.ReadSingle();
            SizeWiggle = reader.ReadSingle();
        }
        SpeedMin = reader.ReadSingle();
        SpeedMax = reader.ReadSingle();
        SpeedIncrease = reader.ReadSingle();
        SpeedWiggle = reader.ReadSingle();
        GravityForce = reader.ReadSingle();
        GravityDirection = reader.ReadSingle();
        DirectionMin = reader.ReadSingle();
        DirectionMax = reader.ReadSingle();
        DirectionIncrease = reader.ReadSingle();
        DirectionWiggle = reader.ReadSingle();
        OrientationMin = reader.ReadSingle();
        OrientationMax = reader.ReadSingle();
        OrientationIncrease = reader.ReadSingle();
        OrientationWiggle = reader.ReadSingle();
        OrientationRelative = reader.ReadBoolean();

        _spawnOnDeath = new();
        _spawnOnDeath.Unserialize(reader);
        SpawnOnDeathCount = reader.ReadInt32();
        _spawnOnUpdate = new();
        _spawnOnUpdate.Unserialize(reader);
        SpawnOnUpdateCount = reader.ReadInt32();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        _sprite.Dispose();
        _spawnOnDeath.Dispose();
        _spawnOnUpdate.Dispose();
    }

    // TODO: More documentation needed
    public enum EmitMode
    {
        Stream,
        Burst
    }
    public enum DistributionEnum
    {
        Linear,
        Gaussian,
        InverseGaussian,
    }
    public enum EmitterShape
    {
        Rectangle,
        Ellipse,
        Diamond,
        Line,
    }
    public enum TextureEnum
    {
        None = -1,
        Pixel = 0,
        Disk = 1,
        Square = 2,
        Line = 3,
        Star = 4,
        Circle = 5,
        Ring = 6,
        Sphere = 7,
        Flare = 8,
        Spark = 9,
        Explosion = 10,
        Cloud = 11,
        Smoke = 12,
        Snow = 13
    }
    public enum TimeUnitEnum
    {
        Seconds,
        Frames
    }
}