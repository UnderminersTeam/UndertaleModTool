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
        private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _Code = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleCode Code { get => _Code.Resource; set { _Code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Code")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleObject(_Code);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            _Code = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
