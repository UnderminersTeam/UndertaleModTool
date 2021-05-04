using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // NOTE: Never seen in GMS1.4 so I'm not sure if the structure was the same
    public class UndertaleGlobalInit : UndertaleObject, INotifyPropertyChanged
    {
        private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _Code = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
        public UndertaleCode Code { get => _Code.Resource; set { _Code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Code))); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            _Code.Serialize(writer);
        }

        public void Unserialize(UndertaleReader reader)
        {
            _Code = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
            _Code.Unserialize(reader); // TODO: reader.ReadUndertaleObject if one object starts with another one
        }
    }
}
