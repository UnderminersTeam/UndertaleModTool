/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using static Underanalyzer.Compiler.Nodes.BinaryChainNode;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer;

/// <summary>
/// Extension methods for <see cref="DataType"/>.
/// </summary>
internal static class VMDataTypeExtensions
{
    /// <summary>
    /// Given two <see cref="DataType"/> enumerations and an <see cref="Opcode"/>, this method returns the 
    /// data type result from a binary operation of the two types, with the given opcode.
    /// </summary>
    public static DataType BinaryResultWith(this DataType type1, Opcode opcode, DataType type2)
    {
        // Depending on opcode being used, and given certain type combinations, choose specific result types.
        switch (opcode)
        {
            case Opcode.Subtract:
            case Opcode.Divide:
            case Opcode.GMLModulo:
            case Opcode.And:
            case Opcode.Or:
            case Opcode.Xor:
            case Opcode.ShiftLeft:
            case Opcode.ShiftRight:
                if (type1 == DataType.String || type2 == DataType.String)
                {
                    return DataType.Double;
                }
                break;
            case Opcode.GMLDivRemainder:
                if ((type1 == DataType.String && type2 != DataType.Variable) || type2 == DataType.String)
                {
                    return DataType.Double;
                }
                break;
            case Opcode.Compare:
                return DataType.Boolean;
        }

        // Choose whichever type has a higher bias, or if equal, the smaller numerical data type value.
        int bias1 = StackTypeBias(type1);
        int bias2 = StackTypeBias(type2);
        if (bias1 == bias2)
        {
            return (DataType)Math.Min((byte)type1, (byte)type2);
        }
        else
        {
            return (bias1 > bias2) ? type1 : type2;
        }
    }

    /// <summary>
    /// Returns the bias a given data type has in a binary operation. Larger is greater bias.
    /// </summary>
    private static int StackTypeBias(DataType type)
    {
        return type switch
        {
            DataType.Int32 or DataType.Boolean or DataType.String => 0,
            DataType.Double or DataType.Int64 => 1,
            DataType.Variable => 2,
            _ => throw new Exception("Unknown data type")
        };
    }
}
