using System;
using System.Collections.Generic;

namespace UndertaleModLib.Models;

/// <summary>
/// A tag entry in a GameMaker data file. Tags are a GameMaker: Studio 2.3+ feature.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleTags : UndertaleObject
{
    public UndertaleSimpleListString Tags { get; set; }
    public Dictionary<int, UndertaleSimpleListString> AssetTags { get; set; }

    public static int GetAssetTagID(UndertaleData data, UndertaleNamedResource resource)
    {
        ResourceType type;
        IList<UndertaleNamedResource> list;
        int offset = 0;

        switch (resource)
        {
            case UndertaleGameObject:
                type = ResourceType.Object;
                list = (IList<UndertaleNamedResource>) data.GameObjects;
                break;
            case UndertaleSprite:
                type = ResourceType.Sprite;
                list = (IList<UndertaleNamedResource>) data.Sprites;
                break;
            case UndertaleSound:
                type = ResourceType.Sound;
                list = (IList<UndertaleNamedResource>) data.Sounds;
                break;
            case UndertaleRoom:
                type = ResourceType.Room;
                list = (IList<UndertaleNamedResource>) data.Rooms;
                break;
            case UndertalePath:
                type = ResourceType.Path;
                list = (IList<UndertaleNamedResource>) data.Paths;
                break;
            case UndertaleScript:
                type = ResourceType.Script;
                list = (IList<UndertaleNamedResource>) data.Scripts;
                offset = 100000;
                break;
            case UndertaleFont:
                type = ResourceType.Font;
                list = (IList<UndertaleNamedResource>) data.Fonts;
                break;
            case UndertaleTimeline:
                type = ResourceType.Timeline;
                list = (IList<UndertaleNamedResource>) data.Timelines;
                break;
            case UndertaleBackground:
                type = ResourceType.Background;
                list = (IList<UndertaleNamedResource>) data.Backgrounds;
                break;
            case UndertaleShader:
                type = ResourceType.Shader;
                list = (IList<UndertaleNamedResource>) data.Shaders;
                break;
            case UndertaleSequence:
                type = ResourceType.Sequence;
                list = (IList<UndertaleNamedResource>) data.Sequences;
                break;
            case UndertaleAnimationCurve:
                type = ResourceType.AnimCurve;
                list = (IList<UndertaleNamedResource>) data.AnimationCurves;
                break;

            default: throw new ArgumentException("Invalid resource type!");
        }

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

    private class TempAssetTags : UndertaleObject
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
    }
}