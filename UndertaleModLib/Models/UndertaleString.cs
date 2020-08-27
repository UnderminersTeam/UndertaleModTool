using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            writer.WriteGMString(_Content);
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
            return ToString(true);
        }

        public string ToString(DecompileContext context)
        {
            return ToString(context.isGameMaker2);
        }

        public string ToString(bool isGMS2)
        {
            if (Content == null)
                return "\"null\""; // NPE Fix.

            if (isGMS2)
                return "\"" + Content.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";

            return "\"" + Content.Replace("\r\n", "\n").Replace("\"", "\" + chr(34) + \"") + "\""; // Do chr(34) instead of chr(ord('"')), because single-quoted strings aren't supported by the syntax highlighter currently.
        }

        public bool SearchMatches(string filter)
        {
            return Content?.ToLower().Contains(filter.ToLower()) ?? false;
        }

        public static string UnescapeText(string text, bool isGMS2 = true)
        {
            if (isGMS2)
                return text.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
            else 
                return text.Replace("\" + chr(34) + \"", "\"");
        }
    }
}
