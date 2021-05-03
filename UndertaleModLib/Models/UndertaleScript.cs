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
        public UndertaleString Name { get; set; }
        private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _Code = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
        public UndertaleCode Code { get => _Code.Resource; set { _Code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Code")); } }
        public bool Constructor { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            if (Constructor)
                writer.Write((uint)_Code.SerializeById(writer) | 2147483648u);
            else
                writer.WriteUndertaleObject(_Code);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            int id = reader.ReadInt32();
            if (id < -1)
            {
                Constructor = true;
                id = (int)((uint)id & 2147483647u);
            }
            _Code.UnserializeById(reader, id);
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
