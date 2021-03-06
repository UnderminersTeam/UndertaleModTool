using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace UndertaleModLib.Models
{
    public class UndertaleExtension : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _EmptyString;
        private UndertaleString _Name;
        private UndertaleString _ClassName;
        private UndertalePointerList<ExtensionFile> _Files = new UndertalePointerList<ExtensionFile>();

        public UndertaleString EmptyString { get => _EmptyString; set { _EmptyString = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EmptyString")); } }
        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleString ClassName { get => _ClassName; set { _ClassName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClassName")); } }

        public UndertalePointerList<ExtensionFile> Files { get => _Files; set { _Files = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Files")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

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

        public class ExtensionFile : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleString _Filename;
            private UndertaleString _CleanupScript;
            private UndertaleString _InitScript;
            private ExtensionKind _Kind;
            private UndertalePointerList<ExtensionFunction> _Functions = new UndertalePointerList<ExtensionFunction>();

            public UndertaleString Filename { get => _Filename; set { _Filename = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Filename")); } }
            public UndertaleString CleanupScript { get => _CleanupScript; set { _CleanupScript = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CleanupScript")); } }
            public UndertaleString InitScript { get => _InitScript; set { _InitScript = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InitScript")); } }
            public ExtensionKind Kind { get => _Kind; set { _Kind = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kind")); } }
            public UndertalePointerList<ExtensionFunction> Functions { get => _Functions; set { _Functions = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Functions")); } }

            public event PropertyChangedEventHandler PropertyChanged;

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

            public override string ToString()
            {
                return Filename.Content + " (" + GetType().Name + ")";
            }
        }

        public class ExtensionFunction : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleString _Name;
            private uint _ID;
            private uint _Kind;
            private ExtensionVarType _RetType;
            private UndertaleString _ExtName;
            private UndertaleSimpleList<ExtensionFunctionArg> _Arguments = new UndertaleSimpleList<ExtensionFunctionArg>();

            public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
            public uint ID { get => _ID; set { _ID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ID")); } }
            public uint Kind { get => _Kind; set { _Kind = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kind")); } }
            public ExtensionVarType RetType { get => _RetType; set { _RetType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RetType")); } }
            public UndertaleString ExtName { get => _ExtName; set { _ExtName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ExtName")); } }
            public UndertaleSimpleList<ExtensionFunctionArg> Arguments { get => _Arguments; set { _Arguments = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Arguments")); } }

            public event PropertyChangedEventHandler PropertyChanged;

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

        public class ExtensionFunctionArg : UndertaleObject, INotifyPropertyChanged
        {
            private ExtensionVarType _Type;

            public ExtensionVarType Type { get => _Type; set { _Type = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Type")); } }

            public event PropertyChangedEventHandler PropertyChanged;

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
