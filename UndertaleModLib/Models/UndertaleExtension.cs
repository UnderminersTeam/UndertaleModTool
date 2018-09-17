using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: INotifyPropertyChanged
    public class UndertaleExtension : UndertaleObject
    {
        public UndertaleString EmptyString;
        public UndertaleString Name;
        public UndertaleString ClassName;
        public UndertalePointerList<ExtensionFile> Files = new UndertalePointerList<ExtensionFile>();

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(EmptyString);
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(ClassName);
            writer.WriteUndertaleObject(Files);
        }

        public void Unserialize(UndertaleReader reader)
        {
            EmptyString = reader.ReadUndertaleString();
            Name = reader.ReadUndertaleString();
            ClassName = reader.ReadUndertaleString();
            Files = reader.ReadUndertaleObject<UndertalePointerList<ExtensionFile>>();
        }

        public enum ExtensionKind : uint
        {
            [Obsolete("Likely unused")]
            Unknown0 = 0,
            DLL = 1,
            GML = 2,
            [Obsolete("Potentially unused before GM:S")]
            Unknown3 = 3,
            Generic = 4,
            JS = 5
        }

        public enum ExtensionVarType : uint
        {
            String = 1,
            Double = 2
        }

        public class ExtensionFile : UndertaleObject
        {
            public UndertaleString Filename;
            public UndertaleString CleanupScript;
            public UndertaleString InitScript;
            public ExtensionKind Kind;
            public UndertalePointerList<ExtensionFunction> Functions = new UndertalePointerList<ExtensionFunction>();

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Filename);
                writer.WriteUndertaleString(CleanupScript);
                writer.WriteUndertaleString(InitScript);
                writer.Write((uint)Kind);
                writer.WriteUndertaleObject(Functions);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Filename = reader.ReadUndertaleString();
                CleanupScript = reader.ReadUndertaleString();
                InitScript = reader.ReadUndertaleString();
                Kind = (ExtensionKind)reader.ReadUInt32();
                Functions = reader.ReadUndertaleObject<UndertalePointerList<ExtensionFunction>>();
            }
        }

        public class ExtensionFunction : UndertaleObject
        {
            public UndertaleString Name;
            public uint ID;
            public uint Kind;
            public ExtensionVarType RetType;
            public UndertaleString ExtName;
            public UndertaleSimpleList<ExtensionFunctionArg> Arguments = new UndertaleSimpleList<ExtensionFunctionArg>();

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Name);
                writer.Write(ID);
                writer.Write(Kind);
                writer.Write((uint)RetType);
                writer.WriteUndertaleString(ExtName);
                writer.WriteUndertaleObject(Arguments);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Name = reader.ReadUndertaleString();
                ID = reader.ReadUInt32();
                Kind = reader.ReadUInt32();
                RetType = (ExtensionVarType)reader.ReadUInt32();
                ExtName = reader.ReadUndertaleString();
                Arguments = reader.ReadUndertaleObject<UndertaleSimpleList<ExtensionFunctionArg>>();
            }
        }

        public class ExtensionFunctionArg : UndertaleObject
        {
            public ExtensionVarType Type;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write((uint)Type);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Type = (ExtensionVarType)reader.ReadUInt32();
            }
        }
    }
}
