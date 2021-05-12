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
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleString : UndertaleResource, ISearchable
    {
        public string Content { get; set; }

        public UndertaleString()
        {
        }

        public UndertaleString(string content)
        {
            this.Content = content;
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteGMString(Content);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Content = reader.ReadGMString();
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

            // Handle GM:S 1's lack of escaping
            string res = Content.Replace("\r\n", "\n");
            bool front, back;
            if (res.StartsWith('"'))
            {
                front = true;
                res = res.Remove(0, 1);
                if (res.Length == 0)
                    return "'\"'";
            }
            else
                front = false;
            if (res.EndsWith('"'))
            {
                res = res.Remove(res.Length - 1);
                back = true;
            }
            else
                back = false;
            res = res.Replace("\"", "\" + '\"' + \"");
            if (front)
                res = "'\"' + \"" + res;
            else
                res = "\"" + res;
            if (back)
                res += "\" + '\"'";
            else
                res += "\"";
            return res;
        }

        public bool SearchMatches(string filter)
        {
            return Content?.ToLower().Contains(filter.ToLower()) ?? false;
        }
        
        // Unescapes text for the assembler
        public static string UnescapeText(string text)
        {
            return text.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
