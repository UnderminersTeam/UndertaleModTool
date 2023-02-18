using System;
using System.Collections;
using System.Collections.Generic;

namespace UndertaleModLib.Models;

/// <summary>
/// A tag entry in a GameMaker data file. Tags are a GameMaker: Studio 2.3+ feature.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleTags : UndertaleObject, IDisposable
{
    public UndertaleSimpleListString Tags { get; set; }
    public Dictionary<int, UndertaleSimpleListString> AssetTags { get; set; }

    public static int GetAssetTagID(UndertaleData data, UndertaleNamedResource resource)
    {
        ResourceType type = resource switch
        {
            UndertaleGameObject => ResourceType.Object,
            UndertaleSprite => ResourceType.Sprite,
            UndertaleSound => ResourceType.Sound,
            UndertaleRoom => ResourceType.Room,
            UndertalePath => ResourceType.Path,
            UndertaleScript => ResourceType.Script,
            UndertaleFont => ResourceType.Font,
            UndertaleTimeline => ResourceType.Timeline,
            UndertaleBackground => ResourceType.Background,
            UndertaleShader => ResourceType.Shader,
            UndertaleSequence => ResourceType.Sequence,
            UndertaleAnimationCurve => ResourceType.AnimCurve,
            _ => throw new ArgumentException("Invalid resource type!")
        };
        IList list = data[resource.GetType()] as IList;

        int offset = resource is UndertaleScript ? 100000 : 0;

        return ((int)type << 24) | ((list.IndexOf(resource) + offset) & 0xFFFFFF);
    }


    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        Tags.Serialize(writer);
        UndertalePointerList<TempAssetTags> temp = new UndertalePointerList<TempAssetTags>();
        foreach (var kvp in AssetTags)
            temp.Add(new TempAssetTags() { ID = kvp.Key, Tags = kvp.Value });
        temp.Serialize(writer);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Tags = new UndertaleSimpleListString();
        Tags.Unserialize(reader);
        UndertalePointerList<TempAssetTags> temp = reader.ReadUndertaleObject<UndertalePointerList<TempAssetTags>>();
        AssetTags = new Dictionary<int, UndertaleSimpleListString>();
        foreach (TempAssetTags t in temp)
            AssetTags[t.ID] = t.Tags;
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        count += UndertaleSimpleListString.UnserializeChildObjectCount(reader);
        count += 1 + UndertalePointerList<TempAssetTags>.UnserializeChildObjectCount(reader);

        return count;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        AssetTags = null;
        Tags = null;
    }

    private class TempAssetTags : UndertaleObject, IDisposable
    {
        public int ID { get; set; }
        public UndertaleSimpleListString Tags { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(ID);
            Tags.Serialize(writer);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            ID = reader.ReadInt32();
            Tags = reader.ReadUndertaleObject<UndertaleSimpleListString>();
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            reader.Position += 4;
            return 1 + UndertaleSimpleListString.UnserializeChildObjectCount(reader);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Tags = null;
        }
    }
}