using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;
using UndertaleModLib.Project.Json;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleSequence"/>.
/// </summary>
internal sealed class SerializableSequence : ISerializableProjectAsset
{
    /// <inheritdoc cref="UndertaleSequence.PlaybackType"/>
    public enum PlaybackType : uint
    {
        Oneshot = 0,
        Loop = 1,
        Pingpong = 2
    }

    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Sequence;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => false;

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    /// <inheritdoc cref="UndertaleSequence.Playback"/>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PlaybackType Playback { get; set; }

    /// <inheritdoc cref="UndertaleSequence.PlaybackSpeed"/>
    public float PlaybackSpeed { get; set; }

    /// <inheritdoc cref="UndertaleSequence.PlaybackSpeedType"/>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SerializableSprite.ExtraAnimSpeedType PlaybackSpeedType { get; set; }

    /// <inheritdoc cref="UndertaleSequence.Length"/>
    public float Length { get; set; }

    /// <inheritdoc cref="UndertaleSequence.OriginX"/>
    public int OriginX { get; set; }

    /// <inheritdoc cref="UndertaleSequence.OriginY"/>
    public int OriginY { get; set; }

    /// <inheritdoc cref="UndertaleSequence.UndertaleSequence"/>
    public float Volume { get; set; }

    /// <inheritdoc cref="UndertaleSequence.Width"/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Width { get; set; }

    /// <inheritdoc cref="UndertaleSequence.Height"/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float Height { get; set; }

    /// <inheritdoc cref="UndertaleSequence.BroadcastMessages"/>
    public List<Keyframe<List<string>>> BroadcastMessages { get; set; }

    /// <inheritdoc cref="UndertaleSequence.Moments"/>
    public List<Keyframe<List<string>>> Moments { get; set; }

    /// <inheritdoc cref="UndertaleSequence.Tracks"/>
    public List<Track> Tracks { get; set; }

    /// <inheritdoc cref="UndertaleSequence.FunctionIDs"/>
    public List<FunctionIDEntry> FunctionIDs { get; set; }

    // TODO: inherited documentation for this, once changes from sequences PR are merged in
    public sealed class FunctionIDEntry
    {
        public int ID { get; set; }
        public string Function { get; set; }
    }

