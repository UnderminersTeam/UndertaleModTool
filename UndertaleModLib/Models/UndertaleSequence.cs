using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: INotifyPropertyChanged
    public class UndertaleSequence : UndertaleNamedResource
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

        public class Keyframe<T> : UndertaleObject where T : UndertaleObject, new()
        {
            public float Key { get; set; }
            public float Length { get; set; }
            public bool Stretch { get; set; }
            public bool Disabled { get; set; }
            public Dictionary<int, T> Channels { get; set; }

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
        }

        public class BroadcastMessage : UndertaleObject
        {
            public UndertaleSimpleListString Messages;

            public void Serialize(UndertaleWriter writer)
            {
                Messages.Serialize(writer);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Messages = new UndertaleSimpleListString();
                Messages.Unserialize(reader);
            }
        }
        
        public class Moment : UndertaleObject
        {
            public int InternalCount; // Should be 0 if none, 1 if there's a message?
            public UndertaleString Event;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(InternalCount);
                if (InternalCount > 0)
                    Event.Serialize(writer);
            }

            public void Unserialize(UndertaleReader reader)
            {
                InternalCount = reader.ReadInt32();
                if (InternalCount > 0)
                    Event = reader.ReadUndertaleString();
            }
        }

        public class Track : UndertaleObject
        {
            public UndertaleString ModelName { get; set; } // Such as GMInstanceTrack, GMRealTrack, etc.
            public UndertaleString Name { get; set; } // An asset or property name
            public int BuiltinName { get; set; }
            public int Traits { get; set; }
            public bool IsCreationTrack { get; set; }
            public List<int> Tags { get; set; }
            public List<Track> Tracks { get; set; } // Sub-tracks
            public TrackKeyframes Keyframes { get; set; }
            public List<UndertaleResource> OwnedResources { get; set; }

            public UndertaleString GMAnimCurveString;

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(ModelName);
                writer.WriteUndertaleString(Name);
                writer.Write(BuiltinName);
                writer.Write(Traits);
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
                        writer.WriteUndertaleObject(Keyframes as RealKeyframes);
                        break;
                }
            }

            public void Unserialize(UndertaleReader reader)
            {
                // This reads the string content immediately, if necessary (which it should be)
                UndertaleString ForceReadString()
                {
                    UndertaleString res = reader.ReadUndertaleString();
                    uint returnTo = reader.Position;
                    reader.Position = reader.GetOffsetMapRev()[res];
                    reader.ReadUndertaleObject<UndertaleString>();
                    reader.Position = returnTo;
                    return res;
                }

                ModelName = ForceReadString();
                Name = reader.ReadUndertaleString();
                BuiltinName = reader.ReadInt32();
                Traits = reader.ReadInt32();
                IsCreationTrack = reader.ReadBoolean();

                int tagCount = reader.ReadInt32();
                int ownedResCount = reader.ReadInt32();
                int trackCount = reader.ReadInt32();

                Tags = new List<int>();
                for (int i = 0; i < tagCount; i++)
                    Tags.Add(reader.ReadInt32());

                OwnedResources = new List<UndertaleResource>();
                for (int i = 0; i < ownedResCount; i++)
                {
                    GMAnimCurveString = ForceReadString();
                    if (GMAnimCurveString.Content != "GMAnimCurve")
                        throw new IOException("Expected GMAnimCurve");
                    UndertaleAnimationCurve res = new UndertaleAnimationCurve();
                    res.Unserialize(reader);
                    OwnedResources.Add(res);
                }

                Tracks = new List<Track>();
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
                        Keyframes = reader.ReadUndertaleObject<RealKeyframes>();
                        break;
                }
            }
        }

        /// Here begins all of the keyframe data classes. Some generics used to shorten sections, but some verbosity maintained

        public class TrackKeyframes : UndertaleObject
        {
            public virtual void Serialize(UndertaleWriter writer)
            {
                while (writer.Position % 4 != 0)
                    writer.Write((byte)0);
            }

            public virtual void Unserialize(UndertaleReader reader)
            {
                while (reader.Position % 4 != 0)
                    if (reader.ReadByte() != 0)
                        throw new IOException("Padding error!");
            }
        }

        public class ResourceData<T> : UndertaleObject where T : UndertaleObject, new()
        {
            public T Resource { get; set; }

            public virtual void Serialize(UndertaleWriter writer)
            {
                Resource.Serialize(writer);
            }

            public virtual void Unserialize(UndertaleReader reader)
            {
                Resource = new T();
                Resource.Unserialize(reader);
            }
        }

        public class AudioKeyframes : TrackKeyframes
        {
            public class Data : ResourceData<UndertaleResourceById<UndertaleSound, UndertaleChunkSOND>>
            {
                public int Mode { get; set; }

                public override void Serialize(UndertaleWriter writer)
                {
                    base.Serialize(writer);
                    writer.Write((int)0);
                    writer.Write(Mode);
                }

                public override void Unserialize(UndertaleReader reader)
                {
                    base.Unserialize(reader);
                    if (reader.ReadUInt32() != 0)
                        throw new IOException("Expected 0 in Audio keyframe");
                    Mode = reader.ReadInt32();
                }
            }

            public UndertaleSimpleList<Keyframe<Data>> List;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                List = new UndertaleSimpleList<Keyframe<Data>>();
                List.Unserialize(reader);
            }
        }

        public class InstanceKeyframes : TrackKeyframes
        {
            public class Data : ResourceData<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>> { }
            public UndertaleSimpleList<Keyframe<Data>> List;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                List = new UndertaleSimpleList<Keyframe<Data>>();
                List.Unserialize(reader);
            }
        }

        public class GraphicKeyframes : TrackKeyframes
        {
            public class Data : ResourceData<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>> { }
            public UndertaleSimpleList<Keyframe<Data>> List;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                List = new UndertaleSimpleList<Keyframe<Data>>();
                List.Unserialize(reader);
            }
        }

        public class SequenceKeyframes : TrackKeyframes
        {
            public class Data : ResourceData<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>> { }
            public UndertaleSimpleList<Keyframe<Data>> List;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                List = new UndertaleSimpleList<Keyframe<Data>>();
                List.Unserialize(reader);
            }
        }

        public class SpriteFramesData : UndertaleObject
        {
            public int Value { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Value);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Value = reader.ReadInt32();
            }
        }

        public class SpriteFramesKeyframes : TrackKeyframes
        {
            public UndertaleSimpleList<Keyframe<SpriteFramesData>> List;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                List = new UndertaleSimpleList<Keyframe<SpriteFramesData>>();
                List.Unserialize(reader);
            }
        }

        public class BoolData : UndertaleObject
        {
            public int Value { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Value);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Value = reader.ReadInt32();
            }
        }

        public class BoolKeyframes : TrackKeyframes
        {
            public UndertaleSimpleList<Keyframe<BoolData>> List;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                List = new UndertaleSimpleList<Keyframe<BoolData>>();
                List.Unserialize(reader);
            }
        }

        public class StringData : UndertaleObject
        {
            public UndertaleString Value { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Value);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Value = reader.ReadUndertaleString();
            }
        }

        public class StringKeyframes : TrackKeyframes
        {
            public UndertaleSimpleList<Keyframe<StringData>> List;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);
                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);
                List = new UndertaleSimpleList<Keyframe<StringData>>();
                List.Unserialize(reader);
            }
        }

        public class CurveData : UndertaleObject
        {
            public bool IsCurveEmbedded { get; set; }
            public UndertaleAnimationCurve EmbeddedAnimCurve { get; set; }
            public UndertaleResourceById<UndertaleAnimationCurve, UndertaleChunkACRV> AssetAnimCurve { get; set; }
            
            public virtual void Serialize(UndertaleWriter writer)
            {
                writer.Write(IsCurveEmbedded);
                if (IsCurveEmbedded)
                {
                    writer.Write(-1);
                    writer.WriteUndertaleObject(EmbeddedAnimCurve);
                }
                else
                {
                    AssetAnimCurve.Serialize(writer);
                }
            }

            public virtual void Unserialize(UndertaleReader reader)
            {
                if (reader.ReadBoolean())
                {
                    // The curve data is embedded in this sequence, right here
                    IsCurveEmbedded = true;
                    if (reader.ReadInt32() != -1)
                        throw new IOException("Expected -1");
                    EmbeddedAnimCurve = reader.ReadUndertaleObject<UndertaleAnimationCurve>();
                }
                else
                {
                    // The curve data is an asset in the project
                    IsCurveEmbedded = false;
                    AssetAnimCurve = new UndertaleResourceById<UndertaleAnimationCurve, UndertaleChunkACRV>();
                    AssetAnimCurve.Unserialize(reader);
                }
            }
        }

        public class IntData : CurveData
        {
            public int Value { get; set; }

            public override void Serialize(UndertaleWriter writer)
            {
                writer.Write(Value);
                base.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                Value = reader.ReadInt32();
                base.Unserialize(reader);
            }
        }

        public class IntKeyframes : TrackKeyframes
        {
            public UndertaleSimpleList<Keyframe<IntData>> List;
            public int Interpolation;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);

                writer.Write(Interpolation);

                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);

                Interpolation = reader.ReadInt32();

                List = new UndertaleSimpleList<Keyframe<IntData>>();
                List.Unserialize(reader);
            }
        }

        public class RealData : CurveData
        {
            public float Value { get; set; }

            public override void Serialize(UndertaleWriter writer)
            {
                writer.Write(Value);
                base.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                Value = reader.ReadSingle();
                base.Unserialize(reader);
            }
        }

        public class RealKeyframes : TrackKeyframes
        {
            public UndertaleSimpleList<Keyframe<RealData>> List;
            public int Interpolation;

            public override void Serialize(UndertaleWriter writer)
            {
                base.Serialize(writer);

                writer.Write(Interpolation);

                List.Serialize(writer);
            }

            public override void Unserialize(UndertaleReader reader)
            {
                base.Unserialize(reader);

                Interpolation = reader.ReadInt32();

                List = new UndertaleSimpleList<Keyframe<RealData>>();
                List.Unserialize(reader);
            }
        }

    }
}
