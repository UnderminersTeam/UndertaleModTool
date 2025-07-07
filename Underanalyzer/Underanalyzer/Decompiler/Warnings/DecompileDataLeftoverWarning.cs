/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler;

/// <summary>
/// Represents a warning that occurs when data is left over on the VM stack at the end of a fragment.
/// With default settings, this is not a warning, and is instead an exception.
/// </summary>
public class DecompileDataLeftoverWarning : IDecompileWarning
{
    public string Message => $"Data left over on VM stack at end of fragment ({NumberOfElements} elements).";
    public string CodeEntryName { get; }
    public int NumberOfElements { get; }

    internal DecompileDataLeftoverWarning(int numberOfElements, string codeEntryName)
    {
        NumberOfElements = numberOfElements;
        CodeEntryName = codeEntryName;
    }
}
