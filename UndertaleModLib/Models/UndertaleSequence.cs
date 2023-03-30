using System;
using System.Collections.Generic;
using System.IO;

namespace UndertaleModLib.Models;

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleSequence : UndertaleNamedResource, IDisposable
{
    public enum PlaybackType : uint
    {
        Oneshot = 0,
        Loop = 1,
        Pingpong = 2
    }

    public UndertaleString Name { get; set; }
    public PlaybackType Playback { get; set; }
    public float PlaybackSpeed { get; set; }
    public AnimSpeedType PlaybackSpeedType { get; set; }
    public float Length { get; set; }
    public int OriginX { get; set; }
    public int OriginY { get; set; }
    public float Volume { get; set; }
    public UndertaleSimpleList<Keyframe<BroadcastMessage>> BroadcastMessages { get; set; }
    public UndertaleSimpleList<Keyframe<Moment>> Moments { get; set; }
    public UndertaleSimpleList<Track> Tracks { get; set; }
    public Dictionary<int, UndertaleString> FunctionIDs { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write((uint)Playback);
        writer.Write(PlaybackSpeed);
        writer.Write((uint)PlaybackSpeedType);
        writer.Write(Length);
        writer.Write(OriginX);
        writer.Write(OriginY);
        writer.Write(Volume);

        BroadcastMessages.Serialize(writer);

        Tracks.Serialize(writer);

        writer.Write(FunctionIDs.Count);
        foreach (KeyValuePair<int, UndertaleString> kvp in FunctionIDs)
        {
            writer.Write(kvp.Key);
            writer.WriteUndertaleString(kvp.Value);
        }

        Moments.Serialize(writer);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Playback = (PlaybackType)reader.ReadUInt32();
        PlaybackSpeed = reader.ReadSingle();
        PlaybackSpeedType = (AnimSpeedType)reader.ReadUInt32();
        Length = reader.ReadSingle();
        OriginX = reader.ReadInt32();
        OriginY = reader.ReadInt32();
        Volume = reader.ReadSingle();

        BroadcastMessages = reader.ReadUndertaleObject<UndertaleSimpleList<Keyframe<BroadcastMessage>>>();

        Tracks = reader.ReadUndertaleObject<UndertaleSimpleList<Track>>();

        FunctionIDs = new Dictionary<int, UndertaleString>();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int key = reader.ReadInt32();
            FunctionIDs[key] = reader.ReadUndertaleString();
        }

        Moments = reader.ReadUndertaleObject<UndertaleSimpleList<Keyframe<Moment>>>();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 32;

        count += 1 + UndertaleSimpleList<Keyframe<BroadcastMessage>>.UnserializeChildObjectCount(reader);

        count += 1 + UndertaleSimpleList<Track>.UnserializeChildObjectCount(reader);

        int funcIDsCount = reader.ReadInt32();
        reader.Position += (uint)funcIDsCount * 8;

        count += 1 + UndertaleSimpleList<Keyframe<Moment>>.UnserializeChildObjectCount(reader);

        return count;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Sequence \"{Name.Content}\"";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        BroadcastMessages = null;
        Moments = null;
        Tracks = null;
        FunctionIDs = null;
    }

    public class Keyframe<T> : UndertaleObject where T : UndertaleObject, new()
    {
        public float Key { get; set; }
        public float Length { get; set; }
        public bool Stretch { get; set; }
        public bool Disabled { get; set; }
        public Dictionary<int, T> Channels { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Key);
            writer.Write(Length);
            writer.Write(Stretch);
            writer.Write(Disabled);
            writer.Write(Channels.Count);
            foreach (KeyValuePair<int, T> kvp in Channels)
            {
                writer.Write(kvp.Key);
                kvp.Value.Serialize(writer);
            }
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Key = reader.ReadSingle();
            Length = reader.ReadSingle();
            Stretch = reader.ReadBoolean();
            Disabled = reader.ReadBoolean();
            int count = reader.ReadInt32();
            Channels = new Dictionary<int, T>();
            for (int i = 0; i < count; i++)
            {
                int channel = reader.ReadInt32();
                T data = new T();
                data.Unserialize(reader);
                Channels[channel] = data;
            }
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            uint count = 0;

            reader.Position += 16;
            int chCount = reader.ReadInt32();

            Type t = typeof(T);
            if (t.IsAssignableTo(typeof(IStaticChildObjectsSize)))
            {
                uint subSize = reader.GetStaticChildObjectsSize(t);
                uint subCount = 0;

                if (t.IsAssignableTo(typeof(IStaticChildObjCount)))
                    subCount = reader.GetStaticChildCount(t);

                reader.Position += (uint)(chCount * 4 + chCount * subSize);

                return (uint)chCount * subCount;
            }

            var unserializeFunc = reader.GetUnserializeCountFunc(t);
            for (int i = 0; i < chCount; i++)
            {
                reader.Position += 4; // channel ID
                count += unserializeFunc(reader);
            }

            return count;
        }
    }

    public class BroadcastMessage : UndertaleObject
    {
        public UndertaleSimpleListString Messages;

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            Messages.Serialize(writer);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Messages = new UndertaleSimpleListString();
            Messages.Unserialize(reader);
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            return UndertaleSimpleListString.UnserializeChildObjectCount(reader);
        }
    }

    public class Moment : UndertaleObject
    {
        public int InternalCount; // Should be 0 if none, 1 if there's a message?
        public UndertaleString Event;

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(InternalCount);
            if (InternalCount > 0)
                writer.WriteUndertaleString(Event);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            InternalCount = reader.ReadInt32();
            if (InternalCount > 0)
                Event = reader.ReadUndertaleString();
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            int internalCount = reader.ReadInt32();
            if (internalCount > 0)
                reader.Position += 4; // "Event"

            return 0;
        }
    }

    public class Track : UndertaleObject
    {
        public enum TrackBuiltinName : int
        {
            Gain = 5,
            Pitch = 6,
            Falloff = 7,
            Rotation = 8,
            BlendAdd = 9,
            BlendMultiply = 10,
            Mask = 12,
            Subject = 13,
            Position = 14,
            Scale = 15,
            Origin = 16,
            ImageSpeed = 17,
            ImageIndex = 18,
            ImageAngle = Rotation,
            ImageBlend = BlendMultiply,
            FrameSize = 20,
            CharacterSpacing = 21,
            LineSpacing = 22,
            ParagraphSpacing = 23
        }

        [Flags]
        public enum TrackTraits : int
        {
            None,
            ChildrenIgnoreOrigin
        }

        public UndertaleString ModelName { get; set; } // Such as GMInstanceTrack, GMRealTrack, etc.
        public UndertaleString Name { get; set; } // An asset or property name
        public TrackBuiltinName BuiltinName { get; set; }
        public TrackTraits Traits { get; set; }
        public bool IsCreationTrack { get; set; }
        public List<int> Tags { get; set; }
        public List<Track> Tracks { get; set; } // Sub-tracks
        public ITrackKeyframes Keyframes { get; set; }
        public List<UndertaleResource> OwnedResources { get; set; }

        public UndertaleString GMAnimCurveString;

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(ModelName);
            writer.WriteUndertaleString(Name);
            writer.Write((int)BuiltinName);
            writer.Write((int)Traits);
            writer.Write(IsCreationTrack);

            writer.Write(Tags.Count);
            writer.Write(OwnedResources.Count);
            writer.Write(Tracks.Count);

            foreach (int i in Tags)
                writer.Write(i);

            foreach (UndertaleResource res in OwnedResources)
            {
                if (res is UndertaleAnimationCurve)
                {
                    writer.WriteUndertaleString(GMAnimCurveString);
                    res.Serialize(writer);
                }
                else
                    throw new IOException("Expected an animation curve");
            }

            foreach (Track t in Tracks)
                writer.WriteUndertaleObject(t);

            // Now, handle specific keyframe/etc. data
            switch (ModelName.Content)
            {
                case "GMAudioTrack":
                    writer.WriteUndertaleObject(Keyframes as AudioKeyframes);
                    break;
                case "GMInstanceTrack":
                    writer.WriteUndertaleObject(Keyframes as InstanceKeyframes);
                    break;
                case "GMGraphicTrack":
                    writer.WriteUndertaleObject(Keyframes as GraphicKeyframes);
                    break;
                case "GMSequenceTrack":
                    writer.WriteUndertaleObject(Keyframes as SequenceKeyframes);
                    break;
                case "GMSpriteFramesTrack":
                    writer.WriteUndertaleObject(Keyframes as SpriteFramesKeyframes);
                    break;
                case "GMAssetTrack": // TODO?
                    throw new NotImplementedException("GMAssetTrack not implemented, report this");
                case "GMBoolTrack":
                    writer.WriteUndertaleObject(Keyframes as BoolKeyframes);
                    break;
                case "GMStringTrack":
                    writer.WriteUndertaleObject(Keyframes as StringKeyframes);
                    break;
                // TODO?
                //case "GMIntTrack":
                //  writer.WriteUndertaleObject(Keyframes as IntKeyframes);
                //  break;
                case "GMRealTrack":
                case "GMColourTrack":
                    writer.WriteUndertaleObject(Keyframes as RealKeyframes);
                    break;
                case "GMTextTrack":     // Introduced in GM 2022.2
                    writer.WriteUndertaleObject(Keyframes as TextKeyframes);
                    break;
                case "GMParticleTrack": // Introduced in GM 2023.2
                    writer.WriteUndertaleObject(Keyframes as ParticleKeyframes);
                    break;
            }
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            // This reads the string content immediately, if necessary (which it should be)
            UndertaleString ForceReadString()
            {
                UndertaleString res = reader.ReadUndertaleString();
                if (res.Content is not null)
                    return res;

                reader.SwitchReaderType(false);
                long returnTo = reader.Position;
                reader.Position = reader.GetOffsetMapRev()[res];
                reader.ReadUndertaleObject<UndertaleString>();
                reader.Position = returnTo;
                reader.SwitchReaderType(true);

                return res;
            }

            ModelName = ForceReadString();
            Name = reader.ReadUndertaleString();
            BuiltinName = (TrackBuiltinName)reader.ReadInt32();
            Traits = (TrackTraits)reader.ReadInt32();
            IsCreationTrack = reader.ReadBoolean();

            int tagCount = reader.ReadInt32();
            int ownedResCount = reader.ReadInt32();
            int trackCount = reader.ReadInt32();

            Tags = new List<int>(tagCount);
            for (int i = 0; i < tagCount; i++)
                Tags.Add(reader.ReadInt32());

            OwnedResources = new List<UndertaleResource>(ownedResCount);
            for (int i = 0; i < ownedResCount; i++)
            {
                GMAnimCurveString = ForceReadString();
                if (GMAnimCurveString.Content != "GMAnimCurve")
                    throw new IOException("Expected GMAnimCurve");
                UndertaleAnimationCurve res = new UndertaleAnimationCurve();
                res.Unserialize(reader);
                OwnedResources.Add(res);
            }

            Tracks = new List<Track>(trackCount);
            for (int i = 0; i < trackCount; i++)
                Tracks.Add(reader.ReadUndertaleObject<Track>());

            // Now, handle specific keyframe/etc. data
            switch (ModelName.Content)
            {
                case "GMAudioTrack":
                    Keyframes = reader.ReadUndertaleObject<AudioKeyframes>();
                    break;
                case "GMInstanceTrack":
                    Keyframes = reader.ReadUndertaleObject<InstanceKeyframes>();
                    break;
                case "GMGraphicTrack":
                    Keyframes = reader.ReadUndertaleObject<GraphicKeyframes>();
                    break;
                case "GMSequenceTrack":
                    Keyframes = reader.ReadUndertaleObject<SequenceKeyframes>();
                    break;
                case "GMSpriteFramesTrack":
                    Keyframes = reader.ReadUndertaleObject<SpriteFramesKeyframes>();
                    break;
                case "GMAssetTrack": // TODO?
                    throw new NotImplementedException("GMAssetTrack not implemented, report this");
                case "GMBoolTrack":
                    Keyframes = reader.ReadUndertaleObject<BoolKeyframes>();
                    break;
                case "GMStringTrack":
                    Keyframes = reader.ReadUndertaleObject<StringKeyframes>();
                    break;
                // TODO?
                //case "GMIntTrack":
                //  Keyframes = reader.ReadUndertaleObject<IntKeyframes>();
                //  break;
                case "GMRealTrack":
                case "GMColourTrack":
                    Keyframes = reader.ReadUndertaleObject<RealKeyframes>();
                    break;
                case "GMTextTrack":     // Introduced in GM 2022.2
                    Keyframes = reader.ReadUndertaleObject<TextKeyframes>();
                    break;
                case "GMParticleTrack": // Introduced in GM 2023.2
                    Keyframes = reader.ReadUndertaleObject<ParticleKeyframes>();
                    break;

                case "GMGroupTrack":
                    throw new NotImplementedException("GMGroupTrack not implemented, report this");
                case "GMClipMaskTrack":
                    throw new NotImplementedException("GMClipMaskTrack not implemented, report this");
            }
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            uint count = 0;

            string ForceReadString()
            {
                uint strPtr = reader.ReadUInt32();

                reader.SwitchReaderType(false);
                long returnTo = reader.Position;
                reader.Position = strPtr - 4;
                string res = reader.ReadGMString();
                reader.Position = returnTo;
                reader.SwitchReaderType(true);

                return res;
            }

            string modelName = ForceReadString();
            reader.Position += 16;

            int tagCount = reader.ReadInt32();
            int ownedResCount = reader.ReadInt32();
            int trackCount = reader.ReadInt32();

            reader.Position += (uint)tagCount * 4; // "Tags"

            for (int i = 0; i < ownedResCount; i++)
            {
                reader.Position += 4; // "GMAnimCurveString"
                count += UndertaleAnimationCurve.UnserializeChildObjectCount(reader);
            }

            // "Tracks"
            for (int i = 0; i < trackCount; i++)
                count += 1 + UnserializeChildObjectCount(reader);

            switch (modelName)
            {
                case "GMAudioTrack":
                    count += 1 + AudioKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMInstanceTrack":
                    count += 1 + InstanceKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMGraphicTrack":
                    count += 1 + GraphicKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMSequenceTrack":
                    count += 1 + SequenceKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMSpriteFramesTrack":
                    count += 1 + SpriteFramesKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMAssetTrack": // TODO?
                    throw new NotImplementedException("GMAssetTrack not implemented, report this");
                case "GMBoolTrack":
                    count += 1 + BoolKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMStringTrack":
                    count += 1 + StringKeyframes.UnserializeChildObjectCount(reader);
                    break;
                // TODO?
                //case "GMIntTrack":
                //  count += 1 + IntKeyframes.UnserializeChildObjectCount(reader);
                //  break;
                case "GMRealTrack":
                case "GMColourTrack":
                    count += 1 + RealKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMTextTrack":     // Introduced in GM 2022.2
                    count += 1 + TextKeyframes.UnserializeChildObjectCount(reader);
                    break;
                case "GMParticleTrack": // Introduced in GM 2023.2
                    count += 1 + ParticleKeyframes.UnserializeChildObjectCount(reader);
                    break;

                case "GMGroupTrack":
                    throw new NotImplementedException("GMGroupTrack not implemented, report this");
                case "GMClipMaskTrack":
                    throw new NotImplementedException("GMClipMaskTrack not implemented, report this");
            }

            return count;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (BuiltinName != 0)
            {
                if (ModelName.Content == "GMColourTrack")
                    return $"Sequence sub-track - \"Color\" (mode \"{BuiltinName}\")";
                else
                    return $"Sequence sub-track - \"{BuiltinName}\"";
            }
            else
                return $"Sequence track \"{Name.Content}\"";
        }
    }

    /// Here begins all of the keyframe data classes. Some generics used to shorten sections, but some verbosity maintained

    public interface ITrackKeyframes : UndertaleObject
    {
    }
    public class TrackKeyframes<T> : ITrackKeyframes where T : UndertaleObject, new()
    {
        public UndertaleSimpleList<Keyframe<T>> List;

        /// <inheritdoc />
        public virtual void Serialize(UndertaleWriter writer)
        {
            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            List.Serialize(writer);
        }

        /// <inheritdoc />
        public virtual void Unserialize(UndertaleReader reader)
        {
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            List = new UndertaleSimpleList<Keyframe<T>>();
            List.Unserialize(reader);
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            while (reader.AbsPosition % 4 != 0)
                reader.Position++;

            return UndertaleSimpleList<Keyframe<T>>.UnserializeChildObjectCount(reader);
        }
    }

    public class ResourceData<T> : UndertaleObject where T : UndertaleObject, new()
    {
        public T Resource { get; set; }

        /// <inheritdoc />
        public virtual void Serialize(UndertaleWriter writer)
        {
            Resource.Serialize(writer);
        }

        /// <inheritdoc />
        public virtual void Unserialize(UndertaleReader reader)
        {
            Resource = new T();
            Resource.Unserialize(reader);
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            // At this moment, T could be only "UndertaleResourceById<>".
            // If that changes, you should replace the contents with the following:
            // return reader.GetChildObjectCount<T>();

            reader.Position += 4;
            return 0;
        }
    }

    public class SimpleIntData : IStaticChildObjectsSize, UndertaleObject
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 4;

        public int Value { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Value);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Value = reader.ReadInt32();
        }
    }

    public class AudioKeyframes : TrackKeyframes<AudioKeyframes.Data>
    {
        public class Data : ResourceData<UndertaleResourceById<UndertaleSound, UndertaleChunkSOND>>
        {
            public int Mode { get; set; }

            /// <inheritdoc />
            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                writer.Write((int)0);
                writer.Write(Mode);
            }

            /// <inheritdoc />
            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                if (reader.ReadUInt32() != 0)
                    throw new IOException("Expected 0 in Audio keyframe");
                Mode = reader.ReadInt32();
            }

            /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
            public static new uint UnserializeChildObjectCount(UndertaleReader reader)
            {
                uint count = ResourceData<UndertaleResourceById<UndertaleSound, UndertaleChunkSOND>>.UnserializeChildObjectCount(reader);

                reader.Position += 8;

                return count;
            }
        }
    }

    public class InstanceKeyframes : TrackKeyframes<InstanceKeyframes.Data>
    {
        public class Data : ResourceData<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>> { }
    }

    public class GraphicKeyframes : TrackKeyframes<GraphicKeyframes.Data>
    {
        public class Data : ResourceData<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>> { }
    }

    public class SequenceKeyframes : TrackKeyframes<SequenceKeyframes.Data>
    {
        public class Data : ResourceData<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>> { }
    }

    public class SpriteFramesKeyframes : TrackKeyframes<SpriteFramesKeyframes.Data>
    {
        public class Data : SimpleIntData { }
    }

    public class BoolKeyframes : TrackKeyframes<BoolKeyframes.Data>
    {
        public class Data : SimpleIntData { }
    }

    public class StringKeyframes : TrackKeyframes<StringKeyframes.Data>
    {
        public class Data : UndertaleObject, IStaticChildObjectsSize
        {
            /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
            public static readonly uint ChildObjectsSize = 4;

            public UndertaleString Value { get; set; }

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Value);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                Value = reader.ReadUndertaleString();
            }
        }
    }

    public class CurveData : UndertaleObject
    {
        public bool IsCurveEmbedded { get; set; }
        public UndertaleAnimationCurve EmbeddedAnimCurve { get; set; }
        public UndertaleResourceById<UndertaleAnimationCurve, UndertaleChunkACRV> AssetAnimCurve { get; set; }

        /// <inheritdoc />
        public virtual void Serialize(UndertaleWriter writer)
        {
            writer.Write(IsCurveEmbedded);
            if (IsCurveEmbedded)
            {
                writer.Write(-1);
                EmbeddedAnimCurve.Serialize(writer, false);
            }
            else
            {
                AssetAnimCurve.Serialize(writer);
            }
        }

        /// <inheritdoc />
        public virtual void Unserialize(UndertaleReader reader)
        {
            if (reader.ReadBoolean())
            {
                // The curve data is embedded in this sequence, right here
                IsCurveEmbedded = true;
                if (reader.ReadInt32() != -1)
                    throw new IOException("Expected -1");
                EmbeddedAnimCurve = new UndertaleAnimationCurve();
                EmbeddedAnimCurve.Unserialize(reader, false);
            }
            else
            {
                // The curve data is an asset in the project
                IsCurveEmbedded = false;
                AssetAnimCurve = new UndertaleResourceById<UndertaleAnimationCurve, UndertaleChunkACRV>();
                AssetAnimCurve.Unserialize(reader);
            }
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            uint count = 0;
            
            // "IsCurveEmbedded"
            if (reader.ReadBoolean())
            {
                reader.Position += 4;

                count += UndertaleAnimationCurve.UnserializeChildObjectCount(reader, false);
            }
            else
                reader.Position += 4;

            return count;
        }
    }

    public class IntData : CurveData
    {
        public int Value { get; set; }

        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
        {
            writer.Write(Value);
            base.Serialize(writer);
        }

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            Value = reader.ReadInt32();
            base.Unserialize(reader);
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static new uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            reader.Position += 4; // "Value"

            return CurveData.UnserializeChildObjectCount(reader);
        }
    }

    public class IntKeyframes : TrackKeyframes<IntData>
    {
        public int Interpolation;

        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
        {
            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write(Interpolation);

            List.Serialize(writer);
        }

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            Interpolation = reader.ReadInt32();

            List = new UndertaleSimpleList<Keyframe<IntData>>();
            List.Unserialize(reader);
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static new uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            while (reader.AbsPosition % 4 != 0)
                reader.Position++;

            reader.Position += 4; // "Interpolation"

            return UndertaleSimpleList<Keyframe<IntData>>.UnserializeChildObjectCount(reader);
        }
    }

    public class RealData : CurveData
    {
        public float Value { get; set; }

        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
        {
            writer.Write(Value);
            base.Serialize(writer);
        }

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            Value = reader.ReadSingle();
            base.Unserialize(reader);
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static new uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            reader.Position += 4; // "Value"

            return CurveData.UnserializeChildObjectCount(reader);
        }
    }

    public class RealKeyframes : TrackKeyframes<RealData>
    {
        public int Interpolation;

        /// <inheritdoc />
        public override void Serialize(UndertaleWriter writer)
        {
            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write(Interpolation);

            List.Serialize(writer);
        }

        /// <inheritdoc />
        public override void Unserialize(UndertaleReader reader)
        {
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            Interpolation = reader.ReadInt32();

            List = new UndertaleSimpleList<Keyframe<RealData>>();
            List.Unserialize(reader);
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static new uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            while (reader.AbsPosition % 4 != 0)
                reader.Position++;

            reader.Position += 4; // "Interpolation"

            return UndertaleSimpleList<Keyframe<RealData>>.UnserializeChildObjectCount(reader);
        }
    }
    
    public class TextKeyframes : TrackKeyframes<TextKeyframes.Data>
    {
        // Source - https://github.com/YoYoGames/GameMaker-HTML5/blob/develop/scripts/yySequence.js#L2227
        // ("yyTextTrackKey")
        public class Data : UndertaleObject, IStaticChildObjectsSize
        {
            /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
            public static readonly uint ChildObjectsSize = 16;

            private int _alignment;

            public UndertaleString Text { get; set; }
            public bool Wrap { get; set; }
            public int AlignmentV
            {
                get => (_alignment >> 8) & 0xff;
                set => _alignment = (_alignment & 0xff) | (value & 0xff) << 8;
            }
            public int AlignmentH
            {
                get => _alignment & 0xff;
                set => _alignment = (_alignment & ~0xff) | (value & 0xff);
            }
            public int FontIndex { get; set; }

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Text);
                writer.Write(Wrap);
                writer.Write(_alignment);
                writer.Write(FontIndex);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                Text = reader.ReadUndertaleString();
                Wrap = reader.ReadBoolean();
                _alignment = reader.ReadInt32();
                FontIndex = reader.ReadInt32();
            }
        }
    }

    public class ParticleKeyframes : TrackKeyframes<ParticleKeyframes.Data>
    {
        // A temporary implementation, its type should be "ResourceData<UndertaleResourceById<...>>"
        public class Data : UndertaleObject, IStaticChildObjectsSize
        {
            /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
            public static readonly uint ChildObjectsSize = 4;

            public int ParticleSystemIndex { get; set; }

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(ParticleSystemIndex);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                ParticleSystemIndex = reader.ReadInt32();
            }
        }
    }
}
