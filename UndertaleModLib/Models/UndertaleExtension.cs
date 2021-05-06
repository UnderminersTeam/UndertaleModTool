using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;

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

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleExtensionFunctionArg : UndertaleObject
    {
        public UndertaleExtensionVarType Type { get; set; }

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

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleExtensionFunction : UndertaleObject
    {
        public UndertaleString Name { get; set; }
        public uint ID { get; set; } 
        public uint Kind { get; set; }
        public UndertaleExtensionVarType RetType { get; set; }
        public UndertaleString ExtName { get; set; }
        public UndertaleSimpleList<UndertaleExtensionFunctionArg> Arguments { get; set; } = new UndertaleSimpleList<UndertaleExtensionFunctionArg>();

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

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleExtensionFile : UndertaleObject
    {
        public UndertaleString Filename { get; set; }
        public UndertaleString CleanupScript { get; set; }
        public UndertaleString InitScript { get; set; }
        public UndertaleExtensionKind Kind { get; set; }
        public UndertalePointerList<UndertaleExtensionFunction> Functions { get; set; } = new UndertalePointerList<UndertaleExtensionFunction>();

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

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleExtension : UndertaleNamedResource
    {
/*                  <LineBreak/>
                - Folder Name thing is a remnant from the legacy
                  <LineBreak/>
                  GM7-8.1 extension editor(aka ExtMaker), Runner reads the name but ignores it.
                  <LineBreak/>
                  (although I advise that you don't change it...)
*/
        public UndertaleString FolderName { get; set; }
        public UndertaleString Name { get; set; }
        public UndertaleString ClassName { get; set; }

        public UndertalePointerList<UndertaleExtensionFile> Files { get; set; } = new UndertalePointerList<UndertaleExtensionFile>();

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
