namespace UndertaleModLib.Models
{
    /// <summary>
    /// A string entry a data file can have.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleString : UndertaleResource, ISearchable
    {
        /// <summary>
        /// The contents of the string.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Initializes a new empty instance of type <see cref="UndertaleString"/>.
        /// </summary>
        public UndertaleString()
        {
        }

        /// <summary>
        /// Initializes a new instance of type <see cref="UndertaleString"/> with a specified content.
        /// </summary>
        /// <param name="content">The content for the string.</param>
        public UndertaleString(string content)
        {
            this.Content = content;
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteGMString(Content);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Content = reader.ReadGMString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ToString(true);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <param name="isGMS2">Whether the string is from a Game Maker: Studio 2 data file or not.</param>
        /// <returns>A string that represents the current object.</returns>
        public string ToString(bool isGMS2)
        {
            if (Content == null)
                return "\"null\""; // NPE Fix.

            if (isGMS2)
                return "\"" + Content.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";

            // Handle GM:S 1's lack of escaping
            string res = Content;
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

        /// <inheritdoc />
        public bool SearchMatches(string filter)
        {
            return Content?.ToLower().Contains(filter.ToLower()) ?? false;
        }

        /// <summary>
        /// Unescapes text for the assembler.
        /// </summary>
        /// <param name="text">The text to unescape.</param>
        /// <returns>A string with <c>\n</c>, <c>\r</c>, <c>"</c> and <c>\</c> being properly escaped.</returns>
        public static string UnescapeText(string text)
        {
            return text.Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
