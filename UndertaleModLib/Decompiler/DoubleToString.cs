using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UndertaleModLib.Decompiler
{
    // Inspired from https://stackoverflow.com/a/49663470, but reimplemented to be more readable.
    public static class DoubleToString
    {
        // This small lookup dictionary is for readability sake. So that if someone opens up a code entry,
        // they don't see "var foo = 0.66666666666" but instead "var foo = 2/3". The values have been mostly taken from Undertale/Deltarune.
        public static Dictionary<double, string> PredefinedValues { get; private set; } = new Dictionary<double, string>()
        {
            { 3.141592653589793, "pi" },
            { 6.283185307179586, "(2 * pi)" },
            { 12.566370614359172, "(4 * pi)" },
            { 31.41592653589793, "(10 * pi)" },
            { 0.3333333333333333, "(1/3)" },
            { 0.6666666666666666, "(2/3)" },
            { 1.3333333333333333, "(4/3)" },
            { 23.333333333333332, "(70/3)" },
            { 73.33333333333333, "(220/3)" },
            { 206.66666666666666, "(620/3)" },
            { 0.06666666666666667, "(1/15)" },
            { 0.03333333333333333, "(1/30)" },
            { 0.008333333333333333, "(1/120)" },
            { 51.42857142857143, "(360/7)" },
            { 1.0909090909090908, "(12/11)" },
            { 0.9523809523809523, "(20/21)" }
        };

        public static string StringOf(double number)
        {
            // This function ensures that a double that is converted to a string is later parsed back into the same numeric value.
            if (PredefinedValues.TryGetValue(number, out string res))
                return res;

            ReadOnlySpan<char> numberAsSpan = number.ToString("G17", CultureInfo.InvariantCulture).AsSpan();      // This can sometimes return a scientific notation
            int indexOfE = numberAsSpan.IndexOf("E".AsSpan());
            if (indexOfE < 0)
                return numberAsSpan.ToString();

            // This converts the scientific notation to standard form/fixed point notation
            // As of time of writing this comment, C# does not offer a way to print fixed point notation while preserving precision.
            // You may ask "But why not use F17?". And the answer is, that for everything but R and G, the precision is hard-capped to 15 (according to MSDN: Double.ToString()).
            // Thus we use G17 to keep our precision, and then manually convert this to fixed point notation.
            // For anyone unaware of the general algorithm: you get the exponent 'n', then move the decimal point n times to the right if it's positive / left if it's negative.
            ReadOnlySpan<char> exponentAsSpan = numberAsSpan.Slice(indexOfE + 1);
            int exponent = Int32.Parse(exponentAsSpan);

            StringBuilder builder = new();
            int indexOfFirstDigitAfterDecimalPoint = number < 0 ? 3 : 2;
            // Get digits before exponent

            // i.e. "-1.2E2"/"1.2E2". Our "E" has to be at least 3/2 places in. "-1.E2"/"1.E2" would not have any decimals at all.
            // Also a safety measure in case we get a string like "1E2".
            int numDecimals = indexOfE - indexOfFirstDigitAfterDecimalPoint;
            if (numDecimals < 0)
                numDecimals = 0;
            
            // If the number is not negative, that means that the first character is a digit, that we want to copy.
            if (number >= 0)
            {
                builder.Append(numberAsSpan[0]);
            }
            // If the number is negative and the exponent is negative, that means that we will have to prepend 0's,
            // which means we can't copy the '-' (first) character in that case.
            else
            {
                if (exponent >= 0)
                    builder.Append(numberAsSpan[0]);
                builder.Append(numberAsSpan[1]);
            }
            
            // If we have decimal digits, append them too.
            if (numDecimals > 0)
            {
                builder.Append(numberAsSpan.Slice(indexOfFirstDigitAfterDecimalPoint, numDecimals));
            }
            

            // Move our "decimal point".
            if (exponent >= 0)
            {
                exponent -= numDecimals;
                builder.Append('0', exponent);
            }
            else
            {
                exponent = (-exponent - 1);     // -1, because we're manually inserting a "0."
                builder.Insert(0, "0", exponent);
                builder.Insert(0, "0.");
                if (number < 0)
                    builder.Insert(0, '-');
            }

            return builder.ToString();
        }
    }
}
