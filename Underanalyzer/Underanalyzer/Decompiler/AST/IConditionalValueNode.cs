/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Interface for all AST nodes that can be used in conditional comparisons.
/// </summary>
public interface IConditionalValueNode : IMacroResolvableNode
{
    public string ConditionalTypeName { get; }
    public string ConditionalValue { get; }
}
