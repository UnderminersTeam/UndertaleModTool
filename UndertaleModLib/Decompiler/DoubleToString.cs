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
    //Implemented from https://stackoverflow.com/a/49663470
    public static class RoundTrip
    {

        private static String[] zeros = new String[1000];

        static RoundTrip()
        {
            for (int i = 0; i < zeros.Length; i++)
            {
                zeros[i] = new String('0', i);
            }
        }

        public static String ToRoundTrip(double value)
        {
            String str = value.ToString("r");
            int x = str.IndexOf('E');
            int x_lower = str.IndexOf('e');
            if ((x < 0) && (x_lower < 0))
                return str;
            else
            {
                x = ((x < 0) ? x_lower : x);
            }
            int x1 = x + 1;
            String exp = str.Substring(x1, str.Length - x1);
            int e = int.Parse(exp);

            String s = null;
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
                String z = (e < zeros.Length ? zeros[e] : new String('0', e));
                s = s + z;
            }
            else
            {
                e = (-e - 1);
                String z = (e < zeros.Length ? zeros[e] : new String('0', e));
                if (value < 0)
                    s = "-0." + z + s;
                else
                    s = "0." + z + s;
            }

            return s;
        }

        public static void RoundTripUnitTest()
        {
            StringBuilder sb33 = new StringBuilder();
            double[] values = new[] { 123450000000000000.0, 1.0 / 7, 10000000000.0/7, 100000000000000000.0/7, 0.001/7, 0.0001/7, 100000000000000000.0, 0.00000000001,
         1.23e-2, 1.234e-5, 1.2345E-10, 1.23456E-20, 5E-20, 1.23E+2, 1.234e5, 1.2345E10, -7.576E-05, 1.23456e20, 5e+20, 9.1093822E-31, 5.9736e24, double.Epsilon };

            foreach (int sign in new[] { 1, -1 })
            {
                foreach (double val in values)
                {
                    double val2 = sign * val;
                    String s1 = val2.ToString("r");
                    String s2 = ToRoundTrip(val2);

                    double val2_ = double.Parse(s2);
                    double diff = Math.Abs(val2 - val2_);
                    if (diff != 0)
                    {
                        throw new Exception("Value " + val.ToString("r") + " did not pass ToRoundTrip.");
                    }
                    sb33.AppendLine(s1);
                    sb33.AppendLine(s2);
                    sb33.AppendLine();
                }
            }
        }
    }
}
