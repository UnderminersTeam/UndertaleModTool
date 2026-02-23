/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Macro type for instance type references.
/// </summary>
public class InstanceMacroType : IMacroTypeInt32
{
    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data)
    {
        // In GMLv2, don't resolve self, other, and global in versions where those no longer compile to numbers
        if (cleaner.Context.GMLv2)
        {
            if (data is (int)IGMInstruction.InstanceType.Self or (int)IGMInstruction.InstanceType.Other)
            {
                return null;
            }
            if (data is (int)IGMInstruction.InstanceType.Global && cleaner.Context.GameContext.UsingGlobalConstantFunction)
            {
                return null;
            }
        }

        return data switch
        {
            (int)IGMInstruction.InstanceType.Self => new InstanceTypeNode(IGMInstruction.InstanceType.Self),
            (int)IGMInstruction.InstanceType.Other => new InstanceTypeNode(IGMInstruction.InstanceType.Other),
            (int)IGMInstruction.InstanceType.All => new InstanceTypeNode(IGMInstruction.InstanceType.All),
            (int)IGMInstruction.InstanceType.Noone => new InstanceTypeNode(IGMInstruction.InstanceType.Noone),
            (int)IGMInstruction.InstanceType.Global => new InstanceTypeNode(IGMInstruction.InstanceType.Global),
            _ => null
        };
    }
}
