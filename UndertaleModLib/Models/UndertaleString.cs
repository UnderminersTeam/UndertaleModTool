using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Decompiler;

namespace UndertaleModLib.Models
{
    public class UndertaleString : UndertaleResource, INotifyPropertyChanged, ISearchable
    {
        private string _Content;

        public string Content { get => _Content; set { _Content = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Content")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public UndertaleString()
        {
        }

        public UndertaleString(string content)
        {
            this.Content = content;
        }

        public void Serialize(UndertaleWriter writer)
        {
            byte[] chars = Encoding.UTF8.GetBytes(Content);
            writer.Write((uint)chars.Length);
            writer.Write(chars);
            writer.Write((byte)0);
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint length = reader.ReadUInt32();
            byte[] chars = reader.ReadBytes((int)length);
            Content = Encoding.UTF8.GetString(chars);
            if (reader.ReadByte() != 0)
                throw new IOException("The string was not null terminated!");
        }

        public override string ToString()
        {
            return "\"" + Content + "\"";
        }

        public string ToString(DecompileContext context)
        {
            if (context.isGameMaker2)
                return "\"" + Content.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";

            return "\"" + Content.Replace("\"", "\" + chr(ord('\"')) + \"") + "\"";
        }

        public bool SearchMatches(string filter)
        {
            return Content?.ToLower().Contains(filter.ToLower()) ?? false;
        }
    }
}
