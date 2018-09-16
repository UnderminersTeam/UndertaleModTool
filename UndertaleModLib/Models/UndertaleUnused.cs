using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // NOTE: Never seen in GMS1.4 so I'm not sure if the structure was the same
    public class UndertaleGlobal : UndertaleObject, INotifyPropertyChanged
    {
        private UndertaleResourceById<UndertaleCode> _Code { get; } = new UndertaleResourceById<UndertaleCode>("CODE");
        public UndertaleCode BackgroundDefinition { get => _Code.Resource; set { _Code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Code")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((int)_Code.Serialize(writer));
        }

        public void Unserialize(UndertaleReader reader)
        {
            _Code.Unserialize(reader, reader.ReadInt32());
        }
    }

    public class UndertaleShader : UndertaleObject
    {
        public UndertaleShader()
        {
            throw new NotImplementedException();
        }

        public void Serialize(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Unserialize(UndertaleReader reader)
        {
            throw new NotImplementedException();
        }
    }

    public class UndertaleTimeline : UndertaleObject
    {
        public UndertaleTimeline()
        {
            throw new NotImplementedException();
        }

        public void Serialize(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Unserialize(UndertaleReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
