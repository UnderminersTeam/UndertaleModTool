using System.Collections.Generic;

namespace UndertaleModLib.Models
{
    /// <summary>
    /// A tag entry in a GameMaker data file.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleTags : UndertaleObject
    {
        public UndertaleSimpleListString Tags { get; set; }
        public Dictionary<int, UndertaleSimpleListString> AssetTags { get; set; }

        //TODO: condense these all into one method
        public static int GetAssetTagID(UndertaleData data, UndertaleGameObject res)
        {
            return ((int)ResourceType.Object << 24) | (data.GameObjects.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleSprite res)
        {
            return ((int)ResourceType.Sprite << 24) | (data.Sprites.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleSound res)
        {
            return ((int)ResourceType.Sound << 24) | (data.Sounds.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleRoom res)
        {
            return ((int)ResourceType.Room << 24) | (data.Rooms.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertalePath res)
        {
            return ((int)ResourceType.Path << 24) | (data.Paths.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleScript res)
        {
            // TODO? the indexof might be in terms of code entries or something?
            return ((int)ResourceType.Script << 24) | ((data.Scripts.IndexOf(res) + 100000) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleFont res)
        {
            return ((int)ResourceType.Font << 24) | (data.Fonts.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleTimeline res)
        {
            return ((int)ResourceType.Timeline << 24) | (data.Timelines.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleBackground res)
        {
            return ((int)ResourceType.Background << 24) | (data.Backgrounds.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleShader res)
        {
            return ((int)ResourceType.Shader << 24) | (data.Shaders.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleSequence res)
        {
            return ((int)ResourceType.Sequence << 24) | (data.Sequences.IndexOf(res) & 0xFFFFFF);
        }

        public static int GetAssetTagID(UndertaleData data, UndertaleAnimationCurve res)
        {
            return ((int)ResourceType.AnimCurve << 24) | (data.AnimationCurves.IndexOf(res) & 0xFFFFFF);
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
}
