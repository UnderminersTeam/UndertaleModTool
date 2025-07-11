/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Globalization;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Macro type for color constants.
/// </summary>
public class ColorMacroType : IMacroTypeInt32
{
    private static Dictionary<int, string> Constants { get; } = new()
    {
        { 16776960, "c_aqua" },
        { 0, "c_black" },
        { 16711680, "c_blue" },
        { 4210752, "c_dkgray" },
        { 16711935, "c_fuchsia" },
        { 8421504, "c_gray" },
        { 32768, "c_green" },
        { 65280, "c_lime" },
        { 12632256, "c_ltgray" },
        { 128, "c_maroon" },
        { 8388608, "c_navy" },
        { 32896, "c_olive" },
        { 4235519, "c_orange" },
        { 8388736, "c_purple" },
        { 255, "c_red" },
        // c_silver omitted (same as c_ltgray)
        { 8421376, "c_teal" },
        { 16777215, "c_white" },
        { 65535, "c_yellow" }
    };

    public IExpressionNode Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data)
    {
        // Check if in constant list
        if (Constants.TryGetValue(data, out string? name))
        {
            return new MacroValueNode(name);
        }

        if (data < 0)
        {
            // If negative, we can't safely guarantee accurate decompilation, so maintain same value.
            return node;
        }

        // Return hex literal
        if (data <= 0xffffff && cleaner.Context.Settings.UseCSSColors)
        {
            // Return RGB hex literal (reverse byte order). Swap R and B.
            int rgb = ((data & 0xff) << 16) | ((data & 0xff0000) >> 16) | (data & 0xff00);
            return new MacroValueNode("#" + rgb.ToString("X6", CultureInfo.InvariantCulture));
        }
        else
        {
            // Return normal hex literal (BGR)
            string notation = cleaner.Context.GameContext.UsingGMS2OrLater ? "0x" : "$";
            return new MacroValueNode(notation + data.ToString((data > 0xffffff) ? "X8" : "X6", CultureInfo.InvariantCulture));
        }
    }
}
