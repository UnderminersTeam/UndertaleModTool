/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler;

/// <summary>
/// Interface for types representing warnings emitted by the decompiler.
/// </summary>
public interface IDecompileWarning
{
    /// <summary>
    /// Human-readable message describing the warning.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Code entry name where the warning was emitted.
    /// </summary>
    public string CodeEntryName { get; }
}