    /// <inheritdoc cref="UndertaleSequence.Keyframe{T}"/>
    public sealed class Keyframe<T>
    {
        /// <inheritdoc cref="UndertaleSequence.Keyframe{T}.Key"/>
        public float Key { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Keyframe{T}.Length"/>
        public float Length { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Keyframe{T}.Stretch"/>
        public bool Stretch { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Keyframe{T}.Disabled"/>
        public bool Disabled { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Keyframe{T}.Channels"/>
        public List<KeyframeChannel<T>> Channels { get; set; }
    }

    // TODO: inherited documentation for this, once changes from sequences PR are merged in
    public sealed class KeyframeChannel<T>
    {
        public int Channel { get; set; }
        public T Value { get; set; }
    }

    public sealed class Track
    {
        /// <inheritdoc cref="UndertaleSequence.Track.ModelName"/>
        public string ModelName { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.Name"/>
        public string Name { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.BuiltinName"/>
        public int BuiltinName { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.Traits"/>
        public int Traits { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.IsCreationTrack"/>
        public bool IsCreationTrack { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.Tags"/>
        public List<int> Tags { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.Tracks"/>
        public List<Track> Tracks { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.Keyframes"/>
        [JsonConverter(typeof(NoPrettyPrintJsonConverter<ITrackKeyframeStore>))]
        public ITrackKeyframeStore KeyframeStore { get; set; }

        /// <inheritdoc cref="UndertaleSequence.Track.OwnedResources"/>
        public List<ISerializableProjectAsset> OwnedResources { get; set; }
    }

    // TODO: Proper keyframe type for colors
    /// <inheritdoc cref="UndertaleSequence.ITrackKeyframes"/>
    [JsonDerivedType(typeof(AudioKeyframes), "AudioKeyframes")]
    [JsonDerivedType(typeof(InstanceKeyframes), "InstanceKeyframes")]
    [JsonDerivedType(typeof(GraphicKeyframes), "GraphicKeyframes")]
    [JsonDerivedType(typeof(SequenceKeyframes), "SequenceKeyframes")]
    [JsonDerivedType(typeof(SpriteFramesKeyframes), "SpriteFramesKeyframes")]
    [JsonDerivedType(typeof(BoolKeyframes), "BoolKeyframes")]
    [JsonDerivedType(typeof(StringKeyframes), "StringKeyframes")]
    [JsonDerivedType(typeof(RealKeyframes), "RealKeyframes")]
    [JsonDerivedType(typeof(TextKeyframes), "TextKeyframes")]
    [JsonDerivedType(typeof(ParticleKeyframes), "ParticleKeyframes")]
    public interface ITrackKeyframeStore
    {
    }

    /// <inheritdoc cref="UndertaleSequence.AudioKeyframes"/>
    public sealed class AudioKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<AudioKeyframe>> Keyframes { get; set; }

        public sealed class AudioKeyframe
        {
            public string SoundAsset { get; set; }
            public int Mode { get; set; }
        }
    }

    /// <inheritdoc cref="UndertaleSequence.InstanceKeyframes"/>
    public sealed class InstanceKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<string>> Keyframes { get; set; }
    }

    /// <inheritdoc cref="UndertaleSequence.GraphicKeyframes"/>
    public sealed class GraphicKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<string>> Keyframes { get; set; }
    }

    /// <inheritdoc cref="UndertaleSequence.SequenceKeyframes"/>
    public sealed class SequenceKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<string>> Keyframes { get; set; }
    }

    /// <inheritdoc cref="UndertaleSequence.SpriteFramesKeyframes"/>
    public sealed class SpriteFramesKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<int>> Keyframes { get; set; }
    }

    /// <inheritdoc cref="UndertaleSequence.BoolKeyframes"/>
    public sealed class BoolKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<int>> Keyframes { get; set; }
    }

    /// <inheritdoc cref="UndertaleSequence.StringKeyframes"/>
    public sealed class StringKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<string>> Keyframes { get; set; }
    }

    /// <inheritdoc cref="UndertaleSequence.RealKeyframes"/>
    public sealed class RealKeyframes : ITrackKeyframeStore
    {
        public int Interpolation { get; set; }
        public List<Keyframe<RealKeyframe>> Keyframes { get; set; }

        public sealed class RealKeyframe
        {
            public SerializableAnimationCurve AnimCurveEmbedded { get; set; }
            public string AnimCurveAsset { get; set; }
            public float Value { get; set; }
        }
    }

    /// <inheritdoc cref="UndertaleSequence.TextKeyframes"/>
    public sealed class TextKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<TextKeyframe>> Keyframes { get; set; }

        public sealed class TextKeyframe
        {
            public string Text { get; set; }
            public bool Wrap { get; set; }
            public int AlignmentV { get; set; }
            public int AlignmentH { get; set; }
            public string FontAsset { get; set; }
        }
    }

    /// <inheritdoc cref="UndertaleSequence.ParticleKeyframes"/>
    public sealed class ParticleKeyframes : ITrackKeyframeStore
    {
        public List<Keyframe<string>> Keyframes { get; set; }
    }

    // Data asset that was located during pre-import.
    private UndertaleSequence _dataAsset = null;

    /// <summary>
    /// Populates this serializable sequence with data from an actual sequence.
    /// </summary>
    internal void PopulateFromData(ProjectContext projectContext, UndertaleSequence seq)
    {
        // Update all main properties
        DataName = seq.Name.Content;
        Playback = (PlaybackType)seq.Playback;
        PlaybackSpeed = seq.PlaybackSpeed;
        PlaybackSpeedType = (SerializableSprite.ExtraAnimSpeedType)seq.PlaybackSpeedType;
        Length = seq.Length;
        OriginX = seq.OriginX;
        OriginY = seq.OriginY;
        Volume = seq.Volume;
        Width = seq.Width;
        Height = seq.Height;

        // Update broadcast messages
        BroadcastMessages = [.. seq.BroadcastMessages.Select((keyframe) => new Keyframe<List<string>>()
        {
            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<List<string>>()
            {
                Channel = kvp.Channel,
                Value = [.. kvp.Value.Messages.Select((message) => message?.Content)]
            })]
        })];

        // Update moments
        Moments = [.. seq.Moments.Select((keyframe) => new Keyframe<List<string>>()
        {
            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<List<string>>()
            {
                Channel = kvp.Channel,
                Value = kvp.Value.Events.Select(e => e?.Content).ToList()
            })]
        })];

