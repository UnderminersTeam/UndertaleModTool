/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Compiler.Errors;

/// <summary>
/// Represents an error produced by the compiler.
/// </summary>
public interface ICompileError
{
    /// <summary>
    /// A simple, but possibly non-user-friendly error message.
    /// </summary>
    public string BaseMessage { get; }

    /// <summary>
    /// Generates a full, user-friendly error message based on the contents of this compile error.
    /// </summary>
    /// <returns>Generated message</returns>
    public string GenerateMessage();
}
