using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    // Implemented from https://stackoverflow.com/a/49663470
    public static class RoundTrip
    {
        private static string[] zeros = new string[1000];
        public static Dictionary<double, string> PredefinedValues { get; private set; } = new Dictionary<double, string>()
        {
            { 3.141592653589793, "pi" },
            { 6.283185307179586, "2 * pi" },
            { 12.566370614359172, "4 * pi" },
            { 31.41592653589793, "10 * pi" },
            { 0.3333333333333333, "1/3" },
            { 0.6666666666666666, "2/3" },
            { 1.3333333333333333, "4/3" },
            { 23.333333333333332, "70/3" },
            { 73.33333333333333, "220/3" },
            { 206.66666666666666, "620/3" },
            { 0.06666666666666667, "1/15" },
            { 0.03333333333333333, "1/30" },
            { 0.008333333333333333, "1/120" },
            { 51.42857142857143, "360/7" },
            { 1.0909090909090908, "12/11" },
            { 0.9523809523809523, "20/21" }
        };

        static RoundTrip()
        {
            for (int i = 0; i < zeros.Length; i++)
            {
                zeros[i] = new string('0', i);
            }
        }

        public static string ToRoundTrip(double value)
        {
            if (PredefinedValues.TryGetValue(value, out string res))
                return res;

            string str = value.ToString("r", CultureInfo.InvariantCulture);
            int x = str.IndexOf('E');
            int x_lower = str.IndexOf('e');
            if ((x < 0) && (x_lower < 0))
                return str;
            else
            {
                x = ((x < 0) ? x_lower : x);
            }
            int x1 = x + 1;
            string exp = str.Substring(x1, str.Length - x1);
            int e = int.Parse(exp);

            string s;
            int numDecimals = 0;
            if (value < 0)
            {
                int len = x - 3;
                if (e >= 0)
                {
                    if (len > 0)
                    {
                        s = str.Substring(0, 2) + str.Substring(3, len);
                        numDecimals = len;
                    }
                    else
                        s = str.Substring(0, 2);
                }
                else
                {
                    // remove the leading minus sign
                    if (len > 0)
                    {
                        s = str.Substring(1, 1) + str.Substring(3, len);
                        numDecimals = len;
                    }
                    else
                        s = str.Substring(1, 1);
                }
            }
            else
            {
                int len = x - 2;
                if (len > 0)
                {
                    s = str[0] + str.Substring(2, len);
                    numDecimals = len;
                }
                else
                    s = str[0].ToString();
            }

            if (e >= 0)
            {
                e = e - numDecimals;
                string z = (e < zeros.Length ? zeros[e] : new string('0', e));
                s = s + z;
            }
            else
            {
                e = (-e - 1);
                string z = (e < zeros.Length ? zeros[e] : new string('0', e));
                if (value < 0)
                    s = "-0." + z + s;
                else
                    s = "0." + z + s;
            }

            return s;
        }
    }
}
