using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace UndertaleModLib.Models
{
    public enum UndertaleExtensionKind : uint
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

    public enum UndertaleExtensionVarType : uint
    {
        String = 1,
        Double = 2
    }

    public class UndertaleExtensionFunctionArg : UndertaleObject, INotifyPropertyChanged
    {
        private UndertaleExtensionVarType _Type;

        public UndertaleExtensionVarType Type { get => _Type; set { _Type = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Type")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public UndertaleExtensionFunctionArg()
        {
            Type = UndertaleExtensionVarType.Double;
        }

        public UndertaleExtensionFunctionArg(UndertaleExtensionVarType type)
        {
            Type = type;
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Type);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Type = (UndertaleExtensionVarType)reader.ReadUInt32();
        }
    }

    public class UndertaleExtensionFunction : UndertaleObject, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private uint _ID;
        private uint _Kind;
        private UndertaleExtensionVarType _RetType;
        private UndertaleString _ExtName;
        private UndertaleSimpleList<UndertaleExtensionFunctionArg> _Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>();

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public uint ID { get => _ID; set { _ID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ID")); } }
        public uint Kind { get => _Kind; set { _Kind = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kind")); } }
        public UndertaleExtensionVarType RetType { get => _RetType; set { _RetType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RetType")); } }
        public UndertaleString ExtName { get => _ExtName; set { _ExtName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ExtName")); } }
        public UndertaleSimpleList<UndertaleExtensionFunctionArg> Arguments { get => _Arguments; set { _Arguments = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Arguments")); } }

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
            RetType = (UndertaleExtensionVarType)reader.ReadUInt32();
            ExtName = reader.ReadUndertaleString();
            Arguments = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleExtensionFunctionArg>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + ExtName.Content + ")";
        }
    }

    public class UndertaleExtensionFile : UndertaleObject, INotifyPropertyChanged
    {
        private UndertaleString _Filename;
        private UndertaleString _CleanupScript;
        private UndertaleString _InitScript;
        private UndertaleExtensionKind _Kind;
        private UndertalePointerList<UndertaleExtensionFunction> _Functions = new UndertalePointerList<UndertaleExtensionFunction>();

        public UndertaleString Filename { get => _Filename; set { _Filename = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Filename")); } }
        public UndertaleString CleanupScript { get => _CleanupScript; set { _CleanupScript = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CleanupScript")); } }
        public UndertaleString InitScript { get => _InitScript; set { _InitScript = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InitScript")); } }
        public UndertaleExtensionKind Kind { get => _Kind; set { _Kind = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kind")); } }
        public UndertalePointerList<UndertaleExtensionFunction> Functions { get => _Functions; set { _Functions = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Functions")); } }

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
            Kind = (UndertaleExtensionKind)reader.ReadUInt32();
            Functions = reader.ReadUndertaleObject<UndertalePointerList<UndertaleExtensionFunction>>();
        }

        public override string ToString()
        {
            return Filename.Content + " (" + GetType().Name + ")";
        }
    }

    public class UndertaleExtension : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _FolderName;
        private UndertaleString _Name;
        private UndertaleString _ClassName;
        private UndertalePointerList<UndertaleExtensionFile> _Files = new UndertalePointerList<UndertaleExtensionFile>();

        public UndertaleString FolderName { get => _FolderName; set { _FolderName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FolderName")); } }
        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleString ClassName { get => _ClassName; set { _ClassName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ClassName")); } }

        public UndertalePointerList<UndertaleExtensionFile> Files { get => _Files; set { _Files = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Files")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(FolderName);
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(ClassName);
            writer.WriteUndertaleObject(Files);
        }

        public void Unserialize(UndertaleReader reader)
        {
            FolderName = reader.ReadUndertaleString();
            Name = reader.ReadUndertaleString();
            ClassName = reader.ReadUndertaleString();
            Files = reader.ReadUndertaleObject<UndertalePointerList<UndertaleExtensionFile>>();
        }
    }
}
