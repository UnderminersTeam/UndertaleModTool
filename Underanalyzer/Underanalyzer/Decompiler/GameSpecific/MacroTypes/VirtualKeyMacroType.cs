/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Macro type for vk_* constants, and relevant calls to ord().
/// </summary>3
public class VirtualKeyMacroType : IMacroTypeInt32
{
    private static Dictionary<int, string> Constants { get; } = new()
    {
        { 0, "vk_nokey" },
        { 1, "vk_anykey" },
        { 8, "vk_backspace" },
        { 9, "vk_tab" },
        { 13, "vk_enter" },
        { 16, "vk_shift" },
        { 17, "vk_control" },
        { 18, "vk_alt" },
        { 19, "vk_pause" },
        { 27, "vk_escape" },
        { 32, "vk_space" },
        { 33, "vk_pageup" },
        { 34, "vk_pagedown" },
        { 35, "vk_end" },
        { 36, "vk_home" },
        { 37, "vk_left" },
        { 38, "vk_up" },
        { 39, "vk_right" },
        { 40, "vk_down" },
        { 44, "vk_printscreen" },
        { 45, "vk_insert" },
        { 46, "vk_delete" },
        { 96, "vk_numpad0" },
        { 97, "vk_numpad1" },
        { 98, "vk_numpad2" },
        { 99, "vk_numpad3" },
        { 100, "vk_numpad4" },
        { 101, "vk_numpad5" },
        { 102, "vk_numpad6" },
        { 103, "vk_numpad7" },
        { 104, "vk_numpad8" },
        { 105, "vk_numpad9" },
        { 106, "vk_multiply" },
        { 107, "vk_add" },
        { 109, "vk_subtract" },
        { 110, "vk_decimal" },
        { 111, "vk_divide" },
        { 112, "vk_f1" },
        { 113, "vk_f2" },
        { 114, "vk_f3" },
        { 115, "vk_f4" },
        { 116, "vk_f5" },
        { 117, "vk_f6" },
        { 118, "vk_f7" },
        { 119, "vk_f8" },
        { 120, "vk_f9" },
        { 121, "vk_f10" },
        { 122, "vk_f11" },
        { 123, "vk_f12" },
        { 160, "vk_lshift" },
        { 161, "vk_rshift" },
        { 162, "vk_lcontrol" },
        { 163, "vk_rcontrol" },
        { 164, "vk_lalt" },
        { 165, "vk_ralt" }
    };

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data)
    {
        // Check if in constant list
        if (Constants.TryGetValue(data, out string? name))
        {
            return new MacroValueNode(name);
        }

        // Check if in A-Z or 0-9
        if ((data >= 'A' && data <= 'Z') || (data >= '0' && data <= '9'))
        {
            return new MacroValueNode($"ord(\"{(char)data}\")");
        }

        // All others are arbitrary numbers
        return null;
    }
}
