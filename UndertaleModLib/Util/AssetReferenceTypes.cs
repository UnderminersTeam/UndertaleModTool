using System;

namespace UndertaleModLib.Util;

public static class AssetReferenceTypes
{
    public enum RefType
    {
        Object,
        Sprite,
        Sound,
        Room,
        Background,
        Path,
        Script,
        Font,
        Timeline,
        Shader,
        Sequence,
        AnimCurve,
        ParticleSystem,
        RoomInstance
    }

    public static RefType ConvertToRefType(UndertaleData data, int type)
    {
        if (data.IsVersionAtLeast(2024, 4))
        {
            return type switch
            {
                0 => RefType.Object,
                1 => RefType.Sprite,
                2 => RefType.Sound,
                3 => RefType.Room,
                4 => RefType.Path,
                5 => RefType.Script,
                6 => RefType.Font,
                7 => RefType.Timeline,
                8 => RefType.Shader,
                9 => RefType.Sequence,
                10 => RefType.AnimCurve,
                11 => RefType.ParticleSystem,
                13 => RefType.Background,
                14 => RefType.RoomInstance,
                _ => throw new Exception($"Unknown ref type {type}")
            };
        }

        return type switch
        {
            0 => RefType.Object,
            1 => RefType.Sprite,
            2 => RefType.Sound,
            3 => RefType.Room,
            4 => RefType.Background,
            5 => RefType.Path,
            6 => RefType.Script,
            7 => RefType.Font,
            8 => RefType.Timeline,
            10 => RefType.Shader,
            11 => RefType.Sequence,
            12 => RefType.AnimCurve,
            13 => RefType.ParticleSystem,
            14 => RefType.RoomInstance,
            _ => throw new Exception($"Unknown ref type {type}")
        };
    }

    public static int ConvertFromRefType(UndertaleData data, RefType type)
    {
        if (data.IsVersionAtLeast(2024, 4))
        {
            return type switch
            {
                RefType.Object => 0,
                RefType.Sprite => 1,
                RefType.Sound => 2,
                RefType.Room => 3,
                RefType.Path => 4,
                RefType.Script => 5,
                RefType.Font => 6,
                RefType.Timeline => 7,
                RefType.Shader => 8,
                RefType.Sequence => 9,
                RefType.AnimCurve => 10,
                RefType.ParticleSystem => 11,
                RefType.Background => 13,
                RefType.RoomInstance => 14,
                _ => throw new Exception($"Unknown ref type {type}")
            };
        }

        return type switch
        {
            RefType.Object => 0,
            RefType.Sprite => 1,
            RefType.Sound => 2,
            RefType.Room => 3,
            RefType.Background => 4,
            RefType.Path => 5,
            RefType.Script => 6,
            RefType.Font => 7,
            RefType.Timeline => 8,
            RefType.Shader => 10,
            RefType.Sequence => 11,
            RefType.AnimCurve => 12,
            RefType.ParticleSystem => 13,
            RefType.RoomInstance => 14,
            _ => throw new Exception($"Unknown ref type {type}")
        };
    }
}