        // Update tracks
        Func<UndertaleSequence.Track, Track> convertTrack = null;
        convertTrack = (track) => new Track()
        {
            ModelName = track.ModelName?.Content,
            Name = track.Name?.Content,
            BuiltinName = (int)track.BuiltinName,
            Traits = (int)track.Traits,
            IsCreationTrack = track.IsCreationTrack,
            Tags = [.. track.Tags],
            Tracks = [.. track.Tracks.Select(convertTrack)],
            KeyframeStore = track.Keyframes switch
            {
                UndertaleSequence.TrackKeyframes<UndertaleSequence.AudioKeyframes.Data> keyframeStore =>
                    new AudioKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<AudioKeyframes.AudioKeyframe>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<AudioKeyframes.AudioKeyframe>()
                            {
                                Channel = kvp.Channel,
                                Value = new AudioKeyframes.AudioKeyframe()
                                {
                                    Mode = kvp.Value.Mode,
                                    SoundAsset = kvp.Value.Resource?.Resource?.Name?.Content
                                }
                            })]
                        })]
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.InstanceKeyframes.Data> keyframeStore =>
                    new InstanceKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<string>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<string>()
                            {
                                Channel = kvp.Channel,
                                Value = kvp.Value.Resource?.Resource?.Name?.Content
                            })]
                        })]
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.GraphicKeyframes.Data> keyframeStore =>
                    new GraphicKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<string>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<string>()
                            {
                                Channel = kvp.Channel,
                                Value = kvp.Value.Resource?.Resource?.Name?.Content
                            })]
                        })]
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.SequenceKeyframes.Data> keyframeStore =>
                    new SequenceKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<string>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<string>()
                            {
                                Channel = kvp.Channel,
                                Value = kvp.Value.Resource?.Resource?.Name?.Content
                            })]
                        })]
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.SpriteFramesKeyframes.Data> keyframeStore =>
                    new SpriteFramesKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<int>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<int>()
                            {
                                Channel = kvp.Channel,
                                Value = kvp.Value.Value
                            })]
                        })]
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.BoolKeyframes.Data> keyframeStore =>
                    new BoolKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<int>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<int>()
                            {
                                Channel = kvp.Channel,
                                Value = kvp.Value.Value
                            })]
                        })]
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.StringKeyframes.Data> keyframeStore =>
                    new StringKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<string>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<string>()
                            {
                                Channel = kvp.Channel,
                                Value = kvp.Value.Value?.Content
                            })]
                        })]
                    },
                UndertaleSequence.RealKeyframes keyframeStore =>
                    new RealKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<RealKeyframes.RealKeyframe>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<RealKeyframes.RealKeyframe>()
                            {
                                Channel = kvp.Channel,
                                Value = new RealKeyframes.RealKeyframe()
                                {
                                    Value = kvp.Value.Value,
                                    AnimCurveAsset = kvp.Value.AssetAnimCurve?.Resource?.Name?.Content,
                                    AnimCurveEmbedded = (SerializableAnimationCurve)kvp.Value.EmbeddedAnimCurve?.GenerateSerializableProjectAsset(projectContext)
                                }
                            })]
                        })],
                        Interpolation = keyframeStore.Interpolation
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.TextKeyframes.Data> keyframeStore =>
                    new TextKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<TextKeyframes.TextKeyframe>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<TextKeyframes.TextKeyframe>()
                            {
                                Channel = kvp.Channel,
                                Value = new TextKeyframes.TextKeyframe()
                                {
                                    Text = kvp.Value.Text?.Content,
                                    Wrap = kvp.Value.Wrap,
                                    AlignmentV = kvp.Value.AlignmentV,
                                    AlignmentH = kvp.Value.AlignmentH,
                                    FontAsset = 
                                        (kvp.Value.FontIndex >= 0 && kvp.Value.FontIndex < projectContext.Data.Fonts.Count) ?
                                        projectContext.Data.Fonts[kvp.Value.FontIndex]?.Name?.Content :
                                        null
                                }
                            })]
                        })]
                    },
                UndertaleSequence.TrackKeyframes<UndertaleSequence.ParticleKeyframes.Data> keyframeStore =>
                    new ParticleKeyframes()
                    {
                        Keyframes = [.. keyframeStore.List.Select((keyframe) => new Keyframe<string>()
                        {
                            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                            Channels = [.. keyframe.Channels.Select((kvp) => new KeyframeChannel<string>()
                            {
                                Channel = kvp.Channel,
                                Value = kvp.Value.Resource?.Resource?.Name?.Content
                            })]
                        })]
                    },
                null => null,
                _ => throw new ProjectException($"Unknown keyframe store type {track.Keyframes.GetType().Name}")
            }, 
            OwnedResources = [.. track.OwnedResources.Select((resource) => resource switch
            {
                UndertaleAnimationCurve curve => (SerializableAnimationCurve)curve?.GenerateSerializableProjectAsset(projectContext),
                _ => throw new Exception($"Unknown owned resource type {resource.GetType().Name}")
            })]
        };
        Tracks = [.. seq.Tracks.Select(convertTrack)];

        // Update function IDs
        FunctionIDs = [.. seq.FunctionIDs.Select((entry) => new FunctionIDEntry()
        {
            ID = entry.ID,
            Function = entry.FunctionName?.Content
        })];
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
        if (projectContext.Data.Sequences.ByName(DataName) is UndertaleSequence existing)
        {
            // Sequence found
            _dataAsset = existing;
        }
        else
        {
            // No sequence found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.Sequences.Add(_dataAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleSequence seq = _dataAsset;

        // Update all main properties
        seq.Playback = (UndertaleSequence.PlaybackType)Playback;
        seq.PlaybackSpeed = PlaybackSpeed;
        seq.PlaybackSpeedType = (AnimSpeedType)PlaybackSpeedType;
        seq.Length = Length;
        seq.OriginX = OriginX;
        seq.OriginY = OriginY;
        seq.Volume = Volume;
        seq.Width = Width;
        seq.Height = Height;

        // Update broadcast messages
        seq.BroadcastMessages = [.. BroadcastMessages.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.BroadcastMessage>()
        {
            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
            Channels = [.. 
                keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.BroadcastMessage>.KeyframeChannel()
                {
                    Channel = channel.Channel,
                    Value = new UndertaleSequence.BroadcastMessage()
                    {
                        Messages = [.. channel.Value.Select((message) => projectContext.MakeString(message))],
                    }
                })]
        })];

        // Update moments
        seq.Moments = [.. Moments.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.Moment>()
        {
            Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
            Channels = [..
                keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.Moment>.KeyframeChannel()
                {
                    Channel = channel.Channel,
                    Value = new UndertaleSequence.Moment()
                    {
                        Events = [.. channel.Value.Select(ev => projectContext.MakeString(ev)) ]
                    }
                })]
        })];

        // Update tracks
        Func<Track, UndertaleSequence.Track> convertTrack = null;
        convertTrack = (track) => new UndertaleSequence.Track()
        {
            ModelName = projectContext.MakeString(track.ModelName),
            Name = projectContext.MakeString(track.Name),
            BuiltinName = (UndertaleSequence.Track.TrackBuiltinName)track.BuiltinName,
            Traits = (UndertaleSequence.Track.TrackTraits)track.Traits,
            IsCreationTrack = track.IsCreationTrack,
            Tags = [.. track.Tags],
            Tracks = [.. track.Tracks.Select(convertTrack)],
            Keyframes = track.KeyframeStore switch
            {
                AudioKeyframes keyframes => new UndertaleSequence.AudioKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.AudioKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [.. 
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.AudioKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.AudioKeyframes.Data()
                                {
                                    Mode = channel.Value.Mode,
                                    Resource = new(projectContext.FindSound(channel.Value.SoundAsset, this))
                                }
                            })]
                    })]
                },
                InstanceKeyframes keyframes => new UndertaleSequence.InstanceKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.InstanceKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.InstanceKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.InstanceKeyframes.Data()
                                {
                                    Resource = new(projectContext.FindGameObject(channel.Value, this))
                                }
                            })]
                    })]
                },
                GraphicKeyframes keyframes => new UndertaleSequence.GraphicKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.GraphicKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.GraphicKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.GraphicKeyframes.Data()
                                {
                                    Resource = new(projectContext.FindSprite(channel.Value, this))
                                }
                            })]
                    })]
                },
                SequenceKeyframes keyframes => new UndertaleSequence.SequenceKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.SequenceKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.SequenceKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.SequenceKeyframes.Data()
                                {
                                    Resource = new(projectContext.FindSequence(channel.Value, this))
                                }
                            })]
                    })]
                },
                SpriteFramesKeyframes keyframes => new UndertaleSequence.SpriteFramesKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.SpriteFramesKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.SpriteFramesKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.SpriteFramesKeyframes.Data()
                                {
                                    Value = channel.Value
                                }
                            })]
                    })]
                },
                BoolKeyframes keyframes => new UndertaleSequence.BoolKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.BoolKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.BoolKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.BoolKeyframes.Data()
                                {
                                    Value = channel.Value
                                }
                            })]
                    })]
                },
                StringKeyframes keyframes => new UndertaleSequence.StringKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.StringKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.StringKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.StringKeyframes.Data()
                                {
                                    Value = projectContext.MakeString(channel.Value)
                                }
                            })]
                    })]
                },
                RealKeyframes keyframes => new UndertaleSequence.RealKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.RealData>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.RealData>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.RealData()
                                {
                                    Value = channel.Value.Value,
                                    IsCurveEmbedded = channel.Value.AnimCurveEmbedded is not null,
                                    EmbeddedAnimCurve = channel.Value.AnimCurveEmbedded?.ImportSubResource(projectContext),
                                    AssetAnimCurve = new(projectContext.FindAnimationCurve(channel.Value.AnimCurveAsset, this))
                                }
                            })]
                    })],
                    Interpolation = keyframes.Interpolation
                },
                TextKeyframes keyframes => new UndertaleSequence.TextKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.TextKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.TextKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.TextKeyframes.Data()
                                {
                                    Text = projectContext.MakeString(channel.Value.Text),
                                    Wrap = channel.Value.Wrap,
                                    AlignmentV = channel.Value.AlignmentV,
                                    AlignmentH = channel.Value.AlignmentH,
                                    FontIndex = projectContext.FindFontIndex(channel.Value.FontAsset, this)
                                }
                            })]
                    })]
                },
                ParticleKeyframes keyframes => new UndertaleSequence.ParticleKeyframes()
                {
                    List = [.. keyframes.Keyframes.Select((keyframe) => new UndertaleSequence.Keyframe<UndertaleSequence.ParticleKeyframes.Data>()
                    {
                        Key = keyframe.Key, Length = keyframe.Length, Stretch = keyframe.Stretch, Disabled = keyframe.Disabled,
                        Channels = [..
                            keyframe.Channels.Select((channel) => new UndertaleSequence.Keyframe<UndertaleSequence.ParticleKeyframes.Data>.KeyframeChannel()
                            {
                                Channel = channel.Channel,
                                Value = new UndertaleSequence.ParticleKeyframes.Data()
                                {
                                    Resource = new(projectContext.FindParticleSystem(channel.Value, this))
                                }
                            })]
                    })]
                },
                null => null,
                _ => throw new ProjectException($"Unknown keyframe store type {track.KeyframeStore.GetType().Name}")
            },
            OwnedResources = [.. track.OwnedResources.Select((resource) => resource switch
            {
                SerializableAnimationCurve curve => curve.ImportSubResource(projectContext),
                _ => throw new ProjectException($"Unknown owned resource type {resource.GetType().Name}")
            })]
        };
        seq.Tracks = [.. Tracks.Select(convertTrack)];

        // Update function IDs
        seq.FunctionIDs = [.. FunctionIDs.Select((entry) => new UndertaleSequence.FunctionIDEntry()
            {
                ID = entry.ID,
                FunctionName = projectContext.MakeString(entry.Function)
            })];

        return seq;
    }

    /// <summary>
    /// Imports this asset as a sub-resource of another asset.
    /// </summary>
    internal UndertaleSequence ImportSubResource(ProjectContext projectContext)
    {
        _dataAsset = new UndertaleSequence();
        if (DataName is not null)
        {
            _dataAsset.Name = projectContext.MakeString(DataName);
        }
        Import(projectContext);
        return _dataAsset;
    }
}
