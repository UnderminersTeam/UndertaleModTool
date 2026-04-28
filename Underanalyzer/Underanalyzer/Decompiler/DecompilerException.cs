/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

namespace Underanalyzer.Decompiler;

/// <summary>Represents errors that occur during decompilation.</summary>
public class DecompilerException : Exception
{
    
    /// <summary>Initializes a new instance of the <see cref="DecompilerException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public DecompilerException(string message)
        : base(message)
    { }

    /// <summary>Initializes a new instance of the <see cref="DecompilerException" /> class with a specified error message and
    /// a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference
    /// (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public DecompilerException(string message, Exception inner)
        : base(message, inner)
    { }
}
