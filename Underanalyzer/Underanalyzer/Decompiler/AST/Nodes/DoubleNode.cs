/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Globalization;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a double constant in the AST.
/// </summary>
public class DoubleNode(double value) : IConstantNode<double>, IConditionalValueNode
{
    public double Value { get; } = value;

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Double;

    public string ConditionalTypeName => "Double";
    public string ConditionalValue => Value.ToString("R", CultureInfo.InvariantCulture); // TODO: maybe do full conversion here

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        if (cleaner.Context.Settings.TryGetPredefinedDouble(Value, out string? predefined, out bool isMultiPart))
        {
            if (isMultiPart)
            {
                return new PredefinedDoubleMultiNode(predefined, Value);
            }
            else
            {
                return new PredefinedDoubleSingleNode(predefined, Value);
            }
        }
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        ReadOnlySpan<char> str = Value.ToString("R", CultureInfo.InvariantCulture);
        
        // Check if there's an exponent in the result
        int exponentIndex = str.IndexOf('E');
        if (exponentIndex == -1)
        {
            // No exponent
            printer.Write(str);
            return;
        }

        // We need to get rid of exponent notation manually...
        // Exponent to fixed point conversion inspired by https://stackoverflow.com/a/49663470
        int exponentValue = int.Parse(str[(exponentIndex + 1)..]);

        string resultStr;
        int numDecimals = 0;
        if (Value < 0)
        {
            int len = exponentIndex - 3;
            if (exponentValue >= 0)
            {
                if (len > 0)
                {
                    resultStr = str[0..2].ToString() + str[3..(3 + len)].ToString();
                    numDecimals = len;
                }
                else
                {
                    resultStr = str[0..2].ToString();
                }
            }
            else
            {
                if (len > 0)
                {
                    resultStr = str[1] + str[3..(3 + len)].ToString();
                    numDecimals = len;
                }
                else
                {
                    resultStr = str[1].ToString();
                }
            }
        }
        else
        {
            int len = exponentIndex - 2;
            if (len > 0)
            {
                resultStr = str[0] + str[2..(2 + len)].ToString();
                numDecimals = len;
            }
            else
                resultStr = str[0].ToString();
        }

        if (exponentValue >= 0)
        {
            exponentValue -= numDecimals;
            resultStr += new string('0', exponentValue);
        }
        else
        {
            exponentValue = (-exponentValue - 1);
            if (Value < 0)
                resultStr = "-0." + new string('0', exponentValue) + resultStr;
            else
                resultStr = "0." + new string('0', exponentValue) + resultStr;
        }

        printer.Write(resultStr);
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }
}
