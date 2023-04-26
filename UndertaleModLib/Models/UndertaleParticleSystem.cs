using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UndertaleModLib.Models;

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleParticleSystem : UndertaleNamedResource, IDisposable
{
    public UndertaleString Name { get; set; }

    public int OriginX { get; set; }

    public int OriginY { get; set; }

    public int DrawOrder { get; set; }

    public UndertaleSimpleResourcesList<UndertaleParticleSystemEmitter, UndertaleChunkPSEM> Emitters { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write(OriginX);
        writer.Write(OriginY);
        writer.Write(DrawOrder);
        writer.WriteUndertaleObject(Emitters);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        OriginX = reader.ReadInt32();
        OriginY = reader.ReadInt32();
        DrawOrder = reader.ReadInt32();
        Emitters = reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleParticleSystemEmitter, UndertaleChunkPSEM>>();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 16;

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

    public UndertaleString Name { get; set; }

    public EmitMode Mode { get; set; }

    public int EmitCount { get; set; }

    public DistributionEnum Distribution { get; set; }

    public EmitterShape Shape { get; set; }

    public float RegionX { get; set; }

    public float RegionY { get; set; }

    public float RegionWidth { get; set; }

    public float RegionHeight { get; set; }

    public float Rotation { get; set; }

    public UndertaleSprite Sprite { get => _sprite.Resource; set { _sprite.Resource = value; OnPropertyChanged(); } }

    public TextureEnum Texture { get; set; }

    public float HeadPosition { get; set; }

    public uint StartColor { get; set; }

    public uint MidColor { get; set; }

    public uint EndColor { get; set; }

    public bool AdditiveBlend { get; set; }

    public float LifetimeMin { get; set; }

    public float LifetimeMax { get; set; }

    public float ScaleX { get; set; }

    public float ScaleY { get; set; }

    public float SizeMin { get; set; }

    public float SizeMax { get; set; }

    public float SizeIncrease { get; set; }

    public float SizeWiggle { get; set; }

    public float SpeedMin { get; set; }

    public float SpeedMax { get; set; }

    public float SpeedIncrease { get; set; }

    public float SpeedWiggle { get; set; }

    public float GravityForce { get; set; }

    public float GravityDirection { get; set; }

    public float DirectionMin { get; set; }

    public float DirectionMax { get; set; }

    public float DirectionIncrease { get; set; }

    public float DirectionWiggle { get; set; }

    public float OrientationMin { get; set; }

    public float OrientationMax { get; set; }

    public float OrientationIncrease { get; set; }

    public float OrientationWiggle { get; set; }

    public bool OrientationRelative { get; set; }

    public UndertaleParticleSystemEmitter SpawnOnDeath { get => _spawnOnDeath.Resource; set { _spawnOnDeath.Resource = value; OnPropertyChanged(); } }

    public int SpawnOnDeathCount { get; set; }

    public UndertaleParticleSystemEmitter SpawnOnUpdate { get => _spawnOnUpdate.Resource; set { _spawnOnUpdate.Resource = value; OnPropertyChanged(); } }

    public int SpawnOnUpdateCount { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write((int)Mode);
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
        writer.Write(HeadPosition);
        writer.Write(StartColor);
        writer.Write(MidColor);
        writer.Write(EndColor);
        writer.Write(AdditiveBlend);
        writer.Write(LifetimeMin);
        writer.Write(LifetimeMax);
        writer.Write(ScaleX);
        writer.Write(ScaleY);
        writer.Write(SizeMin);
        writer.Write(SizeMax);
        writer.Write(SizeIncrease);
        writer.Write(SizeWiggle);
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
        Mode = (EmitMode)reader.ReadInt32();
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
        HeadPosition = reader.ReadSingle();
        StartColor = reader.ReadUInt32();
        MidColor = reader.ReadUInt32();
        EndColor = reader.ReadUInt32();
        AdditiveBlend = reader.ReadBoolean();
        LifetimeMin = reader.ReadSingle();
        LifetimeMax = reader.ReadSingle();
        ScaleX = reader.ReadSingle();
        ScaleY = reader.ReadSingle();
        SizeMin = reader.ReadSingle();
        SizeMax = reader.ReadSingle();
        SizeIncrease = reader.ReadSingle();
        SizeWiggle = reader.ReadSingle();
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
}