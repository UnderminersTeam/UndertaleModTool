using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleScript : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private UndertaleResourceById<UndertaleCode> _Code { get; } = new UndertaleResourceById<UndertaleCode>("CODE");

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleCode Code { get => _Code.Resource; set { _Code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Code")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(_Code.Serialize(writer));
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            _Code.Unserialize(reader, reader.ReadInt32());
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
