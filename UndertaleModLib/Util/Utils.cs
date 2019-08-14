using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Util
{
    class Utils
    {
        private static string DECIMAL_CONSTANT = "0." + new string('#', 339);

        public static string DecimalToStringSafe(object Value) // Turns a number into a string without scientific notation.
        {
            if (Value is float) // Prevents scientific notation by using high bit number.
                return ((float)Value).ToString(DECIMAL_CONSTANT, CultureInfo.InvariantCulture);
            else if (Value is double) // Prevents scientific notation by using high bit number.
                return ((double)Value).ToString(DECIMAL_CONSTANT, CultureInfo.InvariantCulture);
            else
                return (Value as IFormattable)?.ToString(DECIMAL_CONSTANT, CultureInfo.InvariantCulture) ?? Value.ToString();
        }
    }
}
