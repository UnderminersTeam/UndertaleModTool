﻿using System.Collections.ObjectModel;

namespace UndertaleModLib.ModelsDebug
{
    public class UndertaleDebugInfo : ObservableCollection<UndertaleDebugInfo.DebugInfoPair>, UndertaleResource
    {
        public class DebugInfoPair
        {
            public uint BytecodeOffset { get; set; }
            public uint SourceCodeOffset { get; set; }
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Count * 2);
            foreach(DebugInfoPair pair in this)
            {
                writer.Write(pair.BytecodeOffset);
                writer.Write(pair.SourceCodeOffset);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            Clear();
            uint count = reader.ReadUInt32();
            Util.DebugUtil.Assert(count % 2 == 0);
            count /= 2;
            for (int i = 0; i < count; i++)
                Add(new DebugInfoPair() { BytecodeOffset = reader.ReadUInt32(), SourceCodeOffset = reader.ReadUInt32() });
        }
    }
}
